use spacetimedb::{table, reducer, Identity, ReducerContext, Table, Timestamp};
use crate::map::get_or_create_map_instance;

pub mod map;
use crate::map::map_transition;
pub mod movement;
pub mod combat;
pub mod character;
pub mod inventory;
pub mod resource_registry;

#[table(name = player, public)]
#[derive(Clone)]
pub struct Player {
    #[primary_key]
    pub id: u32,
    #[unique]
    pub username_canonical: String,
    pub username_display: String,
    #[unique]
    pub identity: Identity,

    pub position_x: f32,
    pub position_y: f32,
    pub velocity_x: f32,
    pub velocity_y: f32,
    pub current_map_id: String,

    pub health: f32,
    pub max_health: f32,
    pub is_downed: bool,
    pub last_input_sequence: u32,
    pub last_transition_time: Timestamp,
}

// ============================================================================
// LIFECYCLE HANDLERS
// ============================================================================

/// Called when a client connects to the database
#[reducer(client_connected)]
pub fn on_connect(ctx: &ReducerContext) {
    log::info!("🔌 Client connected: {:?}", ctx.sender);

    // 1. IDENTIFICAÇÃO E GARANTIA DE INFRA (A parte que faltava)
    let map_to_init = if let Some(player) = ctx.db.player().iter().find(|p| p.identity == ctx.sender) {
        log::info!("👤 Existing player reconnected: {}, Map: {}",
                   player.username_display, player.current_map_id);
        player.current_map_id.clone()
    } else {
        log::info!("🆕 New client connected. Preparing starting_area.");
        "starting_area".to_string()
    };

    // 2. CRIAÇÃO DA INSTÂNCIA: Aqui o servidor assume a autoridade
    // e popula a tabela map_instance antes do cliente terminar o subscribe.
    get_or_create_map_instance(ctx, &map_to_init);

    // 3. Auto-init map transitions (Manutenção do seu código original)
    if ctx.db.map_transition().iter().count() == 0 {
        log::info!("🔧 DB Empty: Auto-initializing map transitions...");
        map::init_map_transitions(ctx);
    }
}

/// Called when a client disconnects from the database
#[reducer(client_disconnected)]
pub fn on_disconnect(ctx: &ReducerContext) {
    log::info!("🔌 Client disconnected: {:?}", ctx.sender);

    // Log player info if they had a player
    if let Some(player) = ctx.db.player().iter().find(|p| p.identity == ctx.sender) {
        log::info!("👋 Player {} ({}) disconnected from map: {}",
                   player.id, player.username_display, player.current_map_id);

        // Note: We don't delete the player on disconnect
        // Players persist across sessions
    }
}

// ============================================================================
// PLAYER AUTHENTICATION AND REGISTRATION
// ============================================================================
#[reducer]
pub fn register_player(ctx: &ReducerContext, username_display: String) -> Result<(), String> {
    let identity = ctx.sender;

    // idempotência por identity (você escolhe manter assim)
    if let Some(_existing) = ctx.db.player().iter().find(|p| p.identity == identity) {
        return Ok(());
    }

    let display = username_display.trim().to_string();
    if display.is_empty() {
        return Err("Username cannot be empty".into());
    }

    // canonical = lowercase + trim
    let canonical = display.to_lowercase();

    // validações simples
    if canonical.len() < 3 || canonical.len() > 16 {
        return Err("Username must be between 3 and 16 characters".into());
    }
    if !canonical.chars().all(|c| c.is_ascii_alphanumeric() || c == '_' || c == '-') {
        return Err("Username has invalid characters".into());
    }

    // CHECK FOR EXISTING USER TO RECLAIM
    if let Some(existing_player) = ctx.db.player().iter().find(|p| p.username_canonical == canonical) {
        log::info!("🔄 Reclaiming player {} (ID: {}) for new identity {:?}", display, existing_player.id, identity);
        
        let mut p = existing_player.clone();
        
        // Remove old entry by ID (Primary Key)
        ctx.db.player().id().delete(&p.id);
        
        // Update identity
        p.identity = identity;
        
        // Insert new entry
        ctx.db.player().insert(p);
        
        return Ok(());
    }

    let player_id = generate_player_id(ctx);

    let new_player = Player {
        id: player_id,
        identity,
        username_canonical: canonical,
        username_display: display,
        position_x: 100.0,
        position_y: 500.0,
        velocity_x: 0.0,
        velocity_y: 0.0,
        current_map_id: "starting_area".to_string(),
        health: 100.0,
        max_health: 100.0,
        is_downed: false,
        last_input_sequence: 0,
        last_transition_time: ctx.timestamp,
    };

    ctx.db.player().insert(new_player);
    Ok(())
}

#[reducer]
pub fn get_player_info(
    ctx: &ReducerContext,
) -> Result<(), String> {
    let identity = ctx.sender;
    
    if let Some(player) = ctx.db.player().iter().find(|p| p.identity == identity) {
        log::info!("✅ Player authenticated - ID: {}, Username: {}, Map: {}, Position: ({:.1}, {:.1})", 
                   player.id, player.username_display, player.current_map_id,
                   player.position_x, player.position_y);
    } else {
        log::warn!("❌ No player found for identity {:?}", identity);
    }
    
    Ok(())
}

// Get map data for rendering
#[reducer]
pub fn get_map_data(
    ctx: &ReducerContext,
    map_id: String,
) -> Result<(), String> {
    let identity = ctx.sender;
    
    // Verify player exists
    if let Some(player) = ctx.db.player().iter().find(|p| p.identity == identity) {
        log::info!("✅ Map data requested - Player: {}, Map: {}", player.username_display, map_id);
        
        // Log map metadata for client
        let map_info = get_map_metadata(&map_id);
        log::info!("📍 Map Info - ID: {}, Name: {}, Size: {}x{}, Spawn: ({:.1}, {:.1})", 
                   map_info.id, map_info.name, map_info.width, map_info.height,
                   map_info.spawn_x, map_info.spawn_y);
        
        // Log players in this map
        let players_in_map: Vec<Player> = ctx.db.player().iter()
            .filter(|p| p.current_map_id == map_id)
            .collect();
        
        log::info!("👥 Players in map {}: {}", map_id, players_in_map.len());
        for p in &players_in_map {
            log::info!("  - {} (ID: {}) at ({:.1}, {:.1})", 
                       p.username_display, p.id, p.position_x, p.position_y);
        }
    } else {
        log::warn!("❌ Unauthorized map data request from identity {:?}", identity);
        return Err("Player not authenticated".to_string());
    }
    
    Ok(())
}

// Helper: Generate unique player ID
fn generate_player_id(ctx: &ReducerContext) -> u32 {
    use std::collections::hash_map::DefaultHasher;
    use std::hash::{Hash, Hasher};
    
    // Count existing players and use as seed
    let player_count = ctx.db.player().iter().count();
    
    let mut hasher = DefaultHasher::new();
    player_count.hash(&mut hasher);
    // Add some randomness from the context
    ctx.sender.to_hex().hash(&mut hasher);
    
    let base_hash = (hasher.finish() % u32::MAX as u64) as u32;
    
    // Check for collisions
    let mut disambiguation = 0u32;
    loop {
        let final_id = base_hash.wrapping_add(disambiguation);
        if ctx.db.player().id().find(&final_id).is_none() {
            return final_id;
        }
        disambiguation += 1;
    }
}

// Map metadata structure
struct MapMetadata {
    id: String,
    name: String,
    width: u32,
    height: u32,
    spawn_x: f32,
    spawn_y: f32,
}

// Helper: Get map metadata
fn get_map_metadata(map_id: &str) -> MapMetadata {
    match map_id {
        "starting_area" => MapMetadata {
            id: "starting_area".to_string(),
            name: "Starting Village".to_string(),
            width: 1000,
            height: 1000,
            spawn_x: 100.0,
            spawn_y: 500.0,
        },
        "forest_area" => MapMetadata {
            id: "forest_area".to_string(),
            name: "Dark Forest".to_string(),
            width: 1200,
            height: 1200,
            spawn_x: 100.0,
            spawn_y: 400.0,
        },
        _ => MapMetadata {
            id: "unknown".to_string(),
            name: "Unknown Area".to_string(),
            width: 800,
            height: 600,
            spawn_x: 0.0,
            spawn_y: 0.0,
        },
    }
}

// Health check reducer
#[reducer]
pub fn health_check(_ctx: &ReducerContext) {
    log::info!("Guildmaster server health check - OK");
}

// Test message reducer
#[reducer]
pub fn test_message(_ctx: &ReducerContext, message: String) {
    log::info!("Test message: {}", message);
}
