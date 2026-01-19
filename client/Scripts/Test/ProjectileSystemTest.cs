using Godot;
using GuildmasterMVP.Core;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Test class for Projectile System functionality
    /// Tests Requirements 4.1, 4.2, 4.3, 4.4
    /// </summary>
    public partial class ProjectileSystemTest : Node
    {
        private ProjectileManager _projectileManager;
        private InventorySystem _inventorySystem;
        private CombatSystem _combatSystem;
        
        public override void _Ready()
        {
            GD.Print("ProjectileSystemTest: Starting projectile system tests");
            
            // Initialize systems
            _inventorySystem = new InventorySystem();
            AddChild(_inventorySystem);
            
            _projectileManager = new ProjectileManager();
            AddChild(_projectileManager);
            
            _combatSystem = new CombatSystem();
            AddChild(_combatSystem);
            
            // Wait a frame for systems to initialize
            GetTree().ProcessFrame += RunTests;
        }
        
        private void RunTests()
        {
            GetTree().ProcessFrame -= RunTests;
            
            TestProjectileCreation();
            TestAmmunitionConsumption();
            TestProjectileConfiguration();
            TestBowAttackWithAmmo();
            TestBowAttackWithoutAmmo();
            
            GD.Print("ProjectileSystemTest: All tests completed");
        }
        
        /// <summary>
        /// Test basic projectile creation
        /// Requirements 4.1: Create projectile that travels in aimed direction
        /// </summary>
        private void TestProjectileCreation()
        {
            GD.Print("Testing projectile creation...");
            
            uint testPlayerId = 1;
            Vector2 origin = new Vector2(100, 100);
            Vector2 direction = new Vector2(1, 0); // Right direction
            
            // Give player arrows first
            _inventorySystem.GiveArrowsForTesting(testPlayerId, 5);
            
            // Create projectile
            bool success = _projectileManager.CreateProjectile(testPlayerId, origin, direction, ProjectileType.Arrow);
            
            if (success)
            {
                var projectiles = _projectileManager.GetActiveProjectiles();
                if (projectiles.Count > 0)
                {
                    GD.Print("✓ Projectile creation test passed");
                }
                else
                {
                    GD.PrintErr("✗ Projectile creation test failed: No active projectiles found");
                }
            }
            else
            {
                GD.PrintErr("✗ Projectile creation test failed: CreateProjectile returned false");
            }
        }
        
        /// <summary>
        /// Test ammunition consumption
        /// Requirements 4.2: Consume ammunition from inventory
        /// </summary>
        private void TestAmmunitionConsumption()
        {
            GD.Print("Testing ammunition consumption...");
            
            uint testPlayerId = 2;
            
            // Give player exactly 3 arrows
            _inventorySystem.GiveArrowsForTesting(testPlayerId, 3);
            
            int initialArrows = _inventorySystem.GetAmmoCount(testPlayerId, AmmoType.Arrow);
            GD.Print($"Initial arrows: {initialArrows}");
            
            // Fire bow attack (should consume 1 arrow)
            _combatSystem.ExecuteAttack(testPlayerId, WeaponType.Bow, Vector2.Right);
            
            int remainingArrows = _inventorySystem.GetAmmoCount(testPlayerId, AmmoType.Arrow);
            GD.Print($"Remaining arrows: {remainingArrows}");
            
            if (remainingArrows == initialArrows - 1)
            {
                GD.Print("✓ Ammunition consumption test passed");
            }
            else
            {
                GD.PrintErr($"✗ Ammunition consumption test failed: Expected {initialArrows - 1}, got {remainingArrows}");
            }
        }
        
        /// <summary>
        /// Test projectile configuration
        /// </summary>
        private void TestProjectileConfiguration()
        {
            GD.Print("Testing projectile configuration...");
            
            var config = _projectileManager.GetProjectileConfig(ProjectileType.Arrow);
            
            if (config.Type == ProjectileType.Arrow && 
                config.Speed > 0 && 
                config.Damage > 0 && 
                config.RequiredAmmo == AmmoType.Arrow)
            {
                GD.Print("✓ Projectile configuration test passed");
                GD.Print($"  Arrow config: Speed={config.Speed}, Damage={config.Damage}, TTL={config.TimeToLive}");
            }
            else
            {
                GD.PrintErr("✗ Projectile configuration test failed: Invalid configuration");
            }
        }
        
        /// <summary>
        /// Test bow attack with sufficient ammunition
        /// Requirements 3.3: Projectile attacks that consume ammunition
        /// </summary>
        private void TestBowAttackWithAmmo()
        {
            GD.Print("Testing bow attack with ammunition...");
            
            uint testPlayerId = 3;
            
            // Give player arrows
            _inventorySystem.GiveArrowsForTesting(testPlayerId, 10);
            
            int initialProjectiles = _projectileManager.GetActiveProjectiles().Count;
            
            // Execute bow attack
            _combatSystem.ExecuteAttack(testPlayerId, WeaponType.Bow, Vector2.Right);
            
            int finalProjectiles = _projectileManager.GetActiveProjectiles().Count;
            
            if (finalProjectiles > initialProjectiles)
            {
                GD.Print("✓ Bow attack with ammo test passed");
            }
            else
            {
                GD.PrintErr("✗ Bow attack with ammo test failed: No projectile created");
            }
        }
        
        /// <summary>
        /// Test bow attack without ammunition
        /// Requirements 4.5: Prevent firing when ammunition is depleted
        /// </summary>
        private void TestBowAttackWithoutAmmo()
        {
            GD.Print("Testing bow attack without ammunition...");
            
            uint testPlayerId = 4;
            
            // Ensure player has no arrows
            int arrowCount = _inventorySystem.GetAmmoCount(testPlayerId, AmmoType.Arrow);
            if (arrowCount > 0)
            {
                _inventorySystem.RemoveItem(testPlayerId, "arrow", arrowCount);
            }
            
            int initialProjectiles = _projectileManager.GetActiveProjectiles().Count;
            
            // Try to execute bow attack without ammo
            _combatSystem.ExecuteAttack(testPlayerId, WeaponType.Bow, Vector2.Right);
            
            int finalProjectiles = _projectileManager.GetActiveProjectiles().Count;
            
            if (finalProjectiles == initialProjectiles)
            {
                GD.Print("✓ Bow attack without ammo test passed (no projectile created)");
            }
            else
            {
                GD.PrintErr("✗ Bow attack without ammo test failed: Projectile created without ammunition");
            }
        }
    }
}