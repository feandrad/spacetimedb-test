// Core/Game.cs

using Guildmaster.Client.Network;
using Guildmaster.Client.Repository;
using Guildmaster.Client.World;
using Raylib_cs;

public class Game
{
    private GameState _state = GameState.Login;
    private string _usernameInput = "";
    private GuildmasterClient _client;
    private PlayerRepository _players;
    private MapState _mapState; // Adicionado

    // Seus renderizadores de ECS/Raylib
    private PlayerRenderer _playerRenderer = new();
    private MapRenderer _mapRenderer = new();

    public void Initialize()
    {
        _client = new GuildmasterClient();
        // Conecta ao servidor local onde rodam seus arquivos .rs
        _client.Connect("ws://localhost:7734", "guildmaster"); 

        _players = new PlayerRepository(_client);
        _mapState = new MapState(_client, _players);
    }

    public void Update()
    {
        _client.Tick(); // Processa mensagens do SpacetimeDB

        switch (_state)
        {
            case GameState.Login:
                UpdateLogin();
                break;
            case GameState.Playing:
                _mapState.Tick(); // Monitora mudanças de mapa do map.rs
                UpdatePlaying();
                break;
        }
    }

    public void Draw()
    {
        Raylib.BeginDrawing();
        
        if (_state == GameState.Playing)
        {
            Raylib.ClearBackground(Color.Black);
            // Primeiro o chão, depois os bonecos (estilo Zelda)
            _mapRenderer.Draw(_client.Connection, _mapState.CurrentMapId);
            _playerRenderer.Draw(_client.Connection);
        }
        else
        {
            Raylib.ClearBackground(Color.DarkGray);
            DrawLogin();
        }

        Raylib.EndDrawing();
    }
}