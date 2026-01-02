using Godot;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Interface for managing contextual interactions and item pickups
    /// Requirements 6.1, 6.4, 6.5, 6.6
    /// </summary>
    public interface IInteractionManager
    {
        // Interaction discovery
        IInteractableObject[] GetNearbyInteractables(uint playerId, float range);
        ContextualAction[] GetAvailableActions(uint playerId, uint objectId);
        
        // Interaction execution
        InteractionResult ExecuteContextualAction(uint playerId, uint objectId, ActionType actionType);
        bool ValidateActionRequirements(uint playerId, ActionRequirement[] requirements);
        
        // Item pickup management - Requirements 5.2, 5.5
        bool PickupItem(uint playerId, string itemId, int quantity, Vector2 position);
        bool CanPickupItem(uint playerId, string itemId, int quantity);
        
        // Equipment management - Requirements 5.3
        bool SwitchWeapon(uint playerId, string newWeaponId);
        bool SwitchTool(uint playerId, string newToolId);
        bool ToggleEquipment(uint playerId, string itemId);
        
        // Additional methods used by SystemIntegrationManager
        void RegisterInteractableObject(IInteractableObject obj);
        void UnregisterInteractableObject(uint objectId);
    }
    
    /// <summary>
    /// Interface for interactable objects in the world
    /// Requirements 6.1: Display available contextual actions
    /// Requirements 6.4: Execute appropriate interactions
    /// </summary>
    public interface IInteractableObject
    {
        uint Id { get; }
        Vector2 Position { get; }
        float InteractionRange { get; }
        
        ContextualAction[] GetAvailableActions(uint playerId);
        InteractionResult ExecuteAction(uint playerId, ActionType actionType, ActionParameters parameters);
        bool IsInRange(Vector2 playerPosition);
    }
}