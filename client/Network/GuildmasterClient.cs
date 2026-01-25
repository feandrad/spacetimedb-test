using SpacetimeDB;
using SpacetimeDB.Types;

namespace Guildmaster.Client.Network;

public sealed class GuildmasterClient
{
    public DbConnection? Connection { get; private set; }
    public Identity? Identity { get; private set; }

    public bool IsConnected => Connection?.IsActive ?? false;

    public void Connect(string host, string moduleName)
    {
        // We only support in-memory tokens or auto-generated ones now.
        // If we wanted to support passing a token string directly, we could add an argument.
        // For now, we trust the SpacetimeDB SDK to either generate a new one or use internal mechanisms 
        // if we don't explicitly pass one, but the user requested "not as a file".
        // To be safe and purely ephemeral, we can pass nothing or handle it manually if needed.
        
        // However, if we want persistence across runs *without* files, we can't do it.
        // But the user constraint is "token should be saved in memory not as a file".
        // This implies session-based identity. 
        // If we want to support existing AuthToken flow (which might save to file implicitly by SDK),
        // we should probably avoid calling AuthToken.SaveToken explicitly.
        // The SDK's AuthToken.Init() usually loads from a default file. 
        // If the user wants NO file, we should probably SKIP AuthToken.Init() and passing a token.
        // This causes SpacetimeDB to generate a fresh identity every connection.
        
        Connection = DbConnection.Builder()
            .WithUri(host)
            .WithModuleName(moduleName)
            //.WithToken(savedToken) // Don't look for saved tokens
            .OnConnect((conn, identity, token) =>
            {
                Console.WriteLine($"[DEBUG] OnConnect Fired! Identity: {identity}");
                Identity = identity;
                
                // User requested NO file storage for security.
                // We keep the token in memory (it's inside 'conn' or we can store it properly if needed).
                // We DO NOT save it to disk.

                Console.WriteLine($"Connected as {identity} (Ephemeral)");
                
                // Initial subscription: Only listen for players to find ourselves
                SubscribeToPlayers();
                
                // Register Reducer handlers
                conn.Reducers.OnRegisterPlayer += (ctx, name) => 
                {
                    if (ctx.Event.Status is Status.Failed(var reason))
                    {
                        Console.WriteLine($"[Client] Registration Failed: {reason}");
                        OnRegistrationError?.Invoke(reason);
                    }
                    else if (ctx.Event.Status is Status.OutOfEnergy)
                    {
                         Console.WriteLine($"[Client] Registration Failed: Out of Energy");
                         OnRegistrationError?.Invoke("Out of Energy");
                    }
                    else
                    {
                        Console.WriteLine($"[Client] Registration of {name} processed (Status: {ctx.Event.Status})");
                    }
                };
            })
            .OnConnectError(e =>
            {
                Console.WriteLine($"Connect error: {e}");
                OnConnectionError?.Invoke(e.ToString());
            })
            .OnDisconnect((conn, e) =>
            {
                Console.WriteLine(e != null ? $"Disconnected abnormally: {e}" : "Disconnected normally.");
                Identity = null;
                OnDisconnected?.Invoke();
            })
            .Build();
    }

    public event Action<string>? OnConnectionError;
    public event Action<string>? OnRegistrationError;
    public event Action? OnDisconnected;
    
    public void SubscribeToPlayers()
    {
         Connection.SubscriptionBuilder()
            .OnApplied(ctx => Console.WriteLine("Subscribed into players!"))
            .OnError((ctx, ex) => Console.WriteLine($"Error on subscribing players: {ex.Message}"))
            .Subscribe(["SELECT * FROM player"]);
    }
    
    public void SubscribeToMap(string mapId, uint? localPlayerId = null)
    {
        // Subscribe to map-specific entities + Local Player explicitly
        var queries = new List<string>
        {
            $"SELECT * FROM player WHERE current_map_id = '{mapId}'",
            $"SELECT * FROM enemy WHERE map_id = '{mapId}'",
            $"SELECT * FROM interactable_object WHERE map_id = '{mapId}'",
            $"SELECT * FROM map_instance WHERE key_id = '{mapId}'"
        };
        
        // Ensure we always track ourselves so we don't return null in GetLocalPlayer()
        if (localPlayerId.HasValue)
        {
            queries.Add($"SELECT * FROM player WHERE id = {localPlayerId.Value}");
        }

        Connection.SubscriptionBuilder()
            .OnApplied(ctx => {
                Console.WriteLine($"Map data for {mapId} loaded successfully!");
            })
            .OnError((ctx, ex) => {
                Console.WriteLine($"Subscription error: {ex.Message}");
            })
            .Subscribe(queries.ToArray());
    }

    public void Register(string username)
    {
        Connection?.Reducers.RegisterPlayer(username);
    }

    public void Tick() => Connection?.FrameTick();

    public void Disconnect()
    {
        Connection?.Disconnect();
        Connection = null;
        Identity = null;
    }

    public void ResetIdentity()
    {
        Disconnect();
        Console.WriteLine("[Client] Identity reset (Ephemeral).");
    }
}