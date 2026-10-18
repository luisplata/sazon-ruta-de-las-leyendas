# Tasks: Scene Lighting Integration

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~60–150 semantic (delete global + PrefabInstance block per scene); up to ~400 with MCP fileID churn |
| 400-line budget risk | Medium |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → Prototype lighting; PR 2 → Tutorial lighting |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

```text
Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: Medium
```

Semantic change is small; risk is diff **noise** — an MCP scene save can remap fileIDs across
the whole YAML (design.md risk), inflating the diff past 400. Two scenes are independent
deliverables, so splitting is cheap. **User must pick a chain strategy before apply.**

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Prototype.unity lit | PR 1 | Delete no-op global + instance prefab; PlayMode screenshot |
| 2 | Tutorial.unity lit | PR 2 | Same; independent of PR 1 |

## Phase 1: Work Unit 1 — Prototype.unity Lighting

- [ ] 1.1 [scene] `manage_scene(load, "Assets/Scenes/Prototype.unity")`.
- [ ] 1.2 [scene] `find_gameobjects("Global Light 2D", by_name)` → confirm instanceID `69492`; `manage_gameobject(delete, target=69492)`.
- [ ] 1.3 [scene] Instance `Assets/Prefabs/Luces escena.prefab` (guid `9370e1c6418667d4288a69b9db1e373d`) at root: try `manage_gameobject(create, prefab_path=…)`, else `execute_code` `PrefabUtility.InstantiatePrefab`. Name `Luces escena`.
- [ ] 1.4 [scene] Assert instance transform == `(-2.00678,-1.12508,0)`, scale `1`, no rotation; correct **only** if off. Confirm 5 Light2D children (1 Global type `4` + 4 Freeform).
- [ ] 1.5 [scene] `manage_scene(save)`; `refresh_unity`.
- [ ] 1.6 [verify] Play mode → `manage_camera(screenshot)` vs `Assets/Screenshots/explore_anims_intended.png` (teal global, warm pot glow, dark-teal vignette). `read_console` clean; exactly one Global Light 2D.
- [ ] 1.7 [verify] Only if shot shows misalignment: nudge amber children/group onto `BG.png`'s pot (never re-author prefab); record evidence in this file. Commit work unit 1.

## Phase 2: Work Unit 2 — Tutorial.unity Lighting

- [ ] 2.1 [scene] `manage_scene(load, "Assets/Scenes/Tutorial.unity")`.
- [ ] 2.2 [scene] Delete existing no-op white `Global Light 2D` (fileID `&619394800`, `m_Name` `:698`).
- [ ] 2.3 [scene] Instance the same prefab at the same root position `(-2.00678,-1.12508,0)`, scale `1`.
- [ ] 2.4 [scene] Assert transform + 5 children + exactly one Global; `manage_scene(save)`; `refresh_unity`.
- [ ] 2.5 [verify] Play + screenshot. Backdrop is a blank tinted rectangle → if the two `14.83` amber lights read as a random blob, **disable** (not delete) those children; else keep. `read_console` clean.
- [ ] 2.6 [verify] `git status`/diff: only `Assets/Scenes/Prototype.unity` + `Assets/Scenes/Tutorial.unity` changed; no `.cs`/`.mat`. Commit work unit 2.

## Phase 3: Regression Sanity (Strict TDD)

> Strict TDD is active, but this change is scene-only with **no pure logic** → no
> RED-GREEN-REFACTOR. Re-run EditMode tests only as a regression guard.

- [ ] 3.1 [verify] `unityMCP run_tests(mode="EditMode")` → expect **7/7 unchanged**.
- [ ] 3.2 [verify] Confirm rollback path documented: `git checkout -- Assets/Scenes/Prototype.unity Assets/Scenes/Tutorial.unity`.
