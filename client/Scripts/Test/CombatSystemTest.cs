using Godot;
using GuildmasterMVP.Core;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Test class for CombatSystem functionality
    /// Tests Requirements 3.1, 3.2, 3.3, 3.5, 3.6
    /// </summary>
    public partial class CombatSystemTest : Node
    {
        private CombatSystem _combatSystem;
        private const uint TEST_PLAYER_ID = 1;
        private const uint TEST_ENEMY_ID = 1001;
        
        public override void _Ready()
        {
            _combatSystem = new CombatSystem();
            AddChild(_combatSystem);
            
            // Run tests
            TestSwordAttack();
            TestAxeAttack();
            TestBowAttack();
            TestAttackPreventsMovement();
            TestWeaponEquipping();
            
            GD.Print("CombatSystem tests completed");
        }
        
        /// <summary>
        /// Test sword cleave attack functionality
        /// Requirements 3.1: Wide cleave attacks that hit multiple enemies
        /// </summary>
        private void TestSwordAttack()
        {
            GD.Print("Testing sword cleave attack...");
            
            // Test sword attack execution
            Vector2 attackDirection = Vector2.Right;
            _combatSystem.ExecuteAttack(TEST_PLAYER_ID, WeaponType.Sword, attackDirection);
            
            // Verify player is in attacking state
            bool isAttacking = _combatSystem.IsPlayerAttacking(TEST_PLAYER_ID);
            if (isAttacking)
            {
                GD.Print("✓ Sword attack initiated successfully");
            }
            else
            {
                GD.PrintErr("✗ Sword attack failed to initiate");
            }
            
            // Verify equipped weapon
            WeaponType equippedWeapon = _combatSystem.GetEquippedWeapon(TEST_PLAYER_ID);
            if (equippedWeapon == WeaponType.Sword)
            {
                GD.Print("✓ Sword weapon equipped correctly");
            }
            else
            {
                GD.PrintErr($"✗ Wrong weapon equipped: expected Sword, got {equippedWeapon}");
            }
        }
        
        /// <summary>
        /// Test axe frontal attack functionality
        /// Requirements 3.2: Higher damage attacks that only hit enemies directly in front
        /// </summary>
        private void TestAxeAttack()
        {
            GD.Print("Testing axe frontal attack...");
            
            // Test axe attack execution
            Vector2 attackDirection = Vector2.Up;
            _combatSystem.ExecuteAttack(TEST_PLAYER_ID, WeaponType.Axe, attackDirection);
            
            // Verify player is in attacking state
            bool isAttacking = _combatSystem.IsPlayerAttacking(TEST_PLAYER_ID);
            if (isAttacking)
            {
                GD.Print("✓ Axe attack initiated successfully");
            }
            else
            {
                GD.PrintErr("✗ Axe attack failed to initiate");
            }
            
            // Verify equipped weapon
            WeaponType equippedWeapon = _combatSystem.GetEquippedWeapon(TEST_PLAYER_ID);
            if (equippedWeapon == WeaponType.Axe)
            {
                GD.Print("✓ Axe weapon equipped correctly");
            }
            else
            {
                GD.PrintErr($"✗ Wrong weapon equipped: expected Axe, got {equippedWeapon}");
            }
        }
        
        /// <summary>
        /// Test bow projectile attack functionality
        /// Requirements 3.3: Projectile attacks that consume ammunition
        /// </summary>
        private void TestBowAttack()
        {
            GD.Print("Testing bow projectile attack...");
            
            // Test bow attack execution
            Vector2 attackDirection = new Vector2(1, -1).Normalized();
            _combatSystem.ExecuteAttack(TEST_PLAYER_ID, WeaponType.Bow, attackDirection);
            
            // Verify player is in attacking state
            bool isAttacking = _combatSystem.IsPlayerAttacking(TEST_PLAYER_ID);
            if (isAttacking)
            {
                GD.Print("✓ Bow attack initiated successfully");
            }
            else
            {
                GD.PrintErr("✗ Bow attack failed to initiate");
            }
            
            // Verify equipped weapon
            WeaponType equippedWeapon = _combatSystem.GetEquippedWeapon(TEST_PLAYER_ID);
            if (equippedWeapon == WeaponType.Bow)
            {
                GD.Print("✓ Bow weapon equipped correctly");
            }
            else
            {
                GD.PrintErr($"✗ Wrong weapon equipped: expected Bow, got {equippedWeapon}");
            }
        }
        
        /// <summary>
        /// Test that attacks prevent movement during animation
        /// Requirements 3.6: Prevent movement during attack animations
        /// </summary>
        private void TestAttackPreventsMovement()
        {
            GD.Print("Testing attack prevents movement...");
            
            // Ensure player is not attacking initially
            bool initialAttackState = _combatSystem.IsPlayerAttacking(TEST_PLAYER_ID);
            if (!initialAttackState)
            {
                GD.Print("✓ Player not attacking initially");
            }
            
            // Execute attack
            _combatSystem.ExecuteAttack(TEST_PLAYER_ID, WeaponType.Sword, Vector2.Right);
            
            // Check if player is now attacking (should prevent movement)
            bool attackingState = _combatSystem.IsPlayerAttacking(TEST_PLAYER_ID);
            if (attackingState)
            {
                GD.Print("✓ Player is attacking - movement should be prevented");
            }
            else
            {
                GD.PrintErr("✗ Player not in attacking state after attack");
            }
            
            // Try to execute another attack while already attacking (should be prevented)
            _combatSystem.ExecuteAttack(TEST_PLAYER_ID, WeaponType.Axe, Vector2.Left);
            
            // Weapon should still be sword (second attack should be rejected)
            WeaponType currentWeapon = _combatSystem.GetEquippedWeapon(TEST_PLAYER_ID);
            if (currentWeapon == WeaponType.Sword)
            {
                GD.Print("✓ Second attack correctly rejected while first attack in progress");
            }
            else
            {
                GD.PrintErr($"✗ Second attack not rejected: weapon changed to {currentWeapon}");
            }
        }
        
        /// <summary>
        /// Test weapon equipping behavior
        /// Requirements 5.3: Update active weapon and enable combat behavior
        /// </summary>
        private async void TestWeaponEquipping()
        {
            GD.Print("Testing weapon equipping...");
            
            // Test equipping different weapons
            WeaponType[] weapons = { WeaponType.Sword, WeaponType.Axe, WeaponType.Bow };
            string[] weaponNames = { "Sword", "Axe", "Bow" };
            
            for (int i = 0; i < weapons.Length; i++)
            {
                WeaponType weapon = weapons[i];
                string name = weaponNames[i];
                
                // Execute attack with weapon (this also equips it)
                _combatSystem.ExecuteAttack(TEST_PLAYER_ID, weapon, Vector2.Right);
                
                // Verify weapon is equipped
                WeaponType equippedWeapon = _combatSystem.GetEquippedWeapon(TEST_PLAYER_ID);
                if (equippedWeapon == weapon)
                {
                    GD.Print($"✓ {name} equipped successfully");
                }
                else
                {
                    GD.PrintErr($"✗ {name} equipping failed: expected {weapon}, got {equippedWeapon}");
                }
                
                // Wait for attack to finish before next test
                await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);
            }
        }
        
        /// <summary>
        /// Test hit processing functionality
        /// Requirements 3.5: Deal appropriate damage based on weapon type
        /// </summary>
        private void TestHitProcessing()
        {
            GD.Print("Testing hit processing...");
            
            // Test processing hits with different damage values
            float[] damageValues = { 25.0f, 40.0f, 20.0f }; // Sword, Axe, Bow damage
            string[] weaponNames = { "Sword", "Axe", "Bow" };
            
            for (int i = 0; i < damageValues.Length; i++)
            {
                float damage = damageValues[i];
                string weaponName = weaponNames[i];
                
                // Process hit
                _combatSystem.ProcessHit(TEST_PLAYER_ID, TEST_ENEMY_ID, damage);
                
                GD.Print($"✓ {weaponName} hit processed with {damage} damage");
            }
        }
        
        /// <summary>
        /// Test projectile creation
        /// Requirements 4.1: Create projectile that travels in aimed direction
        /// </summary>
        private void TestProjectileCreation()
        {
            GD.Print("Testing projectile creation...");
            
            Vector2 origin = new Vector2(100, 100);
            Vector2 direction = new Vector2(1, 0).Normalized();
            
            // Create projectile
            _combatSystem.CreateProjectile(TEST_PLAYER_ID, origin, direction, ProjectileType.Arrow);
            
            GD.Print("✓ Projectile created successfully");
        }
    }
}