# Map Layout & Transition Zones - Visual Guide

## Map Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    GUILDMASTER WORLD                        │
│                                                             │
│   ┌──────────────────┐         ┌──────────────────────┐   │
│   │  starting_area   │◄───────►│    forest_area       │   │
│   │ (Starting Village)│         │   (Dark Forest)      │   │
│   │   1000 x 1000    │         │    1200 x 1200       │   │
│   └──────────────────┘         └──────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Map 1: starting_area (Starting Village)

```
Y
^
1000 ┌─────────────────────────────────────────────────┐
     │                                                 │
     │                                                 │
     │                                                 │
     │                                                 │
 600 │                                          ┌──────┤ ← Transition Zone
     │                                          │//////│   to forest_area
     │                                          │//////│   (X: 950-1000)
     │                                          │//////│   (Y: 400-600)
 500 │  ⭐ Spawn                                │//////│
     │  (100, 500)                              │//////│
     │                                          │//////│
 400 │                                          └──────┤
     │                                                 │
     │                                                 │
     │                                                 │
   0 └─────────────────────────────────────────────────┘
     0                                              1000 → X

Legend:
⭐ = Spawn Point (100, 500)
////// = Transition Zone (50x200 pixels)
→ = Leads to forest_area at (50, 500)
```

### Spawn Points:
- Primary: (100, 500)
- Secondary: (150, 500), (200, 500), (250, 500)

### Transition Zone:
- **Location:** Right edge
- **Coordinates:** X: 950-1000, Y: 400-600
- **Size:** 50 x 200 pixels
- **Destination:** forest_area at (50, 500)

## Map 2: forest_area (Dark Forest)

```
Y
^
1200 ┌─────────────────────────────────────────────────────┐
     │                                                     │
     │                                                     │
     │                                                     │
     │                                                     │
     │                                                     │
 600 ├──────┐                                              │
     │//////│                                              │
     │//////│ ← Transition Zone                           │
     │//////│   to starting_area                          │
 500 │//////│   (X: 0-50)                                 │
     │//////│   (Y: 400-600)                              │
     │//////│                                              │
 400 ├──────┤  ⭐ Spawn                                    │
     │      │  (100, 400)                                 │
     │                                                     │
     │                                                     │
     │                                                     │
   0 └─────────────────────────────────────────────────────┘
     0                                                  1200 → X

Legend:
⭐ = Spawn Point (100, 400)
////// = Transition Zone (50x200 pixels)
← = Leads to starting_area at (900, 500)
```

### Spawn Points:
- Primary: (100, 400)
- Secondary: (150, 400), (200, 400), (250, 400)

### Transition Zone:
- **Location:** Left edge
- **Coordinates:** X: 0-50, Y: 400-600
- **Size:** 50 x 200 pixels
- **Destination:** starting_area at (900, 500)

## Transition Flow

```
                    Player Movement Flow
                    
┌──────────────────┐                    ┌──────────────────┐
│  starting_area   │                    │   forest_area    │
│                  │                    │                  │
│                  │  Player moves to   │                  │
│              [→] │  right edge        │                  │
│              975 │  (X: 950-1000)     │                  │
│              500 │                    │                  │
│                  │  ─────────────────►│  50              │
│                  │  transition_to_map │  500             │
│                  │                    │  [⭐]            │
│                  │                    │                  │
│                  │  Player moves to   │                  │
│                  │  left edge         │                  │
│                  │  (X: 0-50)         │  [←]             │
│                  │                    │  25              │
│              900 │◄─────────────────  │  500             │
│              500 │  transition_to_map │                  │
│              [⭐]│                    │                  │
│                  │                    │                  │
└──────────────────┘                    └──────────────────┘
```

## Coordinate System

Both maps use a standard 2D coordinate system:

```
(0, max_y) ┌─────────────────┐ (max_x, max_y)
           │                 │
           │                 │
           │     MAP         │
           │                 │
           │                 │
     (0, 0)└─────────────────┘ (max_x, 0)
```

### starting_area:
- Origin: (0, 0)
- Max: (1000, 1000)

### forest_area:
- Origin: (0, 0)
- Max: (1200, 1200)

## Movement Validation

```
Player Input → Server Validation → Database Update
                      │
                      ├─ Speed Check (max 250 px/s)
                      ├─ Delta Check (max 50 px/update)
                      ├─ Boundary Check (map-specific)
                      └─ Sequence Check (prevent replay)
```

## Transition Detection

```
Player Position → Check Transition Zones → In Zone?
                                              │
                                              ├─ Yes → Allow transition
                                              └─ No  → Deny transition
```

### Example: starting_area → forest_area

```
Player at (975, 500)
│
├─ Check X: 975 >= 950 AND 975 <= 1000 ✓
├─ Check Y: 500 >= 400 AND 500 <= 600 ✓
│
└─ In transition zone! ✓
   │
   └─ Call transition_to_map("forest_area")
      │
      └─ Teleport to (50, 500) in forest_area
```

## Client Rendering Guide

### Transition Zone Visualization

```gdscript
# Draw transition zones as semi-transparent overlays
func draw_transition_zone(zone):
    var rect = ColorRect.new()
    rect.rect_position = Vector2(zone.x, zone.y)
    rect.rect_size = Vector2(zone.width, zone.height)
    rect.color = Color(1, 1, 0, 0.3)  # Yellow, 30% opacity
    add_child(rect)
    
    # Add arrow indicator
    var arrow = Sprite.new()
    arrow.texture = preload("res://assets/arrow.png")
    arrow.position = rect.rect_position + rect.rect_size / 2
    add_child(arrow)
    
    # Add destination label
    var label = Label.new()
    label.text = "→ " + zone.destination
    label.rect_position = arrow.position + Vector2(0, -30)
    add_child(label)
```

### Map Boundaries

```gdscript
# Enforce client-side boundaries (server also validates)
func clamp_to_map_bounds(position: Vector2) -> Vector2:
    var bounds = get_map_bounds(current_map)
    return Vector2(
        clamp(position.x, bounds.min_x, bounds.max_x),
        clamp(position.y, bounds.min_y, bounds.max_y)
    )

func get_map_bounds(map_id: String) -> Dictionary:
    match map_id:
        "starting_area":
            return {"min_x": 0, "max_x": 1000, "min_y": 0, "max_y": 1000}
        "forest_area":
            return {"min_x": 0, "max_x": 1200, "min_y": 0, "max_y": 1200}
```

## Testing Coordinates

### Test Path 1: starting_area → forest_area

```
1. Start: (100, 500) - Spawn point
2. Move: (500, 500) - Middle of map
3. Move: (900, 500) - Near transition
4. Move: (975, 500) - In transition zone ✓
5. Transition → (50, 500) in forest_area
```

### Test Path 2: forest_area → starting_area

```
1. Start: (50, 500) - Entry point from transition
2. Move: (100, 400) - Spawn point
3. Move: (50, 500) - Near transition
4. Move: (25, 500) - In transition zone ✓
5. Transition → (900, 500) in starting_area
```

## Performance Notes

### Transition Zone Checks

Transition zones are checked efficiently:
- Only when player is near map edges
- Simple rectangle collision detection
- O(n) where n = number of zones per map (typically 1-4)

### Map Boundaries

Boundaries are enforced with simple clamping:
- O(1) operation per coordinate
- No complex collision detection needed
- Validated both client and server-side

## Summary

Your maps are now:
- ✅ Fully defined with exact coordinates
- ✅ Connected via transition zones
- ✅ Validated server-side
- ✅ Ready for client rendering
- ✅ Optimized for performance

Use this diagram as a reference when implementing your Godot client!
