using Godot;
using System.Collections.Generic;
using System.Linq;
using GuildmasterMVP.Data;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Enemy AI system implementation with state machine
    /// Requirements 8.1: Enemy state machine with Idle, Alert, and Chasing states
    /// Requirements 8.2: Idle state with patrol behavior
    /// Requirements 8.3: Alert state transition when player detected
    /// Requirements 8.4: Alert state investigation behavior
    /// Requirements 8.5: Chasing state with player pursuit
    /// Requirements 8.6: Enemy damage dealing to players
    /// Requirements 8.7: Return to Idle when player leaves leash range
    /// Requirements 8.8: Enemy health and damage from players
    /// Requirements 8.9: Enemy removal when health reaches zero
    /// </summary>
    public partial class EnemyAI : Node, IEnemyAI
    {
        [Signal]
        public delegate void EnemyStateChangedEventHandler(uint enemyId, string newState, uint targetPlayerId); // 0 = no target
        
        [Signal]
        public delegate void EnemyAttackedPlayerEventHandler(uint enemyId, uint playerId, float damage);
        
        // AI configuration constants
        private const float IDLE_PATROL_SPEED = 50.0f;
        private const float ALERT_INVESTIGATION_SPEED = 75.0f;
        private const float CHASE_SPEED = 100.0f;
        private const float ALERT_TIMEOUT = 5.0f; // seconds to investigate before returning to idle
        private const float ATTACK_COOLDOWN = 2.0f; // seconds between attacks
        private const float STATE_UPDATE_INTERVAL = 0.1f; // seconds between AI updates
        
        // Enemy type configurations
        private static readonly Dictionary<EnemyType, EnemyTypeConfig> EnemyConfigs = new Dictionary<EnemyType, EnemyTypeConfig>
        {
            [EnemyType.TestEnemy] = new EnemyTypeConfig
            {
                MaxHealth = 50.0f,
                MovementSpeed = 75.0f,
                AttackDamage = 15.0f,
                AttackRange = 30.0f,
                DetectionRange = 100.0f,
                LeashRange = 200.0f
            },
            [EnemyType.Goblin] = new EnemyTypeConfig
            {
                MaxHealth = 30.0f,
                MovementSpeed = 120.0f,
                AttackDamage = 10.0f,
                AttackRange = 25.0f,
                DetectionRange = 80.0f,
                LeashRange = 150.0f
            },
            [EnemyType.Orc] = new EnemyTypeConfig
            {
                MaxHealth = 80.0f,
                MovementSpeed = 60.0f,
                AttackDamage = 25.0f,
                AttackRange = 40.0f,
                DetectionRange = 120.0f,
                LeashRange = 250.0f
            },
            [EnemyType.Troll] = new EnemyTypeConfig
            {
                MaxHealth = 150.0f,
                MovementSpeed = 40.0f,
                AttackDamage = 40.0f,
                AttackRange = 50.0f,
                DetectionRange = 100.0f,
                LeashRange = 180.0f
            }
        };
        
        private Dictionary<uint, EnemyData> _enemies = new Dictionary<uint, EnemyData>();
        private SpacetimeDBClient _dbClient;
        private MovementSystem _movementSystem;
        private PlayerController _playerController;
        private IHealthSystem _healthSystem;
        private double _lastUpdateTime = 0.0;
        private uint _nextEnemyId = 1000000; // Start enemy IDs at 1M to avoid conflicts
        
        public override void _Ready()
        {
            // Get reference to SpacetimeDB client
            _dbClient = GameManager.Instance?.DbClient;
            if (_dbClient == null)
            {
                GD.PrintErr("EnemyAI: Could not find SpacetimeDB client");
            }
            
            // Get reference to MovementSystem
            _movementSystem = GetNode<MovementSystem>("../MovementSystem");
            if (_movementSystem == null)
            {
                GD.PrintErr("EnemyAI: Could not find MovementSystem");
            }
            
            // Get reference to PlayerController
            _playerController = GetNode<PlayerController>("../PlayerController");
            if (_playerController == null)
            {
                GD.PrintErr("EnemyAI: Could not find PlayerController");
            }
            
            // Get reference to HealthSystem
            _healthSystem = GetNode<HealthSystem>("../HealthSystem");
            if (_healthSystem == null)
            {
                GD.PrintErr("EnemyAI: Could not find HealthSystem");
            }
            
            GD.Print("EnemyAI system initialized");
        }
        
        public override void _Process(double delta)
        {
            // Update AI at regular intervals
            if (Time.GetUnixTimeFromSystem() - _lastUpdateTime >= STATE_UPDATE_INTERVAL)
            {
                UpdateAllEnemies((float)delta);
                _lastUpdateTime = Time.GetUnixTimeFromSystem();
            }
        }
        
        /// <summary>
        /// Update AI state for a specific enemy
        /// Requirements 8.1: State machine updates
        /// </summary>
        public void UpdateState(uint enemyId, float deltaTime)
        {
            if (!_enemies.ContainsKey(enemyId))
            {
                return;
            }
            
            var enemy = _enemies[enemyId];
            enemy.StateTimer += deltaTime;
            
            switch (enemy.State)
            {
                case EnemyState.Idle:
                    UpdateIdleState(ref enemy, deltaTime);
                    break;
                case EnemyState.Alert:
                    UpdateAlertState(ref enemy, deltaTime);
                    break;
                case EnemyState.Chasing:
                    UpdateChasingState(ref enemy, deltaTime);
                    break;
            }
            
            _enemies[enemyId] = enemy;
        }
        
        /// <summary>
        /// Update Idle state behavior
        /// Requirements 8.2: Idle state with patrol behavior
        /// </summary>
        private void UpdateIdleState(ref EnemyData enemy, float deltaTime)
        {
            // Patrol around the patrol center
            Vector2 patrolTarget = GetPatrolTarget(enemy);
            MoveTowardsTarget(ref enemy, patrolTarget, IDLE_PATROL_SPEED, deltaTime);
            
            // Check for nearby players
            var nearbyPlayers = GetPlayersInRange(enemy.Position, enemy.DetectionRange, enemy.CurrentMapId);
            if (nearbyPlayers.Length > 0)
            {
                var closestPlayer = nearbyPlayers[0];
                SetTarget(enemy.Id, closestPlayer.Id);
                GD.Print($"Enemy {enemy.Id} detected player {closestPlayer.Id}, transitioning to Alert");
            }
        }
        
        /// <summary>
        /// Update Alert state behavior
        /// Requirements 8.3: Alert state transition when player detected
        /// Requirements 8.4: Alert state investigation behavior
        /// </summary>
        private void UpdateAlertState(ref EnemyData enemy, float deltaTime)
        {
            // Move towards last known player position
            MoveTowardsTarget(ref enemy, enemy.LastKnownPlayerPosition, ALERT_INVESTIGATION_SPEED, deltaTime);
            
            // Check if we have line of sight to target player
            if (enemy.TargetPlayerId.HasValue)
            {
                if (HasLineOfSight(enemy.Id, enemy.TargetPlayerId.Value))
                {
                    // Transition to chasing
                    enemy.State = EnemyState.Chasing;
                    enemy.StateTimer = 0.0f;
                    GD.Print($"Enemy {enemy.Id} has line of sight to player {enemy.TargetPlayerId.Value}, transitioning to Chasing");
                    return;
                }
                
                // Check if player is still in leash range
                if (!IsPlayerInLeashRange(enemy.Id, enemy.TargetPlayerId.Value))
                {
                    ClearTarget(enemy.Id);
                    return;
                }
            }
            
            // Return to idle after timeout
            if (enemy.StateTimer >= ALERT_TIMEOUT)
            {
                enemy.State = EnemyState.Idle;
                enemy.StateTimer = 0.0f;
                enemy.TargetPlayerId = null;
                GD.Print($"Enemy {enemy.Id} alert timeout, returning to Idle");
            }
        }
        
        /// <summary>
        /// Update Chasing state behavior
        /// Requirements 8.5: Chasing state with player pursuit
        /// Requirements 8.6: Enemy damage dealing to players
        /// </summary>
        private void UpdateChasingState(ref EnemyData enemy, float deltaTime)
        {
            if (!enemy.TargetPlayerId.HasValue)
            {
                enemy.State = EnemyState.Idle;
                enemy.StateTimer = 0.0f;
                return;
            }
            
            var targetPlayer = GetPlayerData(enemy.TargetPlayerId.Value);
            if (targetPlayer == null)
            {
                ClearTarget(enemy.Id);
                return;
            }
            
            // Update last known position
            enemy.LastKnownPlayerPosition = targetPlayer.Value.Position;
            
            // Check if player is still in leash range
            if (!IsPlayerInLeashRange(enemy.Id, enemy.TargetPlayerId.Value))
            {
                ClearTarget(enemy.Id);
                return;
            }
            
            // Move towards player
            MoveTowardsTarget(ref enemy, targetPlayer.Value.Position, CHASE_SPEED, deltaTime);
            
            // Check if we can attack
            if (CanAttackPlayer(enemy.Id, enemy.TargetPlayerId.Value))
            {
                AttackPlayer(enemy.Id, enemy.TargetPlayerId.Value);
            }
            
            // Check if we lost line of sight
            if (!HasLineOfSight(enemy.Id, enemy.TargetPlayerId.Value))
            {
                enemy.State = EnemyState.Alert;
                enemy.StateTimer = 0.0f;
                GD.Print($"Enemy {enemy.Id} lost line of sight to player {enemy.TargetPlayerId.Value}, transitioning to Alert");
            }
        }
        
        /// <summary>
        /// Set target player for an enemy
        /// Requirements 8.3: Alert state transition
        /// Requirements 8.5: Chasing state activation
        /// </summary>
        public void SetTarget(uint enemyId, uint targetPlayerId)
        {
            if (!_enemies.ContainsKey(enemyId))
            {
                return;
            }
            
            var enemy = _enemies[enemyId];
            enemy.TargetPlayerId = targetPlayerId;
            
            var targetPlayer = GetPlayerData(targetPlayerId);
            if (targetPlayer.HasValue)
            {
                enemy.LastKnownPlayerPosition = targetPlayer.Value.Position;
            }
            
            // Transition to Alert state
            enemy.State = EnemyState.Alert;
            enemy.StateTimer = 0.0f;
            
            _enemies[enemyId] = enemy;
            
            // Emit state changed signal
            EmitSignal(SignalName.EnemyStateChanged, enemyId, enemy.State.ToString(), targetPlayerId);
        }
        
        /// <summary>
        /// Clear target for an enemy
        /// Requirements 8.7: Return to Idle when player leaves leash range
        /// </summary>
        public void ClearTarget(uint enemyId)
        {
            if (!_enemies.ContainsKey(enemyId))
            {
                return;
            }
            
            var enemy = _enemies[enemyId];
            enemy.TargetPlayerId = null;
            enemy.State = EnemyState.Idle;
            enemy.StateTimer = 0.0f;
            
            _enemies[enemyId] = enemy;
            GD.Print($"Enemy {enemyId} target cleared, returning to Idle");
            
            // Emit state changed signal
            EmitSignal(SignalName.EnemyStateChanged, enemyId, enemy.State.ToString(), 0u); // 0 = no target
        }
        
        /// <summary>
        /// Spawn a new enemy at the specified location
        /// Requirements 8.1: Enemy spawning and management
        /// </summary>
        public uint SpawnEnemy(EnemySpawnData spawnData)
        {
            var config = EnemyConfigs[spawnData.Type];
            var enemyId = _nextEnemyId++;
            
            var enemy = new EnemyData
            {
                Id = enemyId,
                Position = spawnData.Position,
                Velocity = Vector2.Zero,
                State = EnemyState.Idle,
                Health = config.MaxHealth,
                MaxHealth = config.MaxHealth,
                PatrolCenter = spawnData.PatrolCenter,
                PatrolRadius = spawnData.PatrolRadius,
                DetectionRange = config.DetectionRange,
                LeashRange = config.LeashRange,
                TargetPlayerId = null,
                LastKnownPlayerPosition = Vector2.Zero,
                EnemyType = spawnData.Type.ToString(),
                CurrentMapId = spawnData.MapId,
                StateTimer = 0.0f,
                MovementSpeed = config.MovementSpeed,
                AttackDamage = config.AttackDamage,
                AttackRange = config.AttackRange,
                AttackCooldown = ATTACK_COOLDOWN,
                LastAttackTime = 0.0,
                IsActive = true
            };
            
            _enemies[enemyId] = enemy;
            
            // Notify server to spawn enemy
            if (_dbClient != null && _dbClient.IsConnected)
            {
                _ = _dbClient.SpawnEnemyAsync(enemyId, spawnData.Position.X, spawnData.Position.Y, 
                                           spawnData.MapId, spawnData.Type.ToString());
            }
            
            GD.Print($"Spawned {spawnData.Type} enemy {enemyId} at ({spawnData.Position.X:F1}, {spawnData.Position.Y:F1})");
            return enemyId;
        }
        
        /// <summary>
        /// Remove an enemy from the game world
        /// Requirements 8.9: Enemy removal when health reaches zero
        /// </summary>
        public void RemoveEnemy(uint enemyId)
        {
            if (_enemies.ContainsKey(enemyId))
            {
                _enemies.Remove(enemyId);
                
                // Notify server to remove enemy
                if (_dbClient != null && _dbClient.IsConnected)
                {
                    _ = _dbClient.RemoveEnemyAsync(enemyId);
                }
                
                GD.Print($"Removed enemy {enemyId}");
            }
        }
        
        /// <summary>
        /// Get enemy data by ID
        /// </summary>
        public EnemyData? GetEnemyData(uint enemyId)
        {
            return _enemies.ContainsKey(enemyId) ? _enemies[enemyId] : null;
        }
        
        /// <summary>
        /// Get all enemies in a specific map
        /// </summary>
        public EnemyData[] GetEnemiesInMap(string mapId)
        {
            return _enemies.Values.Where(e => e.CurrentMapId == mapId && e.IsActive).ToArray();
        }
        
        /// <summary>
        /// Apply damage to an enemy
        /// Requirements 8.8: Enemy health and damage from players
        /// Requirements 8.9: Enemy removal when health reaches zero
        /// </summary>
        public bool ApplyDamage(uint enemyId, float damage, uint attackerId)
        {
            if (!_enemies.ContainsKey(enemyId))
            {
                return false;
            }
            
            var enemy = _enemies[enemyId];
            enemy.Health -= damage;
            
            GD.Print($"Enemy {enemyId} took {damage} damage from player {attackerId}, health: {enemy.Health:F1}/{enemy.MaxHealth:F1}");
            
            if (enemy.Health <= 0.0f)
            {
                GD.Print($"Enemy {enemyId} defeated by player {attackerId}");
                RemoveEnemy(enemyId);
                return true; // Enemy was killed
            }
            
            _enemies[enemyId] = enemy;
            return false; // Enemy survived
        }
        
        /// <summary>
        /// Check if enemy can attack a player
        /// Requirements 8.6: Enemy damage dealing to players
        /// </summary>
        public bool CanAttackPlayer(uint enemyId, uint playerId)
        {
            if (!_enemies.ContainsKey(enemyId))
            {
                return false;
            }
            
            var enemy = _enemies[enemyId];
            var player = GetPlayerData(playerId);
            
            if (!player.HasValue)
            {
                return false;
            }
            
            // Check attack range
            float distance = enemy.Position.DistanceTo(player.Value.Position);
            if (distance > enemy.AttackRange)
            {
                return false;
            }
            
            // Check attack cooldown
            double currentTime = Time.GetUnixTimeFromSystem();
            if (currentTime - enemy.LastAttackTime < enemy.AttackCooldown)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Execute enemy attack on a player
        /// Requirements 8.6: Enemy damage dealing to players
        /// </summary>
        public void AttackPlayer(uint enemyId, uint playerId)
        {
            if (!_enemies.ContainsKey(enemyId))
            {
                return;
            }
            
            var enemy = _enemies[enemyId];
            enemy.LastAttackTime = Time.GetUnixTimeFromSystem();
            _enemies[enemyId] = enemy;
            
            // Apply damage through HealthSystem
            if (_healthSystem != null)
            {
                bool damageApplied = _healthSystem.ApplyDamageToPlayer(playerId, enemy.AttackDamage, enemyId);
                if (damageApplied)
                {
                    GD.Print($"Enemy {enemyId} successfully attacked player {playerId} for {enemy.AttackDamage} damage");
                    
                    // Emit enemy attacked player signal
                    EmitSignal(SignalName.EnemyAttackedPlayer, enemyId, playerId, enemy.AttackDamage);
                }
                else
                {
                    GD.Print($"Enemy {enemyId} attack on player {playerId} was blocked (invincibility frames or downed)");
                }
            }
            else
            {
                // Fallback: notify server directly
                if (_dbClient != null && _dbClient.IsConnected)
                {
                    _ = _dbClient.EnemyAttackPlayerAsync(enemyId, playerId, enemy.AttackDamage);
                }
                
                GD.Print($"Enemy {enemyId} attacked player {playerId} for {enemy.AttackDamage} damage (no HealthSystem)");
                
                // Emit signal even without HealthSystem
                EmitSignal(SignalName.EnemyAttackedPlayer, enemyId, playerId, enemy.AttackDamage);
            }
        }
        
        /// <summary>
        /// Check if player is within detection range of enemy
        /// Requirements 8.3: Alert state transition when player detected
        /// </summary>
        public bool IsPlayerInDetectionRange(uint enemyId, uint playerId)
        {
            if (!_enemies.ContainsKey(enemyId))
            {
                return false;
            }
            
            var enemy = _enemies[enemyId];
            var player = GetPlayerData(playerId);
            
            if (!player.HasValue)
            {
                return false;
            }
            
            float distance = enemy.Position.DistanceTo(player.Value.Position);
            return distance <= enemy.DetectionRange;
        }
        
        /// <summary>
        /// Check if player is within leash range of enemy
        /// Requirements 8.7: Return to Idle when player leaves leash range
        /// </summary>
        public bool IsPlayerInLeashRange(uint enemyId, uint playerId)
        {
            if (!_enemies.ContainsKey(enemyId))
            {
                return false;
            }
            
            var enemy = _enemies[enemyId];
            var player = GetPlayerData(playerId);
            
            if (!player.HasValue)
            {
                return false;
            }
            
            float distanceFromPatrolCenter = enemy.PatrolCenter.DistanceTo(player.Value.Position);
            return distanceFromPatrolCenter <= enemy.LeashRange;
        }
        
        /// <summary>
        /// Check if enemy has line of sight to player
        /// Requirements 8.5: Chasing state with line of sight requirement
        /// </summary>
        public bool HasLineOfSight(uint enemyId, uint playerId)
        {
            if (!_enemies.ContainsKey(enemyId))
            {
                return false;
            }
            
            var enemy = _enemies[enemyId];
            var player = GetPlayerData(playerId);
            
            if (!player.HasValue)
            {
                return false;
            }
            
            // For now, assume line of sight is always true if in detection range
            // In a full implementation, this would check for obstacles
            float distance = enemy.Position.DistanceTo(player.Value.Position);
            return distance <= enemy.DetectionRange;
        }
        
        /// <summary>
        /// Update all enemies in the AI system
        /// </summary>
        public void UpdateAllEnemies(float deltaTime)
        {
            var enemyIds = _enemies.Keys.ToArray();
            foreach (var enemyId in enemyIds)
            {
                if (_enemies.ContainsKey(enemyId) && _enemies[enemyId].IsActive)
                {
                    UpdateState(enemyId, deltaTime);
                    
                    // Sync with server periodically
                    if (_enemies[enemyId].StateTimer % 1.0f < deltaTime) // Every second
                    {
                        SyncEnemyWithServer(enemyId);
                    }
                }
            }
        }
        
        /// <summary>
        /// Sync enemy state with server
        /// </summary>
        private void SyncEnemyWithServer(uint enemyId)
        {
            if (!_enemies.ContainsKey(enemyId) || _dbClient == null || !_dbClient.IsConnected)
            {
                return;
            }
            
            var enemy = _enemies[enemyId];
            _ = _dbClient.UpdateEnemyAIAsync(
                enemyId,
                enemy.State.ToString(),
                enemy.Position,
                enemy.Velocity,
                enemy.TargetPlayerId,
                enemy.LastKnownPlayerPosition
            );
        }
        
        /// <summary>
        /// Create a test enemy for demonstration (interface method)
        /// </summary>
        public uint CreateTestEnemy(float x, float y, string mapId)
        {
            return CreateTestEnemy(new Vector2(x, y), mapId);
        }
        
        /// <summary>
        /// Create a test enemy for demonstration (public method for testing)
        /// </summary>
        public uint CreateTestEnemy(Vector2 position, string mapId = "test_map")
        {
            var spawnData = new EnemySpawnData
            {
                Type = EnemyType.TestEnemy,
                Position = position,
                MapId = mapId,
                PatrolCenter = position,
                PatrolRadius = 100.0f,
                DetectionRange = 120.0f,
                LeashRange = 200.0f
            };
            
            return SpawnEnemy(spawnData);
        }
        
        /// <summary>
        /// Get all active enemies (for debugging/testing)
        /// </summary>
        public EnemyData[] GetAllActiveEnemies()
        {
            return _enemies.Values.Where(e => e.IsActive).ToArray();
        }
        
        /// <summary>
        /// Test enemy combat behavior (for development/testing)
        /// </summary>
        public void TestEnemyCombat()
        {
            // Create a test enemy near the player
            if (_playerController != null && _movementSystem != null)
            {
                Vector2 playerPos = _movementSystem.GetPlayerPosition(_playerController.PlayerId);
                Vector2 enemyPos = playerPos + new Vector2(150.0f, 0.0f); // Spawn enemy 150 units to the right
                
                uint enemyId = CreateTestEnemy(enemyPos, "test_map");
                
                GD.Print($"Created test enemy {enemyId} at ({enemyPos.X:F1}, {enemyPos.Y:F1}) for combat testing");
                GD.Print($"Player is at ({playerPos.X:F1}, {playerPos.Y:F1})");
                
                // Force enemy to target the player for testing
                SetTarget(enemyId, _playerController.PlayerId);
                
                GD.Print($"Enemy {enemyId} is now targeting player {_playerController.PlayerId}");
            }
            else
            {
                GD.PrintErr("Cannot test enemy combat: PlayerController or MovementSystem not available");
            }
        }
        
        /// <summary>
        /// Force enemy to attack player (for testing)
        /// </summary>
        public void ForceEnemyAttack(uint enemyId, uint playerId)
        {
            if (_enemies.ContainsKey(enemyId))
            {
                var enemy = _enemies[enemyId];
                enemy.LastAttackTime = 0.0; // Reset cooldown
                _enemies[enemyId] = enemy;
                
                AttackPlayer(enemyId, playerId);
                GD.Print($"Forced enemy {enemyId} to attack player {playerId}");
            }
        }
        
        /// <summary>
        /// Get enemy position (for integration manager)
        /// </summary>
        public Vector2 GetEnemyPosition(uint enemyId)
        {
            return _enemies.ContainsKey(enemyId) ? _enemies[enemyId].Position : Vector2.Zero;
        }
        
        /// <summary>
        /// Get enemy velocity (for integration manager)
        /// </summary>
        public Vector2 GetEnemyVelocity(uint enemyId)
        {
            return _enemies.ContainsKey(enemyId) ? _enemies[enemyId].Velocity : Vector2.Zero;
        }
        
        /// <summary>
        /// Get last known player position for enemy (for integration manager)
        /// </summary>
        public Vector2 GetLastKnownPlayerPosition(uint enemyId)
        {
            return _enemies.ContainsKey(enemyId) ? _enemies[enemyId].LastKnownPlayerPosition : Vector2.Zero;
        }
        
        // Helper methods
        
        private Vector2 GetPatrolTarget(EnemyData enemy)
        {
            // Simple patrol: move in a circle around patrol center
            float angle = enemy.StateTimer * 0.5f; // Slow patrol
            float x = enemy.PatrolCenter.X + Mathf.Cos(angle) * enemy.PatrolRadius;
            float y = enemy.PatrolCenter.Y + Mathf.Sin(angle) * enemy.PatrolRadius;
            return new Vector2(x, y);
        }
        
        private void MoveTowardsTarget(ref EnemyData enemy, Vector2 target, float speed, float deltaTime)
        {
            Vector2 direction = (target - enemy.Position).Normalized();
            Vector2 movement = direction * speed * deltaTime;
            
            // Don't overshoot the target
            if (movement.Length() > enemy.Position.DistanceTo(target))
            {
                enemy.Position = target;
                enemy.Velocity = Vector2.Zero;
            }
            else
            {
                enemy.Position += movement;
                enemy.Velocity = direction * speed;
            }
        }
        
        private PlayerData[] GetPlayersInRange(Vector2 position, float range, string mapId)
        {
            // For now, check if the main player is in range
            // In a full multiplayer implementation, this would query all players in the map
            if (_playerController != null && _movementSystem != null)
            {
                uint playerId = _playerController.PlayerId;
                Vector2 playerPosition = _movementSystem.GetPlayerPosition(playerId);
                
                // If player position is zero, use the PlayerController's position
                if (playerPosition == Vector2.Zero)
                {
                    playerPosition = _playerController.Position;
                }
                
                // Check if player is in the same map (simplified check)
                float distance = position.DistanceTo(playerPosition);
                if (distance <= range)
                {
                    var playerData = new PlayerData
                    {
                        Id = playerId,
                        Position = playerPosition,
                        Velocity = _movementSystem.GetPlayerVelocity(playerId),
                        CurrentMapId = mapId, // Assume same map for now
                        Health = 100.0f, // Default health
                        MaxHealth = 100.0f,
                        IsDowned = false,
                        EquippedWeapon = "sword",
                        LastInputSequence = 0
                    };
                    
                    return new PlayerData[] { playerData };
                }
            }
            
            return new PlayerData[0];
        }
        
        private PlayerData? GetPlayerData(uint playerId)
        {
            // Get player data from the movement system
            if (_playerController != null && _playerController.PlayerId == playerId && _movementSystem != null)
            {
                Vector2 playerPosition = _movementSystem.GetPlayerPosition(playerId);
                
                // If player position is zero, use the PlayerController's position
                if (playerPosition == Vector2.Zero)
                {
                    playerPosition = _playerController.Position;
                }
                
                return new PlayerData
                {
                    Id = playerId,
                    Position = playerPosition,
                    Velocity = _movementSystem.GetPlayerVelocity(playerId),
                    CurrentMapId = "current_map", // Simplified for now
                    Health = 100.0f, // Default health
                    MaxHealth = 100.0f,
                    IsDowned = false,
                    EquippedWeapon = "sword",
                    LastInputSequence = 0
                };
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// Enemy type configuration data
    /// </summary>
    public struct EnemyTypeConfig
    {
        public float MaxHealth;
        public float MovementSpeed;
        public float AttackDamage;
        public float AttackRange;
        public float DetectionRange;
        public float LeashRange;
    }
}