use spacetimedb::{spacetimedb, ReducerContext};

// Inventory system tables and reducers
// Requirements 5.1, 5.2, 5.3, 5.4, 5.5
// Requirements 6.4, 6.5, 6.6: Contextual action validation and execution

#[spacetimedb(table)]
#[derive(Clone)]
pub struct InventoryItem {
    #[spacetimedb(primary_key)]
    pub id: u32,
    pub player_id: u32,
    pub item_id: String,
    pub quantity: i32,
    pub is_equipped: bool,
    pub slot_type: String, // "weapon", "tool", "consumable", etc.
}

#[spacetimedb(table)]
#[derive(Clone)]
pub struct PlayerEquipment {
    #[spacetimedb(primary_key)]
    pub player_id: u32,
    pub main_hand_weapon: String,
    pub off_hand_tool: String,
    pub armor: String,
    pub accessory: String,
}

// Interactable objects in the world
// Requirements 6.1, 6.2, 6.3: Object types with contextual actions
#[spacetimedb(table)]
#[derive(Clone)]
pub struct InteractableObject {
    #[spacetimedb(primary_key)]
    pub id: u32,
    pub object_type: String, // "tree", "rock", etc.
    pub position_x: f32,
    pub position_y: f32,
    pub map_id: String,
    pub health: i32,
    pub max_health: i32,
    pub resource_count: i32, // fruit count for trees, etc.
    pub is_destroyed: bool,
    pub respawn_timer: f32,
}

// Action requirements for validation
#[derive(Clone)]
pub struct ActionRequirement {
    pub requirement_type: String, // "equipped_weapon", "inventory_item", etc.
    pub item_id: String,
    pub must_be_equipped: bool,
    pub minimum_quantity: i32,
}

#[spacetimedb(reducer)]
pub fn add_item_to_inventory(
    ctx: ReducerContext,
    player_id: u32,
    item_id: String,
    quantity: i32,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    // TODO: Validate player ownership and inventory space
    // Requirements 5.5: Prevent picking up when inventory is full
    
    // Check if item already exists in inventory
    let existing_items: Vec<InventoryItem> = InventoryItem::filter_by_player_id(&player_id)
        .filter(|item| item.item_id == item_id)
        .collect();
    
    if let Some(existing_item) = existing_items.first() {
        // Update quantity
        let mut updated_item = existing_item.clone();
        updated_item.quantity += quantity;
        InventoryItem::delete_by_id(&existing_item.id);
        InventoryItem::insert(updated_item);
    } else {
        // Create new inventory entry
        let new_item = InventoryItem {
            id: generate_inventory_id(),
            player_id,
            item_id: item_id.clone(),
            quantity,
            is_equipped: false,
            slot_type: get_item_slot_type(&item_id),
        };
        InventoryItem::insert(new_item);
    }
    
    log::info!("Added {} x{} to player {}'s inventory", item_id, quantity, player_id);
    
    Ok(())
}

#[spacetimedb(reducer)]
pub fn equip_item(
    ctx: ReducerContext,
    player_id: u32,
    item_id: String,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    // Requirements 5.3: Update active weapon and enable combat behavior
    
    // Check if player has the item
    let items: Vec<InventoryItem> = InventoryItem::filter_by_player_id(&player_id)
        .filter(|item| item.item_id == item_id)
        .collect();
    
    if let Some(item) = items.first() {
        // Get or create player equipment
        let equipment_entries: Vec<PlayerEquipment> = PlayerEquipment::filter_by_player_id(&player_id).collect();
        let mut equipment = if let Some(eq) = equipment_entries.first() {
            eq.clone()
        } else {
            PlayerEquipment {
                player_id,
                main_hand_weapon: String::new(),
                off_hand_tool: String::new(),
                armor: String::new(),
                accessory: String::new(),
            }
        };
        
        // Determine equipment slot based on item type
        match item.slot_type.as_str() {
            "weapon" => {
                // Unequip current weapon if any
                if !equipment.main_hand_weapon.is_empty() {
                    unequip_item_internal(player_id, &equipment.main_hand_weapon)?;
                }
                equipment.main_hand_weapon = item_id.clone();
            },
            "tool" => {
                // Unequip current tool if any
                if !equipment.off_hand_tool.is_empty() {
                    unequip_item_internal(player_id, &equipment.off_hand_tool)?;
                }
                equipment.off_hand_tool = item_id.clone();
            },
            "armor" => {
                if !equipment.armor.is_empty() {
                    unequip_item_internal(player_id, &equipment.armor)?;
                }
                equipment.armor = item_id.clone();
            },
            "accessory" => {
                if !equipment.accessory.is_empty() {
                    unequip_item_internal(player_id, &equipment.accessory)?;
                }
                equipment.accessory = item_id.clone();
            },
            _ => {
                return Err("Item cannot be equipped".into());
            }
        }
        
        // Update equipment table
        if equipment_entries.is_empty() {
            PlayerEquipment::insert(equipment);
        } else {
            PlayerEquipment::delete_by_player_id(&player_id);
            PlayerEquipment::insert(equipment);
        }
        
        // Mark item as equipped
        let mut updated_item = item.clone();
        updated_item.is_equipped = true;
        InventoryItem::delete_by_id(&item.id);
        InventoryItem::insert(updated_item);
        
        log::info!("Player {} equipped {}", player_id, item_id);
    } else {
        return Err("Player does not have this item".into());
    }
    
    Ok(())
}

#[spacetimedb(reducer)]
pub fn unequip_item(
    ctx: ReducerContext,
    player_id: u32,
    item_id: String,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    unequip_item_internal(player_id, &item_id)
}

fn unequip_item_internal(player_id: u32, item_id: &str) -> Result<(), Box<dyn std::error::Error>> {
    // Get player equipment
    let equipment_entries: Vec<PlayerEquipment> = PlayerEquipment::filter_by_player_id(&player_id).collect();
    if let Some(mut equipment) = equipment_entries.first().cloned() {
        // Remove from appropriate slot
        if equipment.main_hand_weapon == item_id {
            equipment.main_hand_weapon = String::new();
        } else if equipment.off_hand_tool == item_id {
            equipment.off_hand_tool = String::new();
        } else if equipment.armor == item_id {
            equipment.armor = String::new();
        } else if equipment.accessory == item_id {
            equipment.accessory = String::new();
        } else {
            return Err("Item not equipped".into());
        }
        
        // Update equipment table
        PlayerEquipment::delete_by_player_id(&player_id);
        PlayerEquipment::insert(equipment);
        
        // Mark item as not equipped
        let items: Vec<InventoryItem> = InventoryItem::filter_by_player_id(&player_id)
            .filter(|item| item.item_id == item_id)
            .collect();
        
        if let Some(item) = items.first() {
            let mut updated_item = item.clone();
            updated_item.is_equipped = false;
            InventoryItem::delete_by_id(&item.id);
            InventoryItem::insert(updated_item);
        }
        
        log::info!("Player {} unequipped {}", player_id, item_id);
    }
    
    Ok(())
}

#[spacetimedb(reducer)]
pub fn pickup_item(
    ctx: ReducerContext,
    player_id: u32,
    item_id: String,
    quantity: i32,
    _position_x: f32,
    _position_y: f32,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    // Requirements 5.2: Add items to available inventory space
    // Requirements 5.5: Prevent picking up when inventory is full
    
    // TODO: Validate inventory space before adding
    // For now, just add the item
    add_item_to_inventory(ctx, player_id, item_id, quantity)
}

#[spacetimedb(reducer)]
pub fn execute_contextual_action(
    ctx: ReducerContext,
    player_id: u32,
    object_id: u32,
    action_type: String,
) -> Result<(), Box<dyn std::error::Error>> {
    let _identity = ctx.sender;
    
    // Requirements 6.4: Execute appropriate interactions
    // Requirements 6.5: Server validates all contextual actions
    // Requirements 6.6: Handle item generation and object state changes
    
    // Get the interactable object
    let objects: Vec<InteractableObject> = InteractableObject::filter_by_id(&object_id).collect();
    let object = objects.first().ok_or("Object not found")?;
    
    // Get player position for range validation
    let players: Vec<crate::Player> = crate::Player::filter_by_id(&player_id).collect();
    let player = players.first().ok_or("Player not found")?;
    
    // Validate interaction range
    let distance = ((object.position_x - player.position_x).powi(2) + 
                   (object.position_y - player.position_y).powi(2)).sqrt();
    let max_range = get_interaction_range(&object.object_type);
    
    if distance > max_range {
        return Err("Player too far from object".into());
    }
    
    // Validate action requirements
    let requirements = get_action_requirements(&object.object_type, &action_type);
    if !validate_action_requirements(player_id, &requirements)? {
        return Err("Action requirements not met".into());
    }
    
    // Execute the action based on object type and action
    match (object.object_type.as_str(), action_type.as_str()) {
        ("tree", "shake") => execute_tree_shake(player_id, object_id)?,
        ("tree", "cut") => execute_tree_cut(player_id, object_id)?,
        ("rock", "pick_up") => execute_rock_pickup(player_id, object_id)?,
        ("rock", "break") => execute_rock_break(player_id, object_id)?,
        _ => return Err("Invalid action for object type".into()),
    }
    
    log::info!("Player {} executed action {} on object {}", player_id, action_type, object_id);
    
    Ok(())
}

// Tree interaction implementations
fn execute_tree_shake(player_id: u32, object_id: u32) -> Result<(), Box<dyn std::error::Error>> {
    let objects: Vec<InteractableObject> = InteractableObject::filter_by_id(&object_id).collect();
    let mut object = objects.first().ok_or("Object not found")?.clone();
    
    if object.resource_count <= 0 {
        return Err("No fruit to shake".into());
    }
    
    // Reduce fruit count
    object.resource_count -= 1;
    
    // Update object state
    InteractableObject::delete_by_id(&object_id);
    InteractableObject::insert(object);
    
    // Generate fruit item
    add_item_to_inventory_internal(player_id, "fruit".to_string(), 1)?;
    
    log::info!("Player {} shook fruit from tree {}", player_id, object_id);
    Ok(())
}

fn execute_tree_cut(player_id: u32, object_id: u32) -> Result<(), Box<dyn std::error::Error>> {
    let objects: Vec<InteractableObject> = InteractableObject::filter_by_id(&object_id).collect();
    let mut object = objects.first().ok_or("Object not found")?.clone();
    
    if object.health <= 0 {
        return Err("Tree already cut down".into());
    }
    
    // Reduce tree health
    object.health -= 1;
    
    // Generate wood
    add_item_to_inventory_internal(player_id, "wood".to_string(), 1)?;
    
    // If tree is fully cut down, give extra wood
    if object.health <= 0 {
        add_item_to_inventory_internal(player_id, "wood".to_string(), 2)?;
        object.is_destroyed = true;
        log::info!("Player {} cut down tree {} completely", player_id, object_id);
    } else {
        log::info!("Player {} damaged tree {}", player_id, object_id);
    }
    
    // Update object state
    InteractableObject::delete_by_id(&object_id);
    InteractableObject::insert(object);
    
    Ok(())
}

// Rock interaction implementations
fn execute_rock_pickup(player_id: u32, object_id: u32) -> Result<(), Box<dyn std::error::Error>> {
    let objects: Vec<InteractableObject> = InteractableObject::filter_by_id(&object_id).collect();
    let mut object = objects.first().ok_or("Object not found")?.clone();
    
    if object.is_destroyed {
        return Err("Rock already picked up".into());
    }
    
    // Mark rock as picked up
    object.is_destroyed = true;
    
    // Update object state
    InteractableObject::delete_by_id(&object_id);
    InteractableObject::insert(object);
    
    // Generate stone item
    add_item_to_inventory_internal(player_id, "stone".to_string(), 1)?;
    
    log::info!("Player {} picked up rock {}", player_id, object_id);
    Ok(())
}

fn execute_rock_break(player_id: u32, object_id: u32) -> Result<(), Box<dyn std::error::Error>> {
    let objects: Vec<InteractableObject> = InteractableObject::filter_by_id(&object_id).collect();
    let mut object = objects.first().ok_or("Object not found")?.clone();
    
    if object.health <= 0 {
        return Err("Rock already broken".into());
    }
    
    // Reduce rock durability
    object.health -= 1;
    
    // Generate stone fragment
    add_item_to_inventory_internal(player_id, "stone_fragment".to_string(), 1)?;
    
    // If rock is fully broken, give extra stone
    if object.health <= 0 {
        add_item_to_inventory_internal(player_id, "stone".to_string(), 1)?;
        object.is_destroyed = true;
        log::info!("Player {} broke rock {} completely", player_id, object_id);
    } else {
        log::info!("Player {} chipped rock {}", player_id, object_id);
    }
    
    // Update object state
    InteractableObject::delete_by_id(&object_id);
    InteractableObject::insert(object);
    
    Ok(())
}

// Helper function to validate action requirements
fn validate_action_requirements(player_id: u32, requirements: &[ActionRequirement]) -> Result<bool, Box<dyn std::error::Error>> {
    for requirement in requirements {
        match requirement.requirement_type.as_str() {
            "equipped_weapon" => {
                if requirement.must_be_equipped {
                    let equipment: Vec<PlayerEquipment> = PlayerEquipment::filter_by_player_id(&player_id).collect();
                    if let Some(eq) = equipment.first() {
                        if eq.main_hand_weapon != requirement.item_id && eq.off_hand_tool != requirement.item_id {
                            return Ok(false);
                        }
                    } else {
                        return Ok(false);
                    }
                }
            },
            "inventory_item" => {
                let items: Vec<InventoryItem> = InventoryItem::filter_by_player_id(&player_id)
                    .filter(|item| item.item_id == requirement.item_id)
                    .collect();
                
                let total_quantity: i32 = items.iter().map(|item| item.quantity).sum();
                if total_quantity < requirement.minimum_quantity {
                    return Ok(false);
                }
            },
            _ => {
                // Unknown requirement type, assume not met
                return Ok(false);
            }
        }
    }
    
    Ok(true)
}

// Get action requirements for specific object type and action
fn get_action_requirements(object_type: &str, action_type: &str) -> Vec<ActionRequirement> {
    match (object_type, action_type) {
        ("tree", "shake") => vec![], // No requirements for shaking
        ("tree", "cut") => vec![
            ActionRequirement {
                requirement_type: "equipped_weapon".to_string(),
                item_id: "axe".to_string(),
                must_be_equipped: true,
                minimum_quantity: 1,
            }
        ],
        ("rock", "pick_up") => vec![], // No requirements for picking up
        ("rock", "break") => vec![
            ActionRequirement {
                requirement_type: "equipped_weapon".to_string(),
                item_id: "pickaxe".to_string(),
                must_be_equipped: true,
                minimum_quantity: 1,
            }
        ],
        _ => vec![], // Default: no requirements
    }
}

// Get interaction range for object type
fn get_interaction_range(object_type: &str) -> f32 {
    match object_type {
        "tree" => 2.0,
        "rock" => 1.5,
        _ => 1.0,
    }
}

// Internal helper to add items without context
fn add_item_to_inventory_internal(player_id: u32, item_id: String, quantity: i32) -> Result<(), Box<dyn std::error::Error>> {
    // Check if item already exists in inventory
    let existing_items: Vec<InventoryItem> = InventoryItem::filter_by_player_id(&player_id)
        .filter(|item| item.item_id == item_id)
        .collect();
    
    if let Some(existing_item) = existing_items.first() {
        // Update quantity
        let mut updated_item = existing_item.clone();
        updated_item.quantity += quantity;
        InventoryItem::delete_by_id(&existing_item.id);
        InventoryItem::insert(updated_item);
    } else {
        // Create new inventory entry
        let new_item = InventoryItem {
            id: generate_inventory_id(),
            player_id,
            item_id: item_id.clone(),
            quantity,
            is_equipped: false,
            slot_type: get_item_slot_type(&item_id),
        };
        InventoryItem::insert(new_item);
    }
    
    Ok(())
}

// Reducer to create interactable objects (for testing/setup)
#[spacetimedb(reducer)]
pub fn create_interactable_object(
    _ctx: ReducerContext,
    object_type: String,
    position_x: f32,
    position_y: f32,
    map_id: String,
) -> Result<(), Box<dyn std::error::Error>> {
    let (health, max_health, resource_count) = match object_type.as_str() {
        "tree" => (3, 3, 2), // 3 health, 2 fruit
        "rock" => (2, 2, 0), // 2 durability, no resources
        _ => (1, 1, 0),
    };
    
    let object = InteractableObject {
        id: generate_object_id(),
        object_type,
        position_x,
        position_y,
        map_id,
        health,
        max_health,
        resource_count,
        is_destroyed: false,
        respawn_timer: 0.0,
    };
    
    InteractableObject::insert(object.clone());
    log::info!("Created interactable object: {:?}", object.id);
    
    Ok(())
}

// Simple ID generation for objects
fn generate_object_id() -> u32 {
    use std::collections::hash_map::DefaultHasher;
    use std::hash::{Hash, Hasher};
    
    let mut hasher = DefaultHasher::new();
    std::time::SystemTime::now().hash(&mut hasher);
    ((hasher.finish() % u32::MAX as u64) as u32).wrapping_add(1000) // Offset to avoid collision with other IDs
}

// Helper functions

fn get_item_slot_type(item_id: &str) -> String {
    match item_id {
        "sword" | "axe" | "bow" => "weapon".to_string(),
        "pickaxe" => "tool".to_string(),
        "arrow" => "ammunition".to_string(),
        "wood" | "stone" | "stone_fragment" => "material".to_string(),
        "fruit" => "consumable".to_string(),
        _ => "misc".to_string(),
    }
}

// Simple ID generation for inventory items
fn generate_inventory_id() -> u32 {
    use std::collections::hash_map::DefaultHasher;
    use std::hash::{Hash, Hasher};
    
    let mut hasher = DefaultHasher::new();
    std::time::SystemTime::now().hash(&mut hasher);
    (hasher.finish() % u32::MAX as u64) as u32
}