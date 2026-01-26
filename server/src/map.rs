use spacetimedb::{ReducerContext, Table, reducer};
use crate::{Player, player};
use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};

#[spacetimedb::table(name = map_instance, public)]
#[derive(Clone)]
pub struct MapInstance {
    #[primary_key]
    pub id: u32,
    #[unique]
    pub key_id: String,
    pub state: String,
    pub player_count: u32,
    pub metadata: String,
}

#[spacetimedb::table(name = map_transition, public)]
#[derive(Clone)]
pub struct MapTransition {
    #[primary_key]
    pub id: u32,
    pub map_id: String,
    pub x: f32,
    pub y: f32,
    pub width: f32,
    pub height: f32,
    pub dest_map_id: String,
    pub dest_x: f32,
    pub dest_y: f32,
}

#[reducer]
pub fn init_map_transitions(ctx: &ReducerContext) {

    let transitions = vec![
        MapTransition {
            id: 1, map_id: "starting_area".to_string(),
            x: 950.0, y: 400.0, width: 50.0, height: 200.0,
            dest_map_id: "forest_area".to_string(), dest_x: 50.0, dest_y: 500.0,
        },
        MapTransition {
            id: 2, map_id: "forest_area".to_string(),
            x: 0.0, y: 400.0, width: 50.0, height: 200.0,
            dest_map_id: "starting_area".to_string(), dest_x: 900.0, dest_y: 500.0,
        }
    ];

    for t in transitions { ctx.db.map_transition().insert(t); }
    log::info!("✅ Regras de transição carregadas. Instâncias sob demanda.");
}

// --- INFRAESTRUTURA AUTORITÁRIA ---

fn get_map_metadata(map_id: &str) -> String {
    match map_id {
        "starting_area" => "1800x800".to_string(),
        "forest_area" => "1200x2000".to_string(),
        _ => panic!("❌ FATAL: Mapa não registrado: '{}'", map_id),
    }
}

pub fn get_or_create_map_instance(ctx: &ReducerContext, key_id: &str) -> MapInstance {
    if let Some(instance) = ctx.db.map_instance().key_id().find(key_id.to_string()) {
        instance
    } else {
        let new_instance = MapInstance {
            id: generate_map_instance_id(ctx, key_id),
            key_id: key_id.to_string(),
            state: "Hot".to_string(),
            player_count: 0,
            metadata: get_map_metadata(key_id),
        };
        ctx.db.map_instance().insert(new_instance.clone());
        log::info!("⚡ INFRA: Instância '{}' criada on-demand.", key_id);
        new_instance
    }
}

pub fn get_map_bounds_from_db(ctx: &ReducerContext, map_id: &str) -> (f32, f32, f32, f32) {
    let inst = get_or_create_map_instance(ctx, map_id);
    let parts: Vec<&str> = inst.metadata.split('x').collect();

    if parts.len() != 2 {
        panic!("❌ FATAL: Metadata corrupto: '{}'", inst.metadata);
    }

    let w = parts[0].parse::<f32>().expect("❌ FATAL: Width inválido");
    let h = parts[1].parse::<f32>().expect("❌ FATAL: Height inválido");

    (0.0, w, 0.0, h)
}

// --- REDUCERS DE ESTADO ---

#[reducer]
pub fn spawn_player_at_map(ctx: &ReducerContext, player_id: u32, map_id: String) -> Result<(), String> {
    let identity = ctx.sender;
    let player = ctx.db.player().id().find(&player_id).ok_or("Player not found")?;

    if player.identity != identity { return Err("Unauthorized".to_string()); }
    if !is_valid_map(&map_id) { return Err("Invalid map".to_string()); }

    // Garante que o mapa exista antes do player entrar
    get_or_create_map_instance(ctx, &map_id);

    let (spawn_x, spawn_y) = get_spawn_point(&map_id, player_id);
    let mut updated_player = player.clone();
    updated_player.current_map_id = map_id.clone();
    updated_player.position_x = spawn_x;
    updated_player.position_y = spawn_y;
    updated_player.velocity_x = 0.0;
    updated_player.velocity_y = 0.0;

    ctx.db.player().id().update(updated_player);
    update_map_state(ctx, &map_id)?;

    Ok(())
}

// --- HELPERS ---

fn generate_map_instance_id(_ctx: &ReducerContext, key_id: &str) -> u32 {
    let mut hasher = DefaultHasher::new();
    key_id.hash(&mut hasher);
    (hasher.finish() % u32::MAX as u64) as u32
}

fn count_players_in_map(ctx: &ReducerContext, key_id: &str) -> u32 {
    ctx.db.player().iter().filter(|p| p.current_map_id == key_id).count() as u32
}

fn is_valid_map(map_id: &str) -> bool {
    matches!(map_id, "starting_area" | "forest_area")
}

fn get_spawn_point(map_id: &str, player_index: u32) -> (f32, f32) {
    let spawn_points = match map_id {
        "starting_area" => vec![(100.0, 500.0), (150.0, 500.0)],
        "forest_area" => vec![(100.0, 400.0), (150.0, 400.0)],
        _ => vec![(0.0, 0.0)],
    };
    spawn_points[(player_index as usize) % spawn_points.len()]
}

pub fn update_map_state(ctx: &ReducerContext, key_id: &str) -> Result<(), String> {
    let mut map_instance = get_or_create_map_instance(ctx, key_id);
    let player_count = count_players_in_map(ctx, key_id);

    map_instance.player_count = player_count;
    map_instance.state = if player_count > 0 { "Hot".to_string() } else { "Cold".to_string() };

    ctx.db.map_instance().id().update(map_instance);
    Ok(())
}

pub fn check_map_transition(ctx: &ReducerContext, player_id: u32) -> Result<(), String> {
    let player = ctx.db.player().id().find(&player_id).ok_or("Player not found")?;

    // Busca transições do mapa atual (precisa da trait em escopo)
    use crate::map::map_transition;
    let transitions: Vec<MapTransition> = ctx.db.map_transition().iter()
        .filter(|t| t.map_id == player.current_map_id)
        .collect();

    for t in transitions {
        // Detecção: Centro do player (x, y) dentro do retângulo marrom
        if player.position_x >= t.x && player.position_x <= (t.x + t.width) &&
            player.position_y >= t.y && player.position_y <= (t.y + t.height)
        {
            log::info!("🔄 [TRANSITION] Player {}: {} -> {}",
                      player.username_display, player.current_map_id, t.dest_map_id);

            let old_map = player.current_map_id.clone();
            let mut updated_player = player.clone();

            // Garante que o mapa destino exista no SQL
            get_or_create_map_instance(ctx, &t.dest_map_id);

            updated_player.current_map_id = t.dest_map_id.clone();
            updated_player.position_x = t.dest_x;
            updated_player.position_y = t.dest_y;
            updated_player.velocity_x = 0.0;
            updated_player.velocity_y = 0.0;

            ctx.db.player().id().update(updated_player);

            // Atualiza população de ambos os mapas
            update_map_state(ctx, &old_map)?;
            update_map_state(ctx, &t.dest_map_id)?;
            break;
        }
    }
    Ok(())
}