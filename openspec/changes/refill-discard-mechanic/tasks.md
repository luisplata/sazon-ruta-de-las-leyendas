# Tasks: Refill as Hand-Cycle Discard

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~85–130 semantic (config +3, CardView +22, HandController +30/−15, tutorial text ~0); +40 if optional test |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR (optional PR2 for Tutorial.unity only if the scene save churns fileIDs) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

```text
Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low
```

Small, single-concern diff (Refill = discard hand → redeal). No scene object added; the one
scene edit is a serialized text field.

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Hand-cycle Refill works in Play mode | PR 1 | Config + CardView + HandController; screenshot evidence |
| 2 | Tutorial copy matches new mechanic | PR 2 (only if scene churns) | Code default + `Tutorial.unity` steps[3]; depends on PR 1 |

## Phase 1: Config / Foundation

- [x] 1.1 [config] `CardVisualConfig.cs`: add `public Vector2 discardTargetPosition = new Vector2(-717f, -310f);`; retool `maxRefillsPerGame`/`maxTrashPerGame` tooltips to hand-cycle wording. **Do not rename** (serialized in `.asset`).
- [x] 1.2 [config] `Assets/ScriptableObjects/Cards/CardVisualConfig.asset`: set `discardTargetPosition: {x: -717, y: -310}`.

## Phase 2: Core Implementation

- [x] 2.1 [core] `CardView.cs`: add `FlyLeftTo(Vector2 target)` — guard `_discarding`, `StopMove()`, `group.blocksRaycasts=false`, start routine.
- [x] 2.2 [core] `CardView.cs`: add `IEnumerator FlyLeftRoutine(Vector2 target, float duration)` — lerp `anchoredPosition` via `reLayoutEasingCurve`, scale→0 via `cornerDiscardScaleCurve`, `Destroy`; never call `_zone.Release`.
- [x] 2.3 [core] `HandController.cs`: add `bool _refilling; Coroutine _refillRoutine;`; `Refill()` → guard + `_refillRoutine = StartCoroutine(RefillRoutine())`.
- [x] 2.4 [core] `HandController.cs`: implement `RefillRoutine()` per design sequence — free corner flush first; then charge path (`_refillsUsed++`, `UpdateRefillLabel()`, `OnDiscard()`, snapshot `_cards`→`Clear()`, `FlyLeftTo` each, wait `cornerDiscardDuration`+1 frame, `DealOne`×`maxHandSize`); else free `TopUpHand()`; clear `_refilling` last.
- [x] 2.5 [core] `HandController.cs`: `StartDeal()` stops `_refillRoutine` and clears `_refilling` before reset; **remove** the `TryParkInZone:279` corner gate; update doc comments.
- [x] 2.6 [core] `HandController.cs`: relabel `UpdateRefillLabel()` to `REFILLS (MANO): {used}/{max}`.

## Phase 3: Tutorial / Scene

- [x] 3.1 [tutorial] `TutorialController.cs` `DefaultStepConfigs` step index 3 (`TrashRefill`) text/objective → hand-cycle wording; note `ObserveZones` drain edge still latches (free flush keeps the 2/2 detector).
- [x] 3.2 [scene] `Tutorial.unity` serialized `steps[3]` text/objective (`:2149`/`:2152`) via MCP to match 3.1; `manage_scene(save)`; `refresh_unity`.
- [x] 3.3 [scene] Confirm `Prototype.unity` needs no edit (label overwritten by `Start()`).

## Phase 4: Testing / Validation

- [x] 4.1 [verify] `refresh_unity`; `read_console` clean after compile.
- [x] 4.2 [verify] Prototype: Play → REFILL flies hand LEFT to target, scale→0, re-deals 5; pot intact; label 1/2; double-press yields 5 not 10. Screenshot.
- [x] 4.3 [verify] Tutorial: play to step 4 → drain edge latches, no softlock at 2/2; label width fits. `read_console` clean. Screenshot. (BOTH scenes.)
- [ ] 4.4 [optional] PlayMode test: after Refill `Count==maxHandSize`, budget 1/2, center untouched, corner flushed, double-press ⇒ 5.
- [x] 4.5 [verify] Rollback: `git checkout --` the 4 scripts, `CardVisualConfig.asset`, `Tutorial.unity`.
