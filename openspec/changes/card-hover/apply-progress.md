# Apply Progress — card-hover

## Change summary
Hand cards now react to the mouse hovering them: a scale lift + a z-order raise
above overlapping neighbours, restored cleanly on leave so repeated hovering never
drifts the fan order. Touch surface is limited to `CardView.cs` and two new tunables
in `CardVisualConfig` (+`.asset`). No drag / park / refill / discard / slot logic changed.

## Mode
**Standard** (not Strict TDD). This change is pure Unity presentation — pointer events
and scale coroutines with no pure-logic seam to unit-test; acceptance is validated in
Play mode via live transforms + screenshots, per the task's own validation plan.

## Code changes — `Assets/Scripts/Cards/CardView.cs`
| Item | What |
|---|---|
| Implements | `IPointerEnterHandler`, `IPointerExitHandler` added to the class |
| `_hoverRoutine` | New coroutine field (scale-only tween) |
| `OnPointerEnter` | Guards `_dragging || _parked || _discarding`; `transform.SetAsLastSibling()`; `StartHover(_config.hoverScale)` |
| `OnPointerExit` | Same guard; `transform.SetSiblingIndex(_slotIndex)` (restores fan order); `StartHover(1f)` |
| `StartHover` / `ScaleTo` | Scale-only eased tween of `rect.localScale` (eases with `reLayoutEasingCurve`, so hover needs only 2 new tunables). NEVER touches `anchoredPosition` |
| `StopHover` | Cancels the routine (near `StopMove`) |
| `OnBeginDrag` | Calls `StopHover()` + `transform.SetSiblingIndex(_slotIndex)` before the existing `StopMove()` — hover never fights a drag |
| `PlaceInZone` | `StopHover()` added (park owns the scale) |
| `ConsumeFromZone` | `StopHover()` added (consume owns the scale) |
| `FlyLeftTo` | `StopHover()` added (refill fly owns the scale) |

## Config changes
- `Assets/Scripts/Cards/CardVisualConfig.cs`: new `[Header("Hover")]` block right after Drag —
  `hoverScale = 1.12f`, `hoverDuration = 0.12f`, both with tooltips.
- `Assets/ScriptableObjects/Cards/CardVisualConfig.asset`: `hoverScale: 1.12`, `hoverDuration: 0.12`
  (added right after `dragLiftOffset`; diff is purely additive — the other diff hunks in this
  file, `cardSize`/`parabola*`, are the pre-existing uncommitted `card-fan-tuning` work, not mine).

## Raycast finding
**No change needed.** The card prefab's raycast surface already has `m_RaycastTarget: 1`:
- `Card` root `Image` (fileID 7071562647158609164) → `m_RaycastTarget: 1` ✔
- `BG` child `Image` (8239790296895446617) → `1` ✔
- `Border` child `Image` (4686893465714449379) → `1` ✔
- (`Name` / `Icon` TMP texts are `0`, as expected — text is not the hit surface.)

So pointer enter/exit events fire without any prefab edit. **The prefab was NOT modified.**

## Validation — Unity MCP (instance `GameJamUni2@9eee0fc1`, Unity `6000.3.10f1`)
- `refresh_unity` (compile requested) → domain reload completed; `ready_for_tools=true`, `is_compiling=false`.
- **Console: clean** (0 errors / 0 warnings). Only entry is the unrelated
  `Account API did not become accessible within 30 seconds…` Unity-Services warning (same as prior changes).
- No TMP fallback churn: `LiberationSans SDF - Fallback.asset` is clean.

### Programmatic evidence (Play mode, `Prototype.unity`, 5-card hand)
Hovered via the real handlers (`OnPointerEnter`/`OnPointerExit` with a `PointerEventData`).

**Hover enter (middle card, Maíz):**
```
AFTER ENTER — rootChildren=5
sibling 0 => Pescado        scale=1.000 pos=(-360, -390)
sibling 1 => Pescado        scale=1.000 pos=(-180, -368)
sibling 2 => Pescado        scale=1.000 pos=( 180, -368)
sibling 3 => Cilantro       scale=1.000 pos=( 360, -390)
sibling 4 => Maíz           scale=1.120 pos=(   0, -360)   <-- last sibling + hoverScale
```
→ scale reached `1.120` (=`hoverScale`), card raised to last sibling (renders on top),
**position unchanged** `(0,-360)`.

**Hover exit:** `siblingIndex now=2` (restored synchronously); after the tween all
`scale=1.000`, positions unchanged.

**Repeated hovering (cards 0,4,1,3,2 in a row)** — compared sibling order against
each card's own `_slotIndex` via reflection. Every enter raised the hovered card to the
last sibling; every exit restored the exact baseline order. `FINAL` == `baseline`:
```
baseline 1:Pescado(slot0) 2:Pescado(slot1) 3:Maíz(slot2) 4:Pescado(slot3) 5:Cilantro(slot4)
FINAL    1:Pescado(slot0) 2:Pescado(slot1) 3:Maíz(slot2) 4:Pescado(slot3) 5:Cilantro(slot4)
```
Fan order is intact after hovering several cards in a row. ✔

**Drag not fought by hover:**
```
enter:      sib=4  hoverRoutineActive=True
beginDrag:  sib=2  hoverRoutineActive=False  dragging=True  scale=1.120
drag:       pos=(-460, 0)          (card follows the pointer)
endDrag:    dragging=False sib=2 scale=1.000
```
→ `OnBeginDrag` cancels the hover routine, restores the sibling index, and drag proceeds normally. ✔

### Visual evidence (screenshots)
- `Assets/Screenshots/card-hover_hand_before.png` and `..._hand_after.png` — the meaningful pair.
  - **IMPORTANT:** the committed hand is currently OFF-SCREEN (see finding below). To capture the
    hand at all I raised `HandRoot.anchoredPosition` to `(0,600)` **in Play mode only** — an
    ephemeral runtime tweak that is discarded when Play mode stops; no scene/prefab file was saved.
  - **before:** five cards on the fan — Tomate, Limón, **Pescado**, Limón, Maíz. The middle Pescado
    card is *overlapped* by both neighbouring Limón cards (its frame is partly hidden).
  - **after hover (Pescado):** Pescado is visibly **larger (1.12×)** and now renders **on top of its
    neighbours** — its full red-bordered frame covers the neighbours' edges and its top edge sits
    above them. Neighbours are unchanged in size/position.
- `Assets/Scripts/.../card-hover_before.png` / `card-hover_after.png` were captured first and show the
  hand **below the screen** (no cards) — kept only as a record of the off-screen finding; ignore them.

## Finding (pre-existing, OUT OF SCOPE — not caused by this change)
The **uncommitted** `Card.prefab` edit changed the card root `RectTransform` anchor from
`(0.5, 0.5)` → `(0.5, 0)` (and added `anchoredPosition.y = 540`). Because `HandRoot` is a
full-canvas stretch rect, the card's bottom anchor now sits at the screen's bottom edge, so
`GetSlotPosition`'s negative Y (≈ -360…-390) lands the whole hand **below the visible screen**
(live world corners y ∈ [-519, -232]). This is unrelated to hover (my code never touches anchors
or `HandRoot` and the slot math is untouched) and was left alone per the "do not change the slot
system / anything else" constraint. Whoever owns the prefab/scene layout work should either restore
the root anchor to `(0.5, 0.5)` or re-anchor `HandRoot`. **Flagging for the user.**

## Left for the user
- Tune `hoverScale` (default `1.12`) and `hoverDuration` (default `0.12`) in the Inspector on
  `CardVisualConfig.asset` — both were applied with the requested defaults.
- Decide on the off-screen hand finding above (prefab/scene layout, distinct from hover).

## Status
- Code + config complete; compile clean; hover and its guards verified in Play mode.
- Not committed (per instructions). Not saved to engram (openspec is the artifact store).
