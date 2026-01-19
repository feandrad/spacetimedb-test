using Godot;
using System;

namespace GuildmasterMVP.Network
{
    /// <summary>
    /// Configuration for SpacetimeDB connection
    /// </summary>
    public partial class ConnectionConfig : Resource
    {
        [Export] public string ServerUri { get; set; } = "http://localhost:7734";
        [Export] public string ModuleName { get; set; } = "guildmaster";
        [Export] public float ConnectionTimeoutSeconds { get; set; } = 10f;
        [Export] public float HeartbeatIntervalSeconds { get; set; } = 30f;
        [Export] public int MaxRetryAttempts { get; set; } = 5;
        [Export] public float InitialRetryDelaySeconds { get; set; } = 1f;
        [Export] public float RetryBackoffMultiplier { get; set; } = 2f;
        [Export] public float MaxRetryDelaySeconds { get; set; } = 30f;
        [Export] public bool EnableAutoReconnect { get; set; } = true;
        [Export] public bool EnableDebugLogging { get; set; } = true;
        
        public ConnectionConfig()
        {
        }
        
        public ConnectionConfig(string serverUri, string moduleName)
        {
            ServerUri = serverUri;
            ModuleName = moduleName;
        }
        
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ServerUri))
                throw new ArgumentException("ServerUri cannot be empty");
                
            if (string.IsNullOrWhiteSpace(ModuleName))
                throw new ArgumentException("ModuleName cannot be empty");
                
            if (ConnectionTimeoutSeconds <= 0)
                throw new ArgumentException("ConnectionTimeoutSeconds must be positive");
                
            if (MaxRetryAttempts < 0)
                throw new ArgumentException("MaxRetryAttempts cannot be negative");
        }
        
        public override string ToString()
        {
            return $"ConnectionConfig(Uri={ServerUri}, Module={ModuleName}, Timeout={ConnectionTimeoutSeconds}s)";
        }
    }
}
