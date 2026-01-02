# Guildmaster Server (SpacetimeDB)

## Status
The SpacetimeDB server setup is in progress. The current implementation has some compatibility issues with SpacetimeDB 0.11 API that need to be resolved.

## Structure
- `src/lib.rs`: Main server entry point with Player table and basic reducers
- `src/player.rs`: Player management reducers
- `src/movement.rs`: Movement validation reducers
- `src/combat.rs`: Combat system reducers
- `src/map.rs`: Map transition reducers
- `src/inventory.rs`: Inventory management reducers

## Next Steps
1. Resolve SpacetimeDB API compatibility issues
2. Implement proper table definitions
3. Add reducer implementations
4. Test server deployment

## Notes
The server is designed to be authoritative for all game logic including:
- Player movement validation
- Combat hit detection
- Inventory management
- Map transitions
- Collision detection

All client actions are validated server-side to prevent cheating and ensure consistency across multiplayer sessions.