using Guildmaster.Client.Network;
using Raylib_cs;

namespace Guildmaster.Client.Render;

public class PlayerRenderer(GuildmasterClient client)
{
    public void Draw()
    {
        var conn = client.Connection;
        if (conn == null) return;

        // 1. Renderizar Jogadores (lib.rs)
        foreach (var p in conn.Db.Player.Iter())
        {
            var color = p.Identity == client.Identity ? Color.Blue : Color.Purple;
            
            // Desenha o corpo do player
            Raylib.DrawCircle((int)p.PositionX, (int)p.PositionY, 15, color);
            
            // Nome acima da cabeça
            Raylib.DrawText(p.UsernameDisplay, (int)p.PositionX - 20, (int)p.PositionY - 35, 10, Color.LightGray);

            if (p.IsDowned)
            {
                Raylib.DrawText("DOWN", (int)p.PositionX - 15, (int)p.PositionY + 15, 10, Color.Red);
            }
        }

        // 2. Renderizar Inimigos (combat.rs)
        foreach (var e in conn.Db.Enemy.Iter())
        {
            if (!e.IsActive) continue;

            // Cor baseada no estado da IA (combat.rs atualizado)
            var enemyColor = e.State switch
            {
                "Chasing" => Color.Red,
                "ChasingThroughMap" => Color.Purple,
                _ => Color.Orange
            };

            Raylib.DrawRectangle((int)e.PositionX - 15, (int)e.PositionY - 15, 30, 30, enemyColor);
            
            // Feedback de HP estilo Zelda
            float hpBarWidth = 30 * (e.Health / e.MaxHealth);
            Raylib.DrawRectangle((int)e.PositionX - 15, (int)e.PositionY - 25, (int)hpBarWidth, 4, Color.Green);
        }
    }
}