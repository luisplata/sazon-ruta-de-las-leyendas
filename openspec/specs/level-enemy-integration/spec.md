# level-enemy-integration Specification

## Requirements

### Requirement: Per-Level Enemy Prefab Is Data-Driven

`LevelConfig` SHALL expose a serialized `enemyPrefab` reference so each level declares its own enemy visual, defaulting to the Level 1 Ahuizotl prefab (`Creature1.prefab`). The system SHALL NOT hardcode a specific enemy prefab in code.

**Assumption**: `LevelConfig` is the per-level data source for the gameplay scene; no new ScriptableObject type is introduced.

#### Scenario: Level declares its enemy prefab

- GIVEN Level 1's `LevelConfig` with `enemyPrefab` set to the Ahuizotl prefab
- WHEN the level config is inspected
- THEN `enemyPrefab` resolves to the Ahuizotl prefab without any code change

#### Scenario: Empty prefab keeps the placeholder

- GIVEN a `LevelConfig` whose `enemyPrefab` is unassigned
- WHEN the level starts
- THEN the existing placeholder enemy is shown (current behavior preserved)
- AND no exception is raised

### Requirement: Enemy Instantiated Under Scene Anchor At Game Start

At game start the system SHALL instantiate the level's configured `enemyPrefab` as a child of the existing scene `Enemy` anchor, with its local transform reset so placement is deterministic relative to that anchor. Any previous instance SHALL be cleared before spawning.

**Assumption**: the exact transform/scale contract is finalized in design; this spec only requires deterministic placement under the anchor.

#### Scenario: Prefab spawned under the anchor

- GIVEN a level with a configured `enemyPrefab` and a scene `Enemy` anchor
- WHEN the game starts
- THEN one instance of the prefab exists as a child of the `Enemy` anchor
- AND the placeholder `SpriteRenderer` is hidden while the prefab instance is active

#### Scenario: No duplicate instance

- GIVEN a prefab enemy is already instantiated
- WHEN the start path runs again
- THEN exactly one prefab instance exists under the anchor

### Requirement: Prefab Animator Wired Into EnemyView

The system SHALL locate the `Animator` on the instantiated prefab and assign it to `EnemyView` before any reaction can play, without changing `EnemyView`'s reaction or phrase-bubble behavior.

#### Scenario: Animator available for reactions

- GIVEN a spawned prefab containing an `Animator`
- WHEN a reaction is played
- THEN `EnemyView` targets the spawned prefab's `Animator`

### Requirement: Enemy Swappable Without Code Changes

Changing the configured `enemyPrefab` SHALL change the displayed enemy with no code modification, proving Level 2 readiness.

#### Scenario: Level 2 swap

- GIVEN a second enemy prefab assigned to another level's `LevelConfig`
- WHEN that level starts
- THEN the second prefab is instantiated as the enemy
- AND `EnemyView` is wired to its `Animator` without source changes

### Requirement: Missing Animator Triggers Are Tolerated

When the prefab's `Animator` lacks a reaction trigger (`Win`, `Patience`, `Bite`, `Eaten`), `EnemyView` SHALL NOT throw and SHALL NOT log a console error/warning for the missing parameter. The reaction's phrase bubble SHALL still display, and a missing trigger SHALL be a no-op on the animator (state left unchanged).

**Assumption**: reaction-capable controllers may be authored later; this requirement covers the interim tolerant path.

#### Scenario: Trigger absent

- GIVEN an `Animator` exposing none of the reaction triggers
- WHEN `EnemyView.PlayReaction(Win)` is called
- THEN no exception is thrown and no console error is logged
- AND the animator state is left unchanged
- AND the corresponding phrase is still displayed

#### Scenario: Trigger present still plays

- GIVEN an `Animator` exposing the `Win` trigger
- WHEN `EnemyView.PlayReaction(Win)` is called
- THEN the `Win` trigger is set

### Requirement: Enemy Rules Unchanged

This change SHALL NOT alter enemy gameplay rules: lives, patience, and win/lose conditions SHALL behave exactly as before.

#### Scenario: Rules preserved

- GIVEN the game running with a prefab enemy
- WHEN a cook yields Win, patience loss, Bite, or Eaten
- THEN lives, patience, and win/lose outcomes match pre-change behavior
