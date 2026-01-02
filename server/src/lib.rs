use spacetimedb::{spacetimedb, Identity, ReducerContext};

// Re-export modules
pub mod player;
pub mod movement;
pub mod combat;
pub mod map;
pub mod inventory;

// Player table - simplified without serde derives for Identity
#[spacetimedb(table)]
#[derive(Clone)]
pub struct Player {
    #[spacetimedb(primary_key)]
    pub id: u32,
    pub identity: Identity,
    pub position_x: f32,
    pub position_y: f32,
    pub velocity_x: f32,
    pub velocity_y: f32,
    pub current_map_id: String,
    pub health: f32,
    pub max_health: f32,
    pub is_downed: bool,
    pub equipped_weapon: String,
    pub last_input_sequence: u32,
}

// Initialize a new player when they connect
#[spacetimedb(reducer)]
pub fn create_player(ctx: ReducerContext) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Check if player already exists
    let existing_players: Vec<Player> = Player::filter_by_identity(&identity).collect();
    if !existing_players.is_empty() {
        log::info!("Player already exists for identity: {:?}", identity);
        return Ok(());
    }

    // Create new player
    let player = Player {
        id: generate_player_id(),
        identity,
        position_x: 0.0,
        position_y: 0.0,
        velocity_x: 0.0,
        velocity_y: 0.0,
        current_map_id: "starting_area".to_string(),
        health: 100.0,
        max_health: 100.0,
        is_downed: false,
        equipped_weapon: "".to_string(),
        last_input_sequence: 0,
    };

    Player::insert(player.clone());
    log::info!("Created new player with ID: {}", player.id);
    
    Ok(())
}

// Simple ID generation - in production, use a more robust method
fn generate_player_id() -> u32 {
    use std::collections::hash_map::DefaultHasher;
    use std::hash::{Hash, Hasher};
    
    let mut hasher = DefaultHasher::new();
    std::time::SystemTime::now().hash(&mut hasher);
    (hasher.finish() % u32::MAX as u64) as u32
}

// Health check reducer
#[spacetimedb(reducer)]
pub fn health_check(_ctx: ReducerContext) -> Result<(), Box<dyn std::error::Error>> {
    log::info!("Server health check - OK");
    Ok(())
}