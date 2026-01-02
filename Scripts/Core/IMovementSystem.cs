using Godot;

namespace GuildmasterMVP.Core
{
    public interface IMovementSystem
    {
        // Signals
        [Signal]
        delegate void PlayerPositionUpdatedEventHandler(uint playerId, Vector2 position, Vector2 velocity, uint sequence);
        
        [Signal]
        delegate void PositionCorrectionNeededEventHandler(uint playerId, Vector2 serverPosition, uint sequence);
        
        // Methods
        void ProcessMovementInput(uint playerId, Vector2 direction, float deltaTime, uint sequence);
        void ApplyServerCorrection(uint playerId, Vector2 serverPosition, uint lastSequence);
        Vector2 PredictPosition(uint playerId, Vector2 direction, float deltaTime);
        
        // Additional methods used by SystemIntegrationManager
        Vector2 GetPlayerPosition(uint playerId);
        Vector2 GetPlayerVelocity(uint playerId);
        uint GetNextSequence();
        void UpdatePlayerFromServer(uint playerId, Vector2 position, Vector2 velocity, uint sequence);
    }
}