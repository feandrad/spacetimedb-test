---
inclusion: always
---

# SpacetimeDB Architecture & Integration

## Core Rules
- **Server Authority**: SpacetimeDB is the single source of truth
- **Client Input Only**: Send inputs via reducers, never move entities directly
- **Server Validation**: All game logic runs on server

## Communication Pattern
```
Client Input → Reducer → Server Logic → State Update → Client Sync
```

## Implementation
```gdscript
# ✅ Correct: Send input to server
func _input(event):
    if event.is_action_pressed("move_up"):
        NetworkManager.send_move_input(Vector2.UP)

# ❌ Wrong: Direct movement
# player.position += Vector2.UP * speed

# Entity sync pattern
func _on_entity_update(entity_data):
    var local_entity = get_entity(entity_data.id)
    if local_entity:
        local_entity.sync_from_server(entity_data)
    else:
        spawn_entity(entity_data)
```

## Reducer Patterns
- `move_player(direction: Vector2)` - movement
- `attack_target(target_id: u64)` - combat  
- `pickup_item(item_id: u64)` - interactions

## Performance
- Use client prediction for responsiveness
- Batch inputs when possible
- Interpolate for smooth corrections