using Godot;
using GuildmasterMVP.Core;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Test class for Health System functionality
    /// Requirements 9.1, 9.2, 9.3, 9.4, 9.5, 9.6: Health and damage system
    /// </summary>
    public partial class HealthSystemTest : Node
    {
        private IHealthSystem _healthSystem;
        private IInventorySystem _inventorySystem;
        
        public override void _Ready()
        {
            // Get system references
            _healthSystem = GetNode<HealthSystem>("../HealthSystem");
            _inventorySystem = GetNode<InventorySystem>("../InventorySystem");
            
            if (_healthSystem == null)
            {
                GD.PrintErr("HealthSystemTest: Could not find HealthSystem");
                return;
            }
            
            if (_inventorySystem == null)
            {
                GD.PrintErr("HealthSystemTest: Could not find InventorySystem");
                return;
            }
            
            GD.Print("HealthSystemTest: Running health system tests...");
            RunTests();
        }
        
        private void RunTests()
        {
            TestPlayerHealthInitialization();
            TestPlayerDamageApplication();
            TestPlayerDownedState();
            TestPlayerRevival();
            TestInvincibilityFrames();
            TestHealthConsumables();
            
            GD.Print("HealthSystemTest: All tests completed");
        }
        
        /// <summary>
        /// Test player health initialization
        /// Requirements 9.1: Player health system with maximum health capacity
        /// </summary>
        private void TestPlayerHealthInitialization()
        {
            uint testPlayerId = 1001;
            
            // Test initial health values
            float health = _healthSystem.GetPlayerHealth(testPlayerId);
            float maxHealth = _healthSystem.GetPlayerMaxHealth(testPlayerId);
            
            GD.Print($"Player {testPlayerId} initial health: {health}/{maxHealth}");
            
            // Verify default values
            if (health == 100.0f && maxHealth == 100.0f)
            {
                GD.Print("✓ Player health initialization test passed");
            }
            else
            {
                GD.PrintErr("✗ Player health initialization test failed");
            }
        }
        
        /// <summary>
        /// Test player damage application
        /// Requirements 9.2: Player damage application
        /// </summary>
        private void TestPlayerDamageApplication()
        {
            uint testPlayerId = 1002;
            uint attackerId = 2001;
            
            float initialHealth = _healthSystem.GetPlayerHealth(testPlayerId);
            
            // Apply damage
            bool damageApplied = _healthSystem.ApplyDamageToPlayer(testPlayerId, 25.0f, attackerId);
            float newHealth = _healthSystem.GetPlayerHealth(testPlayerId);
            
            GD.Print($"Player {testPlayerId} health after damage: {newHealth} (was {initialHealth})");
            
            if (damageApplied && newHealth == initialHealth - 25.0f)
            {
                GD.Print("✓ Player damage application test passed");
            }
            else
            {
                GD.PrintErr("✗ Player damage application test failed");
            }
        }
        
        /// <summary>
        /// Test player downed state
        /// Requirements 9.3: Player downed state trigger
        /// </summary>
        private void TestPlayerDownedState()
        {
            uint testPlayerId = 1003;
            uint attackerId = 2001;
            
            // Apply enough damage to down the player
            _healthSystem.ApplyDamageToPlayer(testPlayerId, 150.0f, attackerId);
            
            bool isDowned = _healthSystem.IsPlayerDowned(testPlayerId);
            float health = _healthSystem.GetPlayerHealth(testPlayerId);
            
            GD.Print($"Player {testPlayerId} downed: {isDowned}, health: {health}");
            
            if (isDowned && health <= 0.0f)
            {
                GD.Print("✓ Player downed state test passed");
            }
            else
            {
                GD.PrintErr("✗ Player downed state test failed");
            }
        }
        
        /// <summary>
        /// Test player revival
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        private void TestPlayerRevival()
        {
            uint testPlayerId = 1004;
            uint attackerId = 2001;
            uint reviverId = 1005;
            
            // Down the player first
            _healthSystem.ApplyDamageToPlayer(testPlayerId, 150.0f, attackerId);
            
            // Revive the player
            bool revived = _healthSystem.RevivePlayer(testPlayerId, reviverId);
            bool isDowned = _healthSystem.IsPlayerDowned(testPlayerId);
            float health = _healthSystem.GetPlayerHealth(testPlayerId);
            
            GD.Print($"Player {testPlayerId} revived: {revived}, downed: {isDowned}, health: {health}");
            
            if (revived && !isDowned && health > 0.0f)
            {
                GD.Print("✓ Player revival test passed");
            }
            else
            {
                GD.PrintErr("✗ Player revival test failed");
            }
        }
        
        /// <summary>
        /// Test invincibility frames
        /// Requirements 9.5: Temporary invincibility frames
        /// </summary>
        private void TestInvincibilityFrames()
        {
            uint testPlayerId = 1005;
            uint attackerId = 2001;
            
            // Apply initial damage
            _healthSystem.ApplyDamageToPlayer(testPlayerId, 10.0f, attackerId);
            
            // Check if player has invincibility frames
            bool hasInvincibility = _healthSystem.HasInvincibilityFrames(testPlayerId);
            
            GD.Print($"Player {testPlayerId} has invincibility frames: {hasInvincibility}");
            
            if (hasInvincibility)
            {
                // Try to apply damage again (should be blocked)
                float healthBefore = _healthSystem.GetPlayerHealth(testPlayerId);
                bool damageBlocked = !_healthSystem.ApplyDamageToPlayer(testPlayerId, 10.0f, attackerId);
                float healthAfter = _healthSystem.GetPlayerHealth(testPlayerId);
                
                if (damageBlocked && healthBefore == healthAfter)
                {
                    GD.Print("✓ Invincibility frames test passed");
                }
                else
                {
                    GD.PrintErr("✗ Invincibility frames test failed");
                }
            }
            else
            {
                GD.PrintErr("✗ Invincibility frames test failed - no invincibility after damage");
            }
        }
        
        /// <summary>
        /// Test health consumables
        /// Requirements 9.6: Health consumable restoration
        /// </summary>
        private void TestHealthConsumables()
        {
            uint testPlayerId = 1006;
            uint attackerId = 2001;
            
            // Initialize player inventory
            _inventorySystem.InitializePlayerInventory(testPlayerId);
            
            // Add fruit to inventory
            _inventorySystem.AddItem(testPlayerId, "fruit", 3);
            
            // Apply some damage first
            _healthSystem.ApplyDamageToPlayer(testPlayerId, 30.0f, attackerId);
            float healthBeforeHealing = _healthSystem.GetPlayerHealth(testPlayerId);
            
            // Use fruit to heal
            bool canUse = _healthSystem.CanUseHealthConsumable(testPlayerId, "fruit");
            bool consumed = _healthSystem.UseHealthConsumable(testPlayerId, "fruit");
            float healthAfterHealing = _healthSystem.GetPlayerHealth(testPlayerId);
            
            int remainingFruit = _inventorySystem.GetItemCount(testPlayerId, "fruit");
            
            GD.Print($"Player {testPlayerId} consumable test - Can use: {canUse}, Consumed: {consumed}");
            GD.Print($"Health before: {healthBeforeHealing}, after: {healthAfterHealing}, remaining fruit: {remainingFruit}");
            
            if (canUse && consumed && healthAfterHealing > healthBeforeHealing && remainingFruit == 2)
            {
                GD.Print("✓ Health consumables test passed");
            }
            else
            {
                GD.PrintErr("✗ Health consumables test failed");
            }
        }
    }
}