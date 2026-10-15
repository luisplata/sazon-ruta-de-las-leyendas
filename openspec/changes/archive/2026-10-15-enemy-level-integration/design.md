# Design: enemy-level-integration

## Technical Approach

Pure presentation wiring. `LevelConfig` gains `enemyPrefab`; `GameLoopController.ApplyLevelArt()` instantiates it under the existing scene `Enemy` anchor (`enemyView.transform`) and pushes its `Animator` into the unchanged `EnemyView`. Background reuses the existing `Backdrop` renderer + `LevelConfig.background` (already consumed by `ApplyLayer`). No rules change.

## Architecture Decisions

| # | Decision | Choice | Alternative | Rationale |
|---|----------|--------|-------------|-----------|
| 1 | Config surface | `GameObject enemyPrefab` on `LevelConfig` | new `EnemyConfig` SO | 1 field vs new asset + indirection; jam scale. |
| 2 | Instantiation | Spawn under `enemyView.transform` | in-scene object ref | Per-level swap = one serialized ref. |
| 3 | Animator wiring | `public Animator Animator { get; set; }` (mirrors existing `Art`) | find-in-children / public field | 1 line, matches file pattern; view stays content-agnostic. |
| 4 | Reaction mapping | **A — tolerate missing triggers** | B — author reaction controller | Zero new art; robust to any controller; bubble unaffected. B = later art fix. |
| 5 | Background | Assign `BG_0` sprite to `LevelConfig.background` + `Backdrop` scale → (1,1) | instantiate `BG_0.prefab` | Zero code; `ApplyLayer` already swaps sprite/tint. |
| 6 | Front/foreground | Optional slots left **null** + guard | add an inactive `Foreground` object | No art; enabling later is scene-only, no code change. |

### 2. Instantiation contract
- `Instantiate(level.enemyPrefab, enemyView.transform, instantiateInWorldSpace: true)`, then `instance.transform.localPosition = Vector3.zero`.
- `worldPositionStays: true` preserves the prefab's authored **world scale 0.9** and auto-compensates the anchor's placeholder scale **4.7**; `localPosition = zero` lands the enemy on the anchor spot (5.43, 1.06) where the bubble already sits.
- **Gotcha:** the 2-arg `Instantiate(prefab, parent)` uses `worldPositionStays = false` → local pos (6.68,1.43) × 4.7 ≈ 31 units off-screen, scale ≈ 4.2. MUST use the 3-arg overload with `true`.
- Hide placeholder via `enemyView.HidePlaceholder()` → `sprite.enabled = false` (reuses the view's `sprite`). Creature parts are `SortingOrder` 18–26; the square (0) would peek, so hide it.
- `enemyPrefab == null` → early return; today's placeholder stays.

### 3/4. Animator + reaction guard
```csharp
public Animator Animator { get => animator; set => animator = value; }

void TriggerReaction(EnemyReaction r) {
    if (animator == null) return;
    string t = r.ToString();
    if (!HasParam(animator.parameters, t)) return;   // no warning spam
    animator.SetTrigger(t);
}
public static bool HasParam(AnimatorControllerParameter[] ps, string name) { /* pure, testable */ }

public void ResetToIdle() {
    if (animator != null && animator.HasState(0, Animator.StringToHash("Idle")))
        animator.Play("Idle", 0, 0f);
}
```
`SetTrigger` on a missing parameter warns every call — the guard stops the spam. `Creature1 1.controller` has **0 parameters**, default state `Creature 1 Attack`: Win/Patience/Bite/Eaten fire **nothing**, the creature loops its default `Attack` clip, bubble still works. **Flag:** default should be a looping `Creature 1 Idle` (art bug).

### 5. Placement / scale
Prefab authored (6.68, 1.43) scale 0.9; anchor (5.43, 1.06) scale 4.7. Rule: keep authored scale, snap to anchor. Camera ortho 5 (view ≈ 17.8×10) fits both; verify by screenshot, nudge prefab if off.

### 6. Background
`BG_0` = 1917×1074 @100 ppu → **19.17×10.74** units (fills the view). The `Backdrop` scale (40,24) suits the 0.64-unit placeholder, **not** full-res BG; set it to **(1,1,1)**. Zero code.

### 7. Front/foreground
No-op now = `LevelConfig.foreground` and `GameLoopController.foregroundRenderer` are both null → `ApplyForeground()` returns immediately; no `Foreground` object created. Later: create the layer, drag its renderer into the slot, assign art — no code change.

## Data Flow

    StartGame → ApplyLevelArt → SpawnEnemy (Instantiate under EnemyView.transform →
      EnemyView.Animator + HidePlaceholder); ApplyLayer(backdrop, level.background)
    ServeFood → EnemyView.PlayReaction → [guarded trigger] + bubble

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Assets/Scripts/Cards/LevelConfig.cs` | Modify | `+ GameObject enemyPrefab`, `+ Sprite foreground` |
| `Assets/Scripts/Characters/EnemyView.cs` | Modify | `+ Animator` setter, `+ HidePlaceholder()`, guard `PlayReaction`/`ResetToIdle`, `+ static HasParam` |
| `Assets/Scripts/GameLoop/GameLoopController.cs` | Modify | `+ _enemyInstance`, `+ foregroundRenderer`, `+ SpawnEnemy()`, `+ ApplyForeground()`; called from `ApplyLevelArt()` |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level1Mexico.asset` | Modify | `enemyPrefab = Creature1.prefab`; `background = BG_0` sprite |
| `Assets/Scenes/Prototype.unity` | Modify | `Backdrop` scale (40,24,1) → (1,1,1); `foregroundRenderer` null |
| `Assets/Tests/EditMode/` (asmdef + test) | Create | TDD bootstrap + guard test |

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit (EditMode) | `HasParam`: match / miss / case / null / empty | new `Assets/Tests/EditMode` asmdef, RED→GREEN |
| Integration | none — pure presentation | — |
| E2E (PlayMode) | Creature spawns on-screen at right size, BG renders, bubble works, no console errors; `enemyPrefab = null` restores placeholder | Play `Prototype.unity` at index 0, screenshot + `read_console` |

## Migration / Rollout

None. Rollback = unassign `LevelConfig.enemyPrefab` (placeholder returns) and revert the `Backdrop` scale; one work-unit commit (enemy + background).

## Open Questions

- None blocking. `Creature1 1.controller` default state `Attack` is an art bug (owner: artist; Decision B).
