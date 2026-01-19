using Godot;
using System.Collections.Generic;
using GuildmasterMVP.Data;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Manages projectile creation, movement, and collision detection
    /// Implements Requirements 4.1, 4.2, 4.3, 4.4
    /// </summary>
    public partial class ProjectileManager : Node
    {
        private Dictionary<uint, ProjectileData> _activeProjectiles = new Dictionary<uint, ProjectileData>();
        private Dictionary<ProjectileType, ProjectileConfig> _projectileConfigs = new Dictionary<ProjectileType, ProjectileConfig>();
        private SpacetimeDBClient _dbClient;
        private InventorySystem _inventorySystem;
        private uint _nextProjectileId = 1;
        
        public override void _Ready()
        {
            // Get reference to SpacetimeDB client
            _dbClient = GameManager.Instance?.DbClient;
            if (_dbClient == null)
            {
                GD.PrintErr("ProjectileManager: Could not find SpacetimeDB client");
            }
            
            // Get reference to InventorySystem
            _inventorySystem = GetNode<InventorySystem>("../InventorySystem");
            if (_inventorySystem == null)
            {
                GD.PrintErr("ProjectileManager: Could not find InventorySystem");
            }
            
            InitializeProjectileConfigs();
            GD.Print("ProjectileManager initialized");
        }
        
        /// <summary>
        /// Initialize projectile configurations
        /// </summary>
        private void InitializeProjectileConfigs()
        {
            _projectileConfigs[ProjectileType.Arrow] = new ProjectileConfig
            {
                Type = ProjectileType.Arrow,
                Speed = 400.0f,
                Damage = 20.0f,
                MaxRange = 300.0f,
                TimeToLive = 5.0f,
                RequiredAmmo = AmmoType.Arrow
            };
        }
        
        /// <summary>
        /// Create a new projectile
        /// Requirements 4.1: Create projectile that travels in aimed direction
        /// Requirements 4.2: Consume ammunition from inventory
        /// </summary>
        public bool CreateProjectile(uint playerId, Vector2 origin, Vector2 direction, ProjectileType type)
        {
            if (!_projectileConfigs.ContainsKey(type))
            {
                GD.PrintErr($"Unknown projectile type: {type}");
                return false;
            }
            
            var config = _projectileConfigs[type];
            
            // Check ammunition in inventory system (double-check, should already be done by CombatSystem)
            if (_inventorySystem != null)
            {
                if (_inventorySystem.GetAmmoCount(playerId, config.RequiredAmmo) <= 0)
                {
                    GD.Print($"Player {playerId} has no {config.RequiredAmmo} ammunition");
                    return false;
                }
            }
            
            var projectileId = _nextProjectileId++;
            var projectile = new ProjectileData
            {
                Id = projectileId,
                Position = origin,
                Velocity = direction.Normalized() * config.Speed,
                Damage = config.Damage,
                OwnerId = playerId,
                TimeToLive = config.TimeToLive,
                Type = type,
                IsActive = true
            };
            
            _activeProjectiles[projectileId] = projectile;
            
            GD.Print($"Created {type} projectile {projectileId} for player {playerId} at ({origin.X:F1}, {origin.Y:F1})");
            
            // Send projectile creation to server
            if (_dbClient != null && _dbClient.IsConnected)
            {
                _ = _dbClient.CreateProjectileAsync(playerId, origin.X, origin.Y, direction.X, direction.Y);
            }
            
            return true;
        }
        
        /// <summary>
        /// Update all active projectiles
        /// Requirements 4.3: Projectile collision with enemies
        /// Requirements 4.4: Projectile collision with obstacles
        /// </summary>
        public override void _Process(double delta)
        {
            UpdateProjectiles((float)delta);
        }
        
        /// <summary>
        /// Update projectile positions and handle collisions
        /// </summary>
        private void UpdateProjectiles(float deltaTime)
        {
            var projectilesToRemove = new List<uint>();
            
            foreach (var kvp in _activeProjectiles)
            {
                var projectileId = kvp.Key;
                var projectile = kvp.Value;
                
                if (!projectile.IsActive)
                {
                    projectilesToRemove.Add(projectileId);
                    continue;
                }
                
                // Update position
                projectile.Position += projectile.Velocity * deltaTime;
                projectile.TimeToLive -= deltaTime;
                
                // Check if projectile should be removed due to timeout
                if (projectile.TimeToLive <= 0)
                {
                    projectile.IsActive = false;
                    projectilesToRemove.Add(projectileId);
                    GD.Print($"Projectile {projectileId} expired due to timeout");
                    continue;
                }
                
                // Check if projectile has traveled beyond max range
                var config = _projectileConfigs[projectile.Type];
                var distanceTraveled = projectile.Position.DistanceTo(GetProjectileOrigin(projectile));
                if (distanceTraveled > config.MaxRange)
                {
                    projectile.IsActive = false;
                    projectilesToRemove.Add(projectileId);
                    GD.Print($"Projectile {projectileId} expired due to max range");
                    continue;
                }
                
                // TODO: Check collision with enemies and obstacles
                // This would normally be handled server-side by SpacetimeDB
                // For client-side prediction, we can do basic collision checks
                if (CheckProjectileCollisions(projectile))
                {
                    projectile.IsActive = false;
                    projectilesToRemove.Add(projectileId);
                    continue;
                }
                
                // Update projectile in dictionary
                _activeProjectiles[projectileId] = projectile;
            }
            
            // Remove inactive projectiles
            foreach (var projectileId in projectilesToRemove)
            {
                _activeProjectiles.Remove(projectileId);
            }
        }
        
        /// <summary>
        /// Check projectile collisions with enemies and obstacles
        /// Requirements 4.3: Projectile collision with enemies deals damage
        /// Requirements 4.4: Projectile collision with obstacles removes projectile
        /// </summary>
        private bool CheckProjectileCollisions(ProjectileData projectile)
        {
            // TODO: Implement collision detection
            // This would involve:
            // 1. Check collision with enemies (deal damage, remove projectile)
            // 2. Check collision with obstacles (remove projectile)
            // 3. Check collision with map boundaries (remove projectile)
            
            // For now, return false (no collision)
            return false;
        }
        
        /// <summary>
        /// Get the origin position of a projectile (for range calculation)
        /// </summary>
        private Vector2 GetProjectileOrigin(ProjectileData projectile)
        {
            // TODO: Store original position or calculate from velocity and time
            // For now, assume projectile started at (0,0)
            return Vector2.Zero;
        }
        
        /// <summary>
        /// Get all active projectiles (for rendering/debugging)
        /// </summary>
        public Dictionary<uint, ProjectileData> GetActiveProjectiles()
        {
            return new Dictionary<uint, ProjectileData>(_activeProjectiles);
        }
        
        /// <summary>
        /// Remove a specific projectile (called when hit is confirmed by server)
        /// </summary>
        public void RemoveProjectile(uint projectileId)
        {
            if (_activeProjectiles.ContainsKey(projectileId))
            {
                var projectile = _activeProjectiles[projectileId];
                projectile.IsActive = false;
                _activeProjectiles[projectileId] = projectile;
                GD.Print($"Projectile {projectileId} removed by server confirmation");
            }
        }
        
        /// <summary>
        /// Get projectile configuration for a specific type
        /// </summary>
        public ProjectileConfig GetProjectileConfig(ProjectileType type)
        {
            return _projectileConfigs.ContainsKey(type) ? _projectileConfigs[type] : default;
        }
    }
}