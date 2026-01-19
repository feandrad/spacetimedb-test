using Godot;
using GuildmasterMVP.Core;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Test class for Enemy AI System
    /// Tests Requirements 8.1-8.9: Enemy AI state machine and combat behavior
    /// </summary>
    public partial class EnemyAITest : Node
    {
        private EnemyAI _enemyAI;
        private HealthSystem _healthSystem;
        
        public override void _Ready()
        {
            GD.Print("=== Enemy AI System Test ===");
            
            // Create test systems
            _enemyAI = new EnemyAI();
            _healthSystem = new HealthSystem();
            
            AddChild(_enemyAI);
            AddChild(_healthSystem);
            
            _enemyAI.Name = "EnemyAI";
            _healthSystem.Name = "HealthSystem";
            
            // Wait a frame for systems to initialize
            GetTree().ProcessFrame += RunTests;
        }
        
        private void RunTests()
        {
            TestEnemyDataStructure();
            TestEnemySpawning();
            TestEnemyStates();
            TestEnemyCombat();
            
            GD.Print("=== Enemy AI System Test Complete ===");
        }
        
        private void TestEnemyDataStructure()
        {
            GD.Print("Testing Enemy Data Structure...");
            
            // Test EnemyData structure
            var enemyData = new EnemyData
            {
                Id = 1000001,
                Position = new Vector2(100, 100),
                State = EnemyState.Idle,
                Health = 50.0f,
                MaxHealth = 50.0f,
                PatrolCenter = new Vector2(100, 100),
                PatrolRadius = 100.0f,
                DetectionRange = 120.0f,
                LeashRange = 200.0f,
                EnemyType = "TestEnemy",
                IsActive = true
            };
            
            GD.Print($"✓ EnemyData: ID={enemyData.Id}, State={enemyData.State}, Health={enemyData.Health}/{enemyData.MaxHealth}");
            
            // Test EnemySpawnData structure
            var spawnData = new EnemySpawnData
            {
                Type = EnemyType.TestEnemy,
                Position = new Vector2(200, 200),
                MapId = "test_map",
                PatrolCenter = new Vector2(200, 200),
                PatrolRadius = 100.0f,
                DetectionRange = 120.0f,
                LeashRange = 200.0f
            };
            
            GD.Print($"✓ EnemySpawnData: Type={spawnData.Type}, Position=({spawnData.Position.X}, {spawnData.Position.Y})");
        }
        
        private void TestEnemySpawning()
        {
            GD.Print("Testing Enemy Spawning...");
            
            if (_enemyAI == null)
            {
                GD.PrintErr("✗ EnemyAI system not available");
                return;
            }
            
            // Test spawning different enemy types
            var testEnemyId = _enemyAI.CreateTestEnemy(new Vector2(300, 300), "test_map");
            GD.Print($"✓ Spawned TestEnemy with ID: {testEnemyId}");
            
            var spawnData = new EnemySpawnData
            {
                Type = EnemyType.Goblin,
                Position = new Vector2(400, 400),
                MapId = "test_map",
                PatrolCenter = new Vector2(400, 400),
                PatrolRadius = 80.0f,
                DetectionRange = 100.0f,
                LeashRange = 150.0f
            };
            
            var goblinId = _enemyAI.SpawnEnemy(spawnData);
            GD.Print($"✓ Spawned Goblin with ID: {goblinId}");
            
            // Test getting enemy data
            var enemyData = _enemyAI.GetEnemyData(testEnemyId);
            if (enemyData.HasValue)
            {
                GD.Print($"✓ Retrieved enemy data: State={enemyData.Value.State}, Health={enemyData.Value.Health}");
            }
            else
            {
                GD.PrintErr("✗ Failed to retrieve enemy data");
            }
            
            // Test getting all enemies
            var allEnemies = _enemyAI.GetAllActiveEnemies();
            GD.Print($"✓ Total active enemies: {allEnemies.Length}");
        }
        
        private void TestEnemyStates()
        {
            GD.Print("Testing Enemy AI States...");
            
            if (_enemyAI == null)
            {
                GD.PrintErr("✗ EnemyAI system not available");
                return;
            }
            
            // Create a test enemy
            var enemyId = _enemyAI.CreateTestEnemy(new Vector2(500, 500), "test_map");
            
            // Test initial state (should be Idle)
            var enemyData = _enemyAI.GetEnemyData(enemyId);
            if (enemyData.HasValue && enemyData.Value.State == EnemyState.Idle)
            {
                GD.Print("✓ Enemy starts in Idle state");
            }
            else
            {
                GD.PrintErr("✗ Enemy not in expected Idle state");
            }
            
            // Test setting target (should transition to Alert)
            _enemyAI.SetTarget(enemyId, 1); // Target player ID 1
            enemyData = _enemyAI.GetEnemyData(enemyId);
            if (enemyData.HasValue && enemyData.Value.State == EnemyState.Alert)
            {
                GD.Print("✓ Enemy transitioned to Alert state when target set");
            }
            else
            {
                GD.PrintErr("✗ Enemy did not transition to Alert state");
            }
            
            // Test clearing target (should return to Idle)
            _enemyAI.ClearTarget(enemyId);
            enemyData = _enemyAI.GetEnemyData(enemyId);
            if (enemyData.HasValue && enemyData.Value.State == EnemyState.Idle)
            {
                GD.Print("✓ Enemy returned to Idle state when target cleared");
            }
            else
            {
                GD.PrintErr("✗ Enemy did not return to Idle state");
            }
        }
        
        private void TestEnemyCombat()
        {
            GD.Print("Testing Enemy Combat...");
            
            if (_enemyAI == null || _healthSystem == null)
            {
                GD.PrintErr("✗ Required systems not available for combat test");
                return;
            }
            
            // Create a test enemy
            var enemyId = _enemyAI.CreateTestEnemy(new Vector2(600, 600), "test_map");
            var enemyData = _enemyAI.GetEnemyData(enemyId);
            
            if (!enemyData.HasValue)
            {
                GD.PrintErr("✗ Failed to create test enemy for combat");
                return;
            }
            
            GD.Print($"✓ Created enemy for combat test: Health={enemyData.Value.Health}/{enemyData.Value.MaxHealth}");
            
            // Test enemy taking damage
            uint playerId = 1;
            float damage = 25.0f;
            bool wasKilled = _enemyAI.ApplyDamage(enemyId, damage, playerId);
            
            enemyData = _enemyAI.GetEnemyData(enemyId);
            if (enemyData.HasValue)
            {
                GD.Print($"✓ Enemy took damage: Health={enemyData.Value.Health}/{enemyData.Value.MaxHealth}, Killed={wasKilled}");
            }
            
            // Test enemy attack capabilities
            bool canAttack = _enemyAI.CanAttackPlayer(enemyId, playerId);
            GD.Print($"✓ Enemy can attack player: {canAttack}");
            
            // Test enemy attack range and detection
            bool inDetectionRange = _enemyAI.IsPlayerInDetectionRange(enemyId, playerId);
            bool inLeashRange = _enemyAI.IsPlayerInLeashRange(enemyId, playerId);
            bool hasLineOfSight = _enemyAI.HasLineOfSight(enemyId, playerId);
            
            GD.Print($"✓ Detection range: {inDetectionRange}, Leash range: {inLeashRange}, Line of sight: {hasLineOfSight}");
            
            // Test killing enemy with enough damage
            if (enemyData.HasValue && enemyData.Value.Health > 0)
            {
                float killDamage = enemyData.Value.Health + 10.0f; // Overkill
                bool killed = _enemyAI.ApplyDamage(enemyId, killDamage, playerId);
                
                if (killed)
                {
                    GD.Print("✓ Enemy was successfully killed with overkill damage");
                }
                else
                {
                    GD.PrintErr("✗ Enemy was not killed despite taking fatal damage");
                }
            }
        }
    }
}