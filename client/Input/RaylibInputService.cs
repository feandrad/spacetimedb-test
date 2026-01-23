using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Guildmaster.Client.Input;

public class RaylibInputService : IInputService
{
    private readonly Dictionary<CharacterAction, List<KeyboardKey>> _keyMappings;

    public RaylibInputService()
    {
        _keyMappings = new Dictionary<CharacterAction, List<KeyboardKey>>
        {
            { CharacterAction.MoveUp, new List<KeyboardKey> { KeyboardKey.W, KeyboardKey.Up } },
            { CharacterAction.MoveDown, new List<KeyboardKey> { KeyboardKey.S, KeyboardKey.Down } },
            { CharacterAction.MoveLeft, new List<KeyboardKey> { KeyboardKey.A, KeyboardKey.Left } },
            { CharacterAction.MoveRight, new List<KeyboardKey> { KeyboardKey.D, KeyboardKey.Right } },
            { CharacterAction.Interact, new List<KeyboardKey> { KeyboardKey.E, KeyboardKey.Space } },
            { CharacterAction.Attack, new List<KeyboardKey> { KeyboardKey.J, KeyboardKey.Z } },
            { CharacterAction.Running, new List<KeyboardKey> { KeyboardKey.LeftShift, KeyboardKey.RightShift } }
        };
    }

    public bool IsActionDown(CharacterAction action)
    {
        if (!_keyMappings.TryGetValue(action, out var keys)) return false;
        foreach (var key in keys)
        {
            if (Raylib.IsKeyDown(key)) return true;
        }
        return false;
    }

    public bool IsActionPressed(CharacterAction action)
    {
        if (!_keyMappings.TryGetValue(action, out var keys)) return false;
        foreach (var key in keys)
        {
            if (Raylib.IsKeyPressed(key)) return true;
        }
        return false;
    }

    public bool IsActionReleased(CharacterAction action)
    {
        if (!_keyMappings.TryGetValue(action, out var keys)) return false;
        foreach (var key in keys)
        {
            if (Raylib.IsKeyReleased(key)) return true;
        }
        return false;
    }

    public Vector2 GetMovementVector()
    {
        var input = Vector2.Zero;
        if (IsActionDown(CharacterAction.MoveRight)) input.X += 1;
        if (IsActionDown(CharacterAction.MoveLeft)) input.X -= 1;
        if (IsActionDown(CharacterAction.MoveDown)) input.Y += 1;
        if (IsActionDown(CharacterAction.MoveUp)) input.Y -= 1;

        if (input != Vector2.Zero)
        {
            input = Vector2.Normalize(input);
        }
        
        return input;
    }
}
