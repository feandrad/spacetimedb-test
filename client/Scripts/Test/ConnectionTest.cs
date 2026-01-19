using Godot;
using System;
using System.Threading.Tasks;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Simple test script to verify SpacetimeDB connection
    /// Attach to a Node in a test scene to run connection tests
    /// </summary>
    public partial class ConnectionTest : Node
    {
        private SpacetimeDBClient _client;
        private Label _statusLabel;
        private Button _connectButton;
        private Button _healthCheckButton;
        private Button _testMessageButton;
        private Button _disconnectButton;
        
        public override void _Ready()
        {
            GD.Print("[ConnectionTest] Initializing connection test...");
            
            // Create UI
            CreateUI();
            
            // Create SpacetimeDB client
            _client = new SpacetimeDBClient();
            AddChild(_client);
            
            // Connect to signals
            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
            _client.ConnectionError += OnConnectionError;
            _client.ReducerSuccess += OnReducerSuccess;
            _client.ReducerFailed += OnReducerFailed;
            
            UpdateStatus("Ready to connect");
            GD.Print("[ConnectionTest] Ready");
        }
        
        private void CreateUI()
        {
            // Create vertical container
            var vbox = new VBoxContainer();
            vbox.Position = new Vector2(20, 20);
            AddChild(vbox);
            
            // Status label
            _statusLabel = new Label();
            _statusLabel.Text = "Status: Initializing...";
            _statusLabel.AddThemeColorOverride("font_color", Colors.White);
            vbox.AddChild(_statusLabel);
            
            // Add spacing
            var spacer1 = new Control();
            spacer1.CustomMinimumSize = new Vector2(0, 20);
            vbox.AddChild(spacer1);
            
            // Connect button
            _connectButton = new Button();
            _connectButton.Text = "Connect to Server";
            _connectButton.Pressed += OnConnectPressed;
            vbox.AddChild(_connectButton);
            
            // Health check button
            _healthCheckButton = new Button();
            _healthCheckButton.Text = "Health Check";
            _healthCheckButton.Disabled = true;
            _healthCheckButton.Pressed += OnHealthCheckPressed;
            vbox.AddChild(_healthCheckButton);
            
            // Test message button
            _testMessageButton = new Button();
            _testMessageButton.Text = "Send Test Message";
            _testMessageButton.Disabled = true;
            _testMessageButton.Pressed += OnTestMessagePressed;
            vbox.AddChild(_testMessageButton);
            
            // Disconnect button
            _disconnectButton = new Button();
            _disconnectButton.Text = "Disconnect";
            _disconnectButton.Disabled = true;
            _disconnectButton.Pressed += OnDisconnectPressed;
            vbox.AddChild(_disconnectButton);
        }
        
        private void UpdateStatus(string status)
        {
            _statusLabel.Text = $"Status: {status}";
            GD.Print($"[ConnectionTest] {status}");
        }
        
        private async void OnConnectPressed()
        {
            _connectButton.Disabled = true;
            UpdateStatus("Connecting...");
            
            bool success = await _client.ConnectAsync();
            
            if (!success)
            {
                UpdateStatus("Connection failed");
                _connectButton.Disabled = false;
            }
        }
        
        private async void OnHealthCheckPressed()
        {
            UpdateStatus("Sending health check...");
            bool success = await _client.HealthCheckAsync();
            
            if (success)
            {
                UpdateStatus("Health check successful");
            }
            else
            {
                UpdateStatus("Health check failed");
            }
        }
        
        private async void OnTestMessagePressed()
        {
            UpdateStatus("Sending test message...");
            bool success = await _client.TestMessageAsync("Hello from Godot client!");
            
            if (success)
            {
                UpdateStatus("Test message sent");
            }
            else
            {
                UpdateStatus("Test message failed");
            }
        }
        
        private void OnDisconnectPressed()
        {
            _client.Disconnect();
        }
        
        private void OnConnected(string identity)
        {
            UpdateStatus($"Connected! Identity: {identity}");
            _connectButton.Disabled = true;
            _healthCheckButton.Disabled = false;
            _testMessageButton.Disabled = false;
            _disconnectButton.Disabled = false;
        }
        
        private void OnDisconnected(string reason)
        {
            UpdateStatus($"Disconnected: {reason}");
            _connectButton.Disabled = false;
            _healthCheckButton.Disabled = true;
            _testMessageButton.Disabled = true;
            _disconnectButton.Disabled = true;
        }
        
        private void OnConnectionError(string error)
        {
            UpdateStatus($"Error: {error}");
            _connectButton.Disabled = false;
        }
        
        private void OnReducerSuccess(string reducerName, uint sequence)
        {
            GD.Print($"[ConnectionTest] Reducer success: {reducerName} (seq: {sequence})");
        }
        
        private void OnReducerFailed(string reducerName, string error)
        {
            GD.PrintErr($"[ConnectionTest] Reducer failed: {reducerName} - {error}");
        }
        
        public override void _ExitTree()
        {
            if (_client != null)
            {
                _client.Connected -= OnConnected;
                _client.Disconnected -= OnDisconnected;
                _client.ConnectionError -= OnConnectionError;
                _client.ReducerSuccess -= OnReducerSuccess;
                _client.ReducerFailed -= OnReducerFailed;
            }
        }
    }
}
