using Godot;
using System.Collections.Generic;
using GuildmasterMVP.Core;

namespace GuildmasterMVP.Data
{
    /// <summary>
    /// Inventory data structures for equipment tracking
    /// Requirements 5.1: Storage slots for items and equipment
    /// Requirements 5.4: Track ammunition quantities for projectile weapons
    /// </summary>
    
    public struct InventoryData
    {
        public uint PlayerId;
        public Dictionary<string, InventorySlot> Items;
        public EquipmentSlots Equipment;
        public int MaxSlots;
        public int UsedSlots;
        
        public InventoryData(uint playerId, int maxSlots = 20)
        {
            PlayerId = playerId;
            Items = new Dictionary<string, InventorySlot>();
            Equipment = new EquipmentSlots();
            MaxSlots = maxSlots;
            UsedSlots = 0;
        }
        
        public bool HasSpace(int requiredSlots = 1)
        {
            return UsedSlots + requiredSlots <= MaxSlots;
        }
        
        public bool HasItem(string itemId, int quantity = 1)
        {
            return Items.ContainsKey(itemId) && Items[itemId].Quantity >= quantity;
        }
    }
    
    public struct InventorySlot
    {
        public string ItemId;
        public int Quantity;
        public ItemType Type;
        public ItemRarity Rarity;
        public bool IsStackable;
        public int MaxStackSize;
        
        public InventorySlot(string itemId, int quantity, ItemType type, bool isStackable = true, int maxStackSize = 99)
        {
            ItemId = itemId;
            Quantity = quantity;
            Type = type;
            Rarity = ItemRarity.Common;
            IsStackable = isStackable;
            MaxStackSize = maxStackSize;
        }
        
        public bool CanAddQuantity(int amount)
        {
            if (!IsStackable) return false;
            return Quantity + amount <= MaxStackSize;
        }
    }
    
    public struct EquipmentSlots
    {
        public string MainHandWeapon;
        public string OffHandTool;
        public string Armor;
        public string Accessory;
        
        public EquipmentSlots()
        {
            MainHandWeapon = "";
            OffHandTool = "";
            Armor = "";
            Accessory = "";
        }
        
        public bool HasWeaponEquipped()
        {
            return !string.IsNullOrEmpty(MainHandWeapon);
        }
        
        public bool HasToolEquipped()
        {
            return !string.IsNullOrEmpty(OffHandTool);
        }
        
        public bool IsItemEquipped(string itemId)
        {
            return MainHandWeapon == itemId || 
                   OffHandTool == itemId || 
                   Armor == itemId || 
                   Accessory == itemId;
        }
        
        public string GetEquippedWeapon()
        {
            return MainHandWeapon;
        }
        
        public string GetEquippedTool()
        {
            return OffHandTool;
        }
    }
    
    public enum ItemType
    {
        Weapon,
        Tool,
        Consumable,
        Material,
        Ammunition,
        Armor,
        Accessory
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    public enum EquipmentSlot
    {
        MainHand,
        OffHand,
        Armor,
        Accessory
    }
    
    /// <summary>
    /// Item definition data for validation and behavior
    /// </summary>
    public struct ItemDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public ItemType Type;
        public ItemRarity Rarity;
        public bool IsStackable;
        public int MaxStackSize;
        public EquipmentSlot? EquipmentSlot;
        public WeaponType? WeaponType;
        public AmmoType? AmmoType;
        public bool CanBeEquipped;
        
        public ItemDefinition(string id, string name, ItemType type, bool canBeEquipped = false)
        {
            Id = id;
            Name = name;
            Description = "";
            Type = type;
            Rarity = ItemRarity.Common;
            IsStackable = type != ItemType.Weapon && type != ItemType.Tool && type != ItemType.Armor;
            MaxStackSize = IsStackable ? 99 : 1;
            EquipmentSlot = null;
            WeaponType = null;
            AmmoType = null;
            CanBeEquipped = canBeEquipped;
        }
    }
    
    /// <summary>
    /// Contextual action system data structures
    /// Requirements 6.1: Display available contextual actions
    /// Requirements 6.4: Execute appropriate interactions
    /// </summary>
    public struct ContextualAction
    {
        public ActionType Type;
        public string DisplayName;
        public ActionParameters Parameters;
        public ActionRequirement[] Requirements;
        public bool IsAvailable;
        
        public ContextualAction(ActionType type, string displayName, ActionRequirement[] requirements = null)
        {
            Type = type;
            DisplayName = displayName;
            Parameters = new ActionParameters();
            Requirements = requirements ?? new ActionRequirement[0];
            IsAvailable = true;
        }
    }
    
    public struct ActionParameters
    {
        public Dictionary<string, object> Values;
        
        public ActionParameters()
        {
            Values = new Dictionary<string, object>();
        }
        
        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            if (Values.ContainsKey(key) && Values[key] is T)
            {
                return (T)Values[key];
            }
            return defaultValue;
        }
        
        public void SetValue<T>(string key, T value)
        {
            Values[key] = value;
        }
    }
    
    public enum ActionType
    {
        Shake,
        Cut,
        PickUp,
        Break,
        Fish,
        Jump,
        Interact
    }
    
    /// <summary>
    /// Interaction result data
    /// Requirements 6.6: Add items to inventory when contextual actions yield items
    /// </summary>
    public struct InteractionResult
    {
        public bool Success;
        public ItemDrop[] ItemsGenerated;
        public string Message;
        public ObjectStateChange[] StateChanges;
        
        public InteractionResult(bool success, string message = "")
        {
            Success = success;
            ItemsGenerated = new ItemDrop[0];
            Message = message;
            StateChanges = new ObjectStateChange[0];
        }
    }
    
    public struct ItemDrop
    {
        public string ItemId;
        public int Quantity;
        public Vector2 SpawnPosition;
        
        public ItemDrop(string itemId, int quantity, Vector2 spawnPosition = default(Vector2))
        {
            ItemId = itemId;
            Quantity = quantity;
            SpawnPosition = spawnPosition;
        }
    }
    
    public struct ObjectStateChange
    {
        public string Property;
        public object NewValue;
        
        public ObjectStateChange(string property, object newValue)
        {
            Property = property;
            NewValue = newValue;
        }
    }
}