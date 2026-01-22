using Guildmaster.Client.Network;
using Guildmaster.Client.Repository;
using Raylib_cs;

namespace Guildmaster.Client.Core;

public enum GameState
{
    Login,
    Loading,
    Playing
}

public class Game
{
    private GameState _state = GameState.Login;

    private string _usernameInput = "";
    private GuildmasterClient _client;
    private PlayerRepository _players;

    public void Initialize()
    {
        _client = new GuildmasterClient();
        _client.Connect("ws://localhost:7734", "guildmaster");

        _players = new PlayerRepository(_client);
    }

    public void Update()
    {
        switch (_state)
        {
            case GameState.Login:
                UpdateLogin();
                break;
        }
    }

    public void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.DarkGray);

        switch (_state)
        {
            case GameState.Login:
                DrawLogin();
                break;
        }

        Raylib.EndDrawing();
    }

    private void UpdateLogin()
    {
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if (_usernameInput.Length < 16)
                _usernameInput += (char)key;
            key = Raylib.GetCharPressed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _usernameInput.Length > 0)
            _usernameInput = _usernameInput[..^1];

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) && _usernameInput.Length > 0)
        {
            _state = GameState.Loading;

            bool success = _players.LoginOrRegister(_usernameInput);

            if (success)
                _state = GameState.Playing;
            else
                _state = GameState.Login;
        }
    }

    private void DrawLogin()
    {
        Raylib.DrawText("Enter Username:", 300, 160, 20, Color.White);
        Raylib.DrawRectangle(250, 200, 300, 40, Color.Black);
        Raylib.DrawText(_usernameInput, 260, 210, 20, Color.Green);

        Raylib.DrawText("Press ENTER to login", 280, 260, 14, Color.LightGray);
    }

    public void Shutdown()
    {
        _client.Disconnect();
    }
}
