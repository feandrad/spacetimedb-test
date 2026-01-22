using Raylib_cs;
using Guildmaster.Client.Core;
using Guildmaster.Client.Network;

class Program
{
    static void Main()
    {
        Raylib.InitWindow(800, 450, "Guildmaster – Login");
        Raylib.SetTargetFPS(60);

        var game = new Game();
        game.Initialize();

        while (!Raylib.WindowShouldClose())
        {
            game.Update();
            game.Draw();
        }

        game.Shutdown();
        Raylib.CloseWindow();
    }
}