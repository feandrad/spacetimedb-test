# Implementation Plan: Guildmaster MVP

## Overview

Este plano implementa o Guildmaster MVP com foco em sistemas fundamentais de ação/aventura multiplayer. A implementação segue uma abordagem incremental, construindo primeiro os sistemas base (input, movimento) e progredindo para sistemas mais complexos (combate, IA). O servidor é implementado em Rust (SpacetimeDB) e o cliente em C# (Godot 4). Toda detecção de colisão é processada pelo servidor SpacetimeDB.

## Tasks

- [ ] 1. Setup project structure and core systems
  - Create Godot 4 (C#) client project structure
  - Setup SpacetimeDB server project (Rust)
  - Configure SpacetimeDB SDK communication between Rust server and C# client
  - _Requirements: 1.1, 1.5_

- [ ]* 1.1 Write property test for project setup
  - **Property 1: Movement Direction Consistency**
  - **Validates: Requirements 1.2**

- [ ] 2. Implement Input Manager with remappable controls
  - [ ] 2.1 Create IInputManager interface and implementation
    - Implement console-first input system with WASD/gamepad support
    - Create action mapping system for remappable controls
    - _Requirements: 1.1, 1.3_

  - [ ]* 2.2 Write property test for input consistency
    - **Property 2: Movement Stop on Input Release**
    - **Validates: Requirements 1.4**

- [ ] 3. Implement Movement System with server authority
  - [ ] 3.1 Create client-side movement with input prediction
    - Implement IMovementSystem interface
    - Add client-side prediction for responsive movement
    - _Requirements: 1.2, 1.4, 1.6_

  - [ ] 3.2 Create SpacetimeDB reducers for movement validation (Rust)
    - Implement server-side movement validation and collision detection in Rust
    - Add position reconciliation system using SpacetimeDB reducers
    - _Requirements: 1.5, 1.7_

  - [ ]* 3.3 Write property test for movement direction
    - **Property 1: Movement Direction Consistency**
    - **Validates: Requirements 1.2**

  - [ ]* 3.4 Write property test for position reconciliation
    - **Property 3: Client-Server Position Reconciliation**
    - **Validates: Requirements 1.7**

- [ ] 4. Checkpoint - Basic movement working
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Implement Map System and navigation
  - [ ] 5.1 Create map data structures and registry
    - Implement MapData, TransitionZone structures
    - Create map loading and instance management
    - _Requirements: 2.1, 2.5_

  - [ ] 5.2 Implement map transitions and spawning (Rust server + C# client)
    - Add transition zone detection (server-side Rust logic)
    - Implement player spawning at entry points
    - _Requirements: 2.2, 2.6_

  - [ ]* 5.3 Write property test for map transitions
    - **Property 4: Map Transition Activation**
    - **Validates: Requirements 2.2**

  - [ ]* 5.4 Write property test for state preservation
    - **Property 5: State Preservation During Transitions**
    - **Validates: Requirements 2.3**

  - [ ] 5.5 Implement multiplayer map synchronization
    - Add player position synchronization across maps
    - Ensure consistent state for all connected players
    - _Requirements: 2.4, 2.7_

  - [ ]* 5.6 Write property test for multiplayer sync
    - **Property 6: Multiplayer Position Synchronization**
    - **Validates: Requirements 2.4**

- [ ] 6. Implement basic Combat System
  - [ ] 6.1 Create weapon data structures and interfaces
    - Implement WeaponData, WeaponType enum
    - Create ICombatSystem interface
    - _Requirements: 3.1, 3.2, 3.3_

  - [ ] 6.2 Implement sword combat with cleave attacks (Rust server logic)
    - Add wide cleave attack logic (server-side collision detection in Rust)
    - Implement attack animation prevention of movement
    - _Requirements: 3.1, 3.6_

  - [ ]* 6.3 Write property test for sword cleave
    - **Property 8: Sword Cleave Attack Behavior**
    - **Validates: Requirements 3.1**

  - [ ] 6.4 Implement axe combat with frontal attacks
    - Add high-damage frontal attack logic
    - Ensure only enemies directly in front take damage
    - _Requirements: 3.2_

  - [ ]* 6.5 Write property test for axe attacks
    - **Property 9: Axe Frontal Attack Behavior**
    - **Validates: Requirements 3.2**

  - [ ]* 6.6 Write property test for weapon damage
    - **Property 11: Weapon Damage Application**
    - **Validates: Requirements 3.5**

- [ ] 7. Implement Projectile System
  - [ ] 7.1 Create projectile data structures
    - Implement ProjectileData structure
    - Add projectile creation and management
    - _Requirements: 4.1, 4.3, 4.4_

  - [ ] 7.2 Implement bow combat with ammunition (Rust server + C# client)
    - Add projectile firing with ammo consumption (Rust server logic)
    - Implement server-side projectile collision detection in Rust
    - _Requirements: 3.3, 4.1, 4.2_

  - [ ]* 7.3 Write property test for projectile creation
    - **Property 13: Projectile Direction and Creation**
    - **Validates: Requirements 4.1**

  - [ ]* 7.4 Write property test for ammunition consumption
    - **Property 14: Ammunition Consumption**
    - **Validates: Requirements 4.2**

  - [ ]* 7.5 Write property test for projectile collisions
    - **Property 15: Projectile Enemy Hit Behavior**
    - **Validates: Requirements 4.3**

- [ ] 8. Checkpoint - Combat systems working
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Implement Inventory System with equipment tracking
  - [ ] 9.1 Create inventory data structures with equipment slots
    - Implement player inventory with slots and equipment tracking
    - Add weapon and tool equipping system
    - Add equipment validation for contextual actions
    - _Requirements: 5.1, 5.4_

  - [ ] 9.2 Implement item pickup and equipment management
    - Add item pickup with inventory space validation
    - Implement equipment system with weapon/tool switching
    - Add requirement validation for equipped items
    - _Requirements: 5.2, 5.3, 5.5_

  - [ ]* 9.3 Write property test for item pickup
    - **Property 18: Item Pickup Addition**
    - **Validates: Requirements 5.2**

  - [ ]* 9.4 Write property test for weapon equipping
    - **Property 19: Weapon Equipping Behavior**
    - **Validates: Requirements 5.3**

  - [ ]* 9.5 Write property test for inventory limits
    - **Property 20: Full Inventory Prevention**
    - **Validates: Requirements 5.5**

- [ ] 10. Implement Contextual Actions System with requirements
  - [ ] 10.1 Create IInteractableObject interface with requirement system
    - Implement IInteractableObject interface with action discovery and requirements
    - Create ActionRequirement system for equipment and item prerequisites
    - Add InteractionManager with requirement validation
    - _Requirements: 6.1, 6.4_

  - [ ] 10.2 Implement specific object types with requirements
    - Create TreeObject with axe requirement for cutting
    - Create RockObject with pickaxe requirement for breaking
    - Add requirement validation and user feedback
    - _Requirements: 6.2, 6.3_

  - [ ] 10.3 Implement server-side interaction validation (Rust)
    - Add SpacetimeDB reducers for contextual action execution
    - Validate interaction range, object state, and equipment requirements server-side
    - Handle item generation and object state changes
    - _Requirements: 6.5, 6.6_

  - [ ]* 10.4 Write unit tests for requirement validation
    - Test TreeObject actions with and without axe equipped
    - Test RockObject actions with and without pickaxe equipped
    - Test requirement validation edge cases
    - _Requirements: 6.2, 6.3_

  - [ ]* 10.5 Write property test for contextual action system
    - **Property 21: Contextual Action Execution**
    - **Validates: Requirements 6.4**

- [ ] 11. Implement Enemy AI System
  - [ ] 11.1 Create enemy data structures and state machine
    - Implement EnemyData structure with state machine
    - Add basic enemy spawning and management
    - _Requirements: 8.1, 8.8_

  - [ ] 11.2 Implement enemy AI states (Idle, Alert, Chasing)
    - Add Idle state with patrol behavior
    - Implement Alert state with investigation
    - Add Chasing state with player pursuit
    - _Requirements: 8.2, 8.3, 8.4, 8.5, 8.7_

  - [ ]* 11.3 Write property test for enemy idle behavior
    - **Property 27: Enemy Idle State Behavior**
    - **Validates: Requirements 8.2**

  - [ ]* 11.4 Write property test for enemy state transitions
    - **Property 28: Enemy Alert State Transition**
    - **Validates: Requirements 8.3**

  - [ ] 11.3 Implement enemy combat behavior
    - Add enemy damage dealing to players
    - Implement enemy health and damage from players
    - _Requirements: 8.6, 8.8, 8.9_

  - [ ]* 11.5 Write property test for enemy combat
    - **Property 31: Enemy Damage Dealing**
    - **Validates: Requirements 8.6**

- [ ] 12. Implement Health and Damage System
  - [ ] 12.1 Create player health system
    - Implement health tracking and damage application
    - Add downed state and revival mechanics
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [ ] 12.2 Implement invincibility frames and consumables
    - Add temporary invincibility after damage
    - Implement health restoration consumables
    - _Requirements: 9.5, 9.6_

  - [ ]* 12.3 Write property test for damage application
    - **Property 35: Player Damage Application**
    - **Validates: Requirements 9.2**

  - [ ]* 12.4 Write property test for downed state
    - **Property 36: Player Downed State Trigger**
    - **Validates: Requirements 9.3**

- [ ] 13. Implement Multiplayer Features
  - [ ] 13.1 Add cooperative gameplay features
    - Disable friendly fire between players
    - Disable body blocking between players
    - Enable cooperative enemy attacks
    - _Requirements: 7.3, 7.4, 7.5_

  - [ ] 13.2 Implement individual player systems
    - Ensure separate inventories per player
    - Add player revival mechanics
    - _Requirements: 7.6, 9.4_

  - [ ]* 13.3 Write property test for friendly fire prevention
    - **Property 23: Friendly Fire Prevention**
    - **Validates: Requirements 7.3**

  - [ ]* 13.4 Write property test for cooperative combat
    - **Property 25: Cooperative Enemy Attacks**
    - **Validates: Requirements 7.5**

- [ ] 14. Implement Visual and Audio Feedback
  - [ ] 14.1 Create shader-based visual effects
    - Implement attack telegraph effects using shaders
    - Add hit confirmation and damage feedback effects
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

  - [ ] 14.2 Add simple animations and audio
    - Implement item bob animations with shaders
    - Add sound effects for combat and interactions
    - _Requirements: 10.5, 10.6, 10.7, 10.8_

- [ ] 15. Final integration and testing
  - [ ] 15.1 Integrate all systems and object interactions
    - Connect all systems together including contextual action system
    - Ensure proper communication between client IInteractableObject implementations and server validation
    - Test complete gameplay flow with object interactions
    - _Requirements: All_

  - [ ]* 15.2 Write integration tests
    - Test full gameplay scenarios
    - Test multiplayer synchronization
    - _Requirements: All_

- [ ] 16. Final checkpoint - Complete system working
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- All collision detection is performed server-side by SpacetimeDB (Rust)
- Visual effects are implemented primarily through shaders to avoid complex spritesheets
- Server logic is implemented in Rust using SpacetimeDB reducers and tables
- Client logic is implemented in C# using Godot 4 with SpacetimeDB SDK
- Communication is handled by SpacetimeDB SDK (abstracts WebSocket/BSATN)