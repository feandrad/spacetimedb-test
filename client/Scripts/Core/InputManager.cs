using Godot;
using System.Collections.Generic;

namespace GuildmasterMVP.Core
{
    public partial class InputManager : Node, IInputManager
    {
        private Dictionary<string, InputBinding> _actionBindings = new Dictionary<string, InputBinding>();
        private Dictionary<string, bool> _actionStates = new Dictionary<string, bool>();
        private Dictionary<string, bool> _previousActionStates = new Dictionary<string, bool>();

        public override void _Ready()
        {
            // Initialize default console-first bindings
            InitializeDefaultBindings();
            
            // Set process mode to always to handle input even when paused
            ProcessMode = ProcessModeEnum.Always;
        }

        public override void _Process(double delta)
        {
            // Update previous states for just pressed detection
            _previousActionStates.Clear();
            foreach (var kvp in _actionStates)
            {
                _previousActionStates[kvp.Key] = kvp.Value;
            }

            // Update current action states
            UpdateActionStates();
        }

        private void InitializeDefaultBindings()
        {
            // Movement actions - console-first design with WASD primary
            RegisterAction("move_up", new InputBinding 
            { 
                KeyCode = "W", 
                GamepadButton = "", 
                GamepadAxis = "left_stick_up" 
            });
            
            RegisterAction("move_down", new InputBinding 
            { 
                KeyCode = "S", 
                GamepadButton = "", 
                GamepadAxis = "left_stick_down" 
            });
            
            RegisterAction("move_left", new InputBinding 
            { 
                KeyCode = "A", 
                GamepadButton = "", 
                GamepadAxis = "left_stick_left" 
            });
            
            RegisterAction("move_right", new InputBinding 
            { 
                KeyCode = "D", 
                GamepadButton = "", 
                GamepadAxis = "left_stick_right" 
            });

            // Combat actions
            RegisterAction("attack", new InputBinding 
            { 
                KeyCode = "Space", 
                GamepadButton = "joypad_button_0", // A/X button
                GamepadAxis = "" 
            });

            // Interaction actions
            RegisterAction("interact", new InputBinding 
            { 
                KeyCode = "E", 
                GamepadButton = "joypad_button_1", // B/Circle button
                GamepadAxis = "" 
            });

            // Inventory actions
            RegisterAction("open_inventory", new InputBinding 
            { 
                KeyCode = "Tab", 
                GamepadButton = "joypad_button_6", // Back/Select button
                GamepadAxis = "" 
            });

            // Equipment switching
            RegisterAction("switch_weapon", new InputBinding 
            { 
                KeyCode = "Q", 
                GamepadButton = "joypad_button_4", // LB/L1 button
                GamepadAxis = "" 
            });
        }

        public void RegisterAction(string actionName, InputBinding binding)
        {
            _actionBindings[actionName] = binding;
            _actionStates[actionName] = false;
            _previousActionStates[actionName] = false;
        }

        public bool IsActionPressed(string actionName)
        {
            return _actionStates.GetValueOrDefault(actionName, false);
        }

        public bool IsActionJustPressed(string actionName)
        {
            bool currentState = _actionStates.GetValueOrDefault(actionName, false);
            bool previousState = _previousActionStates.GetValueOrDefault(actionName, false);
            return currentState && !previousState;
        }

        public Vector2 GetMovementVector()
        {
            Vector2 movement = Vector2.Zero;

            // Get keyboard/digital input
            if (IsActionPressed("move_up"))
                movement.Y -= 1.0f;
            if (IsActionPressed("move_down"))
                movement.Y += 1.0f;
            if (IsActionPressed("move_left"))
                movement.X -= 1.0f;
            if (IsActionPressed("move_right"))
                movement.X += 1.0f;

            // Get gamepad analog input (overrides digital if present)
            Vector2 analogMovement = GetAnalogMovement();
            if (analogMovement.Length() > 0.1f) // Deadzone
            {
                movement = analogMovement;
            }

            // Normalize diagonal movement to prevent faster diagonal movement
            if (movement.Length() > 1.0f)
            {
                movement = movement.Normalized();
            }

            return movement;
        }

        public void RemapAction(string actionName, InputBinding newBinding)
        {
            if (_actionBindings.ContainsKey(actionName))
            {
                _actionBindings[actionName] = newBinding;
            }
        }

        private void UpdateActionStates()
        {
            foreach (var kvp in _actionBindings)
            {
                string actionName = kvp.Key;
                InputBinding binding = kvp.Value;
                
                bool isPressed = false;

                // Check keyboard input
                if (!string.IsNullOrEmpty(binding.KeyCode))
                {
                    Key key = GetKeyFromString(binding.KeyCode);
                    if (key != Key.None)
                    {
                        isPressed |= Input.IsKeyPressed(key);
                    }
                }

                // Check gamepad button input
                if (!string.IsNullOrEmpty(binding.GamepadButton))
                {
                    JoyButton button = GetJoyButtonFromString(binding.GamepadButton);
                    if (button != JoyButton.Invalid)
                    {
                        // Check all connected gamepads
                        for (int i = 0; i < 4; i++)
                        {
                            if (Input.IsJoyButtonPressed(i, button))
                            {
                                isPressed = true;
                                break;
                            }
                        }
                    }
                }

                _actionStates[actionName] = isPressed;
            }
        }

        private Vector2 GetAnalogMovement()
        {
            Vector2 analogMovement = Vector2.Zero;

            // Check all connected gamepads for analog stick input
            for (int i = 0; i < 4; i++)
            {
                float leftX = Input.GetJoyAxis(i, JoyAxis.LeftX);
                float leftY = Input.GetJoyAxis(i, JoyAxis.LeftY);
                
                Vector2 stickInput = new Vector2(leftX, leftY);
                
                // Apply deadzone
                if (stickInput.Length() > 0.1f)
                {
                    analogMovement = stickInput;
                    break; // Use first active gamepad
                }
            }

            return analogMovement;
        }

        private Key GetKeyFromString(string keyString)
        {
            return keyString.ToUpper() switch
            {
                "W" => Key.W,
                "A" => Key.A,
                "S" => Key.S,
                "D" => Key.D,
                "E" => Key.E,
                "Q" => Key.Q,
                "SPACE" => Key.Space,
                "TAB" => Key.Tab,
                "ENTER" => Key.Enter,
                "ESCAPE" => Key.Escape,
                "SHIFT" => Key.Shift,
                "CTRL" => Key.Ctrl,
                "ALT" => Key.Alt,
                "UP" => Key.Up,
                "DOWN" => Key.Down,
                "LEFT" => Key.Left,
                "RIGHT" => Key.Right,
                _ => Key.None
            };
        }

        private JoyButton GetJoyButtonFromString(string buttonString)
        {
            return buttonString switch
            {
                "joypad_button_0" => JoyButton.A,           // A/X button
                "joypad_button_1" => JoyButton.B,           // B/Circle button
                "joypad_button_2" => JoyButton.X,           // X/Square button
                "joypad_button_3" => JoyButton.Y,           // Y/Triangle button
                "joypad_button_4" => JoyButton.LeftShoulder, // LB/L1
                "joypad_button_5" => JoyButton.RightShoulder, // RB/R1
                "joypad_button_6" => JoyButton.Back,        // Back/Select
                "joypad_button_7" => JoyButton.Start,       // Start
                "joypad_button_8" => JoyButton.LeftStick,   // Left stick click
                "joypad_button_9" => JoyButton.RightStick,  // Right stick click
                _ => JoyButton.Invalid
            };
        }

        // Public method to get all registered actions (useful for UI)
        public Dictionary<string, InputBinding> GetAllActions()
        {
            return new Dictionary<string, InputBinding>(_actionBindings);
        }

        // Public method to check if an action exists
        public bool HasAction(string actionName)
        {
            return _actionBindings.ContainsKey(actionName);
        }

        // Public method to get current binding for an action
        public InputBinding? GetActionBinding(string actionName)
        {
            return _actionBindings.TryGetValue(actionName, out InputBinding binding) ? binding : null;
        }
    }
}