# Server-Side Player Creation Implementation

## Overview

This guide shows how to implement the player creation reducer on your SpacetimeDB server to work with the new main menu.

## Required Server Changes

### 1. Add Player Table

In your `guildmaster-server/src/lib.rs`, add the Player table:

```rust
use spacetimedb::{spacetimedb, Identity, ReducerContext, Table};

#[spacetimedb(table)]
pub struct Player {
    #[primarykey]
    #[autoinc]
    pub id: u64,
    
    #[unique]
    pub identity: Identity,
    
    pub name: String,
    pub color_r: f32,
    pub color_g: f32,
    pub color_b: f32,
    
    pub position_x: f32,
    pub position_y: f32,
    pub map_id: String,
    
    pub health: f32,
    pub max_health: f32,
    
    pub created_at: u64,
}
```

### 2. Add Player Creation Reducer

```rust
#[spacetimedb(reducer)]
pub fn create_player(
    ctx: ReducerContext,
    name: String,
    color_r: f32,
    color_g: f32,
    color_b: f32,
) -> Result<(), String> {
    // Check if player already exists
    if ctx.db.player().identity().filter(&ctx.sender).count() > 0 {
        return Err("Player already exists".to_string());
    }
    
    // Validate name
    if name.is_empty() || name.len() > 20 {
        return Err("Invalid name length (1-20 characters)".to_string());
    }
    
    // Validate color values (0.0 to 1.0)
    if color_r < 0.0 || color_r > 1.0 ||
       color_g < 0.0 || color_g > 1.0 ||
       color_b < 0.0 || color_b > 1.0 {
        return Err("Invalid color values".to_string());
    }
    
    // Create player at spawn point
    let spawn_position = get_spawn_position();
    
    let player = Player {
        id: 0, // Auto-incremented
        identity: ctx.sender,
        name,
        color_r,
        color_g,
        color_b,
        position_x: spawn_position.0,
        position_y: spawn_position.1,
        map_id: "core:overworld/tavern".to_string(), // Starting map
        health: 100.0,
        max_health: 100.0,
        created_at: ctx.timestamp,
    };
    
    ctx.db.player().insert(player);
    
    log::info!("Player created: {} (identity: {})", name, ctx.sender);
    
    Ok(())
}

fn get_spawn_position() -> (f32, f32) {
    // Return spawn coordinates for the tavern/starting area
    (400.0, 300.0)
}
```

### 3. Add Player Query Reducer

```rust
#[spacetimedb(reducer)]
pub fn get_player_info(ctx: ReducerContext) -> Result<(), String> {
    match ctx.db.player().identity().filter(&ctx.sender).next() {
        Some(player) => {
            log::info!(
                "Player info: id={}, name={}, map={}, pos=({}, {})",
                player.id,
                player.name,
                player.map_id,
                player.position_x,
                player.position_y
            );
            Ok(())
        }
        None => Err("Player not found".to_string()),
    }
}
```

### 4. Add Player Spawn Reducer

```rust
#[spacetimedb(reducer)]
pub fn spawn_player_in_map(
    ctx: ReducerContext,
    map_id: String,
    spawn_x: f32,
    spawn_y: f32,
) -> Result<(), String> {
    let mut player = ctx.db.player().identity().filter(&ctx.sender)
        .next()
        .ok_or("Player not found")?;
    
    // Update player position and map
    player.map_id = map_id.clone();
    player.position_x = spawn_x;
    player.position_y = spawn_y;
    
    ctx.db.player().id().update(player);
    
    log::info!(
        "Player {} spawned in {} at ({}, {})",
        player.name,
        map_id,
        spawn_x,
        spawn_y
    );
    
    Ok(())
}
```

## Build and Publish

After adding these changes:

```bash
# Build the server
cd guildmaster-server
spacetime build

# Publish to your running server
spacetime publish guildmaster --server http://127.0.0.1:7734 --delete-data
```

## Client Integration

Update the `MainMenu.cs` to call the actual reducer:

```csharp
private async Task<bool> CreatePlayerOnServer()
{
    GD.Print($"[MainMenu] Creating player: {_playerName}, Color: {_selectedColor}");
    
    try
    {
        // Call the create_player reducer
        bool success = await _client.CallReducerAsync(
            "create_player",
            _playerName,
            _selectedColor.R,
            _selectedColor.G,
            _selectedColor.B
        );
        
        if (success)
        {
            GD.Print("[MainMenu] Player created successfully");
            return true;
        }
        else
        {
            GD.PrintErr("[MainMenu] Failed to create player");
            return false;
        }
    }
    catch (Exception ex)
    {
        GD.PrintErr($"[MainMenu] Error creating player: {ex.Message}");
        return false;
    }
}
```

## Testing

1. **Start SpacetimeDB server:**
   ```bash
   spacetime start --listen-addr 0.0.0.0:7734
   ```

2. **Publish your module:**
   ```bash
   spacetime publish guildmaster --server http://127.0.0.1:7734 --delete-data
   ```

3. **Run the game:**
   - Open Godot
   - Press F5 to run
   - Click "Connect to Server"
   - Enter name and choose color
   - Click "Start Game"

4. **Verify in server logs:**
   ```bash
   spacetime logs guildmaster --server http://127.0.0.1:7734
   ```
   
   You should see:
   ```
   INFO: Player created: YourName (identity: ...)
   ```

## Next Steps

After player creation works:

1. **Create Main Game Scene** - The actual gameplay scene with map rendering
2. **Implement Player Spawning** - Spawn player entity in the game world
3. **Add Movement System** - Connect input to server-authoritative movement
4. **Subscribe to Player Table** - Receive real-time player updates
5. **Add Other Players** - Render other connected players

## Troubleshooting

### "Player already exists" Error

**Problem:** Trying to create a player when one already exists for this identity

**Solution:** Add a check in the client or implement a "get or create" pattern

### "Invalid color values" Error

**Problem:** Color values outside 0.0-1.0 range

**Solution:** Ensure Godot Color values are normalized (they should be by default)

### Player Not Spawning

**Problem:** Player created but not appearing in game

**Solution:** 
1. Check server logs for successful creation
2. Verify subscription to Player table
3. Ensure game scene is loading correctly
4. Check spawn position coordinates

## Database Queries

Check player data directly:

```bash
# List all players
spacetime sql guildmaster --server http://127.0.0.1:7734 "SELECT * FROM Player"

# Check specific player
spacetime sql guildmaster --server http://127.0.0.1:7734 "SELECT * FROM Player WHERE name = 'YourName'"

# Delete all players (for testing)
spacetime sql guildmaster --server http://127.0.0.1:7734 "DELETE FROM Player"
```
