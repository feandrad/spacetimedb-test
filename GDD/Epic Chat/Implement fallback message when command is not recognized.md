## Goals

Ensure that when a user enters an invalid or unrecognized command in the terminal, the system provides a clear fallback message that points them to the existing `help` command.

## Description

The command system should guide users when they type incorrect commands. If a user enters something that doesn’t match any registered command path, the system must display:

```
Command not found. You can type help for a list of available commands.
```

This ensures better usability and ties into the already implemented `help` command, encouraging users to discover valid commands on their own.

## Acceptance criteria

- When a command does not match any registered command path:
    - The fallback message is printed to the terminal or output log.
- The message must be:
    
    ```
    Command not found. You can type help for a list of available commands.
    ```
    
- If a command is matched, execution continues as normal (no fallback message).
- Case-insensitive matching continues to apply during lookup.

## Steps

1. In the command dispatcher logic:
    - After parsing and attempting to match the input against registered command nodes,
    - If no matching path exists, print the fallback message.
2. Ensure partial command matches (e.g., `admin` but no `admin ban`) still result in fallback if the final node isn’t executable.
3. Do not display the fallback if the command is empty or just whitespace (optional).

## Technical considerations

- Make sure this fallback does not interfere with custom error messages inside command nodes (e.g., usage errors).
- Optional: log unknown command attempts for audit/debug purposes.