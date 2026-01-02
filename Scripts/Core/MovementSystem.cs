using Godot;
using System.Collections.Generic;
using GuildmasterMVP.Data;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Client-side movement system with input prediction and server reconciliation
    /// Implements Requirements 1.2, 1.4, 1.6
    /// </summary>
    public partial class MovementSystem : Node, IMovementSystem
    {
        [Signal]
        public delegate void PlayerPositionUpdatedEventHandler(uint playerId, Vector2 position, Vector2 velocity, uint sequence);
        
        [Signal]
        public delegate void PositionCorrectionNeededEventHandler(uint playerId, Vector2 serverPosition, uint sequence);
        
        private const float MOVEMENT_SPEED = 200.0f; // pixels per second
        private const float PREDICTION_BUFFER_SIZE = 60; // Store 1 second of predictions at 60fps
        private const float RECONCILIATION_THRESHOLD = 5.0f; // pixels
        
        private Dictionary<uint, PlayerMovementState> _playerStates = new Dictionary<uint, PlayerMovementState>();
        private Queue<PredictionState> _predictionBuffer = new Queue<PredictionState>();
        private uint _currentSequence = 0;
        private SpacetimeDBClient _dbClient;
        
        public override void _Ready()
        {
            // Get reference to SpacetimeDB client
            _dbClient = GameManager.Instance?.DbClient;
            if (_dbClient == null)
            {
                GD.PrintErr("MovementSystem: Could not find SpacetimeDB client");
            }
            
            GD.Print("MovementSystem initialized");
        }
        
        /// <summary>
        /// Process movement input with client-side prediction
        /// Requirements 1.2: Move player smoothly in corresponding direction
        /// Requirements 1.4: Stop player smoothly when input released
        /// Requirements 1.6: Implement client-side prediction
        /// </summary>
        public void ProcessMovementInput(uint playerId, Vector2 direction, float deltaTime, uint sequence)
        {
            // Ensure player state exists
            if (!_playerStates.ContainsKey(playerId))
            {
                _playerStates[playerId] = new PlayerMovementState
                {
                    PlayerId = playerId,
                    Position = Vector2.Zero,
                    Velocity = Vector2.Zero,
                    LastSequence = 0
                };
            }
            
            var playerState = _playerStates[playerId];
            
            // Calculate new velocity based on input direction
            Vector2 newVelocity = direction * MOVEMENT_SPEED;
            
            // Predict new position
            Vector2 predictedPosition = PredictPosition(playerId, direction, deltaTime);
            
            // Update local player state immediately for responsiveness
            playerState.Position = predictedPosition;
            playerState.Velocity = newVelocity;
            playerState.LastSequence = sequence;
            
            // Store prediction for later reconciliation
            var prediction = new PredictionState
            {
                Sequence = sequence,
                Position = predictedPosition,
                Velocity = newVelocity,
                Direction = direction,
                DeltaTime = deltaTime,
                Timestamp = Time.GetUnixTimeFromSystem()
            };
            
            _predictionBuffer.Enqueue(prediction);
            
            // Limit buffer size
            while (_predictionBuffer.Count > PREDICTION_BUFFER_SIZE)
            {
                _predictionBuffer.Dequeue();
            }
            
            // Send movement to server for validation
            if (_dbClient != null && _dbClient.IsConnected)
            {
                _ = _dbClient.UpdatePlayerPositionAsync(playerId, predictedPosition, newVelocity, sequence);
            }
            
            // Emit position updated signal
            EmitSignal(SignalName.PlayerPositionUpdated, playerId, predictedPosition, newVelocity, sequence);
        }
        
        /// <summary>
        /// Apply server correction when client prediction differs from server state
        /// Requirements 1.7: Smoothly correct client position to match server state
        /// </summary>
        public void ApplyServerCorrection(uint playerId, Vector2 serverPosition, uint lastSequence)
        {
            if (!_playerStates.ContainsKey(playerId))
            {
                return;
            }
            
            var playerState = _playerStates[playerId];
            Vector2 clientPosition = playerState.Position;
            
            // Calculate position difference
            float distance = clientPosition.DistanceTo(serverPosition);
            
            // Only apply correction if difference is significant
            if (distance > RECONCILIATION_THRESHOLD)
            {
                GD.Print($"Applying server correction for player {playerId}: client=({clientPosition.X:F1}, {clientPosition.Y:F1}), server=({serverPosition.X:F1}, {serverPosition.Y:F1}), distance={distance:F1}");
                
                // Emit position correction needed signal
                EmitSignal(SignalName.PositionCorrectionNeeded, playerId, serverPosition, lastSequence);
                
                // Apply server position as authoritative
                playerState.Position = serverPosition;
                
                // Re-apply any predictions that came after the server's last acknowledged sequence
                ReapplyPredictions(playerId, lastSequence);
            }
            
            // Update last acknowledged sequence
            playerState.LastAcknowledgedSequence = lastSequence;
        }
        
        /// <summary>
        /// Predict player position based on input direction and delta time
        /// Requirements 1.6: Client-side prediction for responsive movement
        /// Requirements 7.4: Disable body blocking between players
        /// </summary>
        public Vector2 PredictPosition(uint playerId, Vector2 direction, float deltaTime)
        {
            if (!_playerStates.ContainsKey(playerId))
            {
                return Vector2.Zero;
            }
            
            var playerState = _playerStates[playerId];
            Vector2 currentPosition = playerState.Position;
            
            // Calculate movement based on direction and speed
            Vector2 velocity = direction * MOVEMENT_SPEED;
            Vector2 movement = velocity * deltaTime;
            
            // Apply movement to current position
            Vector2 newPosition = currentPosition + movement;
            
            // TODO: Add collision detection here when map system is implemented
            // Note: Player-to-player collision is disabled for cooperative gameplay (Requirement 7.4)
            // Players can move through each other without body blocking
            
            return newPosition;
        }
        
        /// <summary>
        /// Get current player position (for rendering/display)
        /// </summary>
        public Vector2 GetPlayerPosition(uint playerId)
        {
            if (_playerStates.ContainsKey(playerId))
            {
                return _playerStates[playerId].Position;
            }
            return Vector2.Zero;
        }
        
        /// <summary>
        /// Get current player velocity (for animation/effects)
        /// </summary>
        public Vector2 GetPlayerVelocity(uint playerId)
        {
            if (_playerStates.ContainsKey(playerId))
            {
                return _playerStates[playerId].Velocity;
            }
            return Vector2.Zero;
        }
        
        /// <summary>
        /// Get next input sequence number
        /// </summary>
        public uint GetNextSequence()
        {
            return ++_currentSequence;
        }
        
        /// <summary>
        /// Update player state from server (for other players)
        /// </summary>
        public void UpdatePlayerFromServer(uint playerId, Vector2 position, Vector2 velocity, uint sequence)
        {
            if (!_playerStates.ContainsKey(playerId))
            {
                _playerStates[playerId] = new PlayerMovementState
                {
                    PlayerId = playerId,
                    Position = position,
                    Velocity = velocity,
                    LastSequence = sequence
                };
            }
            else
            {
                var playerState = _playerStates[playerId];
                playerState.Position = position;
                playerState.Velocity = velocity;
                playerState.LastSequence = sequence;
            }
        }
        
        /// <summary>
        /// Re-apply predictions that occurred after the server's acknowledged sequence
        /// </summary>
        private void ReapplyPredictions(uint playerId, uint lastAcknowledgedSequence)
        {
            if (!_playerStates.ContainsKey(playerId))
            {
                return;
            }
            
            var playerState = _playerStates[playerId];
            
            // Find predictions that need to be re-applied
            var predictionsToReapply = new List<PredictionState>();
            
            foreach (var prediction in _predictionBuffer)
            {
                if (prediction.Sequence > lastAcknowledgedSequence)
                {
                    predictionsToReapply.Add(prediction);
                }
            }
            
            // Re-apply predictions in order
            foreach (var prediction in predictionsToReapply)
            {
                Vector2 movement = prediction.Direction * MOVEMENT_SPEED * prediction.DeltaTime;
                playerState.Position += movement;
            }
        }
        
        /// <summary>
        /// Clean up old predictions to prevent memory leaks
        /// </summary>
        public void CleanupOldPredictions(double maxAge = 2.0)
        {
            double currentTime = Time.GetUnixTimeFromSystem();
            
            while (_predictionBuffer.Count > 0)
            {
                var oldest = _predictionBuffer.Peek();
                if (currentTime - oldest.Timestamp > maxAge)
                {
                    _predictionBuffer.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }
        
        public override void _Process(double delta)
        {
            // Clean up old predictions periodically
            CleanupOldPredictions();
        }
    }
    
    /// <summary>
    /// Internal state tracking for player movement
    /// </summary>
    public class PlayerMovementState
    {
        public uint PlayerId;
        public Vector2 Position;
        public Vector2 Velocity;
        public uint LastSequence;
        public uint LastAcknowledgedSequence;
    }
    
    /// <summary>
    /// Prediction state for client-side prediction and reconciliation
    /// </summary>
    public struct PredictionState
    {
        public uint Sequence;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Direction;
        public float DeltaTime;
        public double Timestamp;
    }
}