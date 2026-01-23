using System.Numerics;
using Guildmaster.Client.Core.Components;
using Guildmaster.Client.Core.ECS;
using Raylib_cs;
using SpacetimeDB.Types;

namespace Guildmaster.Client.Core.Systems;

public class RenderSystem : ISystem
{
    private readonly GameWorld _world;
    private readonly DbConnection? _conn; // For referencing strict DB types if needed, but components should suffice

    public RenderSystem(GameWorld world, DbConnection? conn)
    {
        _world = world;
        _conn = conn;
    }

    public void Update(float deltaTime) { }

    public void Draw()
    {
        // Draw Entity Graphics
        foreach (var entity in _world.GetEntities())
        {
            var pos = entity.GetComponent<PositionComponent>();
            var render = entity.GetComponent<RenderComponent>();
            
            if (pos != null && render != null)
            {
                if (render.IsCircle)
                {
                    Raylib.DrawCircle((int)pos.Position.X, (int)pos.Position.Y, render.Radius, render.Color);
                }
                
                // Draw Name if Player
                var player = entity.GetComponent<PlayerComponent>();
                if (player != null)
                {
                    Raylib.DrawText(player.Username, (int)pos.Position.X - 20, (int)pos.Position.Y - 35, 10, Color.LightGray);
                    if (player.IsDowned)
                    {
                        Raylib.DrawText("DOWN", (int)pos.Position.X - 15, (int)pos.Position.Y + 15, 10, Color.Red);
                    }
                }
                
                // Draw HP Bar if Enemy
                var enemy = entity.GetComponent<EnemyComponent>();
                if (enemy != null && enemy.MaxHealth > 0)
                {
                     float hpBarWidth = 30 * (enemy.Health / enemy.MaxHealth);
                     Raylib.DrawRectangle((int)pos.Position.X - 15, (int)pos.Position.Y - 25, (int)hpBarWidth, 4, Color.Green);
                }
            }
        }
    }
}
