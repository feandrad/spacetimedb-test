# Map Transition System - Complete Guide

## Overview

The server now has a fully functional map transition system with:
- ✅ Server-side movement validation with collision detection
- ✅ Map-specific boundaries
- ✅ Transition zones between maps
- ✅ Automatic transition detection
- ✅ Comprehensive logging

## Available Maps

### 1. starting_area (Starting Village)
- **Size:** 1000x1000 pixels
- **Bounds:** X: 0-1000, Y: 0-1000
- **Spawn Points:** (100, 500), (150, 500), (200, 500), (250, 500)
- **Transition Zone:**
  - **Location:** X: 950-1000, Y: 400-600 (right edge)
  - **Destination:** forest_area at (50, 500)

### 2. forest_area (Dark Forest)
- **Size:** 1200x1200 pixels
- **Bounds:** X: 0-1200, Y: 0-1200
- **Spawn Points:** (100, 400), (150, 400), (200, 400), (250, 400)
- **Transition Zone:**
  - **Location:** X: 0-50, Y: 400-600 (left edge)
  - **Destination:** starting_area at (900, 500)

## Movement System

### Server-Side Validation

The server validates ALL player movement to prevent cheating:

1. **Speed Validation:** Max 250 pixels/second
2. **Position Delta:** Max 50 pixels per update (prevents teleporting)
3. **Map Boundaries:** Players cannot move outside map bounds
4. **Sequence Validation:** Ignores old/duplicate movement packets

### Movement Reducer

```rust
update_player_position(
    player_id: u32,
    new_x: f32,
    new_y: f32,
    velocity_x: f32,
    velocity_y: f32,
    input_sequence: u32
)
```

**Example:**
```bash
spacetime call guildmaster update_player_position --server http://127.0.0.1:7734 \
    3058020705 150.0 500.0 10.0 0.0 1
```

## Map Transition System

### How It Works

1. **Player moves near map edge** (into transition zone)
2. **Client detects transition zone** (optional client-side check)
3. **Client calls `transition_to_map`** reducer
4. **Server validates:**
   - Player is in correct transition zone
   - Destination map is valid
   - Player owns the identity
5. **Server teleports player** to destination map at spawn point
6. **All clients receive update** via Player table subscription

### Transition Reducers

#### 1. Get Transition Zones
```rust
get_transition_zones(map_id: String)
```

Returns information about all transition zones in a map.

**Example:**
```bash
spacetime call guildmaster get_transition_zones --server http://127.0.0.1:7734 "starting_area"
```

**Output:**
```
🚪 Map starting_area has 1 transition zones:
  Zone 1: to forest_area at (950.0, 400.0) size 50x200 -> dest (50.0, 500.0)
```

#### 2. Check Player Transition
```rust
check_player_transition(player_id: u32)
```

Checks if a player is currently in a transition zone.

**Example:**
```bash
spacetime call guildmaster check_player_transition --server http://127.0.0.1:7734 3058020705
```

**Output (if in zone):**
```
✅ Player 3058020705 is in transition zone to forest_area (will spawn at 50.0, 500.0)
```

**Output (if not in zone):**
```
❌ Player 3058020705 is NOT in any transition zone
```

#### 3. Transition to Map
```rust
transition_to_map(player_id: u32, map_id: String)
```

Transitions a player to another map (must be in transition zone).

**Example:**
```bash
# First, move player to transition zone (X: 950-1000, Y: 400-600)
spacetime call guildmaster update_player_position --server http://127.0.0.1:7734 \
    3058020705 975.0 500.0 0.0 0.0 2

# Check if in transition zone
spacetime call guildmaster check_player_transition --server http://127.0.0.1:7734 3058020705

# Transition to forest_area
spacetime call guildmaster transition_to_map --server http://127.0.0.1:7734 \
    3058020705 "forest_area"
```

**Output:**
```
🚪 Player 3058020705 transitioned from starting_area to forest_area at (50.0, 500.0)
```

#### 4. Spawn Player at Map
```rust
spawn_player_at_map(player_id: u32, map_id: String)
```

Spawns a player at a map's spawn point (no transition zone required).

**Example:**
```bash
spacetime call guildmaster spawn_player_at_map --server http://127.0.0.1:7734 \
    3058020705 "forest_area"
```

**Output:**
```
🎯 Player 3058020705 spawned at map forest_area at (100.0, 400.0)
```

## Client Integration

### Step 1: Subscribe to Player Table

```gdscript
# Subscribe to all players
spacetime_client.subscribe("SELECT * FROM player")

# Handle player updates
func _on_player_update(player_data):
    if player_data.id == local_player_id:
        # Update local player
        update_local_player(player_data)
        
        # Check if map changed
        if player_data.current_map_id != current_map:
            load_new_map(player_data.current_map_id)
            current_map = player_data.current_map_id
    else:
        # Update other players
        update_remote_player(player_data)
```

### Step 2: Send Movement Updates

```gdscript
var input_sequence = 0

func _physics_process(delta):
    # Get player input
    var velocity = get_input_velocity()
    var new_position = position + velocity * delta
    
    # Send to server
    input_sequence += 1
    spacetime_client.call_reducer("update_player_position", [
        local_player_id,
        new_position.x,
        new_position.y,
        velocity.x,
        velocity.y,
        input_sequence
    ])
    
    # Apply movement locally (client prediction)
    position = new_position
```

### Step 3: Handle Transition Zones

```gdscript
# Define transition zones (match server configuration)
var transition_zones = {
    "starting_area": [
        {
            "area": Rect2(950, 400, 50, 200),
            "destination": "forest_area"
        }
    ],
    "forest_area": [
        {
            "area": Rect2(0, 400, 50, 200),
            "destination": "starting_area"
        }
    ]
}

func check_transition_zones():
    if current_map not in transition_zones:
        return
    
    for zone in transition_zones[current_map]:
        if zone.area.has_point(player_position):
            # Show transition prompt
            show_transition_ui(zone.destination)
            
            # Or auto-transition
            if Input.is_action_just_pressed("interact"):
                transition_to_map(zone.destination)

func transition_to_map(destination_map: String):
    spacetime_client.call_reducer("transition_to_map", [
        local_player_id,
        destination_map
    ])
```

### Step 4: Render Transition Zones (Optional)

```gdscript
func draw_transition_zones():
    # Visual indicator for transition zones
    for zone in transition_zones[current_map]:
        var rect = ColorRect.new()
        rect.rect_position = Vector2(zone.area.position.x, zone.area.position.y)
        rect.rect_size = Vector2(zone.area.size.x, zone.area.size.y)
        rect.color = Color(1, 1, 0, 0.3)  # Yellow semi-transparent
        add_child(rect)
        
        # Add label
        var label = Label.new()
        label.text = "→ " + zone.destination
        label.rect_position = rect.rect_position + Vector2(10, 10)
        add_child(label)
```

## Testing Workflow

### Complete Test Sequence

```bash
# 1. Register player
spacetime call guildmaster register_player --server http://127.0.0.1:7734 "TestPlayer"

# 2. Check initial position
spacetime call guildmaster get_player_position --server http://127.0.0.1:7734 3058020705

# 3. View transition zones
spacetime call guildmaster get_transition_zones --server http://127.0.0.1:7734 "starting_area"

# 4. Move player to transition zone
spacetime call guildmaster update_player_position --server http://127.0.0.1:7734 \
    3058020705 975.0 500.0 0.0 0.0 1

# 5. Check if in transition zone
spacetime call guildmaster check_player_transition --server http://127.0.0.1:7734 3058020705

# 6. Transition to forest
spacetime call guildmaster transition_to_map --server http://127.0.0.1:7734 \
    3058020705 "forest_area"

# 7. Verify new position
spacetime call guildmaster get_player_position --server http://127.0.0.1:7734 3058020705

# 8. View forest transition zones
spacetime call guildmaster get_transition_zones --server http://127.0.0.1:7734 "forest_area"

# 9. Move to forest transition zone
spacetime call guildmaster update_player_position --server http://127.0.0.1:7734 \
    3058020705 25.0 500.0 0.0 0.0 2

# 10. Transition back to starting area
spacetime call guildmaster transition_to_map --server http://127.0.0.1:7734 \
    3058020705 "starting_area"

# 11. View logs
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 20
```

## Logging

All operations are logged with emojis for easy identification:

- 🏃 **Movement:** Player position updates
- 🚧 **Collision:** Movement clamped to boundaries
- 🚪 **Transition:** Player entering/exiting transition zones
- 🗺️  **Map State:** Map instance state changes
- 🎯 **Spawn:** Player spawning at map
- 📍 **Position:** Position queries
- ✅ **Success:** Successful operations
- ❌ **Error:** Failed operations

## Map Instance States

Maps have three states based on player activity:

1. **Cold:** No players, map inactive
2. **Warm:** No players currently, but recently active
3. **Hot:** Players present, map fully active

This system can be used for:
- Resource optimization
- Dynamic content loading
- Event spawning
- Server load balancing

## Error Handling

### Common Errors

1. **"Player not in valid transition zone"**
   - Player must be inside transition zone boundaries
   - Use `check_player_transition` to verify

2. **"Invalid destination map"**
   - Map ID doesn't exist
   - Valid maps: "starting_area", "forest_area"

3. **"Unauthorized map transition"**
   - Player identity doesn't match sender
   - Security check to prevent cheating

4. **"Player not found"**
   - Player ID doesn't exist
   - Register player first with `register_player`

## Performance Considerations

- Movement updates are validated server-side (prevents cheating)
- Map boundaries prevent out-of-bounds movement
- Transition zones are checked efficiently
- Map instances track player count for optimization
- All operations are logged for debugging

## Next Steps

1. **Add more maps** by editing `get_map_transitions()` in `src/map.rs`
2. **Customize transition zones** by modifying zone coordinates
3. **Add visual effects** in client for transitions
4. **Implement loading screens** during map transitions
5. **Add map-specific content** (enemies, items, NPCs)

## Summary

Your server now has:
- ✅ Complete movement validation system
- ✅ Map-specific collision boundaries
- ✅ Two fully functional maps
- ✅ Transition zones between maps
- ✅ Automatic transition detection
- ✅ Comprehensive logging
- ✅ Ready for client integration

Players can now move between maps seamlessly with full server-side validation!
