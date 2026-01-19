# Browser-Based Architecture

## Core Principles

### Online-Only Game
- **No offline mode** - Game requires active server connection
- **Browser-based** - Runs in web browser via Godot Web export
- **Server-authoritative** - ALL game logic runs on SpacetimeDB server
- **Client is renderer only** - Client displays server state and sends inputs

## Server Authority (SpacetimeDB)

### Server Handles Everything
- ✅ **Position** - All entity positions calculated on server
- ✅ **Movement** - Server processes movement inputs and updates positions
- ✅ **Collisions** - Server detects and resolves all collisions
- ✅ **Combat** - Damage calculation, hit detection, health management
- ✅ **Interactions** - Item pickup, NPC dialogue, object interactions
- ✅ **Game State** - Inventory, quests, progression, time of day
- ✅ **Map Instancing** - Map loading, transitions, instance management
- ✅ **Physics** - All physics simulation (if any)

### Client Does NOT Handle
- ❌ Position calculation
- ❌ Collision detection
- ❌ Movement logic
- ❌ Combat logic
- ❌ Game state management
- ❌ Physics simulation

## Client Responsibilities

### 1. Input Collection
```csharp
// Client collects input
func _input(event):
    if event.is_action_pressed("move_up"):
        // Send to server - don't move locally
        await client.SendMoveInputAsync(playerId, Vector2.UP, delta);
```

### 2. State Rendering
```csharp
// Client receives state from server and renders it
func OnPlayerStateUpdate(playerData):
    // Just display what server says
    playerSprite.Position = new Vector2(playerData.position_x, playerData.position_y);
    playerSprite.Modulate = new Color(playerData.color_r, playerData.color_g, playerData.color_b);
    healthBar.Value = playerData.health;
```

### 3. Visual Effects
```csharp
// Client can add visual polish (not gameplay affecting)
- Particle effects
- Screen shake
- UI animations
- Sound effects
- Camera movement
```

## Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    Browser Client                           │
│                                                             │
│  ┌──────────────┐                    ┌──────────────────┐  │
│  │   Input      │                    │    Renderer      │  │
│  │  - Keyboard  │                    │  - Sprites       │  │
│  │  - Mouse     │                    │  - UI            │  │
│  │  - Touch     │                    │  - Effects       │  │
│  └──────┬───────┘                    └────────▲─────────┘  │
│         │                                     │             │
│         │ Send Input                   Render State        │
│         │                                     │             │
│  ┌──────▼──────────────────────────────────────┴─────────┐ │
│  │           SpacetimeDB Client SDK                      │ │
│  │  - Send reducers (inputs)                            │ │
│  │  - Receive subscriptions (state)                     │ │
│  └──────┬────────────────────────────────────▲──────────┘ │
└─────────┼────────────────────────────────────┼────────────┘
          │                                    │
    WebSocket + BSATN                    WebSocket + BSATN
          │                                    │
┌─────────▼────────────────────────────────────┴────────────┐
│                  SpacetimeDB Server                       │
│                                                           │
│  ┌─────────────────────────────────────────────────────┐ │
│  │                   Reducers                          │ │
│  │  - input_move(direction, delta)                    │ │
│  │  - attack_target(target_id)                        │ │
│  │  - pickup_item(item_id)                            │ │
│  │  - interact_object(object_id)                      │ │
│  └─────────────────────────────────────────────────────┘ │
│                          │                                │
│                          ▼                                │
│  ┌─────────────────────────────────────────────────────┐ │
│  │                 Game Logic                          │ │
│  │  - Calculate new positions                          │ │
│  │  - Detect collisions (AABB)                        │ │
│  │  - Resolve collisions                               │ │
│  │  - Update health/damage                             │ │
│  │  - Manage inventory                                 │ │
│  │  - Handle map transitions                           │ │
│  └─────────────────────────────────────────────────────┘ │
│                          │                                │
│                          ▼                                │
│  ┌─────────────────────────────────────────────────────┐ │
│  │                   Tables                            │ │
│  │  - Player (position, health, inventory)            │ │
│  │  - NpcState (position, AI state)                   │ │
│  │  - WorldItem (position, type)                      │ │
│  │  - MapInstance (state, players)                    │ │
│  └─────────────────────────────────────────────────────┘ │
│                          │                                │
│                          ▼                                │
│              Broadcast state to all clients               │
└───────────────────────────────────────────────────────────┘
```

## Browser Deployment

### Godot Web Export
```
Client runs as WebAssembly in browser
- No native code execution
- WebSocket connection to SpacetimeDB
- Canvas-based rendering
- Web Audio API for sounds
```

### Connection Requirements
- **Always online** - No offline mode
- **Persistent connection** - WebSocket stays open
- **Reconnection** - Auto-reconnect on connection loss
- **Session management** - Identity-based authentication

## Example: Movement Flow

### ❌ Wrong (Client-side movement)
```csharp
// DON'T DO THIS
func _process(delta):
    var input = Input.get_vector("left", "right", "up", "down");
    player.position += input * speed * delta; // ❌ Client calculates position
```

### ✅ Correct (Server-authoritative)
```csharp
// Client: Send input only
func _process(delta):
    var input = Input.get_vector("left", "right", "up", "down");
    if input != Vector2.ZERO:
        await client.SendMoveInputAsync(playerId, input, delta);

// Server: Calculate position (Rust)
#[spacetimedb(reducer)]
pub fn input_move(ctx: ReducerContext, seq: u32, direction_x: f32, direction_y: f32, delta: f32) {
    let mut player = get_player(&ctx)?;
    
    // Server calculates new position
    let new_x = player.position_x + direction_x * MOVE_SPEED * delta;
    let new_y = player.position_y + direction_y * MOVE_SPEED * delta;
    
    // Server checks collisions
    if !check_collision(new_x, new_y, &ctx.db) {
        player.position_x = new_x;
        player.position_y = new_y;
        ctx.db.player().id().update(player);
    }
}

// Client: Render server position
func OnPlayerUpdate(playerData):
    player.position = Vector2(playerData.position_x, playerData.position_y);
```

## Example: Collision Detection

### Server-Side Only
```rust
#[spacetimedb(reducer)]
pub fn input_move(ctx: ReducerContext, seq: u32, direction_x: f32, direction_y: f32, delta: f32) {
    let mut player = get_player(&ctx)?;
    
    // Calculate desired position
    let new_x = player.position_x + direction_x * MOVE_SPEED * delta;
    let new_y = player.position_y + direction_y * MOVE_SPEED * delta;
    
    // Server checks collisions with AABB
    let player_bounds = AABB {
        x: new_x,
        y: new_y,
        width: PLAYER_WIDTH,
        height: PLAYER_HEIGHT,
    };
    
    // Check against map tiles
    if check_map_collision(&player_bounds, &player.map_id) {
        return; // Collision - don't move
    }
    
    // Check against other entities
    if check_entity_collision(&player_bounds, &ctx.db) {
        return; // Collision - don't move
    }
    
    // No collision - update position
    player.position_x = new_x;
    player.position_y = new_y;
    ctx.db.player().id().update(player);
}
```

## Client Prediction (Optional)

For responsiveness, client can predict movement locally:

```csharp
// Client predicts movement for smooth feel
func _process(delta):
    var input = Input.get_vector("left", "right", "up", "down");
    
    if input != Vector2.ZERO:
        // Send to server
        await client.SendMoveInputAsync(playerId, input, delta);
        
        // Predict locally (will be corrected by server)
        predictedPosition += input * MOVE_SPEED * delta;
        player.position = predictedPosition;
    }

// Server correction
func OnPlayerUpdate(playerData):
    var serverPos = Vector2(playerData.position_x, playerData.position_y);
    var distance = predictedPosition.distance_to(serverPos);
    
    if distance > CORRECTION_THRESHOLD:
        // Server says we're wrong - correct it
        player.position = serverPos;
        predictedPosition = serverPos;
        GD.Print($"[SYNC] Correction: {distance}px");
    }
```

## Performance Considerations

### Network Optimization
- **Input batching** - Send multiple inputs per network tick
- **Delta compression** - Only send changed values
- **Subscription filtering** - Only receive relevant entities
- **Spatial partitioning** - Server only sends nearby entities

### Browser Limitations
- **Memory constraints** - Limited compared to native
- **WebSocket latency** - Typically 20-50ms
- **Canvas performance** - Use efficient rendering
- **Audio limitations** - Web Audio API constraints

## Testing in Browser

### Local Testing
```bash
# Export for web
godot --export-release "Web" build/index.html

# Serve locally
python -m http.server 8000 -d build

# Open in browser
open http://localhost:8000
```

### Production Deployment
- Host static files (HTML, WASM, data files)
- Ensure SpacetimeDB server is accessible
- Configure CORS if needed
- Use HTTPS for production

## Key Takeaways

1. **Server is source of truth** - Always
2. **Client sends inputs** - Never calculates game state
3. **Client renders state** - Just displays what server says
4. **All logic on server** - Position, collision, combat, everything
5. **Browser-based** - Runs as WebAssembly in browser
6. **Online-only** - No offline mode, always connected
