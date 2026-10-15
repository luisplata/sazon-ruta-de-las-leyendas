# Explore — abue-protagonist

**Change**: `abue-protagonist`
**Project**: Sazón: Ruta de las Leyendas (Unity 6000.3.10f1, URP 17.3.0, 2D)
**Date**: 2026-10-15
**Scope**: Investigation only — no gameplay code written.
**User clarification**: **Abue is the PROTAGONIST (the player character / the chef), NOT the enemy.**

---

## Overview

A fresh pull added a complete, self-contained Abue package:

- `Assets/Prefabs/Abue.prefab` (guid `166c869ceebd7b8479bf60ba085b6b6b`)
- `Assets/Animations/Abue.controller` (guid `7103c2eba457ee948a59450174742f5c`) with clips `Abue__Idle`, `Abue__Attack`, `Abue__GetHit` (`Assets/Animations/Abue__*.anim`)
- `Assets/_sprites/Abue.png` (guid `1cee9113ef29cb84d9e805078965f0af`, `spriteMode: 2` = sliced sheet)
- `Assets/Scenes/anims.unity` (authoring/preview scene) and `Assets/mk/` (split background art + `timeline.controller` + `bg1.anim`)

**Key discovery (mirrors the enemy change exactly): the Abue package is completely unintegrated.** No scene references `Abue.prefab`'s guid. The protagonist today is a hand-placed scene object whose `PlayerView` drives an old 9-clip `Player.controller`, and it renders as a tinted blue square. This change is the "wire Abue up as the protagonist" step — structurally identical to `enemy-level-integration`, which wired `Creature1.prefab` under the scene `Enemy` anchor.

**Second key discovery: `Abue.controller` is NOT properly parameterized.** It has **zero animator parameters and zero transitions** (`Abue.controller:63`, `13/39/87`) — the same "art-bug" class as the enemy's `Creature1 1.controller` (`Creature1 1.controller:63`). Its default state is `Abue__Idle` (`Abue.controller:130`), so it idles forever; `Abue__Attack`/`Abue__GetHit` cannot be reached by `SetTrigger`. This is the central decision this change must resolve.

This is a **presentation/wiring change, not a rules change** (the same conclusion reached for the enemy), so it fits one focused <1h jam slice.

---

## Current state (file:line evidence)

### How the protagonist is represented today

- **Hand-placed scene object, not instantiated.** In `Prototype.unity`:
  - Root GameObject `Player` (`Prototype.unity:1828-1844`, `m_Name: Player` at `1839`).
  - `PlayerView` component (`Prototype.unity:1845-1860`, `Assembly-CSharp::PlayerView` at `1856`); refs `animator: 1688562719`, `sprite: 1688562717`, `art: 0` (`1857-1859`).
  - Root transform at `(-5.64, 0.45)`, **scale `4.7`** (`1870-1871`). Child `Visual` (`1688562716`, `2279`) holds a square `SpriteRenderer` (white-square guid `b8d3af21e3cfb984c8d20407684086ba`) tinted **blue `(0.45,0.65,0.95)`** (`2334-2335`) + an `Animator` using **`Player.controller`** (guid `ab7297c3091ada848b3fc421b991fe17`, `2369`; meta confirmed).
- `PlayerView` is referenced **twice** by scene components:
  - `GameLoopController.playerView` (`Prototype.unity:2110`)
  - `HandController.playerView` (`Prototype.unity:2783`)
  Both hold the **same** `PlayerView` instance (`fileID 1349994338`).
- `Tutorial.unity` has the identical structure (`PlayerView` at `1835`, `playerView` refs at `2089`/`2968`).

### What `PlayerView` does (`Assets/Scripts/Characters/PlayerView.cs`)

- Declares **9 trigger names** — `GrabCard, ParkCook, ParkTrash, Cook, Discard, LosePatience, LoseLife, Win, GameOver` (`PlayerView.cs:13-21`) — matching the **old `Player.controller`** (9 params, confirmed `Player.controller:372-412+`, clips `Player_*.anim`).
- Fields: `animator` (private `[SerializeField]`, `24`), `sprite` (`27`), `art` (`30`), `tint` blue (`36`).
- Public API: `Art` setter (`32-33`), 9 `OnX()` methods (`52-60`), `ResetToIdle()` (`63-66`).
- **`Fire()` calls `SetTrigger` unconditionally** (`68-71`).
- **`ResetToIdle()` calls `animator.Play("Idle", ...)`** (`65`).

### What the enemy template provides (`Assets/Scripts/Characters/EnemyView.cs`)

- `Animator` public setter — pushed at runtime by the spawner (`EnemyView.cs:41-45`).
- `HidePlaceholder()` (`EnemyView.cs:118-121`).
- **Trigger guard**: `TriggerReaction` only calls `SetTrigger` when the animator exposes the parameter, via pure `AnimParams.HasParam` (`EnemyView.cs:105-111`; helper `Assets/Scripts/Anim/AnimParams.cs:16-27`). This exists precisely so a 0-param controller (`Creature1 1.controller`) does not spam the console.
- `ResetToIdle` guards with `animator.HasState(0, StringToHash("Idle"))` (`EnemyView.cs:146`).

### The enemy spawn template (`Assets/Scripts/GameLoop/GameLoopController.cs`)

- `enemyView` field (`49`); `ApplyLevelArt()` (`116-126`) pushes art + calls `SpawnEnemy(level)` (`121`).
- `SpawnEnemy(LevelConfig level)` (`138-149`): null-guard → `Destroy(_enemyInstance)` → `Instantiate(level.enemyPrefab, enemyView.transform, instantiateInWorldSpace: true)` → `localPosition = Vector3.zero` → `GetComponentInChildren<Animator>()` → `enemyView.Animator = spawnedAnimator` → `enemyView.HidePlaceholder()`.
- `LevelConfig.enemyPrefab` is a `GameObject` (`LevelConfig.cs:48`); assigned in `LevelConfig_Level1Mexico.asset:39` (guid `4e4e5cf5e3f156e4bae9f38a3fdb9c8d`).
- Player reactions are already wired in the outcome switch: `OnWin` (`222`), `OnLosePatience` (`234`), `OnLoseLife` (`244`), `OnGameOver` (`286`).
- Both Player and Enemy scene roots share **scale 4.7** (`Prototype.unity:1871` and `2053`), so the same `instantiateInWorldSpace:true` + `localPosition=0` contract places a prefab at ~world scale 1 under either anchor. Abue's body sprite (`Abue.prefab:513`, size `3.14×3.15`) ≈ the placeholder square (`0.64 × 4.7 ≈ 3`), so on-screen size is comparable.

### The Abue assets

- `Assets/Prefabs/Abue.prefab`: root `Abue` (`5689518329549078047`) with an `Animator` (`6974800928773311037`) whose controller is **Abue.controller** (`Abue.prefab:410`, guid `7103c2eba457ee948a59450174742f5c`). Child `body` (`5841142591167903389`) with children `head`, `Abue_0`, `Abue_1`, `Abue_2`, `Abue_3`, each a `SpriteRenderer`. **No `PlayerView`/`EnemyView` MonoBehaviour — just Animator + SpriteRenderers.** Root `localPosition` is `(0.048, -0.22)` (`Abue.prefab:393`), not zero.
- `Assets/Animations/Abue.controller`: **`m_AnimatorParameters: []`** (`63`); states `Abue__Idle` (default, `130`), `Abue__Attack`, `Abue__GetHit`, **no transitions / no AnyState** (`13`, `39`, `87`, `122`).
- `Assets/Scenes/anims.unity`: contains an **inactive** Abue prefab instance (`m_IsActive: 0`, `~1333`) plus the split background sprites (`Escene 1_0..9`) and a `timeline` GameObject (`timeline.controller`, guid `d44c3a4cf01dcf34b855a20a8d38a5fd`). **No `PlayerView`, no `GameLoopController`** — it is an authoring/preview scene.

---

## Protagonist mechanism (proposed, simplest)

Mirror the enemy wiring exactly: instantiate a configured `Abue.prefab` under the existing scene **Player anchor** (`playerView.transform`) at `StartGame`, plug its `Animator` into the unchanged `PlayerView`, and hide the blue placeholder. `HandController` keeps working untouched because it holds the same `PlayerView` object.

Minimal flow (structural clone of `SpawnEnemy`, `GameLoopController.cs:138-149`):

1. Add a serialized prefab reference for the player.
2. In `StartGame()` (or `ApplyLevelArt()`, `GameLoopController.cs:116`), call a small `SpawnPlayer()`:
   - `Destroy(_playerInstance)` if a previous one exists;
   - `_playerInstance = Instantiate(playerPrefab, playerView.transform, true);`
   - `_playerInstance.transform.localPosition = Vector3.zero;`
   - `var a = _playerInstance.GetComponentInChildren<Animator>(); if (a != null) playerView.Animator = a;`
   - `playerView.HidePlaceholder();`
3. `PlayerView` needs a **settable `Animator`** + `HidePlaceholder()` (both absent today).
4. `PlayerView.Fire()` must **guard with `AnimParams.HasParam`** (mirror `EnemyView.cs:105-111`) so the 0-param `Abue.controller` doesn't log warnings on all 9 events; `ResetToIdle` should guard with `HasState` (mirror `EnemyView.cs:146`) because Abue's state is `Abue__Idle`, not `Idle`.
5. Unassigned prefab → keep today's placeholder behavior (null-guarded fallback).

No new anchor object, no new scene, no rules change.

---

## Animation / event mapping

The player's gameplay events already exist and are already routed to `PlayerView`:

| Event (code) | Call site | Semantic | Proposed Abue clip |
|---|---|---|---|
| Idle (default) | controller default state | waiting | `Abue__Idle` (automatic) |
| Serve dish | `HandController.PrepareFood() → playerView?.OnCook()` (`HandController.cs:172`) | the chef performs the dish | `Abue__Attack` |
| Grab card | `CardView:166 → playerView?.OnGrabCard()` | drag a card | (Attack, optional) |
| Park cook/trash | `HandController.cs:283-284` | drop into pot/trash | (Attack, optional) |
| Discard | `HandController.Refill():140` | dump trash | (Attack, optional) |
| Bite / penalty | `GameLoopController.ServeFood ServeFood Fail → playerView?.OnLoseLife()` (`244`) | takes damage | `Abue__GetHit` |
| Benign retry | `Presented/Cursed/Filler → playerView?.OnLosePatience()` (`234`) | mild reaction | (GetHit, optional) |
| Win / Game over | `222` / `286` | round end | (none in Abue set) |

**Blocker**: `PlayerView` fires all of these as `SetTrigger("Cook")`, `SetTrigger("LoseLife")`, etc. `Abue.controller` has **no parameters and no transitions**, so **none of these can fire**. With the pure mirror, Abue will only ever show `Abue__Idle`. Two ways to make Attack/GetHit actually play (decision for design/propose):

- **(A) State-name driving (no controller edit).** Map `Cook → animator.Play("Abue__Attack")`, `LoseLife → animator.Play("Abue__GetHit")`, plus a short coroutine/`CrossFade` back to `Abue__Idle` (no transitions exist to auto-return). ~15 lines in `PlayerView`.
- **(B) Authoring fix (mirror `Enemy.controller`).** Add `Attack`/`GetHit` triggers + one-shot→Idle transitions to `Abue.controller` in the Unity editor, then remap `PlayerView.Fire` names. Cleaner long-term, but requires editor/YAML controller work and is more than a wiring change.

Given the jam rule (asset-over-custom, 10× simpler) and the enemy change's precedent (it chose "tolerate + flag" over "re-author the controller"), **the recommended first slice is the pure mirror (presentation-only) with the trigger guard**, and Attack/GetHit mapping is a follow-up decision.

---

## Config approach (mirror enemy)

Two options for where the player prefab reference lives:

| # | Approach | Pros | Cons | Effort |
|---|----------|------|------|--------|
| 1 | **`GameLoopController.playerPrefab`** (scene-level `GameObject` ref) | Player is the **same across all levels** → ONE assignment; no per-level drift; simplest; spawn logic still mirrors `SpawnEnemy` | Slightly diverges from `LevelConfig.enemyPrefab` (uniformity nit) | **Low** |
| 2 | **`LevelConfig.playerPrefab`** (mirror the enemy literally) | Perfectly consistent with the enemy pattern; one code shape | Player is not level-specific → must be assigned in **all 3** level configs or Abue vanishes on some levels; conceptually odd | Low |

**Answer to the "any reason to differ?" question: yes, one.** The enemy is per-level (`LevelConfig`), the protagonist is universal. A scene-level `GameLoopController.playerPrefab` is both simpler (one assignment, no drift) and semantically correct. Option 2 is acceptable if the team prefers strict uniformity; if chosen, the prefab MUST be assigned in `LevelConfig_Level0Llorona`, `_Level1Mexico`, and `_Level2Colombia`.

`Abue.controller` is already the prefab's controller (`Abue.prefab:410`), so no controller files/`LevelConfig` edits are needed for the pure mirror beyond the prefab reference itself.

---

## Open questions / assumptions

1. **Make Attack/GetHit fire or ship Idle-only?** `Abue.controller` has no params/transitions. Do we (A) drive by state name + auto-return in `PlayerView`, or (B) re-author the controller to add triggers+transitions, or (C) ship Idle-only (pure mirror) and flag? Recommendation: C for the core slice, A as the smallest follow-up that uses the pulled art.
2. **Which event drives `Attack`?** Assumption: the cook/serve action `OnCook()` (`HandController.cs:172`). `GrabCard`/`ParkCook`/`ParkTrash`/`Discard` are candidates too — pick ONE primary action to avoid trigger-spam.
3. **Which event drives `GetHit`?** Assumption: `OnLoseLife()` (the Bite/penalty, `GameLoopController.cs:244`); `OnLosePatience` (benign retry) is the alternative.
4. **Config home: `GameLoopController` vs `LevelConfig`?** See table above; recommend `GameLoopController` (universal protagonist).
5. **Transform contract.** Mirror `SpawnEnemy` exactly (`instantiateInWorldSpace:true` + `localPosition=Vector3.zero`). Note Abue's prefab root is authored at `(0.048,-0.22)` (`Abue.prefab:393`); snapping to zero shifts it ~0.22 world units — verify visually and adjust in-editor if it looks low.
6. **Apply to `Tutorial.unity` too?** Assumption: `Prototype.unity` only this slice; tutorial keeps its placeholder (matches the enemy decision).
7. **Art-override interaction.** `ApplyLevelArt` today sets `enemyView.Art = level.enemyArt`. There is no `playerArt`/`playerPrefab`. With the prefab present, the placeholder/`Art` path is bypassed by `HidePlaceholder()`.

---

## Recommended approach (10x-simpler)

Ship the smallest slice that makes Abue the visible protagonist, mirroring the shipped enemy wiring:

1. **Add `[SerializeField] GameObject playerPrefab;` to `GameLoopController`** (scene-level — player is universal, one assignment). *Alt: `LevelConfig.playerPrefab` + assign in all 3 configs.* (~1 field.)
2. **Add `PlayerView.Animator` setter + `PlayerView.HidePlaceholder()`** (mirror `EnemyView.cs:41-45`, `118-121`). (~6 lines.)
3. **Add `SpawnPlayer()` to `GameLoopController`**, called from `StartGame()`/`ApplyLevelArt()` — a structural clone of `SpawnEnemy` (`138-149`) under the existing `playerView.transform` anchor. (~12 lines.)
4. **Guard `PlayerView.Fire` with `AnimParams.HasParam`** and `ResetToIdle` with `HasState` (mirror `EnemyView.cs:105-111`, `146`) so the parameter-less `Abue.controller` does not spam the console. (~6 lines.)
5. **Assign `Assets/Prefabs/Abue.prefab`** to the new field in `Prototype.unity`. **Zero gameplay-rule change.**
6. **(Follow-up, optional)** Make `Attack`/`GetHit` actually play: either state-name driving in `PlayerView` (option A) or a controller re-author (option B); map `OnCook → Attack`, `OnLoseLife → GetHit`. Out of this core slice.
7. **Verify** in Play Mode in `Prototype.unity`: Abue replaces the blue square, idles, sits at the player spot at the right scale; cook/penalty still resolve; no console errors. Screenshot + `read_console` per `unity-rapid-prototyping`.

**Out of scope**: `anims.unity` (preview scene), `Assets/mk/` split background + `timeline.controller` (separate background/timeline feature), `Tutorial.unity` wiring, and any rules/lives/patience changes.

Nothing here needs unit tests (pure presentation), consistent with the enemy change; validate via Play Mode + console + screenshots.

---

## Result contract

- **status**: success
- **executive_summary**: Abue is the protagonist and the new `Abue.prefab`/`Abue.controller`/`Abue.png` are completely unintegrated — no scene references the prefab. The protagonist today is a hand-placed `Player` scene object with a `PlayerView` driving the old 9-clip `Player.controller` and a blue placeholder square. The simplest fix mirrors the just-shipped enemy wiring: reference `Abue.prefab`, instantiate it under the existing `playerView.transform` anchor at StartGame, push its `Animator` into `PlayerView`, hide the placeholder, and guard the trigger calls. This is presentation-only, no rules change, <1h. The one real blocker: `Abue.controller` has ZERO parameters/transitions (`Abue.controller:63`), so Attack/GetHit cannot fire via `SetTrigger` — pure mirror yields Idle-only. That mapping is the key design decision.
- **artifacts**: `openspec/changes/abue-protagonist/explore.md`
- **next_recommended**: `sdd-propose` (scope: player-prefab wiring + PlayerView setter/guard; decide config home = GameLoopController vs LevelConfig; explicitly out-of-scope: animation mapping of Attack/GetHit, tutorial scene, mk background/timeline) → then `sdd-design` to lock the animation-driving approach (A state-name vs B controller re-author vs C Idle-only).
- **risks**:
  - `Abue.controller` has no parameters/transitions → `SetTrigger` no-ops/warns unless `PlayerView.Fire` is guarded (core slice includes the guard) and Attack/GetHit stay unused without a follow-up.
  - `PlayerView` has no runtime `Animator` setter / `HidePlaceholder` today (small code change required).
  - Transform: Abue's authored root offset `(0.048,-0.22)` + the mirror's `localPosition=0` may need an in-editor nudge.
  - If `LevelConfig.playerPrefab` is chosen (option 2), forgetting to assign it in all 3 level configs makes Abue vanish on some levels.
- **open_questions**: (1) Attack/GetHit strategy A/B/C; (2) which event drives Attack (`OnCook`?); (3) which event drives GetHit (`OnLoseLife`?); (4) config home GameLoopController vs LevelConfig; (5) apply to Tutorial.unity or Prototype only.
- **skill_resolution**: injected — 2 skills (`unity-rapid-prototyping`, `work-unit-commits`) from the orchestrator's Project Standards block.
