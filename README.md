# Guildmaster MVP

Guildmaster is a multiplayer management simulation and adventure game where players manage a tavern, explore the world, and build their guild. It supports cooperative multiplayer.

## Architecture

*   **Server:** Built with [Rust](https://www.rust-lang.org/) and [SpacetimeDB](https://spacetimedb.com/). The server is authoritative, handling all game logic including movement validation, combat, and inventory management.
*   **Client:** Built with C# and [Raylib-cs](https://github.com/ChrisDill/Raylib-cs).
*   **Protocol:** Communication uses SpacetimeDB's reducers (intents) and subscriptions (state updates) over WebSocket/BSATN.

## Prerequisites

*   **Rust:** [Install Rust](https://www.rust-lang.org/tools/install)
*   **SpacetimeDB CLI:** [Install SpacetimeDB](https://spacetimedb.com/docs/getting-started)
*   **.NET 10.0 SDK:** [Install .NET](https://dotnet.microsoft.com/download)

## Getting Started

### 1. Server Setup

Navigate to the `server/` directory:

```bash
cd server
```

Install the WebAssembly target for Rust (required for SpacetimeDB):

```bash
rustup target add wasm32-unknown-unknown
```

Start the SpacetimeDB local server:

```bash
spacetime start -l 0.0.0.0:7734
```

In a new terminal, publish the server module:

```bash
cd server
spacetime publish guildmaster
```

For more detailed server instructions (including Linux deployment), see [server/README.md](server/README.md).

### 2. Client Setup

Navigate to the `client/` directory:

```bash
cd client
```

Build and run the client:

```bash
dotnet run
```

Or use the provided helper scripts:
*   `./run.sh`: Runs a single client instance.
*   `./run_multiclient.sh`: Runs multiple client instances for testing.

## Project Structure

*   `client/`: C# Raylib game client source code.
*   `server/`: Rust SpacetimeDB server module source code.
*   `GDD/`: Game Design Document and technical documentation.

## Documentation

For more detailed information about the game design and technical implementation, please refer to the documents in the `GDD/` directory:

*   [Game Design Document](GDD/GDD.md)
*   [Multiplayer and Maps](GDD/Multiplayer%20and%20Maps.md)
*   [Combat and Movement](GDD/Combat%20and%20Movement.md)
*   [Controls](GDD/Controls.md)
