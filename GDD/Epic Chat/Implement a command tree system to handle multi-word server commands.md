Here’s a complete task for implementing a **Command Tree system** in the server to support multi-word commands:

---

# Task 4 – Implement a command tree system to handle multi-word server commands

## Goals

Replace the current single-command handling logic with a structured **command tree system** to support multi-word commands such as `/admin kick`, `/guild invite`, and `/chat global`. The system must be modular, extensible, and support command chaining via subcommands.

## Description

The server needs a scalable command system capable of handling multiple commands with nested structures. This task introduces a **command tree (trie-style)** architecture, where each command is represented as a node, and subcommands are modeled as child nodes.

This will allow developers to:

- Add new commands without modifying a central dispatch block.
- Build complex commands using multi-word syntax (e.g., `/admin ban player123`).
- Support command introspection for features like help, permissions, and autocompletion in the future.

## Acceptance criteria

- The system must define a `CommandNode` interface or abstract class with:
    - `name`: the keyword this node represents
    - `children`: subcommands (map of name → node)
    - `execute(context: CommandContext)`: method to run logic
- A root dispatcher must support:
    - Registration of command paths (e.g., `["admin", "kick"]`)
    - Dispatching raw command strings to the correct command node
- The input string must be tokenized using whitespace.
- The dispatcher must support case-insensitive matching.
- Intermediate nodes can contain no logic, acting as command namespaces.
- If no matching command is found, print an error message to the terminal.
- If a leaf node is found but the arguments are invalid, it must respond with a usage hint or error.
- Commands are invoked manually via terminal input (e.g., `admin kick Alice`), or can be reused later for in-game chat commands or RPC.

## Steps

1. Define `CommandContext`, which includes:
    - The source of the command (terminal, session, etc.)
    - The full list of arguments passed after the command path
2. Define the `CommandNode` interface:
    - Must include `name`, `children`, and `execute(context)`
3. Implement a root `CommandDispatcher` object:
    - Supports registering command paths (`List<String>`)
    - Dispatches a raw string to the matching command node
    - Splits input on whitespace
    - Ignores leading `/` in commands
4. Create a base `SubcommandNode` class that can hold children but does not execute anything directly.
5. Migrate the current `/chat` handling to a `ChatCommand` node and register it under `["chat"]`.
6. (Optional) Implement a fallback or `HelpCommand` to list available commands if no match is found.
7. Add logging or output for:
    - Unknown commands
    - Invalid arguments
    - Successful command executions

## Technical considerations

- Command matching should be case-insensitive.
- Support quoted strings in the future (e.g., `announce "Server restarting in 5 minutes"`).
- This structure should eventually support permission checks (e.g., admin-only commands).
- Commands should be unit-testable — each node should be modular and isolated.

Let me know if you want the first command (`chat`) scaffolded as an example under this system.