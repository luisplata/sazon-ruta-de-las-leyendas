# Delta for scene-lighting

## ADDED Requirements

### Requirement: Luces escena Rig Instanced In Both Scenes

Both `Assets/Scenes/Prototype.unity` and `Assets/Scenes/Tutorial.unity` SHALL contain the
`Luces escena` Light2D rig as a **prefab instance** of
`Assets/Prefabs/Luces escena.prefab` (guid `9370e1c6418667d4288a69b9db1e373d`) at the authored
root position `(-2.00678, -1.12508, 0)`, resolving to exactly 5 Light2D children
(1 global + 4 freeform).

#### Scenario: Prefab instanced at authored position in each scene

- GIVEN `Prototype.unity` and `Tutorial.unity` are open in turn
- WHEN each scene's `Luces escena` root is inspected
- THEN it is a prefab instance of `Luces escena.prefab` (guid `9370e1c6418667d4288a69b9db1e373d`)
- AND its transform position equals `(-2.00678, -1.12508, 0)` with scale `1`
- AND it has 5 Light2D children, and each scene contains exactly one such instance

### Requirement: Exactly One Global Light Per Scene

Each scene SHALL contain exactly one Global Light2D — the `Global Light 2D` child of the
prefab. The pre-existing no-op white `Global Light 2D` in each scene SHALL be removed, so no
duplicate/redundant global remains.

#### Scenario: No-op global removed

- GIVEN `Prototype.unity` (prior global id `69492`) and `Tutorial.unity` (prior global id `619394800`)
- WHEN the light rig is integrated
- THEN each scene's pre-existing white `Global Light 2D` is deleted

#### Scenario: Single global remains

- GIVEN a scene with the rig instanced and the old global deleted
- WHEN all Light2D components in the scene are counted by type
- THEN exactly one Global (LightType 4) light exists
- AND it is the prefab's teal global

### Requirement: Lighting Contributes To The Scene

The 2D lighting SHALL visibly affect the rendered scene: the active renderer SHALL remain
`Renderer2D`, sprites SHALL remain on `Sprite-Lit-Default`, and the scene SHALL NOT render
flat/unlit.

#### Scenario: Renderer and materials lit

- GIVEN `Assets/Settings/UniversalRP.asset` and the scene sprite renderers
- WHEN inspected
- THEN `Renderer2D.asset` is the active 2D renderer
- AND the scene's lit sprites reference `Sprite-Lit-Default`
- AND no `Sprite-Unlit-Default` is introduced

#### Scenario: Visible lighting effect

- GIVEN the scene is in Play mode
- WHEN the frame is captured
- THEN a teal global cast, warm pool glow, and dark-teal vignette are readable
- AND the frame is not identical to the flat pre-change look

### Requirement: Alignment Verified By Screenshot

Placement SHALL read correctly against the composed scene, confirmed by a PlayMode screenshot;
the rig's authored layout SHALL be preserved unless a screenshot shows a genuine misalignment.

#### Scenario: Alignment confirmed

- GIVEN the rig is instanced and the scene is in Play mode
- WHEN a screenshot is captured
- THEN the warm pool lights read as intended against the scene art
- AND no light group position was changed absent a confirmed misalignment

#### Scenario: Console clean

- GIVEN the scene has run in Play mode
- WHEN the console is read
- THEN no new errors are reported

### Requirement: Prefab Is Single Source Of Truth

The rig SHALL be consumed as a prefab instance, not copied or unpacked into the scenes.
`Assets/Prefabs/Luces escena.prefab` SHALL NOT be edited by this change.

#### Scenario: Instance, not copy

- GIVEN an integrated scene
- WHEN the `Luces escena` root is inspected
- THEN it remains prefab-linked to `Luces escena.prefab`
- AND the prefab asset is unmodified

### Requirement: No Code Or Material Changes

This change SHALL NOT modify any script or material asset; it is scene-only.

#### Scenario: Scene-only diff

- GIVEN the change's diff
- WHEN reviewed
- THEN only `Assets/Scenes/Prototype.unity` and `Assets/Scenes/Tutorial.unity` change
- AND no `.cs` or `.mat` file is modified

---

**Non-normative notes (assumptions; not requirements):**

- Both scenes use the same rig (no reduced Tutorial set).
- `BG.png` stays as-is; no `mk/` background split integration.
- The prefab's authored values are treated as the intended look.
- Tutorial's backdrop is a blank tinted rectangle, so the pot glow may read differently; trim
  it only if a screenshot confirms it.
