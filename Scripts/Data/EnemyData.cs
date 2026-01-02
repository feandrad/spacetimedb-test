using Godot;

namespace GuildmasterMVP.Data
{
    /// <summary>
    /// Enemy data structure with AI state machine
    /// Requirements 8.1: Enemy state machine with Idle, Alert, and Chasing states
    /// Requirements 8.8: Enemy health points and damage tracking
    /// </summary>
    public struct EnemyData
    {
        public uint Id;
        public Vector2 Position;
        public Vector2 Velocity;
        public EnemyState State;
        public float Health;
        public float MaxHealth;
        public Vector2 PatrolCenter;
        public float PatrolRadius;
        public float DetectionRange;
        public float LeashRange;
        public uint? TargetPlayerId;
        public Vector2 LastKnownPlayerPosition;
        public string EnemyType;
        public string CurrentMapId;
        public float StateTimer; // Time spent in current state
        public float MovementSpeed;
        public float AttackDamage;
        public float AttackRange;
        public float AttackCooldown;
        public double LastAttackTime;
        public bool IsActive;
    }

    /// <summary>
    /// Enemy AI states for state machine
    /// Requirements 8.1: State machine with Idle, Alert, and Chasing states
    /// </summary>
    public enum EnemyState
    {
        Idle,     // Patrolling, no players detected - Requirements 8.2
        Alert,    // Investigating last known player position - Requirements 8.3, 8.4
        Chasing   // Actively pursuing player with line of sight - Requirements 8.5, 8.6
    }

    /// <summary>
    /// Enemy type definitions for different enemy behaviors
    /// </summary>
    public enum EnemyType
    {
        TestEnemy,    // Basic test enemy for development
        Goblin,       // Fast, low health enemy
        Orc,          // Medium health and damage
        Troll         // High health, slow movement
    }

    /// <summary>
    /// Enemy spawn configuration
    /// </summary>
    public struct EnemySpawnData
    {
        public EnemyType Type;
        public Vector2 Position;
        public string MapId;
        public Vector2 PatrolCenter;
        public float PatrolRadius;
        public float DetectionRange;
        public float LeashRange;
    }
}