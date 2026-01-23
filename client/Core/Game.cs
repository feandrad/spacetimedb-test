using Guildmaster.Client.Core.ECS;
using Guildmaster.Client.Core.Systems;
using Guildmaster.Client.Network;
using Guildmaster.Client.Repository;
using Guildmaster.Client.Input;
using Raylib_cs;

namespace Guildmaster.Client.Core;

public class Game
{
    private GameState _state = GameState.Login;
    private string _usernameInput = ""; // Keep for login
    private GuildmasterClient? _client;
    
    // ECS
    private GameWorld? _world;
    private PlayerRepository? _playerRepo;

    public void Initialize()
    {
        // 1. Setup Client
        _client = new GuildmasterClient();
        _client.Connect("ws://localhost:7734", "guildmaster"); 

        // 2. Setup ECS
        _world = new GameWorld();

        // 3. Setup Dependencies
        _playerRepo = new PlayerRepository(_client);
        var inputService = new RaylibInputService();

        // 4. Create Systems (Order matters!)
        var networkSystem = new NetworkSystem(_client);
        var mapSystem = new MapSystem(_client, _playerRepo);
        var syncSystem = new SyncSystem(_world, networkSystem, _client);
        var inputSystem = new InputSystem(_world, inputService, networkSystem);
        var renderSystem = new RenderSystem(_world, _client.Connection);

        // 5. Register Systems
        _world.AddSystem(networkSystem);
        _world.AddSystem(mapSystem);
        _world.AddSystem(syncSystem);
        _world.AddSystem(inputSystem);
        _world.AddSystem(renderSystem);
    }

    public void Update()
    {
        switch (_state)
        {
            case GameState.Login:
                UpdateLogin();
                break;
            case GameState.Playing:
                // ECS Update
                _world?.Update(Raylib.GetFrameTime());
                break;
        }
    }

    public void Draw()
    {
        Raylib.BeginDrawing();
        
        if (_state == GameState.Playing)
        {
            Raylib.ClearBackground(Color.Black);
            
            // Map Background (Placeholder, ideally MapSystem or RenderSystem handles this if using tiles)
            // For now, RenderSystem handles entities. 
            // We should move map background drawing to RenderSystem or a MapRenderSystem.
            // But MapRenderer was replaced. MapSystem logic is strict data.
            // Let's assume Map is empty/black for now or RenderSystem draws it?
            // The old MapRenderer drew "Map: ID".
            // I'll leave background black.
            
            _world?.Draw();
        }
        else
        {
            Raylib.ClearBackground(Color.DarkGray);
            DrawLogin();
        }

        Raylib.EndDrawing();
    }

    public void Shutdown()
    {
        _client?.Disconnect();
    }

    private void UpdateLogin()
    {
        if (_client?.IsConnected == true && _client.Identity != null)
        {
             // Check if we already have a player (Automatic Login or Post-Registration Login)
             var localPlayer = _playerRepo?.GetLocalPlayer();
             if (localPlayer != null)
             {
                 _state = GameState.Playing;
                 return;
             }
        }
        
        // Handle Text Input
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if ((key >= 32) && (key <= 125) && (_usernameInput.Length < 16))
            {
                _usernameInput += (char)key;
            }
            key = Raylib.GetCharPressed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
        {
            if (_usernameInput.Length > 0)
            {
                _usernameInput = _usernameInput.Substring(0, _usernameInput.Length - 1);
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) && !string.IsNullOrWhiteSpace(_usernameInput))
        {
            if (_client?.IsConnected == true)
            {
                _client.Register(_usernameInput);
                // We don't switch state immediately. We wait for the Player entity to appear.
                // The check below handles the switch.
            }
        }
    }

    private void DrawLogin()
    {
        Raylib.DrawText("Welcome to Guildmaster", 100, 50, 20, Color.Gold);
        
        Raylib.DrawText("Enter Username:", 100, 100, 20, Color.White);
        
        // Input Box
        Raylib.DrawRectangle(100, 130, 200, 30, Color.LightGray);
        Raylib.DrawRectangleLines(100, 130, 200, 30, Color.DarkGray);
        Raylib.DrawText(_usernameInput, 105, 138, 20, Color.Black);
        
        if (_client?.IsConnected == true)
        {
             if (_client.Identity != null)
                 Raylib.DrawText($"Connected! Identity: {_client.Identity}", 100, 170, 10, Color.Green);
             else
                 Raylib.DrawText("Identifying...", 100, 170, 10, Color.Yellow);
        }
        else
        {
             Raylib.DrawText("Connecting...", 100, 170, 10, Color.Yellow);
        }

        Raylib.DrawText("Press ENTER to Join", 100, 200, 20, Color.Gray);
    }
}