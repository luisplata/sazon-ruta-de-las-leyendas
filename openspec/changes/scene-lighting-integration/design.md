# Design: Scene Lighting Integration

## Technical Approach

Scene-only integration (proposal option A). In each of `Prototype.unity` and `Tutorial.unity`:
delete the no-op white `Global Light 2D`, then instance `Assets/Prefabs/Luces escena.prefab`
(guid `9370e1c6418667d4288a69b9db1e373d`) at its stored root position `(-2.00678, -1.12508, 0)`
(scale 1, no rotation). No code, no materials, no backgrounds. Both scenes match `anims.unity`
(ortho size 5, pos `(0,0,-10)`, bg `(0.1,0.07,0.06)`), and `BG.png` (1917×1074) matches
`mk/Escene 1.png` (1917×1075), so the prefab's world-space layout transfers 1:1.

## Exact Per-Scene Steps (identical in both scenes)

1. Load scene.
2. Delete GameObject `Global Light 2D` — Prototype instanceID **69492**, Tutorial instanceID
   **69492-equivalent** (fileID `&619394800`, `m_Name` at `:698`). Confirmed white `(1,1,1)`,
   intensity `1`, blend style `0` (Multiply), shadows off → no-op.
3. Instance the prefab at the root, **do not move it**: `PrefabUtility.InstantiatePrefab(...)`
   already places the instance at the prefab root's stored position `(-2.00678,-1.12508,0)`.
   Verify the instance's transform equals that exactly; correct only if it differs.
4. Save scene (`m_SceneDirty` → save), then `read_console`.

Result per scene: exactly **one** global light (prefab's teal, `(0.749,0.933,0.753)` @ `1.8`,
Multiply) + 4 freeforms (2× amber `14.83` pot glow, 1× amber `1.2` top fill, 1× dark-teal
AlphaBlend vignette).

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|---|---|---|---|
| Rig delivery | Instance prefab | Copy 5 lights (option B) | Single source of truth; picks up future prefab fixes; copies in 2 scenes drift. Jam-scale: maintainability > per-scene tweakability. |
| Double global | Delete scene's white global | Disable prefab global (C) | Scene's global is identity on Multiply — free to remove, and leaves one unambiguous global. |
| Tutorial rig | Same full prefab | Reduced Tutorial set | Start identical; trim only if screenshot shows a problem (contingency, not default). |
| Move behaviour | Keep stored root pos | Re-center to (0,0) | Stored pos reproduces the artist's world layout authored against `mk/`. |

## Sorting / Light-Layer Considerations

- All 5 prefab lights carry `m_ApplyToSortingLayers: 00000000` = **Default layer only**. Only
  one sorting layer exists (`Default`), and Backdrop/Midground/Player/Enemy all sit on `Default`
  (orders -100/-50/…) → lights affect every lit sprite. The mask is harmless today; a future new
  sorting layer would require updating the masks (out of scope).
- **Render-order: Light2D is not a sprite.** Lights are composited in the 2D light buffer and
  applied to lit renderers; the rig root's z does **not** occlude sprites. The dark-teal vignette
  is an AlphaBlend *light*, not a scene sprite, so it does not fight the -100/-50 sprite orders.
  **Keep the rig at z=0** (prefab default); do not raise/lower it to "fix" ordering.

## Double-Global Reconciliation

Net lighting per scene after the change = prefab's teal global on the Multiply blend style
(plus pools + vignette). The deleted white global contributed nothing (white × 1 = identity), so
no visual regression; there is no true "double-brightening". Deleting it removes dead weight and
the trap of a later style/colour edit accidentally stacking two globals.

## Light-Touch Tuning Policy

Default = **no tuning**. Only adjust if a PlayMode screenshot shows a problem:
- **Prototype**: confirm the two amber `14.83` lights land on `BG.png`'s Sazón pot. If misaligned,
  nudge only the amber children (or the group) — do not re-author the prefab.
- **Tutorial**: backdrop is a blank tinted rectangle (no pot). If the two amber lights read as a
  random warm blob, **disable** (not delete) those two children; keep global + vignette.
Any such edit requires screenshot evidence, recorded in tasks.md.

## Optional Scripted Integration (MCP)

Fully scriptable; no manual step. Per scene:
1. `manage_scene(load, Prototype.unity)`.
2. `find_gameobjects("Global Light 2D", by_name)` → instanceID 69492.
3. `manage_gameobject(action="delete", target=69492)`.
4. Instance prefab via `execute_code`: `PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Luces escena.prefab"))`, name `"Luces escena"`, assert transform `(-2.00678,-1.12508,0)`. (Try `manage_gameobject(create, prefab_path=…)` first; fall back to `execute_code`.)
5. `manage_scene(save)`.
6. Repeat for `Tutorial.unity`; `read_console` after each.

## File Changes

| File | Action | Description |
|---|---|---|
| `Assets/Scenes/Prototype.unity` | Modify | Delete no-op global; add `Luces escena` prefab instance. |
| `Assets/Scenes/Tutorial.unity` | Modify | Same. |
| `Assets/Prefabs/Luces escena.prefab` | Reference only | Not edited. |
| `Assets/Screenshots/*` | Create | PlayMode proof of both scenes. |

## Verification Plan

1. PlayMode Prototype → `manage_camera(screenshot)` → compare vs
   `Assets/Screenshots/explore_anims_intended.png` (teal global, warm pot glow, vignette).
2. `read_console` clean; confirm exactly one Global Light 2D.
3. Repeat for Tutorial. Confirm both scenes still load and play.

## Risks / Tradeoffs

- **Missed delete → two globals** (Med): verify count post-change.
- **Pot-glow misalign** (Low): screenshot-gated nudge.
- **Tutorial blob** (Med): disable pot-glow children if confirmed.
- **Prefab untested as instance** (Low): visual confirm required.
- **MCP YAML churn** changes fileIDs in scene files — cosmetic; rollback is
  `git checkout -- Assets/Scenes/Prototype.unity Assets/Scenes/Tutorial.unity`.

## Open Questions

- None blocking. Tutorial pot-glow trim is a verification contingency, not a design unknown.
