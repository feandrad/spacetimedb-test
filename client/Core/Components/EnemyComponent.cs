using Guildmaster.Client.Core.ECS;

namespace Guildmaster.Client.Core.Components;

public class EnemyComponent : Component
{
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public string State { get; set; } = "";
}
