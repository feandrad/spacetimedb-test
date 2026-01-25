# Client Integration Guide - Authentication & Map Rendering (C# Raylib)

## Overview

The server now supports player authentication and map data retrieval with comprehensive logging. This guide shows you how to integrate these features into your client.

## Server Features

### 1. Player Registration & Authentication

**Reducer:** `register_player(username: String)`
- Automatically creates a player account on first connection
- Assigns unique player ID
- Places player at starting area spawn point
- Logs: `✅ Player registered successfully - ID: X, Username: Y`

**Reducer:** `get_player_info()`
- Verifies player authentication
- Returns current player state
- Logs: `✅ Player authenticated - ID: X, Username: Y, Map: Z, Position: (X, Y)`

### 2. Map Data Retrieval

**Reducer:** `get_map_data(map_id: String)`
- Retrieves map metadata for rendering
- Lists all players in the map
- Logs:
  - `✅ Map data requested - Player: X, Map: Y`
  - `📍 Map Info - ID: X, Name: Y, Size: WxH, Spawn: (X, Y)`
  - `👥 Players in map X: N`
  - `  - PlayerName (ID: X) at (X, Y)` (for each player)

## Available Maps

### starting_area
- **Name:** Starting Village
- **Size:** 1000x1000
- **Spawn Point:** (100, 500)

### forest_area
- **Name:** Dark Forest
- **Size:** 1200x1200
- **Spawn Point:** (100, 400)

## Client Integration Steps

### Step 1: Connect to Server

```csharp
# In your C# client
var client = new GuildmasterClient();
client.Connect("http://localhost:7734", "guildmaster");
```

### Step 2: Register/Authenticate Player

```csharp
// On connection established
client.Register(username);
// Verify authentication (handled by OnConnect callback in our C# client)

```

### Step 3: Subscribe to Player Table

```csharp
// Subscribe to player updates (handled in Connect method via SubscriptionBuilder)
// client.SubscribeToMap("starting_area");
    
# Handle player updates
func _on_player_update(player_data):
    # player_data contains:
    # - id: u32
    # - identity: Identity
    # - username: String
    # - position_x: f32
    # - position_y: f32
    # - current_map_id: String
    # - health: f32
    # - max_health: f32
    # - is_downed: bool
    
    update_player_position(player_data)
```

### Step 4: Subscribe to Map Data (Reactive Pattern)
Instead of calling a reducer, we subscribe to database updates using SQL queries. This ensures the client automatically receives updates when entities spawn, move, or despawn.

```csharp
public void SubscribeToMap(string mapId)
{
    // Subscribe to map-specific entities and players in that map
    string[] queries =
    [
        $"SELECT * FROM player WHERE current_map_id = '{mapId}'",
        $"SELECT * FROM enemy WHERE map_id = '{mapId}'",
        $"SELECT * FROM interactable_object WHERE map_id = '{mapId}'",
        $"SELECT * FROM map_instance WHERE key_id = '{mapId}'"
    ];

    Connection.SubscriptionBuilder()
        .OnApplied(ctx => Console.WriteLine($"Map data for {mapId} loaded successfully!"))
        .OnError((ctx, ex) => Console.WriteLine($"Subscription error: {ex.Message}"))
        .Subscribe(queries);
}
```

**Key Benefit**: This "Blackholio" pattern means your client is always in sync. You don't need to manually poll for map data or new players. The `SyncSystem` (Step 6) handles the logical updates via `OnInsert`/`OnUpdate`/`OnDelete` events.

### Step 5: Render Map

```csharp
func render_starting_village():
    # Map dimensions: 1000x1000
    # Spawn point: (100, 500)
    
    # Load your tilemap or scene
    var map_scene = preload("res://maps/starting_village.tscn")
    var map_instance = map_scene.instantiate()
    add_child(map_instance)
    
    # Position camera at spawn
    camera.position = Vector2(100, 500)
```

### Step 6: Render Other Players

```csharp
# When you receive player updates from subscription
func _on_player_update(player_data):
    # Only render players in the same map
    if player_data.current_map_id == current_player.current_map_id:
        var player_sprite = get_or_create_player_sprite(player_data.id)
        player_sprite.position = Vector2(player_data.position_x, player_data.position_y)
        player_sprite.set_username_label(player_data.username)
        
        # Update health bar
        player_sprite.set_health(player_data.health, player_data.max_health)
```

## Logging & Debugging

### Server Logs

Check server logs to confirm operations:

```bash
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 20
```

### Expected Log Flow

1. **Player Registration:**
   ```
   ✅ Player registered successfully - ID: 3058020705, Username: TestPlayer
   ```

2. **Authentication Check:**
   ```
   ✅ Player authenticated - ID: 3058020705, Username: TestPlayer, Map: starting_area, Position: (100.0, 500.0)
   ```

3. **Map Data Request:**
   ```
   ✅ Map data requested - Player: TestPlayer, Map: starting_area
   📍 Map Info - ID: starting_area, Name: Starting Village, Size: 1000x1000, Spawn: (100.0, 500.0)
   👥 Players in map starting_area: 1
     - TestPlayer (ID: 3058020705) at (100.0, 500.0)
   ```

## Testing from Command Line

```bash
# Register a player
spacetime call guildmaster register_player --server http://127.0.0.1:7734 "TestPlayer"

# Check authentication
spacetime call guildmaster get_player_info --server http://127.0.0.1:7734

# Get map data
spacetime call guildmaster get_map_data --server http://127.0.0.1:7734 "starting_area"

# View logs
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 20
```

## Next Steps

1. **Movement System:** Use the existing `update_player_position` reducer to move players
2. **Map Transitions:** Use `transition_to_map` or `spawn_player_at_map` reducers
3. **Combat System:** Integrate with existing combat reducers
4. **Inventory:** Use inventory management reducers

## Player Table Schema

```rust
pub struct Player {
    pub id: u32,                    // Unique player ID
    pub identity: Identity,         // SpacetimeDB identity (unique)
    pub username: String,           // Display name
    pub position_x: f32,            // X coordinate
    pub position_y: f32,            // Y coordinate
    pub velocity_x: f32,            // X velocity
    pub velocity_y: f32,            // Y velocity
    pub current_map_id: String,     // Current map
    pub health: f32,                // Current health
    pub max_health: f32,            // Maximum health
    pub is_downed: bool,            // Downed state
    pub last_input_sequence: u32,   // For movement sync
}
```

## Common Issues

### Issue: Player not found
**Solution:** Make sure to call `register_player` first before other operations

### Issue: Map data not showing
**Solution:** Check server logs to confirm the reducer was called successfully

### Issue: Other players not visible
**Solution:** Ensure you're subscribed to the player table and filtering by current_map_id

## Support

For more information, check:
- Server logs: `spacetime logs guildmaster --server http://127.0.0.1:7734`
- SpacetimeDB docs: https://spacetimedb.com/docs
- Server code: `src/lib.rs`
