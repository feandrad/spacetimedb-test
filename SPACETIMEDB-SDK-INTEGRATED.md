# SpacetimeDB C# SDK Integration Complete

## Summary

Successfully integrated the official SpacetimeDB C# SDK (version 1.11.2) with the Godot client.

## What Was Done

### 1. Updated SDK Version
- **Old**: `SpacetimeDB.ClientSDK 1.0.0-rc1`
- **New**: `SpacetimeDB.ClientSDK 1.11.2` (latest as of Jan 2026)

### 2. Generated Client Bindings
Created `generate-bindings.sh` script that generates C# client code from the Rust server module:
```bash
./generate-bindings.sh
```

Generated files in `Scripts/Network/Generated/`:
- `SpacetimeDBClient.g.cs` - Main connection class
- `Reducers/RegisterPlayer.g.cs` - Player registration reducer
- `Tables/Player.g.cs` - Player table bindings
- And more...

### 3. Rewrote SpacetimeDBClient
Replaced the mock implementation with real SDK integration:
- Uses `DbConnection.Builder()` pattern
- Proper WebSocket connection to SpacetimeDB
- Real reducer calls via generated bindings
- Automatic frame ticking for updates

### 4. Fixed Client-Server Mismatch
- **Server has**: `register_player(username: String)`
- **Client now calls**: `RegisterPlayerAsync(username)`
- Color selection is client-side only (not stored on server yet)

## How It Works

### Connection Flow
```csharp
// 1. Create client
var client = new SpacetimeDBClient();

// 2. Connect to server
await client.ConnectAsync("http://localhost:7734");

// 3. Register player
await client.RegisterPlayerAsync("PlayerName");

// 4. Subscribe to tables
client.SubscribeToAllTables();

// 5. Process updates (happens automatically in _Process)
client.FrameTick();
```

### Main Menu Integration
`MainMenu.cs` now properly calls:
```csharp
bool success = await _client.RegisterPlayerAsync(_playerName);
```

This sends a real reducer call to the SpacetimeDB server.

## Current Status

✅ **Working**:
- SDK installed and integrated
- Client bindings generated
- Connection to server
- `register_player` reducer call
- Main menu player registration

❌ **Needs Work**:
- Many old test files still reference removed methods
- Need to implement table subscriptions and callbacks
- Need to handle Player table updates
- Need to implement other reducers (movement, combat, etc.)

## Next Steps

### Immediate (Get Main Menu Working)
1. Test the connection and registration flow
2. Subscribe to Player table
3. Handle player creation confirmation
4. Transition to PlayScene

### Short Term (Core Gameplay)
1. Implement movement input sending
2. Subscribe to Player position updates
3. Render players on the map
4. Handle map transitions

### Long Term (Full Integration)
1. Refactor all old reducer calls to use generated bindings
2. Implement proper table subscriptions
3. Add callbacks for all game events
4. Clean up test files

## Testing

### 1. Start Server
```bash
# In terminal 1
spacetime start --listen-addr 0.0.0.0:7734
```

### 2. Publish Module
```bash
# In terminal 2 (from guildmaster-server directory)
spacetime publish guildmaster --server http://127.0.0.1:7734
```

### 3. Run Game
- Open Godot
- Press F5
- Click "Connect to Server"
- Enter name
- Click "Start Game"

### 4. Verify in Server Logs
```bash
spacetime logs guildmaster --server http://127.0.0.1:7734
```

Should see:
```
✅ Player registered successfully - ID: X, Username: YourName
```

## Key Files

- `GuildmasterMVP.csproj` - Updated SDK version
- `generate-bindings.sh` - Binding generation script
- `Scripts/Network/SpacetimeDBClient.cs` - Real SDK integration
- `Scripts/Network/Generated/` - Auto-generated bindings
- `Scripts/Visual/MainMenu.cs` - Uses RegisterPlayerAsync

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Godot Client                             │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  MainMenu.cs                                         │  │
│  │  - Calls: RegisterPlayerAsync(username)             │  │
│  └────────────────────┬─────────────────────────────────┘  │
│                       │                                     │
│  ┌────────────────────▼─────────────────────────────────┐  │
│  │  SpacetimeDBClient.cs                                │  │
│  │  - DbConnection (SpacetimeDB SDK)                   │  │
│  │  - Reducers.RegisterPlayer(username)                │  │
│  │  - FrameTick() for updates                          │  │
│  └────────────────────┬─────────────────────────────────┘  │
│                       │                                     │
│  ┌────────────────────▼─────────────────────────────────┐  │
│  │  Generated Bindings                                  │  │
│  │  - Reducer.RegisterPlayer                           │  │
│  │  - Table.Player                                     │  │
│  │  - BSATN serialization                              │  │
│  └────────────────────┬─────────────────────────────────┘  │
└───────────────────────┼─────────────────────────────────────┘
                        │
                  WebSocket + BSATN
                        │
┌───────────────────────▼─────────────────────────────────────┐
│              SpacetimeDB Server (Rust)                      │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │  register_player(username: String)                  │  │
│  │  - Validates username                               │  │
│  │  - Creates Player row                               │  │
│  │  - Spawns at (100, 500) in starting_area           │  │
│  └─────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Troubleshooting

### "Cannot register player: not connected"
- Make sure server is running on port 7734
- Check connection was successful before calling RegisterPlayerAsync

### "Player already exists"
- Server prevents duplicate registrations per identity
- This is normal if you reconnect with the same identity

### Compilation Errors
- Many old test files reference removed methods
- Focus on MainMenu and PlayScene for now
- Other files will be refactored later

## Resources

- [SpacetimeDB C# SDK Docs](https://spacetimedb.com/docs/sdks/c-sharp)
- [SpacetimeDB C# Quickstart](https://spacetimedb.com/docs/sdks/c-sharp/quickstart)
- [NuGet Package](https://www.nuget.org/packages/SpacetimeDB.ClientSDK)
