# Guildmaster MVP - Project Memory

## Architecture
- **Server**: Rust + SpacetimeDB (v1.11.2), compiled to WASM (`cdylib`)
- **Client**: C# + Raylib-cs, .NET 10.0, uses SpacetimeDB Client SDK
- **Protocol**: SpacetimeDB reducers (intents) + subscriptions (state) over WebSocket/BSATN
- **Docs**: `GDD/` directory (GDD.md, Multiplayer and Maps, Combat and Movement, Controls)

## Server Structure (`server/src/`)
- `lib.rs` - Main entry, Player table, lifecycle (connect/disconnect), registration, auth
- `map.rs` - Map system, templates, transitions, instances, spawn points
- `movement.rs` - Movement validation and processing
- `combat.rs` - Combat system
- `character.rs` - Character logic
- `inventory.rs` - Inventory management
- `resource_registry.rs` - Resource registry
- Maps stored as CSV in `server/src/maps/` (tavern_inside, tavern_outside, mountains_entrance)
- Transition data in `server/src/transitions/`

## Client Structure (`client/`)
- `Program.cs` - Entry point
- `Core/Game.cs` - Main game class
- `Core/ECS/` - Entity Component System (Entity, Component, GameWorld, ISystem)
- `Core/Components/` - PlayerComponent, EnemyComponent, PositionComponent, RenderComponent
- `Core/Systems/` - NetworkSystem, InputSystem, MapSystem, RenderSystem, SyncSystem
- `Core/GameState.cs` - Game state management
- `Network/GuildmasterClient.cs` - SpacetimeDB client wrapper
- `Generated/` - Auto-generated SpacetimeDB tables/types/reducers
- `Input/` - Input handling (InputSystem, RaylibInputService, CharacterAction)
- `World/TransitionZones.cs` - Map transition zones
- `Repository/PlayerRepository.cs` - Player data access
- `Assets/` - Tiled maps (.tmx), tilesets (.tsx), sprites (.png)

## Key Constants
- Starting map: defined as `STARTING_MAP` in map.rs
- Player struct has: id, username, identity, position, velocity, current_map_id, health, is_downed

## Build & Run
- Server: `cd server && spacetime publish guildmaster`
- Client: `cd client && dotnet run` or `./run.sh`
- Multi-client: `./run_multiclient.sh`
- SpacetimeDB local: `spacetime start -l 0.0.0.0:7734`
