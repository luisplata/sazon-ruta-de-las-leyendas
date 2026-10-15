# Design: abue-protagonist

## Technical Approach

Structural clone of the shipped `enemy-level-integration` slice. `GameLoopController` gains one serialized `playerPrefab`; `StartGame()` instantiates it under the scene `Player` anchor (`playerView.transform`) and pushes its `Animator` into the unchanged `PlayerView`, replacing the old 9-clip `Player.controller`; the placeholder square is hidden. `Fire`/`ResetToIdle` are guarded so the parameter-less `Abue.controller` never spams the console. Zero rules change.

Flow: `StartGame → SpawnPlayer → Instantiate(Abue.prefab, playerView.transform) → PlayerView.Animator + HidePlaceholder`.

## Architecture Decisions

| # | Decision | Choice | Alternative | Rationale |
|---|----------|--------|-------------|-----------|
| 1 | Config surface | `GameObject playerPrefab` on **`GameLoopController`** | `LevelConfig.playerPrefab` | Protagonist is universal → ONE scene assignment, no per-level drift. |
| 2 | Instantiation | Spawn under `playerView.transform`, mirror `SpawnEnemy` | new scene anchor | Reuses the anchor `GameLoopController` + `HandController` share. |
| 3 | Animator wiring | `PlayerView.Animator { get; set; }` + `HidePlaceholder()`, mirror `EnemyView` | find-in-children each call | 1-line, view stays content-agnostic. |
| 4 | Trigger guarding | `AnimParams.HasParam` in `Fire`; `HasState(0,"Idle")` in `ResetToIdle` | raw `SetTrigger` | 0-param controller warns on every call. |
| 5 | Attack/GetHit | **A — Idle-only + guard** | B state-name; C re-author | Jam scale + enemy precedent (tolerate + flag). |
| 6 | Placement | `localPosition = Vector3.zero` | keep authored offset | Mirrors enemy; snaps Abue to the anchor spot. |

### 2. Instantiation contract (`SpawnPlayer()`, clone of `SpawnEnemy` 138–149)
- Guard `if (playerPrefab == null || playerView == null) return;` → null prefab keeps today's placeholder, no exception.
- Duplicate guard `if (_playerInstance != null) Destroy(_playerInstance);` → exactly one under the anchor.
- `Instantiate(playerPrefab, playerView.transform, instantiateInWorldSpace: true)`, then `localPosition = Vector3.zero`.
- `var a = GetComponentInChildren<Animator>(); if (a != null) playerView.Animator = a;` then `playerView.HidePlaceholder();`
- **Gotcha (same as enemy):** the 2-arg `Instantiate(prefab, parent)` uses `worldPositionStays = false` → the child inherits anchor scale **4.7**. Use the 3-arg overload with `instantiateInWorldSpace: true` (preserves world scale 1), then snap `localPosition`.
- Call from `StartGame()`, not `ApplyLevelArt` — protagonist is level-independent.

### 3. Trigger guarding (`Fire` / `ResetToIdle`)
```csharp
void Fire(string t) {
    if (animator == null) return;
    if (!AnimParams.HasParam(animator.parameters, t)) return; // no warning spam
    animator.SetTrigger(t);
}
public void ResetToIdle() {
    if (animator != null && animator.HasState(0, Animator.StringToHash("Idle")))
        animator.Play("Idle", 0, 0f);
}
```
With `Abue.controller`: `HasParam` is false for all 9 triggers → every event is a **silent no-op, no console error/warning**; `HasState(0,"Idle")` is false (state is `Abue__Idle`) → `ResetToIdle` no-ops.

### 4. Attack/GetHit — decision A (recommended)
`Abue.controller` has 0 params / 0 transitions, default `Abue__Idle`; Attack/GetHit are unreachable via `SetTrigger`.
- **(A) Idle-only** (this slice). **What the user sees:** Abue stands at the player spot looping `Abue__Idle`. Cook/penalty/win/lose still resolve lives/patience/fade exactly as before; the protagonist does not visibly play Attack/GetHit/Win. No console noise, no rules change.
- **(B) optional:** state-name `animator.Play("Abue__Attack")` + an explicit timed return to `Abue__Idle` (no exit transitions exist) — clip-length coupling + interrupt risk. Deferred.
- **(C) clean fix:** re-author `Abue.controller` (triggers + transitions). Art-side, out of code.

### 5. Transform / scale placement
Prefab root authored `(0.048,-0.22)`, scale 1; anchor `Player` `(-5.64,0.45)`, scale **4.7**. Rule: keep authored scale, snap root to zero — discards the ~0.22 offset (child `body` cancels it internally), so Abue renders ~0.22 units above authored (negligible). Verify by PlayMode screenshot; nudge the anchor if visibly off.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Assets/Scripts/GameLoop/GameLoopController.cs` | Modify | `+ playerPrefab` field (after `playerView`), `+ _playerInstance`, `+ SpawnPlayer()` from `StartGame()` |
| `Assets/Scripts/Characters/PlayerView.cs` | Modify | `+ Animator` setter, `+ HidePlaceholder()`, guard `Fire`/`ResetToIdle` |
| `Assets/Scenes/Prototype.unity` | Modify | assign `playerPrefab` = Abue.prefab (guid `166c869ceebd7b8479bf60ba085b6b6b`) |
| `Assets/Prefabs/Abue.prefab` | Referenced | instantiated at runtime (unmodified) |
| `Assets/Animations/Abue.controller` | Referenced | unmodified (Decision A) |
| `Assets/Scripts/Cards/LevelConfig.cs` | Unchanged | universal protagonist → no config field |

Only the `GameLoopController.playerPrefab` scene ref changes; **no `LevelConfig_*.asset` edits**.

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit (EditMode) | none — `HasParam` covered by `AnimParamsTests`; guards are Unity-typed, not pure | no new test (matches enemy slice) |
| Integration | none — pure presentation | — |
| E2E (PlayMode) | Abue on-screen at the right size/spot, idles, placeholder hidden; rules resolve; `playerPrefab=null` restores the placeholder; no console errors | Play `Prototype.unity`, screenshot + `read_console` |

## Migration / Rollout

None. Rollback = revert the commit, or clear `playerPrefab` in the Inspector → placeholder returns. No data/rules migration.

## Open Questions

- None blocking. The event driving Attack (`OnCook`?) / GetHit (`OnLoseLife`?) is a follow-up (B/C).
