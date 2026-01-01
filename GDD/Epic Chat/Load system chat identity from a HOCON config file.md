## Goals

Allow the server to load the `playerId` and `name` used for system-generated messages (such as terminal chat) from a **HOCON config file**, and ensure it is always valid by either generating or validating it at startup.

## Description

System-generated chat messages (like those typed in the server terminal) need a defined `author` identity. This task enables the server to load that identity from a `config.conf` file written in HOCON format.

If the config file does **not exist**, the server will generate one automatically using:

- A **random UUID**
- The name `"System"`

If the config file **does exist** but contains **invalid or malformed fields**, the server will **fail to start**, printing a clear error.

## Expected config format (HOCON)

`config.conf`

```hocon
systemIdentity {
  playerId = 9f3c1d40-1d2b-11ee-be56-0242ac120002
  name = "System"
}
```

## Acceptance criteria

- On server startup, the config file (`config.conf`) is loaded using HOCON.
- If the file does **not exist**:
    - A new `config.conf` is created in the working directory.
    - It includes a random UUID for `playerId` and `"System"` as the name.
    - A message is printed to inform the operator that the file was created.
- If the file **exists but is invalid** (e.g., invalid UUID, missing name):
    - The server prints a detailed error message explaining the issue.
    - The server **exits immediately and does not continue**.
- If the config is valid:
    - The loaded values are stored in memory and used in the `author` field for all system chat messages.

## Steps

1. On server startup, check for existence of `config.conf`.
2. If the file is missing:
    - Generate a new file.
    - Write:
        - `playerId = <randomly generated UUID>`
        - `name = "System"`
    - Print a message like:  
        `"Generated default config.conf with random system identity."`
3. If the file exists:
    - Parse it using `ConfigFactory.parseFile(...)`.
    - Validate:
        - `playerId` is present and a valid UUID.
        - `name` is a string, not blank, and ≤ 64 characters.
4. If validation fails:
    - Print a clear error, e.g.:  
        `"Error: config.conf is missing a valid systemIdentity.playerId (UUID) or name."`
    - Exit with a non-zero code.
5. If valid:
    - Store the data in memory for use when broadcasting terminal messages.

## Technical considerations

- Use `UUID.fromString()` to validate the UUID field.
- When generating a file, use `ConfigRenderOptions.concise()` to write clean HOCON.
- Ensure the config is saved with UTF-8 encoding.
- This task avoids fallback ambiguity and guarantees consistent system identity behavior.
---
