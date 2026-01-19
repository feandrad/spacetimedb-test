using Godot;
using System;
using System.Threading.Tasks;
using SpacetimeDB;
using SpacetimeDB.ClientApi;
using GuildmasterMVP.Network.Generated;

namespace GuildmasterMVP.Network
{
    /// <summary>
    /// SpacetimeDB client wrapper for Godot C#
    /// Integrates SpacetimeDB SDK with Godot's architecture
    /// Implements server-authoritative networking
    /// </summary>
    public partial class SpacetimeDBClient : Node
    {
        [Signal]
        public delegate void ConnectedEventHandler(string identity);
        
        [Signal]
        public delegate void DisconnectedEventHandler(string reason);
        
        [Signal]
        public delegate void ConnectionErrorEventHandler(string error);
        
        [Signal]
        public delegate void PlayerUpdatedEventHandler(uint playerId, Vector2 position, float health);
        
        [Signal]
        public delegate void PlayerJoinedEventHandler(uint playerId);
        
        [Signal]
        public delegate void PlayerLeftEventHandler(uint playerId);
        
        [Signal]
        public delegate void ReducerSuccessEventHandler(string reducerName);
        
        [Signal]
        public delegate void ReducerFailedEventHandler(string reducerName, string error);

        private DbConnection? _dbConnection;
        private string? _currentIdentity;
        private bool _enableDebugLogging = true;
        private string _serverUri = "http://localhost:7734";
        private string _moduleName = "guildmaster";
        
        public new bool IsConnected => _dbConnection?.IsActive ?? false;
        public string? Identity => _currentIdentity;
        public DbConnection? Connection => _dbConnection;
        
        public override void _Ready()
        {
            if (_enableDebugLogging)
                GD.Print("[SpacetimeDBClient] Initialized");
        }
        
        /// <summary>
        /// Configure the client with custom settings
        /// </summary>
        public void Configure(string serverUri, string moduleName, bool enableDebugLogging = true)
        {
            _serverUri = serverUri;
            _moduleName = moduleName;
            _enableDebugLogging = enableDebugLogging;
            
            if (_enableDebugLogging)
                GD.Print($"[SpacetimeDBClient] Configured: {_serverUri}/{_moduleName}");
        }
        
        /// <summary>
        /// Connect to the SpacetimeDB server
        /// </summary>
        public async Task<bool> ConnectAsync(string? serverUrl = null)
        {
            if (serverUrl != null)
            {
                _serverUri = serverUrl;
            }
            
            if (_enableDebugLogging)
                GD.Print($"[SpacetimeDBClient] Connecting to {_serverUri}...");
            
            try
            {
                // Create connection using builder pattern
                _dbConnection = DbConnection.Builder()
                    .WithUri(_serverUri)
                    .WithModuleName(_moduleName)
                    .OnConnect(OnConnected)
                    .OnConnectError(OnConnectError)
                    .OnDisconnect(OnDisconnected)
                    .Build();
                
                // Wait a moment for connection to establish
                await Task.Delay(500);
                
                return IsConnected;
            }
            catch (Exception ex)
            {
                if (_enableDebugLogging)
                    GD.PrintErr($"[SpacetimeDBClient] Connection failed: {ex.Message}");
                EmitSignal(SignalName.ConnectionError, ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Disconnect from the SpacetimeDB server
        /// </summary>
        public void Disconnect()
        {
            if (_enableDebugLogging)
                GD.Print("[SpacetimeDBClient] Disconnecting...");
            
            _dbConnection?.Disconnect();
            _dbConnection = null;
            _currentIdentity = null;
        }
        
        // ============================================================================
        // CONNECTION EVENT HANDLERS
        // ============================================================================
        
        private void OnConnected(DbConnection conn, SpacetimeDB.Identity identity, string authToken)
        {
            _currentIdentity = identity.ToString();
            
            if (_enableDebugLogging)
                GD.Print($"[CLIENT] Connected (identity: {_currentIdentity})");
            
            EmitSignal(SignalName.Connected, _currentIdentity);
        }
        
        private void OnConnectError(Exception error)
        {
            if (_enableDebugLogging)
                GD.PrintErr($"[CLIENT] Connection error: {error.Message}");
            
            EmitSignal(SignalName.ConnectionError, error.Message);
        }
        
        private void OnDisconnected(DbConnection conn, Exception? error)
        {
            string reason = error?.Message ?? "Normal disconnect";
            
            if (_enableDebugLogging)
                GD.Print($"[CLIENT] Disconnected: {reason}");
            
            EmitSignal(SignalName.Disconnected, reason);
        }
        
        // ============================================================================
        // GAME-SPECIFIC REDUCER CALLS
        // ============================================================================
        
        /// <summary>
        /// Register a new player on the server with username
        /// </summary>
        public async Task<bool> RegisterPlayerAsync(string username)
        {
            if (!IsConnected || _dbConnection == null)
            {
                string error = "Cannot register player: not connected to server";
                if (_enableDebugLogging)
                    GD.PrintErr($"[SpacetimeDBClient] {error}");
                EmitSignal(SignalName.ReducerFailed, "register_player", error);
                return false;
            }
            
            try
            {
                if (_enableDebugLogging)
                    GD.Print($"[CLIENT] Calling register_player: {username}");
                
                // Set up callback for success/failure
                bool success = false;
                string? errorMessage = null;
                
                void OnRegisterPlayer(ReducerEventContext ctx, string name)
                {
                    if (ctx.Event.Status is Status.Committed)
                    {
                        success = true;
                        if (_enableDebugLogging)
                            GD.Print($"[CLIENT] Player registered: {name}");
                        EmitSignal(SignalName.ReducerSuccess, "register_player");
                    }
                    else if (ctx.Event.Status is Status.Failed(var reason))
                    {
                        errorMessage = reason;
                        if (_enableDebugLogging)
                            GD.PrintErr($"[CLIENT] register_player failed: {reason}");
                        EmitSignal(SignalName.ReducerFailed, "register_player", reason);
                    }
                }
                
                // Register callback
                _dbConnection.Reducers.OnRegisterPlayer += OnRegisterPlayer;
                
                // Call the reducer
                _dbConnection.Reducers.RegisterPlayer(username);
                
                // Wait for response (with timeout)
                int attempts = 0;
                while (!success && errorMessage == null && attempts < 50) // 5 second timeout
                {
                    await Task.Delay(100);
                    attempts++;
                }
                
                // Unregister callback
                _dbConnection.Reducers.OnRegisterPlayer -= OnRegisterPlayer;
                
                if (errorMessage != null)
                {
                    return false;
                }
                
                return success;
            }
            catch (Exception ex)
            {
                string error = $"Reducer call failed: {ex.Message}";
                if (_enableDebugLogging)
                    GD.PrintErr($"[SpacetimeDBClient] register_player: {error}");
                EmitSignal(SignalName.ReducerFailed, "register_player", error);
                return false;
            }
        }
        
        /// <summary>
        /// Subscribe to table updates
        /// </summary>
        public void SubscribeToTable(string tableName)
        {
            if (!IsConnected || _dbConnection == null)
            {
                if (_enableDebugLogging)
                    GD.PrintErr("[SpacetimeDBClient] Cannot subscribe: not connected to server");
                return;
            }
            
            _dbConnection.SubscriptionBuilder()
                .OnApplied(ctx =>
                {
                    if (_enableDebugLogging)
                        GD.Print($"[SpacetimeDBClient] Subscription applied: {tableName}");
                })
                .Subscribe(new[] { $"SELECT * FROM {tableName}" });
            
            if (_enableDebugLogging)
                GD.Print($"[SpacetimeDBClient] Subscribed to table: {tableName}");
        }
        
        /// <summary>
        /// Subscribe to all tables
        /// </summary>
        public void SubscribeToAllTables()
        {
            if (!IsConnected || _dbConnection == null)
            {
                if (_enableDebugLogging)
                    GD.PrintErr("[SpacetimeDBClient] Cannot subscribe: not connected to server");
                return;
            }
            
            _dbConnection.SubscriptionBuilder()
                .OnApplied(ctx =>
                {
                    if (_enableDebugLogging)
                        GD.Print("[SpacetimeDBClient] Subscription to all tables applied");
                })
                .SubscribeToAllTables();
            
            if (_enableDebugLogging)
                GD.Print("[SpacetimeDBClient] Subscribed to all tables");
        }
        
        /// <summary>
        /// Process updates from the server
        /// Call this regularly (e.g. in _Process)
        /// </summary>
        public void FrameTick()
        {
            _dbConnection?.FrameTick();
        }
        
        public override void _Process(double delta)
        {
            // Process SpacetimeDB updates every frame
            if (IsConnected)
            {
                FrameTick();
            }
        }
        
        public override void _ExitTree()
        {
            Disconnect();
        }
    }
}
