# Design Document

## Overview

The SpacetimeDB Integration system provides a comprehensive client-side connectivity layer for the Guildmaster MVP, enabling reliable real-time communication between the Godot 4 (C#) client and SpacetimeDB server. The design emphasizes network resilience, responsive gameplay through client-side prediction, and efficient data synchronization while maintaining server authority for all critical game logic.

The system abstracts the complexity of WebSocket communication, BSATN serialization, and subscription management behind clean interfaces that integrate seamlessly with Godot's architecture and the existing game systems.

## Architecture

### Client-Server Communication Model
- **WebSocket Connection**: Persistent connection using SpacetimeDB's WebSocket protocol
- **BSATN Serialization**: Binary SpaceTime Algebraic Notation for efficient data transfer
- **Reducer Pattern**: Client sends inputs via reducers, server processes and broadcasts state changes
- **Subscription System**: Real-time data streams filtered by relevance and spatial proximity
- **Client Prediction**: Local simulation with server reconciliation for responsive gameplay

### Core Architecture Layers
```
┌─────────────────────────────────────────────────────────────┐
│                    Game Systems Layer                       │
│  (Movement, Combat, Inventory, Map - existing systems)     │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                SpacetimeDB Integration Layer                │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Connection Mgr  │  │ State Sync Mgr  │  │ Prediction  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Auth Manager    │  │ Subscription    │  │ Reducer     │ │
│  │                 │  │ Manager         │  │ Client      │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                SpacetimeDB C# SDK Layer                     │
│        (DbConnection, RemoteTables, RemoteReducers)        │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Network Transport                        │
│              (WebSocket + BSATN Protocol)                  │
└─────────────────────────────────────────────────────────────┘
```

### Integration with Godot Architecture
- **Singleton Pattern**: Core managers implemented as Godot AutoLoad singletons
- **Signal-Based Communication**: Godot signals for decoupled event handling
- **Scene Tree Integration**: Proper lifecycle management with Godot's scene system
- **Thread Safety**: Main thread processing with background network handling

## Components and Interfaces

### Connection Manager
```csharp
public interface IConnectionManager
{
    // Connection lifecycle
    Task<bool> ConnectAsync(string uri, string moduleName, string token = null);
    void Disconnect();
    bool IsConnected { get; }
    ConnectionState State { get; }
    
    // Connection health
    float Latency { get; }
    DateTime LastHeartbeat { get; }
    void StartHeartbeat(float intervalSeconds = 30f);
    
    // Events
    event Action<Identity, string> OnConnected;
    event Action<string> OnConnectionError;
    event Action<string> OnDisconnected;
    event Action<ConnectionState> OnStateChanged;
}

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Failed
}
```

### Authentication Manager
```csharp
public interface IAuthenticationManager
{
    // Identity management
    Task<AuthResult> AuthenticateAsync(string token = null);
    Task<string> RefreshTokenAsync();
    void ClearCredentials();
    
    // Session management
    Identity? CurrentIdentity { get; }
    string? CurrentToken { get; }
    bool IsAuthenticated { get; }
    DateTime TokenExpiry { get; }
    
    // Events
    event Action<Identity> OnAuthenticated;
    event Action<string> OnAuthenticationFailed;
    event Action OnTokenRefreshed;
}

public struct AuthResult
{
    public bool Success;
    public Identity? Identity;
    public string? Token;
    public string? ErrorMessage;
}
```

### State Synchronization Manager
```csharp
public interface IStateSynchronizationManager
{
    // Subscription management
    Task<SubscriptionHandle> SubscribeAsync(string[] queries);
    Task<SubscriptionHandle> SubscribeToMapAsync(string mapId);
    Task UnsubscribeAsync(SubscriptionHandle handle);
    void UnsubscribeAll();
    
    // State access
    RemoteTables Tables { get; }
    T? GetEntity<T>(uint entityId) where T : class;
    IEnumerable<T> GetEntitiesInRange<T>(Vector2 position, float range) where T : class;
    
    // Events
    event Action<SubscriptionHandle> OnSubscriptionApplied;
    event Action<SubscriptionHandle, Exception> OnSubscriptionError;
    event Action OnStateUpdated;
}
```

### Client Prediction System
```csharp
public interface IClientPredictionSystem
{
    // Prediction management
    void PredictAction<T>(T action, uint sequenceNumber) where T : struct;
    void ConfirmAction(uint sequenceNumber, object serverResult);
    void RejectAction(uint sequenceNumber, object serverResult);
    
    // State management
    void SavePredictionState();
    void RollbackToSequence(uint sequenceNumber);
    void ClearPredictionHistory(uint beforeSequence);
    
    // Configuration
    int MaxPredictionHistory { get; set; }
    float ReconciliationSmoothingTime { get; set; }
    
    // Events
    event Action<uint> OnPredictionConfirmed;
    event Action<uint, object> OnPredictionRejected;
    event Action<uint> OnStateReconciled;
}

public struct PredictionState
{
    public uint SequenceNumber;
    public DateTime Timestamp;
    public object GameState;
    public object Action;
}
```

### Reducer Client
```csharp
public interface IReducerClient
{
    // Reducer invocation
    Task<ReducerResult> CallAsync<T>(string reducerName, T parameters) where T : struct;
    void CallWithPrediction<T>(string reducerName, T parameters, uint sequenceNumber) where T : struct;
    
    // Batch operations
    void BeginBatch();
    Task<ReducerResult[]> ExecuteBatchAsync();
    void CancelBatch();
    
    // Callback management
    void RegisterCallback<T>(string reducerName, Action<ReducerEventContext, T> callback) where T : struct;
    void UnregisterCallback<T>(string reducerName, Action<ReducerEventContext, T> callback) where T : struct;
    
    // Events
    event Action<string, object> OnReducerCalled;
    event Action<string, ReducerResult> OnReducerCompleted;
    event Action<string, Exception> OnReducerFailed;
}

public struct ReducerResult
{
    public bool Success;
    public uint SequenceNumber;
    public object? Result;
    public string? ErrorMessage;
    public TimeSpan ExecutionTime;
}
```

### Network Resilience Manager
```csharp
public interface INetworkResilienceManager
{
    // Connection monitoring
    void StartMonitoring();
    void StopMonitoring();
    NetworkHealth CurrentHealth { get; }
    
    // Resilience configuration
    RetryPolicy RetryPolicy { get; set; }
    CircuitBreakerConfig CircuitBreaker { get; set; }
    
    // Recovery operations
    Task<bool> AttemptRecoveryAsync();
    void ForceReconnect();
    
    // Events
    event Action<NetworkHealth> OnHealthChanged;
    event Action OnRecoveryStarted;
    event Action<bool> OnRecoveryCompleted;
}

public struct NetworkHealth
{
    public float Latency;
    public float PacketLoss;
    public int ConsecutiveFailures;
    public DateTime LastSuccessfulOperation;
    public bool IsHealthy;
}

public struct RetryPolicy
{
    public int MaxAttempts;
    public TimeSpan InitialDelay;
    public float BackoffMultiplier;
    public TimeSpan MaxDelay;
}

public struct CircuitBreakerConfig
{
    public int FailureThreshold;
    public TimeSpan OpenDuration;
    public int HalfOpenMaxCalls;
}
```

### Performance Monitor
```csharp
public interface IPerformanceMonitor
{
    // Metrics collection
    void RecordLatency(TimeSpan latency);
    void RecordReducerCall(string reducerName, bool success, TimeSpan duration);
    void RecordSubscriptionUpdate(int rowCount, int bytesReceived);
    void RecordPredictionAccuracy(bool accurate);
    
    // Statistics access
    PerformanceStats GetStats();
    PerformanceStats GetStats(TimeSpan window);
    void ResetStats();
    
    // Monitoring configuration
    bool IsEnabled { get; set; }
    TimeSpan SamplingInterval { get; set; }
    
    // Events
    event Action<PerformanceStats> OnStatsUpdated;
    event Action<string> OnPerformanceAlert;
}

public struct PerformanceStats
{
    public float AverageLatency;
    public float MaxLatency;
    public float ReducerSuccessRate;
    public float PredictionAccuracy;
    public int SubscriptionUpdatesPerSecond;
    public int BytesPerSecond;
    public DateTime CollectionPeriodStart;
    public DateTime CollectionPeriodEnd;
}
```

## Data Models

### Connection Configuration
```csharp
public struct ConnectionConfig
{
    public string ServerUri;
    public string ModuleName;
    public string? AuthToken;
    public TimeSpan ConnectionTimeout;
    public TimeSpan HeartbeatInterval;
    public RetryPolicy RetryPolicy;
    public bool EnableCompression;
    public Dictionary<string, string> CustomHeaders;
}
```

### Subscription Configuration
```csharp
public struct SubscriptionConfig
{
    public string[] Queries;
    public SubscriptionFilter Filter;
    public int MaxRowsPerUpdate;
    public bool EnableBatching;
    public TimeSpan BatchTimeout;
}

public struct SubscriptionFilter
{
    public string? MapId;
    public Vector2? SpatialCenter;
    public float? SpatialRadius;
    public string[] TableNames;
    public Dictionary<string, object> CustomFilters;
}
```

### Network Event Data
```csharp
public struct NetworkEvent
{
    public NetworkEventType Type;
    public DateTime Timestamp;
    public string? Message;
    public object? Data;
    public Exception? Error;
}

public enum NetworkEventType
{
    Connected,
    Disconnected,
    Reconnecting,
    SubscriptionApplied,
    SubscriptionError,
    ReducerCompleted,
    ReducerFailed,
    StateUpdated,
    PerformanceAlert
}
```

### Prediction History Entry
```csharp
public struct PredictionHistoryEntry
{
    public uint SequenceNumber;
    public DateTime Timestamp;
    public string ActionType;
    public object ActionData;
    public object PredictedState;
    public object? ServerState;
    public bool IsConfirmed;
    public TimeSpan? ConfirmationTime;
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

Now I need to use the prework tool to analyze the acceptance criteria before writing the correctness properties:

### Property 1: Connection Lifecycle Management
*For any* connection attempt, the system should handle the complete lifecycle including establishment, health monitoring, failure detection, and automatic recovery with exponential backoff.
**Validates: Requirements 1.2, 1.3, 1.6, 1.7**

### Property 2: Authentication Flow Consistency
*For any* authentication attempt, the system should handle identity generation, credential validation, token management, and session lifecycle consistently.
**Validates: Requirements 2.1, 2.3, 2.4, 2.5, 2.7**

### Property 3: Secure Credential Storage
*For any* player credentials, the system should store them securely locally and automatically refresh tokens before expiration.
**Validates: Requirements 2.2, 2.6**

### Property 4: State Synchronization Lifecycle
*For any* client connection, the system should establish appropriate subscriptions, receive state updates, apply changes locally, and update subscriptions based on context changes.
**Validates: Requirements 3.1, 3.2, 3.3, 3.5**

### Property 5: Subscription Filtering Efficiency
*For any* subscription, the system should filter based on relevance and location, handle updates efficiently, and prioritize server-authoritative data during conflicts.
**Validates: Requirements 3.4, 3.6, 3.7**

### Property 6: Client Prediction and Reconciliation
*For any* player input, the system should immediately apply local prediction, send to server with sequence numbers, compare results, and smoothly reconcile differences.
**Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5, 4.7**

### Property 7: Prediction History Management
*For any* prediction system, it should maintain input and prediction history for reconciliation purposes.
**Validates: Requirements 4.6**

### Property 8: Reducer Communication Lifecycle
*For any* reducer call, the system should provide unified interface, serialize with BSATN, include sequence numbers, handle failures with detailed errors, and notify on success.
**Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.7**

### Property 9: Reducer Input Validation and Retry
*For any* reducer call, the system should validate inputs before sending and implement retry logic with backoff for failures.
**Validates: Requirements 5.5, 5.6**

### Property 10: Network Resilience and Recovery
*For any* network connectivity loss, the system should detect disconnection quickly, maintain local state, automatically reconnect, and synchronize missed updates.
**Validates: Requirements 6.1, 6.2, 6.3, 6.4**

### Property 11: Adaptive Network Behavior
*For any* network conditions, the system should provide visual status indicators, adjust parameters for high latency, and implement circuit breaker patterns.
**Validates: Requirements 6.5, 6.6, 6.7**

### Property 12: Dynamic Subscription Management
*For any* player context change, the system should dynamically manage subscriptions, joining relevant tables and leaving irrelevant ones efficiently.
**Validates: Requirements 7.1, 7.2, 7.3, 7.5**

### Property 13: Subscription Error Handling and Monitoring
*For any* subscription system, it should implement spatial filtering, handle errors with retry logic, and provide status information for debugging.
**Validates: Requirements 7.4, 7.6, 7.7**

### Property 14: Data Serialization Consistency
*For any* data communication, the system should use BSATN format, validate before transmission, maintain type safety, and support both sync and async operations.
**Validates: Requirements 8.1, 8.2, 8.6, 8.7**

### Property 15: Serialization Error Handling and Efficiency
*For any* serialization operation, the system should handle version compatibility, provide clear error messages, and minimize payload size.
**Validates: Requirements 8.3, 8.4, 8.5**

### Property 16: Performance Monitoring and Alerting
*For any* system operation, performance metrics should be continuously tracked, monitored for threshold violations, and provide real-time statistics.
**Validates: Requirements 9.1, 9.2, 9.3, 9.4**

### Property 17: Performance Analysis and Export
*For any* performance monitoring system, it should track prediction accuracy, reconciliation frequency, and support profiling with data export.
**Validates: Requirements 9.5, 9.6, 9.7**

### Property 18: Configuration Management and Validation
*For any* configuration change, the system should support multiple sources, validate settings, provide defaults, and enable runtime switching.
**Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.6**

### Property 19: Secure Configuration and Hot-Reloading
*For any* configuration system, it should securely store sensitive data and support hot-reloading of non-critical changes.
**Validates: Requirements 10.5, 10.7**

## Error Handling

### Connection Errors
- **Initial Connection Failures**: Exponential backoff retry with maximum 5 attempts
- **Authentication Failures**: Clear error messages with retry options and credential validation
- **Network Interruptions**: Automatic reconnection with state preservation and synchronization
- **Timeout Handling**: Configurable timeouts with graceful degradation

### Data Synchronization Errors
- **Subscription Failures**: Retry logic with error reporting and fallback to broader subscriptions
- **State Conflicts**: Server-authoritative resolution with smooth client reconciliation
- **Missing Updates**: Gap detection and recovery through re-synchronization
- **Schema Mismatches**: Version compatibility handling with graceful degradation

### Prediction and Reconciliation Errors
- **Prediction Failures**: Rollback to last known good state with input replay
- **Reconciliation Conflicts**: Smooth interpolation to server state with minimal visual disruption
- **Sequence Number Gaps**: Detection and recovery through state re-synchronization
- **History Overflow**: Automatic cleanup with configurable retention policies

### Serialization Errors
- **BSATN Failures**: Clear error messages with data validation and recovery suggestions
- **Type Mismatches**: Runtime type checking with conversion attempts where safe
- **Version Incompatibility**: Schema evolution support with backward compatibility
- **Payload Size Limits**: Automatic chunking and compression for large data

### Performance and Resource Errors
- **Memory Pressure**: Automatic cleanup of prediction history and cached data
- **CPU Overload**: Adaptive throttling of non-critical operations
- **Network Congestion**: Quality-of-service adjustments and priority queuing
- **Storage Failures**: Fallback to in-memory storage with persistence retry

## Testing Strategy

### Dual Testing Approach
The testing strategy employs both unit tests and property-based tests as complementary approaches:

- **Unit Tests**: Verify specific connection scenarios, authentication flows, and error conditions
- **Property Tests**: Verify universal network behaviors across all inputs using randomized testing
- **Integration Tests**: Verify end-to-end SpacetimeDB communication and multiplayer synchronization
- **Performance Tests**: Verify system behavior under various network conditions and load

### Property-Based Testing Configuration
- **Testing Framework**: Use fast-check for TypeScript/JavaScript or FsCheck for C# property-based testing
- **Test Iterations**: Minimum 100 iterations per property test to ensure comprehensive coverage
- **Test Tagging**: Each property test must reference its design document property using the format:
  - **Feature: spacetimedb-integration, Property {number}: {property_text}**

### Unit Testing Focus
Unit tests should concentrate on:
- Specific connection establishment and failure scenarios
- Authentication token handling and refresh cycles
- Edge cases in network interruption and recovery
- Error conditions in serialization and deserialization
- Integration points between SpacetimeDB SDK and game systems

### Property Testing Focus
Property tests should verify:
- Connection lifecycle behaviors across various network conditions
- Authentication flows with different credential types and states
- State synchronization with randomized data and subscription patterns
- Client prediction accuracy with various input sequences and latencies
- Serialization consistency across different data types and sizes

### Integration Testing Requirements
- **End-to-End Flows**: Complete connection, authentication, and gameplay scenarios
- **Multiplayer Synchronization**: Multiple clients with concurrent actions and state changes
- **Network Resilience**: Connection loss and recovery under various conditions
- **Performance Under Load**: System behavior with maximum concurrent connections and data volume

### Performance Testing Requirements
- **Latency Testing**: Response times under various network conditions (50ms to 500ms)
- **Throughput Testing**: Data volume handling with maximum subscription updates
- **Concurrent Connection Testing**: System stability with multiple simultaneous connections
- **Memory and CPU Profiling**: Resource usage patterns during extended gameplay sessions