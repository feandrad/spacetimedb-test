using Guildmaster.Client.Core.ECS;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace Guildmaster.Client.Core.Components;

public class PlayerComponent : Component
{
    public uint PlayerId { get; set; }
    public string Username { get; set; } = "";
    public Identity Identity { get; set; }
    public bool IsLocalPlayer { get; set; }
    public bool IsDowned { get; set; }
}
