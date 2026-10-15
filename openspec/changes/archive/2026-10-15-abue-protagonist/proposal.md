# Proposal: Abue as the Visible Protagonist

## Intent

The pulled `Abue.prefab` / `Abue.controller` / `Abue.png` are completely unintegrated; the protagonist today is a blue placeholder square driven by the old 9-clip `Player.controller`. Make Abue the visible protagonist by mirroring the shipped enemy wiring (`level-enemy-integration`). Presentation-only, zero rules change, <1h jam slice.

## Scope

### In Scope
- `GameLoopController` player-prefab slot (**recommended**: protagonist is universal → one assignment; `LevelConfig.playerPrefab` needs all 3 configs).
- `PlayerView.Animator` setter + `HidePlaceholder()` (mirror `EnemyView`).
- `SpawnPlayer()` clone of `SpawnEnemy()` under `playerView.transform`.
- Guard `PlayerView.Fire()` with `AnimParams.HasParam` + `ResetToIdle` with `HasState`.
- Assign `Abue.prefab` in `Prototype.unity`.

### Out of Scope
- Re-authoring `Abue.controller`, changing the 9 player triggers or gameplay rules.
- `Tutorial.unity` wiring, `Assets/mk/` background-split + `timeline.controller`, the enemy.

## Capabilities

### New Capabilities
- `player-protagonist-integration`: protagonist prefab is data-driven, instantiated under the scene Player anchor at game start, animator wired into `PlayerView`, placeholder hidden, missing animator params tolerated.

### Modified Capabilities
- None (presentation wiring only; `level-enemy-integration` behavior unchanged).

## Approach

Structural clone of the enemy slice: add `GameLoopController.playerPrefab`; `SpawnPlayer()` destroys any prior instance, `Instantiate(prefab, playerView.transform, instantiateInWorldSpace:true)`, `localPosition=zero`, pushes the child `Animator` into `PlayerView`, then `HidePlaceholder()`. Mirror the guard helpers so the parameter-less `Abue.controller` never spams the console. Unassigned prefab → today's placeholder (null-guarded).

**Key decision (defer to design)** — `Abue.controller` has 0 params / 0 transitions, so Attack/GetHit cannot fire via `SetTrigger`. (A) **Idle-only + guard** (recommended: no errors, no Attack/GetHit yet); (B) state-name `Play`/`CrossFade` fallback; (C) art-side re-author (out of code). Recommend **A**; B/C are follow-ups.

### Alternatives Considered
- `GameLoopController.playerPrefab` vs `LevelConfig.playerPrefab`: universal protagonist favors the single scene-level assignment (no per-level drift).
- Guarded-only vs state-name fallback: guard ships smaller, matches enemy precedent; fallback deferred.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Assets/Scripts/GameLoop/GameLoopController.cs` | Modified | `playerPrefab` field + `SpawnPlayer()` |
| `Assets/Scripts/Characters/PlayerView.cs` | Modified | `Animator` setter, `HidePlaceholder()`, `HasParam`/`HasState` guards |
| `Assets/Scenes/Prototype.unity` | Modified | assign `Abue.prefab` (guid `166c869ceebd7b8479bf60ba085b6b6b`) |
| `Assets/Prefabs/Abue.prefab` | Referenced | instantiated at runtime (unmodified) |
| `Assets/Scripts/Anim/AnimParams.cs` | Referenced | reuse `HasParam` |

## Assumptions

1. Hero prefab lives on `GameLoopController`, not `LevelConfig`.
2. Which events drive Attack/GetHit is a follow-up (not this slice).
3. First slice targets `Prototype.unity` only.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| 0-param controller no-ops/warns on `SetTrigger` | High | `HasParam` guard included in core slice |
| Attack/GetHit never play (Idle-only) | High | Accepted; flagged as follow-up (A/B/C) |
| `Abue` authored root offset `(0.048,-0.22)` vs `localPosition=0` | Med | Verify visually in-editor; nudge if low |

## Rollback Plan

Revert the single work-unit commit. Or, without code revert, clear `playerPrefab` in the Inspector → null-guarded `SpawnPlayer()` leaves the blue placeholder. No data/rules migration.

## Dependencies

- `AnimParams.HasParam` helper (`Assets/Scripts/Anim/AnimParams.cs`).
- `Abue.prefab` present in `Assets/Prefabs/`.

## Success Criteria

- [ ] In `Prototype.unity`, `Abue.prefab` appears as the protagonist, Idle looping, placeholder hidden.
- [ ] No console errors/warnings when player events fire.
- [ ] Swapping the prefab requires no code change.
- [ ] Cook/penalty/win/lose rules behave exactly as before.
