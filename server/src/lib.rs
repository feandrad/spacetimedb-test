use crate::map::get_or_create_map_instance;
use crate::map::map_template;
use crate::map::map_transition;
use crate::map::STARTING_MAP;
use spacetimedb::{reducer, table, Identity, ReducerContext, Table, Timestamp};

pub mod map;
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
        STARTING_MAP.to_string()
    };

    // Garante que a instância existe
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

    // Se já existe por identidade, só atualiza o mapa e retorna
    if let Some(p) = ctx.db.player().iter().find(|p| p.identity == identity) {
        let _ = map::update_map_state(ctx, &p.current_map_id); // Garante sync
        return Ok(());
    }

    let display = username_display.trim().to_string();
    let canonical = display.to_lowercase();

    if display.is_empty() || canonical.len() < 3 || canonical.len() > 16 {
        return Err("Invalid username length".into());
    }

    // Lógica de Reclaim (Recuperar usuário antigo)
    if let Some(existing_player) = ctx.db.player().iter().find(|p| p.username_canonical == canonical) {
        let mut p = existing_player.clone();
        ctx.db.player().id().delete(&p.id);

        p.identity = identity;

        // Lógica de Reclaim (Recuperar usuário antigo)
        // VERIFICAR: O mapa onde ele estava ainda é válido?
        let map_valid = map::get_or_create_map_instance(ctx, &p.current_map_id).is_some();
        
        let mut position_valid = false;
        if map_valid {
            let (min_x, max_x, min_y, max_y) = map::get_map_bounds_from_db(ctx, &p.current_map_id);
            if p.position_x >= min_x && p.position_x <= max_x && 
               p.position_y >= min_y && p.position_y <= max_y {
                position_valid = true;
            }
        }

        if position_valid {
             log::info!("🔙 Reclaim: Player {} restored at {} ({}, {})", 
                p.username_display, p.current_map_id, p.position_x, p.position_y);
        } else {
            // Fallback: Reset para Spawn
             log::warn!("⚠️ Reclaim: Player {} had invalid pos/map. Resetting to spawn.", p.username_display);
            let (spawn_x, spawn_y) = map::get_spawn_point(ctx, STARTING_MAP);
            p.current_map_id = STARTING_MAP.to_string();
            p.position_x = spawn_x;
            p.position_y = spawn_y;
        }

        ctx.db.player().insert(p);

        // --- NOVO: Avisa o mapa que chegou gente ---
        let _ = map::update_map_state(ctx, STARTING_MAP);

        return Ok(());
    }

    // Novo Player
    let (spawn_x, spawn_y) = map::get_spawn_point(ctx, STARTING_MAP);
    let new_player = Player {
        id: generate_player_id(ctx),
        identity,
        username_canonical: canonical,
        username_display: display,
        position_x: spawn_x,
        position_y: spawn_y,
        velocity_x: 0.0,
        velocity_y: 0.0,
        current_map_id: STARTING_MAP.to_string(),
        health: 100.0,
        max_health: 100.0,
        is_downed: false,
        last_input_sequence: 0,
        last_transition_time: ctx.timestamp,
    };

    ctx.db.player().insert(new_player);
    let _ = map::update_map_state(ctx, STARTING_MAP);

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

#[reducer]
pub fn get_map_data(ctx: &ReducerContext, map_id: String) -> Result<(), String> {
    let identity = ctx.sender;

    if let Some(_player) = ctx.db.player().iter().find(|p| p.identity == identity) {
        if let Some(template) = ctx.db.map_template().name().find(map_id.clone()) {
            log::info!("📍 Map Info - Name: {}, Size: {}x{}",
                   template.name, template.width, template.height);
        } else {
            log::warn!("⚠️ Mapa '{}' não encontrado nos templates!", map_id);
        }

        // Log players
        let count = ctx.db.player().iter().filter(|p| p.current_map_id == map_id).count();
        log::info!("👥 Players no mapa {}: {}", map_id, count);
    } else {
        return Err("Player not authenticated".to_string());
    }

    Ok(())
}

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

#[reducer]
pub fn health_check(_ctx: &ReducerContext) {
    log::info!("Guildmaster server health check - OK");
}

#[reducer]
pub fn test_message(_ctx: &ReducerContext, message: String) {
    log::info!("Test message: {}", message);
}
