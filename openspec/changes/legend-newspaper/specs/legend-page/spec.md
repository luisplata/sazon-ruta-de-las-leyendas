# Delta for legend-page

## ADDED Requirements

### Requirement: LegendPage Model

`LegendPage` SHALL be a `ScriptableObject` exposing exactly 13 independently authored string
fields: `masthead`, `dateline`, `headline`, `subhead`, `reportHeader`, `reportBody`,
`demandsHeader`, `adviceHeader`, `adviceBody`, `sidebarBlock1`, `sidebarBlock2`, `sidebarBlock3`,
`footer`. All fields SHALL be authorable in the Unity Inspector without code. The page SHALL NOT
store `demandsList` (generated).

#### Scenario: Authorable asset

- GIVEN a new `LegendPage` asset created in the Inspector
- WHEN its fields are inspected
- THEN all 13 fields are visible and editable
- AND no field requires code to set

#### Scenario: demandsList not stored

- GIVEN a `LegendPage` asset
- WHEN its serialized fields are inspected
- THEN no authored demand-list field exists

### Requirement: LevelConfig Wiring

Each `LevelConfig` SHALL expose a `LegendPage legendPage` reference. The three assets
`LevelConfig_Level0Llorona`, `LevelConfig_Level1Mexico` and `LevelConfig_Level2Colombia` SHALL
reference a `LegendPage`. `legendTitle`/`legendText` SHALL be retained.

#### Scenario: Wired levels

- GIVEN the three LevelConfig assets
- WHEN inspected
- THEN each has a non-null `legendPage`
- AND each retains `legendTitle` and `legendText`

### Requirement: Slot Rendering

When a level has a non-null `legendPage`, `LegendController` MUST fill every newspaper TMP slot
from the matching `LegendPage` field (presentation: slot fill and placement).

#### Scenario: All slots filled

- GIVEN a level with a non-null `legendPage`
- WHEN the Legend scene renders
- THEN every TMP slot text equals its matching `LegendPage` field

### Requirement: demandsList Generation

`demandsList` SHALL be generated from the level's `recipes`, never authored. Each recipe SHALL
yield one line `• {dishName}: {ing1} + {ing2} + …`, where `dishName` is `RecipeData.dishName` and
ingredients are each `CardData.displayName` joined by ` + `, in `recipes` order.

#### Scenario: Mexico list

- GIVEN `LevelConfig_Level1Mexico` with 6 recipes
- WHEN `demandsList` is generated
- THEN it has 6 lines, e.g. `• Tacos de Chapulines: Maíz + Insectos + Chile Piquín`

#### Scenario: Single ingredient

- GIVEN a recipe with one ingredient
- WHEN generated
- THEN the line has no trailing ` + ` and reads `• {dishName}: {ingredient}`

### Requirement: Legacy Fallback

When `legendPage == null`, rendering SHALL fall back to `legendTitle` (title slot) and
`legendText` (body slot), with no regression.

#### Scenario: Fallback renders

- GIVEN a level with `legendPage == null` and non-empty `legendText`
- WHEN the Legend scene renders
- THEN the title/body slots show `legendTitle`/`legendText`

### Requirement: Auto-forward Guard

When BOTH `legendPage == null` AND `legendText` is empty, the Legend scene SHALL auto-forward to
`nextScene` (preserve today's behaviour).

#### Scenario: Empty level forwards

- GIVEN a level with null `legendPage` and empty/null `legendText`
- WHEN the Legend scene starts
- THEN it loads `nextScene` without rendering a page

#### Scenario: Page present does not forward

- GIVEN a level with a non-null `legendPage`
- WHEN the scene starts
- THEN it renders the page and does NOT auto-forward

### Requirement: Text-Only

This change SHALL NOT alter sprites, the backdrop, or any image. Authored copy MUST fit the
existing newspaper art slots.

#### Scenario: No image diff

- GIVEN the change diff
- WHEN reviewed
- THEN no sprite, texture, or backdrop asset is modified

#### Scenario: Copy fits slots

- GIVEN the Ahuizotl legend in Play mode
- WHEN screenshotted
- THEN every slot's text sits within its art box with no overflow

---

**Pure rules (EditMode-testable):** LegendPage Model, demandsList Generation, Legacy Fallback,
Auto-forward Guard. **Presentation:** Slot Rendering.

**Non-normative — out of scope:** `TutorialLegend.unity` scene layout (Llorona page authored but
not rendered there); any image/sprite work; Colombia/La Llorona scene rendering (both fall back via
legacy pair until the scene edit lands). Legacy `legendTitle`/`legendText` removal is excluded.
