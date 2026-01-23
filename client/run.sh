#!/bin/bash
set -e

echo "Building Guildmaster Client..."
dotnet build

echo "Starting Guildmaster Client..."
dotnet run
