# Map Entry/Exit Logging - Complete ✅

## Overview

The server now logs **every time a player enters or leaves a map** with clear, emoji-tagged messages that include the player's username and exact coordinates.

## Log Format

All map entry/exit logs use the 👋 emoji and include:
- Player ID
- Player Username (in parentheses)
- Action (entering/leaving)
- Map name
- Coordinates (for entry)

## Log Examples

### Player Registration (First Entry)
```
✅ Player registered successfully - ID: 3058020705, Username: TestPlayer, Identity: Identity(...)
👋 Player 3058020705 (TestPlayer) entered map: starting_area at (100.0, 500.0)
```

### Map Transition (Exit + Entry)
```
👋 Player 3058020705 (TestPlayer) is leaving map: starting_area
🗺️  Map forest_area transitioned from Cold to Hot (players entering)
👋 Player 3058020705 (TestPlayer) entered map: forest_area at (50.0, 500.0)
🚪 Player 3058020705 transitioned from starting_area to forest_area at (50.0, 500.0)
```

### Spawn at Map (Exit + Entry)
```
👋 Player 3058020705 (TestPlayer) is leaving map: starting_area
🗺️  Map starting_area transitioned from Hot to Warm (no players)
👋 Player 3058020705 (TestPlayer) entered map: forest_area at (150.0, 400.0)
🗺️  Map forest_area transitioned from Warm to Hot (players present)
🎯 Player 3058020705 spawned at map forest_area at (150.0, 400.0)
```

## Complete Log Flow Example

Here's a complete session showing all entry/exit logs:

```bash
# 1. Player registers (enters game world)
2026-01-18T23:11:57.613058Z  INFO: ✅ Player registered successfully - ID: 3058020705, Username: TestPlayer
2026-01-18T23:11:57.613130Z  INFO: 👋 Player 3058020705 (TestPlayer) entered map: starting_area at (100.0, 500.0)

# 2. Player moves to transition zone
2026-01-18T23:12:36.318732Z  INFO: 🔧 Force corrected player 3058020705 position to (975.0, 500.0)

# 3. Player checks if in transition zone
2026-01-18T23:12:41.769300Z  INFO: 🚪 Player 3058020705 entered transition zone to forest_area
2026-01-18T23:12:41.769340Z  INFO: ✅ Player 3058020705 is in transition zone to forest_area (will spawn at 50.0, 500.0)

# 4. Player transitions to forest_area (EXIT + ENTRY)
2026-01-18T23:12:47.167902Z  INFO: 👋 Player 3058020705 (TestPlayer) is leaving map: starting_area
2026-01-18T23:12:47.167984Z  INFO: 🗺️  Map forest_area transitioned from Cold to Hot (players entering)
2026-01-18T23:12:47.168015Z  INFO: 👋 Player 3058020705 (TestPlayer) entered map: forest_area at (50.0, 500.0)
2026-01-18T23:12:47.168057Z  INFO: 🚪 Player 3058020705 transitioned from starting_area to forest_area at (50.0, 500.0)

# 5. Player moves to forest transition zone
2026-01-18T23:12:58.876500Z  INFO: 🔧 Force corrected player 3058020705 position to (25.0, 500.0)

# 6. Player transitions back to starting_area (EXIT + ENTRY)
2026-01-18T23:12:58.896652Z  INFO: 👋 Player 3058020705 (TestPlayer) is leaving map: forest_area
2026-01-18T23:12:58.896691Z  INFO: 🗺️  Map forest_area transitioned from Hot to Warm (no players)
2026-01-18T23:12:58.896714Z  INFO: 🗺️  Map starting_area transitioned from Cold to Hot (players entering)
2026-01-18T23:12:58.896743Z  INFO: 👋 Player 3058020705 (TestPlayer) entered map: starting_area at (900.0, 500.0)
2026-01-18T23:12:58.896770Z  INFO: 🚪 Player 3058020705 transitioned from forest_area to starting_area at (900.0, 500.0)

# 7. Player spawns at forest_area (EXIT + ENTRY)
2026-01-18T23:13:10.011336Z  INFO: 👋 Player 3058020705 (TestPlayer) is leaving map: starting_area
2026-01-18T23:13:10.011388Z  INFO: 🗺️  Map starting_area transitioned from Hot to Warm (no players)
2026-01-18T23:13:10.011418Z  INFO: 👋 Player 3058020705 (TestPlayer) entered map: forest_area at (150.0, 400.0)
2026-01-18T23:13:10.011434Z  INFO: 🗺️  Map forest_area transitioned from Warm to Hot (players present)
2026-01-18T23:13:10.011485Z  INFO: 🎯 Player 3058020705 spawned at map forest_area at (150.0, 400.0)
```

## Log Emoji Guide

| Emoji | Meaning | Example |
|-------|---------|---------|
| 👋 | Player entering/leaving map | `👋 Player 123 (John) entered map: forest_area at (50.0, 500.0)` |
| ✅ | Successful operation | `✅ Player registered successfully` |
| 🚪 | Map transition | `🚪 Player 123 transitioned from starting_area to forest_area` |
| 🎯 | Player spawn | `🎯 Player 123 spawned at map forest_area` |
| 🗺️ | Map state change | `🗺️  Map forest_area transitioned from Cold to Hot` |
| 🏃 | Player movement | `🏃 Player 123 moved to (150.0, 500.0)` |
| 🔧 | Force position correction | `🔧 Force corrected player 123 position` |
| 📍 | Position query | `📍 Player 123 position: (100.0, 500.0)` |

## Filtering Logs

### View only entry/exit logs:
```bash
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 50 | grep "👋"
```

### View only transitions:
```bash
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 50 | grep "🚪"
```

### View only map state changes:
```bash
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 50 | grep "🗺️"
```

### View all map-related logs:
```bash
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 50 | grep -E "👋|🚪|🗺️|🎯"
```

## Use Cases

### 1. Player Activity Monitoring
Track when players join and leave maps:
```
👋 Player 3058020705 (TestPlayer) entered map: starting_area at (100.0, 500.0)
👋 Player 3058020705 (TestPlayer) is leaving map: starting_area
```

### 2. Map Population Tracking
See which maps are active:
```
🗺️  Map forest_area transitioned from Cold to Hot (players entering)
🗺️  Map starting_area transitioned from Hot to Warm (no players)
```

### 3. Debugging Transitions
Verify transitions work correctly:
```
👋 Player 3058020705 (TestPlayer) is leaving map: starting_area
👋 Player 3058020705 (TestPlayer) entered map: forest_area at (50.0, 500.0)
🚪 Player 3058020705 transitioned from starting_area to forest_area at (50.0, 500.0)
```

### 4. Analytics & Metrics
Extract data for analysis:
- Count entries per map
- Track player movement patterns
- Measure map popularity
- Monitor transition frequency

## Client Integration

### Listening for Map Changes

Your client can detect map changes by subscribing to the Player table:

```gdscript
func _on_player_update(player_data):
    if player_data.id == local_player_id:
        if player_data.current_map_id != current_map:
            # Player changed maps
            on_map_changed(current_map, player_data.current_map_id)
            current_map = player_data.current_map_id

func on_map_changed(old_map: String, new_map: String):
    print("Left map: ", old_map)
    print("Entered map: ", new_map)
    
    # Load new map
    load_map(new_map)
    
    # Show transition effect
    show_transition_effect()
    
    # Update UI
    update_map_label(new_map)
```

### Logging Client-Side

Match server logs with client logs:

```gdscript
func on_map_changed(old_map: String, new_map: String):
    # Client-side log (matches server format)
    print("👋 Player %s (%s) is leaving map: %s" % [player_id, username, old_map])
    print("👋 Player %s (%s) entered map: %s" % [player_id, username, new_map])
```

## Testing

### Quick Test Script

```bash
#!/bin/bash
SERVER="http://127.0.0.1:7734"

# Register player (first entry)
echo "=== Testing Player Registration (Entry) ==="
spacetime call guildmaster register_player --server $SERVER "TestPlayer"
sleep 1

# View entry log
spacetime logs guildmaster --server $SERVER --num-lines 5 | grep "👋"
echo ""

# Transition to forest (exit + entry)
echo "=== Testing Map Transition (Exit + Entry) ==="
spacetime call guildmaster force_player_position --server $SERVER 3058020705 975.0 500.0
spacetime call guildmaster transition_to_map --server $SERVER 3058020705 "forest_area"
sleep 1

# View transition logs
spacetime logs guildmaster --server $SERVER --num-lines 10 | grep "👋"
echo ""

# Spawn at starting area (exit + entry)
echo "=== Testing Spawn (Exit + Entry) ==="
spacetime call guildmaster spawn_player_at_map --server $SERVER 3058020705 "starting_area"
sleep 1

# View spawn logs
spacetime logs guildmaster --server $SERVER --num-lines 10 | grep "👋"
```

## Summary

Your server now logs:

### ✅ Player Registration
- Entry to starting map with coordinates

### ✅ Map Transitions
- Exit from old map (with username)
- Entry to new map (with username and coordinates)
- Transition summary

### ✅ Map Spawning
- Exit from old map (if different)
- Entry to new map (with coordinates)
- Spawn confirmation

### ✅ Map State Changes
- Cold → Hot (players entering)
- Hot → Warm (no players)
- Warm → Hot (players returning)

All logs include:
- 👋 Emoji for easy identification
- Player ID and username
- Map names
- Exact coordinates for entries
- Timestamps for analytics

**Perfect for monitoring player activity, debugging transitions, and gathering analytics!** 📊
