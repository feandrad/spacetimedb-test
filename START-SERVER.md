# Starting SpacetimeDB Server

## The Issue

You're trying to publish to a server that isn't running. SpacetimeDB needs a standalone server process running before you can publish modules to it.

## Solution: Start SpacetimeDB Standalone Server

### Option 1: Start Server in Standalone Mode (Recommended for Development)

```bash
# Start SpacetimeDB server on port 7734
spacetime start --listen-addr 0.0.0.0:7734
```

This will:
- Start the SpacetimeDB server
- Listen on port 7734
- Keep running in the foreground
- Show logs in real-time

**Keep this terminal open** - the server needs to stay running.

### Option 2: Start Server in Background

```bash
# Start server in background
spacetime start --listen-addr 0.0.0.0:7734 &

# Or use nohup to keep it running after terminal closes
nohup spacetime start --listen-addr 0.0.0.0:7734 > spacetime.log 2>&1 &
```

### Option 3: Use Docker (Alternative)

```bash
# Pull SpacetimeDB Docker image
docker pull clockworklabs/spacetimedb:latest

# Run SpacetimeDB in Docker
docker run -d \
  --name spacetimedb \
  -p 7734:7734 \
  clockworklabs/spacetimedb:latest \
  start --listen-addr 0.0.0.0:7734
```

## Complete Workflow

### 1. Start the Server

In **Terminal 1** (keep this running):
```bash
spacetime start --listen-addr 0.0.0.0:7734
```

You should see output like:
```
Starting SpacetimeDB server...
Listening on 0.0.0.0:7734
Server ready
```

### 2. Publish Your Module

In **Terminal 2** (new terminal):
```bash
cd path/to/guildmaster-server
spacetime publish guildmaster --server http://127.0.0.1:7734
```

You should see:
```
Build finished successfully.
Uploading to http://127.0.0.1:7734
Module published successfully
```

### 3. Verify Module is Running

```bash
# List databases
spacetime list --server http://127.0.0.1:7734

# Check logs
spacetime logs guildmaster --server http://127.0.0.1:7734
```

### 4. Test Connection from Godot

Now your Godot client can connect to `http://localhost:7734`

## Troubleshooting

### "Connection refused" Error

**Problem**: Server isn't running
**Solution**: Start the server with `spacetime start --listen-addr 0.0.0.0:7734`

### "Port already in use" Error

**Problem**: Something else is using port 7734
**Solution**: 
```bash
# Find what's using the port
lsof -i :7734

# Kill the process or use a different port
spacetime start --listen-addr 0.0.0.0:7735
```

### Server Stops When Terminal Closes

**Problem**: Server runs in foreground
**Solution**: Use background mode or Docker

### Can't Find spacetime Command

**Problem**: SpacetimeDB CLI not installed
**Solution**: 
```bash
# Install SpacetimeDB CLI
curl --proto '=https' --tlsv1.2 -sSf https://install.spacetimedb.com | sh

# Or via Homebrew (macOS)
brew install spacetimedb
```

## Server Management Commands

```bash
# Start server
spacetime start --listen-addr 0.0.0.0:7734

# Stop server (if running in background)
pkill spacetime

# Check if server is running
curl http://localhost:7734/

# View server logs (after publishing module)
spacetime logs guildmaster --server http://127.0.0.1:7734

# List all databases
spacetime list --server http://127.0.0.1:7734

# Delete a database
spacetime delete guildmaster --server http://127.0.0.1:7734
```

## Development Workflow

### Recommended Setup

**Terminal 1 - Server:**
```bash
spacetime start --listen-addr 0.0.0.0:7734
```
Keep this running during development.

**Terminal 2 - Development:**
```bash
cd guildmaster-server

# Make changes to your Rust code
# Then rebuild and republish:
spacetime publish guildmaster --server http://127.0.0.1:7734 --delete-data
```

**Terminal 3 - Logs:**
```bash
# Watch logs in real-time
spacetime logs guildmaster --server http://127.0.0.1:7734 --follow
```

**Godot:**
Run your game - it will connect to the server on port 7734

## Quick Reference

| Command | Purpose |
|---------|---------|
| `spacetime start --listen-addr 0.0.0.0:7734` | Start server |
| `spacetime publish guildmaster --server http://127.0.0.1:7734` | Publish module |
| `spacetime logs guildmaster --server http://127.0.0.1:7734` | View logs |
| `spacetime list --server http://127.0.0.1:7734` | List databases |
| `curl http://localhost:7734/` | Test if server is running |

## Next Steps

1. **Start the server** in Terminal 1
2. **Publish your module** in Terminal 2
3. **Run the Godot test** (`Scenes/ConnectionTest.tscn`)
4. **Verify connection** - should see "Connected!" in Godot

The client is ready - you just need the server running!
