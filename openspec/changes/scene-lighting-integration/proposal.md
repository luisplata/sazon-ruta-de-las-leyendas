# Proposal: Scene Lighting Integration

## Intent

`Prototype.unity` and `Tutorial.unity` are **effectively unlit**: each has one white Global
Light 2D (intensity 1, Multiply = identity). An artist already balanced a full URP Light2D rig
(`Luces escena.prefab`) in `anims.unity` — teal global, warm pool lights on the Sazón pot,
dark-teal vignette. This change brings that rig into both scenes so the lighting reads.

## Scope

### In Scope
- Instance `Luces escena.prefab` in both scenes at root `(-2.00678, -1.12508, 0)`.
- Delete each scene's no-op white `Global Light 2D` (avoid double-global).
- Verify alignment; capture PlayMode screenshots.
- Optional LIGHT-touch tuning **only if** the screenshot demands it.

### Out of Scope
- Code/material changes (sprites already `Sprite-Lit-Default`).
- Background / `mk/` split work; `BG.png` stays as-is.
- Re-authoring prefab values; new scenes/levels; shadow casters; new sorting layers.

## Capabilities

### New Capabilities
- `scene-lighting`: both scenes SHALL render the `Luces escena` rig (teal global, warm pool
  lights, vignette) with exactly one global light per scene.

### Modified Capabilities
- None. (`level-background-art` owns background assignment, not lighting.)

## Approach

Scene-only, 10x-simpler path (exploration option A). Per scene: delete the no-op global,
instance the prefab at its stored position (reproduces the artist's world layout), Play mode,
screenshot, confirm alignment, `read_console`. No code, no materials.

### Alternatives Considered
- **Instance prefab, not copy lights** — keeps one source of truth; copies drift.
- **Delete the no-op global, don't disable** — free and unambiguous.
- **Same rig both scenes** — simpler; trim Tutorial's pot glow only if it reads wrong.

## Affected Areas

- `Assets/Scenes/Prototype.unity` — Modified: delete no-op global, instance prefab.
- `Assets/Scenes/Tutorial.unity` — Modified: same.
- `Assets/Prefabs/Luces escena.prefab` — Referenced only (not edited).

## Assumptions

1. **Same rig for both scenes** (not a reduced Tutorial set).
2. Prefab's values are the intended look (the `anims.unity` instance was disabled — treat the
   composition as final-ish).
3. `BG.png` stays as-is (no `mk/` split integration).
4. Prefab stays the single source of truth (instance it, don't copy lights).

## Risks

- **Two globals → ambiguous trap** (Med) → delete the scene's no-op global.
- **Pot-glow misaligns on Prototype BG** (Low) → screenshot-verify; nudge group.
- **Tutorial pot-glow reads oddly** (Med) → trim that light if confirmed.

## Rollback Plan

`git checkout -- Assets/Scenes/Prototype.unity Assets/Scenes/Tutorial.unity`. The prefab is
untouched, restoring the exact prior flat-lighting state. No code/asset migration.

## Dependencies

- Unity Editor live via MCP; `Renderer2D.asset` active (already true).

## Success Criteria

- [ ] Both scenes show teal global + warm pool lights + vignette.
- [ ] Exactly one global light per scene.
- [ ] Alignment matches the intended `anims` look.
- [ ] Console clean; PlayMode screenshots captured.
