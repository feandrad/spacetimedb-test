using Godot;

namespace GuildmasterMVP.Visual
{
    /// <summary>
    /// Item bob animation effect using shader-based rendering
    /// Requirements 10.7: Simple visual effects and animations through shaders
    /// </summary>
    public partial class ItemBobEffect : ColorRect
    {
        private ShaderMaterial _shaderMaterial;
        private string _itemType;
        
        public string ItemType => _itemType;
        
        public void Initialize(Vector2 position, string itemType, Shader shader)
        {
            _itemType = itemType;
            
            // Set position and size
            Position = position - new Vector2(15, 15); // Center the item
            Size = new Vector2(30, 30);
            
            // Create shader material
            _shaderMaterial = new ShaderMaterial();
            _shaderMaterial.Shader = shader;
            Material = _shaderMaterial;
            
            // Configure shader parameters based on item type
            ConfigureShaderForItem(itemType);
            
            // Create simple colored rectangle as base texture
            CreateItemTexture(itemType);
        }
        
        private void ConfigureShaderForItem(string itemType)
        {
            if (_shaderMaterial == null) return;
            
            // Configure animation parameters based on item type
            switch (itemType.ToLower())
            {
                case "fruit":
                case "food":
                    _shaderMaterial.SetShaderParameter("bob_speed", 1.5f);
                    _shaderMaterial.SetShaderParameter("bob_height", 3.0f);
                    _shaderMaterial.SetShaderParameter("glow_color", new Vector4(1.0f, 0.8f, 0.2f, 0.4f)); // Golden glow
                    _shaderMaterial.SetShaderParameter("glow_intensity", 0.6f);
                    _shaderMaterial.SetShaderParameter("rotation_speed", 0.3f);
                    break;
                    
                case "wood":
                case "stone":
                    _shaderMaterial.SetShaderParameter("bob_speed", 1.0f);
                    _shaderMaterial.SetShaderParameter("bob_height", 2.0f);
                    _shaderMaterial.SetShaderParameter("glow_color", new Vector4(0.8f, 0.8f, 1.0f, 0.3f)); // Blue-ish glow
                    _shaderMaterial.SetShaderParameter("glow_intensity", 0.4f);
                    _shaderMaterial.SetShaderParameter("rotation_speed", 0.1f);
                    break;
                    
                case "arrow":
                case "ammunition":
                    _shaderMaterial.SetShaderParameter("bob_speed", 2.0f);
                    _shaderMaterial.SetShaderParameter("bob_height", 4.0f);
                    _shaderMaterial.SetShaderParameter("glow_color", new Vector4(0.2f, 1.0f, 0.8f, 0.5f)); // Cyan glow
                    _shaderMaterial.SetShaderParameter("glow_intensity", 0.7f);
                    _shaderMaterial.SetShaderParameter("rotation_speed", 0.5f);
                    break;
                    
                case "weapon":
                case "tool":
                    _shaderMaterial.SetShaderParameter("bob_speed", 0.8f);
                    _shaderMaterial.SetShaderParameter("bob_height", 5.0f);
                    _shaderMaterial.SetShaderParameter("glow_color", new Vector4(1.0f, 0.2f, 0.8f, 0.6f)); // Magenta glow
                    _shaderMaterial.SetShaderParameter("glow_intensity", 0.8f);
                    _shaderMaterial.SetShaderParameter("rotation_speed", 0.2f);
                    break;
                    
                default:
                    // Default item animation
                    _shaderMaterial.SetShaderParameter("bob_speed", 1.2f);
                    _shaderMaterial.SetShaderParameter("bob_height", 3.0f);
                    _shaderMaterial.SetShaderParameter("glow_color", new Vector4(1.0f, 1.0f, 1.0f, 0.4f)); // White glow
                    _shaderMaterial.SetShaderParameter("glow_intensity", 0.5f);
                    _shaderMaterial.SetShaderParameter("rotation_speed", 0.3f);
                    break;
            }
        }
        
        private void CreateItemTexture(string itemType)
        {
            // Create a simple colored texture based on item type
            // In a full implementation, this would load actual item sprites
            Color itemColor = GetItemColor(itemType);
            Color = itemColor;
        }
        
        private Color GetItemColor(string itemType)
        {
            return itemType.ToLower() switch
            {
                "fruit" => Colors.Red,
                "wood" => Colors.SaddleBrown,
                "stone" => Colors.Gray,
                "arrow" => Colors.Brown,
                "weapon" => Colors.Silver,
                "tool" => Colors.DarkGray,
                _ => Colors.White
            };
        }
        
        /// <summary>
        /// Animate item collection (fade out and move up)
        /// </summary>
        public void AnimateCollection(System.Action onComplete = null)
        {
            var collectTween = CreateTween();
            collectTween.SetParallel(true);
            
            // Fade out
            collectTween.TweenProperty(this, "modulate:a", 0.0f, 0.3f);
            
            // Move up
            collectTween.TweenProperty(this, "position:y", Position.Y - 20, 0.3f);
            
            // Scale up slightly
            collectTween.TweenProperty(this, "scale", Vector2.One * 1.2f, 0.15f);
            collectTween.TweenProperty(this, "scale", Vector2.Zero, 0.15f).SetDelay(0.15f);
            
            collectTween.TweenCallback(Callable.From(() => {
                onComplete?.Invoke();
                QueueFree();
            })).SetDelay(0.3f);
        }
        
        /// <summary>
        /// Update item bob effect parameters dynamically
        /// </summary>
        public void UpdateBobParameters(float speed, float height, float glowIntensity)
        {
            if (_shaderMaterial != null)
            {
                _shaderMaterial.SetShaderParameter("bob_speed", speed);
                _shaderMaterial.SetShaderParameter("bob_height", height);
                _shaderMaterial.SetShaderParameter("glow_intensity", glowIntensity);
            }
        }
    }
}