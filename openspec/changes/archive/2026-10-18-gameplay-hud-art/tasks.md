# Tasks: Gameplay HUD Art

Each PR is one autonomous work unit. Presentation-only; no scripts, no data, no strict TDD.

## Phase 1: PR1 — HUDArt.prefab + Prototype.unity wiring

- [x] 1.1 [setup] Assert a live instance (`mcpforunity://instances`); pin it.
- [x] 1.2 [prefab] Create `Assets/Prefabs/HUDArt.prefab`: root `HUDArt` stretch RT, anchors (0,0)/(1,1), sizeDelta 0, pivot (0.5,0.5).
- [x] 1.3 [prefab] Add 3 containers + 11 Images per design table: `BookPanel`_2, `ExitCornerArc`_12, `LivesHats`(Hat1-3 _0), `PatienceBar`(Frame _5, Cap _1), `IngredientStone`_6, `ExitButton`_4, `BackButton`_8, `NewButton`_7.
- [x] 1.4 [prefab] Each Image: sprite `{fileID, guid 4d1c34e75cfa48b4499594399e05d055, type 3}`, `m_Type:0`, `m_PreserveAspect:1`, native sizeDelta, pivot/anchor/pos from table, `m_RaycastTarget:0`, layer 0, no `Button`.
- [x] 1.5 [prefab] Slices `_0,_1,_2,_4,_5,_6,_7,_8,_12` only; never `_3`/`_11`. Non-square `_2,_5,_12,_1` keep native sizeDelta + preserveAspect.
- [x] 1.6 [scene] Load `Prototype.unity`; instance prefab as `Canvas &1839082321` child; `SetSiblingIndex(6)` → before `FadeOverlay`.
- [x] 1.7 [scene] `RefillButton.Image &1765695990` ← `Escene UI_10` (`-7074593693958136706`), preserveAspect 1 (scene-local).
- [x] 1.8 [scene] `RefillText &712268014` `m_IsActive:0`; keep every TMP label.
- [x] 1.9 [scene] Save; `refresh_unity`; `read_console` clean.
- [x] 1.10 [verify] Assert Canvas children end `DishSlot, HUDArt, FadeOverlay`; TopBar count still 5; HUDArt 8 children / 11 Images.
- [x] 1.11 [verify] Play → 16:9 screenshot vs mockup; confirm cap above/below (flip prefab `y` 0↔−430); evidence `Assets/Screenshots/`.
- [x] 1.12 [verify] Ultrawide 2560×1080: no distortion, stone clears TopBar.
- [x] 1.13 [verify] Fade covers all art; labels show live values; console clean. Rollback: `git checkout -- Assets/Scenes/Prototype.unity` then `rm Assets/Prefabs/HUDArt.prefab*`.

## Phase 2: PR2 — Tutorial.unity wiring

- [x] 2.1 [scene] Load `Tutorial.unity`; instance prefab; `SetSiblingIndex(6)` before `FadeOverlay`.
- [x] 2.2 [scene] `RefillButton.Image` ← `_10` + preserveAspect; `RefillText` inactive. Art outside TopBar → no overrides.
- [x] 2.3 [scene] Save; `refresh_unity`; console clean.
- [x] 2.4 [verify] Asserts: HUDArt index 6; FadeOverlay last; TopBar count 6; RefillButton sprite `_10`; 11 Images.
- [x] 2.5 [verify] Play + 16:9/wide screenshots: stone clears taller strip (else per-instance `anchoredPosition.y`); fade covers art.
- [x] 2.6 [verify] **Drift check**: `RefillButton.Image`=`_10`+preserveAspect AND children end `HUDArt, FadeOverlay` in BOTH scenes.
- [x] 2.7 [verify] `git status`: only prefab(+meta) + 2 scenes; no `.cs`/`.asset`/sprite `.meta`; EditMode 7/7. Rollback: `git checkout -- Assets/Scenes/Tutorial.unity`.

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Est. changed lines | PR1 ≈1 300 (prefab ~1 210, Prototype ~90); PR2 ≈90; total ≈1 400 |
| Files touched | 4 (HUDArt.prefab + .meta, Prototype.unity, Tutorial.unity) |
| 400-line budget risk | PR1 High · PR2 Low |
| Chained PRs recommended | Yes (2, stacked-to-main) |
| Delivery strategy | ask-on-risk → resolved CHAINED |
| size:exception | APPROVED for PR1 (machine-generated uGUI prefab) |

```text
Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: High
```

Work units: PR1 = prefab + Prototype HUD (rollback: revert Prototype, delete prefab); PR2 = Tutorial mirror (rollback: revert Tutorial.unity).

```text
main (48c78a6 scene-lighting-integration)
 └── 📍 PR1 feat(hud): HUDArt prefab + Prototype art layer  [HUDArt.prefab, Prototype.unity]
      └── PR2 feat(hud): mirror HUDArt into Tutorial         [Tutorial.unity]
```
