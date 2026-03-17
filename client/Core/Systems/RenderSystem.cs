using System.Linq;
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
    private const float SCALE = 4.0f;

    private readonly GameWorld _world;
    private readonly MapSystem _mapSystem;
    private readonly DbConnection? _conn;
    private InputSystem? _inputSystem;

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

    public void SetInputSystem(InputSystem inputSystem)
    {
        _inputSystem = inputSystem;
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
                Vector2 visualTarget = pos.Position * SCALE;
                _camera.Target = new Vector2((int)visualTarget.X, (int)visualTarget.Y);
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
        DrawContainerPrompts();

        Raylib.EndMode2D();

        // UI overlay (screen space)
        DrawContainerUI();
    }

    private void DrawMap()
    {
        var tiles = _mapSystem.TileData;
        int width = _mapSystem.MapWidth; // Largura em TILES (ex: 50)

        if (width == 0) return;

        for (int i = 0; i < tiles.Count; i++)
        {
            uint tileId = tiles[i];

            // Calcular posição no Grid
            int gridX = i % width;
            int gridY = i / width;

            // 1. Recorte da Imagem (Source) - Mantém 8x8 original
            int srcX = (int)(tileId % TILESET_COLUMNS) * TILE_SIZE;
            int srcY = (int)(tileId / TILESET_COLUMNS) * TILE_SIZE;
            Rectangle source = new Rectangle(srcX, srcY, TILE_SIZE, TILE_SIZE);

            Rectangle dest = new Rectangle(
                gridX * TILE_SIZE * SCALE,
                gridY * TILE_SIZE * SCALE,
                TILE_SIZE * SCALE,
                TILE_SIZE * SCALE
            );

            // Desenha esticado
            Raylib.DrawTexturePro(_tilesetTexture, source, dest, Vector2.Zero, 0f, Color.White);
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
                Vector2 drawPos = pos.Position * SCALE;
                if (render.IsCircle)
                {
                    Raylib.DrawCircleV(drawPos, render.Radius, render.Color);
                }
                else
                {
                    float visualWidth = render.Width * SCALE;
                    float visualHeight = render.Height * SCALE;

                    Raylib.DrawRectangle(
                        (int)(drawPos.X),
                        (int)(drawPos.Y),
                        (int)visualWidth,
                        (int)visualHeight,
                        render.Color
                    );
                }
            }
        }
    }

    private void DrawContainerPrompts()
    {
        // Show [E] prompt above nearby containers when not already open
        if (_inputSystem?.OpenContainerId != null) return;

        var localPlayer = _world.GetEntities()
            .FirstOrDefault(e => e.GetComponent<PlayerComponent>()?.IsLocalPlayer == true);
        if (localPlayer == null) return;

        var playerPos = localPlayer.GetComponent<PositionComponent>();
        if (playerPos == null) return;

        foreach (var entity in _world.GetEntities())
        {
            var container = entity.GetComponent<ContainerComponent>();
            var pos = entity.GetComponent<PositionComponent>();
            if (container == null || pos == null) continue;

            float dx = playerPos.Position.X - pos.Position.X;
            float dy = playerPos.Position.Y - pos.Position.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist < 16.0f)
            {
                Vector2 drawPos = pos.Position * SCALE;
                Raylib.DrawText("[E] Open", (int)drawPos.X - 16, (int)drawPos.Y - 20, 10, Color.White);
            }
        }
    }

    private void DrawContainerUI()
    {
        if (_inputSystem?.OpenContainerId == null || _conn == null) return;

        uint containerId = _inputSystem.OpenContainerId.Value;
        string label = _inputSystem.OpenContainerLabel ?? "Chest";

        // Panel background
        int panelX = 500, panelY = 50, panelW = 280, panelH = 300;
        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, new Color(30, 30, 30, 220));
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Color.Gold);

        Raylib.DrawText(label, panelX + 10, panelY + 10, 16, Color.Gold);
        Raylib.DrawText("--- Container ---", panelX + 10, panelY + 30, 10, Color.Gray);

        int y = panelY + 50;
        foreach (var ci in _conn.Db.ContainerItem.Iter())
        {
            if (ci.ContainerId != containerId) continue;
            Raylib.DrawText($"  {ci.ItemId} x{ci.Quantity}", panelX + 10, y, 12, Color.White);
            y += 18;
        }

        if (y == panelY + 50)
        {
            Raylib.DrawText("  (empty)", panelX + 10, y, 12, Color.DarkGray);
            y += 18;
        }

        // Player inventory section
        y += 10;
        Raylib.DrawText("--- Your Inventory ---", panelX + 10, y, 10, Color.Gray);
        y += 18;

        var localPlayer = _world.GetEntities()
            .FirstOrDefault(e => e.GetComponent<PlayerComponent>()?.IsLocalPlayer == true);
        if (localPlayer != null)
        {
            var playerId = localPlayer.GetComponent<PlayerComponent>()!.PlayerId;
            foreach (var inv in _conn.Db.InventoryItem.Iter())
            {
                if (inv.PlayerId != playerId) continue;
                Raylib.DrawText($"  {inv.ItemId} x{inv.Quantity}", panelX + 10, y, 12, Color.LightGray);
                y += 18;
            }
        }

        // Controls
        y += 10;
        Raylib.DrawText("[1] Deposit  [2] Withdraw  [E] Close", panelX + 10, y, 10, Color.Yellow);
    }

}
