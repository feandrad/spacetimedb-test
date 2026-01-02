use spacetimedb::{spacetimedb, ReducerContext, Table};
use crate::Player;

// Map transition zone data
#[derive(Clone)]
struct TransitionZone {
    area_x: f32,
    area_y: f32,
    area_width: f32,
    area_height: f32,
    destination_map_id: String,
    destination_x: f32,
    destination_y: f32,
}

// Map instance tracking for multiplayer synchronization
#[spacetimedb(table)]
pub struct MapInstance {
    #[primarykey]
    pub map_id: String,
    pub player_count: u32,
    pub last_updated: u64,
}

// Map registry with predefined maps and transition zones
fn get_map_transitions(map_id: &str) -> Vec<TransitionZone> {
    match map_id {
        "starting_area" => vec![
            TransitionZone {
                area_x: 950.0,
                area_y: 400.0,
                area_width: 50.0,
                area_height: 200.0,
                destination_map_id: "forest_area".to_string(),
                destination_x: 50.0,
                destination_y: 500.0,
            }
        ],
        "forest_area" => vec![
            TransitionZone {
                area_x: 0.0,
                area_y: 400.0,
                area_width: 50.0,
                area_height: 200.0,
                destination_map_id: "starting_area".to_string(),
                destination_x: 900.0,
                destination_y: 500.0,
            }
        ],
        _ => vec![],
    }
}

fn is_valid_map(map_id: &str) -> bool {
    matches!(map_id, "starting_area" | "forest_area")
}

fn is_in_transition_zone(current_map: &str, pos_x: f32, pos_y: f32, destination_map: &str) -> bool {
    let transitions = get_map_transitions(current_map);
    
    for transition in transitions {
        if transition.destination_map_id == destination_map {
            let in_x_range = pos_x >= transition.area_x && pos_x <= (transition.area_x + transition.area_width);
            let in_y_range = pos_y >= transition.area_y && pos_y <= (transition.area_y + transition.area_height);
            
            if in_x_range && in_y_range {
                return true;
            }
        }
    }
    
    false
}

fn get_spawn_point(map_id: &str, player_index: u32) -> (f32, f32) {
    let spawn_points = match map_id {
        "starting_area" => vec![
            (100.0, 500.0),
            (150.0, 500.0),
            (200.0, 500.0),
            (250.0, 500.0),
        ],
        "forest_area" => vec![
            (100.0, 400.0),
            (150.0, 400.0),
            (200.0, 400.0),
            (250.0, 400.0),
        ],
        _ => vec![(0.0, 0.0)],
    };
    
    let index = (player_index as usize) % spawn_points.len();
    spawn_points[index]
}

fn update_map_instance_count(map_id: &str) {
    // Count players in this map
    let player_count = Player::iter()
        .filter(|p| p.current_map_id == map_id)
        .count() as u32;
    
    // Update or create map instance record
    let existing_instances: Vec<MapInstance> = MapInstance::filter_by_map_id(map_id).collect();
    
    if let Some(instance) = existing_instances.first() {
        // Update existing instance
        let mut updated_instance = instance.clone();
        updated_instance.player_count = player_count;
        updated_instance.last_updated = spacetimedb::sys::unix_epoch();
        
        MapInstance::delete_by_map_id(map_id);
        MapInstance::insert(updated_instance);
    } else {
        // Create new instance
        let new_instance = MapInstance {
            map_id: map_id.to_string(),
            player_count,
            last_updated: spacetimedb::sys::unix_epoch(),
        };
        MapInstance::insert(new_instance);
    }
    
    log::info!("Map {} now has {} players", map_id, player_count);
}

// Map system reducers - server-authoritative map transitions

#[spacetimedb(reducer)]
pub fn transition_to_map(
    ctx: ReducerContext,
    player_id: u32,
    map_id: String,
    entry_point_x: f32,
    entry_point_y: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Find the player
    let players: Vec<Player> = Player::filter_by_id(&player_id).collect();
    if let Some(player) = players.first() {
        // Verify the player belongs to the sender
        if player.identity != identity {
            return Err("Unauthorized map transition".into());
        }
        
        // Validate destination map exists
        if !is_valid_map(&map_id) {
            return Err("Invalid destination map".into());
        }
        
        // Validate map transition is allowed (player must be in transition zone)
        if !is_in_transition_zone(&player.current_map_id, player.position_x, player.position_y, &map_id) {
            return Err("Player not in valid transition zone".into());
        }
        
        let old_map_id = player.current_map_id.clone();
        
        // Update player's map and position
        let mut updated_player = player.clone();
        updated_player.current_map_id = map_id.clone();
        updated_player.position_x = entry_point_x;
        updated_player.position_y = entry_point_y;
        updated_player.velocity_x = 0.0;
        updated_player.velocity_y = 0.0;
        
        // Delete old and insert updated
        Player::delete_by_id(&player_id);
        Player::insert(updated_player);
        
        // Update map instance counts for both old and new maps
        update_map_instance_count(&old_map_id);
        update_map_instance_count(&map_id);
        
        log::info!("Player {} transitioned from {} to {} at ({}, {})", 
                   player_id, old_map_id, map_id, entry_point_x, entry_point_y);
    }
    
    Ok(())
}

#[spacetimedb(reducer)]
pub fn spawn_player_at_map(
    ctx: ReducerContext,
    player_id: u32,
    map_id: String,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Find the player
    let players: Vec<Player> = Player::filter_by_id(&player_id).collect();
    if let Some(player) = players.first() {
        // Verify the player belongs to the sender
        if player.identity != identity {
            return Err("Unauthorized spawn request".into());
        }
        
        // Validate destination map exists
        if !is_valid_map(&map_id) {
            return Err("Invalid map for spawning".into());
        }
        
        // Get appropriate spawn point
        let (spawn_x, spawn_y) = get_spawn_point(&map_id, player_id);
        
        let old_map_id = player.current_map_id.clone();
        
        // Update player's map and position
        let mut updated_player = player.clone();
        updated_player.current_map_id = map_id.clone();
        updated_player.position_x = spawn_x;
        updated_player.position_y = spawn_y;
        updated_player.velocity_x = 0.0;
        updated_player.velocity_y = 0.0;
        
        // Delete old and insert updated
        Player::delete_by_id(&player_id);
        Player::insert(updated_player);
        
        // Update map instance counts
        if old_map_id != map_id {
            update_map_instance_count(&old_map_id);
        }
        update_map_instance_count(&map_id);
        
        log::info!("Player {} spawned at map {} at ({}, {})", 
                   player_id, map_id, spawn_x, spawn_y);
    }
    
    Ok(())
}

#[spacetimedb(reducer)]
pub fn get_players_in_map(
    _ctx: ReducerContext,
    map_id: String,
) -> Result<(), Box<dyn std::error::Error>> {
    // This reducer allows clients to query for all players in a specific map
    // The actual data is returned via subscriptions to the Player table
    
    if !is_valid_map(&map_id) {
        return Err("Invalid map ID".into());
    }
    
    let players_in_map: Vec<Player> = Player::iter()
        .filter(|p| p.current_map_id == map_id)
        .collect();
    
    log::info!("Map {} has {} players", map_id, players_in_map.len());
    
    Ok(())
}

#[spacetimedb(reducer)]
pub fn sync_map_state(
    ctx: ReducerContext,
    player_id: u32,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Find the requesting player
    let players: Vec<Player> = Player::filter_by_id(&player_id).collect();
    if let Some(player) = players.first() {
        // Verify the player belongs to the sender
        if player.identity != identity {
            return Err("Unauthorized sync request".into());
        }
        
        // Update map instance count for the player's current map
        update_map_instance_count(&player.current_map_id);
        
        log::info!("Synced map state for player {} in map {}", 
                   player_id, player.current_map_id);
    }
    
    Ok(())
}