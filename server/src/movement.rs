use spacetimedb::{spacetimedb, ReducerContext};
use crate::Player;

// Movement system reducers - server-authoritative movement validation
// Implements Requirements 1.5, 1.7: Server validation and position reconciliation

// Movement bounds for basic collision detection
const MAP_MIN_X: f32 = -1000.0;
const MAP_MAX_X: f32 = 1000.0;
const MAP_MIN_Y: f32 = -1000.0;
const MAP_MAX_Y: f32 = 1000.0;
const MAX_MOVEMENT_SPEED: f32 = 250.0; // pixels per second
const MAX_POSITION_DELTA: f32 = 50.0; // Maximum position change per update

#[spacetimedb(reducer)]
pub fn update_player_position(
    ctx: ReducerContext,
    player_id: u32,
    new_x: f32,
    new_y: f32,
    velocity_x: f32,
    velocity_y: f32,
    input_sequence: u32,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Find the player
    let players: Vec<Player> = Player::filter_by_id(&player_id).collect();
    if let Some(player) = players.first() {
        // Verify the player belongs to the sender
        if player.identity != identity {
            return Err("Unauthorized movement update".into());
        }
        
        // Basic validation - ensure sequence is newer
        if input_sequence <= player.last_input_sequence {
            return Ok(()); // Ignore old input
        }
        
        // Validate movement bounds (basic collision detection)
        let validated_position = validate_movement_bounds(new_x, new_y);
        
        // Validate movement speed (anti-cheat)
        let validated_velocity = validate_movement_speed(velocity_x, velocity_y);
        
        // Validate position delta (prevent teleporting)
        let validated_position = validate_position_delta(
            player.position_x, 
            player.position_y, 
            validated_position.0, 
            validated_position.1
        );
        
        // Update player state
        let mut updated_player = player.clone();
        updated_player.position_x = validated_position.0;
        updated_player.position_y = validated_position.1;
        updated_player.velocity_x = validated_velocity.0;
        updated_player.velocity_y = validated_velocity.1;
        updated_player.last_input_sequence = input_sequence;
        
        // Delete old and insert updated
        Player::delete_by_id(&player_id);
        Player::insert(updated_player);
        
        log::debug!(
            "Updated player {} position to ({:.1}, {:.1}) with velocity ({:.1}, {:.1})", 
            player_id, validated_position.0, validated_position.1,
            validated_velocity.0, validated_velocity.1
        );
    }
    
    Ok(())
}

/// Validate movement bounds to prevent players from going out of map
/// Requirements 1.5: Server validates all movement inputs
fn validate_movement_bounds(x: f32, y: f32) -> (f32, f32) {
    let clamped_x = x.clamp(MAP_MIN_X, MAP_MAX_X);
    let clamped_y = y.clamp(MAP_MIN_Y, MAP_MAX_Y);
    
    if clamped_x != x || clamped_y != y {
        log::debug!("Movement clamped from ({:.1}, {:.1}) to ({:.1}, {:.1})", x, y, clamped_x, clamped_y);
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
#[spacetimedb(reducer)]
pub fn force_player_position(
    ctx: ReducerContext,
    player_id: u32,
    x: f32,
    y: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Find the player
    let players: Vec<Player> = Player::filter_by_id(&player_id).collect();
    if let Some(player) = players.first() {
        // Verify the player belongs to the sender (or add admin check here)
        if player.identity != identity {
            return Err("Unauthorized position correction".into());
        }
        
        // Validate bounds
        let validated_position = validate_movement_bounds(x, y);
        
        // Update player position
        let mut updated_player = player.clone();
        updated_player.position_x = validated_position.0;
        updated_player.position_y = validated_position.1;
        updated_player.velocity_x = 0.0;
        updated_player.velocity_y = 0.0;
        
        // Delete old and insert updated
        Player::delete_by_id(&player_id);
        Player::insert(updated_player);
        
        log::info!("Force corrected player {} position to ({:.1}, {:.1})", player_id, validated_position.0, validated_position.1);
    }
    
    Ok(())
}

/// Get player position (for client queries)
/// Requirements 1.7: Position reconciliation system
#[spacetimedb(reducer)]
pub fn get_player_position(
    ctx: ReducerContext,
    player_id: u32,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    // Find the player
    let players: Vec<Player> = Player::filter_by_id(&player_id).collect();
    if let Some(player) = players.first() {
        log::info!(
            "Player {} position: ({:.1}, {:.1}), velocity: ({:.1}, {:.1})", 
            player_id, player.position_x, player.position_y,
            player.velocity_x, player.velocity_y
        );
    } else {
        log::warn!("Player {} not found", player_id);
    }
    
    Ok(())
}

/// Collision detection helper functions
/// These will be expanded when map system is implemented

/// Check if position collides with static obstacles
/// Requirements 1.5: Server-side collision detection
fn check_static_collision(x: f32, y: f32) -> bool {
    // TODO: Implement actual collision detection with map obstacles
    // For now, just check bounds
    x < MAP_MIN_X || x > MAP_MAX_X || y < MAP_MIN_Y || y > MAP_MAX_Y
}

/// Check if position collides with other players
/// Requirements 7.4: Disable body blocking between players
fn check_player_collision(_player_id: u32, _x: f32, _y: f32) -> bool {
    // Body blocking disabled for cooperative multiplayer gameplay
    // Players can move through each other without collision
    log::debug!("Player collision check disabled for cooperative gameplay");
    false
}