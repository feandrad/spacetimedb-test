using Godot;
using GuildmasterMVP.Core;

namespace GuildmasterMVP.Core
{
	/// <summary>
	/// Player controller that handles input and coordinates with movement system
	/// Implements Requirements 1.2, 1.4: Player movement with smooth start/stop
	/// </summary>
	public partial class PlayerController : CharacterBody2D
	{
		[Export] public uint PlayerId { get; set; } = 1;
		
		private IInputManager _inputManager;
		private IMovementSystem _movementSystem;
		private bool _isLocalPlayer = true; // Default to local player for testing
		private ColorRect _playerSprite;
		private Label _playerLabel;
		
		public bool IsLocalPlayer 
		{ 
			get => _isLocalPlayer; 
			set => _isLocalPlayer = value; 
		}
		
		public override void _Ready()
		{
			GD.Print($"PlayerController _Ready called for PlayerId: {PlayerId}");
			
			// Create visual representation
			CreatePlayerVisual();
			
			// Create collision shape
			CreateCollisionShape();
			
			// Get references to systems (with delay to ensure GameManager is ready)
			CallDeferred(nameof(InitializeSystems));
			
			GD.Print($"PlayerController setup complete for PlayerId: {PlayerId}");
		}
		
		private void InitializeSystems()
		{
			_inputManager = GameManager.Instance?.InputManager;
			_movementSystem = GameManager.Instance?.MovementSystem;
			
			if (_inputManager == null)
			{
				GD.PrintErr("PlayerController: Could not find InputManager");
			}
			
			if (_movementSystem == null)
			{
				GD.PrintErr("PlayerController: Could not find MovementSystem");
			}
			
			GD.Print($"PlayerController initialized for player {PlayerId} at ({Position.X:F1}, {Position.Y:F1})");
		}
		
		private void CreatePlayerVisual()
		{
			// Create player sprite (blue square)
			_playerSprite = new ColorRect();
			_playerSprite.Size = new Vector2(30, 30);
			_playerSprite.Position = new Vector2(-15, -15);
			_playerSprite.Color = Colors.Blue;
			AddChild(_playerSprite);
			
			// Create player label
			_playerLabel = new Label();
			_playerLabel.Position = new Vector2(-20, -40);
			_playerLabel.Text = $"Player {PlayerId}";
			_playerLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
			AddChild(_playerLabel);
		}
		
		private void CreateCollisionShape()
		{
			// Create collision shape for the CharacterBody2D
			var collisionShape = new CollisionShape2D();
			var shape = new RectangleShape2D();
			shape.Size = new Vector2(30, 30);
			collisionShape.Shape = shape;
			AddChild(collisionShape);
		}
		
		public override void _PhysicsProcess(double delta)
		{
			// Only process input for local player
			if (!_isLocalPlayer)
			{
				return;
			}
			
			// Get movement input directly from Godot's Input system for immediate responsiveness
			Vector2 movementDirection = Vector2.Zero;
			
			if (Input.IsActionPressed("ui_up") || Input.IsKeyPressed(Key.W))
			{
				movementDirection.Y -= 1.0f;
				GD.Print("Moving UP");
			}
			if (Input.IsActionPressed("ui_down") || Input.IsKeyPressed(Key.S))
			{
				movementDirection.Y += 1.0f;
				GD.Print("Moving DOWN");
			}
			if (Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.A))
			{
				movementDirection.X -= 1.0f;
				GD.Print("Moving LEFT");
			}
			if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D))
			{
				movementDirection.X += 1.0f;
				GD.Print("Moving RIGHT");
			}
			
			// Normalize diagonal movement
			if (movementDirection.Length() > 1.0f)
			{
				movementDirection = movementDirection.Normalized();
			}
			
			// Apply movement directly to CharacterBody2D
			const float SPEED = 200.0f;
			Velocity = movementDirection * SPEED;
			MoveAndSlide();
			
			// Debug output
			if (movementDirection != Vector2.Zero)
			{
				GD.Print($"Player {PlayerId} moving: direction={movementDirection}, velocity={Velocity}, position={Position}");
			}
			
			// Also process through movement system if available (for server sync)
			if (_inputManager != null && _movementSystem != null)
			{
				uint sequence = _movementSystem.GetNextSequence();
				_movementSystem.ProcessMovementInput(PlayerId, movementDirection, (float)delta, sequence);
			}
			
			// Update player color based on movement
			if (_playerSprite != null)
			{
				_playerSprite.Color = IsMoving() ? Colors.LightBlue : Colors.Blue;
			}
		}
		
		/// <summary>
		/// Update position from server (for non-local players or corrections)
		/// </summary>
		public void UpdateFromServer(Vector2 position, Vector2 velocity, uint sequence)
		{
			if (_movementSystem != null)
			{
				if (_isLocalPlayer)
				{
					// Apply server correction for local player
					_movementSystem.ApplyServerCorrection(PlayerId, position, sequence);
				}
				else
				{
					// Direct update for remote players
					_movementSystem.UpdatePlayerFromServer(PlayerId, position, velocity, sequence);
				}
				
				// Update visual position
				Position = _movementSystem.GetPlayerPosition(PlayerId);
			}
		}
		
		/// <summary>
		/// Set this as the local player
		/// </summary>
		public void SetAsLocalPlayer()
		{
			_isLocalPlayer = true;
			if (_playerLabel != null)
			{
				_playerLabel.Text = $"Player {PlayerId} (You)";
			}
			GD.Print($"Player {PlayerId} set as local player");
		}
		
		/// <summary>
		/// Get current movement velocity for animation purposes
		/// </summary>
		public new Vector2 GetVelocity()
		{
			if (_movementSystem != null)
			{
				return _movementSystem.GetPlayerVelocity(PlayerId);
			}
			return Vector2.Zero;
		}
		
		/// <summary>
		/// Check if player is moving (for animation states)
		/// </summary>
		public bool IsMoving()
		{
			// Use CharacterBody2D's Velocity property directly
			return Velocity.Length() > 0.1f; // Small threshold to account for floating point precision
		}
	}
}
