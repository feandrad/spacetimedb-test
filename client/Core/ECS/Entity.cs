using System;
using System.Collections.Generic;
using System.Linq;

namespace Guildmaster.Client.Core.ECS;

public class Entity(int id)
{
    public int Id { get; } = id;
    private readonly List<Component> _components = new();

    public void AddComponent<T>(T component) where T : Component
    {
        _components.Add(component);
    }

    public T? GetComponent<T>() where T : Component
    {
        return _components.OfType<T>().FirstOrDefault();
    }
    
    public bool HasComponent<T>() where T : Component
    {
         return _components.OfType<T>().Any();
    }

    public void RemoveComponent<T>() where T : Component
    {
        var comp = GetComponent<T>();
        if (comp != null) _components.Remove(comp);
    }
   
    public void RemoveAllComponents() 
    {
         _components.Clear();
    }
}
