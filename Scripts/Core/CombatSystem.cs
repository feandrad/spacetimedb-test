using Godot;
using System.Collections.Generic;
using GuildmasterMVP.Data;
using GuildmasterMVP.Network;
using GuildmasterMVP.Visual;
using GuildmasterMVP.Audio;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Client-side combat system with server validation
    /// Implements Requirements 3.1, 3.2, 3.3, 3.5, 3.6
    /// </summary>
    public partial class CombatSystem : Node, ICombatSystem
    {
        [Signal]
        public delegate void PlayerDamagedEventHandler(uint playerId, float damage, uint attackerId);
        
        [Signal]
        public delegate void EnemyDamagedEventHandler(uint enemyId, float damage, uint attackerId);
        
        [Signal]
        public delegate void ProjectileCreatedEventHandler(uint playerId, Vector2 origin, Vector2 direction, string projectileType);
        
        [Signal]
        public delegate void AttackExecutedEventHandler(uint playerId, WeaponType weaponType, Vector2 direction);
        
        private const float SWORD_DAMAGE = 25.0f;
        private const float AXE_DAMAGE = 40.0f;
        private const float BOW_DAMAGE = 20.0f;
        private const float SWORD_RANGE = 80.0f;
        private const float AXE_RANGE = 60.0f;
        private const float BOW_RANGE = 300.0f;
        private const float ATTACK_COOLDOWN = 1.0f; // seconds between attacks
        
        private Dictionary<uint, CombatState> _playerCombatStates = new Dictionary<uint, CombatState>();
        private SpacetimeDBClient _dbClient;
        private ProjectileManager _projectileManager;
        private InventorySystem _inventorySystem;
        private IHealthSystem _healthSystem;
        private IEnemyAI _enemyAI;
        
        public override void _Ready()
        {
            // Get reference to SpacetimeDB client
            _dbClient = GameManager.Instance?.DbClient;
            if (_dbClient == null)
            {
                GD.PrintErr("CombatSystem: Could not find SpacetimeDB client");
            }
            
            // Get or create ProjectileManager
            _projectileManager = GetNode<ProjectileManager>("../ProjectileManager");
            if (_projectileManager == null)
            {
                _projectileManager = new ProjectileManager();
                GetParent().AddChild(_projectileManager);
                _projectileManager.Name = "ProjectileManager";
            }
            
            // Get or create InventorySystem
            _inventorySystem = GetNode<InventorySystem>("../InventorySystem");
            if (_inventorySystem == null)
            {
                _inventorySystem = new InventorySystem();
                GetParent().AddChild(_inventorySystem);
                _inventorySystem.Name = "InventorySystem";
            }
            
            // Get reference to HealthSystem
            _healthSystem = GetNode<HealthSystem>("../HealthSystem");
            if (_healthSystem == null)
            {
                GD.PrintErr("CombatSystem: Could not find HealthSystem");
            }
            
            // Get reference to EnemyAI
            _enemyAI = GetNode<EnemyAI>("../EnemyAI");
            if (_enemyAI == null)
            {
                GD.PrintErr("CombatSystem: Could not find EnemyAI");
            }
            
            GD.Print("CombatSystem initialized");
        }
        
        /// <summary>
        /// Execute an attack with the specified weapon type
        /// Requirements 3.1: Sword cleave attacks hit multiple enemies
        /// Requirements 3.2: Axe frontal attacks with higher damage
        /// Requirements 3.3: Bow projectile attacks with ammunition
        /// Requirements 3.6: Prevent movement during attack animations
        /// Requirements 7.5: Enable cooperative enemy attacks
        /// </summary>
        public void ExecuteAttack(uint playerId, WeaponType weapon, Vector2 direction)
        {
            // Ensure player combat state exists
            if (!_playerCombatStates.ContainsKey(playerId))
            {
                _playerCombatStates[playerId] = new CombatState
                {
                    PlayerId = playerId,
                    IsAttacking = false,
                    LastAttackTime = 0.0,
                    EquippedWeapon = WeaponType.Sword
                };
            }
            
            var combatState = _playerCombatStates[playerId];
            
            // Check attack cooldown
            double currentTime = Time.GetUnixTimeFromSystem();
            if (currentTime - combatState.LastAttackTime < ATTACK_COOLDOWN)
            {
                GD.Print($"Player {playerId} attack on cooldown");
                return;
            }
            
            // Check if already attacking (prevents movement during attack)
            if (combatState.IsAttacking)
            {
                GD.Print($"Player {playerId} already attacking");
                return;
            }
            
            // Start attack animation state
            combatState.IsAttacking = true;
            combatState.LastAttackTime = currentTime;
            combatState.EquippedWeapon = weapon;
            
            GD.Print($"Player {playerId} executing {weapon} attack in direction ({direction.X:F1}, {direction.Y:F1}) - cooperative attacks enabled");
            
            // Handle different weapon types
            switch (weapon)
            {
                case WeaponType.Sword:
                    ExecuteSwordAttack(playerId, direction);
                    break;
                case WeaponType.Axe:
                    ExecuteAxeAttack(playerId, direction);
                    break;
                case WeaponType.Bow:
                    ExecuteBowAttack(playerId, direction);
                    break;
            }
            
            // Send attack to server for validation
            if (_dbClient != null && _dbClient.IsConnected)
            {
                _ = _dbClient.ExecuteAttackAsync(playerId, weapon.ToString(), direction.X, direction.Y);
            }
            
            // Emit attack executed signal
            EmitSignal(SignalName.AttackExecuted, playerId, (int)weapon, direction);
            
            // Schedule end of attack animation
            GetTree().CreateTimer(0.5f).Timeout += () => {
                combatState.IsAttacking = false;
            };
        }
        
        /// <summary>
        /// Execute sword cleave attack - wide area hitting multiple enemies
        /// Requirements 3.1: Wide cleave attacks that hit multiple enemies
        /// </summary>
        private void ExecuteSwordAttack(uint playerId, Vector2 direction)
        {
            var weaponData = new WeaponData
            {
                Id = "sword",
                Type = WeaponType.Sword,
                Damage = SWORD_DAMAGE,
                Range = SWORD_RANGE,
                AttackSpeed = 1.0f,
                HitArea = CreateSwordHitArea(direction)
            };
            
            // Create visual telegraph effect
            ShowAttackTelegraph(playerId, weaponData, direction);
            
            GD.Print($"Sword cleave attack: damage={SWORD_DAMAGE}, range={SWORD_RANGE}");
        }
        
        /// <summary>
        /// Execute axe frontal attack - high damage, narrow frontal area
        /// Requirements 3.2: Higher damage attacks that only hit enemies directly in front
        /// </summary>
        private void ExecuteAxeAttack(uint playerId, Vector2 direction)
        {
            var weaponData = new WeaponData
            {
                Id = "axe",
                Type = WeaponType.Axe,
                Damage = AXE_DAMAGE,
                Range = AXE_RANGE,
                AttackSpeed = 0.8f,
                HitArea = CreateAxeHitArea(direction)
            };
            
            // Create visual telegraph effect
            ShowAttackTelegraph(playerId, weaponData, direction);
            
            GD.Print($"Axe frontal attack: damage={AXE_DAMAGE}, range={AXE_RANGE}");
        }
        
        /// <summary>
        /// Execute bow projectile attack - creates projectile that consumes ammunition
        /// Requirements 3.3: Projectile attacks that consume ammunition
        /// Requirements 4.2: Consume ammunition from inventory
        /// </summary>
        private void ExecuteBowAttack(uint playerId, Vector2 direction)
        {
            // Check ammunition before creating projectile
            if (_inventorySystem != null)
            {
                int arrowCount = _inventorySystem.GetAmmoCount(playerId, AmmoType.Arrow);
                if (arrowCount <= 0)
                {
                    GD.Print($"Player {playerId} has no arrows for bow attack");
                    return;
                }
                
                // Consume ammunition (client-side prediction)
                _inventorySystem.RemoveItem(playerId, "arrow", 1);
                GD.Print($"Player {playerId} consumed 1 arrow, {arrowCount - 1} remaining");
            }
            
            // Get player position (would normally come from MovementSystem)
            Vector2 playerPosition = GetPlayerPosition(playerId);
            
            // Create projectile
            CreateProjectile(playerId, playerPosition, direction, ProjectileType.Arrow);
            
            GD.Print($"Bow projectile attack: damage={BOW_DAMAGE}, range={BOW_RANGE}");
        }
        
        /// <summary>
        /// Process hit between attacker and target
        /// Requirements 3.5: Deal appropriate damage based on weapon type
        /// Requirements 7.3: Disable friendly fire between players
        /// Requirements 8.8: Enemy health and damage from players
        /// </summary>
        public void ProcessHit(uint attackerId, uint targetId, float damage)
        {
            GD.Print($"Processing hit: attacker={attackerId}, target={targetId}, damage={damage}");
            
            // Check if target is an enemy (enemy IDs start at 1M)
            if (targetId >= 1000000)
            {
                // Target is an enemy - apply damage normally
                if (_healthSystem != null)
                {
                    bool enemyKilled = _healthSystem.ApplyDamageToEnemy(targetId, damage, attackerId);
                    if (enemyKilled)
                    {
                        GD.Print($"Enemy {targetId} was defeated by player {attackerId}");
                    }
                }
                
                // Show visual hit effects
                ShowHitEffect(targetId, damage);
                
                // Emit enemy damaged signal
                EmitSignal(SignalName.EnemyDamaged, targetId, damage, attackerId);
                
                // Send hit to server for validation
                if (_dbClient != null && _dbClient.IsConnected)
                {
                    _ = _dbClient.ProcessHitAsync(attackerId, targetId, damage);
                }
            }
            else
            {
                // Target is a player - check for friendly fire prevention
                if (IsPlayerAttack(attackerId))
                {
                    // Friendly fire prevention: players cannot damage other players
                    GD.Print($"Friendly fire prevented: player {attackerId} cannot damage player {targetId}");
                    return;
                }
                
                // Non-player attacker (enemy) can damage players
                if (_healthSystem != null)
                {
                    bool damageApplied = _healthSystem.ApplyDamageToPlayer(targetId, damage, attackerId);
                    if (!damageApplied)
                    {
                        GD.Print($"Damage to player {targetId} was blocked");
                    }
                    else
                    {
                        // Show visual hit effects only if damage was applied
                        ShowHitEffect(targetId, damage);
                        
                        // Emit player damaged signal
                        EmitSignal(SignalName.PlayerDamaged, targetId, damage, attackerId);
                    }
                }
                
                // Send hit to server for validation (only for enemy attacks)
                if (_dbClient != null && _dbClient.IsConnected)
                {
                    _ = _dbClient.ProcessHitAsync(attackerId, targetId, damage);
                }
            }
        }
        
        /// <summary>
        /// Create a projectile that travels in the specified direction
        /// Requirements 4.1: Create projectile that travels in aimed direction
        /// Requirements 4.2: Consume ammunition from inventory
        /// </summary>
        public void CreateProjectile(uint playerId, Vector2 origin, Vector2 direction, ProjectileType type)
        {
            if (_projectileManager != null)
            {
                _projectileManager.CreateProjectile(playerId, origin, direction, type);
            }
            else
            {
                GD.PrintErr("ProjectileManager not available for projectile creation");
            }
            
            // Emit projectile created signal
            EmitSignal(SignalName.ProjectileCreated, playerId, origin, direction, type.ToString());
        }
        
        /// <summary>
        /// Check if player is currently attacking (prevents movement)
        /// Requirements 3.6: Prevent movement during attack animations
        /// </summary>
        public bool IsPlayerAttacking(uint playerId)
        {
            if (_playerCombatStates.ContainsKey(playerId))
            {
                return _playerCombatStates[playerId].IsAttacking;
            }
            return false;
        }
        
        /// <summary>
        /// Get equipped weapon for a player
        /// </summary>
        public WeaponType GetEquippedWeapon(uint playerId)
        {
            if (_playerCombatStates.ContainsKey(playerId))
            {
                return _playerCombatStates[playerId].EquippedWeapon;
            }
            return WeaponType.Sword; // default
        }
        
        /// <summary>
        /// Update combat system (no longer handles projectiles directly)
        /// </summary>
        public override void _Process(double delta)
        {
            // Projectiles are now handled by ProjectileManager
        }
        
        /// <summary>
        /// Create hit area shape for sword (wide cleave)
        /// </summary>
        private Shape2D CreateSwordHitArea(Vector2 direction)
        {
            // Create a wide arc shape for cleave attack
            var shape = new RectangleShape2D();
            shape.Size = new Vector2(SWORD_RANGE * 1.5f, SWORD_RANGE); // Wide area
            return shape;
        }
        
        /// <summary>
        /// Create hit area shape for axe (narrow frontal)
        /// </summary>
        private Shape2D CreateAxeHitArea(Vector2 direction)
        {
            // Create a narrow frontal shape
            var shape = new RectangleShape2D();
            shape.Size = new Vector2(AXE_RANGE * 0.8f, AXE_RANGE); // Narrower than sword
            return shape;
        }
        
        /// <summary>
        /// Show visual telegraph effect for attacks
        /// Requirements 10.1: Attack telegraph effects
        /// </summary>
        private void ShowAttackTelegraph(uint playerId, WeaponData weapon, Vector2 direction)
        {
            var visualEffects = GetNode<VisualEffectsManager>("../VisualEffectsManager");
            if (visualEffects != null)
            {
                Vector2 playerPosition = GetPlayerPosition(playerId);
                visualEffects.ShowAttackTelegraph(playerId, weapon.Type, playerPosition, direction);
            }
            else
            {
                GD.Print($"Showing {weapon.Type} telegraph for player {playerId} (VisualEffectsManager not found)");
            }
        }
        
        /// <summary>
        /// Show visual hit effect
        /// Requirements 10.3: Hit confirmation effects
        /// </summary>
        private void ShowHitEffect(uint targetId, float damage)
        {
            var visualEffects = GetNode<VisualEffectsManager>("../VisualEffectsManager");
            if (visualEffects != null)
            {
                Vector2 targetPosition = GetTargetPosition(targetId);
                visualEffects.ShowHitEffect(targetId, targetPosition, damage);
            }
            else
            {
                GD.Print($"Showing hit effect on target {targetId} for {damage} damage (VisualEffectsManager not found)");
            }
        }
        
        /// <summary>
        /// Get player position (placeholder - would normally come from MovementSystem)
        /// </summary>
        private Vector2 GetPlayerPosition(uint playerId)
        {
            // TODO: Get actual player position from MovementSystem
            return Vector2.Zero;
        }
        
        /// <summary>
        /// Get target position (player or enemy)
        /// </summary>
        private Vector2 GetTargetPosition(uint targetId)
        {
            // Check if target is an enemy
            if (targetId >= 1000000 && _enemyAI != null)
            {
                var enemyData = _enemyAI.GetEnemyData(targetId);
                if (enemyData.HasValue)
                {
                    return enemyData.Value.Position;
                }
            }
            else if (targetId < 1000000)
            {
                // Target is a player
                return GetPlayerPosition(targetId);
            }
            
            return Vector2.Zero;
        }
        
        /// <summary>
        /// Handle enemy attack hitting a player
        /// Requirements 8.6: Enemy damage dealing to players
        /// </summary>
        public void ProcessEnemyAttack(uint enemyId, uint playerId, float damage)
        {
            GD.Print($"Processing enemy attack: enemy={enemyId}, player={playerId}, damage={damage}");
            
            // Apply damage through HealthSystem
            if (_healthSystem != null)
            {
                bool damageApplied = _healthSystem.ApplyDamageToPlayer(playerId, damage, enemyId);
                if (damageApplied)
                {
                    // Show visual hit effects on player
                    ShowHitEffect(playerId, damage);
                    GD.Print($"Enemy {enemyId} successfully damaged player {playerId} for {damage}");
                }
                else
                {
                    GD.Print($"Enemy {enemyId} attack on player {playerId} was blocked");
                }
            }
        }
        
        /// <summary>
        /// Check if attacker is a player (for friendly fire prevention)
        /// Requirements 7.3: Disable friendly fire between players
        /// </summary>
        private bool IsPlayerAttack(uint attackerId)
        {
            // Player IDs are typically < 1000000, enemy IDs start at 1000000
            return attackerId < 1000000;
        }
        
        /// <summary>
        /// Check if target is within attack range and area
        /// Requirements 3.1: Sword cleave attacks hit multiple enemies
        /// Requirements 3.2: Axe frontal attacks
        /// Requirements 7.3: Friendly fire prevention - exclude players from attack area
        /// </summary>
        public bool IsTargetInAttackArea(uint attackerId, uint targetId, WeaponType weaponType, Vector2 attackDirection)
        {
            // Friendly fire prevention: players cannot target other players
            if (IsPlayerAttack(attackerId) && targetId < 1000000)
            {
                GD.Print($"Friendly fire prevention: player {attackerId} cannot target player {targetId}");
                return false;
            }
            
            // Get attacker and target positions
            Vector2 attackerPos = GetPlayerPosition(attackerId);
            
            // Check if target is an enemy
            if (targetId >= 1000000 && _enemyAI != null)
            {
                var enemyData = _enemyAI.GetEnemyData(targetId);
                if (enemyData.HasValue)
                {
                    Vector2 targetPos = enemyData.Value.Position;
                    return IsPositionInAttackArea(attackerPos, targetPos, weaponType, attackDirection);
                }
            }
            else if (targetId < 1000000)
            {
                // Target is a player - only allow if attacker is not a player (enemy attack)
                if (!IsPlayerAttack(attackerId))
                {
                    Vector2 targetPos = GetPlayerPosition(targetId);
                    return IsPositionInAttackArea(attackerPos, targetPos, weaponType, attackDirection);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if a position is within the attack area for a weapon type
        /// </summary>
        private bool IsPositionInAttackArea(Vector2 attackerPos, Vector2 targetPos, WeaponType weaponType, Vector2 direction)
        {
            float distance = attackerPos.DistanceTo(targetPos);
            
            switch (weaponType)
            {
                case WeaponType.Sword:
                    return distance <= SWORD_RANGE && IsInCleaveArea(attackerPos, targetPos, direction, 90.0f);
                case WeaponType.Axe:
                    return distance <= AXE_RANGE && IsInCleaveArea(attackerPos, targetPos, direction, 45.0f);
                case WeaponType.Bow:
                    return distance <= BOW_RANGE; // Projectiles handle their own collision
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Get all enemies within attack range for cooperative targeting
        /// Requirements 7.5: Enable cooperative enemy attacks
        /// </summary>
        public List<uint> GetEnemiesInAttackRange(uint playerId, WeaponType weaponType, Vector2 attackDirection, string mapId)
        {
            var enemiesInRange = new List<uint>();
            
            if (_enemyAI == null)
            {
                return enemiesInRange;
            }
            
            // Get all enemies in the current map and check which ones are in attack range
            var enemiesInMap = _enemyAI.GetEnemiesInMap(mapId);
            foreach (var enemyData in enemiesInMap)
            {
                if (IsTargetInAttackArea(playerId, enemyData.Id, weaponType, attackDirection))
                {
                    enemiesInRange.Add(enemyData.Id);
                }
            }
            
            GD.Print($"Player {playerId} can target {enemiesInRange.Count} enemies cooperatively in map {mapId}");
            return enemiesInRange;
        }
        
        /// <summary>
        /// Check if multiple players can attack the same enemy (cooperative attacks)
        /// Requirements 7.5: Enable cooperative enemy attacks
        /// </summary>
        public bool CanPlayersCooperativelyAttack(uint enemyId, List<uint> playerIds)
        {
            // In cooperative mode, multiple players can always attack the same enemy
            // No restrictions on simultaneous attacks
            GD.Print($"Cooperative attack enabled: {playerIds.Count} players can attack enemy {enemyId}");
            return true;
        }
        
        /// <summary>
        /// Execute cooperative attack where multiple players target the same enemy
        /// Requirements 7.5: Enable cooperative enemy attacks
        /// </summary>
        public void ExecuteCooperativeAttack(List<uint> playerIds, uint enemyId, List<float> damages)
        {
            if (playerIds.Count != damages.Count)
            {
                GD.PrintErr("Mismatch between player count and damage count in cooperative attack");
                return;
            }
            
            float totalDamage = 0;
            for (int i = 0; i < playerIds.Count; i++)
            {
                uint playerId = playerIds[i];
                float damage = damages[i];
                
                // Process each player's hit individually
                ProcessHit(playerId, enemyId, damage);
                totalDamage += damage;
                
                GD.Print($"Cooperative attack: Player {playerId} dealt {damage} damage to enemy {enemyId}");
            }
            
            GD.Print($"Cooperative attack complete: {playerIds.Count} players dealt {totalDamage} total damage to enemy {enemyId}");
        }
        
        /// <summary>
        /// Check if target is within cleave area (cone attack)
        /// </summary>
        private bool IsInCleaveArea(Vector2 attackerPos, Vector2 targetPos, Vector2 direction, float coneAngle)
        {
            Vector2 toTarget = (targetPos - attackerPos).Normalized();
            Vector2 attackDir = direction.Normalized();
            
            float dot = toTarget.Dot(attackDir);
            float angle = Mathf.Acos(dot) * 180.0f / Mathf.Pi;
            
            return angle <= coneAngle / 2.0f;
        }
    }
    
    /// <summary>
    /// Combat state tracking for players
    /// </summary>
    public class CombatState
    {
        public uint PlayerId;
        public bool IsAttacking;
        public double LastAttackTime;
        public WeaponType EquippedWeapon;
    }
}