using SpacetimeDB;
using SpacetimeDB.Types;

namespace Guildmaster.Client.Network;

public sealed class GuildmasterClient
{
    public DbConnection? Connection { get; private set; }
    public Identity? Identity { get; private set; }

    public bool IsConnected => Connection?.IsActive ?? false;

    public void Connect(string host, string moduleName)
    {
        Connection = DbConnection.Builder()
            .WithUri(host)
            .WithModuleName(moduleName)
            .OnConnect((conn, identity, token) =>
            {
                Identity = identity;
                Console.WriteLine($"[Network] Conectado como {identity} (Efemero)");
                SubscribeOnlyToMe(identity);
                conn.Reducers.OnRegisterPlayer += (ctx, name) => { SubscribeToStaticData(); };
            })
            .Build();
    }

    private void SubscribeOnlyToMe(Identity identity)
    {
        // CORREÇÃO: ToString() em vez de ToHex()
        string query = $"SELECT * FROM player WHERE identity = '{identity}'";
        Connection?.SubscriptionBuilder().Subscribe([query]);
    }

    private void SubscribeToStaticData()
    {
        Connection?.SubscriptionBuilder().Subscribe(["SELECT * FROM map_template"]);
    }

    public void SubscribeToMap(string mapId, uint? localPlayerId = null)
    {
        // Esta chamada SUBSTITUI a inscrição do login pela inscrição do mapa real
        var queries = new List<string>
        {
            $"SELECT * FROM player WHERE current_map_id = '{mapId}'",
            $"SELECT * FROM enemy WHERE map_id = '{mapId}'",
            $"SELECT * FROM interactable_object WHERE map_id = '{mapId}'",
            $"SELECT * FROM map_instance WHERE key_id = '{mapId}'",
            $"SELECT * FROM map_transition WHERE map_id = '{mapId}'"
        };

        // Garante que VOCÊ nunca suma, mesmo se o servidor demorar para atualizar seu current_map_id
        if (localPlayerId.HasValue)
        {
            queries.Add($"SELECT * FROM player WHERE id = {localPlayerId.Value}");
        }

        Connection?.SubscriptionBuilder()
            .OnApplied(ctx => Console.WriteLine($"[Network] Inscrição de mapa ativa: {mapId}"))
            .Subscribe(queries.ToArray());
    }

    public event Action<string>? OnConnectionError;
    public event Action<string>? OnRegistrationError;
    public event Action? OnDisconnected;

    public void Register(string username)
    {
        Connection?.Reducers.RegisterPlayer(username);
    }

    public void Tick() => Connection?.FrameTick();

    public void Disconnect()
    {
        Connection?.Disconnect();
        Connection = null;
        Identity = null;
    }

    public void ResetIdentity()
    {
        Disconnect();
        Console.WriteLine("[Client] Identity reset (Ephemeral).");
    }
}