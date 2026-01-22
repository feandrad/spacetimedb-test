using Guildmaster.Client.Network;
using SpacetimeDB.Types;

namespace Guildmaster.Client.Repository;

public class PlayerRepository(GuildmasterClient client)
{
    public bool LoginOrRegister(string usernameDisplay)
    {
        var conn = client.Connection;
        if (conn is null || !client.IsConnected || client.Identity is null)
            return false;

        // 1) já existe player pra essa identity?
        var me = conn.Db.Player
            .Iter()
            .FirstOrDefault(p => p.Identity == client.Identity);

        if (me is not null)
            return true;

        // 2) registra com o schema ATUAL (ainda é 1 parâmetro: username)
        conn.Reducers.RegisterPlayer(usernameDisplay);

        return true;
    }
    
    // Local: Repository/PlayerRepository.cs
    public Player? GetLocalPlayer()
    {
        if (client.Connection == null || client.Identity == null) return null;

        // Acessando via Db.Table.Iter() conforme o código gerado que você postou
        return client.Connection.Db.Player.Iter()
            .FirstOrDefault(p => p.Identity == client.Identity);
    }
}