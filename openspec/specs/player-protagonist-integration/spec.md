# player-protagonist-integration Specification

## Requirements

### Requirement: Protagonist Prefab Is Configurable

`GameLoopController` SHALL expose a single serialized `playerPrefab` reference (a universal protagonist slot), defaulting to `Abue.prefab`. The system SHALL NOT hardcode a specific protagonist prefab in code.

**Assumption**: the protagonist is identical across all levels, so one scene-level slot is used instead of `LevelConfig`.

#### Scenario: Protagonist swappable without code changes

- GIVEN a different protagonist prefab assigned to `playerPrefab`
- WHEN the game starts
- THEN that prefab is instantiated as the protagonist
- AND `PlayerView` is wired to its `Animator` without source changes

### Requirement: Protagonist Instantiated Under Scene Player Anchor At Game Start

At game start the system SHALL instantiate the configured `playerPrefab` as a child of the existing scene `Player` anchor (`playerView.transform`), with its local transform reset to a deterministic value. Any previous instance SHALL be cleared before spawning so repeated start calls cannot produce a duplicate.

**Assumption**: the exact transform/scale contract (mirroring the enemy anchor) is finalized in design.

#### Scenario: Prefab spawned under the anchor

- GIVEN a configured `playerPrefab` and the scene `Player` anchor
- WHEN the game starts
- THEN one prefab instance exists as a child of `playerView.transform`
- AND the placeholder `SpriteRenderer` is hidden while the prefab instance is active

#### Scenario: No duplicate instance on repeated calls

- GIVEN a protagonist prefab is already instantiated
- WHEN the start path runs again
- THEN exactly one prefab instance exists under the anchor

### Requirement: Prefab Animator Wired Into PlayerView

The system SHALL locate the `Animator` on the instantiated prefab and assign it to `PlayerView` before any player event plays, without changing `PlayerView`'s other behavior.

#### Scenario: Animator available for player events

- GIVEN a spawned protagonist prefab containing an `Animator`
- WHEN a player event is fired
- THEN `PlayerView` targets the spawned prefab's `Animator`

### Requirement: Placeholder Fallback When No Prefab

When `playerPrefab` is unassigned, the system SHALL keep the existing scene placeholder protagonist visible and SHALL NOT raise an exception.

#### Scenario: Empty prefab keeps the placeholder

- GIVEN a `GameLoopController` whose `playerPrefab` is unassigned
- WHEN the game starts
- THEN the current placeholder protagonist is shown (preserved behavior)
- AND no exception is raised

### Requirement: Missing Animator Triggers Are Tolerated

When the protagonist `Animator` lacks some or all of the nine player trigger parameters (`GrabCard`, `ParkCook`, `ParkTrash`, `Cook`, `Discard`, `LosePatience`, `LoseLife`, `Win`, `GameOver`), `PlayerView` SHALL NOT throw and SHALL NOT log a console error or warning for the missing parameter. A missing trigger SHALL be a no-op on the animator, and `ResetToIdle` SHALL be a no-op when the idle state is absent.

**Assumption**: `Abue.controller` exposes zero parameters/transitions today; a tolerant path is required until it is re-authored (out of scope).

#### Scenario: Trigger absent

- GIVEN an `Animator` exposing none of the nine player triggers
- WHEN any `PlayerView` event method is called
- THEN no exception is thrown and no console error/warning is logged
- AND the animator state is left unchanged

#### Scenario: Trigger present still plays

- GIVEN an `Animator` exposing the `Win` trigger
- WHEN `PlayerView.OnWin()` is called
- THEN the `Win` trigger is set

### Requirement: Protagonist Idles By Default

The configured `Abue.controller` SHALL default to the `Abue__Idle` state so the protagonist visibly idle-loops during gameplay without any player event.

#### Scenario: Idle loop visible

- GIVEN `Abue.prefab` instantiated as the protagonist
- WHEN the game runs with no player events
- THEN the protagonist plays the `Abue__Idle` loop

### Requirement: Player Rules Unchanged

This change SHALL NOT alter player gameplay: the semantics of the nine player events and the cook, patience, lives, win, and lose rules SHALL be preserved.

#### Scenario: Rules preserved

- GIVEN the game running with a prefab protagonist
- WHEN any player event resolves a cook, penalty, win, or game-over
- THEN lives, patience, and win/lose outcomes match pre-change behavior
