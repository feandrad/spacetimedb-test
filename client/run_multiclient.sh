#!/bin/bash
set -e

if [ $# -eq 0 ]; then
    echo "Usage: ./run_multiclient.sh <number_of_clients>"
    echo "Example: ./run_multiclient.sh 2"
    exit 1
fi

COUNT=$1

echo "Building Guildmaster Client..."
dotnet build

echo "Starting $COUNT client instances..."

for i in $(seq 1 $COUNT); do
    TOKEN_FILE="client_$i.token"
    echo "Launching Client $i (Token File: $TOKEN_FILE)..."
    # Run in background
    dotnet run --no-build -- --token-file "$TOKEN_FILE" &
    
    # Slight delay to avoid binding conflicts if any (though OS usually handles it)
    sleep 1
done

wait
