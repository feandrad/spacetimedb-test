using System.Collections.Generic;

namespace Guildmaster.Client.Core.ECS;

public class GameWorld
{
    private readonly List<ISystem> _systems = new();
    private readonly Dictionary<int, Entity> _entities = new();
    private int _nextEntityId = 1;

    public Entity CreateEntity()
    {
        var entity = new Entity(_nextEntityId++);
        _entities.Add(entity.Id, entity);
        return entity;
    }

    public void DestroyEntity(int id)
    {
        _entities.Remove(id);
    }
    
    public Entity? GetEntity(int id) 
    {
        _entities.TryGetValue(id, out var entity);
        return entity;
    }
    
    public IEnumerable<Entity> GetEntities() => _entities.Values;

    public void AddSystem(ISystem system)
    {
        _systems.Add(system);
    }

    public void Update(float deltaTime)
    {
        foreach (var system in _systems)
        {
            system.Update(deltaTime);
        }
    }

    public void Draw()
    {
        foreach (var system in _systems)
        {
            system.Draw();
        }
    }
}
