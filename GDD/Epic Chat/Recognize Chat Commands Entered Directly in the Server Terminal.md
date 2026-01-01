# TASK 1 – Recognize Chat Commands Entered Directly in the Server Terminal

## Goals

Allow the server to recognize and process chat commands entered directly into its own terminal (standard input), simulating a message sent by the server or an admin.

## Description

During development or for admin use, the server operator can type chat messages directly into the terminal where the server is running. These messages are interpreted as global chat and should be broadcast to all connected clients as if sent by the server itself.

The system must recognize chat commands using the following formats:

- `CHAT message...`
- `/chat message...`

The command prefix is **case-insensitive**, and both forms must be treated identically.

## Accepted Input Examples

- `CHAT Hello players!`
- `/chat Maintenance incoming`
- `Chat Welcome`
- `/CHAT The server will restart shortly`

## Acceptance Criteria

- The server continuously reads input from standard input (`stdin`) in a non-blocking thread.
- If the input line starts with `chat` or `/chat` (case-insensitive):
    - The remaining text is treated as the chat message.
- The message is:
    - Trimmed of leading/trailing whitespace.
    - Validated to ensure it's not empty.
    - If the message exceeds 256 characters, it is truncated to 255 characters and an ellipsis character (`…`) is appended, resulting in a final length of 256 characters.
- Valid messages are passed into the broadcast system as if sent by `"from": "Server"`.
- Invalid messages (e.g., empty) trigger a local warning printed to the terminal (e.g., “Message is empty”).

## Steps

1. Start a background thread that reads lines from `System.in`.
2. For each line:
    - Normalize the string to lowercase for command detection.
    - Check if it starts with `chat` or `/chat` (case-insensitive).
3. If a valid command is detected:
    - Extract the substring after the command prefix.
    - Trim whitespace.
    - If the resulting message is empty, print an error and return.
    - If the message exceeds 256 characters:
        - Truncate it to 255 characters.
        - Append a single Unicode ellipsis character (`…`) to make it 256 characters total.
4. Send the message into the internal chat broadcast system as:
    
```json
{
  "type": "chat",
  "author": {
    "playerId": "00000000-0000-0000-0000-000000000000",
    "name": "System"
  },
  "message": "Hello everyone!"
}
```
    
5. Print a confirmation or log the message to the terminal if desired.

## Technical Considerations

- The input reader must not block the main server loop.
- Message parsing must safely handle malformed input (e.g., just `/chat` with no content).
- UTF-8 encoding should be used to support full character sets, including the ellipsis (`…`).
- Avoid letting excessively long or spammy input slow down or flood the system.
- This task should be isolated and easily extendable for supporting future terminal commands like `/kick`, `/shutdown`, etc.