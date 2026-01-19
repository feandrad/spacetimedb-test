using Godot;

namespace GuildmasterMVP.Core
{
    public interface IInputManager
    {
        void RegisterAction(string actionName, InputBinding binding);
        bool IsActionPressed(string actionName);
        bool IsActionJustPressed(string actionName);
        Vector2 GetMovementVector();
        void RemapAction(string actionName, InputBinding newBinding);
    }

    public struct InputBinding
    {
        public string KeyCode;
        public string GamepadButton;
        public string GamepadAxis;
    }
}