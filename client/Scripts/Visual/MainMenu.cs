using Godot;
using System;
using System.Threading.Tasks;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Visual
{
    /// <summary>
    /// Main menu with server connection and character color selection
    /// Handles initial connection flow and player spawn
    /// </summary>
    public partial class MainMenu : Control
    {
        // UI References
        private Label _titleLabel;
        private Label _statusLabel;
        private Button _connectButton;
        private Button _startGameButton;
        private ColorPickerButton _colorPicker;
        private LineEdit _playerNameInput;
        private Panel _connectionPanel;
        private Panel _characterPanel;
        
        // Network
        private SpacetimeDBClient _client;
        private bool _isConnected = false;
        private Color _selectedColor = Colors.Blue;
        private string _playerName = "Player";
        private uint _localPlayerId = 0;
        
        public override void _Ready()
        {
            CreateUI();
            SetupNetworking();
        }
        
        private void CreateUI()
        {
            // Main container
            var vbox = new VBoxContainer();
            vbox.AnchorRight = 1;
            vbox.AnchorBottom = 1;
            vbox.AddThemeConstantOverride("separation", 20);
            AddChild(vbox);
            
            // Spacer top
            var spacerTop = new Control();
            spacerTop.CustomMinimumSize = new Vector2(0, 100);
            vbox.AddChild(spacerTop);
            
            // Title
            _titleLabel = new Label();
            _titleLabel.Text = "GUILDMASTER";
            _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _titleLabel.AddThemeColorOverride("font_color", Colors.White);
            _titleLabel.AddThemeFontSizeOverride("font_size", 48);
            vbox.AddChild(_titleLabel);
            
            // Status label
            _statusLabel = new Label();
            _statusLabel.Text = "Ready to connect";
            _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _statusLabel.AddThemeColorOverride("font_color", Colors.Gray);
            vbox.AddChild(_statusLabel);
            
            // Spacer
            var spacer1 = new Control();
            spacer1.CustomMinimumSize = new Vector2(0, 40);
            vbox.AddChild(spacer1);
            
            // Connection Panel
            _connectionPanel = CreateConnectionPanel();
            vbox.AddChild(_connectionPanel);
            
            // Character Panel (hidden initially)
            _characterPanel = CreateCharacterPanel();
            _characterPanel.Visible = false;
            vbox.AddChild(_characterPanel);
        }
        
        private Panel CreateConnectionPanel()
        {
            var panel = new Panel();
            panel.CustomMinimumSize = new Vector2(400, 150);
            panel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            
            var vbox = new VBoxContainer();
            vbox.AnchorRight = 1;
            vbox.AnchorBottom = 1;
            vbox.AddThemeConstantOverride("separation", 15);
            panel.AddChild(vbox);
            
            // Margin
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 20);
            margin.AddThemeConstantOverride("margin_right", 20);
            margin.AddThemeConstantOverride("margin_top", 20);
            margin.AddThemeConstantOverride("margin_bottom", 20);
            vbox.AddChild(margin);
            
            var innerVbox = new VBoxContainer();
            innerVbox.AddThemeConstantOverride("separation", 10);
            margin.AddChild(innerVbox);
            
            // Connection info
            var infoLabel = new Label();
            infoLabel.Text = "Server: localhost:7734";
            infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
            innerVbox.AddChild(infoLabel);
            
            // Connect button
            _connectButton = new Button();
            _connectButton.Text = "Connect to Server";
            _connectButton.CustomMinimumSize = new Vector2(200, 50);
            _connectButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            _connectButton.Pressed += OnConnectPressed;
            innerVbox.AddChild(_connectButton);
            
            return panel;
        }
        
        private Panel CreateCharacterPanel()
        {
            var panel = new Panel();
            panel.CustomMinimumSize = new Vector2(400, 250);
            panel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            
            var vbox = new VBoxContainer();
            vbox.AnchorRight = 1;
            vbox.AnchorBottom = 1;
            vbox.AddThemeConstantOverride("separation", 15);
            panel.AddChild(vbox);
            
            // Margin
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 20);
            margin.AddThemeConstantOverride("margin_right", 20);
            margin.AddThemeConstantOverride("margin_top", 20);
            margin.AddThemeConstantOverride("margin_bottom", 20);
            vbox.AddChild(margin);
            
            var innerVbox = new VBoxContainer();
            innerVbox.AddThemeConstantOverride("separation", 15);
            margin.AddChild(innerVbox);
            
            // Title
            var titleLabel = new Label();
            titleLabel.Text = "Create Your Character";
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.AddThemeFontSizeOverride("font_size", 24);
            innerVbox.AddChild(titleLabel);
            
            // Name input
            var nameHbox = new HBoxContainer();
            innerVbox.AddChild(nameHbox);
            
            var nameLabel = new Label();
            nameLabel.Text = "Name:";
            nameLabel.CustomMinimumSize = new Vector2(80, 0);
            nameHbox.AddChild(nameLabel);
            
            _playerNameInput = new LineEdit();
            _playerNameInput.Text = "Player";
            _playerNameInput.PlaceholderText = "Enter your name";
            _playerNameInput.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _playerNameInput.TextChanged += OnNameChanged;
            nameHbox.AddChild(_playerNameInput);
            
            // Color picker
            var colorHbox = new HBoxContainer();
            innerVbox.AddChild(colorHbox);
            
            var colorLabel = new Label();
            colorLabel.Text = "Color:";
            colorLabel.CustomMinimumSize = new Vector2(80, 0);
            colorHbox.AddChild(colorLabel);
            
            _colorPicker = new ColorPickerButton();
            _colorPicker.Color = Colors.Blue;
            _colorPicker.CustomMinimumSize = new Vector2(100, 40);
            _colorPicker.ColorChanged += OnColorChanged;
            colorHbox.AddChild(_colorPicker);
            
            var colorPreview = new ColorRect();
            colorPreview.Color = Colors.Blue;
            colorPreview.CustomMinimumSize = new Vector2(100, 40);
            colorPreview.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            colorHbox.AddChild(colorPreview);
            
            // Store reference for color preview update
            _colorPicker.ColorChanged += (color) => colorPreview.Color = color;
            
            // Note: Color is currently client-side only (not stored on server)
            var colorNote = new Label();
            colorNote.Text = "(Color selection coming soon)";
            colorNote.HorizontalAlignment = HorizontalAlignment.Center;
            colorNote.AddThemeColorOverride("font_color", Colors.Gray);
            colorNote.AddThemeFontSizeOverride("font_size", 10);
            innerVbox.AddChild(colorNote);
            
            // Start game button
            _startGameButton = new Button();
            _startGameButton.Text = "Start Game";
            _startGameButton.CustomMinimumSize = new Vector2(200, 50);
            _startGameButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            _startGameButton.Pressed += OnStartGamePressed;
            innerVbox.AddChild(_startGameButton);
            
            return panel;
        }
        
        private void SetupNetworking()
        {
            _client = new SpacetimeDBClient();
            AddChild(_client);
            
            // Connect to signals
            _client.Connected += OnServerConnected;
            _client.Disconnected += OnServerDisconnected;
            _client.ConnectionError += OnConnectionError;
        }
        
        private async void OnConnectPressed()
        {
            _connectButton.Disabled = true;
            _statusLabel.Text = "Connecting to server...";
            _statusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            
            bool success = await _client.ConnectAsync();
            
            if (!success)
            {
                _connectButton.Disabled = false;
                _statusLabel.Text = "Connection failed. Check if server is running.";
                _statusLabel.AddThemeColorOverride("font_color", Colors.Red);
            }
        }
        
        private async void OnServerConnected(string identity)
        {
            _isConnected = true;
            _statusLabel.Text = $"Connected! Checking for existing player...";
            _statusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            
            GD.Print($"[MainMenu] Connected to server (identity: {identity})");
            
            // Wait for subscription to sync (important!)
            await System.Threading.Tasks.Task.Delay(1000);
            
            // Check if player already exists for this identity
            var existingPlayer = CheckForExistingPlayer();
            
            if (existingPlayer != null)
            {
                // Player exists - go straight to game
                GD.Print($"[MainMenu] ✅ EXISTING PLAYER FOUND - LOGGING IN");
                GD.Print($"[MainMenu]   - ID: {existingPlayer.Id}");
                GD.Print($"[MainMenu]   - Username: {existingPlayer.Username}");
                GD.Print($"[MainMenu]   - Map: {existingPlayer.CurrentMapId}");
                GD.Print($"[MainMenu]   - Position: ({existingPlayer.PositionX}, {existingPlayer.PositionY})");
                GD.Print($"[MainMenu]   - Health: {existingPlayer.Health}/{existingPlayer.MaxHealth}");
                
                _statusLabel.Text = $"Welcome back, {existingPlayer.Username}!";
                _statusLabel.AddThemeColorOverride("font_color", Colors.Green);
                
                // Wait a moment to show the message
                await System.Threading.Tasks.Task.Delay(1000);
                
                // Go straight to game - player data is already in the database
                TransitionToGame();
            }
            else
            {
                // No existing player - show character creation
                GD.Print($"[MainMenu] 🆕 No existing player found - showing character creation");
                
                _statusLabel.Text = $"Connected! Identity: {identity.Substring(0, 8)}...";
                _statusLabel.AddThemeColorOverride("font_color", Colors.Green);
                
                _connectionPanel.Visible = false;
                _characterPanel.Visible = true;
            }
        }
        
        private void OnServerDisconnected(string reason)
        {
            _isConnected = false;
            _statusLabel.Text = $"Disconnected: {reason}";
            _statusLabel.AddThemeColorOverride("font_color", Colors.Red);
            
            _connectionPanel.Visible = true;
            _characterPanel.Visible = false;
            _connectButton.Disabled = false;
            
            GD.PrintErr($"[MainMenu] Disconnected from server: {reason}");
        }
        
        private void OnConnectionError(string error)
        {
            _statusLabel.Text = $"Error: {error}";
            _statusLabel.AddThemeColorOverride("font_color", Colors.Red);
            _connectButton.Disabled = false;
            
            GD.PrintErr($"[MainMenu] Connection error: {error}");
        }
        
        private void OnNameChanged(string newName)
        {
            _playerName = newName;
        }
        
        private void OnColorChanged(Color color)
        {
            _selectedColor = color;
        }
        
        private async void OnStartGamePressed()
        {
            if (!_isConnected)
            {
                _statusLabel.Text = "Not connected to server!";
                _statusLabel.AddThemeColorOverride("font_color", Colors.Red);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(_playerName))
            {
                _statusLabel.Text = "Please enter a name!";
                _statusLabel.AddThemeColorOverride("font_color", Colors.Red);
                return;
            }
            
            _startGameButton.Disabled = true;
            _statusLabel.Text = "Creating character...";
            _statusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            
            // Create player on server
            bool success = await CreatePlayerOnServer();
            
            if (success)
            {
                _statusLabel.Text = "Character created! Loading world...";
                
                // Wait a moment for the subscription to sync
                await Task.Delay(1000);
                
                // Transition to main game
                TransitionToGame();
            }
            else
            {
                _statusLabel.Text = "Failed to create character";
                _statusLabel.AddThemeColorOverride("font_color", Colors.Red);
                _startGameButton.Disabled = false;
            }
        }
        
        /// <summary>
        /// Check if a player already exists for the current identity
        /// </summary>
        private GuildmasterMVP.Network.Generated.Player? CheckForExistingPlayer()
        {
            if (_client?.Connection?.Db?.Player == null)
            {
                GD.PrintErr("[MainMenu] Cannot check for existing player - connection not ready");
                return null;
            }
            
            try
            {
                // Iterate through all players and find one matching our identity
                foreach (var player in _client.Connection.Db.Player.Iter())
                {
                    if (player.Identity.ToString().ToUpper() == _client.Identity.ToUpper())
                    {
                        return player;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[MainMenu] Error checking for existing player: {ex.Message}");
                return null;
            }
        }
        
        private async Task<bool> CreatePlayerOnServer()
        {
            GD.Print($"[MainMenu] Registering new player: {_playerName}");
            
            try
            {
                // Call the register_player reducer with username
                // Server will check if player already exists and handle it
                bool success = await _client.RegisterPlayerAsync(_playerName);
                
                if (success)
                {
                    GD.Print("[MainMenu] ✅ Player registration request successful");
                }
                else
                {
                    GD.PrintErr("[MainMenu] ❌ Failed to register player on server");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[MainMenu] Error registering player: {ex.Message}");
                GD.PrintErr($"[MainMenu] Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        private void TransitionToGame()
        {
            GD.Print("[MainMenu] Transitioning to main game...");
            GD.Print($"[MainMenu] Client connected: {_client.IsConnected}");
            GD.Print($"[MainMenu] Client identity: {_client.Identity}");
            
            // Load play scene
            var playScene = GD.Load<PackedScene>("res://Scenes/PlayScene.tscn");
            var playInstance = playScene.Instantiate<PlayScene>();
            
            // Pass client reference BEFORE adding to tree
            playInstance.SetClient(_client);
            
            GD.Print("[MainMenu] Client passed to PlayScene");
            
            // Add to scene tree
            GetTree().Root.AddChild(playInstance);
            
            // Hide menu
            Visible = false;
            
            GD.Print("[MainMenu] Play scene loaded - PlayScene will find and render all players");
        }
        
        public override void _ExitTree()
        {
            if (_client != null)
            {
                _client.Connected -= OnServerConnected;
                _client.Disconnected -= OnServerDisconnected;
                _client.ConnectionError -= OnConnectionError;
            }
        }
    }
}
