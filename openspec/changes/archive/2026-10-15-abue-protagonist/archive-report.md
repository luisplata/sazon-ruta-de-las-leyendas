# Archive Report — abue-protagonist

**Change**: `abue-protagonist`
**Project**: Sazón: Ruta de las Leyendas (Unity 6000.3.10f1, URP 17.3.0, 2D)
**Archived**: 2026-10-15
**Archived by**: sdd-archive
**Artifact store**: openspec (file-based). Engram DEGRADED — no `mem_save` attempted.

---

## Final State

| Field | Value |
|-------|-------|
| Verification verdict | **PASS WITH WARNINGS** (sdd-verify; 0 CRITICAL) |
| Tasks | 13/13 complete (`[x]`), 0 incomplete |
| Tests | 7/7 EditMode pass (independent run, job `9d98e081d7bf4bacaa01d9a1f10915cf`) |
| Build / console | Clean — 0 project errors / 0 project warnings (one unrelated Unity Services network warning) |
| Spec compliance | 7/9 scenarios COMPLIANT, 2/9 PARTIAL (no distinct-prefab swap; no `Win`-exposing animator), 0 FAILING, 0 UNTESTED |
| Tracked diff | ~75 changed lines (72 insertions / 3 deletions) across 3 authored files — under the 400-line budget |
| Delivery | Single PR, one work-unit (`PlayerView` hardening + `GameLoopController.SpawnPlayer` + scene wiring) |

## What Shipped

Pure presentation/wiring slice — the blue placeholder protagonist in `Prototype.unity` is replaced by
the animated `Abue.prefab`, data-driven from one scene-level slot, mirroring the shipped
`enemy-level-integration` slice. **Zero gameplay-rules change.**

- `GameLoopController` gained a serialized `GameObject playerPrefab` (universal protagonist slot),
  a `_playerInstance` field, and `SpawnPlayer()` — a structural clone of `SpawnEnemy()`: null-guards
  `playerPrefab`/`playerView`, destroys any prior instance, `Instantiate(playerPrefab,
  playerView.transform, instantiateInWorldSpace: true)`, `localPosition = Vector3.zero`, pushes the
  child `Animator` into `PlayerView`, then `HidePlaceholder()`. Called from `StartGame()` (not
  `ApplyLevelArt` — the protagonist is level-independent).
- `PlayerView` gained a settable `Animator { get; set; }`, a `HidePlaceholder()`, a `Fire()` guarded
  by the pure `AnimParams.HasParam` (all 9 events funnel through it), and `ResetToIdle()` gated on
  `HasState(0, StringToHash("Idle"))`.
- `Prototype.unity`: `GameLoopController.playerPrefab → Abue.prefab` (guid `166c869ceebd7b8479bf60ba085b6b6b`).

PlayMode evidence confirmed: `Abue(Clone)` spawns under the `Player` anchor at world scale 1 (3-arg
Instantiate contract), loops `Abue__Idle`, the placeholder `Visual` is disabled, the animator is wired
(`Abue`, 0 params), the duplicate guard leaves exactly one instance after repeated `StartGame()`, the
null-prefab fallback keeps the placeholder with no exception, and firing all 9 events + `ResetToIdle`
is a silent no-op. Console clean.

## Spec Sync Summary

The main spec did not exist prior to this change — the delta was promoted to a canonical spec fresh
(the ADDED requirements copied verbatim; requirement/scenario structure preserved).

| Domain | Action | Details |
|--------|--------|---------|
| `player-protagonist-integration` | **Created** | 7 requirements added (R1–R7), 9 scenarios. Source: delta `specs/player-protagonist-integration/spec.md`. |

No MODIFIED or REMOVED requirements — the capability is a pure addition. No destructive merge
(project `rules.archive` guard not triggered).

## Verification Reference

Full verdict and evidence: `openspec/changes/archive/2026-10-15-abue-protagonist/verify-report.md`
(PASS WITH WARNINGS, dated 2026-10-15). Implementation detail: `apply-progress.md` in the same folder.

## Carry-Forward / Known Items (non-blocking)

1. **Art follow-up — Attack/GetHit/Win do not animate (owner: artist or a follow-up code slice).**
   `Abue.controller` has **0 animator parameters / 0 transitions** and a default state of `Abue__Idle`,
   so `Abue__Attack` / `Abue__GetHit` are unreachable via `SetTrigger`. Decision A (Idle-only) ships
   with the guard, so this is a **silent no-op, no console spam** — Abue simply idles forever. Options
   to make the pulled art actually play: **(B)** state-name `animator.Play("Abue__Attack")` +
   `CrossFade` with an explicit timed return to `Abue__Idle` (no exit transitions exist → clip-length
   coupling + interrupt risk), or **(C)** the artist re-authors `Abue.controller` with triggers +
   one-shot→Idle transitions (cleanest, art-side). Which event drives Attack (`OnCook`?) / GetHit
   (`OnLoseLife`?) is still an open mapping question.
2. **W-02 / S-01 — no automated regression guard for the new code.** Every behavioral scenario is
   proven only by MCP-driven PlayMode introspection; there is no EditMode/PlayMode *test* that would
   catch a future regression in `SpawnPlayer` (duplicate guard / null fallback) or the
   `Fire`/`ResetToIdle` guards. Design-sanctioned ("no new pure logic", "pure presentation") — the only
   automated coverage is the pre-existing `AnimParamsTests`. **Suggest a PlayMode `[UnityTest]`** that
   asserts exactly one child under the anchor after repeated `StartGame()`, and that the placeholder
   stays enabled when `playerPrefab == null` (converts the MCP-only evidence into permanent coverage).
3. **W-01 — scope hygiene: unrelated untracked files.** `git status --porcelain` shows untracked
   `Assets/Animations/Enemigo1.meta` and `Assets/Animations/Enemigo1/` (`Creature1.controller`,
   `Enemigo1.anim`). They are referenced only by the unrelated `Assets/Scenes/anims.unity` and
   `Assets/Prefabs/Creature1.prefab`, pre-date this change, and **MUST NOT be swept into the
   abue-protagonist commit**. (The untracked `openspec/changes/abue-protagonist/` folder was expected —
   it is this change's own artifact folder, now archived.)
4. **Deferred — distinct-prefab swap scenario not exercised.** REQ-01 "Protagonist swappable without
   code changes" is PARTIAL: `Abue` flows through the slot, but a genuinely *different* prefab was not
   swapped in at runtime. Static evidence (`Instantiate(playerPrefab, …)`, no hardcoded protagonist)
   supports it; a live swap remains untested.
5. **S-02 (awareness only) —** design §5 discards the prefab's authored ~0.22 root offset via
   `localPosition = zero`. The live screenshot shows Abue correctly placed; no action needed.

## Archive Contents

- proposal.md ✅
- explore.md ✅
- specs/ (player-protagonist-integration) ✅
- design.md ✅
- tasks.md ✅ (13/13 complete)
- apply-progress.md ✅
- verify-report.md ✅
- archive-report.md ✅ (this file)

## Source of Truth Updated

- `openspec/specs/player-protagonist-integration/spec.md` — created (7 requirements, 9 scenarios).

## SDD Cycle Complete

The change was proposed, explored, specified, designed, tasked, implemented, verified, and archived.
Ready for the next change.
