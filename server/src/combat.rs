use spacetimedb::{spacetimedb, ReducerContext, Identity};
use crate::Player;

// Enemy table for combat targets with AI state machine
#[spacetimedb(table)]
#[derive(Clone)]
pub struct Enemy {
    #[spacetimedb(primary_key)]
    pub id: u32,
    pub position_x: f32,
    pub position_y: f32,
    pub velocity_x: f32,
    pub velocity_y: f32,
    pub health: f32,
    pub max_health: f32,
    pub enemy_type: String,
    pub map_id: String,
    pub state: String, // "Idle", "Alert", "Chasing"
    pub patrol_center_x: f32,
    pub patrol_center_y: f32,
    pub patrol_radius: f32,
    pub detection_range: f32,
    pub leash_range: f32,
    pub target_player_id: Option<u32>,
    pub last_known_player_x: f32,
    pub last_known_player_y: f32,
    pub state_timer: f32,
    pub movement_speed: f32,
    pub attack_damage: f32,
    pub attack_range: f32,
    pub attack_cooldown: f32,
    pub last_attack_time: f64,
    pub is_active: bool,
}

// Projectile table for server-side projectile management
#[spacetimedb(table)]
#[derive(Clone)]
pub struct Projectile {
    #[spacetimedb(primary_key)]
    pub id: u32,
    pub owner_id: u32,
    pub position_x: f32,
    pub position_y: f32,
    pub velocity_x: f32,
    pub velocity_y: f32,
    pub damage: f32,
    pub time_to_live: f32,
    pub projectile_type: String,
    pub map_id: String,
    pub is_active: bool,
}

// Combat event for client synchronization
#[spacetimedb(table)]
#[derive(Clone)]
pub struct CombatEvent {
    #[spacetimedb(primary_key)]
    pub id: u32,
    pub attacker_id: u32,
    pub target_id: u32,
    pub weapon_type: String,
    pub damage: f32,
    pub timestamp: u64,
}

// Weapon configuration constants
const SWORD_DAMAGE: f32 = 25.0;
const AXE_DAMAGE: f32 = 40.0;
const BOW_DAMAGE: f32 = 20.0;
const SWORD_RANGE: f32 = 80.0;
const AXE_RANGE: f32 = 60.0;
const SWORD_CLEAVE_ANGLE: f32 = 90.0; // degrees
const AXE_FRONTAL_ANGLE: f32 = 45.0; // degrees

// Projectile configuration constants
const ARROW_SPEED: f32 = 400.0;
const ARROW_MAX_RANGE: f32 = 300.0;
const ARROW_TIME_TO_LIVE: f32 = 5.0;
const PROJECTILE_COLLISION_RADIUS: f32 = 5.0;

#[spacetimedb(reducer)]
pub fn execute_attack(
    ctx: ReducerContext,
    player_id: u32,
    weapon_type: String,
    direction_x: f32,
    direction_y: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Validate player exists and owns this identity
    let player = match Player::filter_by_id(&player_id).next() {
        Some(p) if p.identity == identity => p,
        Some(_) => {
            log::warn!("Player {} attack rejected: identity mismatch", player_id);
            return Ok(());
        }
        None => {
            log::warn!("Player {} not found for attack", player_id);
            return Ok(());
        }
    };
    
    // Validate player is not downed
    if player.is_downed {
        log::info!("Player {} attack rejected: player is downed", player_id);
        return Ok(());
    }
    
    log::info!("Player {} executed {} attack in direction ({}, {})", 
               player_id, weapon_type, direction_x, direction_y);
    
    // Handle different weapon types
    match weapon_type.as_str() {
        "Sword" => execute_sword_attack(player, direction_x, direction_y)?,
        "Axe" => execute_axe_attack(player, direction_x, direction_y)?,
        "Bow" => execute_bow_attack(player, direction_x, direction_y)?,
        _ => {
            log::warn!("Unknown weapon type: {}", weapon_type);
            return Ok(());
        }
    }
    
    Ok(())
}

/// Execute sword cleave attack - wide area hitting multiple enemies
/// Requirements 3.1: Wide cleave attacks that hit multiple enemies
/// Requirements 7.3: Friendly fire prevention between players
fn execute_sword_attack(
    player: Player,
    direction_x: f32,
    direction_y: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    log::info!("Executing sword cleave attack for player {}", player.id);
    
    // Find all enemies in the same map (exclude players for friendly fire prevention)
    let enemies: Vec<Enemy> = Enemy::filter_by_map_id(&player.current_map_id).collect();
    
    // Calculate hit area for sword cleave
    let mut targets_hit = 0;
    
    for enemy in enemies {
        if is_in_sword_cleave_area(
            player.position_x, player.position_y,
            enemy.position_x, enemy.position_y,
            direction_x, direction_y
        ) {
            // Apply damage to enemy
            apply_damage_to_enemy(enemy.id, SWORD_DAMAGE, player.id, "Sword".to_string())?;
            targets_hit += 1;
        }
    }
    
    log::info!("Sword cleave hit {} enemy targets (friendly fire prevented)", targets_hit);
    Ok(())
}

/// Check if target is within sword cleave area
/// Sword has wide cleave attack that hits multiple enemies in an arc
fn is_in_sword_cleave_area(
    attacker_x: f32, attacker_y: f32,
    target_x: f32, target_y: f32,
    direction_x: f32, direction_y: f32,
) -> bool {
    // Calculate distance
    let dx = target_x - attacker_x;
    let dy = target_y - attacker_y;
    let distance = (dx * dx + dy * dy).sqrt();
    
    // Check if within range
    if distance > SWORD_RANGE {
        return false;
    }
    
    // Normalize direction vector
    let dir_length = (direction_x * direction_x + direction_y * direction_y).sqrt();
    if dir_length == 0.0 {
        return false;
    }
    let norm_dir_x = direction_x / dir_length;
    let norm_dir_y = direction_y / dir_length;
    
    // Normalize target vector
    let norm_target_x = dx / distance;
    let norm_target_y = dy / distance;
    
    // Calculate angle between direction and target
    let dot_product = norm_dir_x * norm_target_x + norm_dir_y * norm_target_y;
    let angle_radians = dot_product.acos();
    let angle_degrees = angle_radians.to_degrees();
    
    // Check if within cleave angle (wide arc for sword)
    angle_degrees <= SWORD_CLEAVE_ANGLE / 2.0
}

/// Execute axe frontal attack - high damage, narrow frontal area
/// Requirements 3.2: Higher damage attacks that only hit enemies directly in front
/// Requirements 7.3: Friendly fire prevention between players
fn execute_axe_attack(
    player: Player,
    direction_x: f32,
    direction_y: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    log::info!("Executing axe frontal attack for player {}", player.id);
    
    // Find all enemies in the same map (exclude players for friendly fire prevention)
    let enemies: Vec<Enemy> = Enemy::filter_by_map_id(&player.current_map_id).collect();
    
    // Calculate hit area for axe frontal attack
    let mut targets_hit = 0;
    
    for enemy in enemies {
        if is_in_axe_frontal_area(
            player.position_x, player.position_y,
            enemy.position_x, enemy.position_y,
            direction_x, direction_y
        ) {
            // Apply higher damage to enemy (axe does more damage than sword)
            apply_damage_to_enemy(enemy.id, AXE_DAMAGE, player.id, "Axe".to_string())?;
            targets_hit += 1;
        }
    }
    
    log::info!("Axe frontal attack hit {} enemy targets (friendly fire prevented)", targets_hit);
    Ok(())
}

/// Check if target is within axe frontal area
/// Axe has narrow frontal attack with higher damage
fn is_in_axe_frontal_area(
    attacker_x: f32, attacker_y: f32,
    target_x: f32, target_y: f32,
    direction_x: f32, direction_y: f32,
) -> bool {
    // Calculate distance
    let dx = target_x - attacker_x;
    let dy = target_y - attacker_y;
    let distance = (dx * dx + dy * dy).sqrt();
    
    // Check if within range
    if distance > AXE_RANGE {
        return false;
    }
    
    // Normalize direction vector
    let dir_length = (direction_x * direction_x + direction_y * direction_y).sqrt();
    if dir_length == 0.0 {
        return false;
    }
    let norm_dir_x = direction_x / dir_length;
    let norm_dir_y = direction_y / dir_length;
    
    // Normalize target vector
    let norm_target_x = dx / distance;
    let norm_target_y = dy / distance;
    
    // Calculate angle between direction and target
    let dot_product = norm_dir_x * norm_target_x + norm_dir_y * norm_target_y;
    let angle_radians = dot_product.acos();
    let angle_degrees = angle_radians.to_degrees();
    
    // Check if within frontal angle (narrow arc for axe)
    angle_degrees <= AXE_FRONTAL_ANGLE / 2.0
}

/// Execute bow projectile attack
/// Requirements 3.3: Projectile attacks that consume ammunition
/// Requirements 4.2: Consume ammunition from inventory
fn execute_bow_attack(
    player: Player,
    direction_x: f32,
    direction_y: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    log::info!("Executing bow projectile attack for player {}", player.id);
    
    // Check ammunition in inventory
    let arrows: Vec<crate::inventory::InventoryItem> = crate::inventory::InventoryItem::filter_by_player_id(&player.id)
        .filter(|item| item.item_id == "arrow" && item.quantity > 0)
        .collect();
    
    if arrows.is_empty() {
        log::info!("Player {} has no arrows for bow attack", player.id);
        return Ok(());
    }
    
    // Consume one arrow
    if let Some(arrow_item) = arrows.first() {
        let mut updated_arrow = arrow_item.clone();
        updated_arrow.quantity -= 1;
        
        if updated_arrow.quantity <= 0 {
            // Remove item if no arrows left
            crate::inventory::InventoryItem::delete_by_id(&arrow_item.id);
            log::info!("Player {} used last arrow", player.id);
        } else {
            // Update quantity
            crate::inventory::InventoryItem::delete_by_id(&arrow_item.id);
            crate::inventory::InventoryItem::insert(updated_arrow);
            log::info!("Player {} has {} arrows remaining", player.id, updated_arrow.quantity);
        }
    }
    
    // Normalize direction vector
    let dir_length = (direction_x * direction_x + direction_y * direction_y).sqrt();
    if dir_length == 0.0 {
        log::warn!("Invalid direction vector for bow attack");
        return Ok(());
    }
    let norm_dir_x = direction_x / dir_length;
    let norm_dir_y = direction_y / dir_length;
    
    // Create projectile directly (since we're already in a reducer context)
    let projectile = Projectile {
        id: generate_projectile_id(),
        owner_id: player.id,
        position_x: player.position_x,
        position_y: player.position_y,
        velocity_x: norm_dir_x * ARROW_SPEED,
        velocity_y: norm_dir_y * ARROW_SPEED,
        damage: BOW_DAMAGE,
        time_to_live: ARROW_TIME_TO_LIVE,
        projectile_type: "Arrow".to_string(),
        map_id: player.current_map_id.clone(),
        is_active: true,
    };
    
    Projectile::insert(projectile.clone());
    
    log::info!("Created arrow projectile {} for player {} with velocity ({}, {})", 
               projectile.id, player.id, projectile.velocity_x, projectile.velocity_y);
    
    Ok(())
}

/// Apply damage to an enemy
/// Requirements 3.5: Deal appropriate damage based on weapon type
/// Requirements 7.3: Friendly fire prevention between players
fn apply_damage_to_enemy(
    enemy_id: u32,
    damage: f32,
    attacker_id: u32,
    weapon_type: String,
) -> Result<(), Box<dyn std::error::Error>> {
    // Check if target is actually an enemy (enemy IDs >= 1000000)
    if enemy_id < 1000000 {
        // Target is a player - check if attacker is also a player (friendly fire prevention)
        if attacker_id < 1000000 {
            log::info!("Friendly fire prevented: player {} cannot damage player {}", attacker_id, enemy_id);
            return Ok(());
        }
        
        // Attacker is an enemy, target is a player - apply damage to player instead
        return apply_damage_to_player_from_enemy(enemy_id, damage, attacker_id);
    }
    
    // Find and update enemy
    if let Some(mut enemy) = Enemy::filter_by_id(&enemy_id).next() {
        enemy.health -= damage;
        
        log::info!("Enemy {} took {} damage from {} ({}), health: {}/{}", 
                   enemy_id, damage, attacker_id, weapon_type, enemy.health, enemy.max_health);
        
        if enemy.health <= 0.0 {
            // Enemy is defeated
            log::info!("Enemy {} defeated by player {}", enemy_id, attacker_id);
            Enemy::delete_by_id(&enemy_id);
            
            // TODO: Handle loot drops and experience
        } else {
            // Update enemy health
            Enemy::update_by_id(&enemy_id, enemy);
        }
        
        // Record combat event
        let event = CombatEvent {
            id: generate_combat_event_id(),
            attacker_id,
            target_id: enemy_id,
            weapon_type,
            damage,
            timestamp: get_current_timestamp(),
        };
        CombatEvent::insert(event);
    }
    
    Ok(())
}

/// Apply damage to a player from an enemy
/// Requirements 8.6: Enemy damage dealing to players
/// Requirements 9.2: Player damage application
fn apply_damage_to_player_from_enemy(
    player_id: u32,
    damage: f32,
    attacker_id: u32,
) -> Result<(), Box<dyn std::error::Error>> {
    // Find the player
    if let Some(mut player) = Player::filter_by_id(&player_id).next() {
        // Check if player is already downed
        if player.is_downed {
            log::warn!("Player {} is already downed, cannot take more damage", player_id);
            return Ok(());
        }
        
        // Apply damage
        player.health = (player.health - damage).max(0.0);
        
        // Check if player is downed
        if player.health <= 0.0 {
            player.is_downed = true;
            log::info!("Player {} downed by enemy {}", player_id, attacker_id);
        }
        
        // Update player
        Player::update_by_id(&player_id, player);
        
        // Record combat event
        let event = CombatEvent {
            id: generate_combat_event_id(),
            attacker_id,
            target_id: player_id,
            weapon_type: "Enemy Attack".to_string(),
            damage,
            timestamp: get_current_timestamp(),
        };
        CombatEvent::insert(event);
        
        log::info!("Player {} took {} damage from enemy {}, health: {}/{}", 
                  player_id, damage, attacker_id, player.health, player.max_health);
    } else {
        return Err("Player not found".into());
    }
    
    Ok(())
}

/// Generate unique combat event ID
fn generate_combat_event_id() -> u32 {
    use std::collections::hash_map::DefaultHasher;
    use std::hash::{Hash, Hasher};
    
    let mut hasher = DefaultHasher::new();
    std::time::SystemTime::now().hash(&mut hasher);
    (hasher.finish() % u32::MAX as u64) as u32
}

/// Get current timestamp
fn get_current_timestamp() -> u64 {
    std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap_or_default()
        .as_secs()
}

/// Spawn a test enemy for combat testing
#[spacetimedb(reducer)]
pub fn spawn_test_enemy(
    ctx: ReducerContext,
    position_x: f32,
    position_y: f32,
    map_id: String,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    let enemy = Enemy {
        id: generate_enemy_id(),
        position_x,
        position_y,
        velocity_x: 0.0,
        velocity_y: 0.0,
        health: 50.0,
        max_health: 50.0,
        enemy_type: "test_enemy".to_string(),
        map_id,
        state: "Idle".to_string(),
        patrol_center_x: position_x,
        patrol_center_y: position_y,
        patrol_radius: 100.0,
        detection_range: 120.0,
        leash_range: 200.0,
        target_player_id: None,
        last_known_player_x: 0.0,
        last_known_player_y: 0.0,
        state_timer: 0.0,
        movement_speed: 75.0,
        attack_damage: 15.0,
        attack_range: 30.0,
        attack_cooldown: 2.0,
        last_attack_time: 0.0,
        is_active: true,
    };
    
    Enemy::insert(enemy.clone());
    log::info!("Spawned test enemy {} at ({}, {})", enemy.id, position_x, position_y);
    
    Ok(())
}

/// Generate unique enemy ID
fn generate_enemy_id() -> u32 {
    use std::collections::hash_map::DefaultHasher;
    use std::hash::{Hash, Hasher};
    
    let mut hasher = DefaultHasher::new();
    std::time::SystemTime::now().hash(&mut hasher);
    ((hasher.finish() % u32::MAX as u64) as u32).wrapping_add(1000000) // Offset to avoid player ID conflicts
}

/// Spawn enemy with AI configuration
#[spacetimedb(reducer)]
pub fn spawn_enemy(
    ctx: ReducerContext,
    enemy_id: u32,
    position_x: f32,
    position_y: f32,
    map_id: String,
    enemy_type: String,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    // Get enemy type configuration
    let (max_health, movement_speed, attack_damage, attack_range, detection_range, leash_range) = 
        match enemy_type.as_str() {
            "TestEnemy" => (50.0, 75.0, 15.0, 30.0, 100.0, 200.0),
            "Goblin" => (30.0, 120.0, 10.0, 25.0, 80.0, 150.0),
            "Orc" => (80.0, 60.0, 25.0, 40.0, 120.0, 250.0),
            "Troll" => (150.0, 40.0, 40.0, 50.0, 100.0, 180.0),
            _ => (50.0, 75.0, 15.0, 30.0, 100.0, 200.0), // Default to TestEnemy
        };
    
    let enemy = Enemy {
        id: enemy_id,
        position_x,
        position_y,
        velocity_x: 0.0,
        velocity_y: 0.0,
        health: max_health,
        max_health,
        enemy_type,
        map_id,
        state: "Idle".to_string(),
        patrol_center_x: position_x,
        patrol_center_y: position_y,
        patrol_radius: 100.0,
        detection_range,
        leash_range,
        target_player_id: None,
        last_known_player_x: 0.0,
        last_known_player_y: 0.0,
        state_timer: 0.0,
        movement_speed,
        attack_damage,
        attack_range,
        attack_cooldown: 2.0,
        last_attack_time: 0.0,
        is_active: true,
    };
    
    Enemy::insert(enemy.clone());
    log::info!("Spawned {} enemy {} at ({}, {})", enemy.enemy_type, enemy.id, position_x, position_y);
    
    Ok(())
}

/// Remove enemy from the game world
#[spacetimedb(reducer)]
pub fn remove_enemy(
    ctx: ReducerContext,
    enemy_id: u32,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    if let Some(enemy) = Enemy::filter_by_id(&enemy_id).next() {
        Enemy::delete_by_id(&enemy_id);
        log::info!("Removed enemy {} from map {}", enemy_id, enemy.map_id);
    } else {
        log::warn!("Attempted to remove non-existent enemy {}", enemy_id);
    }
    
    Ok(())
}

/// Update enemy AI state
#[spacetimedb(reducer)]
pub fn update_enemy_ai(
    ctx: ReducerContext,
    enemy_id: u32,
    new_state: String,
    position_x: f32,
    position_y: f32,
    velocity_x: f32,
    velocity_y: f32,
    target_player_id: Option<u32>,
    last_known_player_x: f32,
    last_known_player_y: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    if let Some(mut enemy) = Enemy::filter_by_id(&enemy_id).next() {
        enemy.state = new_state;
        enemy.position_x = position_x;
        enemy.position_y = position_y;
        enemy.velocity_x = velocity_x;
        enemy.velocity_y = velocity_y;
        enemy.target_player_id = target_player_id;
        enemy.last_known_player_x = last_known_player_x;
        enemy.last_known_player_y = last_known_player_y;
        
        Enemy::update_by_id(&enemy_id, enemy);
        log::info!("Updated enemy {} state to {} at ({}, {})", enemy_id, enemy.state, position_x, position_y);
    } else {
        log::warn!("Attempted to update non-existent enemy {}", enemy_id);
    }
    
    Ok(())
}

/// Enemy attacks player
#[spacetimedb(reducer)]
pub fn enemy_attack_player(
    ctx: ReducerContext,
    enemy_id: u32,
    player_id: u32,
    damage: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    // Validate enemy exists
    let mut enemy = match Enemy::filter_by_id(&enemy_id).next() {
        Some(e) => e,
        None => {
            log::warn!("Enemy {} not found for attack", enemy_id);
            return Ok(());
        }
    };
    
    // Validate player exists
    let mut player = match Player::filter_by_id(&player_id).next() {
        Some(p) => p,
        None => {
            log::warn!("Player {} not found for enemy attack", player_id);
            return Ok(());
        }
    };
    
    // Check if player is downed
    if player.is_downed {
        log::info!("Enemy {} cannot attack downed player {}", enemy_id, player_id);
        return Ok(());
    }
    
    // Apply damage to player
    player.health -= damage;
    
    log::info!("Enemy {} attacked player {} for {} damage, player health: {}/{}", 
               enemy_id, player_id, damage, player.health, player.max_health);
    
    // Check if player is downed
    if player.health <= 0.0 {
        player.health = 0.0;
        player.is_downed = true;
        log::info!("Player {} downed by enemy {}", player_id, enemy_id);
    }
    
    // Update enemy attack time
    enemy.last_attack_time = get_current_timestamp() as f64;
    
    // Update both entities
    Player::update_by_id(&player_id, player);
    Enemy::update_by_id(&enemy_id, enemy);
    
    // Record combat event
    let event = CombatEvent {
        id: generate_combat_event_id(),
        attacker_id: enemy_id,
        target_id: player_id,
        weapon_type: "Enemy Attack".to_string(),
        damage,
        timestamp: get_current_timestamp(),
    };
    CombatEvent::insert(event);
    
    Ok(())
}

#[spacetimedb(reducer)]
pub fn create_projectile(
    ctx: ReducerContext,
    player_id: u32,
    origin_x: f32,
    origin_y: f32,
    direction_x: f32,
    direction_y: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Validate player exists and owns this identity
    let player = match Player::filter_by_id(&player_id).next() {
        Some(p) if p.identity == identity => p,
        Some(_) => {
            log::warn!("Player {} projectile creation rejected: identity mismatch", player_id);
            return Ok(());
        }
        None => {
            log::warn!("Player {} not found for projectile creation", player_id);
            return Ok(());
        }
    };
    
    // Validate player is not downed
    if player.is_downed {
        log::info!("Player {} projectile creation rejected: player is downed", player_id);
        return Ok(());
    }
    
    // TODO: Check ammunition in inventory system
    // For now, assume player has ammunition
    
    // Normalize direction vector
    let dir_length = (direction_x * direction_x + direction_y * direction_y).sqrt();
    if dir_length == 0.0 {
        log::warn!("Invalid direction vector for projectile creation");
        return Ok(());
    }
    let norm_dir_x = direction_x / dir_length;
    let norm_dir_y = direction_y / dir_length;
    
    // Create projectile
    let projectile = Projectile {
        id: generate_projectile_id(),
        owner_id: player_id,
        position_x: origin_x,
        position_y: origin_y,
        velocity_x: norm_dir_x * ARROW_SPEED,
        velocity_y: norm_dir_y * ARROW_SPEED,
        damage: BOW_DAMAGE,
        time_to_live: ARROW_TIME_TO_LIVE,
        projectile_type: "Arrow".to_string(),
        map_id: player.current_map_id.clone(),
        is_active: true,
    };
    
    Projectile::insert(projectile.clone());
    
    log::info!("Player {} created projectile {} at ({}, {}) with direction ({}, {})", 
               player_id, projectile.id, origin_x, origin_y, direction_x, direction_y);
    
    Ok(())
}

#[spacetimedb(reducer)]
pub fn process_hit(
    ctx: ReducerContext,
    attacker_id: u32,
    target_id: u32,
    damage: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let identity = ctx.sender;
    
    // Validate attacker exists and owns this identity
    let _attacker = match Player::filter_by_id(&attacker_id).next() {
        Some(p) if p.identity == identity => p,
        Some(_) => {
            log::warn!("Player {} hit processing rejected: identity mismatch", attacker_id);
            return Ok(());
        }
        None => {
            log::warn!("Player {} not found for hit processing", attacker_id);
            return Ok(());
        }
    };
    
    log::info!("Processing hit: attacker={}, target={}, damage={}", 
               attacker_id, target_id, damage);
    
    // Apply damage to target (could be enemy or player)
    apply_damage_to_enemy(target_id, damage, attacker_id, "Unknown".to_string())?;
    
    Ok(())
}

/// Create projectile helper function
pub fn create_projectile(
    player_id: u32,
    origin_x: f32,
    origin_y: f32,
    direction_x: f32,
    direction_y: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    log::info!("Creating projectile for player {} at ({}, {}) with direction ({}, {})", 
               player_id, origin_x, origin_y, direction_x, direction_y);
    
    // This is now handled by the create_projectile reducer
    Ok(())
}

/// Update all active projectiles (called periodically)
/// Requirements 4.3: Projectile collision with enemies
/// Requirements 4.4: Projectile collision with obstacles
#[spacetimedb(reducer)]
pub fn update_projectiles(
    _ctx: ReducerContext,
    delta_time: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let mut projectiles_to_remove = Vec::new();
    
    // Get all active projectiles
    for projectile in Projectile::iter() {
        if !projectile.is_active {
            continue;
        }
        
        let mut updated_projectile = projectile.clone();
        
        // Update position
        updated_projectile.position_x += updated_projectile.velocity_x * delta_time;
        updated_projectile.position_y += updated_projectile.velocity_y * delta_time;
        updated_projectile.time_to_live -= delta_time;
        
        // Check if projectile should be removed due to timeout
        if updated_projectile.time_to_live <= 0.0 {
            updated_projectile.is_active = false;
            projectiles_to_remove.push(updated_projectile.id);
            log::info!("Projectile {} expired due to timeout", updated_projectile.id);
            continue;
        }
        
        // Check collision with enemies
        let mut hit_enemy = false;
        for enemy in Enemy::filter_by_map_id(&updated_projectile.map_id) {
            if check_projectile_enemy_collision(&updated_projectile, &enemy) {
                // Apply damage to enemy
                apply_damage_to_enemy(
                    enemy.id, 
                    updated_projectile.damage, 
                    updated_projectile.owner_id, 
                    "Bow".to_string()
                )?;
                
                // Mark projectile for removal
                updated_projectile.is_active = false;
                projectiles_to_remove.push(updated_projectile.id);
                hit_enemy = true;
                
                log::info!("Projectile {} hit enemy {} for {} damage", 
                          updated_projectile.id, enemy.id, updated_projectile.damage);
                break;
            }
        }
        
        if hit_enemy {
            continue;
        }
        
        // TODO: Check collision with obstacles/map boundaries
        // For now, assume no obstacles
        
        // Update projectile in database
        Projectile::update_by_id(&updated_projectile.id, updated_projectile);
    }
    
    // Remove inactive projectiles
    for projectile_id in projectiles_to_remove {
        Projectile::delete_by_id(&projectile_id);
    }
    
    Ok(())
}

/// Check collision between projectile and enemy
/// Requirements 4.3: Projectile collision with enemies
fn check_projectile_enemy_collision(projectile: &Projectile, enemy: &Enemy) -> bool {
    let dx = projectile.position_x - enemy.position_x;
    let dy = projectile.position_y - enemy.position_y;
    let distance = (dx * dx + dy * dy).sqrt();
    
    distance <= PROJECTILE_COLLISION_RADIUS
}

/// Generate unique projectile ID
fn generate_projectile_id() -> u32 {
    use std::collections::hash_map::DefaultHasher;
    use std::hash::{Hash, Hasher};
    
    let mut hasher = DefaultHasher::new();
    std::time::SystemTime::now().hash(&mut hasher);
    ((hasher.finish() % u32::MAX as u64) as u32).wrapping_add(2000000) // Offset to avoid conflicts
}

/// Get all active projectiles in a map (for client synchronization)
#[spacetimedb(reducer)]
pub fn get_projectiles_in_map(
    _ctx: ReducerContext,
    map_id: String,
) -> Result<(), Box<dyn std::error::Error>> {
    let projectiles: Vec<Projectile> = Projectile::filter_by_map_id(&map_id)
        .filter(|p| p.is_active)
        .collect();
    
    log::info!("Found {} active projectiles in map {}", projectiles.len(), map_id);
    
    // Projectiles are automatically synchronized to clients via SpacetimeDB subscriptions
    Ok(())
}

/// Give arrows to a player for testing
#[spacetimedb(reducer)]
pub fn give_arrows_to_player(
    _ctx: ReducerContext,
    player_id: u32,
    quantity: i32,
) -> Result<(), Box<dyn std::error::Error>> {
    // Check if player already has arrows
    let existing_arrows: Vec<crate::inventory::InventoryItem> = crate::inventory::InventoryItem::filter_by_player_id(&player_id)
        .filter(|item| item.item_id == "arrow")
        .collect();
    
    if let Some(arrow_item) = existing_arrows.first() {
        // Update quantity
        let mut updated_arrow = arrow_item.clone();
        updated_arrow.quantity += quantity;
        crate::inventory::InventoryItem::delete_by_id(&arrow_item.id);
        crate::inventory::InventoryItem::insert(updated_arrow);
    } else {
        // Create new arrow entry
        let new_arrow = crate::inventory::InventoryItem {
            id: generate_inventory_id(),
            player_id,
            item_id: "arrow".to_string(),
            quantity,
            is_equipped: false,
        };
        crate::inventory::InventoryItem::insert(new_arrow);
    }
    
    log::info!("Gave {} arrows to player {}", quantity, player_id);
    Ok(())
}

/// Generate inventory ID (helper function)
fn generate_inventory_id() -> u32 {
    use std::collections::hash_map::DefaultHasher;
    use std::hash::{Hash, Hasher};
    
    let mut hasher = DefaultHasher::new();
    std::time::SystemTime::now().hash(&mut hasher);
    ((hasher.finish() % u32::MAX as u64) as u32).wrapping_add(3000000) // Offset to avoid conflicts
}