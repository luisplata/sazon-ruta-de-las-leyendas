# Apply Progress — enemy-level-integration

**Change**: `enemy-level-integration`
**Project**: Sazón: Ruta de las Leyendas (Unity 6000.3.10f1, URP 17.3.0, 2D)
**Mode**: **Strict TDD** (EditMode, `unityMCP run_tests`)
**Artifact store**: openspec
**Date**: 2026-10-15

> Engram is DEGRADED — no `mem_save` attempted (per orchestrator). Progress persisted to this file only.

---

## Completed Tasks

### Phase 1 — Testability scaffold + guard (TDD)
- [x] 1.1 Runtime asmdef `Assets/Scripts/Anim/Sazon.Anim.asmdef` (no refs, `autoReferenced: true`) + `AnimParams.cs`.
- [x] 1.2 `Assets/Tests/EditMode/EditModeTests.asmdef` (Editor-only, refs `Sazon.Anim`, `optionalUnityReferences:["TestAssemblies"]`) + `AnimParamsTests.cs` (7 cases). RED verified by execution.
- [x] 1.3 `AnimParams.HasParam(AnimatorControllerParameter[], string)` implemented. GREEN verified.

### Phase 2 — Enemy wiring (code)
- [x] 2.1 `LevelConfig.cs`: `+ GameObject enemyPrefab`, `+ Sprite foreground`.
- [x] 2.2 `EnemyView.cs`: `+ public Animator Animator {get;set;}`, `+ TriggerReaction` guarded by `AnimParams.HasParam`, gated `ResetToIdle` on `HasState(0, "Idle")`, `+ HidePlaceholder()`.
- [x] 2.3 `GameLoopController.cs`: `+ _enemyInstance`, `+ foregroundRenderer`, `+ SpawnEnemy()`, `+ ApplyForeground()`; called from `ApplyLevelArt()`.

### Phase 3 — Level data & scene config
- [x] 3.1 `LevelConfig_Level1Mexico.asset`: `enemyPrefab → Creature1.prefab`; `foreground` empty.
- [x] 3.2 `LevelConfig_Level1Mexico.asset`: `background → BG_0` sprite (guid `e19d0ec9…`, fileID `-6695950386315148514`).
- [x] 3.3 `Prototype.unity`: `Backdrop` scale (40,24,1) → (1,1,1); `foregroundRenderer` null.

### Phase 4 — Verification
- [x] 4.1 Play `Prototype.unity`: Ahuizotl visible on-screen at anchor (5.43,1.06), world scale 0.90; BG.png fills view; bubble shows; console clean. Screenshots captured.
- [x] 4.2 Fired Win/Patience/Bite/Eaten on the 0-param `Creature1 1` animator → **0** warnings/errors.
- [x] 4.3 Cleared `enemyPrefab`, replayed → placeholder `Visual` renderer back ON, no clone, no exception; then restored the reference.

---

## Files Changed

| File | Action | What Was Done |
|------|--------|---------------|
| `Assets/Scripts/Anim/Sazon.Anim.asmdef` | Created | Runtime asmdef (auto-referenced by Assembly-CSharp) so the pure guard is testable. |
| `Assets/Scripts/Anim/AnimParams.cs` | Created | Pure `HasParam(AnimatorControllerParameter[], string)`. |
| `Assets/Tests/EditMode/EditModeTests.asmdef` | Created | EditMode test assembly. |
| `Assets/Tests/EditMode/AnimParamsTests.cs` | Created | 7 EditMode tests. |
| `Assets/Scripts/Cards/LevelConfig.cs` | Modified | `+ enemyPrefab`, `+ foreground` (+ tooltips). |
| `Assets/Scripts/Characters/EnemyView.cs` | Modified | Settable `Animator`, tolerant reaction guard, gated `ResetToIdle`, `HidePlaceholder()`. |
| `Assets/Scripts/GameLoop/GameLoopController.cs` | Modified | `+ foregroundRenderer`, `_enemyInstance`, `SpawnEnemy()`, `ApplyForeground()`. |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level1Mexico.asset` | Modified | `enemyPrefab = Creature1.prefab`; `background = BG_0` sprite. |
| `Assets/Scenes/Prototype.unity` | Modified | `Backdrop` scale → (1,1,1); `foregroundRenderer: {fileID: 0}` added. |
| `Assets/Screenshots/enemy_level_integration_playmode.png` | Created | PlayMode evidence (enemy + BG). |
| `Assets/Screenshots/enemy_level_integration_bubble.png` | Created | PlayMode evidence (phrase bubble). |

New folders/files also got Unity-generated `.meta` files (`Assets/Scripts/Anim.meta`, `Assets/Tests*`).

---

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `AnimParamsTests.cs` | Unit | N/A (new) | ✅ Stub threw `NotImplementedException` (compilation bootstrap) | — | — | — |
| 1.2 | `AnimParamsTests.cs` | Unit | N/A (new) | ✅ 7/7 failed — job `88ffacab84404301923b24b1481daede` | ✅ after 1.3 | ✅ 7 cases | ✅ clean |
| 1.3 | `AnimParamsTests.cs` | Unit | N/A (new) | ✅ (from 1.2) | ✅ 7/7 — job `d570954782ba4a02981d250758f21dc7` | ✅ present/absent/case/null/empty/blank/creature-shape | ✅ loop + guards |
| 2.1–2.3 | — | Presentation | ✅ 7/7 EditMode still green | ⚠️ No unit RED — presentation wiring (design: "Integration: none — pure presentation; E2E (PlayMode)") | ✅ PlayMode E2E (4.1–4.3) | ➖ N/A | ➖ N/A |
| 3.1–3.3 | — | Data/Scene | ✅ 7/7 EditMode still green | ⚠️ No unit RED — asset/scene data | ✅ PlayMode E2E (4.1, 4.3) | ➖ N/A | ➖ N/A |

**Honest note on the hard gate**: tasks 2.x/3.x are Unity presentation/scene/data wiring with no unit-testable behavior beyond the already-extracted `HasParam`. Per `design.md` Testing Strategy ("Integration: none — pure presentation; E2E (PlayMode)"), they are validated at the PlayMode layer, not with unit RED tests. The testable logic introduced by this change **was** driven by TDD (1.1–1.3).

## Test Summary
- **Total tests written**: 7 (all `AnimParamsTests`)
- **Total tests passing**: 7
- **Layers used**: Unit `EditMode` (7), E2E `PlayMode` (manual, screenshots + console)
- **Approval tests** (refactoring): None — no refactoring of existing behavior
- **Pure functions created**: 1 (`AnimParams.HasParam`)

---

## Verification Evidence (Phase 4)

**PlayMode run — Prototype.unity (main-camera game view):**
- `Creature1(Clone)` spawned as a child of the `Enemy` anchor; `localPosition (0,0,0)`, world position `(5.43, 1.06)`, `lossyScale (0.900, 0.900, 0.900)`, animator `Creature1 1` (0 params). ✅ anchor + scale contract honored.
- Placeholder `Visual` `SpriteRenderer.enabled = false` (hidden). ✅
- Background renders `BG_0` (`BG.png`) un-tinted. ✅
- Phrase bubble fires (`"¡Delicioso! …"`) on `PlayReaction(Win)`. ✅
- Fired `Win` / `Patience` / `Bite` / `Eaten` against the 0-param creature animator → **0 console warnings/errors** (guard works). ✅
- Cleared `enemyPrefab` → `Enemy` had 2 children (`Bubble`, `Visual[srON]`), no clone, no exception → placeholder fallback preserved; reference then restored. ✅

**Console status**: clean — 0 errors / 0 warnings from project code. The only console warning present is unrelated to this change: `Unity.AI.Toolkit.Accounts` "Account API did not become accessible within 30 seconds" (Unity AI Assistant package / network, pre-existing).

**Screenshots**:
- `Assets/Screenshots/enemy_level_integration_playmode.png` — Ahuizotl on-screen, BG.png visible, placeholder gone.
- `Assets/Screenshots/enemy_level_integration_bubble.png` — phrase bubble over the creature.

---

## Deviations from Design

1. **`foreground` is a `Sprite` (not `GameObject`).** The launch prompt said `public GameObject foreground;`, but `design.md` (File Changes + Decision 6/7) and `tasks.md 2.1` specify `Sprite foreground`, consistent with `background`/`midground` and the `ApplyLayer(renderer, sprite, …)` path. Followed design/tasks → `public Sprite foreground;`. No functional impact (both null today).
2. **Test asmdef references.** `tasks.md 1.2` listed explicit `UnityEngine.TestRunner` + `UnityEditor.TestRunner` name refs. Unity 6 rejected that with *"Assembly has duplicate references"* because `optionalUnityReferences:["TestAssemblies"]` already pulls them in. Removed the two explicit refs; kept `Sazon.Anim` + `optionalUnityReferences:["TestAssemblies"]`. Tests build and run.
3. **Guard method named `HasParam`, not `Has`.** The prompt/design use `AnimParams.HasParam`; `tasks.md 1.3` wrote `Has`. Used `HasParam` per prompt/design.
4. **`ApplyForeground` also disables the renderer when art is missing.** Design said "no-op"; implementation early-returns when `foregroundRenderer == null` (the case today) and, defensively, sets `enabled = false` if a renderer is ever wired without art — satisfies spec "disabled until art exists" with no behavior change now.

## Issues Found

1. **Art bug (owner: artist)** — `Creature1 1.controller` (the prefab's controller) has **0 animator parameters** and its **default state is `Creature 1 Attack`**. Win/Patience/Bite/Eaten fire nothing; the creature loops Attack even when idle. The guard makes this non-fatal (no warnings), but the default should be a looping `Creature 1 Idle`. Not fixed (out of scope).
2. **New untracked art folder `Assets/Animations/Enemigo1/`** appeared during the session (contains `Creature1.controller` + `Enemigo1.anim`); its `Creature1.controller` is also 0-param / Attack-default and is **referenced by nothing**. Not created by this change; left untouched. Flagging in case it is intended to replace the prefab's controller (still an art-side task).
3. **Placement looks acceptable but large-ish.** The Ahuizotl sits at the anchor and is fully on-screen (head/tail visible; tail nears the right edge). World scale 0.90 is per the design contract. If the intended look is smaller/re-centered, that is an **art-side nudge** (prefab transform), not a code change.

## Remaining Tasks
- None. All Phase 1–4 tasks are complete.

## Workload / PR Boundary
- Mode: **single PR** (forecast: 400-line risk Low, chained PRs Not recommended, ~205 estimated lines).
- Current work unit: whole slice (enemy + background + test scaffold).
- Boundary: from untouched repo → enemy prefab wired + BG.png assigned + `Backdrop` scale fixed + EditMode test scaffold, all verified.
- Actual changed lines: **~108 insertions / 5 deletions** in tracked C#/asset/scene (plus new untracked test + asmdef files). Comfortably under the 400-line budget.
- No `size:exception` needed.

## Status
**All 13 tasks (1.1–1.3, 2.1–2.3, 3.1–3.3, 4.1–4.3) complete.** Ready for **sdd-verify**.

### Editor state note
The user's editor was on `Tutorial.unity`; `Prototype.unity` was loaded for the slice and **restored to `Tutorial.unity`** at the end. `Prototype.unity` was saved with the `Backdrop` change.
