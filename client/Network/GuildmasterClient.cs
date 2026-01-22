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
        
        string? savedToken = AuthToken.Token;
        
        Connection = DbConnection.Builder()
            .WithUri(host)
            .WithModuleName(moduleName)
            .WithToken(savedToken) // opcional
            .OnConnect((conn, identity, token) =>
            {
                Identity = identity;
                AuthToken.SaveToken(token);
                Console.WriteLine($"Connected as {identity}");
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

    public void Tick() => Connection?.FrameTick();

    public void Disconnect()
    {
        Connection?.Disconnect();
        Connection = null;
        Identity = null;
    }
}