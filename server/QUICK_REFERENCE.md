# Guildmaster Server - Quick Reference Card

## 🎮 Server Status
- **Running:** http://localhost:7734
- **Module:** guildmaster
- **Version:** SpacetimeDB 1.11.1

## 📋 Log Emojis

| Emoji | Meaning |
|-------|---------|
| ✅ | Success |
| ❌ | Error |
| 👋 | Map entry/exit |
| 🚪 | Transition |
| 🎯 | Spawn |
| 🗺️ | Map state |
| 🏃 | Movement |
| 🔧 | Force position |
| 📍 | Position query |
| 🚧 | Collision |

## 🗺️ Maps

### starting_area
- Size: 1000x1000
- Spawn: (100, 500)
- Transition: X:950-1000, Y:400-600 → forest_area

### forest_area
- Size: 1200x1200
- Spawn: (100, 400)
- Transition: X:0-50, Y:400-600 → starting_area

## 🔧 Essential Commands

### View Logs
```bash
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 20
```

### Register Player
```bash
spacetime call guildmaster register_player --server http://127.0.0.1:7734 "Username"
```

### Check Position
```bash
spacetime call guildmaster get_player_position --server http://127.0.0.1:7734 PLAYER_ID
```

### View Transition Zones
```bash
spacetime call guildmaster get_transition_zones --server http://127.0.0.1:7734 "MAP_ID"
```

### Check if in Transition Zone
```bash
spacetime call guildmaster check_player_transition --server http://127.0.0.1:7734 PLAYER_ID
```

### Transition to Map
```bash
spacetime call guildmaster transition_to_map --server http://127.0.0.1:7734 PLAYER_ID "MAP_ID"
```

### Force Position (Testing)
```bash
spacetime call guildmaster force_player_position --server http://127.0.0.1:7734 PLAYER_ID X Y
```

## 📊 Key Reducers

### Authentication
- `register_player(username)` - Register new player
- `get_player_info()` - Get authenticated player info

### Movement
- `update_player_position(player_id, x, y, vx, vy, seq)` - Update position
- `get_player_position(player_id)` - Query position
- `force_player_position(player_id, x, y)` - Force set position

### Maps
- `get_map_data(map_id)` - Get map metadata
- `get_transition_zones(map_id)` - List transition zones
- `check_player_transition(player_id)` - Check if in zone
- `transition_to_map(player_id, map_id)` - Transition maps
- `spawn_player_at_map(player_id, map_id)` - Spawn at map
- `get_players_in_map(map_id)` - List players in map

## 🔍 Log Filtering

### Entry/Exit Only
```bash
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 50 | grep "👋"
```

### Transitions Only
```bash
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 50 | grep "🚪"
```

### Movement Only
```bash
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 50 | grep "🏃"
```

### All Map Events
```bash
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 50 | grep -E "👋|🚪|🗺️|🎯"
```

## 🧪 Quick Test

```bash
# 1. Register
spacetime call guildmaster register_player --server http://127.0.0.1:7734 "TestPlayer"

# 2. Move to transition zone
spacetime call guildmaster force_player_position --server http://127.0.0.1:7734 3058020705 975.0 500.0

# 3. Check zone
spacetime call guildmaster check_player_transition --server http://127.0.0.1:7734 3058020705

# 4. Transition
spacetime call guildmaster transition_to_map --server http://127.0.0.1:7734 3058020705 "forest_area"

# 5. View logs
spacetime logs guildmaster --server http://127.0.0.1:7734 --num-lines 20
```

## 📚 Documentation Files

- `MAP_TRANSITION_GUIDE.md` - Complete usage guide
- `MAP_LAYOUT_DIAGRAM.md` - Visual map diagrams
- `MAP_ENTRY_EXIT_LOGS.md` - Entry/exit logging
- `CLIENT_INTEGRATION_GUIDE.md` - Client integration
- `MOVEMENT_AND_MAPS_COMPLETE.md` - Full implementation summary
- `QUICK_REFERENCE.md` - This file

## 🚀 Client Integration Checklist

- [ ] Subscribe to Player table
- [ ] Send movement updates
- [ ] Detect transition zones
- [ ] Call transition reducer
- [ ] Handle map changes
- [ ] Render other players
- [ ] Show transition UI

## ⚡ Performance Limits

- Max Speed: 250 px/s
- Max Delta: 50 px/update
- Map Bounds: Enforced per map
- Sequence: Must increment

## 🔐 Security

- ✅ Server-side validation
- ✅ Identity verification
- ✅ Speed checks
- ✅ Position delta limits
- ✅ Map boundary enforcement
- ✅ Sequence validation

## 📞 Support

- SpacetimeDB Docs: https://spacetimedb.com/docs
- Discord: https://discord.gg/spacetimedb
- GitHub: https://github.com/clockworklabs/SpacetimeDB

---

**Quick Tip:** Use `grep` with emojis to filter logs by event type!
