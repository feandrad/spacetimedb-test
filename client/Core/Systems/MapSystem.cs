using System;
using Guildmaster.Client.Core.ECS;
using Guildmaster.Client.Repository;
using Guildmaster.Client.Network;

namespace Guildmaster.Client.Core.Systems;

public class MapSystem : ISystem
{
    private readonly GuildmasterClient _client;
    private readonly PlayerRepository _repo;
    
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

    public void UpdateMapInfo(string mapId, string metadata)
    {
        // Se o metadata for "{}" ou vazio, NÃO mude IsJoining para false.
        if (string.IsNullOrEmpty(metadata) || metadata == "{}")
        {
            Console.Error.WriteLine($"[MapSystem] AVISO: Recebi instância de '{mapId}', mas o metadata está vazio.");
            return;
        }

        var parts = metadata.Split('x');
        if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
        {
            MapWidth = w;
            MapHeight = h;
            IsJoining = false; // AGORA o jogo realmente começou
            Console.WriteLine($"[MapSystem] SUCESSO REAL: Mapa '{mapId}' sincronizado ({w}x{h}).");
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
        // Get our ID to stay subscribed
        var me = _repo.GetLocalPlayer();
        uint? myId = me?.Id;

        _client.SubscribeToMap(newMapId, myId);

        OnMapChanged?.Invoke(newMapId);
    }
}
