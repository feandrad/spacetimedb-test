using System.Numerics;
using Guildmaster.Client.Core.ECS;

namespace Guildmaster.Client.Core.Components;

public class PositionComponent : Component
{
    public Vector2 Position { get; set; }
}
