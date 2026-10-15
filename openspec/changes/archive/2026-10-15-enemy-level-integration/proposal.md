# Proposal: enemy-level-integration

## Intent

Level 1's enemy is a white-square placeholder, while the animated `Creature1.prefab` (El Ahuizotl) and the `BG_0`/`BG.png` background exist but are referenced by no scene. Enemy RULES already work (`GameLoopController`+`EnemyView`); only the visual and its per-level swap are missing. Wire them into Level 1, keep the enemy swappable, and leave the (nonexistent) front layer disabled.

## Scope

**In:** serialized `enemyPrefab` on `LevelConfig`; instantiate it at game start under the scene `Enemy` anchor and plug its `Animator` into `EnemyView`; settable `Animator` + tolerate missing triggers; assign `BG.png`'s sprite to Level 1 `background` (existing `ApplyLevelArt` path); front stays absent/disabled; works for Level 1, trivially swappable for Level 2.

**Out:** new front art, 2nd creature art, new enemy rules/mechanics, tutorial scene changes (unless trivial reuse), any DI/event-bus or new config SO.

## Capabilities

**New:**
- `level-enemy-integration`: per-level enemy prefab instantiation + Animator wiring into `EnemyView`.
- `level-background-art`: per-level background sprite assignment, front/foreground disabled.

**Modified:** None — pure presentation/wiring; no existing spec behavior changes.

## Approach

Simplest jam path. One serialized prefab ref on `LevelConfig`; scene `Enemy` stays the anchor/parent; `GameLoopController` instantiates the prefab in the start/apply-art path, grabs `GetComponentInChildren<Animator>()`, pushes it into `EnemyView`; an empty ref falls back to today's placeholder. Background reuses `backdropRenderer` + `LevelConfig.background`. No new systems.

## Assumptions

1. "front/foreground" = a decorative layer in front of characters; its art does not exist, so we leave it absent/disabled (no new code).
2. Reactions: tolerate missing Animator triggers (map only what exists, no new controller authored). `Attack`-as-default in `Creature1 1.controller` is flagged as a likely bug to fix later.
3. First slice = `Prototype.unity` (Level 1) only; tutorial keeps its placeholder.
4. Prefab transform/placement contract is finalized in the design phase.

## Alternatives Considered

- Instantiate `BG_0.prefab` vs existing `Backdrop` layer + sprite → recommend the existing layer (fewer moving parts; fallback already handled).
- New `EnemyConfig` SO vs field on `LevelConfig` → recommend the field (jam scale; no speculative abstraction).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Assets/Scripts/Cards/LevelConfig.cs` | Modified | add `enemyPrefab` |
| `Assets/Scripts/Characters/EnemyView.cs` | Modified | settable `Animator`; tolerate missing triggers |
| `Assets/Scripts/GameLoop/GameLoopController.cs` | Modified | spawn enemy at game start |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level1Mexico.asset` | Modified | prefab ref + `BG.png` sprite |
| `Assets/Scenes/Prototype.unity` | Modified | reuse existing `Enemy` anchor |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Controller lacks reaction triggers (Attack default) | High | guard with `HasParameter`; flag bug; bubble unaffected |
| Prefab transform/scale mismatch | Med | decide contract in design; verify in Play Mode |
| "Front" misread adds needless objects | Low | assumption 1 |

## Rollback Plan

Revert the single work-unit commit (enemy + background as one deliverable). Unassigning `LevelConfig.enemyPrefab` restores the placeholder; no data migrations; scenes/SOs are git-tracked (LFS).

## Success Criteria

- [ ] In Level 1 Play Mode the Ahuizotl prefab animates as the enemy.
- [ ] Background renders `BG.png`; front layer absent/disabled.
- [ ] Swapping `LevelConfig.enemyPrefab` (or the SO) switches the enemy for Level 2.
- [ ] No console errors; the `EnemyView` phrase bubble still works.
