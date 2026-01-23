using Guildmaster.Client.Core.Components;
using Guildmaster.Client.Core.ECS;
using Guildmaster.Client.Input;
using Guildmaster.Client.Network;
using System.Linq;
using System.Numerics;

namespace Guildmaster.Client.Core.Systems;

public class InputSystem : ISystem
{
    private readonly GameWorld _world;
    private readonly IInputService _input;
    private readonly NetworkSystem _network;
    
    // Throttle input to avoid flooding network
    private float _lastMoveSendTime;
    private const float MoveSendInterval = 0.1f; 
    private uint _inputSequence = 0;

    public InputSystem(GameWorld world, IInputService input, NetworkSystem network)
    {
        _world = world;
        _input = input;
        _network = network;
    }

    public void Update(float deltaTime)
    {
        HandleMovement(deltaTime);
        HandleActions();
    }

    public void Draw() { }

    private void HandleMovement(float deltaTime)
    {
        // Find Local Player
        var localPlayerEntity = _world.GetEntities().FirstOrDefault(e => 
            e.GetComponent<PlayerComponent>()?.IsLocalPlayer == true);
            
        if (localPlayerEntity == null) return;
        
        var playerComp = localPlayerEntity.GetComponent<PlayerComponent>();
        var posComp = localPlayerEntity.GetComponent<PositionComponent>();
        if (playerComp == null || posComp == null) return;

        if (_input.IsActionDown(CharacterAction.MoveUp) || 
            _input.IsActionDown(CharacterAction.MoveDown) || 
            _input.IsActionDown(CharacterAction.MoveLeft) || 
            _input.IsActionDown(CharacterAction.MoveRight))
        {
            var vec = _input.GetMovementVector();
            
            // Client-Side Prediction (Basic): Update local pos immediately?
            // For MVP strict mode, maybe wait for server? 
            // Better experience: Update locally.
            // posComp.Position += vec * 100 * deltaTime; // Example speed

            if (Raylib_cs.Raylib.GetTime() - _lastMoveSendTime > MoveSendInterval) 
            {
                 var conn = _network.GetConnection();
                 if (conn != null)
                 {
                     // Calculate target X/Y or Velocity. 
                     // Reducer expects (PlayerId, NewX, NewY, VelX, VelY, Seq)
                     // Let's assume we are sending the *target* position relative to current?
                     // Or updated position.
                     
                     float speed = 5.0f; // Arbitrary speed matching server?
                     float newX = posComp.Position.X + vec.X * speed;
                     float newY = posComp.Position.Y + vec.Y * speed;
                     
                     conn.Reducers.UpdatePlayerPosition(
                        playerComp.PlayerId,
                        newX,
                        newY,
                        vec.X, // Velocity X
                        vec.Y, // Velocity Y
                        _inputSequence++
                     );
                     _lastMoveSendTime = (float)Raylib_cs.Raylib.GetTime();
                 }
            }
        }
    }

    private void HandleActions()
    {
        if (_input.IsActionPressed(CharacterAction.Attack))
        {
             // conn.Reducers.ExecuteAttack(...)
             // Placeholder
             Console.WriteLine("Input: Attack");
        }
    }
}
