using System;
using Guildmaster.Client.Core.ECS;
using Guildmaster.Client.Repository;
using Guildmaster.Client.Network;

namespace Guildmaster.Client.Core.Systems;

public class MapSystem : ISystem
{
    private readonly GuildmasterClient _client;
    private readonly PlayerRepository _repo;
    
    public string CurrentMapId { get; private set; } = "";
    public int MapWidth { get; private set; } = 1000;
    public int MapHeight { get; private set; } = 1000;
    
    public event Action<string>? OnMapChanged;

    public MapSystem(GuildmasterClient client, PlayerRepository repo)
    {
        _client = client;
        _repo = repo;
    }
    
    public void UpdateMapInfo(string mapId, string metadata)
    {
        if (mapId != CurrentMapId) return; // Only update if it matches current
        
        // Expected Metadata format: "1000x1000" or similar
        // Fallback to defaults if parsing fails
        if (!string.IsNullOrEmpty(metadata))
        {
            var parts = metadata.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
            {
                MapWidth = w;
                MapHeight = h;
                Console.WriteLine($"[MapSystem] Map Size Updated: {MapWidth}x{MapHeight}");
            }
        }
    }

    public void Update(float deltaTime)
    {
        var me = _repo.GetLocalPlayer();
        if (me == null) return;

        if (me.CurrentMapId != CurrentMapId)
        {
            ChangeMap(me.CurrentMapId);
        }
    }

    public void Draw() { }

    private void ChangeMap(string newMapId)
    {
        CurrentMapId = newMapId;
        
        // Subscription Management
        string[] queries =
        [
            $"SELECT * FROM player WHERE current_map_id = '{newMapId}'",
            $"SELECT * FROM enemy WHERE map_id = '{newMapId}'",
            $"SELECT * FROM interactable_object WHERE map_id = '{newMapId}'",
            $"SELECT * FROM map_instance WHERE key_id = '{newMapId}'"
        ];

        _client.Connection?.SubscriptionBuilder().Subscribe(queries);
        
        OnMapChanged?.Invoke(newMapId);
    }
}
