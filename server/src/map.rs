use crate::player;
use include_dir::{include_dir, Dir};
use serde::Deserialize;
use spacetimedb::{reducer, table, ReducerContext, Table};
use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};
use std::str::FromStr;

pub const STARTING_MAP: &str = "tavern_outside";
pub const TILE_SIZE_PX: u32 = 8;
pub const TILE_SIZE: f32 = TILE_SIZE_PX as f32;



#[derive(Deserialize)]
struct MapConfig {
    spawn_x: f32,
    spawn_y: f32,
}

static MAPS_DIR: Dir = include_dir!("src/maps");
static TRANSITIONS_DIR: Dir = include_dir!("src/transitions");

#[table(accessor = map_template, public)]
pub struct MapTemplate {
    #[primary_key]
    pub name: String,        // Ex: "tavern_road"
    pub width: u32,
    pub height: u32,
    pub tile_data: Vec<u32>,
    pub spawn_x: f32,
    pub spawn_y: f32,
}

#[table(accessor = world_mutation, public)]
pub struct WorldMutation {
    #[primary_key]
    pub id: u64,
    pub instance_id: u32,
    pub x: u32,
    pub y: u32,
    pub new_tile_id: u32,
}

#[table(accessor = map_instance, public)]
#[derive(Clone)]
pub struct MapInstance {
    #[primary_key]
    pub id: u32,
    #[unique]
    pub key_id: String,
    pub state: String,
    pub player_count: u32,
    pub template_name: String,
}

#[table(accessor = map_transition, public)]
#[derive(Clone, Deserialize)]
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

#[reducer(init)]
pub fn init(ctx: &ReducerContext) {
    log::info!("🚀 INIT: Processando mapas...");

    for file in MAPS_DIR.files() {
        let filename = file.path().file_name().unwrap().to_str().unwrap();
        if !filename.ends_with(".csv") { continue; }

        let template_name = filename.replace(".csv", "").replace("..", "").trim().to_lowercase();

        let content = file.contents_utf8().expect("Erro Crítico: UTF-8 inválido");
        let lines: Vec<&str> = content.lines().map(|l| l.trim()).filter(|l| !l.is_empty()).collect();

        if lines.is_empty() {
            log::error!("❌ ERRO: O arquivo '{}.csv' está vazio e foi IGNORADO.", template_name);
            continue;
        }

        let height = lines.len() as u32;
        let width = lines[0].split(',').map(|s| s.trim()).filter(|s| !s.is_empty()).count() as u32;

        if let Some(config_file) = MAPS_DIR.get_file(format!("{}.json", template_name)) {
             let config_content = config_file.contents_utf8().expect("Erro Crítico: UTF-8 inválido no JSON do mapa");
             match serde_json::from_str::<MapConfig>(config_content) {
                 Ok(config) => {
                     let tile_data: Vec<u32> = lines.iter()
                        .flat_map(|line| line.split(',').map(|s| s.trim()).filter(|s| !s.is_empty()))
                        .filter_map(|s| u32::from_str(s).ok())
                        .collect();

                     ctx.db.map_template().insert(MapTemplate {
                         name: template_name.clone(),
                         width, height, tile_data, 
                         spawn_x: config.spawn_x, 
                         spawn_y: config.spawn_y,
                     });
                     log::info!("✅ Mapa carregado: '{}' | Spawn: ({}, {})", template_name, config.spawn_x, config.spawn_y);
                 },
                 Err(e) => {
                      log::error!("❌ ERRO ao parsear config '{}': {}", template_name, e);
                 }
             }
        } else {
             log::error!("⛔ CONFIG REJEITADA: '{}' não possui arquivo .json de configuração.", template_name);
        }
    }

    init_map_transitions(ctx);
}

#[reducer]
pub fn init_map_transitions(ctx: &ReducerContext) {
    let mut total_transitions = 0;

    for file in TRANSITIONS_DIR.files() {
        let filename = file.path().file_name().unwrap().to_str().unwrap();
        if !filename.ends_with(".json") { continue; }

        let content = file.contents_utf8().expect("Erro Crítico: UTF-8 inválido no JSON de transições");
        
        match serde_json::from_str::<Vec<MapTransition>>(content) {
            Ok(transitions) => {
                for t in transitions {
                     ctx.db.map_transition().insert(t);
                     total_transitions += 1;
                }
                log::info!("✅ Transições carregadas de '{}'.", filename);
            },
            Err(e) => {
                log::error!("❌ ERRO ao parsear '{}': {}", filename, e);
            }
        }
    }

    log::info!("✅ Total de {} regras de transição carregadas.", total_transitions);
}

#[reducer]
pub fn replace_all_templates(ctx: &ReducerContext, new_templates: Vec<MapTemplate>) -> Result<(), String> {
    // Limpa a tabela atual
    for template in ctx.db.map_template().iter() {
        ctx.db.map_template().name().delete(&template.name);
    }
    // Insere os novos templates vindos do deployer
    for t in new_templates {
        ctx.db.map_template().insert(t);
    }
    log::info!("REDEPLOY: {} templates carregados do zero.", ctx.db.map_template().count());
    Ok(())
}

pub fn get_or_create_map_instance(ctx: &ReducerContext, key_id: &str) -> Option<MapInstance> {
    // 1. Tenta buscar instância existente
    if let Some(instance) = ctx.db.map_instance().key_id().find(key_id.to_string()) {
        return Some(instance);
    }

    // 2. Tenta buscar o template
    let template_opt = ctx.db.map_template().name().find(key_id.to_string());

    match template_opt {
        Some(t) => {
            let new_instance = MapInstance {
                id: generate_map_instance_id(key_id),
                key_id: key_id.to_string(),
                state: "Hot".to_string(),
                player_count: 0,
                template_name: t.name,
            };
            ctx.db.map_instance().insert(new_instance.clone());
            log::info!("✨ Instância '{}' criada.", key_id);
            Some(new_instance)
        },
        None => {
            // AQUI ESTAVA O ERRO (unwrap/expect): Agora retornamos None sem crashar.
            log::warn!("⚠️ ALERTA: Mapa '{}' não encontrado nos templates.", key_id);
            None
        }
    }
}

pub fn get_map_bounds_from_db(ctx: &ReducerContext, map_id: &str) -> (f32, f32, f32, f32) {
    if let Some(template) = ctx.db.map_template().name().find(map_id.to_string()) {
        let w = (template.width * 8) as f32;
        let h = (template.height * 8) as f32;
        return (0.0, w, 0.0, h);
    }
    // Retorna limites zerados em vez de dar CRASH no servidor
    log::warn!("⚠️ BOUNDS: Template '{}' não achado. Retornando (0,0,0,0).", map_id);
    (0.0, 0.0, 0.0, 0.0)
}

#[reducer]
pub fn spawn_player_at_map(ctx: &ReducerContext, player_id: u32, map_id: String) -> Result<(), String> {
    let identity = ctx.sender();
    let player = ctx.db.player().id().find(&player_id).ok_or("Player not found")?;

    if player.identity != identity { return Err("Unauthorized".to_string()); }

    // --- LÓGICA DE FALLBACK NO REGISTRO ---
    // Se o mapa pedido existe, usa ele. Se não, força o STARTING_MAP.
    let final_map_id = if get_or_create_map_instance(ctx, &map_id).is_some() {
        map_id
    } else {
        log::warn!("⚠️ Mapa '{}' inválido. Movendo player para fallback '{}'.", map_id, STARTING_MAP);
        get_or_create_map_instance(ctx, STARTING_MAP);
        STARTING_MAP.to_string()
    };

    let (spawn_x, spawn_y) = get_spawn_point(ctx, &final_map_id);

    let mut updated_player = player.clone();
    updated_player.current_map_id = final_map_id.clone();
    updated_player.position_x = spawn_x;
    updated_player.position_y = spawn_y;
    updated_player.velocity_x = 0.0;
    updated_player.velocity_y = 0.0;

    ctx.db.player().id().update(updated_player);
    update_map_state(ctx, &final_map_id)?;

    Ok(())
}

fn generate_map_instance_id(key_id: &str) -> u32 {
    let mut hasher = DefaultHasher::new();
    key_id.hash(&mut hasher);
    (hasher.finish() % u32::MAX as u64) as u32
}

fn count_players_in_map(ctx: &ReducerContext, key_id: &str) -> u32 {
    ctx.db.player().iter().filter(|p| p.current_map_id == key_id).count() as u32
}

pub fn get_spawn_point(ctx: &ReducerContext, map_id: &str) -> (f32, f32) {
    // 1. Tenta o mapa solicitado
    if let Some(template) = ctx.db.map_template().name().find(map_id.to_string()) {
        return (template.spawn_x, template.spawn_y);
    }

    // 2. Fallback para o STARTING_MAP
    log::warn!("⚠️ Spawn não encontrado para '{}'. Redirecionando para '{}'.", map_id, STARTING_MAP);

    if let Some(start_template) = ctx.db.map_template().name().find(STARTING_MAP.to_string()) {
        return (start_template.spawn_x, start_template.spawn_y);
    }

    // 3. Pânico se nem o mapa inicial existir
    panic!("❌ ERRO CRÍTICO: Nem o mapa '{}' nem o STARTING_MAP '{}' existem!", map_id, STARTING_MAP);
}

pub fn update_map_state(ctx: &ReducerContext, key_id: &str) -> Result<(), String> {
    // Só atualiza se a instância existir (is_some)
    if let Some(mut map_instance) = get_or_create_map_instance(ctx, key_id) {
        let player_count = count_players_in_map(ctx, key_id);
        map_instance.player_count = player_count;
        map_instance.state = if player_count > 0 { "Hot".to_string() } else { "Cold".to_string() };
        ctx.db.map_instance().id().update(map_instance);
    }
    Ok(())
}

pub fn check_map_transition(ctx: &ReducerContext, player_id: u32) -> Result<(), String> {
    let player = ctx.db.player().id().find(&player_id).ok_or("Player not found")?;

    let transitions: Vec<MapTransition> = ctx.db.map_transition().iter()
        .filter(|t| t.map_id == player.current_map_id)
        .collect();

    for t in transitions {
        if player.position_x >= t.x && player.position_x <= (t.x + t.width) &&
            player.position_y >= t.y && player.position_y <= (t.y + t.height)
        {
            // Valida destino antes de mover
            if get_or_create_map_instance(ctx, &t.dest_map_id).is_some() {
                let old_map = player.current_map_id.clone();
                let mut updated_player = player.clone();

                updated_player.current_map_id = t.dest_map_id.clone();
                updated_player.position_x = t.dest_x;
                updated_player.position_y = t.dest_y;
                updated_player.velocity_x = 0.0;
                updated_player.velocity_y = 0.0;

                ctx.db.player().id().update(updated_player);

                update_map_state(ctx, &old_map)?;
                update_map_state(ctx, &t.dest_map_id)?;
                break;
            }
        }
    }
    Ok(())
}