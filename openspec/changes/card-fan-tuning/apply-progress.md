# Apply Progress — card-fan-tuning

## Change summary
Enlarge, lower, and flatten the gameplay hand fan. **Pure data change** in the shared
`CardVisualConfig.asset`; no C# touched. The fan is a quadratic Bezier through
`parabolaStart / parabolaControl / parabolaEnd` (canvas ref 1920×1080).

## Values changed (before → after)

| field | before | after |
|---|---|---|
| `cardSize` | `{x: 140, y: 200}` | `{x: 180, y: 257}` |
| `parabolaStart` | `{x: -280, y: -300}` | `{x: -360, y: -390}` |
| `parabolaControl` | `{x: 0, y: -150}` | `{x: 0, y: -330}` |
| `parabolaEnd` | `{x: 280, y: -300}` | `{x: 360, y: -390}` |

Only these 4 fields changed. `dealOrigin`, durations, easing curves, zone rects, colours
and all sprite/prefab refs are untouched (verified via `git diff` — the diff is exactly
the 4 fields above).

### Resulting fan geometry (verified in play mode via live RectTransforms)
- Apex (mid card): anchored `(0, -360)` → visible apex y = `0.5*control.y + 0.25*(start.y+end.y)`
  = `0.5*(-330) + 0.25*(-390 + -390)` = **-360** ✔ (was -225 → bulge dropped from ~75 px to ~30 px = flatter).
- Outer cards: anchored `(±360, -390)`.
- Card size: `180 × 257` in a `1920×1080` canvas, `CanvasScaler` referenceResolution `1920×1080`, `ScreenSpaceOverlay`.
- Outer card world rect: `x[510,690]` / `x[1230,1410]`, `y[22,279]` (origin bottom-left).
- Mid card top: y ≈ 308.5; card bottom clearance ≈ **22 px** above the screen edge (not clipped).

## Files changed
| File | Action |
|---|---|
| `Assets/ScriptableObjects/Cards/CardVisualConfig.asset` | Modified — 4 fields |
| `openspec/changes/card-fan-tuning/apply-progress.md` | Created — this report |

## Validation
- **Unity MCP**: live, instance `GameJamUni2@9eee0fc1`, Unity `6000.3.10f1`.
- **Editor state**: `assets.is_updating=false`, `ready_for_tools=true`, no compilation pending.
- **Console (post-change + post-refresh)**: clean. Only entry is an unrelated
  `Account API did not become accessible within 30 seconds...` warning (Unity Services
  account network call, unrelated to this change). No errors, no warnings from the asset.
- **Play mode** on `Assets/Scenes/Prototype.unity` with a full 5-card hand
  (Cebolla, Arroz, Limón, Maíz, Tomate) — hand `Count=5`, all 5 `CardView` instances present
  and active, laid out on the new parabola.
- **Screenshot**: `Assets/Screenshots/card-fan-tuning_prototype_5cards-1.png` (1280×720, gitignored).
  - A first capture `card-fan-tuning_prototype_5cards.png` was taken a beat too early (caught
    the deal reveal before the cards faded in) and shows an empty hand — kept for reference but
    the `-1` capture is the valid one.

## Sanity checks (clipping / zone overlap)
- **Screen bottom**: outside cards' bottom = y 22 px (screen 1080) → **not clipped**, ~22 px clearance. ✔
- **Center (pot) drop zone** `CenterDropZone`: `x[861,1081] y[477,697]`. Tallest card top = y 308.5 →
  **no overlap** (168 px gap). The pot artwork's stone base visually sits below the drop-zone rect, so
  the fan reads as "in front of" the pot without covering the zone. ✔
- **Corner (trash) drop zone** `CornerDropZone`: `x[18,278] y[-51,209]`. Outer cards start at x 510 →
  **no horizontal overlap**. ✔
- **Side margins**: leftmost card x 510, rightmost x 1410 in a 1920-wide canvas → ample margin. ✔

## Anything that looked off
1. **Cards now abut exactly.** Slot spacing is 180 px and `cardSize.x` is now 180 px, so adjacent cards
   share an edge (touching, no gap). It reads fine with the white card border, but if you want visible
   separation you'd widen the parabola x-extents or shrink `cardSize.x` slightly — **data only**, tune in
   the Inspector.
2. **First capture timing.** The very first screenshot landed during the fade/deal reveal and showed no
   cards; the hand was correctly dealt moments later. Not a bug — just capture timing (fade-in is
   async). Re-captured.
3. **TMP fallback churn.** `LiberationSans SDF - Fallback.asset` came back dirty with a
   `serializedVersion 6→8` Material re-serialization (109+/66−). Reverted with `git checkout` per the
   task instruction; it is now clean.

## Shared config / other scenes
`CardVisualConfig.asset` is the single shared config, so **Tutorial.unity** picks up the same fan
automatically. Validation was performed on `Prototype.unity` only (per the task); the Tutorial scene
was not separately play-tested.

## Status
- All assigned data changes applied and validated. **Done.**
- Not committed (per instructions). Not saved to engram (openspec is the artifact store).
