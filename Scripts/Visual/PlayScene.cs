using Godot;
using System;
using System.Collections.Generic;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Visual
{
    /// <summary>
    /// Main gameplay scene - renders map, players, and handles input
    /// Client is RENDERER ONLY - all logic on server
    /// </summary>
    public partial class PlayScene : Node2D
    {
        // Network
        private SpacetimeDBClient _client;
        private uint _localPlayerId;
        private string _currentMapId = "starting_area";
        
        // Camera
        private Camera2D _camera;
        
        // Map rendering
        private Node2D _mapContainer;
        private ColorRect _mapBackground;
        private Dictionary<string, Vector2> _mapSizes = new Dictionary<string, Vector2>
        {
            { "starting_area", new Vector2(1000, 1000) },
            { "forest_area", new Vector2(1200, 1200) }
        };
        
        // Player rendering
        private Dictionary<uint, Node2D> _playerSprites = new Dictionary<uint, Node2D>();
        private Node2D _localPlayerSprite;
        
        // Transition zones
        private Node2D _transitionContainer;
        
        // Debug markers
        private Node2D _debugContainer;
        
        // Input
        private uint _inputSequence = 0;
        private Vector2 _lastSentPosition = Vector2.Zero;
        
        public override void _Ready()
        {
            // Don't setup networking yet - wait for SetClient() to be called
            SetupCamera();
            SetupMap();
            SetupTransitionZones();
            
            GD.Print("[PlayScene] Initialized (waiting for client)");
        }
        
        public void Initialize()
        {
            // Called after SetClient()
            SetupNetworking();
            GD.Print("[PlayScene] Fully initialized with client");
        }
        
        private void SetupCamera()
        {
            _camera = new Camera2D();
            _camera.Enabled = true;
            AddChild(_camera);
        }
        
        private void SetupMap()
        {
            _mapContainer = new Node2D();
            _mapContainer.Name = "MapContainer";
            AddChild(_mapContainer);
            
            // Create map background
            _mapBackground = new ColorRect();
            _mapBackground.Name = "MapBackground";
            _mapContainer.AddChild(_mapBackground);
            
            // Create debug container
            _debugContainer = new Node2D();
            _debugContainer.Name = "DebugMarkers";
            _mapContainer.AddChild(_debugContainer);
            
            LoadMap(_currentMapId);
        }
        
        private void LoadMap(string mapId)
        {
            _currentMapId = mapId;
            
            if (!_mapSizes.ContainsKey(mapId))
            {
                GD.PrintErr($"[PlayScene] Unknown map: {mapId}");
                return;
            }
            
            var mapSize = _mapSizes[mapId];
            
            // Set background size and color
            _mapBackground.Size = mapSize;
            _mapBackground.Position = Vector2.Zero;
            
            // Different colors for different maps
            switch (mapId)
            {
                case "starting_area":
                    _mapBackground.Color = new Color(0.6f, 0.9f, 0.6f); // Light green (farm)
                    break;
                case "forest_area":
                    _mapBackground.Color = new Color(0.15f, 0.3f, 0.15f); // Dark green (forest)
                    break;
            }
            
            // Center camera on map
            _camera.Position = mapSize / 2;
            
            // Add debug markers
            RenderDebugMarkers(mapId);
            
            GD.Print($"[PlayScene] Loaded map: {mapId} ({mapSize.X}x{mapSize.Y}), camera centered at {_camera.Position}");
        }
        
        private void SetupTransitionZones()
        {
            _transitionContainer = new Node2D();
            _transitionContainer.Name = "TransitionZones";
            _mapContainer.AddChild(_transitionContainer);
            
            RenderTransitionZones(_currentMapId);
        }
        
        private void RenderTransitionZones(string mapId)
        {
            // Clear existing zones
            foreach (var child in _transitionContainer.GetChildren())
            {
                child.QueueFree();
            }
            
            // Render zones based on map
            switch (mapId)
            {
                case "starting_area":
                    // Right edge transition to forest_area
                    CreateTransitionZone(
                        new Rect2(950, 400, 50, 200),
                        "→ Dark Forest",
                        Colors.Yellow
                    );
                    break;
                    
                case "forest_area":
                    // Left edge transition to starting_area
                    CreateTransitionZone(
                        new Rect2(0, 400, 50, 200),
                        "← Starting Village",
                        Colors.Yellow
                    );
                    break;
            }
        }
        
        private void CreateTransitionZone(Rect2 rect, string label, Color color)
        {
            // Debug area - more visible border
            var border = new ColorRect();
            border.Position = rect.Position - new Vector2(2, 2);
            border.Size = rect.Size + new Vector2(4, 4);
            border.Color = new Color(color.R, color.G, color.B, 0.8f);
            _transitionContainer.AddChild(border);
            
            // Semi-transparent overlay
            var zone = new ColorRect();
            zone.Position = rect.Position;
            zone.Size = rect.Size;
            zone.Color = new Color(color.R, color.G, color.B, 0.4f);
            _transitionContainer.AddChild(zone);
            
            // Label
            var labelNode = new Label();
            labelNode.Text = label;
            labelNode.Position = rect.Position + new Vector2(5, rect.Size.Y / 2 - 10);
            labelNode.AddThemeColorOverride("font_color", Colors.White);
            labelNode.AddThemeFontSizeOverride("font_size", 16);
            _transitionContainer.AddChild(labelNode);
            
            // Debug coordinates label
            var coordLabel = new Label();
            coordLabel.Text = $"({rect.Position.X},{rect.Position.Y})";
            coordLabel.Position = rect.Position + new Vector2(5, -20);
            coordLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            coordLabel.AddThemeFontSizeOverride("font_size", 10);
            _transitionContainer.AddChild(coordLabel);
        }
        
        private void RenderDebugMarkers(string mapId)
        {
            // Clear existing markers
            foreach (var child in _debugContainer.GetChildren())
            {
                child.QueueFree();
            }
            
            // Spawn points based on map
            Vector2[] spawnPoints;
            switch (mapId)
            {
                case "starting_area":
                    spawnPoints = new Vector2[]
                    {
                        new Vector2(100, 500),  // Primary
                        new Vector2(150, 500),  // Secondary
                        new Vector2(200, 500),
                        new Vector2(250, 500)
                    };
                    break;
                    
                case "forest_area":
                    spawnPoints = new Vector2[]
                    {
                        new Vector2(100, 400),  // Primary
                        new Vector2(150, 400),  // Secondary
                        new Vector2(200, 400),
                        new Vector2(250, 400)
                    };
                    break;
                    
                default:
                    return;
            }
            
            // Render spawn point markers
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                CreateSpawnMarker(spawnPoints[i], i == 0); // First one is primary
            }
        }
        
        private void CreateSpawnMarker(Vector2 position, bool isPrimary)
        {
            // Outer circle (border)
            var outerCircle = new ColorRect();
            outerCircle.Size = new Vector2(24, 24);
            outerCircle.Position = position - new Vector2(12, 12);
            outerCircle.Color = isPrimary ? Colors.Red : Colors.Orange;
            _debugContainer.AddChild(outerCircle);
            
            // Inner circle
            var innerCircle = new ColorRect();
            innerCircle.Size = new Vector2(16, 16);
            innerCircle.Position = position - new Vector2(8, 8);
            innerCircle.Color = isPrimary ? Colors.Yellow : Colors.LightYellow;
            _debugContainer.AddChild(innerCircle);
            
            // Center dot
            var centerDot = new ColorRect();
            centerDot.Size = new Vector2(4, 4);
            centerDot.Position = position - new Vector2(2, 2);
            centerDot.Color = Colors.Black;
            _debugContainer.AddChild(centerDot);
            
            // Label
            var label = new Label();
            label.Text = isPrimary ? "⭐ SPAWN" : "spawn";
            label.Position = position + new Vector2(-20, -30);
            label.AddThemeColorOverride("font_color", isPrimary ? Colors.Red : Colors.Orange);
            label.AddThemeFontSizeOverride("font_size", isPrimary ? 14 : 10);
            _debugContainer.AddChild(label);
            
            // Coordinates
            var coordLabel = new Label();
            coordLabel.Text = $"({position.X},{position.Y})";
            coordLabel.Position = position + new Vector2(-25, 15);
            coordLabel.AddThemeColorOverride("font_color", Colors.White);
            coordLabel.AddThemeFontSizeOverride("font_size", 8);
            _debugContainer.AddChild(coordLabel);
        }
        
        private void SetupNetworking()
        {
            // Client is passed from MainMenu via SetClient()
            // Will be set before _Ready() completes
            
            if (_client == null)
            {
                GD.PrintErr("[PlayScene] SpacetimeDBClient not set! Call SetClient() before adding to tree.");
                return;
            }
            
            // Subscribe to Player table
            _client.SubscribeToTable("Player");
            
            // TODO: When SpacetimeDB SDK is integrated, handle player updates
            // _client.OnPlayerUpdate += OnPlayerUpdate;
            // _client.OnPlayerInsert += OnPlayerInsert;
            // _client.OnPlayerDelete += OnPlayerDelete;
            
            GD.Print("[PlayScene] Subscribed to Player table");
        }
        
        public override void _Process(double delta)
        {
            HandleInput((float)delta);
        }
        
        private void HandleInput(float delta)
        {
            if (_client == null || !_client.IsConnected)
                return;
            
            // Get input direction
            var input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
            
            if (input != Vector2.Zero)
            {
                // Send input to server - DON'T move locally
                SendMovementInput(input, delta);
            }
        }
        
        private async void SendMovementInput(Vector2 direction, float delta)
        {
            _inputSequence++;
            
            // Calculate desired velocity (server will validate)
            float moveSpeed = 200f; // pixels per second
            var velocity = direction.Normalized() * moveSpeed;
            
            // Calculate predicted position (for display only)
            var predictedPos = _lastSentPosition + velocity * delta;
            
            // Send to server
            // TODO: Replace with actual reducer call when SDK is integrated
            // await _client.CallReducerAsync("update_player_position",
            //     _localPlayerId,
            //     predictedPos.X,
            //     predictedPos.Y,
            //     velocity.X,
            //     velocity.Y,
            //     _inputSequence
            // );
            
            // For now, just log
            if (_inputSequence % 60 == 0) // Log every 60 frames
            {
                GD.Print($"[PlayScene] Sending movement: dir={direction}, vel={velocity}, seq={_inputSequence}");
            }
        }
        
        // TODO: Implement when SpacetimeDB SDK is integrated
        private void OnPlayerUpdate(dynamic playerData)
        {
            uint playerId = playerData.id;
            float posX = playerData.position_x;
            float posY = playerData.position_y;
            string mapId = playerData.current_map_id;
            string username = playerData.username;
            float health = playerData.health;
            float maxHealth = playerData.max_health;
            
            // Only render players in current map
            if (mapId != _currentMapId)
            {
                // Remove sprite if exists
                if (_playerSprites.ContainsKey(playerId))
                {
                    _playerSprites[playerId].QueueFree();
                    _playerSprites.Remove(playerId);
                }
                return;
            }
            
            // Get or create player sprite
            Node2D playerSprite;
            if (_playerSprites.ContainsKey(playerId))
            {
                playerSprite = _playerSprites[playerId];
            }
            else
            {
                playerSprite = CreatePlayerSprite(playerId, username);
                _playerSprites[playerId] = playerSprite;
            }
            
            // Update position (server is authoritative)
            playerSprite.Position = new Vector2(posX, posY);
            _lastSentPosition = new Vector2(posX, posY);
            
            // Update health bar
            UpdatePlayerHealth(playerSprite, health, maxHealth);
            
            // Camera stays centered on map (not following player)
            // If you want camera to follow player, uncomment:
            // if (playerId == _localPlayerId)
            // {
            //     _camera.Position = playerSprite.Position;
            // }
        }
        
        private Node2D CreatePlayerSprite(uint playerId, string username)
        {
            var container = new Node2D();
            container.Name = $"Player_{playerId}";
            _mapContainer.AddChild(container);
            
            // Player circle (placeholder)
            var sprite = new ColorRect();
            sprite.Size = new Vector2(32, 32);
            sprite.Position = new Vector2(-16, -16); // Center
            sprite.Color = Colors.Blue;
            container.AddChild(sprite);
            
            // Username label
            var label = new Label();
            label.Text = username;
            label.Position = new Vector2(-20, -30);
            label.AddThemeColorOverride("font_color", Colors.White);
            label.AddThemeFontSizeOverride("font_size", 12);
            container.AddChild(label);
            
            // Health bar background
            var healthBg = new ColorRect();
            healthBg.Size = new Vector2(32, 4);
            healthBg.Position = new Vector2(-16, 20);
            healthBg.Color = Colors.DarkRed;
            container.AddChild(healthBg);
            
            // Health bar foreground
            var healthFg = new ColorRect();
            healthFg.Name = "HealthBar";
            healthFg.Size = new Vector2(32, 4);
            healthFg.Position = new Vector2(-16, 20);
            healthFg.Color = Colors.Green;
            container.AddChild(healthFg);
            
            GD.Print($"[PlayScene] Created sprite for player {playerId} ({username})");
            
            return container;
        }
        
        private void UpdatePlayerHealth(Node2D playerSprite, float health, float maxHealth)
        {
            var healthBar = playerSprite.GetNode<ColorRect>("HealthBar");
            if (healthBar != null)
            {
                float healthPercent = health / maxHealth;
                healthBar.Size = new Vector2(32 * healthPercent, 4);
            }
        }
        
        private void OnPlayerInsert(dynamic playerData)
        {
            GD.Print($"[PlayScene] Player joined: {playerData.username} (ID: {playerData.id})");
            OnPlayerUpdate(playerData);
        }
        
        private void OnPlayerDelete(dynamic playerData)
        {
            uint playerId = playerData.id;
            GD.Print($"[PlayScene] Player left: {playerData.username} (ID: {playerId})");
            
            if (_playerSprites.ContainsKey(playerId))
            {
                _playerSprites[playerId].QueueFree();
                _playerSprites.Remove(playerId);
            }
        }
        
        public void SetLocalPlayer(uint playerId)
        {
            _localPlayerId = playerId;
            GD.Print($"[PlayScene] Local player ID set to: {playerId}");
        }
        
        public void SetClient(SpacetimeDBClient client)
        {
            _client = client;
            GD.Print("[PlayScene] Client set");
            
            // Initialize networking now that we have the client
            if (IsInsideTree())
            {
                Initialize();
            }
        }
    }
}
