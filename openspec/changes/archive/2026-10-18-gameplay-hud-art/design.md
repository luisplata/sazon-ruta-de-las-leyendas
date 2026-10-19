# Design: Gameplay HUD Art

## Context

Both gameplay scenes (`Prototype.unity`, `Tutorial.unity`) have a sprite-less HUD: every
`Image`/`Button` graphic is `m_Sprite: {fileID: 0}`, and lives/patience/coins are TMP text only.
`Escene UI.png` (guid `4d1c34e75cfa48b4499594399e05d055`) is referenced **nowhere** in `Assets/`.
This change places its slices as a presentation-only layer. Verified on disk:

- Canvas `&1839082317` / RT `&1839082321`; `CanvasScaler &1839082319` = ScaleWithScreenSize,
  reference **1920×1080**, Match **0.5**. Children, **identical order both scenes**: Hand,
  CenterDropZone, CornerDropZone, **TopBar**, RefillLabel, DishSlot, **FadeOverlay** (last → on top;
  its Image is `m_Enabled: 0`, enabled by `FadeController`).
- `TopBar &870988179` (stretch, `sizeDelta.x` **−683.034** Prototype / **−627.341** Tutorial) has
  `HorizontalLayoutGroup` `ChildForceExpandWidth: 1` → a direct child reflows **all** siblings.
  Prototype children: LivesLabel, PatienceLabel, CoinsLabel, RefillButton, PrepareButton. Tutorial:
  ObjectiveLabel, CoinsLabel, RefillButton, PrepareButton, LivesLabel, PatienceLabel.
- `RefillButton` = GO `&1765695987`, RT `&1765695988` (160×60), **Image `&1765695990`**, child
  `RefillText &712268014` (stretch, TMP `Refill`, 36pt, black, centred). **Same fileIDs both scenes.**
- `scene-lighting-integration` is committed (`48c78a6`); working tree clean → scenes safe to edit.

## Goals / Non-Goals

**Goals**: both scenes render the `Escene UI` art layer as a `HUDArt` Canvas child immediately
before `FadeOverlay`, at native aspect-correct size, with zero change to any HUD value or script.

**Non-Goals**: patience-bar coloured fill (slices not received), button behaviour wiring (no
exit/back/notes code exists), `_3` brushstroke, legend/newspaper art, any C# or data change.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|---|---|---|---|
| Art delivery | **`HUDArt.prefab` instanced in both scenes** | (b) author inline in each scene; (c) hybrid | The *differing* bits (TopBar child set/order/sizes, `sizeDelta.x`) are all **outside** `HUDArt` — nothing in the art cluster references TopBar. So one prefab can hold **100%** of the cluster with **zero** per-scene exceptions. Prefab = 1 source of truth, ~1 380 changed lines total vs ~2 400 for inline, and drift becomes impossible. Hybrid (c) is unnecessary: the only per-scene deltas are the sibling index and the RefillButton sprite, which are scene-local by nature. |
| Where the prefab sits | Root RT **stretch** `(0,0)-(1,1)`, sizeDelta `0`, pivot `(0.5,0.5)`; instanced as Canvas child then `SetSiblingIndex(6)` | child of TopBar | Pushes `FadeOverlay` to index 7 so the round-end cover still hides all art. TopBar is forbidden: `ChildForceExpandWidth: 1` reflows every sibling. |
| Refill glyph slice | **`_10`** (`-7074593693958136706`, 119×106) | `_9` (100×102); both halves as a composite | `_9` (rect x 11–111) and `_10` (x 118–237) are the **left/right halves of one two-arrow loop**; neither is the whole glyph. `_10` is the larger half (≈13% more area) and carries the wider arc + arrowhead, so it reads as a complete "refresh" arrow at 60px height (drawn ≈67×60). Composite (both halves, pixel-offset) is a polish follow-up, not v1. |
| `RefillText` | **Hide it** (`m_IsActive: 0`) | keep caption; move/shrink it | `RefillText` is a *stretch* child filling the 160×60 button, black 36pt, centred — the centred glyph would sit directly behind it and be illegible. The mockup's refill control is icon-only. The refill **value** is still shown by `RefillLabel` (`REFILLS: n/m`), so no information is lost. Reflow-safe (child of the button, not of TopBar) and a one-line revert. |
| Labels | **All TMP labels stay** — art sits *beside* them | delete `LivesLabel`/`PatienceLabel` | Deleting `LivesLabel` would null `GameLoopController.livesText` (NRE on every update) and remove a TopBar child → reflow of every sibling. Forbidden by "no script change". |
| Corner space | Corner clusters live in TopBar's **inset margin** | narrow TopBar | `sizeDelta.x` is *absolute*, so TopBar's side margins are ≈341 px (Prototype) / ≈313 px (Tutorial) at **every** aspect. The corner clusters need ≤171 px → they always clear the strip. |
| Image type | `m_Type: 0` (Simple) + `m_PreserveAspect: 1` + native `sizeDelta` | 9-slice (`m_Type: 1`) | Every slice has `border {0,0,0,0}` → 9-slice is undefined/no-op. With point anchors `sizeDelta` is absolute, so size never changes with aspect; `preserveAspect` is belt-and-braces. |
| New buttons | Plain `Image`, no `Button`, `m_RaycastTarget: 0` | `Button` with empty `onClick` | No exit/back/notes code exists anywhere (verified). A dead `Button` is a fake affordance that still eats clicks. Names kept from `explore.md` for continuity. |
| Patience cap position | `_1` **below** the frame (`y −430`) | cap above frame | Follows `explore.md`. Sprite fangs point down/throat above, so cap-above is also plausible — flagged as the single most likely visual tweak; flip = swap the two `anchoredPosition.y` (0 ↔ −430) in the prefab, no scene change. |

## Hierarchy (render order back → front)

```
Canvas &1839082321
├─ 0 Hand · 1 CenterDropZone · 2 CornerDropZone · 3 TopBar · 4 RefillLabel · 5 DishSlot
├─ 6 HUDArt                       ← prefab instance (NEW; FadeOverlay moves 6 → 7)
│    ├─ 1 BookPanel        _2
│    ├─ 2 ExitCornerArc    _12
│    ├─ 3 LivesHats        (container)
│    │    ├─ Hat1 _0 · Hat2 _0 · Hat3 _0
│    ├─ 4 PatienceBar      (container)
│    │    ├─ Frame _5 · Cap _1
│    ├─ 5 IngredientStone  _6
│    ├─ 6 ExitButton       _4
│    ├─ 7 BackButton       _8
│    └─ 8 NewButton        _7
└─ 7 FadeOverlay          ← must stay LAST
```

Backdrops (`BookPanel`, `ExitCornerArc`) precede the icons drawn on them; the three isolated
clusters (`LivesHats`, `PatienceBar`, `IngredientStone`) have no overlap so their order is stable
but arbitrary.

## Layout table — reference resolution **1920×1080**

`sizeDelta` = the slice's native px, so at 16:9 one sprite px = one UI unit.

| Element | Sprite | anchorMin / anchorMax | pivot | anchoredPosition | sizeDelta |
|---|---|---|---|---|---|
| `HUDArt` (root) | — | (0,0) / (1,1) | (0.5,0.5) | (0,0) | (0,0) |
| `BookPanel` | `_2` | (1,0) / (1,0) | (1,0) | (0,0) | (466,299) |
| `ExitCornerArc` | `_12` | (0,0) / (0,0) | (0,0) | (0,0) | (527,348) |
| `LivesHats` | — | (0,1) / (0,1) | (0,1) | (24,−24) | (115,220) |
| ↳ `Hat1` | `_0` | (0,1) / (0,1) | (0,1) | (0,0) | (115,68) |
| ↳ `Hat2` | `_0` | (0,1) / (0,1) | (0,1) | (0,−76) | (115,68) |
| ↳ `Hat3` | `_0` | (0,1) / (0,1) | (0,1) | (0,−152) | (115,68) |
| `PatienceBar` | — | (1,1) / (1,1) | (1,1) | (−24,−24) | (147,567) |
| ↳ `Frame` | `_5` | (0.5,1) / (0.5,1) | (0.5,1) | (0,0) | (93,430) |
| ↳ `Cap` | `_1` | (0.5,1) / (0.5,1) | (0.5,1) | (0,−430) | (147,137) |
| `IngredientStone` | `_6` | (0.5,1) / (0.5,1) | (0.5,1) | (0,−180) | (153,136) |
| `ExitButton` | `_4` | (0,0) / (0,0) | (0,0) | (40,40) | (160,161) |
| `BackButton` | `_8` | (1,0) / (1,0) | (1,0) | (−230,60) | (160,161) |
| `NewButton` | `_7` | (1,0) / (1,0) | (1,0) | (−70,60) | (160,161) |

`BackButton`/`NewButton` sit side-by-side, vertically centred inside `BookPanel`
(panel x 1454–1920, y 0–299; buttons x 1530–1690 / 1690–1850, y 60–221). `IngredientStone` is at
`y −180` (not −24) because top-centre is inside the strip's x-range; Prototype's strip is ~60 tall
(120 px clearance), Tutorial's ~150 tall at centre-x (30 px clearance). See Edge Cases.
Every new `Image`: `m_RaycastTarget: 0`, `m_Type: 0`, `m_PreserveAspect: 1`, layer `0` (matches
existing HUD children).

## Per-slice sprite reference

Form: `m_Sprite: {fileID: <internalID>, guid: 4d1c34e75cfa48b4499594399e05d055, type: 3}`.
All slice pivots in the meta are `{0,0}` — irrelevant to uGUI `Image`, which fills its rect.

| Slice | Name | internalID | Rect x,y,w,h | Native px | Used by |
|---|---|---|---|---|---|
| `_0` | Escene UI_0 | `-3305728652142534457` | 65,928,115,68 | 115×68 | `LivesHats` ×3 |
| `_1` | Escene UI_1 | `7438932503285152898` | 257,908,147,137 | 147×137 | `PatienceBar/Cap` |
| `_2` | Escene UI_2 | `-5881899963768527644` | 418,766,466,299 | 466×299 | `BookPanel` |
| `_3` | Escene UI_3 | `-4742025654892972584` | 888,48,328,993 | 328×993 | **excluded** (reserved) |
| `_4` | Escene UI_4 | `280097415015255549` | 41,745,160,161 | 160×161 | `ExitButton` |
| `_5` | Escene UI_5 | `917242437539518481` | 286,449,93,430 | 93×430 | `PatienceBar/Frame` |
| `_6` | Escene UI_6 | `7861356603297831984` | 60,574,153,136 | 153×136 | `IngredientStone` |
| `_7` | Escene UI_7 | `2788912981022761824` | 446,565,160,161 | 160×161 | `NewButton` |
| `_8` | Escene UI_8 | `-8992848258393318884` | 676,565,160,161 | 160×161 | `BackButton` |
| `_9` | Escene UI_9 | `4214963178974104771` | 11,421,100,102 | 100×102 | not used (other half of loop) |
| `_10` | Escene UI_10 | `-7074593693958136706` | 118,410,119,106 | 119×106 | **`RefillButton.Image`** |
| `_11` | Escene UI_11 | `3327507871601162226` | 141,409,14,8 | 14×8 | ignored (auto-slice noise) |
| `_12` | Escene UI_12 | `-1945782980059101193` | 0,0,527,348 | 527×348 | `ExitCornerArc` |

## File Changes

| File | Action | Description |
|---|---|---|
| `Assets/Prefabs/HUDArt.prefab` (+ `.meta`) | Create | Root stretch RT + 3 containers + 11 `Image`s on `Escene UI` slices |
| `Assets/Scenes/Prototype.unity` | Modify | Instance `HUDArt` at Canvas index 6; `RefillButton.Image &1765695990` ← `_10` + `preserveAspect: 1`; `RefillText &712268014` `m_IsActive: 0` |
| `Assets/Scenes/Tutorial.unity` | Modify | Same three edits (same fileIDs); verify only — TopBar differs, art is outside it |
| `Assets/_sprites/Escene UI.png.meta` | Reference | Not edited; slices referenced by fileID |
| `Assets/Scripts/**` | None | No script changes |

## Edge Cases

- **TopBar reflow**: `HUDArt` is never a TopBar child; the `RefillText`/`Image` edits touch no child
  list. Assert TopBar child count is still 5 (Prototype) / 6 (Tutorial).
- **FadeOverlay order**: `HUDArt` must land at index 6. Assert `Canvas.m_Children` =
  `… DishSlot, HUDArt, FadeOverlay`. FadeOverlay's Image starts `m_Enabled: 0`, so art is visible
  at rest and covered only when `FadeController` enables it.
- **Ultrawide (Match 0.5)**: at 21:9 the ref area shrinks to ≈2217×935. Tallest top cluster
  (`PatienceBar`, 591) + tallest bottom cluster (`BookPanel`, 299) = 890 < 935 → no vertical
  collision. Corner clusters stay inside TopBar's absolute inset margin at every aspect.
- **`IngredientStone` vs TopBar**: 30 px clearance in Tutorial at `y −180`. Contingency = a
  **per-instance prefab override** of `anchoredPosition.y` in Tutorial (still one prefab).
- **Non-square slices** (`_1 _2 _5 _12`): `preserveAspect: 1` + native `sizeDelta`; `border` is all
  zeros → never `m_Type: 1`.
- **`RefillText` overlap**: resolved by hiding it (see decisions).
- **Prefab drift**: always edit the prefab asset, never a scene instance — an instance edit becomes
  an override and Tutorial silently misses it.
- **Rollback order**: revert scenes *before* deleting the prefab, or the scenes hold missing refs.

## Testing / Validation

No `strict_tdd` (jam, zero C# changed → nothing unit-testable). Validation is structural asserts +
screenshots:

| Layer | What | Approach |
|---|---|---|
| Structural | HUDArt at Canvas index 6; FadeOverlay last; TopBar child count unchanged; `RefillButton.Image.m_Sprite` = `_10`; `RefillText.m_IsActive` = 0; 8 HUDArt children | Unity MCP reads of both scenes |
| Play mode | HUD matches the mockup; art visible, values unchanged | `manage_camera(screenshot)` in **both** scenes |
| Aspect | 16:9 **and** a wide view (21:9 / 2560×1080): no distortion, no crowding, stone clears TopBar | Screenshots at both aspects |
| Fade | Round-end black cover hides **all** new art | Trigger win/lose (or enable FadeOverlay) → screenshot |
| Console | No errors/warnings after each scene edit | `read_console` |

Evidence to `Assets/Screenshots/`; `read_console` clean is a success criterion.

## Chaining

Forecast: prefab ≈ **1 210** lines + meta ≈ 8; `Prototype.unity` ≈ **90**; `Tutorial.unity` ≈ **90**
→ ≈ **1 400** additions, well over 400. **Stacked PRs (2 slices)**, matching the proposal:

```text
main (48c78a6 scene-lighting-integration committed)
 └── 📍 PR1 feat(hud): HUDArt prefab + Prototype art layer   [prefab + Prototype.unity]
      └── PR2 feat(hud): mirror HUDArt into Tutorial          [Tutorial.unity]
```

| PR | Scope | Files | Starts at | Ends with | Rollback | Budget |
|---|---|---|---|---|---|---|
| **PR1** | Prefab + first consumer | `HUDArt.prefab`(+meta), `Prototype.unity` | `main` @ `48c78a6` | Prototype HUD renders the mockup; prefab has a consumer | `git checkout -- Assets/Scenes/Prototype.unity` then `rm Assets/Prefabs/HUDArt.prefab*` | ≈1 300 / 400 ⚠ |
| **PR2** | Tutorial mirror | `Tutorial.unity` | PR1 branch | Both scenes art'd; chain complete | `git checkout -- Assets/Scenes/Tutorial.unity` | ≈90 / 400 ✅ |

**PR1 needs `size:exception`**: its ~1 210 lines are one machine-generated uGUI prefab, reviewed as
a single structural unit (not 1 210 lines of logic) — exactly the "generated code" case the
chained-PR skill lists. Splitting the prefab out alone yields a non-autonomous PR (a prefab with no
consumer), which the skill forbids. If declined: 3 PRs (prefab → Prototype → Tutorial), prefab PR
marked non-reviewable-alone — it still needs the exception, so 2 PRs is preferred.

## Migration / Rollout

No migration. No data, no scripts, no prefab/variant changes. Presentation-only; ships in one jam
step per scene.

## Open Questions

- [ ] Patience cap above vs below the frame — resolved by the first Prototype screenshot
  (prefab-only one-line flip).
- [ ] `RefillText` hidden vs caption kept — assume hidden; revert is one line.
- [ ] `size:exception` for PR1 approved? (blocks nothing; PR2 depends on PR1 either way.)
