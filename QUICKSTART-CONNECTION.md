# Quick Start: Client-Server Connection

## Prerequisites

✅ SpacetimeDB server running on `http://localhost:7734`  
✅ Module `guildmaster` published to server  
✅ Godot 4.5 with C# support  
✅ .NET 8.0 SDK installed

## Step 1: Verify Server is Running

```bash
# Check if server is running
spacetime logs guildmaster --server http://127.0.0.1:7734

# You should see recent log entries
# If not, start your server:
cd path/to/guildmaster-server
spacetime publish guildmaster --server http://127.0.0.1:7734 --delete-data
```

## Step 2: Restore NuGet Packages

```bash
# From the project root
dotnet restore
```

This will install the SpacetimeDB C# SDK.

## Step 3: Build the Project

In Godot:
1. Open the project
2. Go to **Project → Tools → C# → Build**
3. Wait for build to complete

Or from command line:
```bash
dotnet build
```

## Step 4: Test Connection

### Option A: Use the Test Scene

1. Open `Scenes/ConnectionTest.tscn` in Godot
2. Press F5 or click the Play button
3. Click "Connect to Server"
4. You should see "Status: Connected!"
5. Click "Health Check" to test server communication
6. Click "Send Test Message" to send a test message

### Option B: Use the GameManager

1. Open any existing scene
2. Add a script to test connection:

```csharp
using Godot;
using GuildmasterMVP.Network;

public partial class TestConnection : Node
{
    public override async void _Ready()
    {
        var client = new SpacetimeDBClient();
        AddChild(client);
        
        client.Connected += (identity) => 
            GD.Print($"Connected! Identity: {identity}");
        
        bool success = await client.ConnectAsync();
        
        if (success)
        {
            await client.HealthCheckAsync();
            await client.TestMessageAsync("Hello from Godot!");
        }
    }
}
```

## Step 5: Verify Server Received Messages

Check your server logs:

```bash
spacetime logs guildmaster --server http://127.0.0.1:7734
```

You should see:
```
INFO: health_check: Guildmaster server health check - OK
INFO: test_message: Test message: Hello from Godot!
```

## What You've Accomplished

✅ Client successfully connects to SpacetimeDB server  
✅ **Connection properly fails when server is offline**  
✅ Connection manager handles lifecycle and reconnection  
✅ Reducers can be called from client  
✅ Server processes and logs client requests  
✅ Foundation ready for game-specific networking

## Testing Connection Behavior

### Test 1: Verify Failure When Server Offline

1. Make sure your SpacetimeDB server is **NOT** running
2. Open `Scenes/ConnectionBehaviorTest.tscn` in Godot
3. Run the scene
4. Check console output - should show:
   - ✅ TEST 1 PASSED: Properly failed when server offline
   - Connection attempts with retries
   - Final failure after max attempts

### Test 2: Verify Success When Server Online

1. Start your SpacetimeDB server:
   ```bash
   spacetime publish guildmaster --server http://127.0.0.1:7734
   ```
2. Run `Scenes/ConnectionBehaviorTest.tscn` again
3. Check console output - should show:
   - ✅ TEST 1 PASSED (offline test)
   - ✅ TEST 2 PASSED: Successfully connected to server

## Next Steps

### 1. Add Server Tables

Define your game tables in the server:

```rust
#[spacetimedb(table)]
pub struct Player {
    #[primarykey]
    pub id: u64,
    pub position_x: f32,
    pub position_y: f32,
    pub health: f32,
}
```

### 2. Add Game Reducers

Implement game logic reducers:

```rust
#[spacetimedb(reducer)]
pub fn input_move(ctx: ReducerContext, seq: u32, direction_x: f32, direction_y: f32, delta: f32) {
    // Process movement
}
```

### 3. Subscribe to Tables

In your client code:

```csharp
// Subscribe to player updates
client.SubscribeToTable("Player");

// Handle updates (when SDK is integrated)
// _dbConnection.OnUpdate<Player>(OnPlayerUpdated);
```

### 4. Implement Game Systems

Use the client in your game systems:

```csharp
// Movement system
await client.SendMoveInputAsync(playerId, direction, deltaTime);

// Combat system
await client.ExecuteAttackAsync(playerId, weaponType, directionX, directionY);

// Interaction system
await client.PickupItemAsync(playerId, itemId);
```

## Troubleshooting

### "Connection refused"

**Problem**: Server not running or wrong port

**Solution**:
```bash
# Check server status
spacetime logs guildmaster --server http://127.0.0.1:7734

# If not running, publish module
spacetime publish guildmaster --server http://127.0.0.1:7734
```

### "Module not found"

**Problem**: Module not published to server

**Solution**:
```bash
cd path/to/guildmaster-server
spacetime publish guildmaster --server http://127.0.0.1:7734 --delete-data
```

### "Build failed"

**Problem**: NuGet packages not restored

**Solution**:
```bash
dotnet restore
dotnet build
```

### "Reducer not found"

**Problem**: Reducer not defined in server or not published

**Solution**:
1. Check `src/lib.rs` in your server for the reducer
2. Rebuild and republish:
```bash
spacetime build
spacetime publish guildmaster --server http://127.0.0.1:7734
```

## Configuration

### Change Server URL

In `GameManager.cs`:
```csharp
[Export] public string ServerUrl { get; set; } = "http://localhost:7734";
```

Or programmatically:
```csharp
var config = new ConnectionConfig("http://your-server:7734", "guildmaster");
client.Configure(config);
```

### Adjust Retry Policy

```csharp
var config = new ConnectionConfig
{
    MaxRetryAttempts = 3,
    InitialRetryDelaySeconds = 2f,
    RetryBackoffMultiplier = 2f,
    EnableAutoReconnect = true
};
client.Configure(config);
```

### Enable Debug Logging

```csharp
var config = new ConnectionConfig
{
    EnableDebugLogging = true
};
client.Configure(config);
```

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Godot Client                         │
│                                                         │
│  ┌──────────────┐      ┌──────────────────────────┐   │
│  │ Game Systems │─────▶│ SpacetimeDBClient        │   │
│  │ (Movement,   │      │ - Reducer calls          │   │
│  │  Combat,     │      │ - Subscriptions          │   │
│  │  Inventory)  │      │ - State sync             │   │
│  └──────────────┘      └──────────────────────────┘   │
│                                │                        │
│                        ┌───────▼──────────┐            │
│                        │ ConnectionManager│            │
│                        │ - Lifecycle      │            │
│                        │ - Reconnection   │            │
│                        │ - Health monitor │            │
│                        └───────┬──────────┘            │
└────────────────────────────────┼────────────────────────┘
                                 │
                          WebSocket + BSATN
                                 │
┌────────────────────────────────▼────────────────────────┐
│                  SpacetimeDB Server                     │
│                                                         │
│  ┌──────────────┐      ┌──────────────────────────┐   │
│  │   Reducers   │      │        Tables            │   │
│  │ - input_move │      │ - Player                 │   │
│  │ - attack     │      │ - NpcState               │   │
│  │ - pickup     │      │ - WorldItem              │   │
│  └──────────────┘      └──────────────────────────┘   │
│                                                         │
│         Server-Authoritative Game Logic                │
└─────────────────────────────────────────────────────────┘
```

## Key Principles

1. **Server Authority**: All game logic runs on server
2. **Client Input Only**: Client sends inputs, not state changes
3. **State Synchronization**: Server broadcasts state to clients
4. **Client Prediction**: Local prediction for responsiveness
5. **Server Reconciliation**: Correct client state when needed

## Support

- **Documentation**: `Scripts/Network/README.md`
- **SpacetimeDB Docs**: https://spacetimedb.com/docs
- **Project Issues**: Check your project's issue tracker
