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
                
                // The connection is established asynchronously
                // We need to wait for the OnConnect callback
                // IMPORTANT: Must call FrameTick() to process connection messages!
                int attempts = 0;
                while (!IsConnected && attempts < 50) // 5 second timeout
                {
                    _dbConnection?.FrameTick(); // Process connection messages
                    await Task.Delay(100);
                    attempts++;
                }
                
                if (!IsConnected)
                {
                    if (_enableDebugLogging)
                        GD.PrintErr("[SpacetimeDBClient] Connection timeout - failed to connect within 5 seconds");
                    return false;
                }
                
                if (_enableDebugLogging)
                    GD.Print($"[SpacetimeDBClient] Connection established successfully");
                
                return true;
            }
            catch (Exception ex)
            {
                if (_enableDebugLogging)
                    GD.PrintErr($"[SpacetimeDBClient] Connection failed: {ex.Message}");
                    GD.PrintErr($"[SpacetimeDBClient] Stack trace: {ex.StackTrace}");
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
            
            // Unregister event handlers
            CleanupTableEventHandlers();
            
            _dbConnection?.Disconnect();
            _dbConnection = null;
            _currentIdentity = null;
        }
        
        /// <summary>
        /// Clean up table event handlers
        /// </summary>
        private void CleanupTableEventHandlers()
        {
            if (_dbConnection == null) return;
            
            _dbConnection.Db.Player.OnInsert -= OnPlayerInserted;
            _dbConnection.Db.Player.OnUpdate -= OnPlayerUpdated;
            _dbConnection.Db.Player.OnDelete -= OnPlayerDeleted;
            
            if (_enableDebugLogging)
                GD.Print("[SpacetimeDBClient] Table event handlers unregistered");
        }
        
        // ============================================================================
        // CONNECTION EVENT HANDLERS
        // ============================================================================
        
        private void OnConnected(DbConnection conn, SpacetimeDB.Identity identity, string authToken)
        {
            _currentIdentity = identity.ToString();
            
            if (_enableDebugLogging)
            {
                GD.Print($"[CLIENT] Connected successfully!");
                GD.Print($"[CLIENT] Identity: {_currentIdentity}");
                GD.Print($"[CLIENT] Connection active: {conn.IsActive}");
            }
            
            // Subscribe to all tables to receive updates
            SubscribeToAllTables();
            
            // Hook up table events - these fire when FrameTick() processes updates
            SetupTableEventHandlers();
            
            EmitSignal(SignalName.Connected, _currentIdentity);
        }
        
        /// <summary>
        /// Set up event handlers for table insert/update/delete events
        /// This is where we bridge SpacetimeDB events to Godot signals
        /// </summary>
        private void SetupTableEventHandlers()
        {
            if (_dbConnection == null) return;
            
            // Player table events
            _dbConnection.Db.Player.OnInsert += OnPlayerInserted;
            _dbConnection.Db.Player.OnUpdate += OnPlayerUpdated;
            _dbConnection.Db.Player.OnDelete += OnPlayerDeleted;
            
            if (_enableDebugLogging)
                GD.Print("[SpacetimeDBClient] Table event handlers registered");
        }
        
        // ============================================================================
        // TABLE EVENT HANDLERS
        // ============================================================================
        
        private void OnPlayerInserted(EventContext ctx, Player player)
        {
            try
            {
                if (_enableDebugLogging)
                    GD.Print($"[SpacetimeDBClient] Player inserted: ID={player.Id}, Username={player.Username}");
                
                // Emit signal that a player joined
                EmitSignal(SignalName.PlayerJoined, player.Id);
                
                // Emit initial position/health update
                var position = new Vector2(player.PositionX, player.PositionY);
                EmitSignal(SignalName.PlayerUpdated, player.Id, position, player.Health);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SpacetimeDBClient] Error in OnPlayerInserted: {ex.Message}");
                GD.PrintErr($"[SpacetimeDBClient] Stack trace: {ex.StackTrace}");
            }
        }
        
        private void OnPlayerUpdated(EventContext ctx, Player oldPlayer, Player newPlayer)
        {
            try
            {
                if (_enableDebugLogging)
                {
                    GD.Print($"[SpacetimeDBClient] Player updated: ID={newPlayer.Id}");
                    GD.Print($"  Position: ({oldPlayer.PositionX}, {oldPlayer.PositionY}) -> ({newPlayer.PositionX}, {newPlayer.PositionY})");
                    GD.Print($"  Health: {oldPlayer.Health} -> {newPlayer.Health}");
                }
                
                // Emit position/health update
                var position = new Vector2(newPlayer.PositionX, newPlayer.PositionY);
                EmitSignal(SignalName.PlayerUpdated, newPlayer.Id, position, newPlayer.Health);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SpacetimeDBClient] Error in OnPlayerUpdated: {ex.Message}");
                GD.PrintErr($"[SpacetimeDBClient] Stack trace: {ex.StackTrace}");
            }
        }
        
        private void OnPlayerDeleted(EventContext ctx, Player player)
        {
            try
            {
                if (_enableDebugLogging)
                    GD.Print($"[SpacetimeDBClient] Player deleted: ID={player.Id}, Username={player.Username}");
                
                // Emit signal that a player left
                EmitSignal(SignalName.PlayerLeft, player.Id);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SpacetimeDBClient] Error in OnPlayerDeleted: {ex.Message}");
                GD.PrintErr($"[SpacetimeDBClient] Stack trace: {ex.StackTrace}");
            }
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
            {
                GD.Print($"[CLIENT] Disconnected: {reason}");
                if (error != null)
                {
                    GD.PrintErr($"[CLIENT] Disconnect error details: {error}");
                }
            }
            
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
            if (IsConnected && _dbConnection != null)
            {
                try
                {
                    _dbConnection.FrameTick();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[SpacetimeDBClient] FrameTick error: {ex.Message}");
                    GD.PrintErr($"[SpacetimeDBClient] Stack trace: {ex.StackTrace}");
                    
                    // Don't disconnect on error, just log it
                }
            }
        }
        
        public override void _ExitTree()
        {
            Disconnect();
        }
    }
}
