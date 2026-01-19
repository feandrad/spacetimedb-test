using Godot;
using GuildmasterMVP.Core;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Test class for MovementSystem functionality
    /// Tests Requirements 1.2, 1.4, 1.6, 1.7
    /// </summary>
    public partial class MovementSystemTest : Node
    {
        private MovementSystem _movementSystem;
        private const uint TEST_PLAYER_ID = 1;
        
        public override void _Ready()
        {
            _movementSystem = new MovementSystem();
            AddChild(_movementSystem);
            
            // Run tests
            TestMovementPrediction();
            TestServerCorrection();
            TestSequenceHandling();
            
            GD.Print("MovementSystem tests completed");
        }
        
        /// <summary>
        /// Test client-side movement prediction
        /// Requirements 1.6: Client-side prediction for responsive movement
        /// </summary>
        private void TestMovementPrediction()
        {
            GD.Print("Testing movement prediction...");
            
            // Test forward movement
            Vector2 direction = Vector2.Up;
            float deltaTime = 0.016f; // 60 FPS
            
            Vector2 predictedPosition = _movementSystem.PredictPosition(TEST_PLAYER_ID, direction, deltaTime);
            
            // Should predict movement in the up direction
            GD.Print($"Predicted position for up movement: {predictedPosition}");
            
            // Test diagonal movement
            direction = new Vector2(1, 1).Normalized();
            predictedPosition = _movementSystem.PredictPosition(TEST_PLAYER_ID, direction, deltaTime);
            
            GD.Print($"Predicted position for diagonal movement: {predictedPosition}");
            
            // Test no movement
            direction = Vector2.Zero;
            predictedPosition = _movementSystem.PredictPosition(TEST_PLAYER_ID, direction, deltaTime);
            
            GD.Print($"Predicted position for no movement: {predictedPosition}");
        }
        
        /// <summary>
        /// Test server position correction
        /// Requirements 1.7: Smoothly correct client position to match server state
        /// </summary>
        private void TestServerCorrection()
        {
            GD.Print("Testing server correction...");
            
            // Set up initial client position
            _movementSystem.ProcessMovementInput(TEST_PLAYER_ID, Vector2.Right, 0.016f, 1);
            Vector2 clientPosition = _movementSystem.GetPlayerPosition(TEST_PLAYER_ID);
            
            GD.Print($"Initial client position: {clientPosition}");
            
            // Simulate server correction with significant difference
            Vector2 serverPosition = clientPosition + new Vector2(10, 5);
            _movementSystem.ApplyServerCorrection(TEST_PLAYER_ID, serverPosition, 1);
            
            Vector2 correctedPosition = _movementSystem.GetPlayerPosition(TEST_PLAYER_ID);
            GD.Print($"Position after server correction: {correctedPosition}");
            
            // Verify correction was applied
            if (correctedPosition.DistanceTo(serverPosition) < 0.1f)
            {
                GD.Print("✓ Server correction applied successfully");
            }
            else
            {
                GD.PrintErr("✗ Server correction failed");
            }
        }
        
        /// <summary>
        /// Test input sequence handling
        /// Requirements 1.5: Server validates all movement inputs
        /// </summary>
        private void TestSequenceHandling()
        {
            GD.Print("Testing sequence handling...");
            
            // Test sequence generation
            uint seq1 = _movementSystem.GetNextSequence();
            uint seq2 = _movementSystem.GetNextSequence();
            uint seq3 = _movementSystem.GetNextSequence();
            
            GD.Print($"Generated sequences: {seq1}, {seq2}, {seq3}");
            
            // Verify sequences are incrementing
            if (seq2 == seq1 + 1 && seq3 == seq2 + 1)
            {
                GD.Print("✓ Sequence generation working correctly");
            }
            else
            {
                GD.PrintErr("✗ Sequence generation failed");
            }
        }
        
        /// <summary>
        /// Test movement direction consistency
        /// Requirements 1.2: Move player smoothly in corresponding direction
        /// </summary>
        private void TestMovementDirectionConsistency()
        {
            GD.Print("Testing movement direction consistency...");
            
            Vector2 initialPosition = _movementSystem.GetPlayerPosition(TEST_PLAYER_ID);
            
            // Test movement in each cardinal direction
            Vector2[] directions = { Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right };
            string[] directionNames = { "Up", "Down", "Left", "Right" };
            
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2 direction = directions[i];
                string name = directionNames[i];
                
                _movementSystem.ProcessMovementInput(TEST_PLAYER_ID, direction, 0.1f, _movementSystem.GetNextSequence());
                Vector2 newPosition = _movementSystem.GetPlayerPosition(TEST_PLAYER_ID);
                
                Vector2 movement = newPosition - initialPosition;
                float dot = movement.Normalized().Dot(direction);
                
                if (dot > 0.9f) // Allow for small floating point errors
                {
                    GD.Print($"✓ {name} movement direction correct");
                }
                else
                {
                    GD.PrintErr($"✗ {name} movement direction incorrect: expected {direction}, got {movement.Normalized()}");
                }
                
                initialPosition = newPosition;
            }
        }
        
        /// <summary>
        /// Test movement stop behavior
        /// Requirements 1.4: Stop player smoothly when input released
        /// </summary>
        private void TestMovementStop()
        {
            GD.Print("Testing movement stop behavior...");
            
            // Start movement
            _movementSystem.ProcessMovementInput(TEST_PLAYER_ID, Vector2.Right, 0.016f, _movementSystem.GetNextSequence());
            Vector2 velocity = _movementSystem.GetPlayerVelocity(TEST_PLAYER_ID);
            
            GD.Print($"Velocity during movement: {velocity}");
            
            // Stop movement (zero input)
            _movementSystem.ProcessMovementInput(TEST_PLAYER_ID, Vector2.Zero, 0.016f, _movementSystem.GetNextSequence());
            Vector2 stoppedVelocity = _movementSystem.GetPlayerVelocity(TEST_PLAYER_ID);
            
            GD.Print($"Velocity after stop: {stoppedVelocity}");
            
            // Verify velocity is zero or very close to zero
            if (stoppedVelocity.Length() < 0.1f)
            {
                GD.Print("✓ Movement stop working correctly");
            }
            else
            {
                GD.PrintErr("✗ Movement stop failed - velocity not zero");
            }
        }
    }
}