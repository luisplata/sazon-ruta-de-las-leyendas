# Delta for gameplay-hud-art

## Purpose

Both gameplay scenes SHALL render the `Escene UI` slices as a static HUD art layer matching the
mockup; presentation only, no runtime rule or HUD data changes.

## ADDED Requirements

### Requirement: HUDArt Container In Both Scenes (static)

Each gameplay scene SHALL contain exactly one `HUDArt` RectTransform, a direct child of `Canvas`,
holding the whole art cluster. It SHALL sit immediately before `FadeOverlay`, which SHALL stay the
last Canvas child.

#### Scenario: Present in both scenes

- GIVEN either gameplay scene
- WHEN Canvas children are inspected
- THEN exactly one `HUDArt` exists as a direct Canvas child
- AND every new art object is its descendant

#### Scenario: Ordered before the fade

- GIVEN a scene's Canvas child list
- WHEN ordered
- THEN `HUDArt` immediately precedes `FadeOverlay`

#### Scenario: Fade covers art (visual)

- GIVEN Play mode with the round ending
- WHEN the black overlay rises
- THEN every `HUDArt` element is fully covered

### Requirement: Nothing Parented Under TopBar (static)

No `HUDArt` element SHALL be parented to `TopBar` or its descendants, since its
`HorizontalLayoutGroup` (`ChildForceExpandWidth = 1`) reflows siblings. `TopBar`'s direct-child
count, order and `sizeDelta.x` SHALL stay unchanged.

#### Scenario: TopBar untouched

- GIVEN `TopBar`
- WHEN children and layout are inspected
- THEN count, order and `sizeDelta.x` match pre-change values
- AND no `HUDArt` element is a descendant

### Requirement: Element-To-Slice Mapping (static)

Each element SHALL use its mapped slice at native `sizeDelta` with `preserveAspect: 1`:

| Element | Slice(s) | Placement |
|---|---|---|
| Lives hats ×3 | `_0` | top-left, stacked |
| Ingredient stone | `_6` | top-centre, below the TopBar strip |
| Patience frame + cap | `_5`, `_1` | top-right; no fill |
| Exit cluster | `_12` behind `_4` | bottom-left |
| Bottom-right cluster | `_2` behind `_8`, `_7` | bottom-right |
| Refill glyph | `_9`/`_10` | existing `RefillButton.Image` |

#### Scenario: Top strip

- GIVEN either scene
- WHEN HUD art is inspected
- THEN three `_0` hats render top-left as lives
- AND `_6` renders top-centre
- AND `_5`+`_1` render top-right with no fill graphic
- AND neither `_3` nor `_11` is used

#### Scenario: Bottom clusters and refill

- GIVEN either scene
- WHEN HUD art is inspected
- THEN `_12` renders behind `_4` at bottom-left
- AND `_2` renders behind `_8` and `_7` at bottom-right
- AND `RefillButton.Image` uses a `_9`/`_10` slice, with no reflow

### Requirement: Aspect-Preserving Placement (visual)

Non-square slices `_2`, `_5`, `_12`, `_1` SHALL stay undistorted at 16:9 and 21:9 via corner/centre
anchors and native `sizeDelta`.

#### Scenario: 16:9 reference

- GIVEN 1920×1080 Play mode
- WHEN a screenshot is captured
- THEN corner clusters are pinned
- AND no non-square slice is distorted or clipped

#### Scenario: Ultrawide

- GIVEN 2560×1080 Play mode
- WHEN a screenshot is captured
- THEN elements stay aspect-correct and on-screen
- AND none overlaps the TopBar labels or centre content

### Requirement: Presentation-Only (mixed)

No C# script, ScriptableObject or HUD data flow SHALL change. `VIDAS`, `PACIENCIA`, `MONEDAS` and
refills SHALL keep real `GameLoopController`/`GameLoopConfig`/`HandController` values.

#### Scenario: Scene-only diff

- GIVEN the change's diff
- WHEN reviewed
- THEN only the two scene files change, plus an optional `HUDArt` prefab
- AND no `.cs`, `.asset` or sprite `.meta` changes

#### Scenario: Live values and refill intact

- GIVEN Play mode where values change and refills remain
- WHEN the HUD updates and `RefillButton` is clicked
- THEN labels show current runtime values
- AND `HandController.Refill` runs as before
- AND the console reports no new errors

### Requirement: Behaviourless Elements Are Decoration (static)

The stone (`_6`), exit X (`_4`), back (`_8`) and book/New (`_7`) SHALL be decoration:
no click behaviour, no scene load, no script reference.

#### Scenario: No wiring

- GIVEN either scene
- WHEN inspected
- THEN those elements have no active `onClick`
