using Godot;
using GuildmasterMVP.Core;

namespace GuildmasterMVP.Data
{
    public struct WeaponData
    {
        public string Id;
        public WeaponType Type;
        public float Damage;
        public float Range;
        public float AttackSpeed;
        public Shape2D HitArea; // Different shapes for sword (wide), axe (narrow), bow (point)
    }

    public struct ProjectileData
    {
        public uint Id;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Damage;
        public uint OwnerId;
        public float TimeToLive;
        public ProjectileType Type;
        public bool IsActive;
    }

    public struct ProjectileConfig
    {
        public ProjectileType Type;
        public float Speed;
        public float Damage;
        public float MaxRange;
        public float TimeToLive;
        public AmmoType RequiredAmmo;
    }

    public enum ProjectileType
    {
        Arrow
    }

    public enum AmmoType
    {
        Arrow
    }
}