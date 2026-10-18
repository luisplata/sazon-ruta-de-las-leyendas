# Exploration — `scene-lighting-integration`

> Investigation only. No scene, prefab, or script was modified. `anims.unity` was loaded
> read-only via Unity MCP to capture the artist's intended look, then `Prototype.unity`
> was restored (editor confirmed back on `Assets/Scenes/Prototype.unity`).
> Unity live: instance `GameJamUni2@9eee0fc1`, Unity 6000.3.10f1, URP 17.3.0, Linear, quality `Very Low`.

## Overview

The ask is to make the new light rig `Assets/Prefabs/Luces escena.prefab`
(guid `9370e1c6418667d4288a69b9db1e373d`, author enierglobant, tested in `anims.unity`)
land in two gameplay scenes: `Assets/Scenes/Prototype.unity` and
`Assets/Scenes/Tutorial.unity`. Both scenes today run **flat, effectively unlit** 2D lighting
(a single white Global Light 2D that is a no-op), which is the literal "falta amor".

The prefab is a self-contained URP **Light2D** rig: 1 tinted global light + 3 warm amber
"pool" lights + 1 large dark-teal alpha-blend shape that acts as a vignette/darkening layer.
The scene's sprites are **already** on `Sprite-Lit-Default`, so lights WILL affect them —
no material swap is required. The main integration hazards are (a) not duplicating the
existing Global Light, and (b) verifying alignment against the composed background art.

Key correction to the briefing: the "dark teal" light is **not** a Subtractive overlap —
`Light2D.OverlapOperation` is `{ Additive = 0, AlphaBlend = 1 }`, and that light is `1`
(**AlphaBlend**). It darkens by alpha-blending a dark teal shape over the lit result
(vignette / ambient-occlusion trick), not by subtracting light.

---

## Prefab anatomy — `Assets/Prefabs/Luces escena.prefab`

Root `Luces escena` (GameObject `&2676595284654562683`, Transform `&112220007993996133`):
layer `Default (0)`, local position `(-2.00678, -1.12508, 0)`, scale `1`, no components
besides Transform, 5 children. All lights use the Light2D script
`guid 073797afb82c5a1438f328866b10b3f0`, `m_BlendStyleIndex: 0`,
`m_ApplyToSortingLayers: 00000000`, `m_UseNormalMap: 0`, `m_LightOrder: 0`.

| Child (line) | Local pos | World pos *(root + local)* | LightType | Color (r,g,b) | Intensity | FalloffInt. | FalloffSize | Overlap | Shadows |
|---|---|---|---|---|---|---|---|---|---|
| `Global Light 2D` (137) | (2.58678, 3.17508) | **(0.58, 2.05)** | `4` Global | teal-green `(0.749, 0.933, 0.753)` | `1.8` | 0.5 | — | Additive (0) | on, 0.75 / soft 0.3 |
| `Freeform Light 2D` (404) | (1.22378, -0.92192) | **(-0.78, -2.05)** | `1` Freeform | amber `(1.0, 0.829, 0.410)` | `14.83` | 0.5 | 0.5 | Additive (0) | on, 0.75 |
| `Freeform Light 2D (1)` (219) | (1.22378, -1.85492) | **(-0.78, -2.98)** | `1` Freeform | amber `(1.0, 0.829, 0.410)` | `14.83` | 0.5 | 0.5 | Additive (0) | on, 0.75 |
| `Freeform Light 2D (2)` (14) | (3.88678, 5.89508) | **(1.88, 4.77)** | `1` Freeform | amber `(1.0, 0.829, 0.410)` | `1.2` | 0.5 | 0.5 | Additive (0) | on, 0.75 |
| `Freeform Light 2D (3)` (303) | (2.69678, 2.10508) | **(0.69, 0.98)** | `1` Freeform | dark-teal `(0.0706, 0.1569, 0.1529)` | `2.45` | 0.294 | `2.57` | **AlphaBlend (1)** | on, 0.461 / soft 1.0 |

Interpretation:
- **Global Light 2D** — tints the entire frame teal-green and lifts it (Multiply-style,
  intensity 1.8). This replaces the plain white global in each scene.
- **Two amber `14.83` freeforms** (lines 404 & 219) are the **warm focal glow** (the
  Sazón pot). Their shapes are small (`~1.5 × 0.7` units, ShapePath lines 287-291 / 472-476)
  and sit at y ≈ -2, i.e. the pot base.
- **Amber `1.2` freeform** (line 14) is a smaller/wider warm fill higher up (y ≈ 4.77,
  near the top edge of the ortho frame).
- **Dark-teal freeform (3)** (lines 303-392) is the mood piece: an enormous ShapePath
  (bounds extent ≈ `14.7 × 8.4`) with falloff `2.57` and **AlphaBlend** overlap — it paints
  a soft dark-teal wash that darkens the frame edges/background = the vignette.
- `m_ShadowsEnabled: 1` on all lights is **inert**: no sprite in either scene casts 2D
  shadows (`m_CastShadows: 0`, no `ShadowCaster2D`), so shadow settings do nothing today.

Blend-style semantics — from `Assets/Settings/Renderer2D.asset` (lines 33-45):
index `0` = **"Multiply"** (blendMode 1), `1` = Additive, `2` = Multiply-with-Mask,
`3` = Additive-with-Mask. Every prefab light (and the scenes' existing globals) is on
style `0` (Multiply). So the amber pools "multiply" their warm color into the scene
(intensity 14.83 → strong warm brightening inside the shape), and the dark-teal shape
alpha-blends a dark wash on top.

---

## Intended composition — from `anims.unity` (artist's test scene)

Evidence: `Assets/Scenes/anims.unity` + MCP hierarchy of `anims`.

- The scene root set is: `Main Camera` (MCP instanceID **71320**, pos `(0,0,-10)`, ortho
  size 5), `timeline` (instanceID **71348**, pos `(-3.98344, 0.0131, 0)`, 17 children,
  Animator `bg1.anim` / `timeline.controller`), and `Luces escena` (instanceID **-4838**,
  **`activeSelf: false` → DISABLED**, 5 children).
- **The artist left the `Luces escena` prefab instance disabled** (`anims.unity:1820-1822`;
  MCP `activeSelf:false`). The *live, working* lights are separate GameObjects parented
  under `timeline` (`Freeform Light 2D`, `(1)`, `(2)`, `(3)`, `Global Light 2D` at
  `anims.unity:229/421/688/1090/1447`).
- Those live lights have the **same component values as the prefab**, and their world
  positions match the prefab's computed world positions exactly
  (e.g. global: `(4.56344,2.0369)+(-3.98344,0.0131)=(0.58,2.05)`; dark-teal:
  `(4.67344,0.9669)+(-3.98344,0.0131)=(0.69,0.98)` — identical to the prefab table above).
  → **The prefab is a faithful repackage of the tested rig; instancing it at its stored
  root position reproduces the intended world-space lighting.**
- Background in `anims` = the **`Assets/mk/` split background** (`Escene 1.png` +
  `Escene 1_1..1_9.png`, 1917×1075 each, driven by `bg1.anim`), NOT the single `BG.png`.
- Screenshot of the intended look: `Assets/Screenshots/explore_anims_intended.png`.
  It shows the full art: hero (left rock) + Sazón pot (centre, warm amber glow at its base)
  + monster (right rock), under a teal/green ambient with a dark vignette around the frame.
  The warm pot glow IS the two amber `14.83` lights; the teal cast IS the global; the
  darkened frame IS the dark-teal alpha-blend shape.

---

## Target scenes — current state

### `Assets/Scenes/Prototype.unity` (the gameplay scene)

- **Camera** `Main Camera` (MCP instanceID **69474**, file `:547`): orthographic,
  `orthographic size: 5` (`:640`), position `(0,0,-10)`, background
  `(0.1, 0.07, 0.06)` dark brown (`:615`), post-processing OFF (`:584`). Framing is
  **identical to `anims`**.
- **Existing lighting**: exactly ONE Light2D — `Global Light 2D`
  (MCP instanceID **69492**, file `&619394800` at `:787`, component `&619394801` `:793`):
  LightType `4` Global, **white `(1,1,1)`, intensity `1`, blend style `0` (Multiply)**,
  shadows off. White × 1 on Multiply = the identity → **this light has no visual effect;
  the scene is effectively unlit.**
- **Background**: `Backdrop` (MCP instanceID **69634**, file `:2562`) = SpriteRenderer,
  sprite guid `e19d0ec9042957d48b97fc4ce6b927c8` = **`Assets/_sprites/BG.png`**,
  tint `(0.62, 0.55, 0.5)`, `SortingOrder -100`, scale `1`. `Midground`
  (instanceID **69486**, file `:696`) is a `WhiteSprite` band (`40 × 8` at `y=-2.5`,
  sorting `-50`) but is **inactive** (`m_IsActive: 0`).
- **Sprites are LIT**: `Backdrop`, `Player/Visual`, `Enemy/Visual` and `Midground` all use
  material guid `a97c105638bdf8b4a8650670310a4cd3` = **URP built-in `Sprite-Lit-Default`**
  (`Library/PackageCache/.../Universal/Runtime/Materials/Sprite-Lit-Default.mat.meta`).
  4 lit renderers (`Prototype.unity:727, 1920, 2312, 2593`). **No `Sprite-Unlit-Default`
  (guid `9dfc825aed78fcd4ba02077103263b40`) anywhere in the project.**
- Player (instanceID **69576**) / Enemy (instanceID **69590**) are placeholder
  `WhiteSprite` visual squares with Animators. `Canvas` (instanceID **69640**, layer 5)
  is screen-space UI (unaffected by 2D lights).
- Screenshot of current look: `Assets/Screenshots/explore_prototype_current.png`
  — flat, no focal glow, no vignette; BG.png's baked shading is all the depth there is.

### `Assets/Scenes/Tutorial.unity`

- Same skeleton as Prototype: `Main Camera` (`:549`), ortho size 5, background
  `(0.1, 0.07, 0.06)` (`:617`), post-processing off.
- **Existing lighting**: exactly ONE Light2D — `Global Light 2D`
  (`&619394800` at `:698`, component `:704`): Global, **white, intensity 1, Multiply** →
  again a **no-op / flat**.
- **Background**: `Backdrop` (`:2745`) = SpriteRenderer using **`WhiteSprite`**
  (guid `b8d3af21e3cfb984c8d20407684086ba`), tint `(0.62, 0.55, 0.5)`, scale `40 × 24`,
  sorting `-100` (`:2776-2820`). I.e. a plain tinted rectangle — **no scene art, no pot.**
- 4 lit `Sprite-Lit-Default` renderers (`Tutorial.unity:1900, 2496, 2777, 3127`), Player/Enemy
  placeholder squares. No other Light2D.

### Supporting facts

- `Renderer2D.asset` is the active 2D renderer (`UniversalRP.asset:19-21`,
  guid `424799608f7334c24bf367e4bbfa7f9a`); default lit material is `Sprite-Lit-Default`
  (`Renderer2D.asset:66-68`). Light2D IS rendered.
- Only ONE sorting layer exists: `Default` (`ProjectSettings/TagManager.asset:33-35`).
  All lights' `m_ApplyToSortingLayers: 00000000` is the same default seen in every scene
  (including the working `anims`), so the mask is consistent/harmless today — but any new
  sorting layer added later would need the masks updated.
- `BG.png` is `1917×1074`; `mk/Escene 1.png` is `1917×1075` — **same composition scale**,
  so the prefab's pot-glow placement transfers to Prototype's background.

---

## Gaps — "what lacks love" (concrete)

1. **Prototype is effectively unlit.** Its only light is white/intensity-1 on Multiply
   (`Prototype.unity:809-813`) = identity. No colour, no accent, no depth. Evidence:
   screenshot `explore_prototype_current.png`.
2. **Tutorial is identical**: one no-op white global (`Tutorial.unity:720-724`), a flat
   tinted rectangle backdrop. Even less to light than Prototype.
3. **No warm focal glow.** The Sazón pot — the visual focus of the composed art — has no
   light on it. In `anims`, the pot base carries the two amber `14.83` lights; neither
   scene has them.
4. **No vignette / ambient darkening.** `anims` uses the huge dark-teal AlphaBlend shape to
   frame and mood the scene; neither scene has it.
5. **Duplicated global light on integration.** Dropping `Luces escena` as-is adds a second
   `Global Light 2D` next to Prototype's/Tutorial's existing one. Both are on Multiply
   style, so the existing white@1 is a no-op and the net result is just the new teal global
   (it does **not** truly "double-brighten") — but two global lights in one scene is
   ambiguous, dead weight, and a trap if anyone later flips one to Additive. One must go.
6. **Prefab is untested *as an instance*.** The artist disabled the `Luces escena`
   instance in `anims` (MCP instanceID -4838, `activeSelf:false`) and lit the scene with
   duplicates under `timeline`. Component values + world positions match, so instancing
   *should* reproduce the look — but this has not been visually confirmed in a scene.
7. **Background difference (Tutorial).** Prototype's `Backdrop` is `BG.png` (art with a
   pot); Tutorial's is a glyph-less tinted rectangle. The prefab's pot-glow is meaningless
   in Tutorial, so a straight copy of all 5 lights may read as a random warm blob.
8. **Inert shadow settings.** Prefab lights have shadows enabled but no 2D shadow casters
   exist → no effect. Harmless, but not "polish" either.
9. **Alignment unverified in Prototype.** Light positions were authored against `mk/`
   (== `BG.png` scale). Must be confirmed on-screen in Prototype that the amber lights land
   on the pot, not on the rocks/water.

---

## Integration options (tradeoffs)

| # | Approach | Pros | Cons | Effort |
|---|----------|------|------|--------|
| A | **Instance `Luces escena` prefab per scene, delete the scene's existing Global Light** | WYSIWYG with artist's intent; single prefab to maintain; pick up future prefab fixes; fastest | Brings a redundant Global Light (the scene's must be deleted); all 5 lights move as one rigid group; not per-scene tunable without overrides | Low |
| B | **Unpack / copy the 5 lights into each scene, delete old global** | Per-scene tweakable (e.g. Tutorial without the pot glow); no prefab linkage surprises | Config duplicated in 2 scenes → drift; more clicks; loses prefab single-source | Low–Med |
| C | **Instance the prefab but disable its Global Light child; keep/recolour the scene's existing global** | One global; reuses the pool + vignette lights; scene keeps its own global for global tweaks | Override bookkeeping; must port the teal tint/intensity onto the scene global; two places define lighting | Low–Med |
| D | **Swap sprites to `Sprite-Lit-Default`** | — | **Not needed — sprites are already Lit** (`a97c…`). Ruled out. | — |
| E | **Avoid double-global** | | Options: delete scene global (with A), or disable prefab global (C). Deleting the scene global is simplest because it is a no-op anyway. | — |

---

## Open questions / assumptions

1. **Tutorial's intent.** Its backdrop is a blank tinted rectangle. Should Tutorial get the
   full 5-light rig, or only the global + vignette (skip the pot glow that has nothing to
   sit on)? *Assumption for the recommendation: start with the full prefab, then trim the
   pot glow if it looks wrong — verify on screen.*
2. **Is `anims` the final look, or a WIP?** The prefab could be mid-iteration (instance
   disabled). Assumed stable because component values are coherent and world positions match.
3. **Do the scenes need a background swap** to the `mk/` split art (as `anims` uses), or is
   `BG.png` (Prototype) final? This change is scoped to *lights*, so assumed no BG swap.
4. **Per-scene light tuning** (does the team want Tutorial's mood different from the
   gameplay scene)? Affects A vs B/C.
5. **Sorting layers**: only `Default` exists; assumed no new layers are added by this change.

---

## Recommended approach (10x-simpler, jam-scale)

Do the same, minimal, To-Do in each scene — no architecture, no new scripts:

1. In `Prototype.unity`: **delete the existing no-op `Global Light 2D`** (instanceID 69492),
   then **instance `Assets/Prefabs/Luces escena.prefab` at its stored root position
   `(-2.00678, -1.12508, 0)`** (do not move it — that position reproduces the artist's
   world-space layout). This yields exactly one global (the teal one) plus the 4 freeforms.
2. Enter Play mode, screenshot, and confirm on-screen that the amber lights sit on the pot
   and the dark-teal wash frames the scene. Nudge only the group or a single light if needed.
   `read_console` afterwards.
3. In `Tutorial.unity`: same two steps (delete its no-op global at `:698`, instance the
   prefab). Then decide whether to keep the pot-glow lights given Tutorial's blank backdrop
   (open question #1) — trim only if it reads wrong.
4. Stop. No material changes, no shadow-caster additions, no new sorting layers.

Rationale: instancing the prefab is the single highest-value, lowest-effort move; it imports
the exact rig the artist already balanced, works in both scenes because the cameras and
backgrounds are dimensionally identical, and deleting the redundant white global is both
required (avoid two globals) and free (it does nothing). Option A keeps the rig as one
maintainable unit, which matters more than per-scene tuning at jam scale. Fall back to B/C
only if the team wants Tutorial lit differently.

Non-goals to call out explicitly: no `Sprite-Unlit`→`Lit` swap (already lit), no background
art changes, no code changes, no new Light2D authored from scratch.

---

## Evidence index

- Prefab: `Assets/Prefabs/Luces escena.prefab` (root `&112220007993996133`; lights at
  lines 14, 137, 219, 303, 404) · `.meta` guid `9370e1c6418667d4288a69b9db1e373d`.
- `Assets/Scenes/Prototype.unity`: camera `:547`/`:615`/`:640`; existing global `:787-813`;
  `Backdrop` `:2562`; `Midground` `:696` (inactive); lit materials `:727,1920,2312,2593`.
- `Assets/Scenes/Tutorial.unity`: camera `:549`/`:617`; existing global `:698-724`;
  `Backdrop` `:2745-2820`; lit materials `:1900,2496,2777,3127`.
- `Assets/Scenes/anims.unity`: live global light `:1090`; prefab instance (disabled)
  `:1768-1828` (`m_IsActive: 0` at `:1822`); `timeline` `:826-896`.
- `Assets/Settings/Renderer2D.asset` `:33-45` (blend styles), `:66-68` (lit/unlit defaults).
- `Assets/Settings/UniversalRP.asset` `:19-21` (active renderer list).
- `ProjectSettings/TagManager.asset` `:33-35` (sorting layers).
- URP package: `Sprite-Lit-Default.mat.meta` guid `a97c105638bdf8b4a8650670310a4cd3`;
  `Sprite-Unlit-Default.mat.meta` guid `9dfc825aed78fcd4ba02077103263b40`.
- MCP (Unity `GameJamUni2@9eee0fc1`): Prototype roots — Main Camera `69474`,
  Global Light 2D `69492`, Backdrop `69634`, Midground `69486` (inactive), Player `69576`,
  Enemy `69590`, Canvas `69640`, GameLoop `69596`, EventSystem `69656`.
  anims roots — Main Camera `71320`, timeline `71348`, `Luces escena` `-4838` (inactive).
- Screenshots: `Assets/Screenshots/explore_prototype_current.png` (current, flat),
  `Assets/Screenshots/explore_anims_intended.png` (intended look).
