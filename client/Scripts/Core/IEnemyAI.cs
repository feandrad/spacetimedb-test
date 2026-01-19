using Godot;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Interface for Enemy AI system
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
    public interface IEnemyAI
    {
        // Signals
        [Signal]
        delegate void EnemyStateChangedEventHandler(uint enemyId, string newState, uint targetPlayerId);
        
        [Signal]
        delegate void EnemyAttackedPlayerEventHandler(uint enemyId, uint playerId, float damage);
        
        /// <summary>
        /// Update AI state for a specific enemy
        /// Requirements 8.1: State machine updates
        /// </summary>
        void UpdateState(uint enemyId, float deltaTime);
        
        /// <summary>
        /// Set target player for an enemy
        /// Requirements 8.3: Alert state transition
        /// Requirements 8.5: Chasing state activation
        /// </summary>
        void SetTarget(uint enemyId, uint targetPlayerId);
        
        /// <summary>
        /// Clear target for an enemy
        /// Requirements 8.7: Return to Idle when player leaves leash range
        /// </summary>
        void ClearTarget(uint enemyId);
        
        /// <summary>
        /// Spawn a new enemy at the specified location
        /// Requirements 8.1: Enemy spawning and management
        /// </summary>
        uint SpawnEnemy(EnemySpawnData spawnData);
        
        /// <summary>
        /// Remove an enemy from the game world
        /// Requirements 8.9: Enemy removal when health reaches zero
        /// </summary>
        void RemoveEnemy(uint enemyId);
        
        /// <summary>
        /// Get enemy data by ID
        /// </summary>
        EnemyData? GetEnemyData(uint enemyId);
        
        /// <summary>
        /// Get all enemies in a specific map
        /// </summary>
        EnemyData[] GetEnemiesInMap(string mapId);
        
        /// <summary>
        /// Apply damage to an enemy
        /// Requirements 8.8: Enemy health and damage from players
        /// Requirements 8.9: Enemy removal when health reaches zero
        /// </summary>
        bool ApplyDamage(uint enemyId, float damage, uint attackerId);
        
        /// <summary>
        /// Check if enemy can attack a player
        /// Requirements 8.6: Enemy damage dealing to players
        /// </summary>
        bool CanAttackPlayer(uint enemyId, uint playerId);
        
        /// <summary>
        /// Execute enemy attack on a player
        /// Requirements 8.6: Enemy damage dealing to players
        /// </summary>
        void AttackPlayer(uint enemyId, uint playerId);
        
        /// <summary>
        /// Check if player is within detection range of enemy
        /// Requirements 8.3: Alert state transition when player detected
        /// </summary>
        bool IsPlayerInDetectionRange(uint enemyId, uint playerId);
        
        /// <summary>
        /// Check if player is within leash range of enemy
        /// Requirements 8.7: Return to Idle when player leaves leash range
        /// </summary>
        bool IsPlayerInLeashRange(uint enemyId, uint playerId);
        
        /// <summary>
        /// Check if enemy has line of sight to player
        /// Requirements 8.5: Chasing state with line of sight requirement
        /// </summary>
        bool HasLineOfSight(uint enemyId, uint playerId);
        
        /// <summary>
        /// Update all enemies in the AI system
        /// </summary>
        void UpdateAllEnemies(float deltaTime);
        
        // Additional methods used by SystemIntegrationManager
        Vector2 GetEnemyPosition(uint enemyId);
        Vector2 GetEnemyVelocity(uint enemyId);
        Vector2 GetLastKnownPlayerPosition(uint enemyId);
        uint CreateTestEnemy(float x, float y, string mapId);
    }
}