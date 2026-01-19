using Godot;

namespace GuildmasterMVP.Visual
{
    /// <summary>
    /// Hit effect using shader-based rendering for damage feedback
    /// Requirements 10.3, 10.4: Hit confirmation and damage feedback effects
    /// </summary>
    public partial class HitEffect : ColorRect
    {
        private ShaderMaterial _shaderMaterial;
        private Tween _effectTween;
        private float _damage;
        private float _duration;
        
        public void Initialize(Vector2 position, float damage, float duration, Shader shader)
        {
            _damage = damage;
            _duration = duration;
            
            // Set position and size
            Position = position - new Vector2(25, 25); // Center the effect
            Size = new Vector2(50, 50);
            
            // Create shader material
            _shaderMaterial = new ShaderMaterial();
            _shaderMaterial.Shader = shader;
            Material = _shaderMaterial;
            
            // Configure shader parameters based on damage
            ConfigureShaderForDamage(damage);
            
            // Start hit animation
            StartHitAnimation();
        }
        
        private void ConfigureShaderForDamage(float damage)
        {
            if (_shaderMaterial == null) return;
            
            // Scale effect intensity based on damage amount
            float intensity = Mathf.Clamp(damage / 50.0f, 0.5f, 3.0f); // Normalize damage to intensity
            
            // Color based on damage type/amount
            Vector4 hitColor;
            if (damage > 30.0f)
            {
                // High damage - bright red
                hitColor = new Vector4(1.0f, 0.1f, 0.1f, 1.0f);
            }
            else if (damage > 15.0f)
            {
                // Medium damage - orange-red
                hitColor = new Vector4(1.0f, 0.5f, 0.2f, 1.0f);
            }
            else
            {
                // Low damage - yellow-orange
                hitColor = new Vector4(1.0f, 0.8f, 0.3f, 1.0f);
            }
            
            _shaderMaterial.SetShaderParameter("hit_color", hitColor);
            _shaderMaterial.SetShaderParameter("flash_intensity", intensity);
            _shaderMaterial.SetShaderParameter("shake_amount", intensity * 2.0f);
            _shaderMaterial.SetShaderParameter("ring_thickness", 0.05f);
            
            // Initialize progress to 0
            _shaderMaterial.SetShaderParameter("hit_progress", 0.0f);
        }
        
        private void StartHitAnimation()
        {
            _effectTween = CreateTween();
            _effectTween.SetEase(Tween.EaseType.Out);
            _effectTween.SetTrans(Tween.TransitionType.Quart);
            
            // Animate hit progress from 0 to 1
            _effectTween.TweenMethod(Callable.From<float>(UpdateHitProgress), 0.0f, 1.0f, _duration);
            
            // Add screen shake effect for high damage
            if (_damage > 20.0f)
            {
                AddScreenShake();
            }
        }
        
        private void UpdateHitProgress(float progress)
        {
            if (_shaderMaterial != null)
            {
                _shaderMaterial.SetShaderParameter("hit_progress", progress);
            }
        }
        
        private void AddScreenShake()
        {
            // Create subtle screen shake effect
            var originalPosition = Position;
            var shakeTween = CreateTween();
            shakeTween.SetLoops(5);
            
            float shakeIntensity = Mathf.Clamp(_damage / 100.0f, 0.1f, 0.5f) * 3.0f;
            
            for (int i = 0; i < 5; i++)
            {
                Vector2 shakeOffset = new Vector2(
                    (float)GD.RandRange(-shakeIntensity, shakeIntensity),
                    (float)GD.RandRange(-shakeIntensity, shakeIntensity)
                );
                
                shakeTween.TweenProperty(this, "position", originalPosition + shakeOffset, 0.05f);
                shakeTween.TweenProperty(this, "position", originalPosition, 0.05f);
            }
        }
        
        public override void _ExitTree()
        {
            _effectTween?.Kill();
        }
    }
}