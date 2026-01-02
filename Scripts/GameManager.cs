using Godot;
using GuildmasterMVP.Network;
using GuildmasterMVP.Core;
using System.Threading.Tasks;

namespace GuildmasterMVP
{
    /// <summary>
    /// Main game manager that coordinates all systems
    /// </summary>
    public partial class GameManager : Node
    {
        [Export] public string ServerUrl { get; set; } = "ws://localhost:3000";
        
        private SpacetimeDBClient _dbClient;
        private InputManager _inputManager;
        private MovementSystem _movementSystem;
        private CombatSystem _combatSystem;
        private EnemyAI _enemyAI;
        private HealthSystem _healthSystem;
        private MapSystem _mapSystem;
        private InventorySystem _inventorySystem;
        private InteractionManager _interactionManager;
        private SystemIntegrationManager _integrationManager;
        private uint _playerId;
        private bool _isInitialized = false;
        
        public static GameManager Instance { get; private set; }
        
        public SpacetimeDBClient DbClient => _dbClient;
        public IInputManager InputManager => _inputManager;
        public IMovementSystem MovementSystem => _movementSystem;
        public ICombatSystem CombatSystem => _combatSystem;
        public IEnemyAI EnemyAI => _enemyAI;
        public IHealthSystem HealthSystem => _healthSystem;
        public IMapSystem MapSystem => _mapSystem;
        public IInventorySystem InventorySystem => _inventorySystem;
        public IInteractionManager InteractionManager => _interactionManager;
        public SystemIntegrationManager IntegrationManager => _integrationManager;
        public uint PlayerId => _playerId;
        public bool IsInitialized => _isInitialized;
        
        public override void _Ready()
        {
            Instance = this;
            
            // Create and add SpacetimeDB client
            _dbClient = new SpacetimeDBClient();
            AddChild(_dbClient);
            
            // Create and add InputManager
            _inputManager = new InputManager();
            AddChild(_inputManager);
            
            // Create and add MovementSystem
            _movementSystem = new MovementSystem();
            AddChild(_movementSystem);
            
            // Create and add HealthSystem (before CombatSystem and EnemyAI)
            _healthSystem = new HealthSystem();
            AddChild(_healthSystem);
            
            // Create and add EnemyAI (before CombatSystem for proper initialization)
            _enemyAI = new EnemyAI();
            AddChild(_enemyAI);
            
            // Create and add CombatSystem
            _combatSystem = new CombatSystem();
            AddChild(_combatSystem);
            
            // Create and add MapSystem
            _mapSystem = new MapSystem(_dbClient, _playerId);
            AddChild(_mapSystem);
            
            // Create and add InventorySystem
            _inventorySystem = new InventorySystem();
            AddChild(_inventorySystem);
            
            // Create and add InteractionManager
            _interactionManager = new InteractionManager();
            AddChild(_interactionManager);
            
            // Create and add SystemIntegrationManager
            _integrationManager = new SystemIntegrationManager();
            AddChild(_integrationManager);
            
            // Connect to signals
            _dbClient.Connected += OnConnectedToServer;
            _dbClient.Disconnected += OnDisconnectedFromServer;
            _dbClient.PlayerUpdated += OnPlayerUpdated;
            _dbClient.PlayerPositionCorrected += OnPlayerPositionCorrected;
            
            GD.Print("GameManager initialized with all core systems");
        }
        
        /// <summary>
        /// Initialize the game and connect to server
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
            {
                return true;
            }
            
            GD.Print("Initializing game...");
            
            // Connect to SpacetimeDB server
            bool connected = await _dbClient.ConnectAsync(ServerUrl);
            if (!connected)
            {
                GD.PrintErr("Failed to connect to server");
                return false;
            }
            
            // Create player on server
            bool playerCreated = await _dbClient.CreatePlayerAsync();
            if (!playerCreated)
            {
                GD.PrintErr("Failed to create player on server");
                return false;
            }
            
            // Subscribe to relevant tables
            _dbClient.SubscribeToTable("Player");
            _dbClient.SubscribeToTable("Enemy");
            _dbClient.SubscribeToTable("CombatEvent");
            _dbClient.SubscribeToTable("InventoryItem");
            
            _isInitialized = true;
            GD.Print("Game initialized successfully");
            
            return true;
        }
        
        private void OnConnectedToServer()
        {
            GD.Print("Connected to SpacetimeDB server");
        }
        
        private void OnDisconnectedFromServer()
        {
            GD.Print("Disconnected from SpacetimeDB server");
            _isInitialized = false;
        }
        
        private void OnPlayerUpdated(uint playerId, Vector2 position, float health)
        {
            // Handle player updates from server
            GD.Print($"Player {playerId} updated: pos=({position.X}, {position.Y}), health={health}");
        }
        
        private void OnPlayerPositionCorrected(uint playerId, Vector2 serverPosition, uint lastSequence)
        {
            // Apply server position correction through movement system
            if (_movementSystem != null)
            {
                _movementSystem.ApplyServerCorrection(playerId, serverPosition, lastSequence);
                GD.Print($"Applied position correction for player {playerId}: ({serverPosition.X:F1}, {serverPosition.Y:F1})");
            }
        }
        
        public override void _ExitTree()
        {
            if (_dbClient != null)
            {
                _dbClient.Connected -= OnConnectedToServer;
                _dbClient.Disconnected -= OnDisconnectedFromServer;
                _dbClient.PlayerUpdated -= OnPlayerUpdated;
                _dbClient.PlayerPositionCorrected -= OnPlayerPositionCorrected;
            }
            
            Instance = null;
        }
    }
}