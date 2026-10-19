# Apply Progress — hud-action-buttons

Status: done (Prototype full, Tutorial wired except the pot — Tutorial backdrop has no pot).
Scope: presentation + wiring only. No script logic was modified.

## 1. Prefab — `Assets/Prefabs/HUDArt.prefab`

| Change | Detail |
|---|---|
| `BookPanel` | `m_IsActive: 0` |
| `BackButton` | `m_IsActive: 0` |
| `NewButton` | `m_IsActive: 0` |
| `IngredientStone` | `m_IsActive: 0` |
| `ExitButton` | Added `UnityEngine.UI.Button` (interactable, ColorTint, target graphic = own Image) |
| `ExitButton` Image | `m_RaycastTarget: 1` (was 0 — a non-raycast Image cannot receive clicks) |

`LivesHats`, `PatienceBar` and all patience segments were left untouched.
`ExitButton` keeps its existing `Escene UI_4` sprite.

Note: `Prototype.unity` carried an explicit prefab-instance override `BookPanel.m_IsActive = 1`,
so the prefab flag alone did not hide it there. The override was changed to `0` in the scene.

## 2. Scenes — `Prototype.unity` and `Tutorial.unity`

### ExitButton → REFILL
Wiring mechanism: a **serialized `onClick` persistent call** (`m_OnClick.m_PersistentCalls`)
written through `SerializedObject` + `PrefabUtility.RecordPrefabInstancePropertyModifications`,
so it is stored as a prefab-instance override in the scene and survives reload.

- Target: the scene's `Hand` GameObject (`HandController`)
- Method: `Refill`
- `HandController.Refill()` is unchanged.

`UnityEventTools.AddPersistentListener` was tried first and did **not** persist the override
into the scene (lost on reload); the `SerializedObject` route did persist it. This is the reason
for the mechanism choice.

### RefillLabel
- The existing `RefillLabel` GameObject was **kept** (so `HandController.refillLabel` stays valid).
- It was already a direct child of `Canvas` in both scenes (not inside `TopBar`), so no reparent
  was needed; it was **reordered** to sit immediately before `FadeOverlay`, i.e. after `HUDArt`.
  Reason: `ExitCornerArc` (inside `HUDArt`) was drawing over the label.
- Re-anchored bottom-left, pivot (0.5, 0), centered on the ExitButton:
  - `Prototype`: anchoredPosition `(249.6, 341.5)`, size `380x40`, font 26
  - `Tutorial`: anchoredPosition `(120, 211)`, size `240x40`, font 20 (narrower so the
    centered text does not run off the left screen edge — the ExitButton sits at x=40)
- `RefillText` was already inactive in both scenes and is not a scene object at runtime
  (`GameObject.Find` -> null). Nothing to hide.

### PotButton → PREPARAR COMIDA (Prototype only)
- Canvas child, inserted at sibling index 1 (after `Hand`, **before** `CenterDropZone`/`CornerDropZone`
  so the drop zones and cards still win raycasts where they overlap).
- Position: anchored center `(4, -145)`, size `500x420`, pivot (0.5, 0.5).
- Affordance: `Image` using the new `Assets/_sprites/UIRing.png` (soft radial ring, generated
  procedurally), color `(1, 0.82, 0.32, 0.85)`, Button ColorTint that brightens on hover/press.
  A flat radial fill was tested first but washed out the cauldron art, so the ring was used instead.
- Wiring: serialized `onClick` persistent call -> scene `GameLoop` (`GameLoopController.ServeFood`).
  `GameLoopController` unchanged.

How the pot was located: played `Prototype`, measured the cauldron in the 1920x1080 Game view
against the `Backdrop` (`BG_0` sprite, 1917x1074 px, PPU 100, camera ortho size 5, aspect 16:9).
Screen-space measurement (cauldron rim/body) -> canvas pixels, giving center `(964, 395)`
and an effective cauldron size of about `384x314`. The button was then enlarged slightly
(500x420) so the glow ring spills onto the dark surroundings and remains visible.

### Tutorial — no pot
`Tutorial.unity`'s `Backdrop` is `WhiteSprite_0` (scaled 40x24) over grey colour blocks; it is
placeholder art and contains **no pot**. Per instruction, no `PotButton` was invented for it.
ServeFood remains reachable there through the pre-existing `TopBar` "Preparar Comida" button.

## 3. Play-mode evidence

All checks were run in Play mode through the live bridge, screenshot evidence in `Assets/Screenshots/`.

### Prototype (`Assets/Scenes/Prototype.unity`)
- `HUDArt/BookPanel, BackButton, NewButton, IngredientStone` -> all `activeInHierarchy == False`
- `LivesHats`, `PatienceBar`, `ExitCornerArc` -> still active
- GraphicRaycaster at the ExitButton center -> top hit `ExitButton` (`raycastTarget == true`)
- `ExitButton.onClick` -> 1 listener, target `Hand`, method `Refill`
- Invoking it: label `REFILLS (MANO): 0/2` -> `1/2`; the whole hand was replaced by a fresh
  deal afterwards (the discard-fly + re-deal path ran to completion)
- GraphicRaycaster at the pot center (964, 395) -> top hit `PotButton`
- Parked 2 cards in `CenterDropZone`, then invoked `PotButton.onClick`:
  `CenterDropZone.Count 2 -> 0`, dish result spawned ("¡¿Comida cruda?!" + "Comida Cruda" card),
  lives 3 -> 2. ServeFood resolved through the pot button.
- Screenshots: `prototype_before.png`, `prototype_final.png` (1/2 + pot ring), `prototype_cook.png`

### Tutorial (`Assets/Scenes/Tutorial.unity`)
- Same four clusters -> all `activeInHierarchy == False`
- Raycaster at the ExitButton center -> top hit `ExitButton`
- `ExitButton.onClick` -> target `Hand`, method `Refill`; label `0/2` -> `1/2`, hand re-dealt
- No PotButton (no pot in the backdrop), by design.
- Screenshots: `tutorial_before.png`, `tutorial_refilled.png`

## 4. Console status
`read_console` (errors + warnings) after all edits: clean. Only the pre-existing unrelated
`Account API did not become accessible within 30 seconds` message.
`refresh_unity` + compile: clean, no compile errors.
`LiberationSans SDF - Fallback.asset` was not re-dirtied (not present in `git status`).

## 5. Could not do / discrepancies
1. **Tutorial has no pot** — its backdrop is placeholder white/grey art. No `PotButton` added there.
2. **Tutorial `TopBar` is NOT hidden** (`m_IsActive: 1`), contrary to the brief. It shows the old
   Objective text, "Preparar Comida", VIDAS/PACIENCIA. Per the explicit "do NOT touch TopBar"
   constraint it was left as-is. This means Tutorial still shows the old prepare/refill controls
   in the top bar alongside the new ExitButton refill. Needs a user decision.
3. In Tutorial the old `RefillButton` (inside the active TopBar) is still visible; its old
   `onClick -> Refill` is untouched.

## 6. Files changed
- `Assets/Prefabs/HUDArt.prefab` (four clusters inactive, ExitButton Button + raycast)
- `Assets/Scenes/Prototype.unity` (ExitButton wiring, RefillLabel placement/order, PotButton, BookPanel override)
- `Assets/Scenes/Tutorial.unity` (ExitButton wiring, RefillLabel placement/order)
- `Assets/_sprites/UIRing.png` + `.meta` (new, procedural ring affordance)

No commits made. No scripts modified (`Assets/Scripts/GameLoop/GameLoopController.cs` was already
dirty from the previous hud-lives-patience work; it was not touched here).
