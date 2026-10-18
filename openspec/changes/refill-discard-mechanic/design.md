# Design: Refill as Hand-Cycle Discard

## Context

Refill today only drains the trash corner and tops up (`Refill` `:134-146`); the hand is never
discarded, so players cannot fish for a missing ingredient. The corner region `(-717,-310)` already
exists in both scenes and the hand fan is canvas-space ⇒ no scene object is needed.

## Goals / Non-Goals

**Goals**: one press discards the whole hand (fly left → destroy), then re-deals `maxHandSize` from the
existing `_dealIndex` cursor; pot cards untouched; budget charged only when ≥1 card was discarded; the
corner stays operable and flushes free; `_refilling` guard; label/tooltips describe the hand-cycle.
**Non-Goals**: removing `cornerZone` (follow-up), new scene objects, re-shuffle, HUD art, cook flow, DI/events.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|---|---|---|---|
| Discard destination | `CardVisualConfig.discardTargetPosition = (-717,-310)` | New pile RectTransform (2 scene edits) · derive from `cornerZone.Rect.anchoredPosition` | Zero scene edits; decoupled from the zone a follow-up deletes. Duplicates a coordinate (screenshot-verified). |
| Draw order | Keep `_dealIndex` wrap-around (`NextCard` `:361-367`) | Re-shuffle on Refill | Preserves the `:43-53` invariant + seeded reproducibility; the 5-step advance IS "cycle and fish". |
| Corner zone | Keep; flush unconditionally and free | Delete · budget-gate the flush | Removal cascades into tutorial + both scenes; a free flush prevents stranding and keeps the tutorial drain-detector alive at 2/2. |
| Budget | Charge only when ≥1 hand card is discarded; empty hand = free `TopUpHand()` | Charge per press · unlimited | Decisions #3/#4: the valve gates the cycle, not trash. |
| Corner park gate (`TryParkInZone` `:279`) | **Remove** | Keep the 2/2 gate | Refills can now be spent without trashing, so the gate makes Tutorial step 2 (`RoleColors` `:177`) unsatisfiable → softlock; stranding is impossible once the flush is free. |
| Ordering | Async routine; wait `cornerDiscardDuration` + 1 frame, then deal | Deal immediately | No re-layout fights the flyers; the extra frame covers the fly loop's exit. |
| Round-end window | Accept | `GameLoopController.IsRoundActive` | No game-loop reference exists; the reload kills the coroutine and `StartDeal` resets the budget. |

## Sequence — one Refill press

```mermaid
sequenceDiagram
  H->>H: Refill(): _refilling? return; _refilling = true
  opt cornerZone.Count > 0
    H->>H: ConsumeZone(cornerZone)  (free flush: spin/shrink/destroy)
  end
  alt hand non-empty AND _refillsUsed < max
    H->>H: _refillsUsed++ ; UpdateRefillLabel() ; OnDiscard()
    H->>H: snapshot _cards ; _cards.Clear()   (clear BEFORE flying)
    H->>H: each card FlyLeftTo(discardTargetPosition)  (parallel)
    H->>H: wait cornerDiscardDuration + 1 frame
    H->>H: DealOne(i) x maxHandSize  (cursor advances 5)
  else hand empty
    H->>H: TopUpHand()  (free, no charge)
  end
  H->>H: _refilling = false
```

## Interfaces / Contracts

```csharp
// CardView
public void FlyLeftTo(Vector2 targetAnchoredPos);            // guard _discarding; StopMove; blocksRaycasts=false
IEnumerator FlyLeftRoutine(Vector2 target, float duration);  // lerp pos (reLayoutEasingCurve), scale->0 (cornerDiscardScaleCurve), Destroy
// HandController
public void Refill();                       // if (_refilling) return; _refillRoutine = StartCoroutine(RefillRoutine());
IEnumerator RefillRoutine();
bool _refilling; Coroutine _refillRoutine;  // new fields
// CardVisualConfig
public Vector2 discardTargetPosition = new Vector2(-717f, -310f);  // THE one new field
```

Timing/easing reuse `cornerDiscardDuration` (1 s), `reLayoutEasingCurve`, `cornerDiscardScaleCurve`;
`FlyLeftTo` never calls `_zone.Release` (hand cards were never parked, unlike `DiscardCenter` `:263`).
The label refreshes on charge, so the HUD is honest during the ~1.5 s.

## File Changes

| File | Action | Description |
|---|---|---|
| `Assets/Scripts/Cards/CardVisualConfig.cs` | Modify | + `discardTargetPosition`; retool `maxRefillsPerGame`/`maxTrashPerGame` tooltips. **No rename** — the name is serialized in the `.asset`; renaming drops the value. |
| `Assets/ScriptableObjects/Cards/CardVisualConfig.asset` | Modify | `discardTargetPosition: {x: -717, y: -310}`. |
| `Assets/Scripts/Cards/CardView.cs` | Modify | + `FlyLeftTo` / `FlyLeftRoutine`. |
| `Assets/Scripts/Cards/HandController.cs` | Modify | `Refill()` → `RefillRoutine()`; `_refilling` + `_refillRoutine`; `StartDeal` stops it; remove the `:279` gate; label `REFILLS (MANO): {used}/{max}`; doc comments. |
| `Assets/Scripts/Tutorial/TutorialController.cs` | Modify | Mirror the new Step-4 text in `DefaultStepConfigs`; note `ObserveZones` still latches. |
| `Assets/Scenes/Tutorial.unity` | Modify | `steps[3]` text/objective (`:2149`/`:2152`) is **scene-serialized** — code defaults alone won't change it. |
| `Assets/Scenes/Prototype.unity` | Unchanged | `REFILLS: 0/2` is overwritten by `Start()`. |

## Edge Cases

- **Empty hand**: free `TopUpHand()`, no charge, corner flushed. Unreachable at 2/2 (corner cap 2 + center cap 3 ⇒ hand ≥ 2) → no dead state.
- **Budget exhausted, hand non-empty**: no cycle; corner still flushes free; label stays 2/2.
- **Double press**: `_refilling` returns → 5 cards, never 10. **START mid-refill**: `StartDeal` stops the routine and clears `_refilling` first.
- **Card mid-drag**: still in `_cards`; `_discarding` bails the drag handlers; shrink starts from `dragLiftScale`.
- **Refill in the 0.5 s win/lose window**: accepted (reload kills it; budget resets).
- **Tutorial early burn**: gate removed ⇒ step 2 parkable; free flush at 2/2 ⇒ step-4 drain edge still latches. `UpdateNoRefillsHint` becomes inert (harmless).
- **Null/empty deck**: `DealOne` warns and deals nothing (unchanged); overlap with parked trash is cosmetic.

## Testing / Validation

No Refill test exists (only `AnimParamsTests.cs`); strict_tdd is OFF. Optional PlayMode test: after Refill,
`Count == maxHandSize`, budget 1/2, center untouched, corner flushed, double-press ⇒ 5. Primary validation:
Play mode + screenshots in **both** scenes (fly-left, scale→0, 5-card re-deal, pot intact, label width,
console clean).

## Rollback / Open Questions

`git checkout --` the four scripts, `CardVisualConfig.asset`, `Tutorial.unity` — additive config, no new scene
objects, so the old trash-only Refill returns exactly. No blocking questions.
