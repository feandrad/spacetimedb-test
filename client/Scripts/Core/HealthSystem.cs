using Godot;
using System.Collections.Generic;
using GuildmasterMVP.Data;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Health System implementation for players and enemies
    /// Requirements 9.1: Player health system with maximum health capacity
    /// Requirements 9.2: Player damage application
    /// Requirements 9.3: Player downed state trigger
    /// Requirements 9.4: Player revival mechanics
    /// Requirements 9.5: Temporary invincibility frames
    /// Requirements 9.6: Health consumable restoration
    /// Requirements 8.8: Enemy health and damage from players
    /// Requirements 8.9: Enemy removal when health reaches zero
    /// </summary>
    public partial class HealthSystem : Node, IHealthSystem
    {
        // Health system constants
        private const float INVINCIBILITY_DURATION = 1.5f; // seconds
        private const float REVIVAL_TIME = 3.0f; // seconds to revive
        private const float DEFAULT_PLAYER_MAX_HEALTH = 100.0f;
        
        // Player health tracking
        private Dictionary<uint, PlayerHealthData> _playerHealth = new Dictionary<uint, PlayerHealthData>();
        private Dictionary<uint, double> _invincibilityTimers = new Dictionary<uint, double>();
        
        // Enemy health tracking (reference to EnemyAI system)
        private IEnemyAI _enemyAI;
        private SpacetimeDBClient _dbClient;
        
        [Signal]
        public delegate void PlayerHealthChangedEventHandler(uint playerId, float health, float maxHealth);
        
        [Signal]
        public delegate void PlayerDownedEventHandler(uint playerId);
        
        [Signal]
        public delegate void PlayerRevivedEventHandler(uint playerId, uint reviverId);
        
        [Signal]
        public delegate void PlayerHealedEventHandler(uint playerId, float healAmount);
        
        [Signal]
        public delegate void EnemyHealthChangedEventHandler(uint enemyId, float health, float maxHealth);
        
        [Signal]
        public delegate void EnemyDefeatedEventHandler(uint enemyId, uint killerId);
        
        public override void _Ready()
        {
            // Get reference to EnemyAI system
            _enemyAI = GetNode<EnemyAI>("../EnemyAI");
            if (_enemyAI == null)
            {
                GD.PrintErr("HealthSystem: Could not find EnemyAI system");
            }
            
            // Get reference to SpacetimeDB client
            _dbClient = GameManager.Instance?.DbClient;
            if (_dbClient == null)
            {
                GD.PrintErr("HealthSystem: Could not find SpacetimeDB client");
            }
            
            GD.Print("HealthSystem initialized");
        }
        
        // Revival system tracking
        private Dictionary<uint, RevivalData> _activeRevivals = new Dictionary<uint, RevivalData>();
        
        [Signal]
        public delegate void RevivalStartedEventHandler(uint downedPlayerId, uint reviverId);
        
        [Signal]
        public delegate void RevivalProgressEventHandler(uint downedPlayerId, uint reviverId, float progress);
        
        [Signal]
        public delegate void RevivalCancelledEventHandler(uint downedPlayerId, uint reviverId);
        
        public override void _Process(double delta)
        {
            // Update invincibility timers
            var playersToUpdate = new List<uint>();
            foreach (var kvp in _invincibilityTimers)
            {
                if (Time.GetUnixTimeFromSystem() - kvp.Value >= INVINCIBILITY_DURATION)
                {
                    playersToUpdate.Add(kvp.Key);
                }
            }
            
            foreach (var playerId in playersToUpdate)
            {
                _invincibilityTimers.Remove(playerId);
                GD.Print($"Player {playerId} invincibility frames ended");
            }
            
            // Update revival progress
            UpdateRevivalProgress((float)delta);
        }
        
        /// <summary>
        /// Apply damage to a player
        /// Requirements 9.2: Player damage application
        /// Requirements 9.3: Player downed state trigger
        /// Requirements 9.5: Temporary invincibility frames
        /// </summary>
        public bool ApplyDamageToPlayer(uint playerId, float damage, uint attackerId)
        {
            // Check invincibility frames
            if (HasInvincibilityFrames(playerId))
            {
                GD.Print($"Player {playerId} has invincibility frames, damage blocked");
                return false;
            }
            
            // Ensure player health data exists
            if (!_playerHealth.ContainsKey(playerId))
            {
                InitializePlayerHealth(playerId);
            }
            
            var healthData = _playerHealth[playerId];
            
            // Check if already downed
            if (healthData.IsDowned)
            {
                GD.Print($"Player {playerId} is already downed, cannot take more damage");
                return false;
            }
            
            // Apply damage
            healthData.CurrentHealth -= damage;
            if (healthData.CurrentHealth < 0)
            {
                healthData.CurrentHealth = 0;
            }
            
            GD.Print($"Player {playerId} took {damage} damage from {attackerId}, health: {healthData.CurrentHealth:F1}/{healthData.MaxHealth:F1}");
            
            // Check if player is downed
            if (healthData.CurrentHealth <= 0)
            {
                healthData.IsDowned = true;
                EmitSignal(SignalName.PlayerDowned, playerId);
                GD.Print($"Player {playerId} downed by attacker {attackerId}");
            }
            else
            {
                // Apply invincibility frames
                _invincibilityTimers[playerId] = Time.GetUnixTimeFromSystem();
            }
            
            _playerHealth[playerId] = healthData;
            EmitSignal(SignalName.PlayerHealthChanged, playerId, healthData.CurrentHealth, healthData.MaxHealth);
            
            // Notify server
            if (_dbClient != null && _dbClient.IsConnected)
            {
                _ = _dbClient.ApplyDamageToPlayerAsync(playerId, damage, attackerId);
            }
            
            return true;
        }
        
        /// <summary>
        /// Apply damage to an enemy
        /// Requirements 8.8: Enemy health and damage from players
        /// Requirements 8.9: Enemy removal when health reaches zero
        /// </summary>
        public bool ApplyDamageToEnemy(uint enemyId, float damage, uint attackerId)
        {
            if (_enemyAI == null)
            {
                GD.PrintErr("Cannot apply damage to enemy: EnemyAI system not available");
                return false;
            }
            
            // Use EnemyAI system to apply damage
            bool wasKilled = _enemyAI.ApplyDamage(enemyId, damage, attackerId);
            
            if (wasKilled)
            {
                EmitSignal(SignalName.EnemyDefeated, enemyId, attackerId);
            }
            else
            {
                var enemyData = _enemyAI.GetEnemyData(enemyId);
                if (enemyData.HasValue)
                {
                    EmitSignal(SignalName.EnemyHealthChanged, enemyId, enemyData.Value.Health, enemyData.Value.MaxHealth);
                }
            }
            
            return wasKilled;
        }
        
        /// <summary>
        /// Heal a player
        /// Requirements 9.6: Health consumable restoration
        /// </summary>
        public bool HealPlayer(uint playerId, float healAmount)
        {
            // Ensure player health data exists
            if (!_playerHealth.ContainsKey(playerId))
            {
                InitializePlayerHealth(playerId);
            }
            
            var healthData = _playerHealth[playerId];
            
            // Cannot heal downed players
            if (healthData.IsDowned)
            {
                GD.Print($"Cannot heal downed player {playerId}");
                return false;
            }
            
            // Apply healing
            float oldHealth = healthData.CurrentHealth;
            healthData.CurrentHealth += healAmount;
            if (healthData.CurrentHealth > healthData.MaxHealth)
            {
                healthData.CurrentHealth = healthData.MaxHealth;
            }
            
            float actualHealing = healthData.CurrentHealth - oldHealth;
            if (actualHealing > 0)
            {
                _playerHealth[playerId] = healthData;
                EmitSignal(SignalName.PlayerHealthChanged, playerId, healthData.CurrentHealth, healthData.MaxHealth);
                GD.Print($"Player {playerId} healed for {actualHealing:F1}, health: {healthData.CurrentHealth:F1}/{healthData.MaxHealth:F1}");
                
                // Notify server
                if (_dbClient != null && _dbClient.IsConnected)
                {
                    _ = _dbClient.HealPlayerAsync(playerId, healAmount);
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Start reviving a downed player
        /// Requirements 9.4: Player revival mechanics
        /// Requirements 7.6: Individual player systems
        /// </summary>
        public bool StartRevival(uint downedPlayerId, uint reviverId)
        {
            // Check if target player is downed
            if (!IsPlayerDowned(downedPlayerId))
            {
                GD.Print($"Player {downedPlayerId} is not downed, cannot start revival");
                return false;
            }
            
            // Check if reviver is not downed
            if (IsPlayerDowned(reviverId))
            {
                GD.Print($"Player {reviverId} is downed, cannot revive others");
                return false;
            }
            
            // Check if revival is already in progress
            if (_activeRevivals.ContainsKey(downedPlayerId))
            {
                GD.Print($"Revival already in progress for player {downedPlayerId}");
                return false;
            }
            
            // Start revival process
            var revivalData = new RevivalData
            {
                DownedPlayerId = downedPlayerId,
                ReviverId = reviverId,
                StartTime = Time.GetUnixTimeFromSystem(),
                Progress = 0.0f,
                IsActive = true
            };
            
            _activeRevivals[downedPlayerId] = revivalData;
            EmitSignal(SignalName.RevivalStarted, downedPlayerId, reviverId);
            
            GD.Print($"Player {reviverId} started reviving player {downedPlayerId}");
            return true;
        }
        
        /// <summary>
        /// Cancel an ongoing revival
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        public bool CancelRevival(uint downedPlayerId, uint reviverId)
        {
            if (!_activeRevivals.ContainsKey(downedPlayerId))
            {
                return false;
            }
            
            var revivalData = _activeRevivals[downedPlayerId];
            
            // Check if the reviver matches
            if (revivalData.ReviverId != reviverId)
            {
                GD.Print($"Player {reviverId} cannot cancel revival started by player {revivalData.ReviverId}");
                return false;
            }
            
            _activeRevivals.Remove(downedPlayerId);
            EmitSignal(SignalName.RevivalCancelled, downedPlayerId, reviverId);
            
            GD.Print($"Revival of player {downedPlayerId} cancelled by player {reviverId}");
            return true;
        }
        
        /// <summary>
        /// Update revival progress for all active revivals
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        private void UpdateRevivalProgress(float deltaTime)
        {
            var completedRevivals = new List<uint>();
            var cancelledRevivals = new List<uint>();
            
            foreach (var kvp in _activeRevivals)
            {
                uint downedPlayerId = kvp.Key;
                var revivalData = kvp.Value;
                
                if (!revivalData.IsActive)
                {
                    continue;
                }
                
                // Check if reviver is still alive and not downed
                if (IsPlayerDowned(revivalData.ReviverId))
                {
                    cancelledRevivals.Add(downedPlayerId);
                    continue;
                }
                
                // Update progress
                double elapsedTime = Time.GetUnixTimeFromSystem() - revivalData.StartTime;
                revivalData.Progress = (float)(elapsedTime / REVIVAL_TIME);
                
                // Emit progress signal
                EmitSignal(SignalName.RevivalProgress, downedPlayerId, revivalData.ReviverId, revivalData.Progress);
                
                // Check if revival is complete
                if (revivalData.Progress >= 1.0f)
                {
                    completedRevivals.Add(downedPlayerId);
                }
                else
                {
                    _activeRevivals[downedPlayerId] = revivalData;
                }
            }
            
            // Complete successful revivals
            foreach (uint downedPlayerId in completedRevivals)
            {
                var revivalData = _activeRevivals[downedPlayerId];
                _activeRevivals.Remove(downedPlayerId);
                
                // Actually revive the player
                RevivePlayer(downedPlayerId, revivalData.ReviverId);
            }
            
            // Cancel failed revivals
            foreach (uint downedPlayerId in cancelledRevivals)
            {
                var revivalData = _activeRevivals[downedPlayerId];
                _activeRevivals.Remove(downedPlayerId);
                EmitSignal(SignalName.RevivalCancelled, downedPlayerId, revivalData.ReviverId);
                GD.Print($"Revival of player {downedPlayerId} cancelled - reviver {revivalData.ReviverId} is downed");
            }
        }
        
        /// <summary>
        /// Check if a player is currently being revived
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        public bool IsPlayerBeingRevived(uint playerId)
        {
            return _activeRevivals.ContainsKey(playerId) && _activeRevivals[playerId].IsActive;
        }
        
        /// <summary>
        /// Get revival progress for a player
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        public float GetRevivalProgress(uint playerId)
        {
            if (!_activeRevivals.ContainsKey(playerId))
            {
                return 0.0f;
            }
            
            return _activeRevivals[playerId].Progress;
        }
        
        /// <summary>
        /// Get who is reviving a player
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        public uint GetReviver(uint downedPlayerId)
        {
            if (!_activeRevivals.ContainsKey(downedPlayerId))
            {
                return 0; // No reviver
            }
            
            return _activeRevivals[downedPlayerId].ReviverId;
        }
        
        /// <summary>
        /// Check if a player can be revived by another player
        /// Requirements 9.4: Player revival mechanics
        /// Requirements 7.6: Individual player systems
        /// </summary>
        public bool CanRevivePlayer(uint downedPlayerId, uint reviverId)
        {
            // Target must be downed
            if (!IsPlayerDowned(downedPlayerId))
            {
                return false;
            }
            
            // Reviver must not be downed
            if (IsPlayerDowned(reviverId))
            {
                return false;
            }
            
            // Cannot revive yourself
            if (downedPlayerId == reviverId)
            {
                return false;
            }
            
            // No revival already in progress
            if (_activeRevivals.ContainsKey(downedPlayerId))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Revive a downed player (internal method called after revival timer completes)
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        public bool RevivePlayer(uint playerId, uint reviverId)
        {
            // Ensure player health data exists
            if (!_playerHealth.ContainsKey(playerId))
            {
                GD.Print($"Cannot revive player {playerId}: no health data");
                return false;
            }
            
            var healthData = _playerHealth[playerId];
            
            // Check if player is actually downed
            if (!healthData.IsDowned)
            {
                GD.Print($"Player {playerId} is not downed, cannot revive");
                return false;
            }
            
            // Revive player with partial health
            healthData.IsDowned = false;
            healthData.CurrentHealth = healthData.MaxHealth * 0.5f; // Revive with 50% health
            
            _playerHealth[playerId] = healthData;
            EmitSignal(SignalName.PlayerRevived, playerId);
            EmitSignal(SignalName.PlayerHealthChanged, playerId, healthData.CurrentHealth, healthData.MaxHealth);
            
            // Notify server
            if (_dbClient != null && _dbClient.IsConnected)
            {
                _ = _dbClient.RevivePlayerAsync(playerId, reviverId);
            }
            
            GD.Print($"Player {playerId} revived by player {reviverId} with {healthData.CurrentHealth:F1} health");
            return true;
        }
        
        /// <summary>
        /// Check if player has invincibility frames
        /// Requirements 9.5: Temporary invincibility frames
        /// </summary>
        public bool HasInvincibilityFrames(uint playerId)
        {
            if (!_invincibilityTimers.ContainsKey(playerId))
            {
                return false;
            }
            
            double timeSinceLastDamage = Time.GetUnixTimeFromSystem() - _invincibilityTimers[playerId];
            return timeSinceLastDamage < INVINCIBILITY_DURATION;
        }
        
        /// <summary>
        /// Get player health
        /// Requirements 9.1: Player health system
        /// </summary>
        public float GetPlayerHealth(uint playerId)
        {
            if (!_playerHealth.ContainsKey(playerId))
            {
                InitializePlayerHealth(playerId);
            }
            
            return _playerHealth[playerId].CurrentHealth;
        }
        
        /// <summary>
        /// Get player max health
        /// Requirements 9.1: Player health system
        /// </summary>
        public float GetPlayerMaxHealth(uint playerId)
        {
            if (!_playerHealth.ContainsKey(playerId))
            {
                InitializePlayerHealth(playerId);
            }
            
            return _playerHealth[playerId].MaxHealth;
        }
        
        /// <summary>
        /// Check if player is downed
        /// Requirements 9.3: Player downed state
        /// </summary>
        public bool IsPlayerDowned(uint playerId)
        {
            if (!_playerHealth.ContainsKey(playerId))
            {
                return false;
            }
            
            return _playerHealth[playerId].IsDowned;
        }
        
        /// <summary>
        /// Apply damage to a player (alias for interface compatibility)
        /// Requirements 9.2: Player damage application
        /// </summary>
        public bool ApplyDamage(uint playerId, float damage)
        {
            return ApplyDamageToPlayer(playerId, damage, 0); // 0 = unknown attacker
        }
        
        /// <summary>
        /// Set player health (for server updates)
        /// Requirements 9.1: Player health system
        /// </summary>
        public void SetPlayerHealth(uint playerId, float health)
        {
            if (!_playerHealth.ContainsKey(playerId))
            {
                InitializePlayerHealth(playerId);
            }
            
            var playerHealthData = _playerHealth[playerId];
            playerHealthData.CurrentHealth = Mathf.Clamp(health, 0.0f, playerHealthData.MaxHealth);
            _playerHealth[playerId] = playerHealthData;
            
            // Emit health changed signal
            EmitSignal(SignalName.PlayerHealthChanged, playerId, playerHealthData.CurrentHealth, playerHealthData.MaxHealth);
        }
        
        /// <summary>
        /// Get enemy health
        /// Requirements 8.8: Enemy health tracking
        /// </summary>
        public float GetEnemyHealth(uint enemyId)
        {
            if (_enemyAI == null)
            {
                return 0.0f;
            }
            
            var enemyData = _enemyAI.GetEnemyData(enemyId);
            return enemyData?.Health ?? 0.0f;
        }
        
        /// <summary>
        /// Check if enemy is alive
        /// Requirements 8.9: Enemy removal when health reaches zero
        /// </summary>
        public bool IsEnemyAlive(uint enemyId)
        {
            if (_enemyAI == null)
            {
                return false;
            }
            
            var enemyData = _enemyAI.GetEnemyData(enemyId);
            return enemyData?.Health > 0.0f;
        }
        
        /// <summary>
        /// Initialize player health data
        /// </summary>
        private void InitializePlayerHealth(uint playerId)
        {
            _playerHealth[playerId] = new PlayerHealthData
            {
                PlayerId = playerId,
                CurrentHealth = DEFAULT_PLAYER_MAX_HEALTH,
                MaxHealth = DEFAULT_PLAYER_MAX_HEALTH,
                IsDowned = false
            };
            
            GD.Print($"Initialized health for player {playerId}: {DEFAULT_PLAYER_MAX_HEALTH}/{DEFAULT_PLAYER_MAX_HEALTH}");
        }
        
        /// <summary>
        /// Use a consumable item to restore health
        /// Requirements 9.6: Health consumable restoration
        /// </summary>
        public bool UseHealthConsumable(uint playerId, string itemId)
        {
            // Get inventory system reference
            var inventorySystem = GetNode<InventorySystem>("../InventorySystem");
            if (inventorySystem == null)
            {
                GD.PrintErr("HealthSystem: Could not find InventorySystem");
                return false;
            }
            
            // Check if player has the consumable
            if (inventorySystem.GetItemCount(playerId, itemId) <= 0)
            {
                GD.Print($"Player {playerId} does not have {itemId}");
                return false;
            }
            
            // Define healing amounts for different consumables
            float healAmount = 0f;
            switch (itemId)
            {
                case "fruit":
                    healAmount = 25f; // Fruit heals 25 HP
                    break;
                case "health_potion":
                    healAmount = 50f; // Health potion heals 50 HP
                    break;
                case "mega_health_potion":
                    healAmount = 100f; // Mega health potion heals 100 HP
                    break;
                default:
                    GD.PrintErr($"Unknown consumable item: {itemId}");
                    return false;
            }
            
            // Try to heal the player
            bool healSuccessful = HealPlayer(playerId, healAmount);
            
            if (healSuccessful)
            {
                // Remove the consumed item from inventory
                bool itemRemoved = inventorySystem.RemoveItem(playerId, itemId, 1);
                if (itemRemoved)
                {
                    // Notify server
                    if (_dbClient != null && _dbClient.IsConnected)
                    {
                        _ = _dbClient.UseHealthConsumableAsync(playerId, itemId);
                    }
                    
                    GD.Print($"Player {playerId} consumed {itemId} and healed for {healAmount} HP");
                    return true;
                }
                else
                {
                    GD.PrintErr($"Failed to remove {itemId} from player {playerId} inventory");
                    return false;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if player can use a health consumable
        /// Requirements 9.6: Health consumable restoration
        /// </summary>
        public bool CanUseHealthConsumable(uint playerId, string itemId)
        {
            // Get inventory system reference
            var inventorySystem = GetNode<InventorySystem>("../InventorySystem");
            if (inventorySystem == null)
            {
                return false;
            }
            
            // Check if player has the consumable
            if (inventorySystem.GetItemCount(playerId, itemId) <= 0)
            {
                return false;
            }
            
            // Check if player is downed (cannot use consumables when downed)
            if (IsPlayerDowned(playerId))
            {
                return false;
            }
            
            // Check if player needs healing
            if (!_playerHealth.ContainsKey(playerId))
            {
                InitializePlayerHealth(playerId);
            }
            
            var healthData = _playerHealth[playerId];
            return healthData.CurrentHealth < healthData.MaxHealth;
        }
        
        /// <summary>
        /// Set player max health (for upgrades, etc.)
        /// </summary>
        public void SetPlayerMaxHealth(uint playerId, float maxHealth)
        {
            if (!_playerHealth.ContainsKey(playerId))
            {
                InitializePlayerHealth(playerId);
            }
            
            var healthData = _playerHealth[playerId];
            float healthRatio = healthData.CurrentHealth / healthData.MaxHealth;
            
            healthData.MaxHealth = maxHealth;
            healthData.CurrentHealth = healthRatio * maxHealth; // Maintain health percentage
            
            _playerHealth[playerId] = healthData;
            EmitSignal(SignalName.PlayerHealthChanged, playerId, healthData.CurrentHealth, healthData.MaxHealth);
            
            // Notify server
            if (_dbClient != null && _dbClient.IsConnected)
            {
                _ = _dbClient.SetPlayerMaxHealthAsync(playerId, maxHealth);
            }
            
            GD.Print($"Player {playerId} max health set to {maxHealth}, current health: {healthData.CurrentHealth:F1}");
        }
    }
    
    /// <summary>
    /// Player health data structure
    /// </summary>
    public struct PlayerHealthData
    {
        public uint PlayerId;
        public float CurrentHealth;
        public float MaxHealth;
        public bool IsDowned;
        
        // Alias for CurrentHealth to match interface expectations
        public float Health 
        { 
            get => CurrentHealth; 
            set => CurrentHealth = value; 
        }
    }
    
    /// <summary>
    /// Revival process data structure
    /// Requirements 9.4: Player revival mechanics
    /// </summary>
    public struct RevivalData
    {
        public uint DownedPlayerId;
        public uint ReviverId;
        public double StartTime;
        public float Progress;
        public bool IsActive;
    }
}