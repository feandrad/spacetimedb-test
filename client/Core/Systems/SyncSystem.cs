using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Guildmaster.Client.Core.Components;
using Guildmaster.Client.Core.ECS;
using Guildmaster.Client.Network;
using Raylib_cs;
using SpacetimeDB;
using SpacetimeDB.ClientApi; // Added for RemoteTableHandle
using SpacetimeDB.Types; 

namespace Guildmaster.Client.Core.Systems;

public class SyncSystem : ISystem
{
    private readonly GameWorld _world;
    private readonly NetworkSystem _network;
    private readonly GuildmasterClient _client; 
    private readonly MapSystem _mapSystem; // Added dependency

    private readonly Dictionary<string, int> _playerEntityMap = new();
    private readonly Dictionary<uint, int> _enemyEntityMap = new();
    private readonly Dictionary<uint, int> _transitionEntityMap = new();

    public SyncSystem(GameWorld world, NetworkSystem network, GuildmasterClient client, MapSystem mapSystem)
    {
        _world = world;
        _network = network;
        _client = client;
        _mapSystem = mapSystem;
        _mapSystem.OnMapChanged += OnMapChanged;
    }

    private void OnMapChanged(string mapId)
    {
        RefreshPlayers();
        RefreshTransitions();
    }

    private void RefreshTransitions()
    {
        // Clear old transitions
        foreach (var id in _transitionEntityMap.Values)
        {
            _world.DestroyEntity(id);
        }
        _transitionEntityMap.Clear();
        
        var conn = _network.GetConnection();
        if (conn == null) return;

        // Add new transitions
        foreach (var t in conn.Db.MapTransition.Iter())
        {
            HandleTransitionInsert(null!, t);
        }
    }

    private void RefreshPlayers()
    {
        Console.WriteLine($"[Sync] Refreshing players for map: {_mapSystem.CurrentMapId}");
        
        // 1. Remove irrelevant players
        // Create a list to remove to avoid modification during iteration
        var toRemove = new List<string>();
        foreach (var kvp in _playerEntityMap)
        {
            var entity = _world.GetEntity(kvp.Value);
            if (entity == null) continue;
            
            var pComp = entity.GetComponent<PlayerComponent>();
            if (pComp == null) continue;
            
            // Check relevance manually since we don't have the Player struct here easily
            // But we have the data in components
            // Actually, querying the DB is better? 
            // We can check the DB for this identity.
            
            // Simpler: Just rely on DB state? 
            // Or assume if they are in EntityMap, they handled Insert/Update.
            // But now criteria changed (MapId changed).
            
            // We need to check if this entity is still relevant.
            // Problem: We don't store CurrentMapId in PlayerComponent (we noticed this earlier).
            // So we MUST check the DB or store it.
            // Checking DB is safer.
        }
        
        // Easier approach: Iterate ALL players in DB. 
        // If Relevant -> Ensure Entity Exists.
        // If Not Relevant -> Ensure Entity Does NOT Exist.
        
        var conn = _network.GetConnection();
        if (conn == null) return;
        
        // Track valid identities to detect removals
        var validIdentities = new HashSet<string>();

        foreach (var p in conn.Db.Player.Iter())
        {
            if (IsPlayerRelevant(p))
            {
                validIdentities.Add(p.Identity.ToString());
                HandlePlayerInsert(null!, p); // idempotent-ish (checks contains key)
            }
        }
        
        // Remove entities that are no longer relevant
        // We iterate our map, if not in validIdentities, remove.
        var allTracked = _playerEntityMap.Keys.ToList();
        foreach (var idStr in allTracked)
        {
            if (!validIdentities.Contains(idStr))
            {
                if (_playerEntityMap.TryGetValue(idStr, out var eId))
                {
                    _world.DestroyEntity(eId);
                    _playerEntityMap.Remove(idStr);
                }
            }
        }
    }

    private bool IsPlayerRelevant(Player p)
    {
        // Server now filters for us. 
        // We trust that if we receive an update, it is relevant (either same map OR it is me).
        return true; 
    }
    
    private bool _eventsRegistered = false;

    public void Update(float deltaTime)
    {
        var conn = _network.GetConnection();
        if (conn == null) return;
        
        if (!_eventsRegistered)
        {
            _eventsRegistered = true;
            RegisterPlayerEvents(conn);
            RegisterEnemyEvents(conn);
            RegisterMapEvents(conn);
            RegisterInteractableEvents(conn);
            Console.WriteLine("[Sync] Events Registered.");
            
            // Initial Sync
            foreach (var p in conn.Db.Player.Iter()) HandlePlayerInsert(null!, p);
            foreach (var e in conn.Db.Enemy.Iter()) HandleEnemyInsert(null!, e);
            foreach (var m in conn.Db.MapInstance.Iter()) HandleMapInsert(null!, m);
            foreach (var t in conn.Db.MapTransition.Iter()) HandleTransitionInsert(null!, t);
            foreach (var i in conn.Db.InteractableObject.Iter()) HandleInteractableInsert(null!, i);
        }
    }

    private void RegisterPlayerEvents(DbConnection conn)
    {
        conn.Db.Player.OnInsert += HandlePlayerInsert;
        conn.Db.Player.OnUpdate += HandlePlayerUpdate;
        conn.Db.Player.OnDelete += HandlePlayerDelete;
    }

    private void RegisterEnemyEvents(DbConnection conn)
    {
        conn.Db.Enemy.OnInsert += HandleEnemyInsert;
        conn.Db.Enemy.OnUpdate += HandleEnemyUpdate;
        conn.Db.Enemy.OnDelete += HandleEnemyDelete;
    }

    // --- Player Handlers ---

    private void HandlePlayerInsert(EventContext ctx, Player p)
    {
        // Identity is struct, never null, but good to be safe if types change
        var identityStr = p.Identity.ToString();
        
        if (!IsPlayerRelevant(p)) return;

        if (_playerEntityMap.ContainsKey(identityStr)) return; // Already exists

        Console.WriteLine($"[Sync] Player Inserted: {p.UsernameDisplay} (ID: {p.Id})");

        var entity = _world.CreateEntity();
        _playerEntityMap[identityStr] = entity.Id;
        
        // Add Components
        entity.AddComponent(new PositionComponent { Position = new Vector2(p.PositionX, p.PositionY) });
        entity.AddComponent(new RenderComponent { 
            IsCircle = true, 
            Color = (p.Identity == _client.Identity) ? Color.Blue : Color.Purple 
        });
        
        var ply = new PlayerComponent
        {
            PlayerId = p.Id,
            Identity = p.Identity,
            Username = p.UsernameDisplay,
            IsDowned = p.IsDowned,
            IsLocalPlayer = (p.Identity == _client.Identity),
            LastInputSequence = p.LastInputSequence
        };
        entity.AddComponent(ply);
    }

    private void HandlePlayerUpdate(EventContext ctx, Player oldP, Player newP)
    {
        var oldIdentityStr = oldP.Identity.ToString();
        var newIdentityStr = newP.Identity.ToString();
        
        // 1. Try to find entity by OLD identity
        if (!_playerEntityMap.TryGetValue(oldIdentityStr, out var entityId))
        {
             // If not found by old ID, maybe it's a new relevant player (moved into map)?
             if (IsPlayerRelevant(newP))
             {
                 HandlePlayerInsert(ctx, newP);
             }
             return; 
        }

        // Check if player BECAME irrelevant (moved out of map)
        if (!IsPlayerRelevant(newP))
        {
            _world.DestroyEntity(entityId);
            _playerEntityMap.Remove(oldIdentityStr);
            return;
        }

        var entity = _world.GetEntity(entityId);
        if (entity == null) return;

        // 2. If Identity Changed (Reclaim scenario), update the map
        if (oldIdentityStr != newIdentityStr)
        {
            Console.WriteLine($"[Sync] Identity changed from {oldIdentityStr} to {newIdentityStr} (Reclaim). Updating map.");
            _playerEntityMap.Remove(oldIdentityStr);
            _playerEntityMap[newIdentityStr] = entityId;
            
            // Update components that depend on Identity
            var ply = entity.GetComponent<PlayerComponent>();
            if (ply != null)
            {
                ply.Identity = newP.Identity;
                ply.IsLocalPlayer = (newP.Identity == _client.Identity);
            }
            
            var rend = entity.GetComponent<RenderComponent>();
            if (rend != null)
            {
                rend.Color = (newP.Identity == _client.Identity) ? Color.Blue : Color.Purple;
            }
        }

        // Update Components
        var pos = entity.GetComponent<PositionComponent>();
        if (pos != null) pos.Position = new Vector2(newP.PositionX, newP.PositionY);

        var pComp = entity.GetComponent<PlayerComponent>();
        if (pComp != null)
        {
            pComp.IsDowned = newP.IsDowned;
            pComp.LastInputSequence = newP.LastInputSequence;
        }
        
        if (oldP.IsDowned != newP.IsDowned)
            Console.WriteLine($"[Sync] Player {newP.UsernameDisplay} downed status changed to {newP.IsDowned}");
    }

    private void HandlePlayerDelete(EventContext ctx, Player p)
    {
        var identityStr = p.Identity.ToString();
        if (_playerEntityMap.TryGetValue(identityStr, out var entityId))
        {
            Console.WriteLine($"[Sync] Player Deleted: {p.UsernameDisplay}");
            _world.DestroyEntity(entityId);
            _playerEntityMap.Remove(identityStr);
        }
    }

    // --- Enemy Handlers ---

    private void HandleEnemyInsert(EventContext ctx, Enemy e)
    {
        if (_enemyEntityMap.ContainsKey(e.Id)) return;

        var entity = _world.CreateEntity();
        _enemyEntityMap[e.Id] = entity.Id;
        
        entity.AddComponent(new PositionComponent { Position = new Vector2(e.PositionX, e.PositionY) });
        entity.AddComponent(new RenderComponent { 
            IsCircle = false, 
            Color = GetEnemyColor(e.State), 
            Radius = 15 
        });
        entity.AddComponent(new EnemyComponent {
            Health = e.Health,
            MaxHealth = e.MaxHealth,
            State = e.State
        });
    }

    private void HandleEnemyUpdate(EventContext ctx, Enemy oldE, Enemy newE)
    {
        if (!_enemyEntityMap.TryGetValue(newE.Id, out var entityId)) return;

        var entity = _world.GetEntity(entityId);
        if (entity == null) return;

        var pos = entity.GetComponent<PositionComponent>();
        if (pos != null) pos.Position = new Vector2(newE.PositionX, newE.PositionY);

        var enemy = entity.GetComponent<EnemyComponent>();
        if (enemy != null)
        {
            enemy.Health = newE.Health;
            enemy.State = newE.State;
        }

        var rend = entity.GetComponent<RenderComponent>();
        if (rend != null)
        {
            rend.Color = GetEnemyColor(newE.State);
        }
    }

    private void RegisterMapEvents(DbConnection conn)
    {
        conn.Db.MapInstance.OnInsert += HandleMapInsert;
        conn.Db.MapInstance.OnUpdate += HandleMapUpdate;
        conn.Db.MapTransition.OnInsert += HandleTransitionInsert;
    }
    
    private void HandleTransitionInsert(EventContext ctx, MapTransition t)
    {
        // Only spawn if relevant for current map
        if (t.MapId != _mapSystem.CurrentMapId) return;
        
        if (_transitionEntityMap.ContainsKey(t.Id)) return;

        var entity = _world.CreateEntity();
        _transitionEntityMap[t.Id] = entity.Id;
        
        entity.AddComponent(new PositionComponent { Position = new Vector2(t.X, t.Y) });
        
        entity.AddComponent(new RenderComponent { 
            IsCircle = false, 
            Color = Color.Brown,
            Width = t.Width,
            Height = t.Height
        });
        
        Console.WriteLine($"[Sync] Transition Spawned: To {t.DestMapId} at ({t.X}, {t.Y})");
    }
    
    private void RegisterInteractableEvents(DbConnection conn)
    {
        conn.Db.InteractableObject.OnInsert += HandleInteractableInsert;
        // conn.Db.InteractableObject.OnUpdate += HandleInteractableUpdate; // MVP: Static pos usually
        // conn.Db.InteractableObject.OnDelete += HandleInteractableDelete;
    }

    private void HandleMapInsert(EventContext ctx, MapInstance m)
    {
        _mapSystem.UpdateMapInfo(m.KeyId, m.Metadata);
    }
    private void HandleMapUpdate(EventContext ctx, MapInstance oldM, MapInstance newM)
    {
        _mapSystem.UpdateMapInfo(newM.KeyId, newM.Metadata);
    }
    
    private void HandleInteractableInsert(EventContext ctx, InteractableObject i)
    {
         // Assuming we can map interactable ID to entity map for updates later if needed
         // For now, fire and forget entity creation or keep map
         var entity = _world.CreateEntity();
         
         entity.AddComponent(new PositionComponent { Position = new Vector2(i.PositionX, i.PositionY) });
         
         // Brown for Transitions / Portals
         var color = Color.Brown; 
         // Could check i.ObjectType == "Portal" vs "Resource" etc.
         
         entity.AddComponent(new RenderComponent { 
             IsCircle = false, 
             Color = color, 
             Radius = 20 // Slightly larger/distinct
         });
    }

    private void HandleEnemyDelete(EventContext ctx, Enemy e)
    {
        if (_enemyEntityMap.TryGetValue(e.Id, out var entityId))
        {
            _world.DestroyEntity(entityId);
            _enemyEntityMap.Remove(e.Id);
        }
    }

    private Color GetEnemyColor(string state) => state switch
    {
        "Chasing" => Color.Red,
        "ChasingThroughMap" => Color.Purple,
        _ => Color.Orange
    };

    public void Draw() { }
}
