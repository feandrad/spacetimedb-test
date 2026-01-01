# Design Document

## Overview

O Guildmaster MVP é um jogo de ação e aventura multiplayer cooperativo com foco em exploração, combate e coleta de recursos. O design prioriza simplicidade visual (sem spritesheets complexos) enquanto mantém gameplay responsivo através de efeitos baseados em shaders e feedback visual claro.

O jogo utiliza uma arquitetura cliente-servidor autoritativa com SpacetimeDB para o backend e Godot 4 (C#) para o cliente, garantindo sincronização confiável entre até 4 jogadores simultâneos.

## Architecture

### Client-Server Model
- **Servidor Autoritativo**: SpacetimeDB valida todas as ações críticas (movimento, combate, inventário, **colisões**)
- **Cliente Predictivo**: Godot 4 (C#) com input prediction e reconciliação para responsividade
- **Comunicação**: SpacetimeDB SDK gerencia toda comunicação cliente-servidor
- **Sincronização**: Reducers (cliente → servidor) e Subscriptions (servidor → cliente)
- **Detecção de Colisão**: Toda detecção de colisão é processada pelo SpacetimeDB, incluindo movimento, combate e interações

### Core Systems
1. **Input Manager**: Sistema de controles remapeáveis console-first
2. **Movement System**: Movimento autoritativo com prediction e reconciliação
3. **Map System**: Navegação entre mapas conectados com instanciamento dinâmico
4. **Combat System**: Combate PvE com múltiplos tipos de armas
5. **Inventory System**: Gerenciamento de itens e equipamentos por jogador
6. **AI System**: State machine para comportamento de inimigos

## Components and Interfaces

### Input Manager
```csharp
interface IInputManager
{
    void RegisterAction(string actionName, InputBinding binding);
    bool IsActionPressed(string actionName);
    bool IsActionJustPressed(string actionName);
    Vector2 GetMovementVector();
    void RemapAction(string actionName, InputBinding newBinding);
}
```

### Interaction Manager
```csharp
interface IInteractionManager
{
    InteractableObject[] GetNearbyInteractables(PlayerId playerId, float range);
    ContextualAction[] GetAvailableActions(PlayerId playerId, InteractableId objectId);
    InteractionResult ExecuteContextualAction(PlayerId playerId, InteractableId objectId, ActionType actionType);
    bool ValidateActionRequirements(PlayerId playerId, ActionRequirement[] requirements);
}
```

### Movement System
```csharp
interface IMovementSystem
{
    void ProcessMovementInput(PlayerId playerId, Vector2 direction, float deltaTime, uint sequence);
    void ApplyServerCorrection(PlayerId playerId, Vector2 serverPosition, uint lastSequence);
    Vector2 PredictPosition(PlayerId playerId, Vector2 direction, float deltaTime);
    // Note: All collision detection is performed server-side by SpacetimeDB
}
```

### Combat System
```csharp
interface ICombatSystem
{
    void ExecuteAttack(PlayerId playerId, WeaponType weapon, Vector2 direction);
    void ProcessHit(PlayerId attackerId, EntityId targetId, float damage);
    void CreateProjectile(PlayerId playerId, Vector2 origin, Vector2 direction, ProjectileType type);
    // Note: All hit detection and collision checking is performed server-side by SpacetimeDB
}

enum WeaponType
{
    Sword,    // Wide cleave attacks
    Axe,      // High damage, frontal only
    Bow       // Projectile with ammo consumption
}
```

### Map System
```csharp
interface IMapSystem
{
    void TransitionToMap(PlayerId playerId, string mapId, Vector2 entryPoint);
    bool IsInTransitionZone(Vector2 position, out string destinationMapId);
    void LoadMapInstance(string mapId);
    void UnloadMapInstance(string mapId);
}
```

### Contextual Action System
```csharp
interface IInteractableObject
{
    InteractableId Id { get; }
    Vector2 Position { get; }
    float InteractionRange { get; }
    ContextualAction[] GetAvailableActions(PlayerId playerId);
    InteractionResult ExecuteAction(PlayerId playerId, ActionType actionType, ActionParameters parameters);
}

public struct ContextualAction
{
    public ActionType Type;
    public string DisplayName;
    public ActionParameters RequiredParameters;
    public ActionRequirement[] Requirements;
    public bool IsAvailable;
}

public struct ActionRequirement
{
    public RequirementType Type;
    public string ItemId;
    public bool MustBeEquipped;
    public int MinimumQuantity;
    public string Description;
}

public enum RequirementType
{
    EquippedWeapon,
    InventoryItem,
    PlayerState,
    ObjectState
}

public enum ActionType
{
    Shake,
    Cut,
    PickUp,
    Break,
    Fish,
    Jump
}

public struct InteractionResult
{
    public bool Success;
    public ItemDrop[] ItemsGenerated;
    public string Message;
    public ObjectStateChange[] StateChanges;
}

public struct ItemDrop
{
    public string ItemId;
    public int Quantity;
    public Vector2 SpawnPosition;
}

public struct ObjectStateChange
{
    public string Property;
    public object NewValue;
}
```

### Specific Object Implementations
```csharp
public class TreeObject : IInteractableObject
{
    public InteractableId Id { get; private set; }
    public Vector2 Position { get; private set; }
    public float InteractionRange => 2.0f;
    
    private int health = 3;
    private int fruitCount = 2;
    
    public ContextualAction[] GetAvailableActions(PlayerId playerId)
    {
        var actions = new List<ContextualAction>();
        
        if (fruitCount > 0)
        {
            actions.Add(new ContextualAction
            {
                Type = ActionType.Shake,
                DisplayName = "Shake",
                Requirements = new ActionRequirement[0], // No requirements for shaking
                IsAvailable = true
            });
        }
        
        if (health > 0)
        {
            actions.Add(new ContextualAction
            {
                Type = ActionType.Cut,
                DisplayName = "Cut",
                Requirements = new[]
                {
                    new ActionRequirement
                    {
                        Type = RequirementType.EquippedWeapon,
                        ItemId = "axe",
                        MustBeEquipped = true,
                        Description = "Requires equipped axe"
                    }
                },
                IsAvailable = false // Will be set based on player's equipment
            });
        }
        
        return actions.ToArray();
    }
    
    public InteractionResult ExecuteAction(PlayerId playerId, ActionType actionType, ActionParameters parameters)
    {
        // Validate requirements before execution
        var action = GetAvailableActions(playerId).FirstOrDefault(a => a.Type == actionType);
        if (action.Requirements != null && !ValidateRequirements(playerId, action.Requirements))
        {
            return new InteractionResult 
            { 
                Success = false, 
                Message = "Requirements not met: " + string.Join(", ", action.Requirements.Select(r => r.Description))
            };
        }
        
        switch (actionType)
        {
            case ActionType.Shake:
                return HandleShake();
            case ActionType.Cut:
                return HandleCut();
            default:
                return new InteractionResult { Success = false, Message = "Invalid action" };
        }
    }
    
    private bool ValidateRequirements(PlayerId playerId, ActionRequirement[] requirements)
    {
        // This would be implemented by the InteractionManager
        // For now, assume it's validated elsewhere
        return true;
    }
    
    private InteractionResult HandleShake()
    {
        if (fruitCount <= 0)
            return new InteractionResult { Success = false, Message = "No fruit to shake" };
            
        fruitCount--;
        return new InteractionResult
        {
            Success = true,
            ItemsGenerated = new[] { new ItemDrop { ItemId = "fruit", Quantity = 1 } },
            Message = "Shook fruit from tree"
        };
    }
    
    private InteractionResult HandleCut()
    {
        if (health <= 0)
            return new InteractionResult { Success = false, Message = "Tree already cut" };
            
        health--;
        var items = new List<ItemDrop> { new ItemDrop { ItemId = "wood", Quantity = 1 } };
        
        if (health == 0)
        {
            items.Add(new ItemDrop { ItemId = "wood", Quantity = 2 });
        }
        
        return new InteractionResult
        {
            Success = true,
            ItemsGenerated = items.ToArray(),
            Message = health == 0 ? "Tree cut down" : "Damaged tree"
        };
    }
}

public class RockObject : IInteractableObject
{
    public InteractableId Id { get; private set; }
    public Vector2 Position { get; private set; }
    public float InteractionRange => 1.5f;
    
    private bool isPickedUp = false;
    private int durability = 2;
    
    public ContextualAction[] GetAvailableActions(PlayerId playerId)
    {
        var actions = new List<ContextualAction>();
        
        if (!isPickedUp)
        {
            actions.Add(new ContextualAction
            {
                Type = ActionType.PickUp,
                DisplayName = "Pick Up",
                Requirements = new ActionRequirement[0], // No requirements for picking up
                IsAvailable = true
            });
        }
        
        if (durability > 0)
        {
            actions.Add(new ContextualAction
            {
                Type = ActionType.Break,
                DisplayName = "Break",
                Requirements = new[]
                {
                    new ActionRequirement
                    {
                        Type = RequirementType.EquippedWeapon,
                        ItemId = "pickaxe",
                        MustBeEquipped = true,
                        Description = "Requires equipped pickaxe"
                    }
                },
                IsAvailable = false // Will be set based on player's equipment
            });
        }
        
        return actions.ToArray();
    }
    
    public InteractionResult ExecuteAction(PlayerId playerId, ActionType actionType, ActionParameters parameters)
    {
        // Validate requirements before execution
        var action = GetAvailableActions(playerId).FirstOrDefault(a => a.Type == actionType);
        if (action.Requirements != null && !ValidateRequirements(playerId, action.Requirements))
        {
            return new InteractionResult 
            { 
                Success = false, 
                Message = "Requirements not met: " + string.Join(", ", action.Requirements.Select(r => r.Description))
            };
        }
        
        switch (actionType)
        {
            case ActionType.PickUp:
                return HandlePickUp();
            case ActionType.Break:
                return HandleBreak();
            default:
                return new InteractionResult { Success = false, Message = "Invalid action" };
        }
    }
    
    private bool ValidateRequirements(PlayerId playerId, ActionRequirement[] requirements)
    {
        // This would be implemented by the InteractionManager
        return true;
    }
    
    private InteractionResult HandlePickUp()
    {
        if (isPickedUp)
            return new InteractionResult { Success = false, Message = "Rock already picked up" };
            
        isPickedUp = true;
        return new InteractionResult
        {
            Success = true,
            ItemsGenerated = new[] { new ItemDrop { ItemId = "stone", Quantity = 1 } },
            StateChanges = new[] { new ObjectStateChange { Property = "visible", NewValue = false } },
            Message = "Picked up rock"
        };
    }
    
    private InteractionResult HandleBreak()
    {
        if (durability <= 0)
            return new InteractionResult { Success = false, Message = "Rock already broken" };
            
        durability--;
        var items = new List<ItemDrop> { new ItemDrop { ItemId = "stone_fragment", Quantity = 1 } };
        
        if (durability == 0)
        {
            items.Add(new ItemDrop { ItemId = "stone", Quantity = 1 });
        }
        
        return new InteractionResult
        {
            Success = true,
            ItemsGenerated = items.ToArray(),
            Message = durability == 0 ? "Rock broken completely" : "Chipped rock"
        };
    }
}
```

### Inventory System
```csharp
interface IInventorySystem
{
    bool AddItem(PlayerId playerId, ItemId itemId, int quantity);
    bool RemoveItem(PlayerId playerId, ItemId itemId, int quantity);
    bool EquipWeapon(PlayerId playerId, WeaponId weaponId);
    bool EquipTool(PlayerId playerId, ToolId toolId);
    WeaponId GetEquippedWeapon(PlayerId playerId);
    ToolId GetEquippedTool(PlayerId playerId);
    bool HasEquippedItem(PlayerId playerId, string itemId);
    int GetAmmoCount(PlayerId playerId, AmmoType ammoType);
    int GetItemCount(PlayerId playerId, string itemId);
    bool MeetsRequirements(PlayerId playerId, ActionRequirement[] requirements);
}
```

### Enemy AI System
```csharp
interface IEnemyAI
{
    void UpdateState(EnemyId enemyId, float deltaTime);
    void SetTarget(EnemyId enemyId, PlayerId targetId);
    void ClearTarget(EnemyId enemyId);
}

enum EnemyState
{
    Idle,     // Patrolling, no players detected
    Alert,    // Investigating last known player position
    Chasing   // Actively pursuing player with line of sight
}
```

## Data Models

### Player Data
```csharp
public struct PlayerData
{
    public PlayerId Id;
    public Vector2 Position;
    public Vector2 Velocity;
    public string CurrentMapId;
    public float Health;
    public float MaxHealth;
    public bool IsDowned;
    public WeaponId EquippedWeapon;
    public uint LastInputSequence;
}
```

### Map Data
```csharp
public struct MapData
{
    public string MapId;
    public Vector2 Size;
    public List<TransitionZone> Transitions;
    public List<Vector2> SpawnPoints;
    public List<InteractableObject> Objects;
}

public struct TransitionZone
{
    public Rectangle Area;
    public string DestinationMapId;
    public Vector2 DestinationPoint;
}

public struct InteractableObject
{
    public InteractableId Id;
    public ObjectType Type;
    public Vector2 Position;
    public float InteractionRange;
    public ObjectState CurrentState;
}

public enum ObjectType
{
    Tree,
    Rock,
    WaterEdge,
    WaterDeep,
    Block
}

public struct ObjectState
{
    public Dictionary<string, object> Properties; // Health, resources, etc.
    public bool IsDestroyed;
    public float RespawnTimer;
}
```

### Combat Data
```csharp
public struct WeaponData
{
    public WeaponId Id;
    public WeaponType Type;
    public float Damage;
    public float Range;
    public float AttackSpeed;
    public Shape HitArea; // Different shapes for sword (wide), axe (narrow), bow (point)
}

public struct ProjectileData
{
    public ProjectileId Id;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Damage;
    public PlayerId OwnerId;
    public float TimeToLive;
}
```

### Enemy Data
```csharp
public struct EnemyData
{
    public EnemyId Id;
    public Vector2 Position;
    public EnemyState State;
    public float Health;
    public float MaxHealth;
    public Vector2 PatrolCenter;
    public float PatrolRadius;
    public float DetectionRange;
    public float LeashRange;
    public PlayerId? TargetPlayerId;
    public Vector2 LastKnownPlayerPosition;
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Movement Direction Consistency
*For any* movement input direction, the player should move in the corresponding direction when the input is applied.
**Validates: Requirements 1.2**

### Property 2: Movement Stop on Input Release
*For any* movement input release, the player should stop smoothly when the input is no longer active.
**Validates: Requirements 1.4**

### Property 3: Client-Server Position Reconciliation
*For any* position conflict between client and server, the client should smoothly correct to match the authoritative server position.
**Validates: Requirements 1.7**

### Property 4: Map Transition Activation
*For any* player approaching a map transition zone, the system should initiate a transition to the connected map.
**Validates: Requirements 2.2**

### Property 5: State Preservation During Transitions
*For any* map transition, the player's position and state should be maintained and properly transferred to the destination map.
**Validates: Requirements 2.3**

### Property 6: Multiplayer Position Synchronization
*For any* multiple players in the same map, all players should see consistent positions and actions for each other.
**Validates: Requirements 2.4**

### Property 7: Map Transition Spawning
*For any* player transitioning to a new map, the player should be spawned at the appropriate entry point in the destination map.
**Validates: Requirements 2.6**

### Property 8: Sword Cleave Attack Behavior
*For any* sword attack, all enemies within the cleave area should take damage from the attack.
**Validates: Requirements 3.1**

### Property 9: Axe Frontal Attack Behavior
*For any* axe attack, only enemies directly in front of the player should take damage, and the damage should be higher than sword damage.
**Validates: Requirements 3.2**

### Property 10: Bow Projectile Creation
*For any* bow attack, a projectile should be created that travels in the aimed direction and consumes ammunition.
**Validates: Requirements 3.3**

### Property 11: Weapon Damage Application
*For any* weapon attack that hits an enemy, the enemy should take damage appropriate to the weapon type.
**Validates: Requirements 3.5**

### Property 12: Attack Movement Prevention
*For any* weapon attack being performed, player movement should be prevented during the attack animation.
**Validates: Requirements 3.6**

### Property 13: Projectile Direction and Creation
*For any* bow firing action, a projectile should be created that travels in the aimed direction.
**Validates: Requirements 4.1**

### Property 14: Ammunition Consumption
*For any* projectile fired, exactly one unit of ammunition should be consumed from the player's inventory.
**Validates: Requirements 4.2**

### Property 15: Projectile Enemy Hit Behavior
*For any* projectile hitting an enemy, the enemy should take damage and the projectile should be removed.
**Validates: Requirements 4.3**

### Property 16: Projectile Obstacle Collision
*For any* projectile hitting a solid obstacle, the projectile should be removed without dealing damage.
**Validates: Requirements 4.4**

### Property 17: Ammunition Depletion Prevention
*For any* attempt to fire when ammunition is depleted, the firing action should be prevented.
**Validates: Requirements 4.5**

### Property 18: Item Pickup Addition
*For any* item pickup when inventory space is available, the item should be added to the player's inventory.
**Validates: Requirements 5.2**

### Property 19: Weapon Equipping Behavior
*For any* weapon equipping action, the active weapon should be updated and the appropriate combat behavior should be enabled.
**Validates: Requirements 5.3**

### Property 20: Full Inventory Prevention
*For any* attempt to pick up items when inventory is full, the pickup should be prevented.
**Validates: Requirements 5.5**

### Property 21: Contextual Action Execution
*For any* contextual action performed by the player, the appropriate interaction should be executed in the game world.
**Validates: Requirements 6.4**

### Property 22: Contextual Action Item Generation
*For any* contextual action that yields items, the items should be added to the player's inventory if space is available.
**Validates: Requirements 6.6**

### Property 23: Friendly Fire Prevention
*For any* player attack, other players should not take damage from the attack.
**Validates: Requirements 7.3**

### Property 24: Player Body Blocking Disabled
*For any* player movement, players should be able to move through each other without collision blocking.
**Validates: Requirements 7.4**

### Property 25: Cooperative Enemy Attacks
*For any* enemy being attacked, multiple players should be able to attack the same enemy simultaneously.
**Validates: Requirements 7.5**

### Property 26: Individual Player Inventories
*For any* inventory operation, each player should have their own separate inventory state that doesn't affect other players.
**Validates: Requirements 7.6**

### Property 27: Enemy Idle State Behavior
*For any* enemy with no nearby players, the enemy should remain in Idle state and patrol within predefined areas.
**Validates: Requirements 8.2**

### Property 28: Enemy Alert State Transition
*For any* player entering an enemy's detection range, the enemy should transition to Alert state.
**Validates: Requirements 8.3**

### Property 29: Enemy Alert Investigation
*For any* enemy in Alert state, the enemy should move toward the player's last known position.
**Validates: Requirements 8.4**

### Property 30: Enemy Chasing State Transition
*For any* enemy with line of sight to a player, the enemy should transition to Chasing state and move toward the player.
**Validates: Requirements 8.5**

### Property 31: Enemy Damage Dealing
*For any* enemy in Chasing state that reaches a player, the enemy should deal damage to the player.
**Validates: Requirements 8.6**

### Property 32: Enemy Leash Range Return
*For any* player leaving an enemy's leash range, the enemy should return to Idle state.
**Validates: Requirements 8.7**

### Property 33: Enemy Health and Damage
*For any* enemy taking damage from player attacks, the enemy's health should be reduced appropriately.
**Validates: Requirements 8.8**

### Property 34: Enemy Death Behavior
*For any* enemy whose health reaches zero, the enemy should be removed from the game world.
**Validates: Requirements 8.9**

### Property 35: Player Damage Application
*For any* player taking damage, the player's current health should be reduced.
**Validates: Requirements 9.2**

### Property 36: Player Downed State Trigger
*For any* player whose health reaches zero, the player should enter a downed state.
**Validates: Requirements 9.3**

### Property 37: Player Revival Mechanics
*For any* downed player, other players should be able to revive them.
**Validates: Requirements 9.4**

### Property 38: Invincibility Frames
*For any* player taking damage, the player should have temporary invincibility frames preventing immediate additional damage.
**Validates: Requirements 9.5**

### Property 39: Health Consumable Restoration
*For any* health consumable item used, the player's health should be restored appropriately.
**Validates: Requirements 9.6**

## Error Handling

### Network Errors
- **Connection Loss**: Client maintains local state and attempts reconnection with state synchronization
- **Packet Loss**: Redundant state updates and sequence numbering for reliable delivery
- **Desynchronization**: Server correction mechanisms with smooth interpolation

### Combat Errors
- **Invalid Attacks**: Server validates weapon equipped, ammunition available, and cooldown status
- **Hit Detection Conflicts**: Server-authoritative hit detection with client prediction (all collision detection by SpacetimeDB)
- **Damage Calculation Errors**: Server validates all damage calculations and applies anti-cheat measures

### Collision Detection Errors
- **Movement Collisions**: SpacetimeDB handles all movement collision detection and validation
- **Combat Collisions**: SpacetimeDB processes all weapon hit detection and projectile collisions
- **Interaction Collisions**: SpacetimeDB validates all player-object and player-environment interactions

### Map System Errors
- **Invalid Transitions**: Validate transition zones and destination maps before allowing transitions
- **Map Loading Failures**: Fallback to safe spawn points and error reporting
- **Instance Management**: Proper cleanup of map instances and player state migration

### Inventory Errors
- **Invalid Item Operations**: Validate item existence, quantities, and inventory space
- **Duplication Prevention**: Server-authoritative item state with operation validation
- **Equipment Conflicts**: Validate equipment compatibility and requirements

## Testing Strategy

### Dual Testing Approach
The testing strategy employs both unit tests and property-based tests as complementary approaches:

- **Unit Tests**: Verify specific examples, edge cases, and error conditions
- **Property Tests**: Verify universal properties across all inputs using randomized testing
- **Integration Tests**: Verify system interactions and multiplayer synchronization

### Property-Based Testing Configuration
- **Testing Framework**: Use fast-check for TypeScript/JavaScript or FsCheck for C# property-based testing
- **Test Iterations**: Minimum 100 iterations per property test to ensure comprehensive coverage
- **Test Tagging**: Each property test must reference its design document property using the format:
  - **Feature: guildmaster-mvp, Property {number}: {property_text}**

### Unit Testing Focus
Unit tests should concentrate on:
- Specific weapon behavior examples (sword cleave patterns, axe damage calculations)
- Edge cases in inventory management (full inventory, invalid items)
- Error conditions in map transitions and network failures
- Integration points between combat, movement, and inventory systems

### Property Testing Focus
Property tests should verify:
- Universal movement and combat behaviors across all valid inputs
- Inventory operations with randomized item types and quantities
- Enemy AI state transitions with various player configurations
- Multiplayer synchronization with different player counts and actions

### Performance Testing
- **Network Latency**: Test responsiveness under various network conditions
- **Concurrent Players**: Verify stability with maximum player count (4 players)
- **Map Transitions**: Ensure smooth transitions without performance degradation
- **Combat Intensity**: Test system performance during intense combat scenarios