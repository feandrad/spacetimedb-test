using Godot;
using System.Collections.Generic;
using System.Linq;
using GuildmasterMVP.Data;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Core
{
    public partial class MapSystem : Node, IMapSystem
    {
        private Dictionary<string, MapData> _mapRegistry;
        private Dictionary<string, Node> _loadedMapInstances;
        private Dictionary<uint, Vector2> _otherPlayerPositions;
        private string _currentMapId;
        private SpacetimeDBClient _networkClient;
        private uint _playerId;
        
        [Signal]
        public delegate void PlayerEnteredMapEventHandler(uint playerId, string mapId);
        
        [Signal]
        public delegate void PlayerLeftMapEventHandler(uint playerId, string mapId);
        
        [Signal]
        public delegate void MapTransitionStartedEventHandler(string fromMapId, string toMapId);
        
        [Signal]
        public delegate void MapTransitionCompletedEventHandler(string mapId, Vector2 spawnPosition);
        
        public MapSystem(SpacetimeDBClient networkClient, uint playerId)
        {
            _mapRegistry = new Dictionary<string, MapData>();
            _loadedMapInstances = new Dictionary<string, Node>();
            _otherPlayerPositions = new Dictionary<uint, Vector2>();
            _networkClient = networkClient;
            _playerId = playerId;
            InitializeMapRegistry();
            ConnectNetworkSignals();
        }
        
        private void ConnectNetworkSignals()
        {
            if (_networkClient != null)
            {
                _networkClient.PlayerJoined += OnPlayerJoined;
                _networkClient.PlayerLeft += OnPlayerLeft;
                _networkClient.PlayerUpdated += OnPlayerUpdated;
            }
        }
        
        private void OnPlayerJoined(uint playerId)
        {
            if (playerId != _playerId)
            {
                GD.Print($"Player {playerId} joined the game");
                // Request sync to get their current map
                _ = _networkClient?.CallReducerAsync("sync_map_state", _playerId);
            }
        }
        
        private void OnPlayerLeft(uint playerId)
        {
            if (playerId != _playerId)
            {
                _otherPlayerPositions.Remove(playerId);
                EmitSignal(SignalName.PlayerLeftMap, playerId, _currentMapId);
                GD.Print($"Player {playerId} left the game");
            }
        }
        
        private void OnPlayerUpdated(uint playerId, Vector2 position, float health)
        {
            if (playerId != _playerId)
            {
                _otherPlayerPositions[playerId] = position;
                // Note: In a real implementation, we'd also check if the player is in the same map
                // For now, we assume position updates only come for players in the same map
            }
        }
        
        private void InitializeMapRegistry()
        {
            // Initialize with basic test maps
            var startingMap = new MapData
            {
                MapId = "starting_area",
                Size = new Vector2(1000, 1000),
                Transitions = new List<TransitionZone>
                {
                    new TransitionZone
                    {
                        Area = new Rect2(950, 400, 50, 200),
                        DestinationMapId = "forest_area",
                        DestinationPoint = new Vector2(50, 500)
                    }
                },
                SpawnPoints = new List<Vector2>
                {
                    new Vector2(100, 500),
                    new Vector2(150, 500),
                    new Vector2(200, 500),
                    new Vector2(250, 500)
                },
                Objects = new List<InteractableObjectData>()
            };
            
            var forestMap = new MapData
            {
                MapId = "forest_area",
                Size = new Vector2(1200, 800),
                Transitions = new List<TransitionZone>
                {
                    new TransitionZone
                    {
                        Area = new Rect2(0, 400, 50, 200),
                        DestinationMapId = "starting_area",
                        DestinationPoint = new Vector2(900, 500)
                    }
                },
                SpawnPoints = new List<Vector2>
                {
                    new Vector2(100, 400),
                    new Vector2(150, 400),
                    new Vector2(200, 400),
                    new Vector2(250, 400)
                },
                Objects = new List<InteractableObjectData>()
            };
            
            _mapRegistry["starting_area"] = startingMap;
            _mapRegistry["forest_area"] = forestMap;
        }
        
        public void TransitionToMap(uint playerId, string mapId, Vector2 entryPoint)
        {
            if (!_mapRegistry.ContainsKey(mapId))
            {
                GD.PrintErr($"Map {mapId} not found in registry");
                return;
            }
            
            string previousMapId = _currentMapId;
            
            // Emit transition started signal
            EmitSignal(SignalName.MapTransitionStarted, previousMapId ?? "", mapId);
            
            // Send transition request to server
            if (_networkClient != null)
            {
                _ = _networkClient.CallReducerAsync("transition_to_map", playerId, mapId, entryPoint.X, entryPoint.Y);
            }
            
            // Load the destination map if not already loaded
            LoadMapInstance(mapId);
            
            // Update current map
            _currentMapId = mapId;
            
            // Clear other player positions (they'll be updated via network)
            _otherPlayerPositions.Clear();
            
            // Emit transition completed signal
            EmitSignal(SignalName.MapTransitionCompleted, mapId, entryPoint);
            
            GD.Print($"Player {playerId} transitioning to map {mapId} at {entryPoint}");
        }
        
        public bool IsInTransitionZone(Vector2 position, out string destinationMapId)
        {
            destinationMapId = "";
            
            if (string.IsNullOrEmpty(_currentMapId) || !_mapRegistry.ContainsKey(_currentMapId))
                return false;
                
            var currentMap = _mapRegistry[_currentMapId];
            
            foreach (var transition in currentMap.Transitions)
            {
                if (transition.Area.HasPoint(position))
                {
                    destinationMapId = transition.DestinationMapId;
                    return true;
                }
            }
            
            return false;
        }
        
        public void LoadMapInstance(string mapId)
        {
            if (_loadedMapInstances.ContainsKey(mapId))
            {
                GD.Print($"Map {mapId} already loaded");
                return;
            }
            
            if (!_mapRegistry.ContainsKey(mapId))
            {
                GD.PrintErr($"Cannot load map {mapId} - not found in registry");
                return;
            }
            
            // Create a basic map instance node
            var mapInstance = new Node2D();
            mapInstance.Name = $"Map_{mapId}";
            
            // Add map boundaries and collision shapes based on map data
            var mapData = _mapRegistry[mapId];
            
            // Create collision boundaries
            var staticBody = new StaticBody2D();
            staticBody.Name = "MapBoundaries";
            
            // Add collision shapes for map boundaries
            var collisionShape = new CollisionShape2D();
            var rectangleShape = new RectangleShape2D();
            rectangleShape.Size = mapData.Size;
            collisionShape.Shape = rectangleShape;
            collisionShape.Position = mapData.Size / 2;
            
            staticBody.AddChild(collisionShape);
            mapInstance.AddChild(staticBody);
            
            // Add transition zone visual indicators (for debugging)
            foreach (var transition in mapData.Transitions)
            {
                var transitionArea = new Area2D();
                transitionArea.Name = $"Transition_to_{transition.DestinationMapId}";
                
                var transitionCollision = new CollisionShape2D();
                var transitionShape = new RectangleShape2D();
                transitionShape.Size = transition.Area.Size;
                transitionCollision.Shape = transitionShape;
                transitionCollision.Position = transition.Area.Position + transition.Area.Size / 2;
                
                transitionArea.AddChild(transitionCollision);
                mapInstance.AddChild(transitionArea);
            }
            
            _loadedMapInstances[mapId] = mapInstance;
            GD.Print($"Loaded map instance: {mapId}");
        }
        
        public void UnloadMapInstance(string mapId)
        {
            if (_loadedMapInstances.ContainsKey(mapId))
            {
                var mapInstance = _loadedMapInstances[mapId];
                mapInstance.QueueFree();
                _loadedMapInstances.Remove(mapId);
                GD.Print($"Unloaded map instance: {mapId}");
            }
        }
        
        public MapData? GetMapData(string mapId)
        {
            return _mapRegistry.ContainsKey(mapId) ? _mapRegistry[mapId] : null;
        }
        
        public Vector2 GetSpawnPoint(string mapId, int playerIndex = 0)
        {
            if (!_mapRegistry.ContainsKey(mapId))
                return Vector2.Zero;
                
            var mapData = _mapRegistry[mapId];
            if (mapData.SpawnPoints.Count == 0)
                return Vector2.Zero;
                
            // Return spawn point based on player index, cycling if needed
            int spawnIndex = playerIndex % mapData.SpawnPoints.Count;
            return mapData.SpawnPoints[spawnIndex];
        }
        
        public string GetCurrentMapId()
        {
            return _currentMapId;
        }
        
        public Vector2 GetTransitionDestination(string mapId, Vector2 position)
        {
            if (!_mapRegistry.ContainsKey(_currentMapId))
                return Vector2.Zero;
                
            var currentMap = _mapRegistry[_currentMapId];
            
            foreach (var transition in currentMap.Transitions)
            {
                if (transition.Area.HasPoint(position) && transition.DestinationMapId == mapId)
                {
                    return transition.DestinationPoint;
                }
            }
            
            return Vector2.Zero;
        }
        
        public void CheckForMapTransition(Vector2 playerPosition)
        {
            if (IsInTransitionZone(playerPosition, out string destinationMapId))
            {
                var destinationPoint = GetTransitionDestination(destinationMapId, playerPosition);
                if (destinationPoint != Vector2.Zero)
                {
                    TransitionToMap(_playerId, destinationMapId, destinationPoint);
                }
            }
        }
        
        public void SpawnPlayerAtMap(string mapId)
        {
            if (!_mapRegistry.ContainsKey(mapId))
            {
                GD.PrintErr($"Cannot spawn at map {mapId} - not found in registry");
                return;
            }
            
            // Send spawn request to server
            if (_networkClient != null)
            {
                _ = _networkClient.CallReducerAsync("spawn_player_at_map", _playerId, mapId);
            }
            
            // Load the map locally
            LoadMapInstance(mapId);
            _currentMapId = mapId;
            
            // Clear other player positions
            _otherPlayerPositions.Clear();
            
            var spawnPoint = GetSpawnPoint(mapId, (int)_playerId);
            EmitSignal(SignalName.MapTransitionCompleted, mapId, spawnPoint);
            
            GD.Print($"Player {_playerId} spawning at map {mapId}");
        }
        
        public Dictionary<uint, Vector2> GetOtherPlayerPositions()
        {
            return new Dictionary<uint, Vector2>(_otherPlayerPositions);
        }
        
        public void SyncMapState()
        {
            if (_networkClient != null)
            {
                _ = _networkClient.CallReducerAsync("sync_map_state", _playerId);
            }
        }
        
        public void RequestPlayersInMap(string mapId)
        {
            if (_networkClient != null)
            {
                _ = _networkClient.CallReducerAsync("get_players_in_map", mapId);
            }
        }
        
        public bool ArePlayersInSameMap(uint playerId1, uint playerId2)
        {
            // This would need to be implemented with server data
            // For now, assume players are in the same map if we have their position
            return _otherPlayerPositions.ContainsKey(playerId1) && _otherPlayerPositions.ContainsKey(playerId2);
        }
    }
}