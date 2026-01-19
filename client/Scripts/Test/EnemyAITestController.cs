using Godot;
using GuildmasterMVP.Core;
using GuildmasterMVP.Data;
using System.Linq;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Interactive test controller for Enemy AI System
    /// Allows real-time testing of enemy behavior through keyboard controls
    /// </summary>
    public partial class EnemyAITestController : Node
    {
        private EnemyAI _enemyAI;
        private HealthSystem _healthSystem;
        private PlayerController _playerController;
        private Label _statusLabel;
        private Camera2D _camera;
        
        private uint _lastCreatedEnemyId = 0;
        private bool _systemsReady = false;
        
        public override void _Ready()
        {
            GD.Print("=== Enemy AI Interactive Test Controller ===");
            
            // Wait a frame for all systems to initialize
            CallDeferred(nameof(InitializeSystems));
        }
        
        private void InitializeSystems()
        {
            // Get references to systems
            _enemyAI = GameManager.Instance?.EnemyAI as EnemyAI;
            _healthSystem = GameManager.Instance?.HealthSystem as HealthSystem;
            _playerController = GetNode<PlayerController>("../Player");
            _statusLabel = GetNode<Label>("../UILayer/UI/StatusLabel");
            _camera = GetNode<Camera2D>("../Camera2D");
            
            if (_enemyAI == null)
            {
                GD.PrintErr("EnemyAI system not found!");
                return;
            }
            
            if (_healthSystem == null)
            {
                GD.PrintErr("HealthSystem not found!");
                return;
            }
            
            if (_playerController == null)
            {
                GD.PrintErr("PlayerController not found!");
                return;
            }
            
            // Connect to health system signals for feedback
            _healthSystem.PlayerHealthChanged += OnPlayerHealthChanged;
            _healthSystem.PlayerDowned += OnPlayerDowned;
            _healthSystem.PlayerRevived += OnPlayerRevived;
            _healthSystem.EnemyHealthChanged += OnEnemyHealthChanged;
            _healthSystem.EnemyDefeated += OnEnemyDefeated;
            
            _systemsReady = true;
            GD.Print("✓ All systems initialized and ready for testing");
            
            // Run initial tests
            RunInitialTests();
            
            // Update status
            UpdateStatusDisplay();
        }
        
        public override void _Process(double delta)
        {
            if (!_systemsReady)
                return;
                
            // Update camera to follow player
            if (_playerController != null && _camera != null)
            {
                _camera.Position = _playerController.Position;
            }
            
            // Update status display
            UpdateStatusDisplay();
        }
        
        public override void _Input(InputEvent @event)
        {
            if (!_systemsReady || !@event.IsPressed())
                return;
                
            if (@event is InputEventKey keyEvent)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Space:
                        CreateTestEnemy();
                        break;
                    case Key.E:
                        ForceEnemyAttack();
                        break;
                    case Key.Q:
                        TestEnemyCombat();
                        break;
                    case Key.R:
                        RemoveAllEnemies();
                        break;
                    case Key.T:
                        ToggleEnemyTarget();
                        break;
                    case Key.H:
                        HealPlayer();
                        break;
                    case Key.F1:
                        ShowHelp();
                        break;
                }
            }
        }
        
        private void CreateTestEnemy()
        {
            if (_enemyAI == null || _playerController == null)
                return;
                
            // Create enemy near player but not too close
            Vector2 playerPos = _playerController.Position;
            Vector2 enemyPos = playerPos + new Vector2(
                (GD.Randf() - 0.5f) * 300.0f,
                (GD.Randf() - 0.5f) * 300.0f
            );
            
            _lastCreatedEnemyId = _enemyAI.CreateTestEnemy(enemyPos, "test_map");
            
            GD.Print($"✓ Created test enemy {_lastCreatedEnemyId} at ({enemyPos.X:F1}, {enemyPos.Y:F1})");
            GD.Print($"  Player at ({playerPos.X:F1}, {playerPos.Y:F1})");
        }
        
        private void ForceEnemyAttack()
        {
            if (_enemyAI == null || _playerController == null)
                return;
                
            var enemies = _enemyAI.GetAllActiveEnemies();
            if (enemies.Length == 0)
            {
                GD.Print("No enemies to attack with. Press SPACE to create one first.");
                return;
            }
            
            // Use the last created enemy or first available
            uint enemyId = _lastCreatedEnemyId != 0 ? _lastCreatedEnemyId : enemies[0].Id;
            _enemyAI.ForceEnemyAttack(enemyId, _playerController.PlayerId);
        }
        
        private void TestEnemyCombat()
        {
            if (_enemyAI == null)
                return;
                
            GD.Print("=== Testing Enemy Combat ===");
            _enemyAI.TestEnemyCombat();
        }
        
        private void RemoveAllEnemies()
        {
            if (_enemyAI == null)
                return;
                
            var enemies = _enemyAI.GetAllActiveEnemies();
            foreach (var enemy in enemies)
            {
                _enemyAI.RemoveEnemy(enemy.Id);
            }
            
            GD.Print($"✓ Removed {enemies.Length} enemies");
            _lastCreatedEnemyId = 0;
        }
        
        private void ToggleEnemyTarget()
        {
            if (_enemyAI == null || _playerController == null)
                return;
                
            var enemies = _enemyAI.GetAllActiveEnemies();
            if (enemies.Length == 0)
            {
                GD.Print("No enemies to target. Press SPACE to create one first.");
                return;
            }
            
            var enemy = enemies[0];
            if (enemy.TargetPlayerId.HasValue)
            {
                _enemyAI.ClearTarget(enemy.Id);
                GD.Print($"✓ Cleared target for enemy {enemy.Id}");
            }
            else
            {
                _enemyAI.SetTarget(enemy.Id, _playerController.PlayerId);
                GD.Print($"✓ Set enemy {enemy.Id} to target player {_playerController.PlayerId}");
            }
        }
        
        private void HealPlayer()
        {
            if (_healthSystem == null || _playerController == null)
                return;
                
            bool healed = _healthSystem.HealPlayer(_playerController.PlayerId, 25.0f);
            if (healed)
            {
                GD.Print("✓ Player healed for 25 HP");
            }
            else
            {
                GD.Print("Player could not be healed (full health or downed)");
            }
        }
        
        private void ShowHelp()
        {
            GD.Print("=== Enemy AI Test Controls ===");
            GD.Print("SPACE - Create test enemy near player");
            GD.Print("E - Force enemy attack on player");
            GD.Print("Q - Run enemy combat test");
            GD.Print("R - Remove all enemies");
            GD.Print("T - Toggle enemy target (set/clear)");
            GD.Print("H - Heal player");
            GD.Print("F1 - Show this help");
            GD.Print("WASD - Move player");
        }
        
        private void RunInitialTests()
        {
            GD.Print("=== Running Initial Enemy AI Tests ===");
            
            // Test enemy data structures
            TestEnemyDataStructures();
            
            // Create a test enemy for demonstration
            CreateTestEnemy();
            
            GD.Print("=== Initial Tests Complete ===");
            GD.Print("Press F1 for controls, or start testing with SPACE, E, Q, etc.");
        }
        
        private void TestEnemyDataStructures()
        {
            // Test EnemyData structure
            var enemyData = new EnemyData
            {
                Id = 999999,
                Position = Vector2.Zero,
                State = EnemyState.Idle,
                Health = 50.0f,
                MaxHealth = 50.0f,
                EnemyType = "TestEnemy",
                IsActive = true
            };
            
            GD.Print($"✓ EnemyData structure test: ID={enemyData.Id}, State={enemyData.State}");
            
            // Test EnemySpawnData structure
            var spawnData = new EnemySpawnData
            {
                Type = EnemyType.Goblin,
                Position = Vector2.Zero,
                MapId = "test_map"
            };
            
            GD.Print($"✓ EnemySpawnData structure test: Type={spawnData.Type}");
        }
        
        private void UpdateStatusDisplay()
        {
            if (_statusLabel == null || _playerController == null || _enemyAI == null || _healthSystem == null)
                return;
                
            var enemies = _enemyAI.GetAllActiveEnemies();
            var playerPos = _playerController.Position;
            var playerHealth = _healthSystem.GetPlayerHealth(_playerController.PlayerId);
            var playerMaxHealth = _healthSystem.GetPlayerMaxHealth(_playerController.PlayerId);
            var isPlayerDowned = _healthSystem.IsPlayerDowned(_playerController.PlayerId);
            
            string statusText = $"Enemy AI System - Active\n";
            statusText += $"Player: ({playerPos.X:F0}, {playerPos.Y:F0}) | Health: {playerHealth:F0}/{playerMaxHealth:F0}";
            
            if (isPlayerDowned)
            {
                statusText += " [DOWNED]";
            }
            
            statusText += $"\nActive Enemies: {enemies.Length}";
            
            if (enemies.Length > 0)
            {
                var enemy = enemies[0];
                statusText += $"\nFirst Enemy: State={enemy.State}, Health={enemy.Health:F0}/{enemy.MaxHealth:F0}";
                statusText += $", Target={enemy.TargetPlayerId?.ToString() ?? "None"}";
            }
            
            _statusLabel.Text = statusText;
        }
        
        // Health system event handlers
        private void OnPlayerHealthChanged(uint playerId, float health, float maxHealth)
        {
            GD.Print($"Player {playerId} health: {health:F1}/{maxHealth:F1}");
        }
        
        private void OnPlayerDowned(uint playerId)
        {
            GD.Print($"💀 Player {playerId} was downed!");
        }
        
        private void OnPlayerRevived(uint playerId, uint reviverId)
        {
            GD.Print($"✨ Player {playerId} was revived by player {reviverId}!");
        }
        
        private void OnEnemyHealthChanged(uint enemyId, float health, float maxHealth)
        {
            GD.Print($"Enemy {enemyId} health: {health:F1}/{maxHealth:F1}");
        }
        
        private void OnEnemyDefeated(uint enemyId, uint killerId)
        {
            GD.Print($"💀 Enemy {enemyId} defeated by player {killerId}!");
            
            // Clear reference if this was our tracked enemy
            if (_lastCreatedEnemyId == enemyId)
            {
                _lastCreatedEnemyId = 0;
            }
        }
        
        public override void _ExitTree()
        {
            // Disconnect signals
            if (_healthSystem != null)
            {
                _healthSystem.PlayerHealthChanged -= OnPlayerHealthChanged;
                _healthSystem.PlayerDowned -= OnPlayerDowned;
                _healthSystem.PlayerRevived -= OnPlayerRevived;
                _healthSystem.EnemyHealthChanged -= OnEnemyHealthChanged;
                _healthSystem.EnemyDefeated -= OnEnemyDefeated;
            }
        }
    }
}