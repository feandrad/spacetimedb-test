using Guildmaster.Client.Core.ECS;

namespace Guildmaster.Client.Core.Components;

public class ContainerComponent : Component
{
    public uint ContainerId { get; set; }
    public string Label { get; set; } = "";
    public uint Capacity { get; set; }
    public string MapId { get; set; } = "";
}
