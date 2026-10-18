# Proposal: Refill as Hand-Cycle Discard

## Intent

Refill today only drains the bottom-left trash corner (max 2) and tops up the hand
(`HandController.Refill` `:134-146`); it never discards. Make it a **hand-cycle tool**: dump
the whole hand **left** and re-deal 5, so players park ingredients in the **pot** then fish
for the missing one.

## Scope

### In Scope
- `Refill()` rewritten: discard all `_cards` (fly left → destroy) + redeal 5.
- `_refilling` guard; keep `_refillsUsed` cap (2); define empty-hand; keep counter/label.
- ONE new `CardView.FlyLeftTo` + async `RefillRoutine`.
- New `CardVisualConfig.discardTargetPosition` (`(-717,-310)`) + `refillDiscardDuration`.
- Verify Tutorial Step-4 wording.

### Out of Scope
- **HUD ART integration** (`Escene UI.png`) — separate change.
- Removing/reworking `cornerZone` (cascades into tutorial + both scenes) — follow-up.
- Re-shuffle, new scenes, cook flow, `maxCook`, DI/event-bus.

## Capabilities

### New Capabilities
- `hand-refill`: Refill SHALL discard the entire active hand, MUST NOT touch center/pot cards,
  and SHALL re-deal a full hand only after discard completes.

### Modified Capabilities
- None.

## Approach

10x-simplest path; no new architecture or scene objects:
1. `CardVisualConfig`: add `discardTargetPosition=(-717,-310)`, `refillDiscardDuration=0.5f`;
   update tooltip.
2. `CardView`: add `FlyLeftTo(target,duration,ease)` — `_discarding=true`, `StopMove()`, eased
   lerp + scale→0, `Destroy` (mirrors `DiscardCenter`).
3. `Refill()` → `RefillRoutine()`: gate `_refillsUsed`+`_refilling`; relabel; `OnDiscard()`;
   snapshot `_cards` then `Clear()`; `ConsumeZone(cornerZone)`; `FlyLeftTo` each; yield;
   `DealOne`×`maxHandSize`. Keep `TopUpHand()` for cook.
4. Play mode: verify fly-left, re-deal, pot intact, clean `read_console`.

Center cards never enter `_cards`. Keep the `_dealIndex` cursor; never re-shuffle.

**Alternatives Considered**: new pile RectTransform (edits both scenes) · reuse
`DiscardCorner`/`ConsumeZone` (zone-bound/immediate) · unlimited Refill or corner removal
(breaks puzzle/tutorial).

## Affected Areas
- `Assets/Scripts/Cards/HandController.cs` — `Refill()` → `RefillRoutine()`; `_refilling` guard.
- `Assets/Scripts/Cards/CardView.cs` — new `FlyLeftTo`.
- `Assets/Scripts/Cards/CardVisualConfig.cs` + `.asset` — new fields/defaults.
- `Assets/Scripts/Tutorial/TutorialController.cs` — Step-4 wording; verify `ObserveZones`/`TrashThenRefill`.
- `Assets/Scenes/{Prototype,Tutorial}.unity` — Unchanged (config target avoids edits).

## Assumptions
1. Keep cap 2; raising it is post-playtest tuning.
2. `cornerZone` stays; Refill flushes it (preserves tutorial detector).
3. Empty hand → still consumes one.
4. Hand-space ≈ canvas-space, so `(-717,-310)` is valid.
5. No Refill test exists (only `AnimParamsTests.cs`).

## Risks
- Double-click → two routines deal 10 (Med) → `_refilling` guard.
- Refill during win/lose window (`ReactionShowDelay 0.5s`) (Low) → scene destroy stops
  coroutine; optionally gate on `_roundActive`.
- `ObserveZones` couples "refill used" to corner drain (Med) → Refill still drains the corner;
  verify the edge latches.
- Corner-point visual collision (Low) → cosmetic.
- Cycle trivializes puzzle (Low) → keep the cap.

## Rollback Plan

`git checkout --` the three card scripts, `CardVisualConfig.asset`, and `TutorialController.cs`.
Config additions are additive/defaulted and no scene/prefab was edited, so the old trash-only
Refill returns exactly.

## Dependencies

- Unity Editor live via MCP; Play mode for validation. No new packages.

## Success Criteria

- [ ] Refill discards each hand card flying left to `discardTargetPosition`, then destroys.
- [ ] Full hand re-deals only after discard completes.
- [ ] Center/pot cards untouched; corner zone (if any) flushed.
- [ ] Counter/label intact; `_refilling` blocks double-click; empty-hand defined.
- [ ] `read_console` clean; PlayMode confirms the cycle-and-fish loop reads correctly.
