using Guildmaster.Client.Network;

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
}