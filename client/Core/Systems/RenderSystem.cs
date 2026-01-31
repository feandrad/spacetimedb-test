using System.Numerics;
using Guildmaster.Client.Core.Components;
using Guildmaster.Client.Core.ECS;
using Raylib_cs;
using SpacetimeDB.Types;

namespace Guildmaster.Client.Core.Systems;

public class RenderSystem : ISystem
{
    private Texture2D _tilesetTexture;
    private const int TILE_SIZE = 8;
    private const int TILESET_COLUMNS = 8;
    private readonly GameWorld _world;
    private readonly MapSystem _mapSystem;
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
        _camera.Offset = new Vector2(400, 225);

        _tilesetTexture = Raylib.LoadTexture("Assets/assets.png");
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

        DrawMap();
        DrawEntities();

        Raylib.EndMode2D();
    }

    private void DrawMap()
    {
        var tiles = _mapSystem.TileData;
        int width = _mapSystem.MapWidth;

        if (width == 0) return;

        for (int i = 0; i < tiles.Count; i++)
        {
            uint tileId = tiles[i];

            // Posição no Mundo (Tela)
            int mapX = (i % width) * TILE_SIZE;
            int mapY = (i / width) * TILE_SIZE;

            // Recorte da Imagem (Assets.png)
            int srcX = (int)(tileId % TILESET_COLUMNS) * TILE_SIZE;
            int srcY = (int)(tileId / TILESET_COLUMNS) * TILE_SIZE;

            Rectangle source = new Rectangle(srcX, srcY, TILE_SIZE, TILE_SIZE);
            Vector2 dest = new Vector2(mapX, mapY);

            Raylib.DrawTextureRec(_tilesetTexture, source, dest, Color.White);
        }
    }

    private void RenderOverlay(string title, Color color, string sub = "")
    {
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawText(title, 250, 200, 20, color);
        if (!string.IsNullOrEmpty(sub)) Raylib.DrawText(sub, 250, 230, 10, Color.Gray);
    }

    public void DrawEntities()
    {
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
            }
        }
    }
}
