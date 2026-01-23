using Guildmaster.Client.Core.ECS;
using Guildmaster.Client.Network;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace Guildmaster.Client.Core.Systems;

public class NetworkSystem : ISystem
{
    private readonly GuildmasterClient _client;

    public NetworkSystem(GuildmasterClient client)
    {
        _client = client;
    }

    public void Update(float deltaTime)
    {
        _client.Tick();
    }

    public void Draw() { }
    
    public DbConnection? GetConnection() => _client.Connection;
}
