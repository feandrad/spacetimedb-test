using Godot;
using GuildmasterMVP.Core;
using GuildmasterMVP.Visual;
using GuildmasterMVP.Audio;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Test script for visual effects and audio systems
    /// Requirements 10.1-10.8: Visual and audio feedback testing
    /// </summary>
    public partial class VisualEffectsTest : Node2D
    {
        private VisualEffectsManager _visualEffects;
        private AudioManager _audioManager;
        private AnimationManager _animationManager;
        
        private uint _testPlayerId = 1;
        private uint _testEnemyId = 1000001;
        
        public override void _Ready()
        {
            // Create visual effects manager
            _visualEffects = new VisualEffectsManager();
            AddChild(_visualEffects);
            _visualEffects.Name = "VisualEffectsManager";
            
            // Create audio manager
            _audioManager = new AudioManager();
            AddChild(_audioManager);
            _audioManager.Name = "AudioManager";
            
            // Create animation manager
            _animationManager = new AnimationManager();
            AddChild(_animationManager);
            _animationManager.Name = "AnimationManager";
            
            GD.Print("VisualEffectsTest initialized - Press keys to test effects:");
            GD.Print("1 - Sword attack telegraph");
            GD.Print("2 - Axe attack telegraph");
            GD.Print("3 - Bow attack telegraph");
            GD.Print("4 - Hit effect (low damage)");
            GD.Print("5 - Hit effect (high damage)");
            GD.Print("6 - Projectile impact");
            GD.Print("7 - Enemy attack telegraph");
            GD.Print("8 - Item bob effect");
            GD.Print("9 - Interaction sounds");
        }
        
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                Vector2 testPosition = GetGlobalMousePosition();
                Vector2 testDirection = Vector2.Right;
                
                switch (keyEvent.Keycode)
                {
                    case Key.Key1:
                        TestSwordAttack(testPosition, testDirection);
                        break;
                    case Key.Key2:
                        TestAxeAttack(testPosition, testDirection);
                        break;
                    case Key.Key3:
                        TestBowAttack(testPosition, testDirection);
                        break;
                    case Key.Key4:
                        TestHitEffect(testPosition, 15.0f);
                        break;
                    case Key.Key5:
                        TestHitEffect(testPosition, 45.0f);
                        break;
                    case Key.Key6:
                        TestProjectileImpact(testPosition);
                        break;
                    case Key.Key7:
                        TestEnemyAttack(testPosition, testDirection);
                        break;
                    case Key.Key8:
                        TestItemBobEffect(testPosition);
                        break;
                    case Key.Key9:
                        TestInteractionSounds(testPosition);
                        break;
                }
            }
        }
        
        private void TestSwordAttack(Vector2 position, Vector2 direction)
        {
            GD.Print("Testing sword attack telegraph and audio");
            _visualEffects.ShowAttackTelegraph(_testPlayerId, WeaponType.Sword, position, direction);
        }
        
        private void TestAxeAttack(Vector2 position, Vector2 direction)
        {
            GD.Print("Testing axe attack telegraph and audio");
            _visualEffects.ShowAttackTelegraph(_testPlayerId, WeaponType.Axe, position, direction);
        }
        
        private void TestBowAttack(Vector2 position, Vector2 direction)
        {
            GD.Print("Testing bow attack telegraph and audio");
            _visualEffects.ShowAttackTelegraph(_testPlayerId, WeaponType.Bow, position, direction);
        }
        
        private void TestHitEffect(Vector2 position, float damage)
        {
            GD.Print($"Testing hit effect with {damage} damage and audio");
            _visualEffects.ShowHitEffect(_testEnemyId, position, damage);
        }
        
        private void TestProjectileImpact(Vector2 position)
        {
            GD.Print("Testing projectile impact effect and audio");
            _visualEffects.ShowProjectileImpact(position, ProjectileType.Arrow);
        }
        
        private void TestEnemyAttack(Vector2 position, Vector2 direction)
        {
            GD.Print("Testing enemy attack telegraph and audio");
            _visualEffects.ShowEnemyAttackTelegraph(_testEnemyId, position, direction);
        }
        
        private void TestItemBobEffect(Vector2 position)
        {
            GD.Print("Testing item bob animation and audio");
            var itemEffect = _visualEffects.CreateItemBobEffect(position, "fruit");
            
            // Test collection animation after 3 seconds
            GetTree().CreateTimer(3.0f).Timeout += () => {
                if (IsInstanceValid(itemEffect))
                {
                    itemEffect.AnimateCollection(() => {
                        GD.Print("Item collected!");
                    });
                }
            };
        }
        
        private void TestInteractionSounds(Vector2 position)
        {
            GD.Print("Testing interaction sounds");
            
            // Test different interaction sounds with delays
            _visualEffects.PlayInteractionSound("shake", position);
            
            GetTree().CreateTimer(0.5f).Timeout += () => {
                _visualEffects.PlayInteractionSound("cut", position);
            };
            
            GetTree().CreateTimer(1.0f).Timeout += () => {
                _visualEffects.PlayInteractionSound("break", position);
            };
            
            GetTree().CreateTimer(1.5f).Timeout += () => {
                _visualEffects.PlayInteractionSound("pickup", position);
            };
        }
        
        public override void _Draw()
        {
            // Draw instructions
            var font = ThemeDB.FallbackFont;
            var instructions = new string[]
            {
                "Visual Effects Test - Press keys to test:",
                "1-3: Weapon attack telegraphs",
                "4-5: Hit effects (low/high damage)",
                "6: Projectile impact",
                "7: Enemy attack telegraph",
                "8: Item bob animation",
                "9: Interaction sounds"
            };
            
            for (int i = 0; i < instructions.Length; i++)
            {
                DrawString(font, new Vector2(10, 30 + i * 20), instructions[i], HorizontalAlignment.Left, -1, 16);
            }
        }
    }
}