## Goals

Enable the server to process JSON-based chat messages received from connected clients and broadcast them to all **other** connected players (excluding the sender). The message must include both a **trusted identifier** and a **display name** in the broadcast format.

## Description

Clients send chat messages to the server in JSON format. The server validates, sanitizes, and repackages each message with an `author` object containing the sender's `playerId` (UUID) and their display `name`. The message is then broadcasted to all other clients via TCP.

## Expected incoming message format (from client)

```json
{
  "type": "chat",
  "message": "Hello everyone!"
}
```

## Outgoing broadcast format (to other clients)

```json
{
  "type": "chat",
  "author": {
    "playerId": "9f3c1d40-1d2b-11ee-be56-0242ac120002",
    "name": "Alice"
  },
  "message": "Hello everyone!"
}
```

- The `author` object is created by the server using the session data — the client **must not** supply this.
- The `message` field must be sanitized by the server before processing or broadcasting.

## Acceptance criteria

- The server receives a JSON message over TCP.
- The message is parsed and validated:
    - `"type"` must be `"chat"`.
    - `"message"` must be a non-empty string after trimming.
- The server sanitizes the message:
    - Trims whitespace.
    - Removes control characters (e.g., `\n`, `\r`, `\t`).
    - Truncates to 255 characters and appends an ellipsis (`…`) if needed.
- The server constructs a new broadcast message with:
    - `"type": "chat"`
    - `"author"` object containing:
        - `"playerId"`: the UUID of the sender (from session)
        - `"name"`: the display name of the sender (from session)
    - `"message"`: the cleaned message string
- The message is sent to all connected players except the sender.

## Steps

1. Parse the incoming TCP JSON message.
2. Validate that `"type"` is `"chat"` and `"message"` is a string.
3. Sanitize the message:
    - Trim.
    - Strip line breaks and control characters.
    - Truncate to 256 characters (with `…` if necessary).
4. Retrieve the sender's `playerId` and `name` from the session.
5. Construct the final broadcast JSON with `type`, `author`, and `message`.
6. Loop through all active sessions:
    - Skip the sender.
    - Send the message over TCP.
7. Handle errors gracefully (e.g., disconnected clients, send failures).

## Technical considerations

- The `author` object should be reusable in other future features (e.g., private messages, logging).
- Never allow clients to supply or override their own `playerId` or `name`.
- Broadcasting should be thread-safe and should not block the main loop.
- Consider rate-limiting or filtering spammy content at this level.
- Ensure encoding and TCP framing are consistent with the rest of the protocol.
