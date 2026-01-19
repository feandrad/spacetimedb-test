using Godot;
using System.Collections.Generic;
using GuildmasterMVP.Data;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Client-side inventory system with server synchronization
    /// Implements Requirements 5.1, 5.2, 5.3, 5.4, 5.5
    /// Requirements 7.6: Individual player inventories (each player has separate inventory state)
    /// </summary>
    public partial class InventorySystem : Node, IInventorySystem
    {
        [Signal]
        public delegate void ItemAddedEventHandler(uint playerId, string itemId, int quantity);
        
        [Signal]
        public delegate void ItemRemovedEventHandler(uint playerId, string itemId, int quantity);
        
        [Signal]
        public delegate void WeaponEquippedEventHandler(uint playerId, string weaponId);
        
        [Signal]
        public delegate void ToolEquippedEventHandler(uint playerId, string toolId);
        
        // Individual player inventories - Requirements 7.6: Separate inventories per player
        private Dictionary<uint, InventoryData> _playerInventories = new Dictionary<uint, InventoryData>();
        private Dictionary<string, ItemDefinition> _itemDefinitions = new Dictionary<string, ItemDefinition>();
        private SpacetimeDBClient _dbClient;
        
        public override void _Ready()
        {
            // Get reference to SpacetimeDB client
            _dbClient = GameManager.Instance?.DbClient;
            if (_dbClient == null)
            {
                GD.PrintErr("InventorySystem: Could not find SpacetimeDB client");
            }
            
            InitializeItemDefinitions();
            GD.Print("InventorySystem initialized with equipment tracking");
        }
        
        /// <summary>
        /// Initialize item definitions for validation
        /// </summary>
        private void InitializeItemDefinitions()
        {
            // Weapons
            _itemDefinitions["sword"] = new ItemDefinition("sword", "Sword", ItemType.Weapon, true);
            _itemDefinitions["axe"] = new ItemDefinition("axe", "Axe", ItemType.Weapon, true);
            _itemDefinitions["bow"] = new ItemDefinition("bow", "Bow", ItemType.Weapon, true);
            
            // Tools
            _itemDefinitions["pickaxe"] = new ItemDefinition("pickaxe", "Pickaxe", ItemType.Tool, true);
            
            // Ammunition
            var arrowDef = new ItemDefinition("arrow", "Arrow", ItemType.Ammunition, false);
            arrowDef.AmmoType = AmmoType.Arrow;
            _itemDefinitions["arrow"] = arrowDef;
            
            // Materials
            _itemDefinitions["wood"] = new ItemDefinition("wood", "Wood", ItemType.Material);
            _itemDefinitions["stone"] = new ItemDefinition("stone", "Stone", ItemType.Material);
            _itemDefinitions["stone_fragment"] = new ItemDefinition("stone_fragment", "Stone Fragment", ItemType.Material);
            
            // Consumables
            _itemDefinitions["fruit"] = new ItemDefinition("fruit", "Fruit", ItemType.Consumable);
            _itemDefinitions["health_potion"] = new ItemDefinition("health_potion", "Health Potion", ItemType.Consumable);
            _itemDefinitions["mega_health_potion"] = new ItemDefinition("mega_health_potion", "Mega Health Potion", ItemType.Consumable);
        }
        
        /// <summary>
        /// Add item to player inventory
        /// Requirements 5.2: Add items to available inventory space
        /// Requirements 5.5: Prevent picking up when inventory is full
        /// </summary>
        public bool AddItem(uint playerId, string itemId, int quantity)
        {
            if (!_playerInventories.ContainsKey(playerId))
            {
                _playerInventories[playerId] = new InventoryData(playerId);
            }
            
            var inventory = _playerInventories[playerId];
            
            // Check if we have space
            if (!HasInventorySpace(playerId, 1))
            {
                GD.Print($"Cannot add {itemId} to player {playerId}: inventory full");
                return false;
            }
            
            // Get item definition for validation
            if (!_itemDefinitions.ContainsKey(itemId))
            {
                GD.PrintErr($"Unknown item: {itemId}");
                return false;
            }
            
            var itemDef = _itemDefinitions[itemId];
            
            if (inventory.Items.ContainsKey(itemId))
            {
                var existingSlot = inventory.Items[itemId];
                if (existingSlot.CanAddQuantity(quantity))
                {
                    existingSlot.Quantity += quantity;
                    inventory.Items[itemId] = existingSlot;
                }
                else
                {
                    GD.Print($"Cannot add {quantity} {itemId}: would exceed stack limit");
                    return false;
                }
            }
            else
            {
                var newSlot = new InventorySlot(itemId, quantity, itemDef.Type, itemDef.IsStackable, itemDef.MaxStackSize);
                inventory.Items[itemId] = newSlot;
                inventory.UsedSlots++;
            }
            
            _playerInventories[playerId] = inventory;
            
            GD.Print($"Added {quantity} {itemId} to player {playerId} inventory");
            
            // Emit item added signal
            EmitSignal(SignalName.ItemAdded, playerId, itemId, quantity);
            
            // TODO: Send to server for validation
            // _dbClient?.CallReducerAsync("add_item_to_inventory", playerId, itemId, quantity);
            
            return true;
        }
        
        /// <summary>
        /// Remove item from player inventory
        /// </summary>
        public bool RemoveItem(uint playerId, string itemId, int quantity)
        {
            if (!_playerInventories.ContainsKey(playerId) || 
                !_playerInventories[playerId].Items.ContainsKey(itemId))
            {
                return false;
            }
            
            var inventory = _playerInventories[playerId];
            var slot = inventory.Items[itemId];
            
            if (slot.Quantity < quantity)
            {
                return false; // Not enough items
            }
            
            slot.Quantity -= quantity;
            
            if (slot.Quantity <= 0)
            {
                inventory.Items.Remove(itemId);
                inventory.UsedSlots--;
            }
            else
            {
                inventory.Items[itemId] = slot;
            }
            
            _playerInventories[playerId] = inventory;
            
            GD.Print($"Removed {quantity} {itemId} from player {playerId} inventory");
            
            // Emit item removed signal
            EmitSignal(SignalName.ItemRemoved, playerId, itemId, quantity);
            
            // TODO: Send to server for validation
            
            return true;
        }
        
        /// <summary>
        /// Equip a weapon
        /// Requirements 5.3: Update active weapon and enable combat behavior
        /// </summary>
        public bool EquipWeapon(uint playerId, string weaponId)
        {
            if (!CanEquipItem(playerId, weaponId))
            {
                return false;
            }
            
            if (!_playerInventories.ContainsKey(playerId))
            {
                return false;
            }
            
            var inventory = _playerInventories[playerId];
            
            // Unequip current weapon if any
            if (!string.IsNullOrEmpty(inventory.Equipment.MainHandWeapon))
            {
                UnequipItem(playerId, inventory.Equipment.MainHandWeapon);
            }
            
            inventory.Equipment.MainHandWeapon = weaponId;
            _playerInventories[playerId] = inventory;
            
            GD.Print($"Player {playerId} equipped weapon {weaponId}");
            
            // Emit weapon equipped signal
            EmitSignal(SignalName.WeaponEquipped, playerId, weaponId);
            
            GD.Print($"Player {playerId} equipped weapon {weaponId}");
            
            // TODO: Send to server for validation
            // _dbClient?.CallReducerAsync("equip_item", playerId, weaponId);
            
            return true;
        }
        
        /// <summary>
        /// Equip a tool
        /// </summary>
        public bool EquipTool(uint playerId, string toolId)
        {
            if (!CanEquipItem(playerId, toolId))
            {
                return false;
            }
            
            if (!_playerInventories.ContainsKey(playerId))
            {
                return false;
            }
            
            var inventory = _playerInventories[playerId];
            
            // Unequip current tool if any
            if (!string.IsNullOrEmpty(inventory.Equipment.OffHandTool))
            {
                UnequipItem(playerId, inventory.Equipment.OffHandTool);
            }
            
            inventory.Equipment.OffHandTool = toolId;
            _playerInventories[playerId] = inventory;
            
            GD.Print($"Player {playerId} equipped tool {toolId}");
            
            // Emit tool equipped signal
            EmitSignal(SignalName.ToolEquipped, playerId, toolId);
            
            return true;
        }
        
        /// <summary>
        /// Unequip an item
        /// </summary>
        public bool UnequipItem(uint playerId, string itemId)
        {
            if (!_playerInventories.ContainsKey(playerId))
            {
                return false;
            }
            
            var inventory = _playerInventories[playerId];
            
            if (inventory.Equipment.MainHandWeapon == itemId)
            {
                inventory.Equipment.MainHandWeapon = "";
            }
            else if (inventory.Equipment.OffHandTool == itemId)
            {
                inventory.Equipment.OffHandTool = "";
            }
            else if (inventory.Equipment.Armor == itemId)
            {
                inventory.Equipment.Armor = "";
            }
            else if (inventory.Equipment.Accessory == itemId)
            {
                inventory.Equipment.Accessory = "";
            }
            else
            {
                return false; // Item not equipped
            }
            
            _playerInventories[playerId] = inventory;
            
            GD.Print($"Player {playerId} unequipped {itemId}");
            
            return true;
        }
        
        /// <summary>
        /// Get equipped weapon for player
        /// </summary>
        public string GetEquippedWeapon(uint playerId)
        {
            if (!_playerInventories.ContainsKey(playerId))
            {
                return "";
            }
            
            return _playerInventories[playerId].Equipment.GetEquippedWeapon();
        }
        
        /// <summary>
        /// Get equipped tool for player
        /// </summary>
        public string GetEquippedTool(uint playerId)
        {
            if (!_playerInventories.ContainsKey(playerId))
            {
                return "";
            }
            
            return _playerInventories[playerId].Equipment.GetEquippedTool();
        }
        
        /// <summary>
        /// Check if player has specific item equipped
        /// </summary>
        public bool HasEquippedItem(uint playerId, string itemId)
        {
            if (!_playerInventories.ContainsKey(playerId))
            {
                return false;
            }
            
            return _playerInventories[playerId].Equipment.IsItemEquipped(itemId);
        }
        
        /// <summary>
        /// Get equipment slots for player
        /// </summary>
        public EquipmentSlots GetEquipmentSlots(uint playerId)
        {
            if (!_playerInventories.ContainsKey(playerId))
            {
                return new EquipmentSlots();
            }
            
            return _playerInventories[playerId].Equipment;
        }
        
        /// <summary>
        /// Get ammunition count for specific ammo type
        /// Requirements 4.2: Track ammunition quantities
        /// Requirements 4.5: Prevent firing when ammunition is depleted
        /// </summary>
        public int GetAmmoCount(uint playerId, AmmoType ammoType)
        {
            string itemId = ammoType.ToString().ToLower();
            return GetItemCount(playerId, itemId);
        }
        
        /// <summary>
        /// Get item count for specific item
        /// </summary>
        public int GetItemCount(uint playerId, string itemId)
        {
            if (!_playerInventories.ContainsKey(playerId) || 
                !_playerInventories[playerId].Items.ContainsKey(itemId))
            {
                return 0;
            }
            
            return _playerInventories[playerId].Items[itemId].Quantity;
        }
        
        /// <summary>
        /// Check if player has inventory space
        /// Requirements 5.5: Prevent picking up when inventory is full
        /// </summary>
        public bool HasInventorySpace(uint playerId, int requiredSlots = 1)
        {
            if (!_playerInventories.ContainsKey(playerId))
            {
                return true; // New inventory has space
            }
            
            return _playerInventories[playerId].HasSpace(requiredSlots);
        }
        
        /// <summary>
        /// Get complete inventory data for player
        /// </summary>
        public InventoryData GetInventoryData(uint playerId)
        {
            if (!_playerInventories.ContainsKey(playerId))
            {
                return new InventoryData(playerId);
            }
            
            return _playerInventories[playerId];
        }
        
        /// <summary>
        /// Check if player meets action requirements
        /// Requirements 5.4: Equipment validation for contextual actions
        /// </summary>
        public bool MeetsRequirements(uint playerId, ActionRequirement[] requirements)
        {
            foreach (var requirement in requirements)
            {
                switch (requirement.Type)
                {
                    case RequirementType.EquippedWeapon:
                        if (requirement.MustBeEquipped && !HasEquippedItem(playerId, requirement.ItemId))
                            return false;
                        break;
                    case RequirementType.InventoryItem:
                        if (GetItemCount(playerId, requirement.ItemId) < requirement.MinimumQuantity)
                            return false;
                        break;
                    // TODO: Handle other requirement types
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if item can be equipped by player
        /// </summary>
        public bool CanEquipItem(uint playerId, string itemId)
        {
            // Check if player has the item
            if (!HasItem(playerId, itemId))
            {
                return false;
            }
            
            // Check if item can be equipped
            if (!_itemDefinitions.ContainsKey(itemId))
            {
                return false;
            }
            
            var itemDef = _itemDefinitions[itemId];
            return itemDef.CanBeEquipped;
        }
        
        /// <summary>
        /// Validate equipment for specific slot
        /// </summary>
        public bool ValidateEquipment(uint playerId, string itemId, EquipmentSlot slot)
        {
            if (!_itemDefinitions.ContainsKey(itemId))
            {
                return false;
            }
            
            var itemDef = _itemDefinitions[itemId];
            
            // Check if item type matches slot
            switch (slot)
            {
                case EquipmentSlot.MainHand:
                    return itemDef.Type == ItemType.Weapon;
                case EquipmentSlot.OffHand:
                    return itemDef.Type == ItemType.Tool;
                case EquipmentSlot.Armor:
                    return itemDef.Type == ItemType.Armor;
                case EquipmentSlot.Accessory:
                    return itemDef.Type == ItemType.Accessory;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Check if player has specific item
        /// </summary>
        private bool HasItem(uint playerId, string itemId)
        {
            return GetItemCount(playerId, itemId) > 0;
        }
        
        /// <summary>
        /// Give arrows to player for testing
        /// </summary>
        public void GiveArrowsForTesting(uint playerId, int quantity = 10)
        {
            AddItem(playerId, "arrow", quantity);
            
            // Also send to server
            if (_dbClient != null && _dbClient.IsConnected)
            {
                _ = _dbClient.GiveArrowsToPlayerAsync(playerId, quantity);
            }
        }
        
        /// <summary>
        /// Initialize player with basic equipment for testing
        /// Requirements 5.1: Provide storage slots for items and equipment
        /// </summary>
        public void InitializePlayerInventory(uint playerId)
        {
            // Initialize empty inventory
            _playerInventories[playerId] = new InventoryData(playerId);
            
            // Give basic weapons
            AddItem(playerId, "sword", 1);
            AddItem(playerId, "axe", 1);
            AddItem(playerId, "bow", 1);
            
            // Give tools
            AddItem(playerId, "pickaxe", 1);
            
            // Give arrows for bow
            GiveArrowsForTesting(playerId, 20);
            
            // Equip sword by default
            EquipWeapon(playerId, "sword");
            
            GD.Print($"Initialized inventory for player {playerId} with equipment slots");
        }
    }
}