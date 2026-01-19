using Godot;
using System.Collections.Generic;
using System.Linq;
using GuildmasterMVP.Core;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Integrated gameplay test that demonstrates all systems working together
    /// Requirements: All - Complete gameplay flow with object interactions
    /// </summary>
    public partial class IntegratedGameplayTest : Node
    {
        private GameManager _gameManager;
        private PlayerController _player;
        private Label _statusLabel;
        private Label _interactionLabel;
        private Label _inventoryLabel;
        private SystemIntegrationValidator _validator;
        
        // Interactable objects
        private Dictionary<uint, IInteractableObject> _interactableObjects = new Dictionary<uint, IInteractableObject>();
        private TreeObject _tree;
        private RockObject _rock;
        
        // UI update timer
        private float _uiUpdateTimer = 0.0f;
        private const float UI_UPDATE_INTERVAL = 0.1f; // Update UI 10 times per second
        
        public override void _Ready()
        {
            GD.Print("Starting Integrated Gameplay Test");
            
            // Get references
            _gameManager = GetNode<GameManager>("GameManager");
            _player = GetNode<PlayerController>("World/Player");
            _statusLabel = GetNode<Label>("UILayer/UI/StatusPanel/StatusLabel");
            _interactionLabel = GetNode<Label>("UILayer/UI/InteractionPanel/InteractionLabel");
            _inventoryLabel = GetNode<Label>("UILayer/UI/InventoryPanel/InventoryLabel");
            
            // Create and add integration validator
            _validator = new SystemIntegrationValidator();
            AddChild(_validator);
            
            // Wait for GameManager to initialize
            CallDeferred(nameof(InitializeTest));
        }
        
        private async void InitializeTest()
        {
            // Wait for GameManager initialization
            while (!_gameManager.IsInitialized)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            
            GD.Print("GameManager initialized, setting up test environment");
            
            // Set up player as local player
            _player.SetAsLocalPlayer();
            
            // Create interactable objects
            SetupInteractableObjects();
            
            // Give player some starting equipment for testing
            await SetupPlayerInventory();
            
            GD.Print("Integrated gameplay test setup complete");
        }
        
        private void SetupInteractableObjects()
        {
            // Create tree object
            Vector2 treePosition = GetNode<Node2D>("World/InteractableObjects/Tree1").Position;
            _tree = new TreeObject(1001, treePosition);
            _interactableObjects[1001] = _tree;
            
            // Create rock object
            Vector2 rockPosition = GetNode<Node2D>("World/InteractableObjects/Rock1").Position;
            _rock = new RockObject(1002, rockPosition);
            _interactableObjects[1002] = _rock;
            
            // Register objects with interaction manager
            if (_gameManager.InteractionManager != null)
            {
                _gameManager.InteractionManager.RegisterInteractableObject(_tree);
                _gameManager.InteractionManager.RegisterInteractableObject(_rock);
            }
            
            GD.Print($"Created interactable objects: Tree at {treePosition}, Rock at {rockPosition}");
        }
        
        private async System.Threading.Tasks.Task SetupPlayerInventory()
        {
            if (_gameManager.InventorySystem == null)
            {
                GD.PrintErr("InventorySystem not available");
                return;
            }
            
            uint playerId = _player.PlayerId;
            
            // Give player basic equipment
            _gameManager.InventorySystem.AddItem(playerId, "sword", 1);
            _gameManager.InventorySystem.AddItem(playerId, "axe", 1);
            _gameManager.InventorySystem.AddItem(playerId, "bow", 1);
            _gameManager.InventorySystem.AddItem(playerId, "pickaxe", 1);
            _gameManager.InventorySystem.AddItem(playerId, "arrow", 20);
            _gameManager.InventorySystem.AddItem(playerId, "fruit", 3);
            
            // Equip sword by default
            _gameManager.InventorySystem.EquipWeapon(playerId, "sword");
            
            // Give arrows to player on server for bow testing
            if (_gameManager.DbClient != null)
            {
                await _gameManager.DbClient.GiveArrowsToPlayerAsync(playerId, 20);
            }
            
            GD.Print("Player inventory setup complete");
        }
        
        public override void _Process(double delta)
        {
            _uiUpdateTimer += (float)delta;
            
            if (_uiUpdateTimer >= UI_UPDATE_INTERVAL)
            {
                UpdateUI();
                _uiUpdateTimer = 0.0f;
            }
        }
        
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.E:
                        HandleInteractionInput();
                        break;
                    case Key.Key1:
                        SwitchWeapon("sword");
                        break;
                    case Key.Key2:
                        SwitchWeapon("axe");
                        break;
                    case Key.Key3:
                        SwitchWeapon("bow");
                        break;
                    case Key.Space:
                        HandleAttackInput();
                        break;
                    case Key.F:
                        ConsumeHealthItem();
                        break;
                }
            }
        }
        
        private void HandleInteractionInput()
        {
            if (_gameManager.InteractionManager == null || _player == null)
            {
                return;
            }
            
            uint playerId = _player.PlayerId;
            Vector2 playerPosition = _player.Position;
            
            // Find nearby interactable objects
            var nearbyObjects = GetNearbyInteractableObjects(playerPosition, 50.0f);
            
            if (nearbyObjects.Count > 0)
            {
                var closestObject = nearbyObjects.First();
                var availableActions = closestObject.GetAvailableActions(playerId);
                
                // Validate requirements and execute first available action
                foreach (var action in availableActions)
                {
                    if (_gameManager.InteractionManager.ValidateActionRequirements(playerId, action.Requirements))
                    {
                        var result = _gameManager.InteractionManager.ExecuteContextualAction(
                            playerId, closestObject.Id, action.Type);
                        
                        GD.Print($"Executed action {action.Type}: {result.Message}");
                        break;
                    }
                    else
                    {
                        GD.Print($"Cannot execute {action.Type}: requirements not met");
                    }
                }
            }
            else
            {
                GD.Print("No interactable objects nearby");
            }
        }
        
        private void SwitchWeapon(string weaponId)
        {
            if (_gameManager.InteractionManager == null || _player == null)
            {
                return;
            }
            
            uint playerId = _player.PlayerId;
            bool success = _gameManager.InteractionManager.SwitchWeapon(playerId, weaponId);
            
            if (success)
            {
                GD.Print($"Switched to {weaponId}");
            }
            else
            {
                GD.Print($"Cannot switch to {weaponId} - not available");
            }
        }
        
        private async void HandleAttackInput()
        {
            if (_gameManager.CombatSystem == null || _player == null)
            {
                return;
            }
            
            uint playerId = _player.PlayerId;
            
            // Get equipped weapon
            string equippedWeapon = _gameManager.InventorySystem?.GetEquippedWeapon(playerId) ?? "";
            if (string.IsNullOrEmpty(equippedWeapon))
            {
                GD.Print("No weapon equipped");
                return;
            }
            
            // Get attack direction (for simplicity, attack to the right)
            Vector2 attackDirection = Vector2.Right;
            
            // Execute attack through combat system
            _gameManager.CombatSystem.ExecuteAttack(playerId, GetWeaponType(equippedWeapon), attackDirection);
            
            // Also send to server
            if (_gameManager.DbClient != null)
            {
                await _gameManager.DbClient.ExecuteAttackAsync(playerId, GetWeaponType(equippedWeapon).ToString(), 
                    attackDirection.X, attackDirection.Y);
            }
            
            GD.Print($"Attacked with {equippedWeapon} in direction {attackDirection}");
        }
        
        private void ConsumeHealthItem()
        {
            if (_gameManager.InventorySystem == null || _player == null)
            {
                return;
            }
            
            uint playerId = _player.PlayerId;
            
            // Try to consume fruit
            if (_gameManager.InventorySystem.GetItemCount(playerId, "fruit") > 0)
            {
                _gameManager.InventorySystem.RemoveItem(playerId, "fruit", 1);
                
                // Heal player through health system
                if (_gameManager.HealthSystem != null)
                {
                    _gameManager.HealthSystem.HealPlayer(playerId, 25.0f);
                }
                
                GD.Print("Consumed fruit and healed 25 HP");
            }
            else
            {
                GD.Print("No fruit available to consume");
            }
        }
        
        private WeaponType GetWeaponType(string weaponId)
        {
            return weaponId switch
            {
                "sword" => WeaponType.Sword,
                "axe" => WeaponType.Axe,
                "bow" => WeaponType.Bow,
                _ => WeaponType.Sword
            };
        }
        
        private List<IInteractableObject> GetNearbyInteractableObjects(Vector2 playerPosition, float range)
        {
            var nearbyObjects = new List<IInteractableObject>();
            
            foreach (var obj in _interactableObjects.Values)
            {
                if (obj.IsInRange(playerPosition))
                {
                    nearbyObjects.Add(obj);
                }
            }
            
            return nearbyObjects;
        }
        
        private void UpdateUI()
        {
            if (_player == null || _gameManager == null)
            {
                return;
            }
            
            uint playerId = _player.PlayerId;
            Vector2 playerPosition = _player.Position;
            
            // Update status
            string equippedWeapon = _gameManager.InventorySystem?.GetEquippedWeapon(playerId) ?? "None";
            float health = _gameManager.HealthSystem?.GetPlayerHealth(playerId) ?? 100.0f;
            float maxHealth = _gameManager.HealthSystem?.GetPlayerMaxHealth(playerId) ?? 100.0f;
            
            _statusLabel.Text = $"Integrated Gameplay Test\n" +
                               $"Position: ({playerPosition.X:F0}, {playerPosition.Y:F0})\n" +
                               $"Health: {health:F0}/{maxHealth:F0}\n" +
                               $"Equipped: {equippedWeapon}\n" +
                               $"WASD: Move, E: Interact\n" +
                               $"1/2/3: Switch Weapon\n" +
                               $"Space: Attack, F: Consume Fruit";
            
            // Update interactions
            var nearbyObjects = GetNearbyInteractableObjects(playerPosition, 50.0f);
            if (nearbyObjects.Count > 0)
            {
                var interactionText = "Nearby Interactions:\n";
                foreach (var obj in nearbyObjects)
                {
                    var actions = obj.GetAvailableActions(playerId);
                    foreach (var action in actions)
                    {
                        bool canExecute = _gameManager.InteractionManager?.ValidateActionRequirements(playerId, action.Requirements) ?? false;
                        string status = canExecute ? "[E]" : "[X]";
                        interactionText += $"{status} {action.DisplayName}\n";
                    }
                }
                _interactionLabel.Text = interactionText;
            }
            else
            {
                _interactionLabel.Text = "Nearby Interactions:\nNone";
            }
            
            // Update inventory
            if (_gameManager.InventorySystem != null)
            {
                var inventoryText = "Inventory:\n";
                var items = new[] { "sword", "axe", "bow", "pickaxe", "arrow", "wood", "stone", "fruit" };
                
                foreach (var item in items)
                {
                    int count = _gameManager.InventorySystem.GetItemCount(playerId, item);
                    if (count > 0)
                    {
                        bool equipped = _gameManager.InventorySystem.HasEquippedItem(playerId, item);
                        string equippedMark = equipped ? " [E]" : "";
                        inventoryText += $"{item}: {count}{equippedMark}\n";
                    }
                }
                
                _inventoryLabel.Text = inventoryText;
            }
        }
    }
}