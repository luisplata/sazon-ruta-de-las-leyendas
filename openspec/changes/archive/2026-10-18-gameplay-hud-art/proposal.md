# Proposal: Gameplay HUD Art

## Intent

The gameplay HUD has **no art**: lives / patience / coins are bare TMP text and every HUD
`Image`/`Button` is sprite-less. This change places the approved `Escene UI.png` slices as a
**presentation-only** layer so the HUD reads as the mockup, without touching any runtime rule.

## Scope

### In Scope
- New empty `HUDArt` RectTransform under `Canvas`, inserted **immediately before `FadeOverlay`**
  (FadeOverlay moves to last). Never a child of `TopBar`.
- Static decor + slice assignment in **both** `Prototype.unity` and `Tutorial.unity`:
  - `_0` sombrero ×3 → lives (top-left); `_6` aztec stone → ingredient indicator (top-centre).
  - `_5` bar frame + `_1` creature cap → patience bar (top-right) — frame/cap only.
  - `_12` corner arc + `_4` red X → exit cluster (bottom-left).
  - `_8` back + `_7` book/New + `_2` open-book panel → bottom-right cluster.
  - `_9`/`_10` loop arrows → EXISTING `RefillButton.Image` (no reflow).
- Decoration elements are `Image` only, `preserveAspect: 1`, native `sizeDelta`, corner anchors.
- Decide `_3` (brushstroke): **leave out** (reserved; no banner in the mockup).
- Mirror to `Tutorial.unity` **by hand** (its `TopBar` differs).

### Out of Scope
- Patience-bar coloured **fill** slices (not received) → bar stays frame+cap.
- Behaviour wiring for ingredient / exit / back / New buttons (no scene-load code exists).
- Legend / newspaper art (separate `legend-newspaper-art`).
- Backdrop, pot, `Creature talk` (done/committed); Prototype lighting rig (committed).
- Any change to `GameLoopController` / `GameLoopConfig` / `HandController` / HUD data.

## Capabilities

### New Capabilities
- `gameplay-hud-art`: both gameplay scenes SHALL render the `Escene UI` art layer (lives hats,
  ingredient stone, patience frame+cap, exit cluster, bottom-right cluster, refill glyph) as a
  `HUDArt` layer below `FadeOverlay`, at native aspect-correct size, without altering HUD values.

### Modified Capabilities
- None.

## Approach

Add one `HUDArt` container (Canvas child, before `FadeOverlay`); populate plain `Image`s at
native `sizeDelta` with corner/centre anchors + `preserveAspect`. Reuse only `RefillButton.
Image`. Numeric labels keep showing real values. Do it in `Prototype.unity` first
(screenshot + `read_console`), then mirror to `Tutorial.unity`. If drift gets heavy, extract
`HUDArt` as a **prefab** instanced in both scenes (one source of truth). No scripts.

### Alternatives Considered
- **Add art as `TopBar` children** — rejected: `HorizontalLayoutGroup` (`ChildForceExpandWidth=1`)
  reflows every sibling.
- **New HUD framework / data binding** — rejected: jam scope, presentation-only; the patience
  fill is blocked anyway.
- **Duplicate raw YAML per scene** — kept as v1 default; prefab extraction is the de-drift lever.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Assets/Scenes/Prototype.unity` | Modified | Add `HUDArt` + ~10 UI objects; set `RefillButton` sprite |
| `Assets/Scenes/Tutorial.unity` | Modified | Mirror by hand (different `TopBar`) |
| `Assets/_sprites/Escene UI.png` | Referenced | Slice sprites by fileID only |
| `Assets/Prefabs/HUDArt.prefab` | New (optional) | Only if prefab extraction is chosen |
| `Assets/Scripts/**` | None | No script changes |

## Assumptions

1. `scene-lighting-integration` is committed (confirmed) → scenes are safe to edit.
2. New elements are decor; the numeric labels remain the source of truth for values.
3. `_9`/`_10` are one glyph split in two — v1 uses one slice on `RefillButton`.
4. Editor edits via Unity MCP with the **correct scene loaded** (`GameJamUni2@9eee0fc1`).

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| `TopBar` reflow | High | Never parent art to `TopBar`; use `HUDArt` |
| Render order vs `FadeOverlay` | Med | Insert `HUDArt` before it; verify end-of-round fade |
| Non-square slices distort | Med | `preserveAspect` + native `sizeDelta` (`_2`,`_5`,`_12`,`_1`) |
| Ultrawide crowding (Match 0.5) | Med | Keep elements compact; screenshot 16:9 + wide |
| Ingredient stone overlaps `TopBar` | Med | Sink below top strip; screenshot |
| Two-scene drift | Med | Hand-mirror + screenshot both; prefab fallback |
| YAML hand-edit corruption | Low | Prefer Unity MCP with correct scene open |

## Rollback Plan

`git checkout -- Assets/Scenes/Prototype.unity Assets/Scenes/Tutorial.unity` restores the exact
prior HUD. If a `HUDArt.prefab` was extracted, delete it too. No script, asset, or data
migration — rollback is total.

## Dependencies

- `Assets/_sprites/Escene UI.png` (guid `4d1c34e75cfa48b4499594399e05d055`, 13 slices) — present.
- Unity Editor live via MCP; `scene-lighting-integration` committed (satisfied).

## Size / Chaining

This change is **blocks-nothing and presentation-only** (no code, no data). It **likely exceeds
the 400-line review budget**: ~10 new UI objects × ~60–90 YAML lines × **2 scenes** is well past
400 → **chain by slice** (Prototype static art → Prototype meters/refill → Tutorial mirror), or
extract `HUDArt` as a prefab and review the prefab + two thin instantiations.

## Success Criteria

- [ ] `HUDArt` exists as a Canvas child immediately before `FadeOverlay` in both scenes.
- [ ] All listed slices placed in both scenes; `RefillButton` shows the loop glyph.
- [ ] Non-square slices keep aspect at 16:9 and a wider aspect (screenshots).
- [ ] Round-end fade still covers all new art.
- [ ] No change to lives/patience/coins/refill values; `read_console` clean.
- [ ] No script files modified.
