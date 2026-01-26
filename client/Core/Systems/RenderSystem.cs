using System.Numerics;
using Guildmaster.Client.Core.Components;
using Guildmaster.Client.Core.ECS;
using Raylib_cs;
using SpacetimeDB.Types;

namespace Guildmaster.Client.Core.Systems;

public class RenderSystem : ISystem
{
    private readonly GameWorld _world;
    private readonly MapSystem _mapSystem; // Added dependency
    private readonly DbConnection? _conn; 

    private Camera2D _camera;

    public RenderSystem(GameWorld world, MapSystem mapSystem, DbConnection? conn)
    {
        _world = world;
        _mapSystem = mapSystem;
        _conn = conn;
        
        _camera = new Camera2D();
        _camera.Zoom = 1.0f;
        _camera.Rotation = 0.0f;
        _camera.Offset = new Vector2(400, 225); // Center of 800x450
    }

    public void Update(float deltaTime) 
    {
        // Find local player to center camera
        foreach (var entity in _world.GetEntities())
        {
            var player = entity.GetComponent<PlayerComponent>();
            var pos = entity.GetComponent<PositionComponent>();
            
            if (player != null && player.IsLocalPlayer && pos != null)
            {
                // Update camera target
                _camera.Target = pos.Position;
                break;
            }
        }
    }

    public void Draw()
    {
        // 1. Outside Map (Black)
        Raylib.ClearBackground(Color.Black);

        Raylib.BeginMode2D(_camera);

        // 2. Movable Area (Dark Green)
        Raylib.DrawRectangle(0, 0, _mapSystem.MapWidth, _mapSystem.MapHeight, new Color(30, 40, 30, 255));
        
        // 3. Grid (Optional, but good for spatial awareness)
        for(int x = 0; x <= _mapSystem.MapWidth; x+=50) 
            Raylib.DrawLine(x, 0, x, _mapSystem.MapHeight, new Color(50, 60, 50, 255));
        for(int y = 0; y <= _mapSystem.MapHeight; y+=50) 
            Raylib.DrawLine(0, y, _mapSystem.MapWidth, y, new Color(50, 60, 50, 255));

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
                else
                {
                    Raylib.DrawRectangle((int)pos.Position.X, (int)pos.Position.Y, (int)render.Width, (int)render.Height, render.Color);
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
        
        Raylib.EndMode2D();
    }
}
