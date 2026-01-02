using Godot;

namespace GuildmasterMVP.Data
{
    public struct PlayerData
    {
        public uint Id;
        public Vector2 Position;
        public Vector2 Velocity;
        public string CurrentMapId;
        public float Health;
        public float MaxHealth;
        public bool IsDowned;
        public string EquippedWeapon;
        public uint LastInputSequence;
    }
}