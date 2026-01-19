using Godot;
using System.Threading.Tasks;
using GuildmasterMVP.Network;

namespace GuildmasterMVP.Test
{
    /// <summary>
    /// Test to verify connection behavior with server offline/online
    /// Run this to verify proper error handling
    /// </summary>
    public partial class ConnectionBehaviorTest : Node
    {
        public override async void _Ready()
        {
            GD.Print("=== Connection Behavior Test ===");
            GD.Print("");
            
            await TestOfflineServer();
            await Task.Delay(2000);
            await TestOnlineServer();
        }
        
        private async Task TestOfflineServer()
        {
            GD.Print("TEST 1: Connecting to offline server (wrong port)");
            GD.Print("Expected: Connection should FAIL after retries");
            GD.Print("");
            
            var client = new SpacetimeDBClient();
            AddChild(client);
            
            bool connected = false;
            bool failed = false;
            
            client.Connected += (identity) =>
            {
                connected = true;
                GD.Print("❌ UNEXPECTED: Connected when server should be offline!");
            };
            
            client.ConnectionError += (error) =>
            {
                failed = true;
                GD.Print($"✅ EXPECTED: Connection failed - {error}");
            };
            
            // Try to connect to wrong port (server offline)
            var config = new ConnectionConfig
            {
                ServerUri = "http://localhost:9999", // Wrong port
                MaxRetryAttempts = 2, // Fewer retries for faster test
                InitialRetryDelaySeconds = 0.5f,
                EnableDebugLogging = true
            };
            client.Configure(config);
            
            bool result = await client.ConnectAsync();
            
            GD.Print("");
            GD.Print($"Connection result: {result}");
            GD.Print($"Connected: {connected}");
            GD.Print($"Failed: {failed}");
            
            if (!result && failed && !connected)
            {
                GD.Print("✅ TEST 1 PASSED: Properly failed when server offline");
            }
            else
            {
                GD.PrintErr("❌ TEST 1 FAILED: Did not handle offline server correctly");
            }
            
            client.QueueFree();
            GD.Print("");
            GD.Print("---");
            GD.Print("");
        }
        
        private async Task TestOnlineServer()
        {
            GD.Print("TEST 2: Connecting to online server (correct port)");
            GD.Print("Expected: Connection should SUCCEED if server is running");
            GD.Print("         Connection should FAIL if server is not running");
            GD.Print("");
            
            var client = new SpacetimeDBClient();
            AddChild(client);
            
            bool connected = false;
            bool failed = false;
            
            client.Connected += (identity) =>
            {
                connected = true;
                GD.Print($"✅ Connected successfully (identity: {identity})");
            };
            
            client.ConnectionError += (error) =>
            {
                failed = true;
                GD.Print($"⚠️  Connection failed (server not running?) - {error}");
            };
            
            // Try to connect to correct port
            var config = new ConnectionConfig
            {
                ServerUri = "http://localhost:7734", // Correct port
                MaxRetryAttempts = 2,
                InitialRetryDelaySeconds = 0.5f,
                EnableDebugLogging = true
            };
            client.Configure(config);
            
            bool result = await client.ConnectAsync();
            
            GD.Print("");
            GD.Print($"Connection result: {result}");
            GD.Print($"Connected: {connected}");
            GD.Print($"Failed: {failed}");
            
            if (result && connected)
            {
                GD.Print("✅ TEST 2 PASSED: Successfully connected to server");
            }
            else if (!result && failed)
            {
                GD.Print("⚠️  TEST 2: Server not running on port 7734");
                GD.Print("   Start your SpacetimeDB server to test successful connection");
            }
            else
            {
                GD.PrintErr("❌ TEST 2 FAILED: Unexpected connection state");
            }
            
            client.QueueFree();
            GD.Print("");
            GD.Print("=== Test Complete ===");
        }
    }
}
