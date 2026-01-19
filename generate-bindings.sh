#!/bin/bash
# Generate SpacetimeDB C# client bindings from the server module

# Server path (adjust if needed)
SERVER_PATH="../guildmaster-server"

# Output directory for generated bindings
OUTPUT_DIR="Scripts/Network/Generated"

# Check if server exists
if [ ! -d "$SERVER_PATH" ]; then
    echo "❌ Error: Server directory not found at $SERVER_PATH"
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

echo "🔧 Generating C# client bindings..."
echo "   Server: $SERVER_PATH"
echo "   Output: $OUTPUT_DIR"
echo ""

# Generate bindings
spacetime generate \
    --lang csharp \
    --out-dir "$OUTPUT_DIR" \
    --project-path "$SERVER_PATH" \
    --namespace GuildmasterMVP.Network.Generated

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Bindings generated successfully!"
    echo "   Check $OUTPUT_DIR for generated files"
else
    echo ""
    echo "❌ Failed to generate bindings"
    exit 1
fi
