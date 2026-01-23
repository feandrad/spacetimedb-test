using System.Numerics;

namespace Guildmaster.Client.Input;

public interface IInputService
{
    bool IsActionDown(CharacterAction action);
    bool IsActionPressed(CharacterAction action);
    bool IsActionReleased(CharacterAction action);
    Vector2 GetMovementVector();
}
