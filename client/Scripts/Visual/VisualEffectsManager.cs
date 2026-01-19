using Godot;
using GuildmasterMVP.Core;
using System.Collections.Generic;
using GuildmasterMVP.Data;
using GuildmasterMVP.Audio;

namespace GuildmasterMVP.Visual
{
    /// <summary>
    /// Manages visual effects using shader-based implementations
    /// Requirements 10.1, 10.2, 10.3, 10.4: Attack telegraph and hit confirmation effects
    /// </summary>
    public partial class VisualEffectsManager : Node2D
    {
        private Dictionary<uint, AttackTelegraphEffect> _activeTelegraphs = new Dictionary<uint, AttackTelegraphEffect>();
        private Dictionary<uint, HitEffect> _activeHitEffects = new Dictionary<uint, HitEffect>();
        private List<ItemBobEffect> _itemBobEffects = new List<ItemBobEffect>();
        
        // Shader resources
        private Shader _attackTelegraphShader;
        private Shader _hitEffectShader;
        private Shader _itemBobShader;
        
        // Audio manager reference
        private AudioManager _audioManager;
        
        public override void _Ready()
        {
            LoadShaders();
            
            // Get or create AudioManager
            _audioManager = GetNode<AudioManager>("../AudioManager");
            if (_audioManager == null)
            {
                _audioManager = new AudioManager();
                GetParent().AddChild(_audioManager);
                _audioManager.Name = "AudioManager";
            }
            
            GD.Print("VisualEffectsManager initialized with shader-based effects and audio");
        }
        
        private void LoadShaders()
        {
            // Load shader resources
            _attackTelegraphShader = GD.Load<Shader>("res://Shaders/AttackTelegraph.gdshader");
            _hitEffectShader = GD.Load<Shader>("res://Shaders/HitEffect.gdshader");
            _itemBobShader = GD.Load<Shader>("res://Shaders/ItemBob.gdshader");
            
            if (_attackTelegraphShader == null)
                GD.PrintErr("Failed to load AttackTelegraph shader");
            if (_hitEffectShader == null)
                GD.PrintErr("Failed to load HitEffect shader");
            if (_itemBobShader == null)
                GD.PrintErr("Failed to load ItemBob shader");
        }
        
        /// <summary>
        /// Show attack telegraph effect for weapon attacks
        /// Requirements 10.1: Attack telegraph effects for all weapon types
        /// Requirements 10.2: Visual telegraph indicators for attack areas
        /// Requirements 10.5: Sound effects for combat
        /// </summary>
        public void ShowAttackTelegraph(uint playerId, WeaponType weaponType, Vector2 position, Vector2 direction, float duration = 0.5f)
        {
            // Remove existing telegraph for this player
            if (_activeTelegraphs.ContainsKey(playerId))
            {
                _activeTelegraphs[playerId].QueueFree();
                _activeTelegraphs.Remove(playerId);
            }
            
            // Create new telegraph effect
            var telegraph = new AttackTelegraphEffect();
            telegraph.Initialize(weaponType, position, direction, duration, _attackTelegraphShader);
            
            AddChild(telegraph);
            _activeTelegraphs[playerId] = telegraph;
            
            // Play combat sound effect
            if (_audioManager != null)
            {
                _audioManager.PlayCombatSound(weaponType, position);
            }
            
            // Schedule removal
            GetTree().CreateTimer(duration).Timeout += () => {
                if (_activeTelegraphs.ContainsKey(playerId))
                {
                    _activeTelegraphs[playerId].QueueFree();
                    _activeTelegraphs.Remove(playerId);
                }
            };
            
            GD.Print($"Showing {weaponType} telegraph with audio for player {playerId} at ({position.X:F1}, {position.Y:F1})");
        }
        
        /// <summary>
        /// Show hit confirmation effect when damage is dealt
        /// Requirements 10.3: Hit confirmation effects and audio
        /// Requirements 10.4: Damage feedback effects
        /// </summary>
        public void ShowHitEffect(uint targetId, Vector2 position, float damage, float duration = 0.8f)
        {
            // Remove existing hit effect for this target
            if (_activeHitEffects.ContainsKey(targetId))
            {
                _activeHitEffects[targetId].QueueFree();
                _activeHitEffects.Remove(targetId);
            }
            
            // Create new hit effect
            var hitEffect = new HitEffect();
            hitEffect.Initialize(position, damage, duration, _hitEffectShader);
            
            AddChild(hitEffect);
            _activeHitEffects[targetId] = hitEffect;
            
            // Play hit sound effect
            if (_audioManager != null)
            {
                _audioManager.PlayHitSound(targetId, damage, position);
            }
            
            // Schedule removal
            GetTree().CreateTimer(duration).Timeout += () => {
                if (_activeHitEffects.ContainsKey(targetId))
                {
                    _activeHitEffects[targetId].QueueFree();
                    _activeHitEffects.Remove(targetId);
                }
            };
            
            GD.Print($"Showing hit effect with audio on target {targetId} for {damage} damage at ({position.X:F1}, {position.Y:F1})");
        }
        
        /// <summary>
        /// Create item bob animation effect for collectible items
        /// Requirements 10.7: Simple visual effects and animations through shaders
        /// Requirements 10.8: Sound effects for interactions
        /// </summary>
        public ItemBobEffect CreateItemBobEffect(Vector2 position, string itemType)
        {
            var bobEffect = new ItemBobEffect();
            bobEffect.Initialize(position, itemType, _itemBobShader);
            
            AddChild(bobEffect);
            _itemBobEffects.Add(bobEffect);
            
            // Play item spawn sound
            if (_audioManager != null)
            {
                _audioManager.PlayInteractionSound("pickup", position);
            }
            
            GD.Print($"Created item bob effect with audio for {itemType} at ({position.X:F1}, {position.Y:F1})");
            return bobEffect;
        }
        
        /// <summary>
        /// Play interaction sound effect
        /// Requirements 10.8: Sound effects for interactions
        /// </summary>
        public void PlayInteractionSound(string interactionType, Vector2 position)
        {
            if (_audioManager != null)
            {
                _audioManager.PlayInteractionSound(interactionType, position);
            }
        }
        
        /// <summary>
        /// Remove item bob effect (when item is collected)
        /// </summary>
        public void RemoveItemBobEffect(ItemBobEffect effect)
        {
            if (_itemBobEffects.Contains(effect))
            {
                _itemBobEffects.Remove(effect);
                effect.QueueFree();
            }
        }
        
        /// <summary>
        /// Show projectile impact effect
        /// Requirements 10.6: Projectile visuals and impact effects
        /// </summary>
        public void ShowProjectileImpact(Vector2 position, ProjectileType projectileType)
        {
            // Create temporary impact effect
            var impact = new HitEffect();
            impact.Initialize(position, 0, 0.3f, _hitEffectShader);
            
            // Customize for projectile impact
            var material = impact.Material as ShaderMaterial;
            if (material != null)
            {
                material.SetShaderParameter("hit_color", new Vector4(0.8f, 0.8f, 1.0f, 1.0f)); // Blue-ish for projectile
                material.SetShaderParameter("flash_intensity", 1.5f);
            }
            
            AddChild(impact);
            
            // Play projectile impact sound
            if (_audioManager != null)
            {
                _audioManager.PlayProjectileImpact(projectileType, position);
            }
            
            // Auto-remove after duration
            GetTree().CreateTimer(0.3f).Timeout += () => {
                impact.QueueFree();
            };
            
            GD.Print($"Showing {projectileType} impact effect with audio at ({position.X:F1}, {position.Y:F1})");
        }
        
        /// <summary>
        /// Show enemy telegraph effect for incoming attacks
        /// Requirements 10.5: Enemy attack telegraph effects to warn players
        /// </summary>
        public void ShowEnemyAttackTelegraph(uint enemyId, Vector2 position, Vector2 direction, float duration = 1.0f)
        {
            var telegraph = new AttackTelegraphEffect();
            telegraph.Initialize(WeaponType.Sword, position, direction, duration, _attackTelegraphShader);
            
            // Customize for enemy attack (red color)
            var material = telegraph.Material as ShaderMaterial;
            if (material != null)
            {
                material.SetShaderParameter("telegraph_color", new Vector4(1.0f, 0.2f, 0.2f, 0.6f)); // Red for enemy
                material.SetShaderParameter("pulse_speed", 3.0f); // Faster pulse for urgency
            }
            
            AddChild(telegraph);
            
            // Play enemy attack sound
            if (_audioManager != null)
            {
                _audioManager.PlayEnemySound("attack", position);
            }
            
            // Auto-remove after duration
            GetTree().CreateTimer(duration).Timeout += () => {
                telegraph.QueueFree();
            };
            
            GD.Print($"Showing enemy {enemyId} attack telegraph with audio at ({position.X:F1}, {position.Y:F1})");
        }
        
        public override void _Process(double delta)
        {
            // Clean up any orphaned effects
            CleanupOrphanedEffects();
        }
        
        private void CleanupOrphanedEffects()
        {
            // Remove any effects that have been freed
            var telegraphsToRemove = new List<uint>();
            foreach (var kvp in _activeTelegraphs)
            {
                if (!IsInstanceValid(kvp.Value))
                {
                    telegraphsToRemove.Add(kvp.Key);
                }
            }
            foreach (var id in telegraphsToRemove)
            {
                _activeTelegraphs.Remove(id);
            }
            
            var hitEffectsToRemove = new List<uint>();
            foreach (var kvp in _activeHitEffects)
            {
                if (!IsInstanceValid(kvp.Value))
                {
                    hitEffectsToRemove.Add(kvp.Key);
                }
            }
            foreach (var id in hitEffectsToRemove)
            {
                _activeHitEffects.Remove(id);
            }
            
            _itemBobEffects.RemoveAll(effect => !IsInstanceValid(effect));
        }
    }
}