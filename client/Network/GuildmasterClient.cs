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
        AuthToken.Init();
        
        var savedToken = AuthToken.Token;
        
        Connection = DbConnection.Builder()
            .WithUri(host)
            .WithModuleName(moduleName)
            .WithToken(savedToken) // opcional
            .OnConnect((conn, identity, token) =>
            {
                Identity = identity;
                AuthToken.SaveToken(token);
                Console.WriteLine($"Connected as {identity}");
                
                SubscribeToMap("starting_area");
            })
            .OnConnectError(e =>
            {
                Console.WriteLine($"Connect error: {e}");
            })
            .OnDisconnect((conn, e) =>
            {
                Console.WriteLine(e != null ? $"Disconnected abnormally: {e}" : "Disconnected normally.");
                Identity = null;
            })
            .Build();
    }
    
    public void SubscribeToMap(string mapId)
    {
        // Definimos as queries baseadas nas tabelas do seu Rust
        string[] queries =
        [
            $"SELECT * FROM Player WHERE current_map_id = '{mapId}'",
            $"SELECT * FROM Enemy WHERE map_id = '{mapId}'",
            $"SELECT * FROM InteractableObject WHERE map_id = '{mapId}'",
            $"SELECT * FROM map_instance WHERE key_id = '{mapId}'"
        ];

        // Usando o builder do arquivo que você postou
        Connection.SubscriptionBuilder()
            .OnApplied(ctx => {
                Console.WriteLine($"Dados do mapa {mapId} carregados com sucesso!");
            })
            .OnError((ctx, ex) => {
                Console.WriteLine($"Erro na subscrição: {ex.Message}");
            })
            .Subscribe(queries); // Passa o array de strings aqui
    }

    public void Tick() => Connection?.FrameTick();

    public void Disconnect()
    {
        Connection?.Disconnect();
        Connection = null;
        Identity = null;
    }
}