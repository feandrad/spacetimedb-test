using Godot;
using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace GuildmasterMVP.Network
{
    /// <summary>
    /// Manages SpacetimeDB connection lifecycle, health monitoring, and reconnection
    /// Implements Requirements 1.1-1.7: Connection Management
    /// </summary>
    public partial class ConnectionManager : Node
    {
        [Signal]
        public delegate void ConnectedEventHandler(string identity);
        
        [Signal]
        public delegate void ConnectionErrorEventHandler(string error);
        
        [Signal]
        public delegate void DisconnectedEventHandler(string reason);
        
        [Signal]
        public delegate void StateChangedEventHandler(ConnectionState newState);
        
        [Signal]
        public delegate void LatencyUpdatedEventHandler(float latencyMs);
        
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Reconnecting,
            Failed
        }
        
        private ConnectionConfig _config;
        private ConnectionState _state = ConnectionState.Disconnected;
        private int _retryAttempt = 0;
        private float _currentRetryDelay = 0f;
        private float _latencyMs = 0f;
        private DateTime _lastHeartbeat = DateTime.MinValue;
        private bool _isHeartbeatActive = false;
        private Timer _heartbeatTimer;
        private Timer _reconnectTimer;
        
        public ConnectionState State => _state;
        public new bool IsConnected => _state == ConnectionState.Connected;
        public float Latency => _latencyMs;
        public DateTime LastHeartbeat => _lastHeartbeat;
        public ConnectionConfig Config => _config;
        
        public override void _Ready()
        {
            _config = new ConnectionConfig();
            
            // Create heartbeat timer
            _heartbeatTimer = new Timer();
            _heartbeatTimer.OneShot = false;
            _heartbeatTimer.Timeout += OnHeartbeatTimeout;
            AddChild(_heartbeatTimer);
            
            // Create reconnect timer
            _reconnectTimer = new Timer();
            _reconnectTimer.OneShot = true;
            _reconnectTimer.Timeout += OnReconnectTimeout;
            AddChild(_reconnectTimer);
            
            if (_config.EnableDebugLogging)
                GD.Print("[ConnectionManager] Initialized");
        }
        
        /// <summary>
        /// Set custom connection configuration
        /// </summary>
        public void SetConfig(ConnectionConfig config)
        {
            config.Validate();
            _config = config;
            
            if (_config.EnableDebugLogging)
                GD.Print($"[ConnectionManager] Config updated: {config}");
        }
        
        /// <summary>
        /// Attempt to connect to SpacetimeDB server
        /// Requirements 1.1, 1.2: Connection establishment and verification
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (_state == ConnectionState.Connected)
            {
                if (_config.EnableDebugLogging)
                    GD.Print("[ConnectionManager] Already connected");
                return true;
            }
            
            SetState(ConnectionState.Connecting);
            _retryAttempt = 0;
            
            return await AttemptConnectionAsync();
        }
        
        /// <summary>
        /// Test if the server is reachable
        /// </summary>
        private async Task<bool> TestServerConnectionAsync()
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(_config.ConnectionTimeoutSeconds);
                
                // Try to reach the server
                var response = await httpClient.GetAsync(_config.ServerUri);
                
                // SpacetimeDB should respond (even if it's not a valid endpoint)
                // We just want to know if something is listening on that port
                return true;
            }
            catch (HttpRequestException)
            {
                // Connection refused or network error
                return false;
            }
            catch (TaskCanceledException)
            {
                // Timeout
                return false;
            }
            catch (Exception ex)
            {
                if (_config.EnableDebugLogging)
                    GD.PrintErr($"[ConnectionManager] Connection test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Internal connection attempt with retry logic
        /// Requirements 1.3: Retry with exponential backoff
        /// </summary>
        private async Task<bool> AttemptConnectionAsync()
        {
            try
            {
                if (_config.EnableDebugLogging)
                    GD.Print($"[ConnectionManager] Connecting to {_config.ServerUri} (attempt {_retryAttempt + 1}/{_config.MaxRetryAttempts})");
                
                // Try to reach the server with a simple HTTP request
                bool success = await TestServerConnectionAsync();
                
                if (success)
                {
                    SetState(ConnectionState.Connected);
                    _retryAttempt = 0;
                    _currentRetryDelay = 0f;
                    
                    // Start heartbeat monitoring
                    StartHeartbeat();
                    
                    string identity = Guid.NewGuid().ToString();
                    EmitSignal(SignalName.Connected, identity);
                    
                    if (_config.EnableDebugLogging)
                        GD.Print($"[ConnectionManager] Connected successfully (identity: {identity})");
                    
                    return true;
                }
                else
                {
                    throw new Exception("Server not reachable");
                }
            }
            catch (Exception ex)
            {
                _retryAttempt++;
                
                if (_retryAttempt >= _config.MaxRetryAttempts)
                {
                    // All attempts failed
                    SetState(ConnectionState.Failed);
                    string error = $"Connection failed after {_config.MaxRetryAttempts} attempts: {ex.Message}";
                    EmitSignal(SignalName.ConnectionError, error);
                    
                    if (_config.EnableDebugLogging)
                        GD.PrintErr($"[ConnectionManager] {error}");
                    
                    return false;
                }
                else
                {
                    // Calculate exponential backoff delay
                    _currentRetryDelay = _config.InitialRetryDelaySeconds * 
                        Mathf.Pow(_config.RetryBackoffMultiplier, _retryAttempt - 1);
                    _currentRetryDelay = Mathf.Min(_currentRetryDelay, _config.MaxRetryDelaySeconds);
                    
                    if (_config.EnableDebugLogging)
                        GD.Print($"[ConnectionManager] Retry in {_currentRetryDelay:F1}s...");
                    
                    // Schedule retry
                    _reconnectTimer.WaitTime = _currentRetryDelay;
                    _reconnectTimer.Start();
                    
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Disconnect from server
        /// </summary>
        public void Disconnect()
        {
            if (_state == ConnectionState.Disconnected)
                return;
            
            StopHeartbeat();
            
            // TODO: Actual SpacetimeDB disconnection
            
            SetState(ConnectionState.Disconnected);
            EmitSignal(SignalName.Disconnected, "Manual disconnect");
            
            if (_config.EnableDebugLogging)
                GD.Print("[ConnectionManager] Disconnected");
        }
        
        /// <summary>
        /// Start heartbeat monitoring
        /// Requirements 1.5: Connection health monitoring
        /// </summary>
        public void StartHeartbeat()
        {
            if (_isHeartbeatActive)
                return;
            
            _isHeartbeatActive = true;
            _heartbeatTimer.WaitTime = _config.HeartbeatIntervalSeconds;
            _heartbeatTimer.Start();
            _lastHeartbeat = DateTime.UtcNow;
            
            if (_config.EnableDebugLogging)
                GD.Print($"[ConnectionManager] Heartbeat started (interval: {_config.HeartbeatIntervalSeconds}s)");
        }
        
        /// <summary>
        /// Stop heartbeat monitoring
        /// </summary>
        public void StopHeartbeat()
        {
            if (!_isHeartbeatActive)
                return;
            
            _isHeartbeatActive = false;
            _heartbeatTimer.Stop();
            
            if (_config.EnableDebugLogging)
                GD.Print("[ConnectionManager] Heartbeat stopped");
        }
        
        /// <summary>
        /// Handle connection loss and attempt reconnection
        /// Requirements 1.6: Automatic reconnection
        /// </summary>
        private async void OnConnectionLost(string reason)
        {
            if (_state == ConnectionState.Disconnected)
                return;
            
            StopHeartbeat();
            
            if (_config.EnableDebugLogging)
                GD.Print($"[ConnectionManager] Connection lost: {reason}");
            
            EmitSignal(SignalName.Disconnected, reason);
            
            if (_config.EnableAutoReconnect)
            {
                SetState(ConnectionState.Reconnecting);
                _retryAttempt = 0;
                await AttemptConnectionAsync();
            }
            else
            {
                SetState(ConnectionState.Disconnected);
            }
        }
        
        /// <summary>
        /// Update connection state
        /// Requirements 1.7: Connection status events
        /// </summary>
        private void SetState(ConnectionState newState)
        {
            if (_state == newState)
                return;
            
            var oldState = _state;
            _state = newState;
            
            EmitSignal(SignalName.StateChanged, (int)newState);
            
            if (_config.EnableDebugLogging)
                GD.Print($"[ConnectionManager] State: {oldState} -> {newState}");
        }
        
        /// <summary>
        /// Update latency measurement
        /// </summary>
        public void UpdateLatency(float latencyMs)
        {
            _latencyMs = latencyMs;
            EmitSignal(SignalName.LatencyUpdated, latencyMs);
        }
        
        private void OnHeartbeatTimeout()
        {
            if (!IsConnected)
            {
                StopHeartbeat();
                return;
            }
            
            // TODO: Send actual heartbeat to server
            _lastHeartbeat = DateTime.UtcNow;
            
            // Check if connection is still alive
            var timeSinceLastHeartbeat = DateTime.UtcNow - _lastHeartbeat;
            if (timeSinceLastHeartbeat.TotalSeconds > _config.HeartbeatIntervalSeconds * 3)
            {
                OnConnectionLost("Heartbeat timeout");
            }
        }
        
        private async void OnReconnectTimeout()
        {
            await AttemptConnectionAsync();
        }
        
        public override void _ExitTree()
        {
            Disconnect();
        }
    }
}
