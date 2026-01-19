using Godot;

/// <summary>
/// Minimal player movement script that works independently
/// </summary>
public partial class MinimalPlayer : CharacterBody2D
{
    [Export] public float Speed = 200.0f;
    
    public override void _Ready()
    {
        GD.Print("MinimalPlayer: Ready!");
    }
    
    public override void _PhysicsProcess(double delta)
    {
        Vector2 direction = Vector2.Zero;
        
        // Simple WASD input
        if (Input.IsKeyPressed(Key.W))
            direction.Y -= 1.0f;
        if (Input.IsKeyPressed(Key.S))
            direction.Y += 1.0f;
        if (Input.IsKeyPressed(Key.A))
            direction.X -= 1.0f;
        if (Input.IsKeyPressed(Key.D))
            direction.X += 1.0f;
        
        // Apply movement
        Velocity = direction.Normalized() * Speed;
        MoveAndSlide();
        
        // Debug
        if (direction != Vector2.Zero)
        {
            GD.Print($"Moving: {direction} -> Position: {Position}");
        }
    }
}