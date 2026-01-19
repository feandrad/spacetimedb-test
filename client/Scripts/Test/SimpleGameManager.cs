using Godot;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Simplified GameManager for basic movement testing
    /// Avoids complex system initialization that causes compilation errors
    /// </summary>
    public partial class SimpleGameManager : Node
    {
        public static SimpleGameManager Instance { get; private set; }
        
        public override void _Ready()
        {
            Instance = this;
            GD.Print("SimpleGameManager initialized for basic testing");
        }
        
        public override void _ExitTree()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}