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

    // Container interaction state
    public uint? OpenContainerId { get; private set; }
    public string? OpenContainerLabel { get; private set; }
    private const float INTERACT_RANGE = 16.0f;

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

        // Sync Sequence if needed (e.g. freshly loaded player)
        if (_inputSequence == 0 && playerComp.LastInputSequence > 0)
        {
             _inputSequence = playerComp.LastInputSequence + 1;
             Console.WriteLine($"[Input] Synced sequence to {_inputSequence}");
        }

        if (_input.IsActionDown(CharacterAction.MoveUp) || 
            _input.IsActionDown(CharacterAction.MoveDown) || 
            _input.IsActionDown(CharacterAction.MoveLeft) || 
            _input.IsActionDown(CharacterAction.MoveRight))
        {
            var vec = _input.GetMovementVector();
            
            // Client-Side Prediction (Basic): Update local pos immediately?
            // For MVP strict mode, maybe wait for server? 
            // Better experience: Update locally.
            posComp.Position += vec * 100 * deltaTime; // Example speed

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
             Console.WriteLine("Input: Attack");
        }

        if (_input.IsActionPressed(CharacterAction.Interact))
        {
            if (OpenContainerId.HasValue)
            {
                // Close container
                OpenContainerId = null;
                OpenContainerLabel = null;
                Console.WriteLine("[Input] Container closed");
            }
            else
            {
                TryOpenNearbyContainer();
            }
        }

        // Container UI input (number keys to deposit/withdraw)
        if (OpenContainerId.HasValue)
        {
            HandleContainerInput();
        }
    }

    private void TryOpenNearbyContainer()
    {
        var localPlayerEntity = _world.GetEntities().FirstOrDefault(e =>
            e.GetComponent<PlayerComponent>()?.IsLocalPlayer == true);
        if (localPlayerEntity == null) return;

        var playerPos = localPlayerEntity.GetComponent<PositionComponent>();
        if (playerPos == null) return;

        // Find nearest container in range
        float bestDist = float.MaxValue;
        Entity? bestContainer = null;

        foreach (var entity in _world.GetEntities())
        {
            var container = entity.GetComponent<ContainerComponent>();
            var pos = entity.GetComponent<PositionComponent>();
            if (container == null || pos == null) continue;

            float dx = playerPos.Position.X - pos.Position.X;
            float dy = playerPos.Position.Y - pos.Position.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist < INTERACT_RANGE && dist < bestDist)
            {
                bestDist = dist;
                bestContainer = entity;
            }
        }

        if (bestContainer != null)
        {
            var cc = bestContainer.GetComponent<ContainerComponent>()!;
            OpenContainerId = cc.ContainerId;
            OpenContainerLabel = cc.Label;
            Console.WriteLine($"[Input] Opened container '{cc.Label}' (ID: {cc.ContainerId})");
        }
    }

    private void HandleContainerInput()
    {
        var conn = _network.GetConnection();
        if (conn == null || !OpenContainerId.HasValue) return;

        // D key: Deposit first inventory item
        if (Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.One))
        {
            var localPlayer = _world.GetEntities().FirstOrDefault(e =>
                e.GetComponent<PlayerComponent>()?.IsLocalPlayer == true);
            if (localPlayer == null) return;

            var playerId = localPlayer.GetComponent<PlayerComponent>()!.PlayerId;

            // Find first item in player inventory
            var firstItem = conn.Db.InventoryItem.Iter()
                .FirstOrDefault(i => i.PlayerId == playerId && i.Quantity > 0);

            if (firstItem != null)
            {
                conn.Reducers.DepositItem(OpenContainerId.Value, firstItem.ItemId, 1);
                Console.WriteLine($"[Input] Depositing 1x {firstItem.ItemId} into container");
            }
            else
            {
                Console.WriteLine("[Input] No items in inventory to deposit");
            }
        }

        // W key: Withdraw first container item
        if (Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Two))
        {
            var firstContainerItem = conn.Db.ContainerItem.Iter()
                .FirstOrDefault(ci => ci.ContainerId == OpenContainerId.Value && ci.Quantity > 0);

            if (firstContainerItem != null)
            {
                conn.Reducers.WithdrawItem(OpenContainerId.Value, firstContainerItem.ItemId, 1);
                Console.WriteLine($"[Input] Withdrawing 1x {firstContainerItem.ItemId} from container");
            }
            else
            {
                Console.WriteLine("[Input] Container is empty");
            }
        }
    }

    public void CloseContainer()
    {
        OpenContainerId = null;
        OpenContainerLabel = null;
    }
}
