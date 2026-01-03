---
inclusion: always
---

# Project Structure & Naming

## Structure
```
Scripts/
├── Core/      # GameManager, singletons
├── Network/   # SpacetimeDB client integration  
├── Data/      # DTOs, data structures
├── Audio/     # Audio systems
├── Visual/    # UI, effects, rendering
└── Test/      # Client testing
```

## Naming (GDScript)
- **Classes**: `PascalCase` 
- **Methods/Signals**: `snake_case`
- **Constants**: `UPPER_SNAKE_CASE`
- **Files**: `PascalCase.gd`

## Core Principles
- **Network**: SpacetimeDB client communication only
- **Input**: Send inputs to server, never move entities directly
- **Rendering**: Visual representation of server state
- **State Sync**: Receive and apply server updates

## Data Flow
```
User Input → NetworkManager → SpacetimeDB → Server State → Visual Update
```