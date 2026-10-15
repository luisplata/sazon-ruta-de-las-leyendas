# Tasks: Abue as the Visible Protagonist

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~35–45 total (C# ~30, scene YAML ~2) |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR (independent slices not warranted) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

```text
Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low
```

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | `PlayerView` runtime animator + tolerant guards | PR 1 | Independently compiles; pure view hardening. |
| 2 | `GameLoopController.SpawnPlayer` + scene prefab assignment | PR 1 | Depends on Unit 1; the visible behavior. |
| 3 | Verification (EditMode regression + PlayMode + console) | PR 1 | Rides with Unit 2; no new logic. |

## Phase 1: PlayerView Wiring (C#)

- [x] 1.1 `Assets/Scripts/Characters/PlayerView.cs`: add `public Animator Animator { get => animator; set => animator = value; }` (mirror `EnemyView.cs:41-45`).
- [x] 1.2 `PlayerView.cs`: add `public void HidePlaceholder()` → `if (sprite != null) sprite.enabled = false;` (mirror `EnemyView.cs:118-121`).
- [x] 1.3 `PlayerView.cs` `Fire()`: guard `if (!AnimParams.HasParam(animator.parameters, trigger)) return;` before `SetTrigger`.
- [x] 1.4 `PlayerView.cs` `ResetToIdle()`: gate on `animator.HasState(0, Animator.StringToHash("Idle"))` (Decision A — `Abue__Idle` ≠ `Idle`).
- [x] 1.5 No new EditMode test (same Unity-typed pattern as the enemy slice; `AnimParams.HasParam` already covered by `AnimParamsTests`).

## Phase 2: GameLoopController Spawn (C#)

- [x] 2.1 `Assets/Scripts/GameLoop/GameLoopController.cs`: add `[SerializeField] GameObject playerPrefab;` after `playerView` (line ~49).
- [x] 2.2 Add `GameObject _playerInstance;` field beside `_enemyInstance`.
- [x] 2.3 Add `SpawnPlayer()` (structural clone of `SpawnEnemy`, lines 138–149): null-guard `playerPrefab`/`playerView`; `if (_playerInstance != null) Destroy(_playerInstance);`; `Instantiate(playerPrefab, playerView.transform, instantiateInWorldSpace: true)`; `localPosition = Vector3.zero`; push child `Animator` into `playerView.Animator`; `playerView.HidePlaceholder()`.
- [x] 2.4 Call `SpawnPlayer()` from `StartGame()` (NOT `ApplyLevelArt`) — protagonist is level-independent.
- [x] 2.5 `read_console` after the script change — confirm zero compile errors before proceeding.

## Phase 3: Scene Data Wiring

- [x] 3.1 `Assets/Scenes/Prototype.unity`: assign `Assets/Prefabs/Abue.prefab` (guid `166c869ceebd7b8479bf60ba085b6b6b`) to `GameLoopController.playerPrefab` on the `GameLoop` object.
- [x] 3.2 Do NOT edit `Tutorial.unity`, `LevelConfig_*.asset`, `Abue.controller`, or `Abue.prefab` (out of scope).

## Phase 4: Verification (PlayMode + Console)

- [x] 4.1 `unityMCP run_tests(mode="EditMode")` — existing 7 tests (incl. `AnimParamsTests`) stay green.
- [x] 4.2 Play `Prototype.unity`: Abue replaces the blue square, loop `Abue__Idle`, correct scale/spot; placeholder hidden (spec: Idle loop, anchor spawn).
- [x] 4.3 Trigger cook/penalty/win/lose: `read_console` shows NO errors/warnings (spec: trigger absent = silent no-op).
- [x] 4.4 Clear `playerPrefab` in Inspector, replay → placeholder returns, no exception (spec: fallback).
- [x] 4.5 Confirm `StartGame()` re-entry yields exactly one instance (duplicate guard).

## Notes

Strict TDD is active, but no new pure logic is introduced — the guard helper is pre-tested. Validation = EditMode regression + PlayMode + `read_console` (Decision A: Idle-only).
