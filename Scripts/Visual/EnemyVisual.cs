using Godot;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Visual
{
    /// <summary>
    /// Visual representation of an enemy in the game world
    /// Shows enemy position, state, and health
    /// </summary>
    public partial class EnemyVisual : CharacterBody2D
    {
        private uint _enemyId;
        private EnemyData _enemyData;
        private Label _stateLabel;
        private ProgressBar _healthBar;
        private ColorRect _enemySprite;
        private Line2D _detectionRange;
        private Line2D _leashRange;
        
        [Export] public bool ShowRanges { get; set; } = true;
        
        public uint EnemyId => _enemyId;
        
        public override void _Ready()
        {
            // Create visual components
            CreateVisualComponents();
        }
        
        public void Initialize(uint enemyId, EnemyData enemyData)
        {
            _enemyId = enemyId;
            _enemyData = enemyData;
            
            Position = enemyData.Position;
            UpdateVisuals();
        }
        
        public void UpdateEnemyData(EnemyData enemyData)
        {
            _enemyData = enemyData;
            Position = enemyData.Position;
            UpdateVisuals();
        }
        
        private void CreateVisualComponents()
        {
            // Enemy sprite (colored rectangle)
            _enemySprite = new ColorRect();
            _enemySprite.Size = new Vector2(20, 20);
            _enemySprite.Position = new Vector2(-10, -10);
            _enemySprite.Color = Colors.Red;
            AddChild(_enemySprite);
            
            // State label
            _stateLabel = new Label();
            _stateLabel.Position = new Vector2(-15, -35);
            _stateLabel.Text = "Idle";
            _stateLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
            AddChild(_stateLabel);
            
            // Health bar
            _healthBar = new ProgressBar();
            _healthBar.Size = new Vector2(30, 6);
            _healthBar.Position = new Vector2(-15, 15);
            _healthBar.Value = 100;
            _healthBar.ShowPercentage = false;
            AddChild(_healthBar);
            
            // Detection range circle
            if (ShowRanges)
            {
                _detectionRange = new Line2D();
                _detectionRange.Width = 2.0f;
                _detectionRange.DefaultColor = Colors.Yellow;
                _detectionRange.DefaultColor = new Color(Colors.Yellow.R, Colors.Yellow.G, Colors.Yellow.B, 0.3f);
                AddChild(_detectionRange);
                
                // Leash range circle
                _leashRange = new Line2D();
                _leashRange.Width = 1.0f;
                _leashRange.DefaultColor = new Color(Colors.Orange.R, Colors.Orange.G, Colors.Orange.B, 0.2f);
                AddChild(_leashRange);
            }
        }
        
        private void UpdateVisuals()
        {
            if (_stateLabel != null)
            {
                _stateLabel.Text = _enemyData.State.ToString();
                
                // Color based on state
                Color stateColor = _enemyData.State switch
                {
                    EnemyState.Idle => Colors.Green,
                    EnemyState.Alert => Colors.Yellow,
                    EnemyState.Chasing => Colors.Red,
                    _ => Colors.White
                };
                
                _stateLabel.Modulate = stateColor;
            }
            
            if (_healthBar != null)
            {
                float healthPercent = (_enemyData.Health / _enemyData.MaxHealth) * 100.0f;
                _healthBar.Value = healthPercent;
                
                // Color health bar based on health
                Color healthColor = healthPercent > 60 ? Colors.Green :
                                  healthPercent > 30 ? Colors.Yellow : Colors.Red;
                _healthBar.Modulate = healthColor;
            }
            
            if (_enemySprite != null)
            {
                // Change enemy color based on state
                _enemySprite.Color = _enemyData.State switch
                {
                    EnemyState.Idle => Colors.DarkRed,
                    EnemyState.Alert => Colors.Orange,
                    EnemyState.Chasing => Colors.Red,
                    _ => Colors.Gray
                };
            }
            
            // Update range circles
            if (ShowRanges && _detectionRange != null && _leashRange != null)
            {
                UpdateRangeCircles();
            }
        }
        
        private void UpdateRangeCircles()
        {
            // Clear existing points
            _detectionRange.ClearPoints();
            _leashRange.ClearPoints();
            
            // Draw detection range circle
            int segments = 32;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.Tau;
                Vector2 point = new Vector2(
                    Mathf.Cos(angle) * _enemyData.DetectionRange,
                    Mathf.Sin(angle) * _enemyData.DetectionRange
                );
                _detectionRange.AddPoint(point);
            }
            
            // Draw leash range circle
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.Tau;
                Vector2 point = new Vector2(
                    Mathf.Cos(angle) * _enemyData.LeashRange,
                    Mathf.Sin(angle) * _enemyData.LeashRange
                );
                _leashRange.AddPoint(point);
            }
        }
        
        public override void _Draw()
        {
            // Draw target line if enemy has a target
            if (_enemyData.TargetPlayerId.HasValue)
            {
                Vector2 targetPos = _enemyData.LastKnownPlayerPosition - Position;
                DrawLine(Vector2.Zero, targetPos, Colors.Red, 2.0f);
                
                // Draw arrow at target position
                Vector2 arrowDir = targetPos.Normalized();
                Vector2 arrowLeft = arrowDir.Rotated(2.5f) * 10.0f;
                Vector2 arrowRight = arrowDir.Rotated(-2.5f) * 10.0f;
                
                DrawLine(targetPos, targetPos - arrowLeft, Colors.Red, 2.0f);
                DrawLine(targetPos, targetPos - arrowRight, Colors.Red, 2.0f);
            }
        }
        
        public override void _Process(double delta)
        {
            // Trigger redraw for target lines
            QueueRedraw();
        }
    }
}