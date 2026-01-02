using Godot;
using System.Collections.Generic;

namespace GuildmasterMVP.Visual
{
    /// <summary>
    /// Manages simple animations for UI and gameplay elements
    /// Requirements 10.7: Simple animations primarily implemented through shaders
    /// </summary>
    public partial class AnimationManager : Node
    {
        private Dictionary<Node, Tween> _activeTweens = new Dictionary<Node, Tween>();
        
        public override void _Ready()
        {
            GD.Print("AnimationManager initialized for simple animations");
        }
        
        /// <summary>
        /// Animate node scale with bounce effect
        /// </summary>
        public void AnimateScale(Node node, Vector2 targetScale, float duration = 0.3f, bool bounce = true)
        {
            if (node == null) return;
            
            StopAnimation(node);
            
            var tween = CreateTween();
            _activeTweens[node] = tween;
            
            if (bounce)
            {
                tween.SetEase(Tween.EaseType.Out);
                tween.SetTrans(Tween.TransitionType.Back);
            }
            else
            {
                tween.SetEase(Tween.EaseType.Out);
                tween.SetTrans(Tween.TransitionType.Cubic);
            }
            
            tween.TweenProperty(node, "scale", targetScale, duration);
            tween.TweenCallback(Callable.From(() => CleanupTween(node)));
        }
        
        /// <summary>
        /// Animate node position with smooth movement
        /// </summary>
        public void AnimatePosition(Node node, Vector2 targetPosition, float duration = 0.5f)
        {
            if (node == null) return;
            
            StopAnimation(node);
            
            var tween = CreateTween();
            _activeTweens[node] = tween;
            
            tween.SetEase(Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Cubic);
            
            tween.TweenProperty(node, "position", targetPosition, duration);
            tween.TweenCallback(Callable.From(() => CleanupTween(node)));
        }
        
        /// <summary>
        /// Animate node fade in/out
        /// </summary>
        public void AnimateFade(Node node, float targetAlpha, float duration = 0.3f)
        {
            if (node == null) return;
            
            StopAnimation(node);
            
            var tween = CreateTween();
            _activeTweens[node] = tween;
            
            tween.SetEase(Tween.EaseType.InOut);
            tween.SetTrans(Tween.TransitionType.Sine);
            
            tween.TweenProperty(node, "modulate:a", targetAlpha, duration);
            tween.TweenCallback(Callable.From(() => CleanupTween(node)));
        }
        
        /// <summary>
        /// Animate UI button press effect
        /// </summary>
        public void AnimateButtonPress(Control button)
        {
            if (button == null) return;
            
            StopAnimation(button);
            
            var tween = CreateTween();
            _activeTweens[button] = tween;
            
            var originalScale = button.Scale;
            
            tween.SetEase(Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Back);
            
            // Scale down then back up
            tween.TweenProperty(button, "scale", originalScale * 0.95f, 0.1f);
            tween.TweenProperty(button, "scale", originalScale * 1.05f, 0.1f);
            tween.TweenProperty(button, "scale", originalScale, 0.1f);
            tween.TweenCallback(Callable.From(() => CleanupTween(button)));
        }
        
        /// <summary>
        /// Animate health bar change
        /// </summary>
        public void AnimateHealthBar(ProgressBar healthBar, float targetValue, float duration = 0.4f)
        {
            if (healthBar == null) return;
            
            StopAnimation(healthBar);
            
            var tween = CreateTween();
            _activeTweens[healthBar] = tween;
            
            tween.SetEase(Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Cubic);
            
            tween.TweenProperty(healthBar, "value", targetValue, duration);
            tween.TweenCallback(Callable.From(() => CleanupTween(healthBar)));
        }
        
        /// <summary>
        /// Animate damage number popup
        /// </summary>
        public void AnimateDamageNumber(Label damageLabel, float damage, Vector2 startPosition)
        {
            if (damageLabel == null) return;
            
            // Set initial properties
            damageLabel.Position = startPosition;
            damageLabel.Text = damage.ToString("F0");
            damageLabel.Modulate = Colors.White;
            damageLabel.Scale = Vector2.Zero;
            
            // Color based on damage amount
            Color damageColor = damage > 30.0f ? Colors.Red : 
                               damage > 15.0f ? Colors.Orange : Colors.Yellow;
            damageLabel.Modulate = damageColor;
            
            var tween = CreateTween();
            tween.SetParallel(true);
            
            // Scale up
            tween.TweenProperty(damageLabel, "scale", Vector2.One * 1.2f, 0.2f);
            tween.TweenProperty(damageLabel, "scale", Vector2.One, 0.1f).SetDelay(0.2f);
            
            // Move up
            tween.TweenProperty(damageLabel, "position:y", startPosition.Y - 50, 0.8f);
            
            // Fade out
            tween.TweenProperty(damageLabel, "modulate:a", 0.0f, 0.3f).SetDelay(0.5f);
            
            // Remove after animation
            tween.TweenCallback(Callable.From(() => damageLabel.QueueFree())).SetDelay(0.8f);
        }
        
        /// <summary>
        /// Animate item collection effect
        /// </summary>
        public void AnimateItemCollection(Node2D item, Vector2 targetPosition, System.Action onComplete = null)
        {
            if (item == null) return;
            
            StopAnimation(item);
            
            var tween = CreateTween();
            _activeTweens[item] = tween;
            tween.SetParallel(true);
            
            // Move to target
            tween.TweenProperty(item, "position", targetPosition, 0.5f);
            
            // Scale down
            tween.TweenProperty(item, "scale", Vector2.Zero, 0.3f).SetDelay(0.2f);
            
            // Fade out
            tween.TweenProperty(item, "modulate:a", 0.0f, 0.3f).SetDelay(0.2f);
            
            tween.TweenCallback(Callable.From(() => {
                onComplete?.Invoke();
                CleanupTween(item);
            })).SetDelay(0.5f);
        }
        
        /// <summary>
        /// Animate screen shake effect
        /// </summary>
        public void AnimateScreenShake(Camera2D camera, float intensity = 5.0f, float duration = 0.3f)
        {
            if (camera == null) return;
            
            StopAnimation(camera);
            
            var originalOffset = camera.Offset;
            var tween = CreateTween();
            _activeTweens[camera] = tween;
            
            // Create shake effect with decreasing intensity
            int shakeCount = (int)(duration * 30); // 30 shakes per second
            for (int i = 0; i < shakeCount; i++)
            {
                float progress = (float)i / shakeCount;
                float currentIntensity = intensity * (1.0f - progress);
                
                Vector2 shakeOffset = new Vector2(
                    (float)GD.RandRange(-currentIntensity, currentIntensity),
                    (float)GD.RandRange(-currentIntensity, currentIntensity)
                );
                
                tween.TweenProperty(camera, "offset", originalOffset + shakeOffset, duration / shakeCount);
            }
            
            // Return to original position
            tween.TweenProperty(camera, "offset", originalOffset, 0.1f);
            tween.TweenCallback(Callable.From(() => CleanupTween(camera)));
        }
        
        /// <summary>
        /// Stop animation for a specific node
        /// </summary>
        public void StopAnimation(Node node)
        {
            if (_activeTweens.ContainsKey(node))
            {
                _activeTweens[node].Kill();
                _activeTweens.Remove(node);
            }
        }
        
        /// <summary>
        /// Stop all active animations
        /// </summary>
        public void StopAllAnimations()
        {
            foreach (var tween in _activeTweens.Values)
            {
                tween.Kill();
            }
            _activeTweens.Clear();
        }
        
        /// <summary>
        /// Clean up completed tween
        /// </summary>
        private void CleanupTween(Node node)
        {
            if (_activeTweens.ContainsKey(node))
            {
                _activeTweens.Remove(node);
            }
        }
        
        public override void _ExitTree()
        {
            StopAllAnimations();
        }
    }
}