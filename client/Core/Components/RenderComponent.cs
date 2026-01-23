using Raylib_cs;
using Guildmaster.Client.Core.ECS;

namespace Guildmaster.Client.Core.Components;

public class RenderComponent : Component
{
    public Color Color { get; set; }
    public float Radius { get; set; } = 15f;
    // Simple shape type for MVP
    public bool IsCircle { get; set; } = true; 
}
