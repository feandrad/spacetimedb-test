using System;
using Guildmaster.Client.Core.ECS;
using Guildmaster.Client.Repository;
using Guildmaster.Client.Network;

namespace Guildmaster.Client.Core.Systems;

public class MapSystem : ISystem
{
    private readonly GuildmasterClient _client;
    private readonly PlayerRepository _repo;
    public List<uint> TileData { get; private set; } = new();
    
    public int MapWidth { get; private set; } = 0;
    public int MapHeight { get; private set; } = 0;
    public string CurrentMapId { get; private set; } = "";
    
    public event Action<string>? OnMapChanged;

    public MapSystem(GuildmasterClient client, PlayerRepository repo)
    {
        _client = client;
        _repo = repo;
    }

    public bool IsJoining { get; private set; } = true;

    public void Update(float deltaTime)
    {
        var me = _repo.GetLocalPlayer();
        if (me == null) return;

        if (me.CurrentMapId != CurrentMapId)
        {
            Console.WriteLine($"[MapSystem] TRIGGER CHANGE MAP: PlayerMap='{me.CurrentMapId}' != CurrentMapId='{CurrentMapId}'");
            ChangeMap(me.CurrentMapId);
        }
    }

    public void Draw() { }

    private void ChangeMap(string newMapId)
    {
        Console.WriteLine($"[MapSystem] ChangeMap START: '{newMapId}'. IsJoining=true");
        CurrentMapId = newMapId;

        IsJoining = true;
        MapWidth = 0;
        MapHeight = 0;
        TileData.Clear();

        var me = _repo.GetLocalPlayer();
        _client.SubscribeToMap(newMapId, me?.Id);

        OnMapChanged?.Invoke(newMapId);
    }

    public void LoadMapData(string mapId, int width, int height, List<uint> tiles)
    {
        MapWidth = width;
        MapHeight = height;
        TileData = new List<uint>(tiles); // Salva os dados dos tiles
        IsJoining = false;

        Console.WriteLine($"[MapSystem] LoadMapData: {width}x{height} com {tiles.Count} tiles. IsJoining=false");
    }
}
