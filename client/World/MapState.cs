// Local: World/MapState.cs

using Guildmaster.Client.Network;
using Guildmaster.Client.Repository;

namespace Guildmaster.Client.World;

public class MapState(GuildmasterClient client, PlayerRepository repo)
{
    private string CurrentMapId { get; set; } = "";
    public event Action<string>? OnMapChanged;

    public void Tick()
    {
        // Usa o Repository para achar o player local com segurança
        var me = repo.GetLocalPlayer();
        if (me == null) return;

        // Se o servidor mudou nosso mapa (via waypoint/transição)
        if (me.CurrentMapId != CurrentMapId)
        {
            UpdateMapContext(me.CurrentMapId);
        }
    }

    private void UpdateMapContext(string newMapId)
    {
        CurrentMapId = newMapId;
        
        // Dispara a troca de subscrição SQL (Interest Management)
        string[] queries =
        [
            $"SELECT * FROM player WHERE current_map_id = '{newMapId}'",
            $"SELECT * FROM Enemy WHERE map_id = '{newMapId}'",
            $"SELECT * FROM InteractableObject WHERE map_id = '{newMapId}'"
        ];

        client.Connection.SubscriptionBuilder().Subscribe(queries);
        
        // Notifica o ECS e o Renderizador para limparem o mundo
        OnMapChanged?.Invoke(newMapId);
    }
}