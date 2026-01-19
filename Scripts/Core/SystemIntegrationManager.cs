using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using GuildmasterMVP.Data;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Manages integration between all game systems
    /// Ensures proper communication between client and server components
    /// Requirements: All - Complete system integration
    /// </summary>
    public partial class SystemIntegrationManager : Node
    {
        private GameManager _gameManager;
        private SpacetimeDBClient _dbClient;
        
        // System references
        private IInputManager _inputManager;
        private IMovementSystem _movementSystem;
        private ICombatSystem _combatSystem;
        private IInventorySystem _inventorySystem;
        private IInteractionManager _interactionManager;
        private IHealthSystem _healthSystem;
        private IEnemyAI _enemyAI;
        private IMapSystem _mapSystem;
        
        // Integration state
        private bool _isInitialized = false;
        private Dictionary<uint, PlayerController> _playerControllers = new Dictionary<uint, PlayerController>();
        
        public bool IsInitialized => _isInitialized;
        
        public override void _Ready()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager == null)
            {
                GD.PrintErr("SystemIntegrationManager: GameManager not found");
                return;
            }
            
            // Wait for GameManager to initialize
            CallDeferred(nameof(InitializeIntegration));
        }
        
        private async void InitializeIntegration()
        {
            // Wait for GameManager initialization
            while (!_gameManager.IsInitialized)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            
            // Get system references
            _dbClient = _gameManager.DbClient;
            _inputManager = _gameManager.InputManager;
            _movementSystem = _gameManager.MovementSystem;
            _combatSystem = _gameManager.CombatSystem;
            _inventorySystem = _gameManager.InventorySystem;
            _interactionManager = _gameManager.InteractionManager;
            _healthSystem = _gameManager.HealthSystem;
            _enemyAI = _gameManager.EnemyAI;
            _mapSystem = _gameManager.MapSystem;
            
            // Set up system integrations
            SetupSystemIntegrations();
            
            // Set up server event handlers
            SetupServerEventHandlers();
            
            _isInitialized = true;
            GD.Print("SystemIntegrationManager initialized successfully");
        }
        
        /// <summary>
        /// Set up integrations between different systems
        /// </summary>
        private void SetupSystemIntegrations()
        {
            // Combat System Integration
            if (_combatSystem is CombatSystem combatSystemImpl)
            {
                // Connect combat events to health system
                combatSystemImpl.PlayerDamaged += OnPlayerDamaged;
                combatSystemImpl.EnemyDamaged += OnEnemyDamaged;
                combatSystemImpl.ProjectileCreated += OnProjectileCreated;
                combatSystemImpl.AttackExecuted += OnAttackExecuted;
            }
            
            // Health System Integration
            if (_healthSystem is HealthSystem healthSystemImpl)
            {
                healthSystemImpl.PlayerDowned += OnPlayerDowned;
                healthSystemImpl.PlayerRevived += OnPlayerRevived;
                healthSystemImpl.PlayerHealed += OnPlayerHealed;
            }
            
            // Inventory System Integration
            if (_inventorySystem is InventorySystem inventorySystemImpl)
            {
                inventorySystemImpl.ItemAdded += OnItemAdded;
                inventorySystemImpl.ItemRemoved += OnItemRemoved;
                inventorySystemImpl.WeaponEquipped += OnWeaponEquipped;
                inventorySystemImpl.ToolEquipped += OnToolEquipped;
            }
            
            // Interaction System Integration
            if (_interactionManager != null)
            {
                // Interaction manager already integrates with inventory system
                GD.Print("Interaction system integration ready");
            }
            
            // Enemy AI Integration
            if (_enemyAI is EnemyAI enemyAIImpl)
            {
                enemyAIImpl.EnemyStateChanged += OnEnemyStateChanged;
                enemyAIImpl.EnemyAttackedPlayer += OnEnemyAttackedPlayer;
            }
            
            // Movement System Integration
            if (_movementSystem is MovementSystem movementSystemImpl)
            {
                movementSystemImpl.PlayerPositionUpdated += OnPlayerPositionUpdated;
                movementSystemImpl.PositionCorrectionNeeded += OnPositionCorrectionNeeded;
            }
            
            GD.Print("System integrations configured");
        }
        
        /// <summary>
        /// Set up server event handlers for SpacetimeDB communication
        /// </summary>
        private void SetupServerEventHandlers()
        {
            if (_dbClient == null)
            {
                GD.PrintErr("SpacetimeDB client not available for server integration");
                return;
            }
            
            // Connect to server events
            _dbClient.Connected += OnServerConnected;
            _dbClient.Disconnected += OnServerDisconnected;
            _dbClient.PlayerUpdated += OnServerPlayerUpdated;
            _dbClient.PlayerPositionCorrected += OnServerPositionCorrected;
            
            GD.Print("Server event handlers configured");
        }
        
        #region Combat System Event Handlers
        
        private async void OnPlayerDamaged(uint playerId, float damage, uint attackerId)
        {
            GD.Print($"Player {playerId} took {damage} damage from {attackerId}");
            
            // Apply damage through health system
            if (_healthSystem != null)
            {
                _healthSystem.ApplyDamage(playerId, damage);
            }
            
            // Send to server
            if (_dbClient != null)
            {
                await _dbClient.ApplyDamageToPlayerAsync(playerId, damage, attackerId);
            }
        }
        
        private async void OnEnemyDamaged(uint enemyId, float damage, uint attackerId)
        {
            GD.Print($"Enemy {enemyId} took {damage} damage from player {attackerId}");
            
            // Server handles enemy health, just log locally
            // Enemy health is managed server-side through combat reducers
        }
        
        private async void OnProjectileCreated(uint playerId, Vector2 origin, Vector2 direction, string projectileType)
        {
            GD.Print($"Player {playerId} created {projectileType} projectile");
            
            // Send projectile creation to server
            if (_dbClient != null)
            {
                await _dbClient.CreateProjectileAsync(playerId, origin.X, origin.Y, direction.X, direction.Y);
            }
        }
        
        private async void OnAttackExecuted(uint playerId, WeaponType weaponType, Vector2 direction)
        {
            GD.Print($"Player {playerId} executed {weaponType} attack");
            
            // Send attack to server for validation and processing
            if (_dbClient != null)
            {
                await _dbClient.ExecuteAttackAsync(playerId, weaponType.ToString(), direction.X, direction.Y);
            }
        }
        
        #endregion
        
        #region Health System Event Handlers
        
        private async void OnPlayerDowned(uint playerId)
        {
            GD.Print($"Player {playerId} was downed");
            
            // Update player controller state
            if (_playerControllers.ContainsKey(playerId))
            {
                // TODO: Add downed state to PlayerController
                GD.Print($"Updated player {playerId} controller to downed state");
            }
        }
        
        private async void OnPlayerRevived(uint playerId, uint reviverId)
        {
            GD.Print($"Player {playerId} was revived by player {reviverId}");
            
            // Send revival to server
            if (_dbClient != null)
            {
                await _dbClient.RevivePlayerAsync(playerId, reviverId);
            }
        }
        
        private async void OnPlayerHealed(uint playerId, float healAmount)
        {
            GD.Print($"Player {playerId} healed for {healAmount}");
            
            // Send healing to server
            if (_dbClient != null)
            {
                await _dbClient.HealPlayerAsync(playerId, healAmount);
            }
        }
        
        #endregion
        
        #region Inventory System Event Handlers
        
        private void OnItemAdded(uint playerId, string itemId, int quantity)
        {
            GD.Print($"Player {playerId} gained {quantity} {itemId}");
            
            // Update UI or other systems that depend on inventory changes
            UpdatePlayerInventoryUI(playerId);
        }
        
        private void OnItemRemoved(uint playerId, string itemId, int quantity)
        {
            GD.Print($"Player {playerId} lost {quantity} {itemId}");
            
            // Update UI or other systems that depend on inventory changes
            UpdatePlayerInventoryUI(playerId);
        }
        
        private void OnWeaponEquipped(uint playerId, string weaponId)
        {
            GD.Print($"Player {playerId} equipped weapon {weaponId}");
            
            // Update combat system with new weapon
            if (_combatSystem != null)
            {
                // Combat system will query inventory system for equipped weapon when needed
            }
            
            UpdatePlayerInventoryUI(playerId);
        }
        
        private void OnToolEquipped(uint playerId, string toolId)
        {
            GD.Print($"Player {playerId} equipped tool {toolId}");
            
            // Update interaction system capabilities
            UpdatePlayerInventoryUI(playerId);
        }
        
        #endregion
        
        #region Enemy AI Event Handlers
        
        private async void OnEnemyStateChanged(uint enemyId, string newState, uint targetPlayerId)
        {
            uint? nullableTargetPlayerId = targetPlayerId == 0 ? null : targetPlayerId;
            GD.Print($"Enemy {enemyId} changed state to {newState}, target: {nullableTargetPlayerId}");
            
            // Send AI state update to server
            if (_dbClient != null && _enemyAI != null)
            {
                var enemyPosition = _enemyAI.GetEnemyPosition(enemyId);
                var enemyVelocity = _enemyAI.GetEnemyVelocity(enemyId);
                var lastKnownPlayerPos = _enemyAI.GetLastKnownPlayerPosition(enemyId);
                
                await _dbClient.UpdateEnemyAIAsync(enemyId, newState, enemyPosition, enemyVelocity, 
                    nullableTargetPlayerId, lastKnownPlayerPos);
            }
        }
        
        private async void OnEnemyAttackedPlayer(uint enemyId, uint playerId, float damage)
        {
            GD.Print($"Enemy {enemyId} attacked player {playerId} for {damage} damage");
            
            // Apply damage through health system
            if (_healthSystem != null)
            {
                _healthSystem.ApplyDamage(playerId, damage);
            }
            
            // Send attack to server
            if (_dbClient != null)
            {
                await _dbClient.EnemyAttackPlayerAsync(enemyId, playerId, damage);
            }
        }
        
        #endregion
        
        #region Movement System Event Handlers
        
        private async void OnPlayerPositionUpdated(uint playerId, Vector2 position, Vector2 velocity, uint sequence)
        {
            // Send position update to server
            if (_dbClient != null)
            {
                await _dbClient.UpdatePlayerPositionAsync(playerId, position, velocity, sequence);
            }
        }
        
        private void OnPositionCorrectionNeeded(uint playerId, Vector2 serverPosition, uint sequence)
        {
            GD.Print($"Position correction needed for player {playerId}: {serverPosition}");
            
            // Update player controller position
            if (_playerControllers.ContainsKey(playerId))
            {
                _playerControllers[playerId].UpdateFromServer(serverPosition, Vector2.Zero, sequence);
            }
        }
        
        #endregion
        
        #region Server Event Handlers
        
        private void OnServerConnected(string identity)
        {
            GD.Print($"SystemIntegrationManager: Connected to server (identity: {identity})");
            
            // Initialize server-side objects and state
            InitializeServerState();
        }
        
        private void OnServerDisconnected(string reason)
        {
            GD.Print($"SystemIntegrationManager: Disconnected from server: {reason}");
            
            // Handle disconnection cleanup
        }
        
        private void OnServerPlayerUpdated(uint playerId, Vector2 position, float health)
        {
            // Update local player state from server
            if (_playerControllers.ContainsKey(playerId))
            {
                _playerControllers[playerId].UpdateFromServer(position, Vector2.Zero, 0);
            }
            
            // Update health system
            if (_healthSystem != null)
            {
                _healthSystem.SetPlayerHealth(playerId, health);
            }
        }
        
        private void OnServerPositionCorrected(uint playerId, Vector2 serverPosition, uint lastSequence)
        {
            // Apply server position correction
            if (_movementSystem != null)
            {
                _movementSystem.ApplyServerCorrection(playerId, serverPosition, lastSequence);
            }
        }
        
        #endregion
        
        #region Public Integration Methods
        
        /// <summary>
        /// Register a player controller with the integration manager
        /// </summary>
        public void RegisterPlayerController(uint playerId, PlayerController controller)
        {
            _playerControllers[playerId] = controller;
            GD.Print($"Registered player controller for player {playerId}");
        }
        
        /// <summary>
        /// Unregister a player controller
        /// </summary>
        public void UnregisterPlayerController(uint playerId)
        {
            _playerControllers.Remove(playerId);
            GD.Print($"Unregistered player controller for player {playerId}");
        }
        
        /// <summary>
        /// Initialize server-side state for testing
        /// </summary>
        private async void InitializeServerState()
        {
            if (_dbClient == null) return;
            
            // Create some test interactable objects on the server
            await _dbClient.CallReducerAsync("create_interactable_object", "tree", 200.0f, 200.0f, "default_map");
            await _dbClient.CallReducerAsync("create_interactable_object", "rock", 600.0f, 200.0f, "default_map");
            
            // Spawn a test enemy
            await _dbClient.SpawnTestEnemyAsync(500.0f, 400.0f, "default_map");
            
            GD.Print("Server state initialized with test objects");
        }
        
        /// <summary>
        /// Update player inventory UI (placeholder for UI system integration)
        /// </summary>
        private void UpdatePlayerInventoryUI(uint playerId)
        {
            // This would integrate with a UI system to update inventory displays
            // For now, just log the update
            GD.Print($"Inventory UI update needed for player {playerId}");
        }
        
        /// <summary>
        /// Execute contextual action with full system integration
        /// </summary>
        public async Task<InteractionResult> ExecuteContextualActionIntegrated(
            uint playerId, uint objectId, ActionType actionType)
        {
            if (_interactionManager == null)
            {
                return new InteractionResult(false, "Interaction manager not available");
            }
            
            // Execute action locally
            var result = _interactionManager.ExecuteContextualAction(playerId, objectId, actionType);
            
            // Send to server for validation and synchronization
            if (_dbClient != null && result.Success)
            {
                await _dbClient.CallReducerAsync("execute_contextual_action", 
                    playerId, objectId, actionType.ToString().ToLower());
            }
            
            return result;
        }
        
        /// <summary>
        /// Get comprehensive player state for debugging
        /// </summary>
        public PlayerState GetPlayerState(uint playerId)
        {
            var state = new PlayerState
            {
                PlayerId = playerId,
                Position = _movementSystem?.GetPlayerPosition(playerId) ?? Vector2.Zero,
                Velocity = _movementSystem?.GetPlayerVelocity(playerId) ?? Vector2.Zero,
                Health = _healthSystem?.GetPlayerHealth(playerId) ?? 100.0f,
                MaxHealth = _healthSystem?.GetPlayerMaxHealth(playerId) ?? 100.0f,
                EquippedWeapon = _inventorySystem?.GetEquippedWeapon(playerId) ?? "",
                EquippedTool = _inventorySystem?.GetEquippedTool(playerId) ?? "",
                IsDowned = _healthSystem?.IsPlayerDowned(playerId) ?? false
            };
            
            return state;
        }
        
        #endregion
        
        public override void _ExitTree()
        {
            // Clean up event handlers
            if (_combatSystem is CombatSystem combatSystemImpl)
            {
                combatSystemImpl.PlayerDamaged -= OnPlayerDamaged;
                combatSystemImpl.EnemyDamaged -= OnEnemyDamaged;
                combatSystemImpl.ProjectileCreated -= OnProjectileCreated;
                combatSystemImpl.AttackExecuted -= OnAttackExecuted;
            }
            
            if (_healthSystem is HealthSystem healthSystemImpl)
            {
                healthSystemImpl.PlayerDowned -= OnPlayerDowned;
                healthSystemImpl.PlayerRevived -= OnPlayerRevived;
                healthSystemImpl.PlayerHealed -= OnPlayerHealed;
            }
            
            if (_inventorySystem is InventorySystem inventorySystemImpl)
            {
                inventorySystemImpl.ItemAdded -= OnItemAdded;
                inventorySystemImpl.ItemRemoved -= OnItemRemoved;
                inventorySystemImpl.WeaponEquipped -= OnWeaponEquipped;
                inventorySystemImpl.ToolEquipped -= OnToolEquipped;
            }
            
            if (_enemyAI is EnemyAI enemyAIImpl)
            {
                enemyAIImpl.EnemyStateChanged -= OnEnemyStateChanged;
                enemyAIImpl.EnemyAttackedPlayer -= OnEnemyAttackedPlayer;
            }
            
            if (_movementSystem is MovementSystem movementSystemImpl)
            {
                movementSystemImpl.PlayerPositionUpdated -= OnPlayerPositionUpdated;
                movementSystemImpl.PositionCorrectionNeeded -= OnPositionCorrectionNeeded;
            }
            
            if (_dbClient != null)
            {
                _dbClient.Connected -= OnServerConnected;
                _dbClient.Disconnected -= OnServerDisconnected;
                _dbClient.PlayerUpdated -= OnServerPlayerUpdated;
                _dbClient.PlayerPositionCorrected -= OnServerPositionCorrected;
            }
        }
    }
    
    /// <summary>
    /// Comprehensive player state for debugging and integration
    /// </summary>
    public struct PlayerState
    {
        public uint PlayerId;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Health;
        public float MaxHealth;
        public string EquippedWeapon;
        public string EquippedTool;
        public bool IsDowned;
    }
}