# Apply Progress: abue-protagonist

**Change**: abue-protagonist
**Mode**: Strict TDD (active — `strict_tdd: true` in `openspec/config.yaml`)
**Date**: 2026-10-15
**Batch**: 1 of 1 (single PR — no chaining warranted)

## Status

**Complete — 13/13 tasks. Ready for verify.**

## Executive Summary

Structural clone of the shipped `enemy-level-integration` slice. `PlayerView` now exposes a settable `Animator`, a `HidePlaceholder()`, and tolerant guards on all 9 `SetTrigger` calls plus `ResetToIdle`; `GameLoopController` gained a universal `playerPrefab` slot and a `SpawnPlayer()` invoked from `StartGame()`; `Prototype.unity` wires `Abue.prefab` into that slot. Verified live in PlayMode: Abue(Clone) spawns under the `Player` anchor at world scale 1, loops `Abue__Idle`, the placeholder square is hidden, the animator is wired (`Abue`, 0 params), the duplicate guard leaves exactly one instance, the null-prefab fallback keeps the placeholder with no exception, and firing all 9 events logs no errors/warnings. Existing 7 EditMode tests stay green.

## Files Changed

| File | Action | What Was Done |
|------|--------|---------------|
| `Assets/Scripts/Characters/PlayerView.cs` | Modified | `+ Animator { get; set; }`, `+ HidePlaceholder()`, guarded `Fire()` with `AnimParams.HasParam`, gated `ResetToIdle()` on `HasState(0, "Idle")`. |
| `Assets/Scripts/GameLoop/GameLoopController.cs` | Modified | `+ [SerializeField] GameObject playerPrefab`, `+ GameObject _playerInstance`, `+ SpawnPlayer()` (clone of `SpawnEnemy`), call `SpawnPlayer()` from `StartGame()`. |
| `Assets/Scenes/Prototype.unity` | Modified | `GameLoopController.playerPrefab` → `Assets/Prefabs/Abue.prefab` (guid `166c869ceebd7b8479bf60ba085b6b6b`). |
| `openspec/changes/abue-protagonist/tasks.md` | Modified | All 13 checkboxes marked `[x]`. |

Diff stat: **75 changed lines** (72 insertions, 3 deletions) across 3 authored files — well under the 400-line review budget. The 3 deletions are the replaced `ResetToIdle`/`Fire` bodies.

## Tasks Completed

- [x] 1.1 `PlayerView.Animator` settable property (mirrors `EnemyView.cs:41-45`).
- [x] 1.2 `PlayerView.HidePlaceholder()` (mirrors `EnemyView.cs:118-121`).
- [x] 1.3 `Fire()` guarded with `AnimParams.HasParam` (silent no-op on missing param).
- [x] 1.4 `ResetToIdle()` gated on `HasState(0, StringToHash("Idle"))` (Decision A — `Abue__Idle` ≠ `Idle`).
- [x] 1.5 No new EditMode test (Unity-typed guards; `HasParam` already covered by `AnimParamsTests`) — see TDD evidence.
- [x] 2.1 `[SerializeField] GameObject playerPrefab` added after `playerView`.
- [x] 2.2 `GameObject _playerInstance` field added beside `_enemyInstance`.
- [x] 2.3 `SpawnPlayer()` added (null-guard, duplicate guard, 3-arg Instantiate w/ `instantiateInWorldSpace: true`, `localPosition = zero`, push child `Animator`, `HidePlaceholder()`).
- [x] 2.4 `SpawnPlayer()` called from `StartGame()` (level-independent, not `ApplyLevelArt`).
- [x] 2.5 `read_console` after script change — zero compile errors.
- [x] 3.1 `Prototype.unity`: `playerPrefab` = `Abue.prefab`.
- [x] 3.2 No edits to `Tutorial.unity`, `LevelConfig_*.asset`, `Abue.controller`, `Abue.prefab`.
- [x] 4.x Verification (EditMode regression + PlayMode + console) — see below.

## Verification Evidence

### EditMode regression (unit)

- Command: `unityMCP run_tests(mode="EditMode")`
- Job ID: `c055f587968649f9ab6ce1f57895543b`
- Result: **7 passed / 0 failed / 0 skipped** (`AnimParamsTests` × 7).
- Note: first attempt (`dc963321b3b848c38275a134068aa668`) failed to initialize ("tests did not start within timeout", an infrastructure flake after the domain reload); retried with `init_timeout=90000` and it passed.

### PlayMode (E2E) — `Assets/Scenes/Prototype.unity`

Runtime introspection via `execute_code` while playing:

| Check | Observed | Spec scenario |
|-------|----------|---------------|
| Spawn under anchor | `Player` child `Abue(Clone)`, `localPosition=(0,0,0)`, `lossyScale=(1,1,1)` | Prefab spawned under the anchor |
| Animator wired | `playerView.animator = Abue(Clone)`, controller `Abue`, 0 params | Animator available for player events |
| Idle loop | Current state `Abue__Idle` | Idle loop visible |
| Placeholder hidden | `playerView.sprite (Visual).enabled = False` | placeholder hidden |
| Duplicate guard | after `StartGame()` re-entry + frame settle → **exactly 1** clone | No duplicate instance |
| Null fallback | `playerPrefab=null` → `SpawnPlayer()` no exception, no new clone, placeholder stays `enabled=True` | Empty prefab keeps the placeholder |
| Missing triggers | all 9 events + `ResetToIdle()` invoked, no exception, state unchanged | Trigger absent (silent no-op) |
| Rules unchanged | no edits to the 9 events / cook / lives / patience / win/lose | Rules preserved |

### Console status

Clean. `read_console` shows **0 compile errors** and **0 code-related errors/warnings** after the script changes and during/after PlayMode and the 9-event exercise. The only warning present is the environment-level `Account API did not become accessible within 30 seconds` (Unity Services/network, unrelated to this change).

### Screenshot evidence

- Path: `Assets/Screenshots/abue_protagonist_playmode.png` (game view, 531×299, Main Camera).
- Shows the protagonist sprite rendered at the player anchor (blue placeholder square gone), the Ahuizotl enemy, HUD, and the round in play.

## TDD Cycle Evidence (Strict TDD active)

No new *pure* logic is introduced by this change — the only testable pure helper (`AnimParams.HasParam`) already exists and is covered by `AnimParamsTests`. The new code is Unity-typed view/spawn plumbing whose observable contract is exercised through PlayMode (above), matching the shipped `enemy-level-integration` precedent and the design's explicit "Unit (EditMode): none" decision.

| Task | RED (test first) | GREEN (impl passes) | REFACTOR | Verdict |
|------|------------------|---------------------|----------|---------|
| 1.1–1.4 PlayerView guards/wiring | N/A — no new pure logic; `HasParam` pre-covered | EditMode regression green (job `c055f587…`, 7/7) + PlayMode event exercise (no warnings) | n/a | PASS (documented exception, design-sanctioned) |
| 2.1–2.4 GameLoopController spawn | N/A — presentation plumbing | PlayMode: spawn/anchor/wiring/duplicate-guard/fallback all verified | n/a | PASS (documented exception) |
| 3.1 Scene wiring | N/A | YAML diff (1 line) + PlayMode load | n/a | PASS |
| 4.x Verification | N/A | EditMode 7/7 + PlayMode + `read_console` clean | n/a | PASS |

> Per the strict-TDD hard gate, the absence of a *new* RED test is recorded explicitly here rather than silently skipped: the change authors no new pure logic, and the guard helper it relies on is already RED-then-GREEN covered. If the verifier requires a dedicated test, it would be a PlayMode/`[Test]` integration test, which is outside this slice's scope (design: "Integration: none — pure presentation").

## Deviations from Design

None — implementation matches `design.md` (Decisions 1–6) exactly. The estimated ~35–45 changed lines came in at 75; still trivially within the 400-line budget.

## Issues Found

- **Bridge port churn**: the compile/domain-reload cycle moved the MCP bridge from port 6401 → 6400 and the `mcpforunity://editor/state` resource served a stale (cached) snapshot for a while. Worked around by routing through `read_console`, `find_gameobjects`, component resources, and `execute_code` (all live). No impact on correctness.
- **Test-run init flake** (job `dc963321…`) — retried successfully. Infrastructure, not code.

## Remaining Tasks

None. All 13 tasks complete.

## Workload / PR Boundary

- Mode: single PR (forecast: 400-line budget risk **Low**, chained PRs **not** recommended).
- Unit: 1 (`PlayerView` hardening + `GameLoopController.SpawnPlayer` + scene wiring) — one autonomous deliverable with verification included.
- Boundary: starts at "PlayerView has no runtime animator + unguarded triggers" → ends at "Abue is the visible, idle-looping protagonist wired from the scene with tolerant guards and placeholder fallback".
- Review budget impact: ~75 lines — comfortable for a single focused review.

## Rollback

Revert the commit, or clear `playerPrefab` in the Inspector → the placeholder returns. No data/rules migration.

## Next

`sdd-verify`.
