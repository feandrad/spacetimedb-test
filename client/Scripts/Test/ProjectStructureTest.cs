using Godot;
using GuildmasterMVP.Core;
using GuildmasterMVP.Data;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Test
{
	/// <summary>
	/// Simple test to verify project structure and dependencies
	/// </summary>
	public partial class ProjectStructureTest : Node
	{
		public override void _Ready()
		{
			GD.Print("=== Project Structure Test ===");
			
			// Test core interfaces exist
			TestCoreInterfaces();
			
			// Test data structures exist
			TestDataStructures();
			
			// Test network components exist
			TestNetworkComponents();
			
			GD.Print("=== Project Structure Test Complete ===");
		}
		
		private void TestCoreInterfaces()
		{
			GD.Print("Testing Core Interfaces...");
			
			// Verify interfaces are accessible
			var inputManagerType = typeof(IInputManager);
			var movementSystemType = typeof(IMovementSystem);
			var combatSystemType = typeof(ICombatSystem);
			var mapSystemType = typeof(IMapSystem);
			var inventorySystemType = typeof(IInventorySystem);
			var interactionManagerType = typeof(IInteractionManager);
			
			GD.Print($"✓ IInputManager: {inputManagerType.Name}");
			GD.Print($"✓ IMovementSystem: {movementSystemType.Name}");
			GD.Print($"✓ ICombatSystem: {combatSystemType.Name}");
			GD.Print($"✓ IMapSystem: {mapSystemType.Name}");
			GD.Print($"✓ IInventorySystem: {inventorySystemType.Name}");
			GD.Print($"✓ IInteractionManager: {interactionManagerType.Name}");
		}
		
		private void TestDataStructures()
		{
			GD.Print("Testing Data Structures...");
			
			// Test data structure creation
			var playerData = new PlayerData
			{
				Id = 1,
				Position = Vector2.Zero,
				Health = 100.0f,
				MaxHealth = 100.0f
			};
			
			var weaponData = new WeaponData
			{
				Id = "sword",
				Type = WeaponType.Sword,
				Damage = 25.0f,
				Range = 2.0f
			};
			
			GD.Print($"✓ PlayerData: ID={playerData.Id}, Health={playerData.Health}");
			GD.Print($"✓ WeaponData: ID={weaponData.Id}, Type={weaponData.Type}");
		}
		
		private void TestNetworkComponents()
		{
			GD.Print("Testing Network Components...");
			
			// Test SpacetimeDB client can be instantiated
			var dbClient = new SpacetimeDBClient();
			GD.Print($"✓ SpacetimeDBClient: {dbClient.GetType().Name}");
			
			// Test GameManager can be instantiated
			var gameManager = new GameManager();
			GD.Print($"✓ GameManager: {gameManager.GetType().Name}");
			
			// Clean up
			dbClient?.QueueFree();
			gameManager?.QueueFree();
		}
	}
}
