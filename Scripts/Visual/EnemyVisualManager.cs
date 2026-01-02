using Godot;
using System.Collections.Generic;
using GuildmasterMVP.Core;
using GuildmasterMVP.Data;
using GuildmasterMVP.Visual;

namespace GuildmasterMVP.Visual
{
    /// <summary>
    /// Manages visual representations of enemies in the game world
    /// </summary>
    public partial class EnemyVisualManager : Node2D
    {
        private Dictionary<uint, EnemyVisual> _enemyVisuals = new Dictionary<uint, EnemyVisual>();
        private EnemyAI _enemyAI;
        private PackedScene _enemyVisualScene;
        
        public override void _Ready()
        {
            // Create enemy visual scene programmatically since we don't have a .tscn file
            _enemyVisualScene = CreateEnemyVisualScene();
            
            // Get reference to EnemyAI system
            CallDeferred(nameof(InitializeSystem));
        }
        
        private void InitializeSystem()
        {
            _enemyAI = GameManager.Instance?.EnemyAI as EnemyAI;
            if (_enemyAI == null)
            {
                GD.PrintErr("EnemyVisualManager: Could not find EnemyAI system");
                return;
            }
            
            GD.Print("EnemyVisualManager initialized");
        }
        
        private PackedScene CreateEnemyVisualScene()
        {
            // Since we can't easily create a .tscn file, we'll instantiate directly
            return null; // We'll create instances directly in SpawnEnemyVisual
        }
        
        public override void _Process(double delta)
        {
            if (_enemyAI == null)
                return;
                
            // Update all enemy visuals
            var activeEnemies = _enemyAI.GetAllActiveEnemies();
            var activeEnemyIds = new HashSet<uint>();
            
            // Update existing visuals and create new ones
            foreach (var enemyData in activeEnemies)
            {
                activeEnemyIds.Add(enemyData.Id);
                
                if (_enemyVisuals.ContainsKey(enemyData.Id))
                {
                    // Update existing visual
                    _enemyVisuals[enemyData.Id].UpdateEnemyData(enemyData);
                }
                else
                {
                    // Create new visual
                    SpawnEnemyVisual(enemyData);
                }
            }
            
            // Remove visuals for enemies that no longer exist
            var visualsToRemove = new List<uint>();
            foreach (var kvp in _enemyVisuals)
            {
                if (!activeEnemyIds.Contains(kvp.Key))
                {
                    visualsToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var enemyId in visualsToRemove)
            {
                RemoveEnemyVisual(enemyId);
            }
        }
        
        private void SpawnEnemyVisual(EnemyData enemyData)
        {
            var enemyVisual = new EnemyVisual();
            enemyVisual.Name = $"Enemy_{enemyData.Id}";
            
            AddChild(enemyVisual);
            enemyVisual.Initialize(enemyData.Id, enemyData);
            
            _enemyVisuals[enemyData.Id] = enemyVisual;
            
            GD.Print($"Created visual for enemy {enemyData.Id} at ({enemyData.Position.X:F1}, {enemyData.Position.Y:F1})");
        }
        
        private void RemoveEnemyVisual(uint enemyId)
        {
            if (_enemyVisuals.ContainsKey(enemyId))
            {
                var visual = _enemyVisuals[enemyId];
                visual.QueueFree();
                _enemyVisuals.Remove(enemyId);
                
                GD.Print($"Removed visual for enemy {enemyId}");
            }
        }
        
        public EnemyVisual GetEnemyVisual(uint enemyId)
        {
            return _enemyVisuals.ContainsKey(enemyId) ? _enemyVisuals[enemyId] : null;
        }
        
        public EnemyVisual[] GetAllEnemyVisuals()
        {
            var visuals = new EnemyVisual[_enemyVisuals.Count];
            _enemyVisuals.Values.CopyTo(visuals, 0);
            return visuals;
        }
    }
}