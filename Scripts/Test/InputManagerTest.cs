using Godot;
using GuildmasterMVP.Core;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Test to verify InputManager functionality
    /// </summary>
    public partial class InputManagerTest : Node
    {
        private InputManager _inputManager;
        
        public override void _Ready()
        {
            GD.Print("=== InputManager Test ===");
            
            // Create InputManager instance
            _inputManager = new InputManager();
            AddChild(_inputManager);
            
            // Wait a frame for InputManager to initialize
            CallDeferred(nameof(RunTests));
        }
        
        private void RunTests()
        {
            TestActionRegistration();
            TestDefaultBindings();
            TestActionRemapping();
            TestMovementVector();
            
            GD.Print("=== InputManager Test Complete ===");
            
            // Clean up
            _inputManager?.QueueFree();
        }
        
        private void TestActionRegistration()
        {
            GD.Print("Testing Action Registration...");
            
            // Test registering a new action
            var testBinding = new InputBinding
            {
                KeyCode = "F",
                GamepadButton = "joypad_button_2",
                GamepadAxis = ""
            };
            
            _inputManager.RegisterAction("test_action", testBinding);
            
            // Verify action was registered
            bool hasAction = _inputManager.HasAction("test_action");
            var binding = _inputManager.GetActionBinding("test_action");
            
            GD.Print($"✓ Action registered: {hasAction}");
            GD.Print($"✓ Binding retrieved: KeyCode={binding?.KeyCode}, GamepadButton={binding?.GamepadButton}");
        }
        
        private void TestDefaultBindings()
        {
            GD.Print("Testing Default Bindings...");
            
            // Verify default movement actions exist
            string[] movementActions = { "move_up", "move_down", "move_left", "move_right" };
            
            foreach (string action in movementActions)
            {
                bool hasAction = _inputManager.HasAction(action);
                var binding = _inputManager.GetActionBinding(action);
                GD.Print($"✓ {action}: exists={hasAction}, KeyCode={binding?.KeyCode}");
            }
            
            // Verify combat actions exist
            string[] combatActions = { "attack", "interact" };
            
            foreach (string action in combatActions)
            {
                bool hasAction = _inputManager.HasAction(action);
                var binding = _inputManager.GetActionBinding(action);
                GD.Print($"✓ {action}: exists={hasAction}, KeyCode={binding?.KeyCode}");
            }
        }
        
        private void TestActionRemapping()
        {
            GD.Print("Testing Action Remapping...");
            
            // Get original binding
            var originalBinding = _inputManager.GetActionBinding("move_up");
            GD.Print($"Original move_up binding: {originalBinding?.KeyCode}");
            
            // Remap the action
            var newBinding = new InputBinding
            {
                KeyCode = "I",
                GamepadButton = "",
                GamepadAxis = "left_stick_up"
            };
            
            _inputManager.RemapAction("move_up", newBinding);
            
            // Verify remapping worked
            var remappedBinding = _inputManager.GetActionBinding("move_up");
            GD.Print($"✓ Remapped move_up binding: {remappedBinding?.KeyCode}");
            
            // Restore original binding
            if (originalBinding.HasValue)
            {
                _inputManager.RemapAction("move_up", originalBinding.Value);
            }
        }
        
        private void TestMovementVector()
        {
            GD.Print("Testing Movement Vector...");
            
            // Test that GetMovementVector returns a valid Vector2
            Vector2 movement = _inputManager.GetMovementVector();
            GD.Print($"✓ Movement vector: ({movement.X}, {movement.Y})");
            
            // Test that movement vector is normalized (length <= 1)
            float length = movement.Length();
            bool isNormalized = length <= 1.0f;
            GD.Print($"✓ Movement vector normalized: {isNormalized} (length: {length})");
        }
    }
}