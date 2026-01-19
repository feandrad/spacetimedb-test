using Godot;
using GuildmasterMVP.Core;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Visual
{
    /// <summary>
    /// Attack telegraph effect using shader-based rendering
    /// Requirements 10.1, 10.2: Attack telegraph effects and visual indicators
    /// </summary>
    public partial class AttackTelegraphEffect : ColorRect
    {
        private ShaderMaterial _shaderMaterial;
        private Tween _progressTween;
        private WeaponType _weaponType;
        private float _duration;
        
        public void Initialize(WeaponType weaponType, Vector2 position, Vector2 direction, float duration, Shader shader)
        {
            _weaponType = weaponType;
            _duration = duration;
            
            // Set position and size based on weapon type
            Position = position;
            SetSizeAndShape(weaponType, direction);
            
            // Create shader material
            _shaderMaterial = new ShaderMaterial();
            _shaderMaterial.Shader = shader;
            Material = _shaderMaterial;
            
            // Configure shader parameters based on weapon type
            ConfigureShaderForWeapon(weaponType);
            
            // Start animation
            StartTelegraphAnimation();
        }
        
        private void SetSizeAndShape(WeaponType weaponType, Vector2 direction)
        {
            Vector2 size;
            Vector2 offset;
            
            switch (weaponType)
            {
                case WeaponType.Sword:
                    // Wide cleave area
                    size = new Vector2(120, 80);
                    offset = new Vector2(-60, -40);
                    break;
                case WeaponType.Axe:
                    // Narrow frontal area
                    size = new Vector2(80, 60);
                    offset = new Vector2(-40, -30);
                    break;
                case WeaponType.Bow:
                    // Projectile path indicator
                    size = new Vector2(200, 20);
                    offset = new Vector2(0, -10);
                    break;
                default:
                    size = new Vector2(80, 80);
                    offset = new Vector2(-40, -40);
                    break;
            }
            
            Size = size;
            Position += offset;
            
            // Rotate based on direction
            if (direction != Vector2.Zero)
            {
                Rotation = direction.Angle();
            }
        }
        
        private void ConfigureShaderForWeapon(WeaponType weaponType)
        {
            if (_shaderMaterial == null) return;
            
            switch (weaponType)
            {
                case WeaponType.Sword:
                    _shaderMaterial.SetShaderParameter("telegraph_color", new Vector4(1.0f, 0.8f, 0.2f, 0.6f)); // Golden
                    _shaderMaterial.SetShaderParameter("pulse_speed", 2.0f);
                    _shaderMaterial.SetShaderParameter("fade_edge", 0.2f);
                    break;
                case WeaponType.Axe:
                    _shaderMaterial.SetShaderParameter("telegraph_color", new Vector4(1.0f, 0.4f, 0.2f, 0.7f)); // Orange-red
                    _shaderMaterial.SetShaderParameter("pulse_speed", 1.5f);
                    _shaderMaterial.SetShaderParameter("fade_edge", 0.1f);
                    break;
                case WeaponType.Bow:
                    _shaderMaterial.SetShaderParameter("telegraph_color", new Vector4(0.2f, 0.8f, 1.0f, 0.5f)); // Blue
                    _shaderMaterial.SetShaderParameter("pulse_speed", 3.0f);
                    _shaderMaterial.SetShaderParameter("fade_edge", 0.3f);
                    break;
            }
            
            // Initialize progress to 0
            _shaderMaterial.SetShaderParameter("progress", 0.0f);
            _shaderMaterial.SetShaderParameter("intensity", 1.0f);
        }
        
        private void StartTelegraphAnimation()
        {
            _progressTween = CreateTween();
            _progressTween.SetEase(Tween.EaseType.Out);
            _progressTween.SetTrans(Tween.TransitionType.Cubic);
            
            // Animate progress from 0 to 1 over the duration
            _progressTween.TweenMethod(Callable.From<float>(UpdateProgress), 0.0f, 1.0f, _duration);
            
            // Fade out at the end
            _progressTween.TweenMethod(Callable.From<float>(UpdateIntensity), 1.0f, 0.0f, _duration * 0.2f);
        }
        
        private void UpdateProgress(float progress)
        {
            if (_shaderMaterial != null)
            {
                _shaderMaterial.SetShaderParameter("progress", progress);
            }
        }
        
        private void UpdateIntensity(float intensity)
        {
            if (_shaderMaterial != null)
            {
                _shaderMaterial.SetShaderParameter("intensity", intensity);
            }
        }
        
        public override void _ExitTree()
        {
            _progressTween?.Kill();
        }
    }
}