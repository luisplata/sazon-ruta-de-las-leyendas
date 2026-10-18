# Apply Progress: Refill as Hand-Cycle Discard

**Change**: `refill-discard-mechanic`
**Mode**: Standard (strict_tdd OFF per orchestrator; no test asmdef created)
**Delivery**: single PR, ~153 inserted / 48 deleted across 6 files (forecast said ~85–130 semantic
lines; the extra is doc comments). Well inside the 400-line budget.
**Date**: 2026-10-18

## Tasks

### Phase 1: Config / Foundation

- [x] 1.1 [config] `CardVisualConfig.cs`: added `discardTargetPosition` (default `(-717,-310)`);
      retooled `maxRefillsPerGame` / `maxTrashPerGame` tooltips to hand-cycle wording.
      `maxRefillsPerGame` **NOT renamed** (serialized in the `.asset`).
- [x] 1.2 [config] `CardVisualConfig.asset`: `discardTargetPosition: {x: -717, y: -310}`.

### Phase 2: Core Implementation

- [x] 2.1 [core] `CardView.cs`: `FlyLeftTo(Vector2 targetAnchoredPos)` — guards `_discarding`,
      clears `_dragging`, `StopMove()`, `group.blocksRaycasts = false`, starts the routine.
- [x] 2.2 [core] `CardView.cs`: `FlyLeftRoutine(Vector2 target, float duration)` — lerps
      `anchoredPosition` with `reLayoutEasingCurve`, scales live start-scale → 0 with
      `cornerDiscardScaleCurve`, `Destroy(gameObject)`. Never calls `_zone.Release`.
- [x] 2.3 [core] `HandController.cs`: `bool _refilling` + `Coroutine _refillRoutine`; `Refill()`
      is now guard + `_refillRoutine = StartCoroutine(RefillRoutine())`.
- [x] 2.4 [core] `HandController.cs`: `RefillRoutine()` per design sequence — free corner flush →
      charge path (`_refillsUsed++`, `UpdateRefillLabel()`, `OnDiscard()`, snapshot `_cards` →
      `Clear()`, `FlyLeftTo` each, `WaitForSeconds(cornerDiscardDuration)` + 1 frame, `DealOne` ×
      `maxHandSize`) → else free `TopUpHand()`; `_refilling` cleared last.
- [x] 2.5 [core] `HandController.cs`: `StartDeal()` stops `_refillRoutine` + clears `_refilling`
      before the reset; **removed** the `TryParkInZone` corner budget gate (was ~:279); doc
      comments updated (class, `_refillsUsed`, `StartDeal`, `Refill`, `PrepareFood`, label, park).
- [x] 2.6 [core] `HandController.cs`: label → `REFILLS (MANO): {used}/{max}`.

### Phase 3: Tutorial / Scene

- [x] 3.1 [tutorial] `TutorialController.cs` step index 3 (`TrashRefill`) text + objective →
      hand-cycle wording; `ObserveZones` / `TrashThenRefill` doc comments updated (drain edge still
      latches because the flush is free and unconditional).
- [x] 3.2 [scene] `Tutorial.unity` serialized `steps[3]` text/objective updated to match 3.1.
- [x] 3.3 [scene] `Prototype.unity` confirmed **unchanged** — its `m_text: 'REFILLS: 0/2'` is
      overwritten by `Start()` → `UpdateRefillLabel()`.

### Phase 4: Testing / Validation

- [x] 4.1 [verify] `refresh_unity` (force + compile) then `read_console`: clean compile, 0 errors.
- [x] 4.2 [verify] Prototype Play-mode run (see Evidence).
- [x] 4.3 [verify] Tutorial Play-mode run through step 4 (see Evidence).
- [ ] 4.4 [optional] PlayMode test — **skipped on purpose**: strict_tdd OFF, no test asmdef in the
      project, and the mechanic is fully covered by the Play-mode run below.
- [x] 4.5 [verify] Rollback recipe valid (purely additive: 4 scripts + `CardVisualConfig.asset` +
      `Tutorial.unity`); not executed — it would discard the work. Nothing was committed.

## Files Changed

| File | Action | What Changed |
|------|--------|--------------|
| `Assets/Scripts/Cards/CardVisualConfig.cs` | Modified | + `[Header("Hand discard (REFILL)")] discardTargetPosition = (-717,-310)`; `maxRefillsPerGame` / `maxTrashPerGame` tooltips retooled to hand-cycle wording. |
| `Assets/ScriptableObjects/Cards/CardVisualConfig.asset` | Modified | + `discardTargetPosition: {x: -717, y: -310}`. |
| `Assets/Scripts/Cards/CardView.cs` | Modified | + `FlyLeftTo` / `FlyLeftRoutine`. |
| `Assets/Scripts/Cards/HandController.cs` | Modified | + `using System.Collections`; `_refilling` + `_refillRoutine`; `Refill()` → coroutine launcher; + `RefillRoutine()`; `StartDeal` aborts an in-flight refill; corner park gate removed; label `REFILLS (MANO)`; doc comments. |
| `Assets/Scripts/Tutorial/TutorialController.cs` | Modified | Step-3 text/objective → hand-cycle wording; 3 doc comments updated. |
| `Assets/Scenes/Tutorial.unity` | Modified | `steps[3]` text + objective (2 lines). |
| `Assets/Screenshots/*.png` | Created (gitignored) | Play-mode evidence — `Assets/Screenshots/` is in `.gitignore`, so the PR stays clean. |

## Play-mode Evidence (Unity 6000.3.10f1, instance `GameJamUni2@9eee0fc1`)

Screenshots (gitignored, kept as evidence):

| File | What it shows |
|------|---------------|
| `Assets/Screenshots/refill_proto_00_baseline.png` | Prototype baseline: 5-card fan, label `REFILLS (MANO): 0/2`. |
| `Assets/Screenshots/refill_proto_01_midflight.png` | **Mid-fly**: the 5 hand cards moved LEFT/down toward `(-717,-310)` and are shrinking; label already `1/2` (charged on consume). |
| `Assets/Screenshots/refill_proto_02_after.png` | After the routine: 5 NEW cards re-dealt into the fan, label `1/2`. |
| `Assets/Screenshots/refill_proto_03_doublepress_midflight.png` | Double-press run: 2 cards parked in the pot + hand re-dealt to 5, label `2/2`. |
| `Assets/Screenshots/refill_tutorial_00_step3.png` | Tutorial at step 3 with the new text/objective and `REFILLS (MANO): 0/2`. |
| `Assets/Screenshots/refill_tutorial_01_after_2of2.png` | Tutorial after the 2nd refill: corner empty, 4-card hand (1 flushed), label `2/2`, step advanced to 4. |

### Prototype

- `Refill` pressed via the real `RefillButton.onClick` (not a direct method call).
- Click 1 (timeScale 0.12 to freeze the fly for capture): `_cards.Clear()` ran synchronously
  (`Count == 0` right after the click), 5 cards flew left while shrinking, then `Count == 5`,
  `CardView` objects alive == 5 (all flyers destroyed — no leak), label `1/2`.
- Re-deal uses the cursor: the second hand had different card identities than the first (verified
  visually), no reshuffle.
- **Re-entrancy guard (the important assertion)**: two `onClick.Invoke()` in the same frame with
  `timeScale = 0.12` → after click 1 `_refilling == true, _refillsUsed == 1, hand == 0`; after
  click 2 the state was **unchanged** (`_refilling == true, _refillsUsed == 1, hand == 0`). Had the
  guard failed, the second routine would have seen an empty hand and dealt 5 immediately — it did
  not. Final hand 5, never 10.
- **Pot intact**: 2 cards parked in the center, then a refill → `Center` zone count stayed 2, the
  parked cards kept `scale = 0.55` under `CenterDropZone` while the hand was discarded/redealt.
- **Budget**: label went `0/2 → 1/2 → 2/2`, one charge per real hand discard.

### Tutorial

- Played the ladder for real (10 s `minHold` per step in the serialized scene): step 0 → park in the
  corner → step 1 → step 2 unlocks the cook zone → park in the center → step 3 (`TrashRefill`).
- Step 3 showed the NEW text and objective (`Tira una carta a la ZONA ROJA (basura) y pulsa REFILL
  para reciclar tu mano`) — confirming the scene-serialized strings parsed correctly (accents/`¡`
  intact, read back through MCP reflection).
- Refill at step 3: corner `1 → 0` (flushed), center untouched, hand `4 → 0 → 5`, label `1/2`;
  next frame `_sawRefillDump == true`, `_refillDumps == 1`, and the ladder **advanced to step 4** →
  no softlock.
- **2/2 stranding check**: burned the 2nd use (`2/2`), then parked ANOTHER corner card at 2/2 →
  accepted (gate removal confirmed) → pressed Refill at 2/2 → corner `1 → 0` flushed **free**,
  hand unchanged (4, no discard/redeal), center untouched, label stayed `2/2`. No card can be
  stranded at 2/2.

### Console

- Post-compile: 0 errors, 0 warnings.
- During both Play-mode runs the only entries were pre-existing/unrelated:
  1. `The character with Unicode value \U0001F32E was not found in the [LiberationSans SDF] font
     asset ... text object [ObjectiveLabel]` (the 🌮 emoji in the goal line — pre-existing).
  2. `Account API did not become accessible within 30 seconds` (Unity Services, unrelated).
- No Refill/HandController/CardView errors or warnings.

### Label width

`refillLabel` rect = 300 px wide, `preferredWidth` = 278.7 px for `REFILLS (MANO): 1/2` → fits with
~21 px margin, no overflow (measured in Play mode; visually confirmed in both scenes).

## Deviations from Design

1. **`Tutorial.unity` edited on disk instead of `manage_scene(save)`** (task 3.2 wording). The scene
   was not loaded in the editor, so the two serialized string fields were patched in the YAML using
   Unity's own escaping style, then verified by loading the scene and reading `steps[3]` back through
   MCP reflection (`isDirty: false` after both Play-mode runs). Equivalent outcome with zero scene
   re-serialization churn.
2. **`playerView?.OnDiscard()` is also called on the free corner flush** (the design's sequence
   diagram lists it only in the charge path). This preserves the pre-existing "player reacts to a
   trash dump" feedback for a press that only flushes the corner (budget spent). When both happen in
   one press the trigger is set twice in the same frame, which is idempotent for
   `Animator.SetTrigger` — no visual difference. Removing it would silently drop that feedback.
3. **Spec vs design on the "empty press" wording**: the spec scenario says an empty press deals
   nothing; decisions #3/#4 + design say an empty hand is a free `TopUpHand()`. Implemented as
   `TopUpHand()` (per the confirmed decisions). The truly-empty state (hand 0 **and** corner 0) is
   unreachable in play (3 center + 2 corner caps ⇒ hand ≥ 2), so the difference is unobservable.
4. **Spec vs design on charging the corner flush**: the spec's budget requirement says a use is
   charged when "at least one card (hand or corner)" is discarded; decision #6 and the design say
   the corner flush is **unconditional and free**. Implemented free (design + tasks 2.4). Only
   observable in the near-unreachable empty-hand + parked-corner state, where the spec version would
   charge the same exhausted budget anyway.

## Issues Found

- `minHold` is serialized as `10` for every tutorial step in `Tutorial.unity` (the code default is
  `0.4`/`2.5`), so a full ladder walk takes ~70 s. Pre-existing, out of scope.
- The tutorial `objectiveLabel` rect is narrow, so the longer step-3 objective wraps to ~7 lines
  (it renders fully, no clipping — verified in the screenshot). Pre-existing rect width; cosmetic.

## Remaining Tasks

- [ ] 4.4 [optional] PlayMode test (intentionally skipped).
- Nothing else — all non-optional tasks are complete.

## Workload / PR Boundary

- Mode: **single PR** (no chain needed; 153/48 lines across 6 files).
- Boundary: starts from the trash-only `Refill()` and ends with the hand-cycle `RefillRoutine()` +
  tutorial copy, fully validated in both scenes.
- Estimated review budget impact: low (~200 changed lines incl. comments, 0 binary/scene churn
  beyond two serialized strings).

## Status

15/16 tasks complete (the 16th is the optional PlayMode test). Ready for verify.
