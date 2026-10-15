# Explore — enemy-level-integration

**Change**: `enemy-level-integration`
**Project**: Sazón: Ruta de las Leyendas (Unity 6000.3.10f1, URP 17.3.0, 2D)
**Date**: 2026-10-15
**Scope**: Investigation only — no gameplay code written.

---

## Overview

The user wants three things for LEVEL 1 (México = **El Ahuizotl**, played in `Prototype.unity`):

1. Give the level-1 enemy **real gameplay presence** — today it is a white square placeholder.
2. Make the enemy **swappable per level** (level 2 Colombia = **Hombre Caimán** will ship its own prefab).
3. **Integrate the background** (the `BG_0` art), with a **front/foreground layer reserved but DISABLED** until the artist delivers that art.

Key discovery: **an animated enemy prefab already exists but is completely unintegrated.** The last two commits (`20bda9a Enemy and background 1`, `63ccf11 anims creature 1`) added `Creature1.prefab`, `BG_0.prefab` and their sprites/controllers, but **no scene references them** (grep across `Assets/Scenes/*.unity` finds zero references to either prefab GUID). This change is the "wire them up" step.

Second key discovery: the enemy **gameplay rules already exist and work** (`GameLoopController` + `EnemyView` reactions). What is missing is not logic — it is *which visual is shown* and *how it is swapped per level*. So this is mostly a **presentation/wiring** change, not a rules change. That keeps it well under the <1h jam slice.

---

## Current state (file:line evidence)

### Where the enemy comes from today

- **Not instantiated at runtime.** In `Prototype.unity` the GameObject `Enemy` (scene line 1981/2002) is a hand-placed scene object carrying:
  - `EnemyView` component (`Prototype.unity:2008-2017`, script GUID `d310639d9a2ab2a46b96d90e5a714d55`).
  - Child placeholder `SpriteRenderer` (`m_SortingOrder: 0`, white-square sprite guid `b8d3af21e3cfb984c8d20407684086ba`, tint `(0.95,0.5,0.45)` — `Prototype.unity:1942-1945`).
  - Child bubble `Canvas` + `TMP` (wired in `Prototype.unity:2024-2025`).
  - Child `Animator` using `Animations/Enemy.controller` (guid `09954f2b912004141aef49b2f4caf28c`, `Prototype.unity:1979`).
- `EnemyView` is referenced by `GameLoopController.enemyView` as a **serialized scene reference** (`Prototype.unity:2111`). There is **no spawn code anywhere**.
- `Tutorial.unity` has the identical structure: `Enemy` at line 1981, same `EnemyView` script (line 1996) and same `Enemy.controller` (line 1958), tint `(0.55,0.65,0.75)`.

### What `EnemyView` does (Assets/Scripts/Characters/EnemyView.cs)

- Fields: `animator`, `sprite`, `art`, `tint`, `bubble`, `bubbleText`, `bubbleHideDelay`, four phrase pools (`winPhrases`/`patiencePhrases`/`bitePhrases`/`eatenPhrases`) — lines 16-52.
- Public API: `PlayReaction(EnemyReaction, string preferredPhrase=null)` (79), `SayPhrase(EnemyReaction)` (86), `Say(string)` (96), `ResetToIdle()` (102), `Art` setter (30-34).
- `PlayReaction` does `animator.SetTrigger(reaction.ToString())` (line 81) — triggers **must match `Enemy.controller` params** `Win`/`Patience`/`Bite`/`Eaten` (verified in `Animations/Enemy.controller:133-157`; one-shot states auto-return to `Idle`, default state `Idle`).
- `ApplyArt` (64-76): real `art` sprite replaces the placeholder and renders un-tinted; empty slot keeps the tint.
- **It is a pure view.** It holds no lives/patience/win/lose — those live in `GameLoopController`.

### Enemy gameplay rules already implemented (Assets/Scripts/GameLoop/GameLoopController.cs)

- `Enemy => handController.CurrentLevel` (line 74) — the enemy identity **is the current `LevelConfig`**.
- `StartGame()` (90) → `ApplyLevelArt()` (110) → `_lives=MaxLives`, `_patience=MaxPatience`, HUD, fade-in, deal.
- `ApplyLevelArt()` (110-118) already pushes `level.enemyArt` into `enemyView.Art` and `level.background`/`level.midground` into `backdropRenderer`/`midgroundRenderer`.
- `ServeFood()` (157-198) is the outcome switch: Win → `enemyView.PlayReaction(Win, recipeReactionText)`; Presented/Cursed/Filler → patience −1 → `PlayReaction(Patience)`; Fail → `PlayReaction(Bite)` + `Bite()`; `Bite()` (227) → life −1 and at 0 lives `PlayReaction(Eaten)`.
- All view calls are null-guarded (`enemyView?.`).

### How a level is selected (Tutorial vs Level1 vs Level2)

- `LevelConfig` SO (`Assets/Scripts/Cards/LevelConfig.cs`) fields: `levelName`, `regionName`, `requiredIngredient`, `recipes`, `legendTitle`, `legendText`, `audioNarration`, `marketConfig`, `background`, `midground`, `enemyArt` (lines 13-45). **No prefab/enemy-prefab field exists.**
- `HandController.Awake()` (`Assets/Scripts/Cards/HandController.cs:230-244`): if `GameSession.tutorialLevel != null` → use it (Tutorial), else `recipeDatabase.levels[GameSession.currentLevelIndex]` (0=México, 1=Colombia).
- `GameSession.currentLevelIndex` is 0-based into `RecipeDatabase.levels` (`RecipeDatabase.asset:15-17` → Mexico, Colombia); advanced by `GameLoopController.LoadNextScene` (250-274). `GameSession.tutorialLevel` is set by `MenuController` before loading the Tutorial scene (`GameSession.cs:35`).
- Level flow: Map `PlayLevel()` → `Legend` → `Market` → **`Prototype`** (`MapController.cs:134`, `Scenes.cs:13`). `Prototype.unity` is the real gameplay scene; `Tutorial.unity` is the guided tutorial.
- Assets are still empty: `LevelConfig_Level1Mexico.asset` → `background: {fileID: 0}`, `midground: {fileID: 0}`, `enemyArt: {fileID: 0}` (same for Colombia and Llorona).

### The unintegrated art assets

- `Assets/Prefabs/Creature1.prefab` — self-contained animated creature: root GameObject `Creature1` (line 655) with an `Animator` (line 677, controller guid `5187571f9d705cb46895b2d2e27a65b8`) + a child rig `Hip`(710) → `Body`(105)/`Tail`(290)/`Back leg`(199)/`back leg 1`(564), and `Head`/`tail hand`. Parts are placed sprites of `creature hambre 1.png` (guid `75f42c264b8c977499bd7b42012de992`, 1917-ish sheet, 10 sub-sprites). Root transform in prefab: position `(6.68, 1.43)`, scale `0.9`. **No `EnemyView`, no `SpriteRenderer` on root, no bubble.**
- `Animations/Creature1 1.controller` (guid `518...`): states `Creature 1 Idle` + `Creature 1 Attack`, **zero animator parameters, zero transitions**, and `m_DefaultState` = **`Creature 1 Attack`** (line 101). This does **not** expose `Win`/`Patience`/`Bite`/`Eaten`.
- `Animations/Enemy.controller` (guid `0995...`, used by the scene placeholders): the reaction-capable one (Idle + Win/Patience/Bite/Eaten one-shots).
- `Assets/Prefabs/BG_0.prefab` — a **single** GameObject `BG_0` with one `SpriteRenderer` (sprite `BG_0` from `BG.png`, guid `e19d0ec9042957d48b97fc4ce6b927c8`, world size 19.17×10.74, sorting order 0). **No children, no layer structure.**
- `Assets/_sprites/BG.png.meta`: `spriteMode: 2` but only **one** sub-sprite `BG_0` sized 1917×1074 (≈16:9 full-screen backdrop) — so the art is a **single flat background**, not a back/mid/front set.

### How the background is represented in the scene today

- `Prototype.unity` has two placeholder layers, both wired to `GameLoopController`:
  - `Backdrop` (line 2548/2559): `SpriteRenderer` `m_SortingOrder: -100` (2612), white-square sprite, scale `40×24`, colour `(0.62,0.55,0.5)` (2615). Wired at `Prototype.unity:2112`.
  - `Midground` (line 696): `SpriteRenderer` `m_SortingOrder: -50` (749), white-square sprite, colour `(0.55,0.48,0.44)` (752). Wired at `Prototype.unity:2113`.
  - `Enemy` sprite order 0 (1942), `Player` sprite order 1 (2331). Canvas/UI order 0 on layer 5.
- `Tutorial.unity` also has `Backdrop` (2745) + `Midground` (3095).
- **No `Foreground`/`Front`/`Fondo` object exists in any scene** (grep `Front|Foreground|Frente` over `Assets/Scenes` + `Assets/Prefabs` = 0 hits). "The front" is a new concept. `docs/art-brief.md:35-37` only asks for a flat "Fondo de la partida", never a layered foreground.

### Art brief confirmation (docs/art-brief.md)

- Characters (line 25-33): La Llorona (tutorial), El Ahuizotl (México), Hombre Caimán (Colombia), el chef — "El enemigo cambia según el nivel → se necesitan los 3 sprites".
- Background (line 36): one per country + tutorial; today a solid brown colour.

---

## Enemy gameplay mechanism (proposed, simplest)

The rules are done. The mechanism is: **a per-level enemy visual is instantiated into the existing scene `Enemy` anchor at `StartGame`, and its `Animator` is plugged into the unchanged `EnemyView`** (reactions + phrase bubble stay as-is).

Minimal flow:

1. `LevelConfig` gains one serialized reference: `public GameObject enemyPrefab;` (Unity can serialize a prefab asset reference as `GameObject`).
2. `GameLoopController.StartGame()` (before/inside `ApplyLevelArt()`), calls a small `SpawnEnemy()`:
   - `Destroy(_enemyInstance)` if a previous one exists (scene reload already clears it, but null-guard);
   - if `enemyPrefab != null`: `Instantiate(enemyPrefab, enemyAnchor)` (the existing `Enemy` transform), reset local position/scale to the anchor, then find `GetComponentInChildren<Animator>()` and assign it to `EnemyView`;
   - hide the placeholder `SpriteRenderer` while a prefab instance is active (so the white square does not sit behind/over the creature);
   - if `enemyPrefab == null`: keep today's behaviour (placeholder square / `enemyArt`).
3. `EnemyView` needs a **settable animator** (today `animator` is a private `[SerializeField]` with no runtime setter). Add `public Animator Animator { set; }` (or a `Wire(Animator)` method) — 3 lines, no behaviour change.
4. Reaction mapping vs the shipped controller: `Creature1 1.controller` has **no `Win/Patience/Bite/Eaten` parameters and defaults to Attack**. Two options for the design phase:
   - **(A, cheap)** Have `EnemyView` guard on parameter existence (`animator.HasParameter(id)`) or `try`-style: if the creature controller lacks the trigger, fall back to `animator.Play("Creature 1 Attack")` for `Bite`/`Eaten` and stay in Idle otherwise. Keeps the phrase bubble working regardless of the artist's controller. **Note the controller's default state is Attack — likely an authoring bug to flag.**
   - **(B, cleaner, more work)** Ask the artist/task to author a reaction-capable controller per creature (Idle default + Win/Patience/Bite/Eaten one-shots), mirroring `Enemy.controller`. Then `EnemyView` needs no change beyond the animator setter.

Because the jam rule is "asset-over-custom / 10x simpler", **A + flagging the default-state bug** is the pragmatic first slice; B is the right long-term shape once the second creature exist.

Also decide transform handling: the prefab bakes position `(6.68,1.43)` / scale `0.9`, while the scene anchor is `(5.43,1.06)` / scale `4.7`. Recommendation: instantiate as a child **with local transform reset to identity** and set an explicit per-level placement, OR accept the prefab's own transform by not reparenting — this must be an explicit decision (see Open questions).

---

## Background integration + front-disable plan

Reality: the background system already exists as **two serialized layer renderers** on `GameLoopController` (`backdropRenderer`, `midgroundRenderer`) fed by `LevelConfig.background` / `LevelConfig.midground`. There is **no front layer**.

Simplest integration + forward-compatible front:

1. **Assign `BG.png`'s sprite to `LevelConfig_Level1Mexico.background`** (and Colombia/tutorial when their art exists). `ApplyLevelArt()` already swaps it in and sets `Color.white` — no code change needed. Same mechanism for `midground` when the artist provides a separate mid layer.
2. Keep using `Backdrop` (order −100) for the back layer. `BG_0.prefab` itself is not needed if we assign the sprite directly to the existing `Backdrop` renderer — **prefer the existing layer + sprite slot over instantiating `BG_0.prefab`** (fewer moving parts; `ApplyLayer` handles the placeholder fallback and null-guard).
3. **Front/foreground**: introduce a reserved **`Foreground` GameObject** in `Prototype.unity` (and `Tutorial.unity`) with a `SpriteRenderer` at `m_SortingOrder: 5` (in front of `Player`=1), plus an optional `LevelConfig.foreground` sprite slot and a `GameLoopController.foregroundRenderer` field — but **leave the GameObject `m_IsActive: 0` (disabled)** and the slot empty until art exists. "Disable the front" then means concretely: *the front layer exists in the hierarchy but is inactive and renders nothing; the assign path is null-guarded and only enables it when `foreground != null`.*
   - 10x-simpler alternative: **do not add the front layer at all** this slice and simply document that it is a future `foreground` slot. Only add it if the user wants the slot ready. Recommend this unless the user explicitly wants the empty slot pre-created.

---

## Per-level configurability options (tradeoffs)

| # | Approach | Pros | Cons | Effort |
|---|----------|------|------|--------|
| 1 | **`enemyPrefab` (GameObject) on `LevelConfig`**, instantiate under the `Enemy` anchor, wire Animator into `EnemyView` | Matches "another prefab per level" exactly; one serialized ref; no new asset type; reuses `EnemyView` unchanged; scales to N levels | Needs the prefab to expose an Animator; needs reaction-mapping decision vs creature controller; needs transform-placement decision | **Low** |
| 2 | **Keep single scene enemy, swap only sprite via `enemyArt`** (already supported) | Zero new code; works today | Only supports a flat sprite, not the animated multi-part `Creature1` rig; cannot swap the whole prefab; contradicts the user's "another prefab" ask | Very Low |
| 3 | **Dedicated `EnemyConfig` SO** (`prefab`, `reactions`, `bubblePrefab`) referenced from `LevelConfig` | Bulks enemy-specific config in one place | New SO + new indirection for jam scale; overkill now; violates "no speculative abstraction" | Medium |
| 4 | **Scene-per-enemy (one gameplay scene per level)** | Each enemy placed by hand | Duplicates the whole game scene; breaks the "single Prototype scene serves both levels" design (`HandController.cs:224-226`); high maintenance | High |

**Recommended: Option 1** (with the reaction-mapping choice deferred to design). It is the only option that literally satisfies "whatever gets instantiated must be configurable / swappable per level" while staying inside the existing architecture.

---

## Open questions / assumptions

1. **"Front/foreground" meaning — confirm.** Assumption: a future art layer in front of the characters (sorting order > 1) that does not exist yet; "disable" = keep its GameObject inactive/empty. If instead the user means something inside `BG.png` or `BG_0.prefab`, the plan changes.
2. **Transform/scale contract for instantiated prefabs.** `Creature1.prefab` is at pos `(6.68,1.43)` scale `0.9`; the scene `Enemy` anchor is `(5.43,1.06)` scale `4.7`. Do we reset the child to identity and place it by the anchor, or honour the prefab's own transform? (Affects on-screen size/position visibly.)
3. **Reaction triggers for the new controller.** `Creature1 1.controller` has no params and defaults to `Attack`. Do we (A) make `EnemyView` tolerate missing triggers, or (B) require the artist to author `Win/Patience/Bite/Eaten`? Also: is the Attack default an authoring bug?
4. **Bubble for the prefab enemy.** Today the bubble lives on the scene `Enemy` object. Keep the scene bubble (reparent/anchor) or add a bubble per prefab? Recommendation: keep the single scene bubble, position it near the spawned prefab.
5. **`EnemyView.art` vs `enemyPrefab`.** When a prefab is present, `enemyArt` (sprite) is redundant. Rule: prefab wins; `enemyArt` remains the fallback for levels without a prefab (Llorona/tutorial until its art exists).
6. **Do we apply this to `Tutorial.unity` too?** La Llorona enemy is also a placeholder. Assumption: only `Prototype.unity` this slice; tutorial keeps the placeholder.
7. **`BG_0.prefab` vs `Backdrop` layer.** Assumption: use the existing `Backdrop` SpriteRenderer + `LevelConfig.background` sprite and do **not** instantiate `BG_0.prefab` (it is a single sprite renderer, already equivalent to the layer). Confirm the user is OK leaving `BG_0.prefab` unused or wants it used.

---

## Recommended approach (10x-simpler version)

Ship the smallest slice that gives level 1 a real, swappable enemy and a real background:

1. **Add `public GameObject enemyPrefab;` to `LevelConfig`.** (1 field.)
2. **Add `public Animator Animator { set; }` accessor to `EnemyView`.** (~3 lines.)
3. **Add `SpawnEnemy()` to `GameLoopController`**, called from `StartGame()`/`ApplyLevelArt()`: instantiate `level.enemyPrefab` under the existing `Enemy` anchor, `GetComponentInChildren<Animator>()`, push it into `EnemyView`, hide the placeholder renderer when a prefab exists. Null-guarded; falls back to today's placeholder when empty. (~20 lines.)
4. **Make `EnemyView.PlayReaction` tolerant of a missing trigger** (guard with `HasParameter`), so the creature controller's lack of `Win/Patience/Bite/Eaten` doesn't spam console warnings; keep the phrase bubble path untouched. Flag the controller's `Attack`-as-default as an art bug.
5. **Background**: assign the `BG.png` sprite to `LevelConfig_Level1Mexico.background` in the Inspector — **zero code** (`ApplyLevelArt` already does the work). Leave `midground` empty for now.
6. **Front**: **do not add** a front layer this slice (no art). Document `foreground` as a future optional `LevelConfig` slot + a disabled `Foreground` GameObject. If the user insists on a ready slot now, add the inactive `Foreground` GameObject + `foreground` field with a null-guard that only enables it when art is assigned.
7. **Verify** by Play Mode in `Prototype.unity` at level index 0: creature appears, reacts to a cook, bubble shows; background shows `BG.png`; no console errors. Screenshot per `unity-rapid-prototyping`.

This is a **wiring change, not a rules change**, so it should fit one focused <1h slice. Nothing here needs tests (pure presentation); validate with Play Mode + `read_console` + screenshots.

---

## Result contract

- **status**: complete
- **executive_summary**: The enemy gameplay RULES already exist (`GameLoopController` + `EnemyView`); what is missing is that the level enemy is a white-square scene placeholder and the real animated `Creature1.prefab` (El Ahuizotl) plus `BG_0`/`BG.png` background are completely unintegrated (added in the last two commits, referenced by no scene). The simplest fix is a forward-compatible presentation wiring slice: add `LevelConfig.enemyPrefab`, instantiate it under the existing scene `Enemy` anchor, plug its `Animator` into `EnemyView`, and assign the `BG.png` sprite to `LevelConfig.background` (already supported by `ApplyLevelArt`). A front/foreground layer does not exist anywhere and its art is missing, so it should stay disabled/absent this slice. No rules change, so it fits one <1h jam slice.
- **artifacts**: `openspec/changes/enemy-level-integration/explore.md`
- **next_recommended**: `sdd-propose` (define scope: enemy prefab instantiation + background sprite assignment; explicitly out-of-scope: front layer art, tutorial scene, second creature) — then `sdd-design` to lock the reaction-mapping (option A vs B) and transform contract.
- **risks**:
  - `Creature1 1.controller` has no `Win/Patience/Bite/Eaten` params and defaults to `Attack` → `EnemyView` triggers will warn/no-op unless handled (decision A vs B).
  - Prefab transform mismatch (`Creature1` at 6.68/0.9 vs `Enemy` anchor at 5.43/4.7) can place the creature off-screen or wrong-sized.
  - "Front" is ambiguous — misreading it could add unnecessary scene objects.
  - `EnemyView.animator` has no runtime setter today; small code change required.
- **open_questions**: (1) exact meaning of "front/foreground"; (2) transform/placement contract for instantiated prefabs; (3) reaction-mapping choice A vs B and whether the Attack default is a bug; (4) use existing `Backdrop` layer vs instantiate `BG_0.prefab`; (5) apply to tutorial scene or Prototype only.
- **skill_resolution**: `unity-rapid-prototyping` (project standard, gameplay-first/jam-scale gates) applied; `work-unit-commits` noted for the eventual implementation commit (enemy+background as one deliverable).
