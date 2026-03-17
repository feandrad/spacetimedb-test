use crate::inventory::inventory_item;
use crate::player;
use spacetimedb::{reducer, table, ReducerContext, Table};

#[table(accessor = container, public)]
#[derive(Clone)]
pub struct Container {
    #[primary_key]
    pub id: u32,
    pub map_id: String,
    pub position_x: f32,
    pub position_y: f32,
    pub capacity: u32,
    pub label: String,
}

#[table(accessor = container_item, public)]
#[derive(Clone)]
pub struct ContainerItem {
    #[primary_key]
    pub id: u32,
    pub container_id: u32,
    pub item_id: String,
    pub quantity: i32,
    pub slot: u32,
}

const INTERACT_RANGE: f32 = 16.0;

#[reducer]
pub fn create_container(
    ctx: &ReducerContext,
    position_x: f32,
    position_y: f32,
    map_id: String,
    capacity: u32,
    label: String,
) -> Result<(), String> {
    let container = Container {
        id: generate_container_id(ctx),
        map_id: map_id.clone(),
        position_x,
        position_y,
        capacity,
        label: label.clone(),
    };

    ctx.db.container().insert(container);
    log::info!("Created container '{}' at ({}, {}) on map '{}'", label, position_x, position_y, map_id);
    Ok(())
}

#[reducer]
pub fn deposit_item(
    ctx: &ReducerContext,
    container_id: u32,
    item_id: String,
    quantity: i32,
) -> Result<(), String> {
    if quantity <= 0 {
        return Err("Quantity must be positive".to_string());
    }

    let identity = ctx.sender();
    let player = ctx.db.player().iter()
        .find(|p| p.identity == identity)
        .ok_or("Player not found")?;

    let container = ctx.db.container().id().find(&container_id)
        .ok_or("Container not found")?;

    // Validate same map
    if player.current_map_id != container.map_id {
        return Err("Player is not on the same map as the container".to_string());
    }

    // Validate range
    let dx = player.position_x - container.position_x;
    let dy = player.position_y - container.position_y;
    let dist = (dx * dx + dy * dy).sqrt();
    if dist > INTERACT_RANGE {
        return Err("Too far from container".to_string());
    }

    // Check player has the item
    let inv_item = ctx.db.inventory_item().iter()
        .find(|i| i.player_id == player.id && i.item_id == item_id && i.quantity >= quantity)
        .ok_or("Player does not have enough of this item")?;

    // Check container capacity
    let current_slots: u32 = ctx.db.container_item().iter()
        .filter(|ci| ci.container_id == container_id)
        .count() as u32;

    // Try to stack into existing slot first
    let existing_slot = ctx.db.container_item().iter()
        .find(|ci| ci.container_id == container_id && ci.item_id == item_id);

    if let Some(slot) = existing_slot {
        // Stack onto existing
        let mut updated = slot.clone();
        updated.quantity += quantity;
        ctx.db.container_item().id().delete(&slot.id);
        ctx.db.container_item().insert(updated);
    } else {
        // Need a new slot
        if current_slots >= container.capacity {
            return Err("Container is full".to_string());
        }

        let next_slot = current_slots;
        ctx.db.container_item().insert(ContainerItem {
            id: generate_container_item_id(ctx),
            container_id,
            item_id: item_id.clone(),
            quantity,
            slot: next_slot,
        });
    }

    // Remove from player inventory
    let mut updated_inv = inv_item.clone();
    updated_inv.quantity -= quantity;
    ctx.db.inventory_item().id().delete(&inv_item.id);
    if updated_inv.quantity > 0 {
        ctx.db.inventory_item().insert(updated_inv);
    }

    log::info!("Player {} deposited {}x{} into container {}", player.id, quantity, item_id, container_id);
    Ok(())
}

#[reducer]
pub fn withdraw_item(
    ctx: &ReducerContext,
    container_id: u32,
    item_id: String,
    quantity: i32,
) -> Result<(), String> {
    if quantity <= 0 {
        return Err("Quantity must be positive".to_string());
    }

    let identity = ctx.sender();
    let player = ctx.db.player().iter()
        .find(|p| p.identity == identity)
        .ok_or("Player not found")?;

    let container = ctx.db.container().id().find(&container_id)
        .ok_or("Container not found")?;

    // Validate same map
    if player.current_map_id != container.map_id {
        return Err("Player is not on the same map as the container".to_string());
    }

    // Validate range
    let dx = player.position_x - container.position_x;
    let dy = player.position_y - container.position_y;
    let dist = (dx * dx + dy * dy).sqrt();
    if dist > INTERACT_RANGE {
        return Err("Too far from container".to_string());
    }

    // Check container has the item
    let container_slot = ctx.db.container_item().iter()
        .find(|ci| ci.container_id == container_id && ci.item_id == item_id && ci.quantity >= quantity)
        .ok_or("Container does not have enough of this item")?;

    // Remove from container
    let mut updated_slot = container_slot.clone();
    updated_slot.quantity -= quantity;
    ctx.db.container_item().id().delete(&container_slot.id);
    if updated_slot.quantity > 0 {
        ctx.db.container_item().insert(updated_slot);
    }

    // Add to player inventory
    let existing_inv = ctx.db.inventory_item().iter()
        .find(|i| i.player_id == player.id && i.item_id == item_id);

    if let Some(inv) = existing_inv {
        let mut updated: crate::inventory::InventoryItem = inv.clone();
        updated.quantity += quantity;
        ctx.db.inventory_item().id().delete(&inv.id);
        ctx.db.inventory_item().insert(updated);
    } else {
        ctx.db.inventory_item().insert(crate::inventory::InventoryItem {
            id: generate_container_item_id(ctx),
            player_id: player.id,
            item_id: item_id.clone(),
            quantity,
            is_equipped: false,
            slot_type: crate::inventory::get_item_slot_type_pub(&item_id),
        });
    }

    log::info!("Player {} withdrew {}x{} from container {}", player.id, quantity, item_id, container_id);
    Ok(())
}

fn generate_container_id(ctx: &ReducerContext) -> u32 {
    use std::collections::hash_map::DefaultHasher;
    use std::hash::{Hash, Hasher};
    let mut hasher = DefaultHasher::new();
    ctx.timestamp.hash(&mut hasher);
    "container".hash(&mut hasher);
    ctx.db.container().iter().count().hash(&mut hasher);
    (hasher.finish() % u32::MAX as u64) as u32
}

fn generate_container_item_id(ctx: &ReducerContext) -> u32 {
    use std::collections::hash_map::DefaultHasher;
    use std::hash::{Hash, Hasher};
    let mut hasher = DefaultHasher::new();
    ctx.timestamp.hash(&mut hasher);
    "container_item".hash(&mut hasher);
    ctx.db.container_item().iter().count().hash(&mut hasher);
    (hasher.finish() % u32::MAX as u64) as u32
}
