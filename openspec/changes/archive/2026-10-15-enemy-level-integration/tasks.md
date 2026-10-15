# Tasks: enemy-level-integration

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~165 C# · ~35 asset/meta · ~4 scene ≈ **~205** |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR, 2 commits (scaffold+wiring, then data+verify) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

> **Decision required — testability:** Unity 6000.3 docs state a test assembly **cannot reference `Assembly-CSharp`**, so the design's direct `EnemyView.HasParam` unit test will not compile. Resolution: encode the pure guard in a tiny runtime asmdef (`Sazon.Anim`) that the game code auto-references; the test asmdef references `Sazon.Anim`. Confirm before apply.

### Suggested Work Units

| Unit | Goal | PR | Notes |
|------|------|----|-------|
| 1 | Pure guard + test scaffold | PR 1 | code+test, no behavior change |
| 2 | Enemy spawn + anim wiring | PR 1 | code |
| 3 | Level data + scene config | PR 1 | data/scene |
| 4 | PlayMode verification | PR 1 | verification |

## Phase 1 — Testability scaffold + guard (TDD)

- [x] 1.1 [code] Create `Assets/Scripts/Anim/Sazon.Anim.asmdef` (runtime, no refs) + `AnimParams.cs` stub.
- [x] 1.2 [test] Create `Assets/Tests/EditMode/EditModeTests.asmdef` (refs `Sazon.Anim`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `optionalUnityReferences:["TestAssemblies"]`; Editor-only) + `AnimParamsTests.cs` (match / miss / case / null array / empty name). **Check**: `run_tests(EditMode)` RED, polled via `get_test_job`.
- [x] 1.3 [code] Implement `public static bool Has(AnimatorControllerParameter[] ps, string name)`. **Check**: all `AnimParamsTests` GREEN.

## Phase 2 — Enemy wiring (code)

- [x] 2.1 [code] `LevelConfig.cs`: add `public GameObject enemyPrefab;` (+ `public Sprite foreground;` — Sprite per design.md, not GameObject). 
- [x] 2.2 [code] `EnemyView.cs`: settable `public Animator Animator {get;set;}`; guard `PlayReaction` trigger with `AnimParams.HasParam(animator.parameters, t)`; gate `ResetToIdle` on `animator.HasState(0, Animator.StringToHash("Idle"))`; add `HidePlaceholder()` (disables `sprite`). **Check**: compiles; `read_console` clean; placeholder still idles.
- [x] 2.3 [code] `GameLoopController.cs`: add `_enemyInstance`, `foregroundRenderer`, `SpawnEnemy()` (destroy prior → `Instantiate(prefab, enemyView.transform, true)` → `localPosition=zero` → push `GetComponentInChildren<Animator>()` → `HidePlaceholder()`; early-return when prefab null), `ApplyForeground()` no-op; call both from `ApplyLevelArt()`. **Check**: compiles; console clean.

## Phase 3 — Level data & scene config (data/scene)

- [x] 3.1 [data] `LevelConfig_Level1Mexico.asset`: `enemyPrefab = Creature1.prefab`; leave `foreground` empty.
- [x] 3.2 [data] Set `background` to the `BG_0` sprite (`BG.png`, guid `e19d0ec9…`, fileID `-6695950386315148514`).
- [x] 3.3 [scene] `Prototype.unity`: `Backdrop` Transform scale (40,24,1) → (1,1,1); `foregroundRenderer` left null.

## Phase 4 — Verification

- [x] 4.1 [verification] Play `Prototype.unity` (index 0): screenshot → Ahuizotl visible on-screen at anchor (5.43,1.06), scale ≈0.9; BG fills view; bubble shows. **Check**: screenshot + `read_console` shows no errors. Evidence: `Assets/Screenshots/enemy_level_integration_playmode.png`, `..._bubble.png`.
- [x] 4.2 [verification] Fire Win / Bite / Eaten: no `SetTrigger` missing-parameter warnings (controller has 0 params). Note `Creature1 1.controller` Attack-default as an art bug.
- [x] 4.3 [verification] Clear `enemyPrefab`, replay → placeholder shown, animator untouched, no exception.
