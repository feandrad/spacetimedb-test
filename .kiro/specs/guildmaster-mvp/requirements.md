# Requirements Document

## Introduction

Um MVP (Minimum Viable Product) do Guildmaster focado nos elementos de ação e aventura multiplayer. O jogo deve incluir exploração, combate, navegação entre mapas e inventário, sem os elementos de gerenciamento de taverna. Desenvolvido com servidor autoritativo (SpacetimeDB) e cliente em Godot 4 (C#) com design console-first.

## Glossary

- **Player**: O personagem controlado pelo jogador
- **Game_World**: O ambiente de jogo composto por múltiplos mapas conectados
- **Map_System**: Sistema que gerencia navegação e transições entre mapas
- **Combat_System**: Sistema que gerencia todas as interações de combate
- **Movement_System**: Sistema que controla movimentação e input prediction
- **Inventory_System**: Sistema que gerencia itens e equipamentos do jogador
- **Input_Manager**: Sistema que mapeia ações do jogador para controles customizáveis
- **Server**: Servidor autoritativo que valida todas as ações
- **Enemy**: Entidades hostis controladas por IA
- **Weapon**: Equipamentos de combate (espada, machado, arco)
- **Projectile**: Projéteis disparados por armas à distância

## Requirements

### Requirement 1: Player Movement and Input System

**User Story:** Como jogador, quero controlar meu personagem com movimentação suave e controles customizáveis, para que eu possa explorar o mundo de forma intuitiva com controle ou teclado.

#### Acceptance Criteria

1. THE Input_Manager SHALL provide a remappable interface for all player actions
2. WHEN the player presses movement inputs, THE Movement_System SHALL move the Player smoothly in the corresponding direction
3. THE Movement_System SHALL support both WASD/arrow keys and gamepad analog stick input with console-first design philosophy
4. WHEN the player releases movement inputs, THE Movement_System SHALL stop the Player smoothly
5. THE Server SHALL validate all movement inputs and provide authoritative position updates
6. THE Movement_System SHALL implement client-side prediction with server reconciliation
7. WHEN movement conflicts occur, THE Movement_System SHALL smoothly correct client position to match server state

### Requirement 2: Map System and Navigation

**User Story:** Como jogador, quero navegar entre diferentes mapas conectados, para que eu possa explorar um mundo maior e mais interessante.

#### Acceptance Criteria

1. THE Map_System SHALL support navigation between connected map areas
2. WHEN a player approaches a map transition zone, THE Map_System SHALL initiate a transition to the connected map
3. THE Map_System SHALL maintain player position and state during map transitions
4. WHEN multiple players are in the same map, THE Map_System SHALL synchronize their positions and actions
5. THE Map_System SHALL support both interior and exterior map types
6. WHEN a player transitions to a new map, THE Map_System SHALL load the destination map and spawn the player at the appropriate entry point
7. THE Server SHALL manage map instances and ensure consistent state across all connected players

### Requirement 3: Combat System with Multiple Weapon Types

**User Story:** Como jogador, quero usar diferentes tipos de armas com comportamentos únicos, para que eu possa escolher meu estilo de combate preferido.

#### Acceptance Criteria

1. WHEN the player equips a sword, THE Combat_System SHALL enable wide cleave attacks that hit multiple enemies
2. WHEN the player equips an axe, THE Combat_System SHALL provide higher damage attacks that only hit enemies directly in front
3. WHEN the player equips a bow, THE Combat_System SHALL allow shooting projectiles that consume ammunition
4. THE Combat_System SHALL provide visual telegraph effects for all attack types and hit confirmations
5. WHEN a weapon attack hits an enemy, THE Combat_System SHALL deal appropriate damage based on weapon type and show visual hit effects
6. THE Combat_System SHALL prevent movement during attack animations
7. THE Server SHALL validate all combat actions and damage calculations

### Requirement 4: Projectile and Ammunition System

**User Story:** Como jogador, quero usar armas à distância que consomem munição, para que eu tenha opções táticas de combate.

#### Acceptance Criteria

1. WHEN the player fires a bow, THE Combat_System SHALL create a Projectile that travels in the aimed direction
2. THE Projectile SHALL consume one unit of ammunition from the player's inventory
3. WHEN a Projectile hits an enemy, THE Combat_System SHALL deal damage and remove the Projectile
4. WHEN a Projectile hits a solid obstacle, THE Combat_System SHALL remove the Projectile without dealing damage
5. THE Inventory_System SHALL track ammunition quantities and prevent firing when ammunition is depleted
6. THE Combat_System SHALL provide visual trajectory feedback when aiming projectile weapons

### Requirement 5: Inventory and Equipment System

**User Story:** Como jogador, quero gerenciar meus itens e equipamentos, para que eu possa organizar recursos e trocar equipamentos conforme necessário.

#### Acceptance Criteria

1. THE Inventory_System SHALL provide storage slots for items and equipment
2. WHEN the player picks up an item, THE Inventory_System SHALL add it to available inventory space
3. WHEN the player equips a weapon, THE Inventory_System SHALL update the active weapon and enable its combat behavior
4. THE Inventory_System SHALL track ammunition quantities for projectile weapons
5. WHEN inventory is full, THE Inventory_System SHALL prevent picking up additional items
6. THE Inventory_System SHALL provide a user interface for viewing and managing items
7. THE Server SHALL validate all inventory operations and maintain authoritative item state

### Requirement 6: Contextual Actions and Interactions

**User Story:** Como jogador, quero interagir com objetos no mundo usando ações contextuais, para que eu possa coletar recursos e interagir com o ambiente.

#### Acceptance Criteria

1. WHEN the player is near an interactable object, THE Input_Manager SHALL display available contextual actions
2. WHEN the player is unarmed and near a tree, THE Input_Manager SHALL provide "shake" and "cut" actions
3. WHEN the player is unarmed and near a rock, THE Input_Manager SHALL provide "pick up" and "break" actions
4. WHEN the player performs a contextual action, THE Game_World SHALL execute the appropriate interaction
5. THE Server SHALL validate all contextual actions and update world state accordingly
6. WHEN contextual actions yield items, THE Inventory_System SHALL attempt to add them to the player's inventory

### Requirement 7: Multiplayer Cooperative Gameplay

**User Story:** Como jogador, quero jogar cooperativamente com até 3 amigos, para que possamos explorar e combater juntos.

#### Acceptance Criteria

1. THE Server SHALL support up to 4 players simultaneously in the same game session
2. WHEN multiple players are in the same map, THE Map_System SHALL synchronize all player positions and actions
3. THE Combat_System SHALL disable friendly fire between players
4. THE Combat_System SHALL disable body blocking between players
5. WHEN players are in combat, THE Combat_System SHALL allow cooperative attacks against the same enemies
6. THE Inventory_System SHALL handle individual player inventories separately
7. THE Server SHALL maintain consistent game state for all connected players

### Requirement 8: Enemy AI and Combat Behavior

**User Story:** Como jogador, quero enfrentar inimigos com comportamentos básicos de IA baseados em estados, para que o jogo tenha desafio e interação previsível.

#### Acceptance Criteria

1. THE Enemy SHALL implement a state machine with Idle, Alert, and Chasing states
2. WHEN no players are nearby, THE Enemy SHALL remain in Idle state and patrol in predefined areas
3. WHEN a player enters the enemy's detection range, THE Enemy SHALL transition to Alert state
4. WHEN in Alert state, THE Enemy SHALL investigate the player's last known position
5. WHEN the enemy has line of sight to a player, THE Enemy SHALL transition to Chasing state and move toward the player
6. WHEN an enemy in Chasing state reaches a player, THE Combat_System SHALL deal damage to the player
7. WHEN a player leaves the enemy's leash range, THE Enemy SHALL return to Idle state
8. THE Enemy SHALL have health points and take damage from player weapon attacks
9. WHEN an enemy's health reaches zero, THE Combat_System SHALL remove the enemy and potentially spawn loot
10. THE Server SHALL control all enemy AI behavior and validate combat interactions

### Requirement 9: Health and Damage System

**User Story:** Como jogador, quero ter um sistema de vida que me permita receber dano e me recuperar, para que o jogo tenha consequências e progressão.

#### Acceptance Criteria

1. THE Player SHALL have a health system with maximum health capacity
2. WHEN a player takes damage, THE Combat_System SHALL reduce the player's current health
3. WHEN a player's health reaches zero, THE Combat_System SHALL trigger a downed state
4. WHEN a player is downed, THE Combat_System SHALL allow other players to revive them
5. THE Combat_System SHALL provide temporary invincibility frames after taking damage
6. THE Inventory_System SHALL support consumable items that restore health
7. THE Server SHALL validate all health and damage calculations

### Requirement 10: Visual and Audio Feedback

**User Story:** Como jogador, quero feedback visual e sonoro para minhas ações, para que o jogo seja responsivo e claro.

#### Acceptance Criteria

1. WHEN the player attacks with any weapon, THE Combat_System SHALL display attack telegraph effects and play appropriate sound effects
2. WHEN a weapon attack is about to hit, THE Combat_System SHALL show visual telegraph indicators for the attack area
3. WHEN a weapon hits an enemy, THE Combat_System SHALL provide visual hit effects and audio hit confirmation
4. WHEN the player takes damage, THE Combat_System SHALL display damage effects and temporary visual feedback
5. WHEN enemies are about to attack, THE Combat_System SHALL show telegraph effects to warn players
6. WHEN projectiles are fired, THE Combat_System SHALL display projectile visuals and impact effects
7. THE Game_World SHALL use simple visual effects and animations primarily implemented through shaders (such as item bob animations)
8. THE Combat_System SHALL provide clear visual indicators for attack ranges and hit zones using shader-based effects