---
inclusion: always
---

# Client Debugging

## Essential Logging
```gdscript
# Input logging
func send_input(input_type: String, data):
    print("[CLIENT] Input: ", input_type, " - ", data)
    NetworkManager.send_reducer(input_type, data)

# Connection status
func _on_connection_changed(connected: bool):
    print("[CLIENT] ", "Connected" if connected else "Disconnected")

# Sync corrections
func sync_from_server(server_data):
    var distance = position.distance_to(server_pos)
    if distance > SYNC_THRESHOLD:
        print("[SYNC] Correction: ", distance, "px")
```

## Debug Tools
- Visual overlay showing position, ping, prediction error
- Network status monitor
- FPS/performance counters

## Red Flags
- ❌ Jittery movement (prediction issues)
- ❌ Entity teleporting (sync problems)  
- ❌ Ignored inputs (reducer failures)

## Health Signs
- ✅ Smooth movement with imperceptible corrections
- ✅ Consistent latency < 100ms
- ✅ Identical state across clients