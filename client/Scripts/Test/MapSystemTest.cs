using Godot;
using GuildmasterMVP.Core;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Test
{
    public partial class MapSystemTest : Node
    {
        private MapSystem _mapSystem;
        private SpacetimeDBClient _mockClient;
        
        public override void _Ready()
        {
            GD.Print("MapSystemTest: Starting map system tests");
            
            // Create mock network client
            _mockClient = new SpacetimeDBClient();
            
            // Create map system
            _mapSystem = new MapSystem(_mockClient, 1);
            
            RunTests();
        }
        
        private void RunTests()
        {
            TestMapRegistry();
            TestTransitionZoneDetection();
            TestSpawnPoints();
            TestMapLoading();
            
            GD.Print("MapSystemTest: All tests completed");
        }
        
        private void TestMapRegistry()
        {
            GD.Print("Testing map registry...");
            
            // Test getting map data
            var startingMapData = _mapSystem.GetMapData("starting_area");
            var forestMapData = _mapSystem.GetMapData("forest_area");
            var invalidMapData = _mapSystem.GetMapData("invalid_map");
            
            if (startingMapData.HasValue)
            {
                GD.Print("✓ Starting area map data found");
                GD.Print($"  Map size: {startingMapData.Value.Size}");
                GD.Print($"  Transitions: {startingMapData.Value.Transitions.Count}");
                GD.Print($"  Spawn points: {startingMapData.Value.SpawnPoints.Count}");
            }
            else
            {
                GD.PrintErr("✗ Starting area map data not found");
            }
            
            if (forestMapData.HasValue)
            {
                GD.Print("✓ Forest area map data found");
            }
            else
            {
                GD.PrintErr("✗ Forest area map data not found");
            }
            
            if (!invalidMapData.HasValue)
            {
                GD.Print("✓ Invalid map correctly returns null");
            }
            else
            {
                GD.PrintErr("✗ Invalid map should return null");
            }
        }
        
        private void TestTransitionZoneDetection()
        {
            GD.Print("Testing transition zone detection...");
            
            // Set current map to starting area
            _mapSystem.SpawnPlayerAtMap("starting_area");
            
            // Test position inside transition zone
            var transitionPosition = new Vector2(975, 500); // Inside transition area
            bool inTransition = _mapSystem.IsInTransitionZone(transitionPosition, out string destinationMapId);
            
            if (inTransition && destinationMapId == "forest_area")
            {
                GD.Print("✓ Transition zone detection works correctly");
                GD.Print($"  Destination: {destinationMapId}");
            }
            else
            {
                GD.PrintErr($"✗ Transition zone detection failed. InTransition: {inTransition}, Destination: {destinationMapId}");
            }
            
            // Test position outside transition zone
            var normalPosition = new Vector2(500, 500); // Normal area
            bool notInTransition = _mapSystem.IsInTransitionZone(normalPosition, out string noDestination);
            
            if (!notInTransition)
            {
                GD.Print("✓ Non-transition area correctly detected");
            }
            else
            {
                GD.PrintErr($"✗ Non-transition area incorrectly detected as transition to {noDestination}");
            }
        }
        
        private void TestSpawnPoints()
        {
            GD.Print("Testing spawn points...");
            
            // Test spawn points for starting area
            var spawnPoint1 = _mapSystem.GetSpawnPoint("starting_area", 0);
            var spawnPoint2 = _mapSystem.GetSpawnPoint("starting_area", 1);
            var spawnPoint5 = _mapSystem.GetSpawnPoint("starting_area", 4); // Should cycle back to index 0
            
            GD.Print($"✓ Spawn point 0: {spawnPoint1}");
            GD.Print($"✓ Spawn point 1: {spawnPoint2}");
            GD.Print($"✓ Spawn point 4 (cycled): {spawnPoint5}");
            
            if (spawnPoint1 == spawnPoint5)
            {
                GD.Print("✓ Spawn point cycling works correctly");
            }
            else
            {
                GD.PrintErr("✗ Spawn point cycling failed");
            }
            
            // Test invalid map
            var invalidSpawn = _mapSystem.GetSpawnPoint("invalid_map", 0);
            if (invalidSpawn == Vector2.Zero)
            {
                GD.Print("✓ Invalid map spawn point returns zero");
            }
            else
            {
                GD.PrintErr("✗ Invalid map spawn point should return zero");
            }
        }
        
        private void TestMapLoading()
        {
            GD.Print("Testing map loading...");
            
            // Test loading valid maps
            _mapSystem.LoadMapInstance("starting_area");
            _mapSystem.LoadMapInstance("forest_area");
            
            // Test loading invalid map
            _mapSystem.LoadMapInstance("invalid_map");
            
            GD.Print("✓ Map loading tests completed (check console for errors)");
        }
    }
}