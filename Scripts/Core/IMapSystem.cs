using Godot;

namespace GuildmasterMVP.Core
{
    public interface IMapSystem
    {
        void TransitionToMap(uint playerId, string mapId, Vector2 entryPoint);
        bool IsInTransitionZone(Vector2 position, out string destinationMapId);
        void LoadMapInstance(string mapId);
        void UnloadMapInstance(string mapId);
    }
}