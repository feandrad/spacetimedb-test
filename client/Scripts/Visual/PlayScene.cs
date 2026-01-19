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
            GD.Print("========================================");
            GD.Print("[PlayScene] ⚡ READY CALLED - SCENE IS LOADING");
            GD.Print("========================================");
            
            SetupCamera();
            SetupMap();
            SetupTransitionZones();
            
            GD.Print("[PlayScene] Scene setup complete (waiting for client)");
            GD.Print($"[PlayScene] MapContainer children: {_mapContainer?.GetChildCount() ?? 0}");
            
            // If client was already set before _Ready, initialize now
            if (_client != null)
            {
                SetupNetworking();
            }
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
            
            // Add a test marker at camera center to verify rendering
            CreateTestMarker(mapSize / 2);
            
            GD.Print($"[PlayScene] Loaded map: {mapId} ({mapSize.X}x{mapSize.Y}), camera centered at {_camera.Position}");
        }
        
        private void CreateTestMarker(Vector2 position)
        {
            // Big red square at camera center
            var testMarker = new ColorRect();
            testMarker.Name = "TestMarker_CameraCenter";
            testMarker.Size = new Vector2(100, 100);
            testMarker.Position = position - new Vector2(50, 50);
            testMarker.Color = Colors.Red;
            _mapContainer.AddChild(testMarker);
            
            var label = new Label();
            label.Text = "CAMERA CENTER";
            label.Position = position + new Vector2(-60, -120);
            label.AddThemeColorOverride("font_color", Colors.White);
            label.AddThemeFontSizeOverride("font_size", 20);
            _mapContainer.AddChild(label);
            
            GD.Print($"[PlayScene] Created test marker at camera center: {position}");
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
                GD.PrintErr("[PlayScene] ❌ SpacetimeDBClient not set! Call SetClient() before adding to tree.");
                return;
            }
            
            GD.Print($"[PlayScene] 🔧 Setting up networking...");
            GD.Print($"[PlayScene]   - Client connected: {_client.IsConnected}");
            GD.Print($"[PlayScene]   - Connection: {(_client.Connection != null ? "✓" : "✗")}");
            GD.Print($"[PlayScene]   - Db: {(_client.Connection?.Db != null ? "✓" : "✗")}");
            GD.Print($"[PlayScene]   - Player table: {(_client.Connection?.Db?.Player != null ? "✓" : "✗")}");
            
            // 1. Subscribe to FUTURE updates (new players connecting)
            _client.PlayerJoined += OnPlayerJoinedSignal;
            _client.PlayerUpdated += OnPlayerUpdatedSignal;
            _client.PlayerLeft += OnPlayerLeftSignal;
            GD.Print("[PlayScene] ✓ Subscribed to player signals");
            
            // 2. Catch up on EXISTING players (the ones who are already here)
            // This fixes the race condition where players joined before we subscribed
            if (_client.Connection?.Db?.Player != null)
            {
                GD.Print("[PlayScene] 🔍 Checking for existing players in cache...");
                
                int existingPlayerCount = 0;
                try
                {
                    foreach (var player in _client.Connection.Db.Player.Iter())
                    {
                        existingPlayerCount++;
                        GD.Print($"[PlayScene] 👤 Found existing player #{existingPlayerCount}:");
                        GD.Print($"         ID: {player.Id}");
                        GD.Print($"         Username: {player.Username}");
                        GD.Print($"         Map: {player.CurrentMapId}");
                        GD.Print($"         Position: ({player.PositionX}, {player.PositionY})");
                        GD.Print($"         Health: {player.Health}/{player.MaxHealth}");
                        
                        // Manually trigger the join logic for existing players
                        OnPlayerJoined(player.Id, player.Username, player.CurrentMapId, 
                                       player.PositionX, player.PositionY, player.Health, player.MaxHealth);
                    }
                    
                    GD.Print($"[PlayScene] ✅ Loaded {existingPlayerCount} existing player(s)");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[PlayScene] ❌ Error iterating players: {ex.Message}");
                    GD.PrintErr($"[PlayScene] Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                GD.PrintErr("[PlayScene] ⚠️ Warning: Connection.Db.Player is null - cannot load existing players");
                GD.PrintErr("[PlayScene]   This usually means the subscription hasn't been applied yet");
            }
            
            GD.Print($"[PlayScene] ✅ Networking setup complete. Total sprites: {_playerSprites.Count}");
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
        
        // ============================================================================
        // SIGNAL HANDLERS (from SpacetimeDBClient)
        // ============================================================================
        
        private void OnPlayerJoinedSignal(uint playerId)
        {
            GD.Print($"[PlayScene] Player joined signal received: ID={playerId}");
            
            // Get player data from client's local cache
            if (_client?.Connection?.Db.Player.Id.Find(playerId) is var player && player != null)
            {
                OnPlayerJoined(player.Id, player.Username, player.CurrentMapId, 
                               player.PositionX, player.PositionY, player.Health, player.MaxHealth);
            }
        }
        
        /// <summary>
        /// Core player join logic - used by both signal handler and initial load
        /// </summary>
        private void OnPlayerJoined(uint playerId, string username, string mapId, 
                                    float posX, float posY, float health, float maxHealth)
        {
            GD.Print($"[PlayScene] 🎮 Processing player join:");
            GD.Print($"         ID: {playerId}");
            GD.Print($"         Username: {username}");
            GD.Print($"         Map: {mapId}");
            GD.Print($"         Current map: {_currentMapId}");
            GD.Print($"         Position: ({posX}, {posY})");
            
            // Only render players in current map
            if (mapId == _currentMapId)
            {
                GD.Print($"[PlayScene] ✓ Player is in current map, creating sprite...");
                
                // Get player data from client's local cache
                if (_client?.Connection?.Db.Player.Id.Find(playerId) is var player && player != null)
                {
                    // Check if this is the local player by matching identity
                    if (player.Identity.ToString().ToUpper() == _client.Identity.ToUpper())
                    {
                        _localPlayerId = playerId;
                        GD.Print($"[PlayScene] ⭐ This is the LOCAL PLAYER!");
                    }
                    
                    CreatePlayerSprite(player);
                }
                else
                {
                    GD.PrintErr($"[PlayScene] ❌ Could not find player {playerId} in cache!");
                }
            }
            else
            {
                GD.Print($"[PlayScene] ⏭️ Player is in different map ({mapId}), skipping sprite creation");
            }
        }
        
        private void OnPlayerUpdatedSignal(uint playerId, Vector2 position, float health)
        {
            // Get full player data from client's local cache
            if (_client?.Connection?.Db.Player.Id.Find(playerId) is var player && player != null)
            {
                // If player changed maps, remove or add sprite
                if (player.CurrentMapId != _currentMapId)
                {
                    if (_playerSprites.ContainsKey(playerId))
                    {
                        // Player left our map
                        _playerSprites[playerId].QueueFree();
                        _playerSprites.Remove(playerId);
                    }
                    return;
                }
                
                // Update existing player sprite
                if (_playerSprites.ContainsKey(playerId))
                {
                    UpdatePlayerSprite(player);
                }
                else
                {
                    // Player is in our map but we don't have a sprite yet
                    CreatePlayerSprite(player);
                }
            }
        }
        
        private void OnPlayerLeftSignal(uint playerId)
        {
            GD.Print($"[PlayScene] Player left signal received: ID={playerId}");
            
            if (_playerSprites.ContainsKey(playerId))
            {
                _playerSprites[playerId].QueueFree();
                _playerSprites.Remove(playerId);
            }
        }
        
        private void CreatePlayerSprite(GuildmasterMVP.Network.Generated.Player player)
        {
            // Check if sprite already exists
            if (_playerSprites.ContainsKey(player.Id))
            {
                GD.Print($"[PlayScene] Sprite already exists for player {player.Id}, skipping creation");
                return;
            }
            
            GD.Print($"[PlayScene] 🎨 Creating sprite for player {player.Id}...");
            GD.Print($"[PlayScene]   - MapContainer: {_mapContainer != null}");
            GD.Print($"[PlayScene]   - MapContainer in tree: {_mapContainer?.IsInsideTree()}");
            
            var container = new Node2D();
            container.Name = $"Player_{player.Id}";
            _mapContainer.AddChild(container);
            
            GD.Print($"[PlayScene]   - Container added to MapContainer");
            
            // Determine if this is the local player
            bool isLocalPlayer = player.Identity.ToString().ToUpper() == _client.Identity.ToUpper();
            if (isLocalPlayer)
            {
                _localPlayerId = player.Id;
                GD.Print($"[PlayScene]   - ⭐ This is the LOCAL PLAYER!");
            }
            
            // Player circle (placeholder) - BIGGER and more visible
            var sprite = new ColorRect();
            sprite.Size = new Vector2(64, 64); // Doubled size
            sprite.Position = new Vector2(-32, -32); // Center
            sprite.Color = isLocalPlayer ? Colors.Cyan : Colors.Blue;
            container.AddChild(sprite);
            
            GD.Print($"[PlayScene]   - Sprite color: {(isLocalPlayer ? "Cyan (local)" : "Blue (other)")}");
            
            // Username label
            var label = new Label();
            label.Text = player.Username;
            label.Position = new Vector2(-40, -60);
            label.AddThemeColorOverride("font_color", Colors.White);
            label.AddThemeFontSizeOverride("font_size", 18);
            container.AddChild(label);
            
            // Health bar background
            var healthBg = new ColorRect();
            healthBg.Size = new Vector2(64, 6);
            healthBg.Position = new Vector2(-32, 40);
            healthBg.Color = Colors.DarkRed;
            container.AddChild(healthBg);
            
            // Health bar foreground
            var healthFg = new ColorRect();
            healthFg.Name = "HealthBar";
            healthFg.Size = new Vector2(64, 6);
            healthFg.Position = new Vector2(-32, 40);
            healthFg.Color = Colors.Green;
            container.AddChild(healthFg);
            
            // Set initial position
            container.Position = new Vector2(player.PositionX, player.PositionY);
            
            // Store reference
            _playerSprites[player.Id] = container;
            
            GD.Print($"[PlayScene] ✅ Created sprite for player {player.Id} ({player.Username}) at ({player.PositionX}, {player.PositionY})");
            GD.Print($"[PlayScene]   - Container position: {container.Position}");
            GD.Print($"[PlayScene]   - Container global position: {container.GlobalPosition}");
            GD.Print($"[PlayScene]   - Camera position: {_camera.Position}");
            GD.Print($"[PlayScene]   - Total sprites now: {_playerSprites.Count}");
            GD.Print($"[PlayScene]   - MapContainer children: {_mapContainer.GetChildCount()}");
        }
        
        private void UpdatePlayerSprite(GuildmasterMVP.Network.Generated.Player player)
        {
            if (!_playerSprites.ContainsKey(player.Id))
                return;
            
            var sprite = _playerSprites[player.Id];
            
            // Update position
            sprite.Position = new Vector2(player.PositionX, player.PositionY);
            
            // Update health bar
            var healthBar = sprite.GetNodeOrNull<ColorRect>("HealthBar");
            if (healthBar != null)
            {
                float healthPercent = player.Health / player.MaxHealth;
                healthBar.Size = new Vector2(32 * healthPercent, 4);
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
            
            // If we're already in the tree, setup networking now
            if (IsInsideTree())
            {
                SetupNetworking();
            }
            // Otherwise, _Ready() will call SetupNetworking() when the scene is added
        }
    }
}
