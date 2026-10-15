# Delta for level-background-art

## ADDED Requirements

### Requirement: Level Background Rendered From Level Data

The level's background SHALL be rendered from level data by assigning the `BG.png` sprite (Level 1) to the existing `LevelConfig.background` slot, applied through the existing background path at game start. Swapping per-level background art SHALL require no code changes.

#### Scenario: Level 1 background shows BG.png

- GIVEN `LevelConfig_Level1Mexico.background` set to the `BG.png` sprite
- WHEN the level starts
- THEN the background layer renders `BG.png` un-tinted

#### Scenario: Missing background falls back

- GIVEN a `LevelConfig` with an empty `background`
- WHEN the level starts
- THEN the background layer keeps the placeholder (current behavior)
- AND no exception is raised

### Requirement: Foreground Layer Disabled Until Art Exists

The foreground/front layer SHALL be absent or disabled while its art is missing, and SHALL be enable-able later through configuration/data only (an optional reference or data toggle), requiring no code change.

**Assumption**: "front/foreground" is a decorative layer in front of the characters; its art does not exist yet.

#### Scenario: No foreground art present

- GIVEN the foreground art is not assigned
- WHEN the level runs
- THEN no foreground is rendered
- AND no error is logged

#### Scenario: Enabling later is data-only

- GIVEN the foreground art is assigned to the configured foreground reference
- WHEN the level starts
- THEN the foreground renders in front of characters without source changes

### Requirement: Layer Ordering Not Regressed

Background and foreground SHALL preserve the existing ordering: background behind midground and behind characters; an enabled foreground SHALL render in front of characters.

#### Scenario: Ordering preserved

- GIVEN a level with background, midground, and characters present
- WHEN the frame renders
- THEN the background is behind the midground and the characters
- AND an enabled foreground is in front of the characters
