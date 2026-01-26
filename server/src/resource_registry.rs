use spacetimedb::{table, reducer, ReducerContext, Table};
use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};

/// Resource Registry table for storing all game resources
/// Requirements 1.1: Store resources with unique ID and key_id mapping
/// Requirements 1.4: Maintain bidirectional mapping between IDs and key_ids
#[table(name = resource_registry, public)]
#[derive(Clone)]
pub struct ResourceRegistry {
    #[primary_key]
    pub id: u32,
    pub key_id: String,        // "core:overworld/farm"
    pub resource_type: String, // "map", "item", "npc"
    pub data: String,          // Serialized resource data
    pub hash_disambiguation: u32, // For handling hash collisions
}

/// Resource ID Mapping table for efficient key_id to ID lookups
/// Requirements 1.4: Maintain bidirectional mapping between IDs and key_ids
#[table(name = resource_id_mapping, public)]
#[derive(Clone)]
pub struct ResourceIdMapping {
    #[primary_key]
    pub key_id: String,
    pub resource_id: u32,
}

/// Generate a unique resource ID from key_id with collision handling
/// Requirements 1.3: Handle hash collisions automatically
fn generate_resource_id(ctx: &ReducerContext, key_id: &str) -> u32 {
    let mut hasher = DefaultHasher::new();
    key_id.hash(&mut hasher);
    let base_hash = (hasher.finish() % u32::MAX as u64) as u32;
    
    // Check for collisions and disambiguate
    let mut disambiguation = 0u32;
    loop {
        let final_id = base_hash.wrapping_add(disambiguation);
        
        // Check if this ID already exists
        if ctx.db.resource_registry().id().find(&final_id).is_none() {
            return final_id;
        }
        
        disambiguation += 1;
        
        // Prevent infinite loops (though extremely unlikely)
        if disambiguation > 1000 {
            log::error!("Too many hash collisions for key_id: {}", key_id);
            // Fall back to a simple counter-based approach
            return std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap_or_default()
                .as_nanos() as u32;
        }
    }
}

/// Register a new resource in the registry
/// Requirements 1.1: Store resource with unique ID and key_id mapping
/// Requirements 1.3: Handle hash collisions automatically
/// Requirements 1.4: Maintain bidirectional mapping
#[reducer]
pub fn register_resource(
    ctx: &ReducerContext,
    key_id: String,
    resource_type: String,
    data: String,
) -> Result<(), Box<dyn std::error::Error>> {
    // Check if resource already exists
    if ctx.db.resource_id_mapping().key_id().find(&key_id).is_some() {
        log::warn!("Resource with key_id {} already exists", key_id);
        return Err("Resource already registered".into());
    }
    
    // Validate resource type
    if !matches!(resource_type.as_str(), "map" | "item" | "npc") {
        return Err("Invalid resource type. Must be 'map', 'item', or 'npc'".into());
    }
    
    // Generate unique ID with collision handling
    let resource_id = generate_resource_id(ctx, &key_id);
    
    // Calculate disambiguation value for this specific collision resolution
    let mut hasher = DefaultHasher::new();
    key_id.hash(&mut hasher);
    let base_hash = (hasher.finish() % u32::MAX as u64) as u32;
    let disambiguation = resource_id.wrapping_sub(base_hash);
    
    // Create resource registry entry
    let resource = ResourceRegistry {
        id: resource_id,
        key_id: key_id.clone(),
        resource_type: resource_type.clone(),
        data: data.clone(),
        hash_disambiguation: disambiguation,
    };
    
    // Create ID mapping entry
    let mapping = ResourceIdMapping {
        key_id: key_id.clone(),
        resource_id,
    };
    
    // Insert both records
    ctx.db.resource_registry().insert(resource);
    ctx.db.resource_id_mapping().insert(mapping);
    
    log::info!("Registered resource: key_id={}, type={}, id={}", key_id, resource_type, resource_id);
    
    Ok(())
}

/// Retrieve a resource by its key_id
/// Requirements 1.2: Return corresponding resource data when requested by key_id
/// Requirements 1.4: Use bidirectional mapping for efficient lookup
#[reducer]
pub fn get_resource_by_key(
    ctx: &ReducerContext,
    key_id: String,
) -> Result<(), Box<dyn std::error::Error>> {
    // Look up the resource ID from the mapping table
    if let Some(mapping) = ctx.db.resource_id_mapping().key_id().find(&key_id) {
        // Get the resource data using the ID
        if let Some(resource) = ctx.db.resource_registry().id().find(&mapping.resource_id) {
            log::info!("Retrieved resource: key_id={}, type={}, id={}", 
                      resource.key_id, resource.resource_type, resource.id);
            // In a real implementation, this would return the resource data to the client
            // For now, we just log it as SpacetimeDB handles the response automatically
        } else {
            log::error!("Resource mapping exists but resource not found: key_id={}, id={}", 
                       key_id, mapping.resource_id);
            return Err("Resource data not found".into());
        }
    } else {
        log::warn!("Resource not found: key_id={}", key_id);
        return Err("Resource not found".into());
    }
    
    Ok(())
}

/// Retrieve a resource by its ID
/// Requirements 1.4: Support bidirectional mapping - lookup by ID
#[reducer]
pub fn get_resource_by_id(
    ctx: &ReducerContext,
    resource_id: u32,
) -> Result<(), Box<dyn std::error::Error>> {
    // Get the resource data directly by ID
    if let Some(resource) = ctx.db.resource_registry().id().find(&resource_id) {
        log::info!("Retrieved resource: id={}, key_id={}, type={}", 
                  resource.id, resource.key_id, resource.resource_type);
        // In a real implementation, this would return the resource data to the client
        // For now, we just log it as SpacetimeDB handles the response automatically
    } else {
        log::warn!("Resource not found: id={}", resource_id);
        return Err("Resource not found".into());
    }
    
    Ok(())
}

/// Update an existing resource's data
/// Requirements 1.1, 1.4: Allow updating resource data while maintaining mappings
#[reducer]
pub fn update_resource(
    ctx: &ReducerContext,
    key_id: String,
    new_data: String,
) -> Result<(), Box<dyn std::error::Error>> {
    // Look up the resource ID from the mapping table
    if let Some(mapping) = ctx.db.resource_id_mapping().key_id().find(&key_id) {
        // Get the existing resource
        if let Some(resource) = ctx.db.resource_registry().id().find(&mapping.resource_id) {
            // Create updated resource
            let mut updated_resource = resource.clone();
            updated_resource.data = new_data;
            
            // Delete old and insert updated
            ctx.db.resource_registry().id().delete(&mapping.resource_id);
            ctx.db.resource_registry().insert(updated_resource);
            
            log::info!("Updated resource: key_id={}, id={}", key_id, mapping.resource_id);
        } else {
            return Err("Resource data not found".into());
        }
    } else {
        return Err("Resource not found".into());
    }
    
    Ok(())
}

/// Remove a resource from the registry
/// Requirements 1.4: Maintain bidirectional mapping consistency during removal
#[reducer]
pub fn remove_resource(
    ctx: &ReducerContext,
    key_id: String,
) -> Result<(), Box<dyn std::error::Error>> {
    // Look up the resource ID from the mapping table
    if let Some(mapping) = ctx.db.resource_id_mapping().key_id().find(&key_id) {
        let resource_id = mapping.resource_id;
        
        // Remove both the resource and the mapping
        ctx.db.resource_registry().id().delete(&resource_id);
        ctx.db.resource_id_mapping().key_id().delete(&key_id);
        
        log::info!("Removed resource: key_id={}, id={}", key_id, resource_id);
    } else {
        return Err("Resource not found".into());
    }
    
    Ok(())
}

/// List all resources of a specific type
/// Requirements 1.1: Support querying resources by type
#[reducer]
pub fn list_resources_by_type(
    ctx: &ReducerContext,
    resource_type: String,
) -> Result<(), Box<dyn std::error::Error>> {
    // Validate resource type
    if !matches!(resource_type.as_str(), "map" | "item" | "npc") {
        return Err("Invalid resource type. Must be 'map', 'item', or 'npc'".into());
    }
    
    // Get all resources of the specified type
    let resources: Vec<ResourceRegistry> = ctx.db.resource_registry().iter()
        .filter(|r| r.resource_type == resource_type)
        .collect();
    
    log::info!("Found {} resources of type '{}'", resources.len(), resource_type);
    
    for resource in &resources {
        log::info!("  - key_id: {}, id: {}", resource.key_id, resource.id);
    }
    
    Ok(())
}

/// Sync the resource registry with clients
/// Requirements 1.5: Sync registry with clients when they connect
#[reducer]
pub fn sync_resource_registry(
    ctx: &ReducerContext,
) -> Result<(), Box<dyn std::error::Error>> {
    // Count total resources
    let total_resources = ctx.db.resource_registry().iter().count();
    let total_mappings = ctx.db.resource_id_mapping().iter().count();
    
    log::info!("Resource registry sync: {} resources, {} mappings", 
              total_resources, total_mappings);
    
    // In a real implementation, this would trigger sending the registry data to the client
    // For SpacetimeDB, the subscription system handles this automatically
    
    Ok(())
}