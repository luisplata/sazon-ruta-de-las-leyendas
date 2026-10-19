# Apply Progress: Gameplay HUD Art

**Mode**: Standard (strict_tdd: OFF — zero C# changed; nothing unit-testable).
**Delivery**: 2 chained PRs, `stacked-to-main`. PR1 = `size:exception` (APPROVED). Both implemented in this run.
**Instance**: `GameJamUni2@9eee0fc1` (pinned). Unity 6000.3.10f1.

Presented artifacts are machine-generated uGUI only — no scripts, no data, no logic touched.

---

## PR1 — HUDArt.prefab + Prototype.unity wiring

| # | Task | Status |
|---|---|---|
| 1.1 | Live instance asserted + pinned | [x] |
| 1.2 | `HUDArt.prefab` root stretch RT (0,0)-(1,1), sizeDelta 0, pivot (0.5,0.5) | [x] |
| 1.3 | 8 clusters / 11 Images created | [x] |
| 1.4 | Each Image: slice sprite, `m_Type:0`, `m_PreserveAspect:1`, native sizeDelta, table pivot/anchor/pos, `m_RaycastTarget:0`, layer 0, no `Button` | [x] |
| 1.5 | Slices `_0,_1,_2,_4,_5,_6,_7,_8,_12` only; `_3`/`_11` never used | [x] |
| 1.6 | Instanced as Canvas child, `SetSiblingIndex(6)` (before `FadeOverlay`) | [x] |
| 1.7 | `RefillButton.Image` ← `Escene UI_10` + `preserveAspect:1` | [x] |
| 1.8 | `RefillText` `m_IsActive:0`; every TMP label kept | [x] |
| 1.9 | Saved; `refresh_unity`; console clean | [x] |
| 1.10 | Structural asserts pass | [x] |
| 1.11 | Play → 16:9 screenshot; cap confirmed **below** frame (no flip needed) | [x] |
| 1.12 | Ultrawide 2560×1080: no distortion, stone clears TopBar | [x] |
| 1.13 | Fade covers all art; labels live; console clean | [x] |

## PR2 — Tutorial.unity wiring

| # | Task | Status |
|---|---|---|
| 2.1 | Loaded; instanced; `SetSiblingIndex(6)` before `FadeOverlay` | [x] |
| 2.2 | `RefillButton.Image` ← `_10` + preserveAspect; `RefillText` inactive; no overrides | [x] |
| 2.3 | Saved; `refresh_unity`; console clean | [x] |
| 2.4 | Asserts pass (index 6, FadeOverlay last, TopBar 6, sprite `_10`, 11 Images) | [x] |
| 2.5 | Play + 16:9/wide screenshots; stone clears taller strip; fade covers art | [x] |
| 2.6 | Drift check passes in BOTH scenes | [x] |
| 2.7 | git status clean of code/asset changes; EditMode 7/7 | [x] |

---

## Files Created / Changed

| File | Action | Detail |
|---|---|---|
| `Assets/Prefabs/HUDArt.prefab` (+ `.meta`, guid `a8f04f699e5d36b48acba64163692aba`) | **Created** | 945-line uGUI prefab: root `HUDArt` + 8 children (2 containers + 11 Images) |
| `Assets/Scenes/Prototype.unity` | Modified | +106 / −3 — `HUDArt` instance at Canvas index 6; `RefillButton.Image` ← `_10`; `RefillText` inactive |
| `Assets/Scenes/Tutorial.unity` | Modified | +115 / −7 — same three edits (same GO/fileIDs; TopBar differs) |
| `Assets/_sprites/Escene UI.png.meta` | **Not changed** | Slices referenced by internalID |
| `Assets/Scripts/**` | **Not changed** | No C# diff |

Evidence: `Assets/Screenshots/proto_16x9_hud.png`, `proto_21x9_hud.png`, `proto_16x9_fade.png`,
`tutorial_16x9_hud.png`, `tutorial_21x9_hud.png` (gitignored).

---

## Structural verification (Unity MCP reads)

**Prototype.unity**
- Canvas children: `Hand, CenterDropZone, CornerDropZone, TopBar, RefillLabel, DishSlot, HUDArt, FadeOverlay` → ends `DishSlot, HUDArt, FadeOverlay`. **HUDArt index 6, FadeOverlay last.**
- `TopBar` child count **5** (unchanged), `sizeDelta.x` **−683.034** (unchanged).
- `HUDArt`: **8 children / 11 Images**, all `m_Type:0`, `preserveAspect:True`, `raycastTarget:False`.
- `RefillButton.Image` = `Escene UI_10`, `preserveAspect:True`, `raycastTarget:True`, Button interactable, targetGraphic intact, 1 persistent onClick.
- `RefillText.activeSelf=False`.
- `PrefabUtility.HasPrefabInstanceAnyOverrides(HUDArt)=False` → clean instance, no per-instance overrides.

**Tutorial.unity**
- Canvas children end `DishSlot, HUDArt, FadeOverlay`; **HUDArt index 6, FadeOverlay last.**
- `TopBar` child count **6** (unchanged), `sizeDelta.x` **−627.341** (unchanged).
- `HUDArt`: 8 children / 11 Images; clean prefab instance.
- `RefillButton.Image` = `Escene UI_10`, `preserveAspect:True`; `RefillText.activeSelf=False`.

**Geometry @ 1920×1080 (canvas-local, verified computed world corners)** — matches the design table:

| Element | box (x0,y0 → x1,y1) |
|---|---|
| BookPanel | 1454,0 → 1920,299 |
| ExitCornerArc | 0,0 → 527,348 |
| Hat1/2/3 | 24,988→1056 / 24,912→980 / 24,836→904 |
| Frame | 1776,626 → 1869,1056 |
| Cap | 1749,489 → 1896,626 (**below** Frame) |
| IngredientStone | 884,764 → 1037,900 |
| ExitButton | 40,40 → 200,201 |
| BackButton | 1530,60 → 1690,221 |
| NewButton | 1690,60 → 1850,221 |

**Geometry @ 2560×1080 (canvas ref 2217×935)** — corner clusters stay pinned, `sizeDelta` unchanged (no distortion); `IngredientStone` top edge at 180 px from top → clears the strip.

## Play-mode evidence (what was seen)

- **Prototype 16:9**: 3 hats top-left, aztec stone top-centre (below strip), patience frame + cap top-right (cap below), refill glyph in TopBar, orange arc + red X bottom-left, book panel + back arrow + green "New" bottom-right. Labels show live values `VIDAS: 3 / PACIENCIA: 3 / MONEDAS: 0 / REFILLS (MANO): 0/2`. No distortion.
- **Prototype 21:9 (2560×1080)**: identical clusters pinned to corners, no distortion, stone clears the strip, labels live.
- **Prototype fade**: forcing `FadeOverlay` on produced a full black frame → **all art covered**.
- **Tutorial 16:9 / 21:9**: same clusters; labels live (`VIDAS: 5 / PACIENCIA: 3 / MONEDAS: 0`); refill glyph visible; stone clears the taller Tutorial strip.
- **Cross-scene drift check**: both scenes → `RefillButton.Image`=`_10`+preserveAspect, children end `HUDArt, FadeOverlay`, clean instances.

## Console status

- After Prototype edit + refresh: **0 errors** (only pre-existing TMP font-unicode, Unity AI-assistant and MCP-serializer warnings, none caused by this change).
- During Prototype Play: **0 errors**.
- After Tutorial edit + refresh: **0 errors**.
- EditMode tests: **7/7 passed**.

## Deviations from design

1. **None functional.** Layout, slices, anchors, ordering and edits match `design.md` exactly.
2. **Cap position**: design's open question resolved — cap renders **below** the frame (`y −430`), as planned; the one-line flip is unnecessary.
3. **Prefab size**: 945 lines (design estimated ≈1210). Only a sizing estimate; `size:exception` for PR1 still applies.
4. **Observation (not a defect)**: Tutorial's intro dialogue (a world-space bubble under `Enemy`, **not** a Canvas child) occupies the top-right during the first seconds of Play, visually adjacent to the patience frame. It is transient and renders *below* the Screen-Space-Overlay HUD (only one Canvas exists). No ordering change made.

## Notes

- Custom Game-view size `2560×1080` and `1920×1080` were added via editor reflection for captures — editor-side only, not committed.
- **DO NOT COMMIT** was honoured; no commit was made.
