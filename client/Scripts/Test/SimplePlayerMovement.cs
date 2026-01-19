using Godot;

namespace GuildmasterMVP.Test
{
	/// <summary>
	/// Simple player movement for testing - doesn't depend on complex systems
	/// </summary>
	public partial class SimplePlayerMovement : CharacterBody2D
	{
		[Export] public float Speed = 200.0f;
		
		private ColorRect _playerSprite;
		private Label _playerLabel;
		
		public override void _Ready()
		{
			GD.Print("SimplePlayerMovement: _Ready called");
			
			// Try to get existing visual components from scene, but create them if they don't exist
			_playerSprite = GetNodeOrNull<ColorRect>("PlayerSprite");
			_playerLabel = GetNodeOrNull<Label>("PlayerLabel");
			
			if (_playerSprite == null)
			{
				GD.Print("PlayerSprite not found, creating new one");
				CreatePlayerSprite();
			}
			
			if (_playerLabel == null)
			{
				GD.Print("PlayerLabel not found, creating new one");
				CreatePlayerLabel();
			}
			
			// Create collision shape only if it doesn't exist
			if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") == null)
			{
				GD.Print("CollisionShape2D not found, creating new one");
				CreateCollisionShape();
			}
			
			GD.Print("SimplePlayerMovement: Setup complete");
		}
		
		private void CreatePlayerSprite()
		{
			_playerSprite = new ColorRect();
			_playerSprite.Name = "PlayerSprite";
			_playerSprite.Size = new Vector2(30, 30);
			_playerSprite.Position = new Vector2(-15, -15);
			_playerSprite.Color = Colors.Blue;
			AddChild(_playerSprite);
		}
		
		private void CreatePlayerLabel()
		{
			_playerLabel = new Label();
			_playerLabel.Name = "PlayerLabel";
			_playerLabel.Position = new Vector2(-25, -35);
			_playerLabel.Text = "Player";
			AddChild(_playerLabel);
		}
		
		private void CreateCollisionShape()
		{
			// Create collision shape
			var collisionShape = new CollisionShape2D();
			collisionShape.Name = "CollisionShape2D";
			var shape = new RectangleShape2D();
			shape.Size = new Vector2(30, 30);
			collisionShape.Shape = shape;
			AddChild(collisionShape);
			
			GD.Print("SimplePlayerMovement: Collision shape created");
		}
		
		public override void _PhysicsProcess(double delta)
		{
			try
			{
				// Get input
				Vector2 direction = Vector2.Zero;
				
				if (Input.IsKeyPressed(Key.W) || Input.IsActionPressed("ui_up"))
				{
					direction.Y -= 1.0f;
					GD.Print("W pressed - moving up");
				}
				if (Input.IsKeyPressed(Key.S) || Input.IsActionPressed("ui_down"))
				{
					direction.Y += 1.0f;
					GD.Print("S pressed - moving down");
				}
				if (Input.IsKeyPressed(Key.A) || Input.IsActionPressed("ui_left"))
				{
					direction.X -= 1.0f;
					GD.Print("A pressed - moving left");
				}
				if (Input.IsKeyPressed(Key.D) || Input.IsActionPressed("ui_right"))
				{
					direction.X += 1.0f;
					GD.Print("D pressed - moving right");
				}
				
				// Normalize diagonal movement
				if (direction.Length() > 1.0f)
				{
					direction = direction.Normalized();
				}
				
				// Apply movement
				Velocity = direction * Speed;
				MoveAndSlide();
				
				// Update color based on movement
				if (_playerSprite != null)
				{
					_playerSprite.Color = Velocity.Length() > 0.1f ? Colors.LightBlue : Colors.Blue;
				}
				
				// Debug output when moving
				if (direction != Vector2.Zero)
				{
					GD.Print($"Moving: direction={direction}, velocity={Velocity}, position={Position}");
				}
			}
			catch (System.Exception ex)
			{
				GD.PrintErr($"Error in _PhysicsProcess: {ex.Message}");
			}
		}
	}
}
