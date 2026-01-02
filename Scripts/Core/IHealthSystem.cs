using Godot;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Interface for Health System
    /// Requirements 9.1: Player health system with maximum health capacity
    /// Requirements 9.2: Player damage application
    /// Requirements 9.3: Player downed state trigger
    /// Requirements 9.4: Player revival mechanics
    /// Requirements 9.5: Temporary invincibility frames
    /// Requirements 9.6: Health consumable restoration
    /// Requirements 8.8: Enemy health and damage from players
    /// Requirements 8.9: Enemy removal when health reaches zero
    /// </summary>
    public interface IHealthSystem
    {
        // Signals
        [Signal]
        delegate void PlayerDownedEventHandler(uint playerId);
        
        [Signal]
        delegate void PlayerRevivedEventHandler(uint playerId, uint reviverId);
        
        [Signal]
        delegate void PlayerHealedEventHandler(uint playerId, float healAmount);
        
        /// <summary>
        /// Apply damage to a player
        /// Requirements 9.2: Player damage application
        /// Requirements 9.3: Player downed state trigger
        /// </summary>
        bool ApplyDamageToPlayer(uint playerId, float damage, uint attackerId);
        
        /// <summary>
        /// Apply damage to an enemy
        /// Requirements 8.8: Enemy health and damage from players
        /// Requirements 8.9: Enemy removal when health reaches zero
        /// </summary>
        bool ApplyDamageToEnemy(uint enemyId, float damage, uint attackerId);
        
        /// <summary>
        /// Heal a player
        /// Requirements 9.6: Health consumable restoration
        /// </summary>
        bool HealPlayer(uint playerId, float healAmount);
        
        /// <summary>
        /// Revive a downed player
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        bool RevivePlayer(uint playerId, uint reviverId);
        
        /// <summary>
        /// Start reviving a downed player
        /// Requirements 9.4: Player revival mechanics
        /// Requirements 7.6: Individual player systems
        /// </summary>
        bool StartRevival(uint downedPlayerId, uint reviverId);
        
        /// <summary>
        /// Cancel an ongoing revival
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        bool CancelRevival(uint downedPlayerId, uint reviverId);
        
        /// <summary>
        /// Check if a player can be revived by another player
        /// Requirements 9.4: Player revival mechanics
        /// Requirements 7.6: Individual player systems
        /// </summary>
        bool CanRevivePlayer(uint downedPlayerId, uint reviverId);
        
        /// <summary>
        /// Check if a player is currently being revived
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        bool IsPlayerBeingRevived(uint playerId);
        
        /// <summary>
        /// Get revival progress for a player
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        float GetRevivalProgress(uint playerId);
        
        /// <summary>
        /// Get who is reviving a player
        /// Requirements 9.4: Player revival mechanics
        /// </summary>
        uint GetReviver(uint downedPlayerId);
        
        /// <summary>
        /// Check if player has invincibility frames
        /// Requirements 9.5: Temporary invincibility frames
        /// </summary>
        bool HasInvincibilityFrames(uint playerId);
        
        /// <summary>
        /// Get player health
        /// Requirements 9.1: Player health system
        /// </summary>
        float GetPlayerHealth(uint playerId);
        
        /// <summary>
        /// Get player max health
        /// Requirements 9.1: Player health system
        /// </summary>
        float GetPlayerMaxHealth(uint playerId);
        
        /// <summary>
        /// Check if player is downed
        /// Requirements 9.3: Player downed state
        /// </summary>
        bool IsPlayerDowned(uint playerId);
        
        /// <summary>
        /// Get enemy health
        /// Requirements 8.8: Enemy health tracking
        /// </summary>
        float GetEnemyHealth(uint enemyId);
        
        /// <summary>
        /// Check if enemy is alive
        /// Requirements 8.9: Enemy removal when health reaches zero
        /// </summary>
        bool IsEnemyAlive(uint enemyId);
        
        /// <summary>
        /// Use a consumable item to restore health
        /// Requirements 9.6: Health consumable restoration
        /// </summary>
        bool UseHealthConsumable(uint playerId, string itemId);
        
        /// <summary>
        /// Check if player can use a health consumable
        /// Requirements 9.6: Health consumable restoration
        /// </summary>
        bool CanUseHealthConsumable(uint playerId, string itemId);
        
        /// <summary>
        /// Apply damage to a player (alias method used by SystemIntegrationManager)
        /// </summary>
        bool ApplyDamage(uint playerId, float damage);
        
        /// <summary>
        /// Set player health directly (used by server updates)
        /// </summary>
        void SetPlayerHealth(uint playerId, float health);
    }
}