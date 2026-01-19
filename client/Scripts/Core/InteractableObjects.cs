using Godot;
using System.Collections.Generic;
using System.Linq;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Core
{
    /// <summary>
    /// Tree object implementation
    /// Requirements 6.2: Tree actions with axe requirement for cutting
    /// </summary>
    public class TreeObject : IInteractableObject
    {
        public uint Id { get; private set; }
        public Vector2 Position { get; private set; }
        public float InteractionRange => 2.0f;
        
        private int _health = 3;
        private int _fruitCount = 2;
        
        public TreeObject(uint id, Vector2 position)
        {
            Id = id;
            Position = position;
        }
        
        public ContextualAction[] GetAvailableActions(uint playerId)
        {
            var actions = new List<ContextualAction>();
            
            // Shake action - no requirements
            if (_fruitCount > 0)
            {
                actions.Add(new ContextualAction(
                    ActionType.Shake,
                    "Shake Tree",
                    new ActionRequirement[0] // No requirements for shaking
                ));
            }
            
            // Cut action - requires axe
            if (_health > 0)
            {
                actions.Add(new ContextualAction(
                    ActionType.Cut,
                    "Cut Tree",
                    new ActionRequirement[]
                    {
                        new ActionRequirement(
                            RequirementType.EquippedWeapon,
                            "axe",
                            true,
                            1,
                            "Requires equipped axe"
                        )
                    }
                ));
            }
            
            return actions.ToArray();
        }
        
        public InteractionResult ExecuteAction(uint playerId, ActionType actionType, ActionParameters parameters)
        {
            switch (actionType)
            {
                case ActionType.Shake:
                    return HandleShake();
                case ActionType.Cut:
                    return HandleCut();
                default:
                    return new InteractionResult(false, "Invalid action for tree");
            }
        }
        
        public bool IsInRange(Vector2 playerPosition)
        {
            return Position.DistanceTo(playerPosition) <= InteractionRange;
        }
        
        private InteractionResult HandleShake()
        {
            if (_fruitCount <= 0)
            {
                return new InteractionResult(false, "No fruit to shake");
            }
            
            _fruitCount--;
            
            var result = new InteractionResult(true, "Shook fruit from tree");
            result.ItemsGenerated = new ItemDrop[]
            {
                new ItemDrop("fruit", 1, Position)
            };
            
            return result;
        }
        
        private InteractionResult HandleCut()
        {
            if (_health <= 0)
            {
                return new InteractionResult(false, "Tree already cut down");
            }
            
            _health--;
            
            var items = new List<ItemDrop>
            {
                new ItemDrop("wood", 1, Position)
            };
            
            string message = "Damaged tree";
            
            if (_health == 0)
            {
                // Tree is fully cut down, give extra wood
                items.Add(new ItemDrop("wood", 2, Position));
                message = "Tree cut down completely";
            }
            
            var result = new InteractionResult(true, message);
            result.ItemsGenerated = items.ToArray();
            
            return result;
        }
    }
    
    /// <summary>
    /// Rock object implementation
    /// Requirements 6.3: Rock actions with pickaxe requirement for breaking
    /// </summary>
    public class RockObject : IInteractableObject
    {
        public uint Id { get; private set; }
        public Vector2 Position { get; private set; }
        public float InteractionRange => 1.5f;
        
        private bool _isPickedUp = false;
        private int _durability = 2;
        
        public RockObject(uint id, Vector2 position)
        {
            Id = id;
            Position = position;
        }
        
        public ContextualAction[] GetAvailableActions(uint playerId)
        {
            var actions = new List<ContextualAction>();
            
            // Pick up action - no requirements
            if (!_isPickedUp)
            {
                actions.Add(new ContextualAction(
                    ActionType.PickUp,
                    "Pick Up Rock",
                    new ActionRequirement[0] // No requirements for picking up
                ));
            }
            
            // Break action - requires pickaxe
            if (_durability > 0 && !_isPickedUp)
            {
                actions.Add(new ContextualAction(
                    ActionType.Break,
                    "Break Rock",
                    new ActionRequirement[]
                    {
                        new ActionRequirement(
                            RequirementType.EquippedWeapon,
                            "pickaxe",
                            true,
                            1,
                            "Requires equipped pickaxe"
                        )
                    }
                ));
            }
            
            return actions.ToArray();
        }
        
        public InteractionResult ExecuteAction(uint playerId, ActionType actionType, ActionParameters parameters)
        {
            switch (actionType)
            {
                case ActionType.PickUp:
                    return HandlePickUp();
                case ActionType.Break:
                    return HandleBreak();
                default:
                    return new InteractionResult(false, "Invalid action for rock");
            }
        }
        
        public bool IsInRange(Vector2 playerPosition)
        {
            return Position.DistanceTo(playerPosition) <= InteractionRange;
        }
        
        private InteractionResult HandlePickUp()
        {
            if (_isPickedUp)
            {
                return new InteractionResult(false, "Rock already picked up");
            }
            
            _isPickedUp = true;
            
            var result = new InteractionResult(true, "Picked up rock");
            result.ItemsGenerated = new ItemDrop[]
            {
                new ItemDrop("stone", 1, Position)
            };
            result.StateChanges = new ObjectStateChange[]
            {
                new ObjectStateChange("visible", false)
            };
            
            return result;
        }
        
        private InteractionResult HandleBreak()
        {
            if (_durability <= 0)
            {
                return new InteractionResult(false, "Rock already broken");
            }
            
            _durability--;
            
            var items = new List<ItemDrop>
            {
                new ItemDrop("stone_fragment", 1, Position)
            };
            
            string message = "Chipped rock";
            
            if (_durability == 0)
            {
                // Rock is fully broken, give extra stone
                items.Add(new ItemDrop("stone", 1, Position));
                message = "Rock broken completely";
            }
            
            var result = new InteractionResult(true, message);
            result.ItemsGenerated = items.ToArray();
            
            return result;
        }
    }
}