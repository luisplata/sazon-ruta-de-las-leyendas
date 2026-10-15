# Archive Report — enemy-level-integration

**Change**: `enemy-level-integration`
**Project**: Sazón: Ruta de las Leyendas (Unity 6000.3.10f1, URP 17.3.0, 2D)
**Archived**: 2026-10-15
**Archived by**: sdd-archive
**Artifact store**: openspec (file-based). Engram DEGRADED — no `mem_save` attempted.

---

## Final State

| Field | Value |
|-------|-------|
| Verification verdict | **PASS-WITH-WARNINGS** (sdd-verify; 0 CRITICAL) |
| Tasks | 13/13 complete (`[x]`), 0 incomplete |
| Tests | 7/7 EditMode pass (independent run, job `84d292d44dec47d5b8872f2991579e16`) |
| Build / console | Clean — 0 project errors / 0 project warnings |
| Spec compliance | 5/14 scenarios COMPLIANT, 8/14 PARTIAL, 1/14 NOT VERIFIED, 0 FAILING |
| Tracked diff | ~108 insertions / 5 deletions (apply) → ~191/8 across 6 files (verify, incl. meta) — under 400-line budget |
| Delivery | Single PR, one work-unit (enemy + background + test scaffold) |

## What Shipped

Pure presentation/wiring slice — Level 1's white-square placeholder enemy replaced by the animated
Ahuizotl and the cave/river `BG.png` background, both data-driven from `LevelConfig`:

- `LevelConfig` gained serialized `enemyPrefab` (`GameObject`) and `foreground` (`Sprite`) slots.
- `GameLoopController.SpawnEnemy()` instantiates the level's `enemyPrefab` under the scene `Enemy`
  anchor (3-arg `Instantiate(prefab, parent, instantiateInWorldSpace: true)` + `localPosition = zero`),
  destroys any prior instance, pushes the prefab's `Animator` into `EnemyView`, and hides the
  placeholder sprite. `ApplyForeground()` is a guarded no-op (art absent).
- `EnemyView` gained a settable `Animator`, a `HidePlaceholder()`, and missing-trigger tolerance:
  reaction triggers are guarded by the pure `AnimParams.HasParam` (no console spam on a 0-param
  controller) and `ResetToIdle` is state-guarded.
- `LevelConfig_Level1Mexico.asset`: `enemyPrefab → Creature1.prefab`, `background → BG_0` (BG.png).
- `Prototype.unity`: `Backdrop` transform scale (40,24,1) → (1,1,1); `foregroundRenderer` left null.
- Testability scaffold: runtime asmdef `Sazon.Anim` + `AnimParams.cs` and EditMode test assembly
  `Assets/Tests/EditMode` with `AnimParamsTests.cs` (7 cases, RED→GREEN).

PlayMode evidence confirmed: Ahuizotl rendered on-screen at the anchor (world scale ≈0.90), BG.png
fills the view un-tinted, phrase bubble works, no `Foreground` object, console clean.

## Spec Sync Summary

Main specs did not exist prior to this change — the deltas were promoted to canonical specs fresh
(ADDED requirements copied verbatim; requirement/scenario structure preserved).

| Domain | Action | Details |
|--------|--------|---------|
| `level-enemy-integration` | **Created** | 6 requirements added (R1–R6), 8 scenarios. Source: delta `specs/level-enemy-integration/spec.md`. |
| `level-background-art` | **Created** | 3 requirements added (R7–R9), 5 scenarios. Source: delta `specs/level-background-art/spec.md`. |

No MODIFIED or REMOVED requirements — both capabilities are pure additions. No destructive merge
(project `rules.archive` guard not triggered).

## Verification Reference

Full verdict and evidence: `openspec/changes/archive/2026-10-15-enemy-level-integration/verify-report.md`
(PASS-WITH-WARNINGS, dated 2026-10-15). Implementation detail: `apply-progress.md` in the same folder.

## Carry-Forward / Known Items (non-blocking)

1. **Art defect (owner: artist)** — `Creature1.prefab`'s controller (`Creature1 1.controller`, guid
   `5187571f9d705cb46895b2d2e27a65b8`) has **0 animator parameters** and its default state is
   `Creature 1 Attack`. Win/Patience/Bite/Eaten animate nothing and the creature loops Attack at
   idle. The `AnimParams` guard makes this non-fatal (no console spam), but a reaction-capable
   controller with a looping `Idle` default is needed. Not fixable in code.
2. **Unreferenced untracked art folder** `Assets/Animations/Enemigo1/` (`Creature1.controller` +
   `Enemigo1.anim`) appeared during the session; referenced by nothing. Ownership/intent unresolved —
   wire it or remove it.
3. **Out-of-scope file modified** `.atl/skill-registry.md` (+83/−3) — modified by sdd-init earlier,
   NOT by this change. Recorded but **not reverted** (per instruction). Exclude from this change's
   commit.
4. **Coverage warning (from verify)** — only the extracted pure guard is unit-tested; spawn
   idempotency, empty-prefab fallback, `EnemyView.Animator` wiring, background fallback, and the
   "enable foreground later" path rely on static review + manual PlayMode. Suggest one PlayMode test
   asserting exactly one spawned child with `localPosition == zero` and the placeholder hidden.
5. **Deferred — Level 2 (Colombia) enemy swap** not exercised. Design anticipates swapping
   `enemyPrefab` on `LevelConfig_Level2Colombia` with no code change; untested.
6. **Deferred — front/foreground art**: assign `LevelConfig.foreground` when art exists — data-only,
   no code change needed.
7. **Cosmetic** — `LevelConfig.cs` missing trailing newline; `Tutorial.unity` `GameLoopController`
   lacks the new `foregroundRenderer` field (defaults to null, harmless).

## Archive Contents

- proposal.md ✅
- explore.md ✅
- specs/ (level-enemy-integration, level-background-art) ✅
- design.md ✅
- tasks.md ✅ (13/13 complete)
- apply-progress.md ✅
- verify-report.md ✅
- archive-report.md ✅ (this file)
