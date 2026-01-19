using Godot;
using System.Collections.Generic;
using System.Linq;
using GuildmasterMVP.Data;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Manages contextual interactions and item pickups
    /// Requirements 6.1, 6.4, 6.5, 6.6, 5.2, 5.3, 5.5
    /// </summary>
    public partial class InteractionManager : Node, IInteractionManager
    {
        private IInventorySystem _inventorySystem;
        private SpacetimeDBClient _dbClient;
        private Dictionary<uint, IInteractableObject> _interactableObjects = new Dictionary<uint, IInteractableObject>();
        private uint _nextObjectId = 1;
        
        public override void _Ready()
        {
            _inventorySystem = GetNode<IInventorySystem>("/root/GameManager/InventorySystem");
            _dbClient = GameManager.Instance?.DbClient;
            
            if (_inventorySystem == null)
            {
                GD.PrintErr("InteractionManager: Could not find InventorySystem");
            }
            
            if (_dbClient == null)
            {
                GD.PrintErr("InteractionManager: Could not find SpacetimeDB client");
            }
            
            GD.Print("InteractionManager initialized");
        }
        
        /// <summary>
        /// Get nearby interactable objects
        /// Requirements 6.1: Display available contextual actions
        /// </summary>
        public IInteractableObject[] GetNearbyInteractables(uint playerId, float range)
        {
            // TODO: Get player position from player system
            Vector2 playerPosition = Vector2.Zero; // Placeholder
            
            var nearbyObjects = new List<IInteractableObject>();
            
            foreach (var obj in _interactableObjects.Values)
            {
                if (obj.IsInRange(playerPosition))
                {
                    nearbyObjects.Add(obj);
                }
            }
            
            return nearbyObjects.ToArray();
        }
        
        /// <summary>
        /// Get available actions for specific object
        /// </summary>
        public ContextualAction[] GetAvailableActions(uint playerId, uint objectId)
        {
            if (!_interactableObjects.ContainsKey(objectId))
            {
                return new ContextualAction[0];
            }
            
            var obj = _interactableObjects[objectId];
            var actions = obj.GetAvailableActions(playerId);
            
            // Validate requirements for each action
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i].IsAvailable = ValidateActionRequirements(playerId, actions[i].Requirements);
            }
            
            return actions;
        }
        
        /// <summary>
        /// Execute contextual action
        /// Requirements 6.4: Execute appropriate interactions
        /// Requirements 6.6: Add items to inventory when actions yield items
        /// </summary>
        public InteractionResult ExecuteContextualAction(uint playerId, uint objectId, ActionType actionType)
        {
            if (!_interactableObjects.ContainsKey(objectId))
            {
                return new InteractionResult(false, "Object not found");
            }
            
            var obj = _interactableObjects[objectId];
            var availableActions = GetAvailableActions(playerId, objectId);
            var action = availableActions.FirstOrDefault(a => a.Type == actionType);
            
            if (!action.IsAvailable)
            {
                return new InteractionResult(false, "Action not available or requirements not met");
            }
            
            // Execute the action
            var result = obj.ExecuteAction(playerId, actionType, action.Parameters);
            
            // Handle item drops
            if (result.Success && result.ItemsGenerated != null)
            {
                foreach (var itemDrop in result.ItemsGenerated)
                {
                    bool pickedUp = PickupItem(playerId, itemDrop.ItemId, itemDrop.Quantity, itemDrop.SpawnPosition);
                    if (!pickedUp)
                    {
                        GD.Print($"Could not pickup {itemDrop.ItemId} x{itemDrop.Quantity} - inventory full");
                        // TODO: Drop item on ground for later pickup
                    }
                }
            }
            
            // TODO: Send interaction to server for validation
            if (_dbClient != null)
            {
                try
                {
                    // Convert ActionType enum to string for server
                    string actionTypeStr = actionType.ToString().ToLower();
                    _dbClient.CallReducerAsync("execute_contextual_action", playerId, objectId, actionTypeStr);
                    GD.Print($"Sent contextual action to server: player={playerId}, object={objectId}, action={actionTypeStr}");
                }
                catch (System.Exception ex)
                {
                    GD.PrintErr($"Failed to send contextual action to server: {ex.Message}");
                }
            }
            else
            {
                GD.PrintErr("SpacetimeDB client not available for server validation");
            }
            
            return result;
        }
        
        /// <summary>
        /// Validate action requirements
        /// Requirements 5.4: Equipment validation for contextual actions
        /// </summary>
        public bool ValidateActionRequirements(uint playerId, ActionRequirement[] requirements)
        {
            if (_inventorySystem == null)
            {
                return false;
            }
            
            return _inventorySystem.MeetsRequirements(playerId, requirements);
        }
        
        /// <summary>
        /// Pickup item from world
        /// Requirements 5.2: Add items to available inventory space
        /// Requirements 5.5: Prevent picking up when inventory is full
        /// </summary>
        public bool PickupItem(uint playerId, string itemId, int quantity, Vector2 position)
        {
            if (_inventorySystem == null)
            {
                return false;
            }
            
            // Check if player can pickup the item
            if (!CanPickupItem(playerId, itemId, quantity))
            {
                return false;
            }
            
            // Add item to inventory
            bool success = _inventorySystem.AddItem(playerId, itemId, quantity);
            
            if (success)
            {
                GD.Print($"Player {playerId} picked up {quantity} {itemId}");
                
                // Send pickup to server for validation and synchronization
                if (_dbClient != null)
                {
                    try
                    {
                        _dbClient.CallReducerAsync("pickup_item", playerId, itemId, quantity, position.X, position.Y);
                        GD.Print($"Sent pickup to server: player={playerId}, item={itemId}, quantity={quantity}");
                    }
                    catch (System.Exception ex)
                    {
                        GD.PrintErr($"Failed to send pickup to server: {ex.Message}");
                    }
                }
                else
                {
                    GD.PrintErr("SpacetimeDB client not available for server validation");
                }
            }
            
            return success;
        }
        
        /// <summary>
        /// Check if player can pickup item
        /// </summary>
        public bool CanPickupItem(uint playerId, string itemId, int quantity)
        {
            if (_inventorySystem == null)
            {
                return false;
            }
            
            // Check inventory space
            return _inventorySystem.HasInventorySpace(playerId, 1); // Simplified - assumes 1 slot per item type
        }
        
        /// <summary>
        /// Switch to different weapon
        /// Requirements 5.3: Update active weapon and enable combat behavior
        /// </summary>
        public bool SwitchWeapon(uint playerId, string newWeaponId)
        {
            if (_inventorySystem == null)
            {
                return false;
            }
            
            // Check if player has the weapon
            if (_inventorySystem.GetItemCount(playerId, newWeaponId) <= 0)
            {
                GD.Print($"Player {playerId} does not have weapon {newWeaponId}");
                return false;
            }
            
            // Equip the new weapon
            bool success = _inventorySystem.EquipWeapon(playerId, newWeaponId);
            
            if (success)
            {
                GD.Print($"Player {playerId} switched to weapon {newWeaponId}");
            }
            
            return success;
        }
        
        /// <summary>
        /// Switch to different tool
        /// </summary>
        public bool SwitchTool(uint playerId, string newToolId)
        {
            if (_inventorySystem == null)
            {
                return false;
            }
            
            // Check if player has the tool
            if (_inventorySystem.GetItemCount(playerId, newToolId) <= 0)
            {
                GD.Print($"Player {playerId} does not have tool {newToolId}");
                return false;
            }
            
            // Equip the new tool
            bool success = _inventorySystem.EquipTool(playerId, newToolId);
            
            if (success)
            {
                GD.Print($"Player {playerId} switched to tool {newToolId}");
            }
            
            return success;
        }
        
        /// <summary>
        /// Toggle equipment (equip if not equipped, unequip if equipped)
        /// </summary>
        public bool ToggleEquipment(uint playerId, string itemId)
        {
            if (_inventorySystem == null)
            {
                return false;
            }
            
            // Check if item is currently equipped
            if (_inventorySystem.HasEquippedItem(playerId, itemId))
            {
                // Unequip the item
                return _inventorySystem.UnequipItem(playerId, itemId);
            }
            else
            {
                // Try to equip the item
                if (_inventorySystem.CanEquipItem(playerId, itemId))
                {
                    // Determine if it's a weapon or tool and equip accordingly
                    // This is simplified - in practice you'd check item type
                    return _inventorySystem.EquipWeapon(playerId, itemId) || _inventorySystem.EquipTool(playerId, itemId);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Register an interactable object
        /// </summary>
        public void RegisterInteractableObject(IInteractableObject obj)
        {
            uint id = _nextObjectId++;
            _interactableObjects[id] = obj;
            
            // Register object with server
            if (_dbClient != null)
            {
                try
                {
                    string objectType = GetObjectTypeFromInterface(obj);
                    _dbClient.CallReducerAsync("create_interactable_object", objectType, obj.Position.X, obj.Position.Y, "default_map");
                    GD.Print($"Registered object {id} with server: type={objectType}, position=({obj.Position.X}, {obj.Position.Y})");
                }
                catch (System.Exception ex)
                {
                    GD.PrintErr($"Failed to register object with server: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Get object type string from interface implementation
        /// </summary>
        private string GetObjectTypeFromInterface(IInteractableObject obj)
        {
            if (obj is TreeObject)
                return "tree";
            else if (obj is RockObject)
                return "rock";
            else
                return "unknown";
        }
        
        /// <summary>
        /// Unregister an interactable object
        /// </summary>
        public void UnregisterInteractableObject(uint objectId)
        {
            _interactableObjects.Remove(objectId);
        }
    }
}