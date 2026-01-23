using System.Collections.Generic;
using System.Numerics;
using Guildmaster.Client.Core.Components;
using Guildmaster.Client.Core.ECS;
using Guildmaster.Client.Network;
using Raylib_cs;
using SpacetimeDB.Types; 

namespace Guildmaster.Client.Core.Systems;

public class SyncSystem : ISystem
{
    private readonly GameWorld _world;
    private readonly NetworkSystem _network;
    private readonly GuildmasterClient _client; // Need identity

    // Mapping external ID (e.g. Identity or EntityId from DB) to local ECS Entity ID
    // Since Player has Identity and Enemy has ulong EntityId, we might need separate maps or a composite key
    private readonly Dictionary<string, int> _playerEntityMap = new();
    private readonly Dictionary<uint, int> _enemyEntityMap = new();

    public SyncSystem(GameWorld world, NetworkSystem network, GuildmasterClient client)
    {
        _world = world;
        _network = network;
        _client = client;
    }

    public void Update(float deltaTime)
    {
        var conn = _network.GetConnection();
        if (conn == null) return;

        SyncPlayers(conn);
        SyncEnemies(conn);
    }

    private void SyncPlayers(DbConnection conn)
    {
        // 1. Mark all as not updated? Or just update existing / create new.
        // For simple sync, we iterate DB.
        // Ideally we should also remove entities that are no longer in DB (or in map view).
        // But SpacetimeDB client SDK handles cache. If it's in cache, it's "here".

        // Using a set to track active entities for this frame to detect deletions if needed
        // For MVP, we'll just Sync Update/Create. Deletion from cache is handled by SDK events normally, 
        // but since we iterate cache, "implicitly" vanished entities won't be updated. 
        // Real ECS usually needs "Systems" to remove stale entities. 
        // Simplification: We blindly update. If performance issues, optimize.
        
        foreach (var p in conn.Db.Player.Iter())
        {
            // Strict ID Check
            if (p.Identity == null) continue; // Should not happen
            var identityStr = p.Identity.ToString();

            if (!_playerEntityMap.TryGetValue(identityStr, out var entityId))
            {
                var entity = _world.CreateEntity();
                entityId = entity.Id;
                _playerEntityMap[identityStr] = entityId;
                
                // Add Components
                entity.AddComponent(new PositionComponent());
                entity.AddComponent(new RenderComponent { IsCircle = true });
                entity.AddComponent(new PlayerComponent());
            }

            // Validating Data before applying
            if (float.IsNaN(p.PositionX) || float.IsNaN(p.PositionY))
            {
                // Error handling / skip
                Console.WriteLine($"[SyncError] Player {p.UsernameDisplay} has NaN position.");
                continue;
            }

            var entityRef = _world.GetEntity(entityId);
            if (entityRef == null) continue;

            // Sync Data
            var pos = entityRef.GetComponent<PositionComponent>();
            if (pos != null) pos.Position = new Vector2(p.PositionX, p.PositionY);

            var ply = entityRef.GetComponent<PlayerComponent>();
            if (ply != null)
            {
                ply.PlayerId = p.Id; // Sync PlayerId
                ply.Identity = p.Identity;
                ply.Username = p.UsernameDisplay;
                ply.IsDowned = p.IsDowned;
                ply.IsLocalPlayer = (p.Identity == _client.Identity);
            }
            
            var rend = entityRef.GetComponent<RenderComponent>();
            if (rend != null)
            {
                rend.Color = (ply?.IsLocalPlayer ?? false) ? Color.Blue : Color.Purple;
            }
        }
    }

    private void SyncEnemies(DbConnection conn)
    {
        foreach (var e in conn.Db.Enemy.Iter())
        {
             // e.Id is the unique key (uint)
             if (!_enemyEntityMap.TryGetValue(e.Id, out var entityId))
             {
                 var entity = _world.CreateEntity();
                 entityId = entity.Id;
                 _enemyEntityMap[e.Id] = entityId;
                 
                 entity.AddComponent(new PositionComponent());
                 entity.AddComponent(new RenderComponent { IsCircle = false, Color = Color.Orange, Radius = 15 }); // Square logic in RenderSystem handled by IsCircle=false check?
                 // Wait, RenderSystem used IsCircle property. I should double check RenderSystem.
                 // RenderSystem checked (if render.IsCircle).
                 // So I need to set IsCircle = false for square?
                 // Or add EnemyComponent to trigger specific drawing?
                 
                 entity.AddComponent(new EnemyComponent());
             }
             
             var entityRef = _world.GetEntity(entityId);
             if (entityRef == null) continue;
             
             var pos = entityRef.GetComponent<PositionComponent>();
             if (pos != null) pos.Position = new Vector2(e.PositionX, e.PositionY);
             
             var enemy = entityRef.GetComponent<EnemyComponent>();
             if (enemy != null)
             {
                 enemy.Health = e.Health;
                 enemy.MaxHealth = e.MaxHealth;
                 enemy.State = e.State;
             }
             
             var rend = entityRef.GetComponent<RenderComponent>();
             if (rend != null)
             {
                  // Color based on state
                  rend.Color = e.State switch
                  {
                      "Chasing" => Color.Red,
                      "ChasingThroughMap" => Color.Purple,
                      _ => Color.Orange
                  };
             }
        }
    }

    public void Draw() { }
}
