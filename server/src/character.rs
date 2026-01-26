use spacetimedb::{reducer, ReducerContext, Table};
use crate::{player};

#[reducer]
pub fn apply_damage_to_player(
    ctx: &ReducerContext,
    player_id: u32,
    damage: f32,
    attacker_id: u32,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    // Find the player
    if let Some(player) = ctx.db.player().id().find(&player_id) {
        // Check if player is already downed
        if player.is_downed {
            log::warn!("Player {} is already downed, cannot take more damage", player_id);
            return Ok(());
        }
        
        // Apply damage
        let mut updated_player = player.clone();
        updated_player.health = (updated_player.health - damage).max(0.0);
        
        // Check if player is downed
        if updated_player.health <= 0.0 {
            updated_player.is_downed = true;
            log::info!("Player {} downed by attacker {}", player_id, attacker_id);
        }
        
        // Delete old and insert updated
        ctx.db.player().id().delete(&player_id);
        ctx.db.player().insert(updated_player.clone());
        
        log::info!("Player {} took {} damage from {}, health: {}/{}", 
                  player_id, damage, attacker_id, updated_player.health, updated_player.max_health);
    } else {
        return Err("Player not found".into());
    }
    
    Ok(())
}

/// Heal a player
/// Requirements 9.6: Health consumable restoration
#[reducer]
pub fn heal_player(
    ctx: &ReducerContext,
    player_id: u32,
    heal_amount: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Find the player
    if let Some(player) = ctx.db.player().id().find(&player_id) {
        // Verify the player belongs to the sender
        if player.identity != identity {
            return Err("Unauthorized player update".into());
        }
        
        // Cannot heal downed players
        if player.is_downed {
            log::warn!("Cannot heal downed player {}", player_id);
            return Ok(());
        }
        
        // Apply healing
        let mut updated_player = player.clone();
        let old_health = updated_player.health;
        updated_player.health = (updated_player.health + heal_amount).min(updated_player.max_health);
        
        let actual_healing = updated_player.health - old_health;
        if actual_healing > 0.0 {
            // Delete old and insert updated
            ctx.db.player().id().delete(&player_id);
            ctx.db.player().insert(updated_player.clone());
            
            log::info!("Player {} healed for {}, health: {}/{}", 
                      player_id, actual_healing, updated_player.health, updated_player.max_health);
        }
    } else {
        return Err("Player not found".into());
    }
    
    Ok(())
}

/// Revive a downed player
/// Requirements 9.4: Player revival mechanics
#[reducer]
pub fn revive_player(
    ctx: &ReducerContext,
    player_id: u32,
    reviver_id: u32,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    // Find the player to revive
    if let Some(player) = ctx.db.player().id().find(&player_id) {
        // Check if player is actually downed
        if !player.is_downed {
            log::warn!("Player {} is not downed, cannot revive", player_id);
            return Ok(());
        }
        
        // Revive player with partial health
        let mut updated_player = player.clone();
        updated_player.is_downed = false;
        updated_player.health = updated_player.max_health * 0.5; // Revive with 50% health
        
        // Delete old and insert updated
        ctx.db.player().id().delete(&player_id);
        ctx.db.player().insert(updated_player.clone());
        
        log::info!("Player {} revived by player {} with {} health", 
                  player_id, reviver_id, updated_player.health);
    } else {
        return Err("Player not found".into());
    }
    
    Ok(())
}

/// Set player max health (for upgrades, etc.)
/// Requirements 9.1: Player health system with maximum health capacity
#[reducer]
pub fn set_player_max_health(
    ctx: &ReducerContext,
    player_id: u32,
    max_health: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Find the player
    if let Some(player) = ctx.db.player().id().find(&player_id) {
        // Verify the player belongs to the sender
        if player.identity != identity {
            return Err("Unauthorized player update".into());
        }
        
        // Update max health and maintain health percentage
        let mut updated_player = player.clone();
        let health_ratio = updated_player.health / updated_player.max_health;
        
        updated_player.max_health = max_health.max(1.0); // Minimum 1 health
        updated_player.health = (health_ratio * updated_player.max_health).min(updated_player.max_health);
        
        // Delete old and insert updated
        ctx.db.player().id().delete(&player_id);
        ctx.db.player().insert(updated_player.clone());
        
        log::info!("Player {} max health set to {}, current health: {}", 
                  player_id, max_health, updated_player.health);
    } else {
        return Err("Player not found".into());
    }
    
    Ok(())
}

/// Use a health consumable item
/// Requirements 9.6: Health consumable restoration
#[reducer]
pub fn use_health_consumable(
    ctx: &ReducerContext,
    player_id: u32,
    item_id: String,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Find the player
    if let Some(player) = ctx.db.player().id().find(&player_id) {
        // Verify the player belongs to the sender
        if player.identity != identity {
            return Err("Unauthorized player update".into());
        }
        
        // Check if player is downed (cannot use consumables when downed)
        if player.is_downed {
            log::warn!("Cannot use consumable: player {} is downed", player_id);
            return Ok(());
        }
        
        // Define healing amounts for different consumables
        let heal_amount = match item_id.as_str() {
            "fruit" => 25.0,
            "health_potion" => 50.0,
            "mega_health_potion" => 100.0,
            _ => {
                log::warn!("Unknown consumable item: {}", item_id);
                return Err("Unknown consumable item".into());
            }
        };
        
        // Apply healing
        let mut updated_player = player.clone();
        let old_health = updated_player.health;
        updated_player.health = (updated_player.health + heal_amount).min(updated_player.max_health);
        
        let actual_healing = updated_player.health - old_health;
        if actual_healing > 0.0 {
            // Delete old and insert updated
            ctx.db.player().id().delete(&player_id);
            ctx.db.player().insert(updated_player.clone());
            
            log::info!("Player {} consumed {} and healed for {}, health: {}/{}", 
                      player_id, item_id, actual_healing, updated_player.health, updated_player.max_health);
        }
    } else {
        return Err("Player not found".into());
    }
    
    Ok(())
}