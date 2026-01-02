using Godot;
using System.Collections.Generic;

namespace GuildmasterMVP.Data
{
    public struct MapData
    {
        public string MapId;
        public Vector2 Size;
        public List<TransitionZone> Transitions;
        public List<Vector2> SpawnPoints;
        public List<InteractableObjectData> Objects;
    }

    public struct TransitionZone
    {
        public Rect2 Area;
        public string DestinationMapId;
        public Vector2 DestinationPoint;
    }

    public struct InteractableObjectData
    {
        public uint Id;
        public ObjectType Type;
        public Vector2 Position;
        public float InteractionRange;
        public ObjectState CurrentState;
    }

    public enum ObjectType
    {
        Tree,
        Rock,
        WaterEdge,
        WaterDeep,
        Block
    }

    public struct ObjectState
    {
        public Dictionary<string, object> Properties; // Health, resources, etc.
        public bool IsDestroyed;
        public float RespawnTimer;
    }
}