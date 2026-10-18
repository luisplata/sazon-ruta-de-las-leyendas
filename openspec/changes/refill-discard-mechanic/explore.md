# Exploration — `refill-discard-mechanic`

> Investigation only. No script, scene, prefab, or ScriptableObject was modified.
> All findings are from reading source + scene YAML on disk.

## Overview

The ask is to change **REFILL** from a *trash-dump valve* into a **hand-cycle tool**:
press Refill → the **entire current hand** flies **left** to a discard pile → the game
**re-deals a full hand of 5**. Cards the player already parked in the **POT (center/cook
zone)** must **stay**. The strategic point: park what you can in the pot, then cycle the
hand to fish for the last ingredient you need.

Today Refill does something much narrower and inert-feeling: it only drains the bottom-left
**trash corner zone** (max 2 parked) and tops the hand up. The hand itself is never discarded.
This is the literal gap the change closes.

---

## Current behavior (with file/line references)

### `Refill()` — `Assets/Scripts/Cards/HandController.cs:134-146`

```csharp
public void Refill()
{
    if (_refillsUsed >= config.maxRefillsPerGame) return;   // hard no-op at 2/2
    if (cornerZone.Count > 0)
    {
        playerView?.OnDiscard();
        ConsumeZone(cornerZone);       // spin+scale-to-0 the trash queue
        _refillsUsed++;
        UpdateRefillLabel();
    }
    TopUpHand();                        // fill missing hand slots from the draw cursor
}
```

- Only the **corner (trash) zone** is drained. **Center (cook) cards are untouched** —
  confirmed (only `cornerZone` appears here).
- **Empty-trash Refill is FREE** (does not increment `_refillsUsed`).
- After draining, `TopUpHand()` deals `maxHandSize - _cards.Count` cards. The hand is
  refilled, never discarded.

### Supporting pieces

- `_refillsUsed` (`HandController.cs:62`, reset at `:111`) gates both `Refill()` (`:136`)
  and corner park acceptance (`TryParkInZone`, `:279`). At 2/2 the trash zone *closes*.
- `UpdateRefillLabel()` (`:259-263`) prints `REFILLS: {used}/{max}`.
- `TopUpHand()` (`:208-221`) deals the `missing` cards onto full-fan slots then re-layouts
  the pre-existing cards via `SetSlot`.
- `StartDeal()` (`:96-125`) resets zones + counter, destroys existing hand cards, deals 5.
- `TryParkInZone()` (`:273-290`) removes a card from `_cards` **synchronously**, re-layouts,
  and starts `PlaceInZone`; the corner branch is special-cased for the refill gate at `:279`.
- `ConsumeZone()` (`:297-301`) → `CardView.ConsumeFromZone()` drives the visual consume.
- `ClearZone()` (`:307-311`) destroys parked cards (board reset).
- `DealOne()` (`:335-353`) instantiates `config.cardPrefab`, sets `anchoredPosition =
  config.dealOrigin`, calls `card.DealIn()`.
- Draw cursor: `_drawOrder` / `_dealIndex` (`:46-53`), `NextCard()` (`:361-367`) advances a
  wrap-around pointer. **Reset to 0 only in `StartDeal`; never re-shuffled mid-game.**
- Trigger: the `RefillButton` `onClick` calls `HandController.Refill` — Prototype
  `Assets/Scenes/Prototype.unity:2555-2560`, Tutorial `Assets/Scenes/Tutorial.unity:2686`.
- `PlayerView.OnDiscard()` (`Assets/Scripts/Characters/PlayerView.cs:67`) fires only when
  trash is actually dumped. Trigger name `Discard` (`:17`).

### Existing card animations — `Assets/Scripts/Cards/CardView.cs`

| Coroutine | Lines | What it does |
|---|---|---|
| `DealIn` | 83-91 | `MoveTo(dealOrigin → slot)` |
| `SetSlot` | 100-116 | `MoveTo(current → slot)`; no-ops if `_parked`/`_discarding` (`:102`,`:110`) |
| `ReturnToSlot` | 122-131 | `MoveTo(current → slot)` |
| `ParkTo` | 217-238 | lerp pos + scale, **re-parent under zone**, lock raycasts |
| `DiscardCenter` | 245-265 | eased move to zone-local **zero** + scale→0, then destroy |
| `DiscardCorner` | 273-291 | spin (rotation curve) + scale→0, then destroy |
| `MoveTo` | 294-303 | **generic** eased `anchoredPosition` lerp — the reusable primitive |

There is **no** existing "fly to an arbitrary point and destroy" coroutine. `MoveTo` is the
closest primitive; `DiscardCenter`/`DiscardCorner` are the closest *destroy-at-end* shapes.

### Zone semantics — `Assets/Scripts/Cards/DropZone.cs`

- `enum ZoneKind { Center = cook, Corner = trash }` (`:16-17`).
- Capacity by kind: Center → `config.maxCook` (3), Corner → `config.maxTrashPerGame` (2) (`:53`).
- `Accept` (`:69-74`) rejects when `locked` or at capacity; `GetParkedSlot` (`:101-105`) lays
  out slots horizontally.

### Config — `Assets/Scripts/Cards/CardVisualConfig.cs` (asset: `Assets/ScriptableObjects/Cards/CardVisualConfig.asset`)

- `maxHandSize = 5` (`:15`), `maxCook = 3` (`:45`), `maxTrashPerGame = 2` (`:49`),
  `maxRefillsPerGame = 2` (`:53`), `parkedScale = 0.55` (`:56`), `parkedSpacing = 80` (`:59`).
- `dealOrigin = (280, -380)` (`:21`), fan `parabolaStart (-280,-300)` / `Control (0,-150)` /
  `End (280,-300)` (`:30-36`). **The hand fan lives in roughly x ∈ [-280, 280], y ∈ [-300,-150].**
- Discard timings: `centerDiscardDuration = 1` (`:74`), `cornerDiscardDuration = 1` (`:82`).
- **No anchor/target field for a left discard pile exists today.**

### Scene layout (Canvas, Screen Space Overlay, 1920×1080 match 0.5)

- `Canvas` rect `Prototype.unity:2781-2806`; children include `Hand`, `CenterDropZone`,
  `CornerDropZone`, `TopBar`, `RefillLabel`, `DishSlot`.
- `Hand` is a full-canvas stretch root (`:2861-2864`) — **hand-space ≈ canvas-space**, so a
  card's `anchoredPosition` and a Canvas-child's `anchoredPosition` are directly comparable.
- `CornerDropZone` = **bottom-left** at **`(-717, -310)`**, size 260×260
  (Prototype `:1242-1243`; Tutorial `:1096`); parent = Canvas.
- `RefillLabel` sits at **`(-717, -140)`** (Prototype `:2223`; Tutorial `:2349`) — directly
  above the corner zone.
- `CenterDropZone` at `(0, 0)`, 220×220 (Prototype `:1470-1471`).
- `HandController` wiring: `Prototype.unity:2836-2846` (`cornerZone: 789115914`).
- **A left-side anchor already exists in spirit: the bottom-left region at x ≈ -717 is the
  red trash zone + the REFILLS label.** No empty dedicated "discard pile" RectTransform exists.

### Game loop / game-over timing — `Assets/Scripts/GameLoop/GameLoopController.cs`

- `ServeFood()` (`:245-286`) resolves the cook; win → `WinRound()` (`:291-295`) → sets
  `_roundActive = false` → `DelayedFade(0.5s)` → scene load.
- `Bite()` (`:315-327`) on last life → GAME OVER → `DelayedFade(0.5s)` → load.
- `ReactionShowDelay = 0.5f` (`:74`). During that 0.5s **input is NOT yet blocked** (the fade
  overlay only becomes a raycast blocker once alpha > 0 — `FadeController.cs:83`). So a Refill
  click *is* reachable during the reaction window.
- `Refill()` **does not check `_roundActive`** today (HandController has no such reference).

### Tutorial coupling — `Assets/Scripts/Tutorial/TutorialController.cs`

- Step 2 `RoleColors` completes on `cornerZone.Count >= 1` (`:177`).
- Step 4 `TrashRefill` completes via `TrashThenRefill()` (`:324`) = `_sawTrash && _sawRefillDump`.
- `ObserveZones()` (`:268-278`) infers "a refill was used" from a **corner drain edge**
  (`_prevCornerCount >= 1 && cornerZone.Count == 0`). **This is coupled to Refill touching
  the corner zone.**
- `MaxTutorialRefills = 2` const (`:82`) mirrors `maxRefillsPerGame` for the "no refills hint"
  (`NoRefillsHint`, `:83`, `:332-338`).
- Step 4 text: *"...pulsa REFILL para descartarla y llenar tu mano. ¡Los refills son limitados!"*
  (`:230`) — assumes the old behavior + limit.
- **No EditMode/PlayMode test references Refill** — the only test file is
  `Assets/Tests/EditMode/AnimParamsTests.cs`. Changing Refill breaks no automated test.

---

## New mechanic (target behavior)

1. **Refill discards the ENTIRE current hand** (all cards in `_cards`), not just the corner.
2. Each discarded hand card **flies LEFT** to a discard pile, then is destroyed.
3. After the discard animation completes, **re-deal a full hand of `maxHandSize` (5)**.
4. Cards already parked in the **POT (center zone)** **stay untouched**.
5. Refill becomes a **hand-cycle** tool ("fish for the one ingredient you still need").

---

## Open questions — answers + recommendations

### 1. Refill limit: keep, change, or unlimited?

**Recommendation: KEEP the limit** (default `maxRefillsPerGame = 2`; make raising it a
post-playtest tuning, not this change).

Reasoning: the new Refill is *far stronger* — it discards 5 and deals 5, so unlimited use
would let the player brute-force the draw order until the exact ingredient appears, dissolving
the puzzle. The scarce valve is precisely what makes "park what you can, then cycle" a
*decision* instead of a free reroll. Since the value is already a `CardVisualConfig` field,
a jam-feel tune (2 → 3) later is a one-number change with no code edit. **Do NOT hardcode
"unlimited".** Keep the tutorial's "los refills son limitados" message honest.

> Tradeoff noted: if playtest shows the hand-cycle is the *fun*, raise to 3–4; keep the cap.

### 2. Fate of `cornerZone` (trash)

**Recommendation: KEEP `cornerZone` operable (do not delete it in this change), and make
Refill drain it too.** Rename nothing; keep the object, wiring, and tutorial intact.

Reasoning:
- Deleting `cornerZone` cascades: `DropZone.ZoneKind`, `CardView._corner` + the corner branch
  in `OnEndDrag` (`:203-207`) + `DiscardCorner`, `HandController.cornerZone` + `ClearZone` +
  the `:279` special-case, both scenes' `CornerDropZone` objects, and **two tutorial steps**
  (RoleColors `:177`, TrashRefill `:179`,312-324) plus `ObserveZones`. That is a large,
  separate refactor — wrong for a jam.
- In the new flow, the corner remains a useful **"spot-discard one card without spending a
  refill"** affordance, and Refill flushes it so nothing strands at 2/2.
- **Reframe it: the red corner *is* the left discard pile.** Hand cards fly to that bottom-left
  region on Refill; manually parked trash already flies there. One region, two affordances —
  coherent and minimal.

If the team truly wants the corner gone, that is a **follow-up change**, not this one. This
change should only redefine *what Refill discards*.

### 3. Discard pile location / anchor

**Recommendation: no new scene GameObject. Add a `Vector2 discardTargetPosition` to
`CardVisualConfig`**, defaulted to the corner-zone region — **`(-717, -310)`** (the existing
`CornerDropZone` anchoredPosition; same on both scenes). Cards animate to that hand-space point
(hand-space ≈ canvas-space, see layout) then are destroyed.

- **Fly-off-and-destroy is enough at jam scale.** A persistent pile sprite is optional polish
  and can be added later without changing the mechanic.
- Rationale for reusing the corner location: it already reads as "the discard area", it's the
  same spot on Prototype and Tutorial, and it needs **zero scene editing** — the 10x-simplest
  path. (A dedicated pile RectTransform is a nicety, not a requirement, and would mean editing
  two scenes.)

### 4. Animation approach

**Recommendation: add ONE new coroutine to `CardView`, e.g.
`FlyLeftTo(Vector2 targetLocalPos, float duration, AnimationCurve ease)`**, modeled on
`DiscardCenter` (`:245-265`): eased `anchoredPosition` lerp + scale→0, then `Destroy(gameObject)`.

- Reuses the exact idioms already present: `MoveTo` (`:294-303`), the `Time.deltaTime` loop
  pattern, and `config` easing curves.
- Set `_discarding = true` on entry so `SetSlot`/`ReturnToSlot` (already guarded at `:102`,
  `:110`, `:124`) can never start a competing move.
- **Timing:** discard must finish **before** the re-deal. Add
  `refillDiscardDuration` (or reuse `cornerDiscardDuration = 1`s) and have `HandController`
  wait that long (coroutine) before dealing. Keep it short (0.4–0.6s feels snappier than the
  1s consume); the current 1s consumes are for a different beat.
- **Do NOT** repurpose `DiscardCorner` (it spins in place and is zone-bound), and do **not**
  reuse `ConsumeZone` (it deletes immediately and assumes a zone parent).

### 5. Hand-state timing (avoid discard/re-deal overlap)

**Recommendation: `Refill()` starts a coroutine and becomes asynchronous; the re-deal happens
only after the discard completes.**

Sequence inside `RefillRoutine()`:
1. Guard re-entrancy with a `bool _refilling` (ignore clicks while true) **and** keep the
   `_refillsUsed >= max` gate.
2. `_refillsUsed++` + `UpdateRefillLabel()` **immediately** (label reflects the spent refill).
3. `playerView?.OnDiscard()` (was trash-only; now the hand-discard reaction).
4. **Snapshot `_cards` into a local list and `_cards.Clear()` synchronously.** Clearing first
   guarantees no `SetSlot` re-layout runs on cards that are leaving.
5. If `cornerZone.Count > 0` → `ConsumeZone(cornerZone)` (existing trash dump; keeps tutorial
   semantics).
6. Call `FlyLeftTo(config.discardTargetPosition, ...)` on each snapshot card.
7. `yield return new WaitForSeconds(discardDuration)` (or wait on completion).
8. `for i in 0..maxHandSize: DealOne(i)` — the 5 fresh cards fan onto the full hand.
9. `_refilling = false`.

- **Center/pot cards are never in `_cards`** (removed synchronously at park, `:286`), so they
  are structurally untouched — requirement satisfied for free.
- **Clear `_cards` BEFORE the animation, not after**, so the empty-hand state is consistent
  and no re-layout fights the flying cards.

### 6. Draw order / `_dealIndex` cursor — still meaningful?

**Recommendation: KEEP the cursor as-is. Do NOT re-shuffle on Refill.**

- Every Refill now consumes exactly 5 positions via `NextCard()` (`:361-367`), which is
  *more* predictable than the old variable top-up. The wrap-around already lets discarded
  cards return eventually — which is exactly the "cycle and fish" fantasy.
- Re-shuffling would break the deliberate invariant documented at `:43-53` ("never re-shuffled
  mid-game") and the seeded-debug reproducibility (`ShuffleFisherYates`, `:373-383`).
- No change required here beyond `Refill` using the same `DealOne` path.

---

## Affected files

| File | Why |
|---|---|
| `Assets/Scripts/Cards/HandController.cs` | `Refill()` rewritten to discard whole hand + wait + re-deal; new `_refilling` guard; update header summary (`:6-11`, `:127-146`). |
| `Assets/Scripts/Cards/CardView.cs` | New `FlyLeftTo` coroutine; ensure `_discarding` guards it. |
| `Assets/Scripts/Cards/CardVisualConfig.cs` | New `discardTargetPosition` (+ optional `refillDiscardDuration`); update `maxRefillsPerGame` tooltip. |
| `Assets/ScriptableObjects/Cards/CardVisualConfig.asset` | New serialized values (defaults). |
| `Assets/Scripts/Tutorial/TutorialController.cs` | Step-4 text/objective may need rewording; verify `ObserveZones`/`TrashThenRefill` still hold. |
| `Assets/Scenes/Prototype.unity`, `Assets/Scenes/Tutorial.unity` | Only if a dedicated pile anchor is chosen — **avoided** by the config-target recommendation. |

## Risks

1. **Tutorial softlock if `cornerZone` is removed** — `RoleColors` (`:177`) needs a corner
   park and `ObserveZones` (`:268-278`) infers "refill used" from a corner drain. Removing the
   corner breaks Step 4. Mitigation: keep corner (recommended).
2. **Empty-refill rule changes.** Today an empty-trash Refill is free (`:138-144`). In the new
   model a Refill always discards the hand, so it should always consume one. Edge case: hand
   empty (all parked in pot) → define as still consuming (simplest) or no-op. **Decide and
   document.**
3. **Rapid double-click Refill** → two routines deal 10 cards. Mitigation: `_refilling` guard.
4. **Refill during win/lose reaction window.** `Refill()` doesn't check `_roundActive`; the
   fade only blocks input once alpha>0, and the fade is delayed 0.5s (`ReactionShowDelay`).
   A Refill coroutine can start, then the scene loads mid-animation. Low harm (scene destroy
   stops the coroutine) but flag. Mitigation: ignore Refill while `_roundActive == false`, or
   accept as harmless.
5. **"Discard then fish" could trivialize the puzzle** if the limit is raised/removed.
   Mitigation: keep the cap (Q1).
6. **Visual collision** between freshly-parked manual trash and the flying hand cards at the
   same bottom-left point. Cosmetic at jam scale.
7. **`maxTrashPerGame`/capacity unchanged** but the corner is now also flushed by Refill —
   verify no double-count with the tutorial's corner-edge detector (it still sees a >=1→0
   transition, so it should still latch).

## Recommended 10x-simplest implementation approach

No new architecture, no new scene objects, no event bus. Four small edits:

1. **`CardVisualConfig`**: add
   `public Vector2 discardTargetPosition = new Vector2(-717f, -310f);`
   (and optionally `public float refillDiscardDuration = 0.5f;`). Update the `maxRefillsPerGame`
   tooltip to say Refill now discards the whole hand.
2. **`CardView`**: add `FlyLeftTo(Vector2 target, float duration, AnimationCurve ease)` —
   set `_discarding = true`; `StopMove()`; ease `anchoredPosition` from current → target while
   scaling to 0; `Destroy(gameObject)`. (Copy the shape of `DiscardCenter`.)
3. **`HandController.Refill()`**: replace the body with a `StartCoroutine(RefillRoutine())`
   that: gates on `_refillsUsed` + `_refilling`; increments + relabels; fires `OnDiscard()`;
   snapshots `_cards` and `_cards.Clear()`; `ConsumeZone(cornerZone)` if any; `FlyLeftTo` each
   snapshot card; waits `refillDiscardDuration`; then `DealOne(i)` × `maxHandSize`. Keep
   `TopUpHand()` for the cook path (`PrepareFood` still uses it).
4. **Play mode**: press Refill, confirm the hand flies left, 5 new cards deal, pot cards stay,
   counters/console clean (`read_console`). Then decide the tutorial Step-4 wording.

Fallback only if the team wants the corner gone: **remove the corner as a separate follow-up
change** (touches tutorial + both scenes); out of scope here.

**Non-goals:** no removal of `cornerZone`, no new scene RectTransform, no re-shuffle, no change
to `PrepareFood`/cook flow, no change to `maxCook`, no new DI/events.

---

## Evidence index

- `Assets/Scripts/Cards/HandController.cs`: `Refill` `:134-146`; `TopUpHand` `:208-221`;
  `StartDeal` `:96-125`; `TryParkInZone` `:273-290` (corner gate `:279`); `ConsumeZone`
  `:297-301`; `ClearZone` `:307-311`; `DealOne` `:335-353`; `NextCard`/`_dealIndex` `:46-53`,
  `:361-367`; `_refillsUsed` `:62`; `UpdateRefillLabel` `:259-263`.
- `Assets/Scripts/Cards/CardView.cs`: `DealIn` `:83-91`; `SetSlot` `:100-116`;
  `ReturnToSlot` `:122-131`; `ParkTo` `:217-238`; `DiscardCenter` `:245-265`;
  `DiscardCorner` `:273-291`; `MoveTo` `:294-303`; corner drag branch `:203-207`.
- `Assets/Scripts/Cards/DropZone.cs`: `ZoneKind` `:16-17`; `MaxCount` `:53`; `Accept` `:69-74`.
- `Assets/Scripts/Cards/CardVisualConfig.cs` + `.asset`: `maxHandSize` `:15`;
  `dealOrigin` `:21`; parabola `:30-36`; `maxCook` `:45`; `maxTrashPerGame` `:49`;
  `maxRefillsPerGame` `:53`; `parkedScale` `:56`; `cornerDiscardDuration` `:82`.
- `Assets/Scripts/GameLoop/GameLoopController.cs`: `ServeFood` `:245-286`; `WinRound`
  `:291-295`; `Bite` `:315-327`; `ReactionShowDelay` `:74`; `DelayedFade` `:369-373`.
- `Assets/Scripts/GameLoop/FadeController.cs`: input blocking `:83`.
- `Assets/Scripts/Characters/PlayerView.cs`: `OnDiscard` `:67`; trigger `:17`.
- `Assets/Scripts/Tutorial/TutorialController.cs`: `MaxTutorialRefills` `:82`;
  `ObserveZones` `:268-278`; `TrashThenRefill` `:324`; hint `:332-338`; `RoleColors` `:177`;
  Step-4 text/obj `:230-231`.
- `Assets/Scenes/Prototype.unity`: `CornerDropZone` `:1167`, pos `:1242-1243`;
  `CenterDropZone` `:1395`, pos `:1470-1471`; `RefillLabel` `:2201`, pos `:2223`;
  `RefillButton` `onClick→Refill` `:2555-2560`; `Canvas` scaler `:2747-2752`;
  `HandController` wiring `:2836-2846`; `Hand` rect `:2861-2864`.
- `Assets/Scenes/Tutorial.unity`: `CornerDropZone` pos `:1096`; `RefillLabel` pos `:2349`;
  `RefillButton` `onClick→Refill` `:2686`.
- Tests: only `Assets/Tests/EditMode/AnimParamsTests.cs` exists — **no Refill test coupling.**
