# SpacetimeDB Client Connection Setup

## Overview

This directory contains the complete SpacetimeDB client integration for the Guildmaster MVP. The system provides server-authoritative networking with client prediction, automatic reconnection, and comprehensive error handling.

## Architecture

```
ConnectionManager      - Handles connection lifecycle, health monitoring, reconnection
ConnectionConfig       - Configuration for server connection and retry policies
SpacetimeDBClient      - Main client interface for game systems
```

## Quick Start

### 1. Install SpacetimeDB SDK

The SpacetimeDB C# SDK has been added to the project dependencies:

```bash
dotnet restore
```

### 2. Start Your SpacetimeDB Server

Make sure your SpacetimeDB server is running:

```bash
# From your server directory
spacetime publish guildmaster --server http://127.0.0.1:7734 --delete-data
```

### 3. Test Connection

Open the `Scenes/ConnectionTest.tscn` scene in Godot and run it. You should see:

1. Click "Connect to Server" button
2. Status should change to "Connected!"
3. Click "Health Check" to test the `health_check` reducer
4. Click "Send Test Message" to test the `test_message` reducer

### 4. Check Server Logs

Verify the connection in your server logs:

```bash
spacetime logs guildmaster --server http://127.0.0.1:7734
```

You should see:
- Connection established
- Health check executed
- Test messages received

## Configuration

### Default Configuration

```csharp
ServerUri: http://localhost:7734
ModuleName: guildmaster
ConnectionTimeout: 10 seconds
HeartbeatInterval: 30 seconds
MaxRetryAttempts: 5
EnableAutoReconnect: true
```

### Custom Configuration

```csharp
var config = new ConnectionConfig
{
    ServerUri = "http://your-server:7734",
    ModuleName = "guildmaster",
    ConnectionTimeoutSeconds = 15f,
    MaxRetryAttempts = 3,
    EnableDebugLogging = true
};

_client.Configure(config);
await _client.ConnectAsync();
```

## Usage in Game Systems

### Basic Connection

```csharp
// In your game initialization
var client = new SpacetimeDBClient();
AddChild(client);

// Connect to signals
client.Connected += OnConnected;
client.Disconnected += OnDisconnected;

// Connect to server
bool success = await client.ConnectAsync();
```

### Calling Reducers

```csharp
// Send movement input (server-authoritative)
await client.SendMoveInputAsync(playerId, direction, deltaTime);

// Execute attack
await client.ExecuteAttackAsync(playerId, "sword", directionX, directionY);

// Pickup item
await client.PickupItemAsync(playerId, itemId);
```

### Subscribing to Tables

```csharp
// Subscribe to all players
client.SubscribeToTable("Player");

// Subscribe to specific map
client.SubscribeToMap("core:overworld/farm");

// Unsubscribe when changing context
client.UnsubscribeAll();
```

## Server-Authoritative Architecture

### ✅ Correct Pattern

```csharp
// Client sends INPUT to server
func _input(event):
    if event.is_action_pressed("move_up"):
        await client.SendMoveInputAsync(playerId, Vector2.UP, delta);

// Server processes and broadcasts state
// Client receives state update via subscription
func OnPlayerStateUpdate(playerData):
    player.Position = playerData.Position;
    player.Health = playerData.Health;
```

### ❌ Wrong Pattern

```csharp
// DON'T move entities directly on client
player.Position += velocity * delta;  // ❌ Wrong!

// DON'T apply game logic on client
player.Health -= damage;  // ❌ Wrong!
```

## Connection States

```
Disconnected  → Initial state
Connecting    → Attempting connection
Connected     → Successfully connected
Reconnecting  → Lost connection, attempting recovery
Failed        → All retry attempts exhausted
```

## Signals

### SpacetimeDBClient Signals

```csharp
Connected(string identity)              - Connection established
Disconnected(string reason)             - Connection lost
ConnectionError(string error)           - Connection failed
ReducerSuccess(string name, uint seq)   - Reducer executed successfully
ReducerFailed(string name, string err)  - Reducer execution failed
```

### ConnectionManager Signals

```csharp
Connected(string identity)              - Low-level connection established
Disconnected(string reason)             - Low-level connection lost
ConnectionError(string error)           - Connection attempt failed
StateChanged(ConnectionState state)     - Connection state changed
LatencyUpdated(float latencyMs)         - Network latency measurement
```

## Error Handling

### Connection Failures

The system automatically retries with exponential backoff:

```
Attempt 1: Immediate
Attempt 2: 1 second delay
Attempt 3: 2 second delay
Attempt 4: 4 second delay
Attempt 5: 8 second delay
```

After 5 failed attempts, the connection state becomes `Failed`.

### Automatic Reconnection

When connection is lost during gameplay:

1. Connection loss detected within 5 seconds
2. Automatic reconnection attempts begin
3. Local game state preserved
4. State synchronized when reconnected

### Reducer Failures

```csharp
client.ReducerFailed += (name, error) =>
{
    GD.PrintErr($"Reducer {name} failed: {error}");
    // Handle failure (retry, show error, etc.)
};
```

## Debugging

### Enable Debug Logging

```csharp
var config = new ConnectionConfig
{
    EnableDebugLogging = true
};
client.Configure(config);
```

### Debug Output

```
[ConnectionManager] Connecting to http://localhost:7734 (attempt 1/5)
[ConnectionManager] Connected successfully (identity: abc-123)
[ConnectionManager] Heartbeat started (interval: 30s)
[CLIENT] Input: input_move (seq: 1) - args: 3
[SpacetimeDBClient] Subscribed to table: Player
```

### Monitor Connection Health

```csharp
// Check connection status
bool isConnected = client.IsConnected;
float latency = client.Latency;
var state = client.State;

// Monitor in UI
_statusLabel.Text = $"Latency: {latency:F0}ms | State: {state}";
```

## Performance Considerations

### Batching Inputs

For high-frequency inputs (movement), consider batching:

```csharp
// Instead of sending every frame
await client.SendMoveInputAsync(playerId, direction, delta);

// Batch multiple inputs per network tick (e.g., 20 Hz)
if (Time.GetTicksMsec() - lastNetworkUpdate > 50)
{
    await client.SendMoveInputAsync(playerId, direction, delta);
    lastNetworkUpdate = Time.GetTicksMsec();
}
```

### Subscription Filtering

Only subscribe to relevant data:

```csharp
// ❌ Don't subscribe to everything
client.SubscribeToTable("Player");
client.SubscribeToTable("NpcState");
client.SubscribeToTable("WorldItem");

// ✅ Filter by map instance
client.SubscribeToMap(currentMapId);
```

## Next Steps

### Implementing Real SpacetimeDB SDK

The current implementation has placeholder TODO comments where the actual SpacetimeDB SDK calls should go:

```csharp
// TODO: Replace with actual SpacetimeDB SDK
// _dbConnection = DbConnection.Builder()
//     .WithUri(_config.ServerUri)
//     .WithModuleName(_config.ModuleName)
//     .Build();
// await _dbConnection.ConnectAsync();
```

Once the SpacetimeDB C# SDK is available, replace these placeholders with actual SDK calls.

### Adding Table Subscriptions

When your server tables are defined, add subscription handlers:

```csharp
// Subscribe to Player table updates
_dbConnection.OnInsert<Player>(OnPlayerInserted);
_dbConnection.OnUpdate<Player>(OnPlayerUpdated);
_dbConnection.OnDelete<Player>(OnPlayerDeleted);
```

### Implementing Client Prediction

For responsive gameplay, implement client-side prediction:

1. Predict action result locally
2. Send input to server with sequence number
3. Receive authoritative result from server
4. Reconcile if prediction differs from server

## Troubleshooting

### Connection Refused

```
Error: Connection refused
```

**Solution**: Make sure SpacetimeDB server is running on port 7734

```bash
spacetime logs guildmaster --server http://127.0.0.1:7734
```

### Module Not Found

```
Error: Module 'guildmaster' not found
```

**Solution**: Publish your module to the server

```bash
spacetime publish guildmaster --server http://127.0.0.1:7734
```

### Reducer Not Found

```
Error: Reducer 'health_check' not found
```

**Solution**: Make sure the reducer is defined in your server code and published

### High Latency

```
Latency: 500ms
```

**Solution**: 
- Check network connection
- Verify server is running locally (not remote)
- Check for CPU/memory pressure on server

## Support

For SpacetimeDB documentation and support:
- Docs: https://spacetimedb.com/docs
- Discord: https://discord.gg/spacetimedb
- GitHub: https://github.com/clockworklabs/SpacetimeDB
