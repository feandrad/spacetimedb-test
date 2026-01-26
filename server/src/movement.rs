use spacetimedb::{reducer, ReducerContext, Table};
use crate::player;
use crate::map::{map_instance, MapInstance}; // Importa a trait e a struct

const MAX_MOVEMENT_SPEED: f32 = 250.0; // pixels per second
const MAX_POSITION_DELTA: f32 = 50.0; // Maximum position change per update

#[reducer]
pub fn update_player_position(
    ctx: &ReducerContext,
    player_id: u32,
    new_x: f32,
    new_y: f32,
    velocity_x: f32,
    velocity_y: f32,
    input_sequence: u32,
) -> Result<(), String> {
    let identity = ctx.sender;

    let player = ctx.db.player().id().find(&player_id)
        .ok_or_else(|| "Player not found".to_string())?;

    if player.identity != identity {
        return Err("Unauthorized movement update".to_string());
    }

    if input_sequence <= player.last_input_sequence {
        return Ok(());
    }

    // BUSCA AUTORITÁRIA: Se não achar a instância, dá ERRO e para tudo.
    let instance = ctx.db.map_instance().iter()
        .find(|m| m.key_id == player.current_map_id)
        .ok_or_else(|| {
            let err = format!("❌ ERRO CRÍTICO: Instância '{}' não existe na tabela map_instance!", player.current_map_id);
            log::error!("{}", err);
            err
        })?;

    // PARSE DIRETO: Sem valores default escondidos.
    let parts: Vec<&str> = instance.metadata.split('x').collect();
    if parts.len() != 2 {
        let err = format!("❌ ERRO DE DATA: Metadata '{}' do mapa '{}' está malformado!", instance.metadata, instance.key_id);
        log::error!("{}", err);
        return Err(err);
    }

    let max_x = parts[0].parse::<f32>().map_err(|_| "Width inválido".to_string())?;
    let max_y = parts[1].parse::<f32>().map_err(|_| "Height inválido".to_string())?;
    let (min_x, min_y) = (0.0, 0.0);

    // Validações usando os limites reais vindos da Row do banco
    let validated_position = validate_movement_bounds(new_x, new_y, min_x, max_x, min_y, max_y);
    let validated_velocity = validate_movement_speed(velocity_x, velocity_y);
    let (final_x, final_y) = validate_position_delta(
        player.position_x,
        player.position_y,
        validated_position.0,
        validated_position.1
    );

    let mut updated_player = player.clone();
    updated_player.position_x = final_x;
    updated_player.position_y = final_y;
    updated_player.velocity_x = validated_velocity.0;
    updated_player.velocity_y = validated_velocity.1;
    updated_player.last_input_sequence = input_sequence;

    ctx.db.player().id().update(updated_player);

    crate::map::check_map_transition(ctx, player_id)?;

    Ok(())
}

/// Validate movement bounds to prevent players from going out of map
/// Requirements 1.5: Server validates all movement inputs
fn validate_movement_bounds(x: f32, y: f32, min_x: f32, max_x: f32, min_y: f32, max_y: f32) -> (f32, f32) {
    let clamped_x = x.clamp(min_x, max_x);
    let clamped_y = y.clamp(min_y, max_y);
    
    if clamped_x != x || clamped_y != y {
        log::debug!("🚧 Movement clamped from ({:.1}, {:.1}) to ({:.1}, {:.1})", x, y, clamped_x, clamped_y);
    }
    
    (clamped_x, clamped_y)
}

/// Validate movement speed to prevent speed hacking
/// Requirements 1.5: Server validates all movement inputs
fn validate_movement_speed(velocity_x: f32, velocity_y: f32) -> (f32, f32) {
    let speed = (velocity_x * velocity_x + velocity_y * velocity_y).sqrt();
    
    if speed > MAX_MOVEMENT_SPEED {
        // Normalize to maximum allowed speed
        let scale = MAX_MOVEMENT_SPEED / speed;
        let validated_x = velocity_x * scale;
        let validated_y = velocity_y * scale;
        
        log::warn!(
            "Speed validation: reduced from {:.1} to {:.1} (max: {:.1})", 
            speed, MAX_MOVEMENT_SPEED, MAX_MOVEMENT_SPEED
        );
        
        (validated_x, validated_y)
    } else {
        (velocity_x, velocity_y)
    }
}

/// Validate position delta to prevent teleporting
/// Requirements 1.5: Server validates all movement inputs
fn validate_position_delta(old_x: f32, old_y: f32, new_x: f32, new_y: f32) -> (f32, f32) {
    let delta_x = new_x - old_x;
    let delta_y = new_y - old_y;
    let delta_distance = (delta_x * delta_x + delta_y * delta_y).sqrt();
    
    if delta_distance > MAX_POSITION_DELTA {
        // Limit movement to maximum allowed delta
        let scale = MAX_POSITION_DELTA / delta_distance;
        let validated_x = old_x + (delta_x * scale);
        let validated_y = old_y + (delta_y * scale);
        
        log::warn!(
            "Position delta validation: limited movement from {:.1} to {:.1} pixels", 
            delta_distance, MAX_POSITION_DELTA
        );
        
        (validated_x, validated_y)
    } else {
        (new_x, new_y)
    }
}

/// Force position correction for a player (admin/debug function)
/// Requirements 1.7: Position reconciliation system
#[reducer]
pub fn force_player_position(
    ctx: &ReducerContext,
    player_id: u32,
    x: f32,
    y: f32,
) -> Result<(), String> {
    let identity = ctx.sender;
    
    // Find the player
    if let Some(player) = ctx.db.player().id().find(&player_id) {
        // Verify the player belongs to the sender (or add admin check here)
        if player.identity != identity {
            return Err("Unauthorized position correction".to_string());
        }

        let (min_x, max_x, min_y, max_y) = crate::map::get_map_bounds_from_db(ctx, &player.current_map_id);
        let validated_position = validate_movement_bounds(x, y, min_x, max_x, min_y, max_y);
        
        // Update player position
        let mut updated_player = player.clone();
        updated_player.position_x = validated_position.0;
        updated_player.position_y = validated_position.1;
        updated_player.velocity_x = 0.0;
        updated_player.velocity_y = 0.0;
        
        // Delete old and insert updated
        ctx.db.player().id().delete(&player_id);
        ctx.db.player().insert(updated_player);
        
        log::info!("🔧 Force corrected player {} position to ({:.1}, {:.1})", player_id, validated_position.0, validated_position.1);
    }
    
    Ok(())
}

#[reducer]
pub fn get_player_position(
    ctx: &ReducerContext,
    player_id: u32,
) -> Result<(), String> {
    // Find the player
    if let Some(player) = ctx.db.player().id().find(&player_id) {
        log::info!(
            "📍 Player {} position: ({:.1}, {:.1}), velocity: ({:.1}, {:.1}), map: {}", 
            player_id, player.position_x, player.position_y,
            player.velocity_x, player.velocity_y, player.current_map_id
        );
    } else {
        log::warn!("❌ Player {} not found", player_id);
    }
    
    Ok(())
}