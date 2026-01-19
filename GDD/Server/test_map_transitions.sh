#!/bin/bash

# Map Transition Test Script
# This script demonstrates the complete map transition workflow

SERVER="http://127.0.0.1:7734"
MODULE="guildmaster"

echo "🎮 Guildmaster Map Transition Test"
echo "=================================="
echo ""

# Step 1: Register player
echo "📝 Step 1: Registering player..."
spacetime call $MODULE register_player --server $SERVER "TestPlayer"
echo ""

# Wait a moment
sleep 1

# Step 2: Check initial position
echo "📍 Step 2: Checking initial position..."
spacetime call $MODULE get_player_position --server $SERVER 3058020705
echo ""

sleep 1

# Step 3: View starting area transition zones
echo "🚪 Step 3: Viewing starting_area transition zones..."
spacetime call $MODULE get_transition_zones --server $SERVER "starting_area"
echo ""

sleep 1

# Step 4: Move player to transition zone
echo "🏃 Step 4: Moving player to transition zone (X:975, Y:500)..."
spacetime call $MODULE update_player_position --server $SERVER 3058020705 975.0 500.0 0.0 0.0 1
echo ""

sleep 1

# Step 5: Check if in transition zone
echo "🔍 Step 5: Checking if player is in transition zone..."
spacetime call $MODULE check_player_transition --server $SERVER 3058020705
echo ""

sleep 1

# Step 6: Transition to forest
echo "🌲 Step 6: Transitioning to forest_area..."
spacetime call $MODULE transition_to_map --server $SERVER 3058020705 "forest_area"
echo ""

sleep 1

# Step 7: Verify new position
echo "📍 Step 7: Verifying new position in forest_area..."
spacetime call $MODULE get_player_position --server $SERVER 3058020705
echo ""

sleep 1

# Step 8: View forest transition zones
echo "🚪 Step 8: Viewing forest_area transition zones..."
spacetime call $MODULE get_transition_zones --server $SERVER "forest_area"
echo ""

sleep 1

# Step 9: Move to forest transition zone
echo "🏃 Step 9: Moving player to forest transition zone (X:25, Y:500)..."
spacetime call $MODULE update_player_position --server $SERVER 3058020705 25.0 500.0 0.0 0.0 2
echo ""

sleep 1

# Step 10: Check transition
echo "🔍 Step 10: Checking if player is in transition zone..."
spacetime call $MODULE check_player_transition --server $SERVER 3058020705
echo ""

sleep 1

# Step 11: Transition back to starting area
echo "🏠 Step 11: Transitioning back to starting_area..."
spacetime call $MODULE transition_to_map --server $SERVER 3058020705 "starting_area"
echo ""

sleep 1

# Step 12: Final position check
echo "📍 Step 12: Final position check..."
spacetime call $MODULE get_player_position --server $SERVER 3058020705
echo ""

sleep 1

# Step 13: View logs
echo "📋 Step 13: Viewing server logs..."
spacetime logs $MODULE --server $SERVER --num-lines 30
echo ""

echo "✅ Test complete!"
echo ""
echo "Summary:"
echo "- Player registered at starting_area (100, 500)"
echo "- Moved to transition zone (975, 500)"
echo "- Transitioned to forest_area (50, 500)"
echo "- Moved to forest transition zone (25, 500)"
echo "- Transitioned back to starting_area (900, 500)"
echo ""
echo "Check the logs above for detailed emoji-tagged output! 🎉"
