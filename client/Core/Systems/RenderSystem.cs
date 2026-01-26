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
        // 1. Estado de Transição/Carregamento
        if (_mapSystem.IsJoining)
        {
            RenderOverlay("JOINING WORLD...", Color.White);
            return;
        }

        // 2. Falha Crítica: O servidor não enviou a instância (Fail Fast)
        if (_mapSystem.MapWidth <= 0)
        {
            RenderOverlay("ERROR: MAP INSTANCE DATA MISSING", Color.Red,
                $"Map '{_mapSystem.CurrentMapId}' is not initialized in server DB");
            return;
        }

        // 3. Renderização do Mundo (Apenas se houver dados válidos)
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginMode2D(_camera);

        DrawWorldState(); // Encapsula a lógica de Grid e Entidades

        Raylib.EndMode2D();
    }

    private void RenderOverlay(string title, Color color, string sub = "")
    {
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawText(title, 250, 200, 20, color);
        if (!string.IsNullOrEmpty(sub)) Raylib.DrawText(sub, 250, 230, 10, Color.Gray);
    }

    public void DrawWorldState()
    {
        // Área Móvel (Verde Escuro) - Agora garantido que MapWidth > 0
        Raylib.DrawRectangle(0, 0, _mapSystem.MapWidth, _mapSystem.MapHeight, new Color(30, 40, 30, 255));

        // Grid
        for(int x = 0; x <= _mapSystem.MapWidth; x+=50)
            Raylib.DrawLine(x, 0, x, _mapSystem.MapHeight, new Color(50, 60, 50, 255));
        for(int y = 0; y <= _mapSystem.MapHeight; y+=50)
            Raylib.DrawLine(0, y, _mapSystem.MapWidth, y, new Color(50, 60, 50, 255));

        // Desenhar Entidades
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
                    // Aqui as áreas marrons (transições) serão desenhadas
                    Raylib.DrawRectangle((int)pos.Position.X, (int)pos.Position.Y, (int)render.Width, (int)render.Height, render.Color);
                }

                // ... (restante do código de UI de Player e Enemy)
            }
        }
    }
}
