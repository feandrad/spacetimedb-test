using Guildmaster.Client.Core.ECS;
using Guildmaster.Client.Core.Systems;
using Guildmaster.Client.Network;
using Guildmaster.Client.Repository;
using Guildmaster.Client.Input;
using Guildmaster.Client.Core.Components;
using SpacetimeDB.Types;
using Raylib_cs;

namespace Guildmaster.Client.Core;

public class Game
{
    private GameState _state = GameState.Login;
    private string _usernameInput = ""; // Keep for login
    private GuildmasterClient? _client;
    
    // Auto-register state
    private string _errorMessage = "";
    private float _stateTimer = 0f;
    private const float CONNECT_TIMEOUT = 5.0f;
    
    // ECS
    private GameWorld? _world;
    private PlayerRepository? _playerRepo;

    public void Initialize()
    {
        // 1. Setup Client
        _client = new GuildmasterClient();
        _client.OnConnectionError += (err) => 
        {
            _errorMessage = $"Connection Failed: {err}";
            _state = GameState.Login;
            Console.WriteLine($"[Game] {_errorMessage}");
        }; 
        _client.OnRegistrationError += (err) =>
        {
            _errorMessage = $"Registration Failed: {err}";
            _state = GameState.Login;
            Console.WriteLine($"[Game] {_errorMessage}");
        }; 

        // 2. Setup ECS
        _world = new GameWorld();

        // 3. Setup Dependencies
        _playerRepo = new PlayerRepository(_client);
        var inputService = new RaylibInputService();

        // 4. Create Systems (Order matters!)
        var networkSystem = new NetworkSystem(_client);
        var mapSystem = new MapSystem(_client, _playerRepo);
        var syncSystem = new SyncSystem(_world, networkSystem, _client, mapSystem); // Need to update SyncSystem too!
        var inputSystem = new InputSystem(_world, inputService, networkSystem);
        var renderSystem = new RenderSystem(_world, mapSystem, _client.Connection);

        // 5. Register Systems
        _world.AddSystem(networkSystem);
        _world.AddSystem(mapSystem);
        _world.AddSystem(syncSystem);
        _world.AddSystem(inputSystem);
        _world.AddSystem(renderSystem);
    }

    private int _tickDebug = 0;
    public void Update()
    {
        _client?.Tick();
        _tickDebug++;
        if (_tickDebug % 120 == 0) Console.WriteLine($"[DEBUG] Game Tick {_tickDebug} - Connected: {_client?.IsConnected}, Identity: {_client?.Identity}");

        switch (_state)
        {
            case GameState.Login:
                UpdateLogin();
                break;
            case GameState.Connecting:
            case GameState.Registering:
                UpdateLogin();
                // Run ECS to ensure systems (Sync, Network) are active
                _world?.Update(Raylib.GetFrameTime());
                break;
            case GameState.Playing:
                // ECS Update
                _world?.Update(Raylib.GetFrameTime());
                break;
        }
    }

    public void Draw()
    {
        if (_state == GameState.Playing)
        {
            // RenderSystem handles the game view (Clear + Map + Entities)
            _world?.Draw();
            
            // HUD
            var localPlayer = _playerRepo?.GetLocalPlayer();
            if (localPlayer != null)
            {
                Raylib.DrawText($"Playing as: {localPlayer.UsernameDisplay} (ID: {localPlayer.Id})", 10, 10, 20, Color.White);
                Raylib.DrawText($"Map: {localPlayer.CurrentMapId} | Pos: ({localPlayer.PositionX:F0}, {localPlayer.PositionY:F0})", 10, 35, 10, Color.Gray);
            }
            else
            {
                 Raylib.DrawText("Spectating / Syncing...", 10, 10, 20, Color.Yellow);
            }
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

    private string? _lastLoadedMap = null;

    private void UpdateLogin()
    {
        // Always handle input if in Login state (and maybe error state)
        if (_state == GameState.Login)
        {
             int key = Raylib.GetCharPressed();
             while (key > 0)
             {
                 if ((key >= 32) && (key <= 125) && (_usernameInput.Length < 16))
                 {
                     _usernameInput += (char)key;
                 }
                 key = Raylib.GetCharPressed();
             }

             if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _usernameInput.Length > 0)
             {
                 _usernameInput = _usernameInput.Substring(0, _usernameInput.Length - 1);
             }

             // Submit
             if (Raylib.IsKeyPressed(KeyboardKey.Enter) && !string.IsNullOrWhiteSpace(_usernameInput))
             {
                 _state = GameState.Connecting;
                 _errorMessage = "";
                 _stateTimer = 0f;
                 Console.WriteLine($"[Game] Connecting as {_usernameInput}...");
                 
                 // If already connected (e.g. from previous attempt?), disconnect first?
                 if (_client!.IsConnected) _client.Disconnect();
                 
                 _client.Connect("http://localhost:7734", "guildmaster");
             }
        }
        else if (_state == GameState.Connecting)
        {
             _stateTimer += Raylib.GetFrameTime();
             
             if (_client != null && _client.IsConnected && _client.Identity != null)
             {
                 Console.WriteLine("[Game] Connected! Checking registration...");
                 _state = GameState.Registering;
                 _stateTimer = 0f;
             }
             
             if (_stateTimer > CONNECT_TIMEOUT)
             {
                 _errorMessage = "Connection Timed Out.";
                 _state = GameState.Login;
                 _client?.Disconnect();
             }
        }
        else if (_state == GameState.Registering)
        {
             // Wait for syncing to find if we exist
             var localPlayer = _playerRepo?.GetLocalPlayer();
             
             if (localPlayer != null)
             {
                 // We exist! Match username?
                 if (!localPlayer.UsernameDisplay.Equals(_usernameInput, StringComparison.OrdinalIgnoreCase))
                 {
                     Console.WriteLine($"[Game] Identity exists as '{localPlayer.UsernameDisplay}'. Ignoring input '{_usernameInput}' and using existing.");
                 }
                 
                 JoinGame(localPlayer);
             }
             else
             {
                 // Not found yet. 
                 
                 // Debug: specific to diagnosis
                 if (_tickDebug % 60 == 0) // Every ~1s
                 {
                      var count = _client?.Connection?.Db?.Player?.Count ?? 0;
                      Console.WriteLine($"[Game] Check registration... LocalPlayer: null. Total Players in DB: {count}");
                 }
                 
                 if (_stateTimer == 0f) // First frame of Registering
                 {
                     Console.WriteLine($"[Game] Registering/Checking player '{_usernameInput}'...");
                     _client?.Register(_usernameInput);
                 }
                 
                 _stateTimer += Raylib.GetFrameTime();
                 
                 // If we wait too long, something is wrong? or just lag.
                 if (_stateTimer > 5.0f)
                 {
                     _errorMessage = "Registration/Sync Timed Out.";
                     _state = GameState.Login; // Back to login? or stay?
                     // Verify connection
                     if (_client?.IsConnected == false) _state = GameState.Login;
                 }
             }
        }
    }

    private void JoinGame(Player player)
    {
         Console.WriteLine($"[Game] Joining as {player.UsernameDisplay}...");
         if (_lastLoadedMap != player.CurrentMapId)
         {
             _client?.SubscribeToMap(player.CurrentMapId);
             _lastLoadedMap = player.CurrentMapId;
         }
         _state = GameState.Playing;
    }

    private void DrawLogin()
    {
        Raylib.DrawText("Welcome to Guildmaster", 100, 50, 20, Color.Gold);
        
        // Input Box
        Raylib.DrawText("Enter Username:", 100, 100, 20, Color.White);
        Raylib.DrawRectangle(100, 130, 200, 30, Color.LightGray);
        Raylib.DrawRectangleLines(100, 130, 200, 30, Color.DarkGray);
        Raylib.DrawText(_usernameInput, 105, 138, 20, Color.Black);
        
        // Connect Button
        Color btnColor = Color.DarkBlue;
        if (Raylib.GetMouseX() >= 100 && Raylib.GetMouseX() <= 300 && 
            Raylib.GetMouseY() >= 170 && Raylib.GetMouseY() <= 200)
        {
            btnColor = Color.Blue;
             if (Raylib.IsMouseButtonPressed(MouseButton.Left) && _state == GameState.Login && !string.IsNullOrWhiteSpace(_usernameInput))
             {
                 // Simulate Enter
                 _state = GameState.Connecting;
                 _errorMessage = "";
                 _stateTimer = 0f;
                 Console.WriteLine($"[Game] Connecting as {_usernameInput} (Mouse)...");
                 if (_client!.IsConnected) _client.Disconnect();
                 _client.Connect("http://localhost:7734", "guildmaster");
             }
        }
        
        Raylib.DrawRectangle(100, 170, 200, 30, btnColor);
        Raylib.DrawText("CONNECT", 160, 178, 10, Color.White);

        // Status / Error
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            Raylib.DrawText(_errorMessage, 100, 210, 20, Color.Red);
        }
        else if (_state == GameState.Connecting)
        {
            Raylib.DrawText("Connecting...", 100, 210, 20, Color.Yellow);
        }
        else if (_state == GameState.Registering)
        {
            Raylib.DrawText("Creating Player...", 100, 210, 20, Color.Orange);
        }
        else
        {
            Raylib.DrawText("Press ENTER or Click Connect", 100, 240, 10, Color.Gray);
        }
    }
}
