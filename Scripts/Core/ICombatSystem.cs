using Godot;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Core
{
    public interface ICombatSystem
    {
        // Signals
        [Signal]
        delegate void PlayerDamagedEventHandler(uint playerId, float damage, uint attackerId);
        
        [Signal]
        delegate void EnemyDamagedEventHandler(uint enemyId, float damage, uint attackerId);
        
        [Signal]
        delegate void ProjectileCreatedEventHandler(uint playerId, Vector2 origin, Vector2 direction, string projectileType);
        
        [Signal]
        delegate void AttackExecutedEventHandler(uint playerId, WeaponType weaponType, Vector2 direction);
        
        // Methods
        void ExecuteAttack(uint playerId, WeaponType weapon, Vector2 direction);
        void ProcessHit(uint attackerId, uint targetId, float damage);
        void CreateProjectile(uint playerId, Vector2 origin, Vector2 direction, ProjectileType type);
    }

    public enum WeaponType
    {
        Sword,    // Wide cleave attacks
        Axe,      // High damage, frontal only
        Bow       // Projectile with ammo consumption
    }
}