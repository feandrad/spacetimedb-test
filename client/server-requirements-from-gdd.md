# Server Requirements - Extracted from GDD

## Core Architecture

### SpacetimeDB as Server
- **SpacetimeDB é o servidor** - não há servidor web/game separado
- Database executa a lógica da aplicação internamente
- Comunicação via **WebSocket + BSATN** (binário)
- **Servidor é autoritativo** para todo movimento, combate e interações

### Client-Server Communication
```
Client Input → SpacetimeDB Reducer → Server Logic → State Update → Client Sync
```

## Required Tables (SpacetimeDB)

### Player Management
```rust
#[spacetimedb(table)]
pub struct Player {
    #[primarykey]
    pub id: u64,
    pub position: Vector2,
    pub velocity: Vector2,
    pub health: i32,
    pub max_health: i32,
    pub last_update: u64,
    pub map_instance_id: u32,
    pub is_downed: bool,
}
```

### Map System
```rust
#[spacetimedb(table)]
pub struct MapInstance {
    #[primarykey]
    pub id: u32,
    pub key_id: String,        // "core:overworld/farm"
    pub state: InstanceState,  // Cold/Warm/Hot
    pub last_activity: u64,
    pub seed: Option<u64>,     // For procedural maps
}

#[spacetimedb(table)]
pub struct MapConnection {
    pub source_map: String,
    pub target_map: String,
    pub transition_shape: String, // Serialized shape data
    pub entry_points: String,     // Serialized Vector2 array
}
```

### Combat System
```rust
#[spacetimedb(table)]
pub struct CombatEvent {
    #[primarykey]
    pub id: u64,
    pub event_type: String,    // "hit", "heal", "dodge"
    pub source_id: u64,
    pub target_id: u64,
    pub damage: i32,
    pub timestamp: u64,
}

#[spacetimedb(table)]
pub struct NpcState {
    #[primarykey]
    pub id: u64,
    pub position: Vector2,
    pub health: i32,
    pub ai_state: String,      // "idle", "alert", "chasing"
    pub target_player: Option<u64>,
    pub map_instance_id: u32,
    pub aggro_level: i32,
}
```

### Inventory & Items
```rust
#[spacetimedb(table)]
pub struct PlayerInventory {
    #[primarykey]
    pub player_id: u64,
    pub equipped_weapon: Option<String>, // item key_id
    pub equipped_tool: Option<String>,
    pub items: String,                   // Serialized inventory data
}

#[spacetimedb(table)]
pub struct WorldItem {
    #[primarykey]
    pub id: u64,
    pub item_key: String,
    pub position: Vector2,
    pub map_instance_id: u32,
    pub spawn_time: u64,
}
```

## Required Reducers

### Movement System
```rust
#[spacetimedb(reducer)]
pub fn input_move(ctx: ReducerContext, seq: u32, direction_x: f32, direction_y: f32, delta_time: f32) {
    // Validate movement
    // Update player position
    // Check collisions
    // Update player table
}

#[spacetimedb(reducer)]
pub fn request_map_transition(ctx: ReducerContext, target_map: String, entry_point: u32) {
    // Validate transition
    // Update player map_instance_id
    // Ensure target map is Hot
    // Emit transition events
}
```

### Combat System
```rust
#[spacetimedb(reducer)]
pub fn attack_target(ctx: ReducerContext, target_id: u64, weapon_type: String) {
    // Validate attack (range, cooldown, weapon)
    // Calculate damage
    // Apply damage to target
    // Create combat event
    // Update health/downed state
}

#[spacetimedb(reducer)]
pub fn revive_player(ctx: ReducerContext, target_player_id: u64) {
    // Validate proximity and state
    // Restore player health
    // Update downed state
}
```

### Interaction System
```rust
#[spacetimedb(reducer)]
pub fn interact_with_object(ctx: ReducerContext, object_type: String, action_type: String) {
    // Validate interaction (proximity, requirements)
    // Execute action logic
    // Update world state
    // Grant items/resources
}

#[spacetimedb(reducer)]
pub fn pickup_item(ctx: ReducerContext, item_id: u64) {
    // Validate proximity
    // Add to player inventory
    // Remove from world
}
```

## Map Instance Management

### Instance States
- **Cold**: No simulation, metadata only
- **Warm**: Pre-loaded, no active simulation (TTL: 60s)
- **Hot**: Active simulation with players present

### State Transitions
```rust
#[spacetimedb(reducer)]
pub fn update_map_instances(ctx: ReducerContext) {
    // Check player presence in each instance
    // Transition Hot → Warm when last player leaves
    // Transition Warm → Cold when TTL expires
    // Transition Cold → Warm when prefetch needed
}
```

### Daily Reset System
```rust
#[spacetimedb(reducer)]
pub fn daily_reset(ctx: ReducerContext) {
    // Reset procedural dungeons/caves
    // Clear temporary world items
    // Reset NPC states
    // Maintain player progress
}
```

## Registry System

### Resource Registration
```rust
#[spacetimedb(table)]
pub struct ResourceRegistry {
    #[primarykey]
    pub id: u32,
    pub key_id: String,        // "core:overworld/farm"
    pub resource_type: String, // "map", "item", "npc"
    pub data: String,          // Serialized resource data
}
```

### ID Generation
- Generate `id (u32)` from `key_id (String)` hash
- Maintain bidirectional mapping
- Handle hash collisions with disambiguation
- Sync registry with clients on connection

## Anti-Griefing Measures

### Rate Limiting
```rust
#[spacetimedb(table)]
pub struct PlayerRateLimit {
    #[primarykey]
    pub player_id: u64,
    pub last_action_time: u64,
    pub action_count: u32,
}

// Enforce 10 actions per second limit
```

### Validation Rules
- No friendly fire between players
- No body-blocking between players
- Interaction cooldowns (500ms)
- Idempotent reducers using `player_id + seq`
- Server-side collision validation

## Performance Considerations

### Batching & Optimization
- Group multiple inputs per frame
- Prioritize critical inputs (movement, combat)
- Use efficient serialization (BSATN)
- Implement spatial partitioning for collision detection

### Subscription Filtering
- Filter subscriptions by `map_instance_id`
- Only send relevant updates to each client
- Use incremental updates (diffs) when possible

## Multiplayer Features

### Server Management
- Support up to 4 players per server
- Official hosted servers
- Self-hosted server kit
- Save data tied to server, not host

### Party System
```rust
#[spacetimedb(table)]
pub struct Party {
    #[primarykey]
    pub id: u64,
    pub leader_id: u64,
    pub members: String,       // Serialized player ID array
    pub shared_loot: bool,
}
```

## Time & Calendar System

### Game Time Management
```rust
#[spacetimedb(table)]
pub struct GameTime {
    #[primarykey]
    pub server_id: u32,
    pub current_day: u32,
    pub current_time: f32,     // 0.0 = midnight, 0.5 = noon
    pub time_scale: f32,
    pub paused: bool,          // Pause when no players online
}
```

### Daily Cycle Events
- Morning: Special vendor appears
- Day: Exploration and resource gathering
- Dusk: Visual transition warning
- Night: Tavern interactions (until midnight)

## Tavern Management

### NPC Interactions
```rust
#[spacetimedb(table)]
pub struct TavernNpc {
    #[primarykey]
    pub id: u64,
    pub npc_type: String,
    pub current_request: Option<String>,
    pub loyalty_level: i32,
    pub last_interaction: u64,
}

#[spacetimedb(reducer)]
pub fn serve_npc_request(ctx: ReducerContext, npc_id: u64, item_offered: String) {
    // Validate item availability
    // Calculate satisfaction/reputation
    // Update loyalty and unlock content
}
```

## Security & Validation

### Input Validation
- Validate all movement within map bounds
- Check action prerequisites (tools, proximity)
- Verify resource availability before consumption
- Prevent impossible state transitions

### Cheat Prevention
- Server-side physics simulation
- Validate all client predictions
- Detect impossible movements/actions
- Implement rollback for corrections

## Deployment Requirements

### SpacetimeDB Setup
- Configure database schema
- Set up reducer endpoints
- Configure subscription filters
- Implement backup/restore procedures

### Monitoring & Logging
- Track player actions and performance
- Monitor server resource usage
- Log security violations
- Implement crash recovery