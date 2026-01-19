using System;
using Godot;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Inventory system interface with equipment tracking
    /// Requirements 5.1, 5.2, 5.3, 5.4, 5.5
    /// </summary>
    public interface IInventorySystem
    {
        // Signals
        [Signal]
        delegate void ItemAddedEventHandler(uint playerId, string itemId, int quantity);
        
        [Signal]
        delegate void ItemRemovedEventHandler(uint playerId, string itemId, int quantity);
        
        [Signal]
        delegate void WeaponEquippedEventHandler(uint playerId, string weaponId);
        
        [Signal]
        delegate void ToolEquippedEventHandler(uint playerId, string toolId);
        
        // Basic inventory operations
        bool AddItem(uint playerId, string itemId, int quantity);
        bool RemoveItem(uint playerId, string itemId, int quantity);
        
        // Equipment management - Requirements 5.3
        bool EquipWeapon(uint playerId, string weaponId);
        bool EquipTool(uint playerId, string toolId);
        bool UnequipItem(uint playerId, string itemId);
        
        // Equipment queries
        string GetEquippedWeapon(uint playerId);
        string GetEquippedTool(uint playerId);
        bool HasEquippedItem(uint playerId, string itemId);
        EquipmentSlots GetEquipmentSlots(uint playerId);
        
        // Inventory queries
        int GetAmmoCount(uint playerId, AmmoType ammoType);
        int GetItemCount(uint playerId, string itemId);
        bool HasInventorySpace(uint playerId, int requiredSlots = 1);
        InventoryData GetInventoryData(uint playerId);
        
        // Validation - Requirements 5.4, 5.5
        bool MeetsRequirements(uint playerId, ActionRequirement[] requirements);
        bool CanEquipItem(uint playerId, string itemId);
        bool ValidateEquipment(uint playerId, string itemId, EquipmentSlot slot);
        
        // Initialization
        void InitializePlayerInventory(uint playerId);
    }

    public struct ActionRequirement
    {
        public RequirementType Type;
        public string ItemId;
        public bool MustBeEquipped;
        public int MinimumQuantity;
        public string Description;
        
        public ActionRequirement(RequirementType type, string itemId, bool mustBeEquipped = false, int minimumQuantity = 1, string description = "")
        {
            Type = type;
            ItemId = itemId;
            MustBeEquipped = mustBeEquipped;
            MinimumQuantity = minimumQuantity;
            Description = description;
        }
    }

    public enum RequirementType
    {
        EquippedWeapon,
        InventoryItem,
        PlayerState,
        ObjectState
    }
}