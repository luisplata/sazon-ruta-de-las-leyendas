# Archive Report — gameplay-hud-art

**Change**: `gameplay-hud-art`
**Project**: Sazón: Ruta de las Leyendas (Unity 6000.3.10f1, URP 17.3.0, 2D)
**Archived**: 2026-10-18
**Archived by**: sdd-archive
**Artifact store**: openspec (file-based). No `mem_save` attempted (openspec is the artifact store).

---

## Final State

| Field | Value |
|-------|-------|
| Verification verdict | **PASS WITH WARNINGS** (sdd-verify; **0 CRITICAL**) |
| Tasks | 20/20 complete (`[x]`) — PR1 13/13, PR2 7/7, 0 incomplete |
| Tests | 7/7 EditMode pass (regression guard; change touches zero C#) |
| Build / console | Clean — 0 project errors |
| Spec compliance | 11/11 scenarios verified — 10 COMPLIANT, 1 PARTIAL (fade proven by proxy), 0 FAILING |
| Tracked diff | `HUDArt.prefab` (~945 lines) + `Prototype.unity` +106/−3 + `Tutorial.unity` +115/−7 |
| Delivery | 2 chained PRs, `stacked-to-main`; **`size:exception` GRANTED for PR1** (machine-generated uGUI prefab reviewed as one structural unit) |

## What Shipped

Presentation-only HUD art layer — the `Escene UI.png` slices are placed as a static `HUDArt` layer in
both gameplay scenes, so the HUD reads as the mockup with **zero gameplay-rules or data change**.

- **New `Assets/Prefabs/HUDArt.prefab`** (guid `a8f04f699e5d36b48acba64163692aba`): root stretch RT
  `(0,0)-(1,1)`, 8 children / 11 `Image`s, slices `_0,_1,_2,_4,_5,_6,_7,_8,_12` only; every Image
  `m_Type:0`, `m_PreserveAspect:1`, native `sizeDelta`, `m_RaycastTarget:0`, layer 0, no `Button`.
  Hierarchy (back→front): `BookPanel _2`, `ExitCornerArc _12`, `LivesHats` (Hat1–3 `_0`),
  `PatienceBar` (Frame `_5` + Cap `_1`, cap **below** frame), `IngredientStone _6`, `ExitButton _4`,
  `BackButton _8`, `NewButton _7`.
- **`Assets/Scenes/Prototype.unity`** (+106/−3): `HUDArt` prefab instanced as Canvas child,
  `SetSiblingIndex(6)` (immediately before `FadeOverlay`); `RefillButton.Image` ← `Escene UI_10` +
  `preserveAspect`; `RefillText` `m_IsActive:0`. TopBar child count 5 / `sizeDelta.x −683.034` unchanged.
- **`Assets/Scenes/Tutorial.unity`** (+115/−7): same three edits (same GO/fileIDs). TopBar child
  count 6 / `sizeDelta.x −627.341` unchanged.
- **All TMP labels kept**; `VIDAS` / `PACIENCIA` / `MONEDAS` / refill values show live runtime data.
- **No `.cs`, `.asset` or sprite `.meta` changed; no script file modified.**

PlayMode evidence: 16:9 and 2560×1080 rendered both scenes with live labels, corner clusters pinned,
non-square slices undistorted, stone clears the strip, forced fade covers all art, console 0 errors.

## Verification Reference

Full verdict and evidence: `openspec/changes/archive/2026-10-18-gameplay-hud-art/verify-report.md`
(PASS WITH WARNINGS, dated 2026-10-18, 0 CRITICAL). Implementation detail: `apply-progress.md` in the
same folder. Baseline `main @ 48c78a6` (scene-lighting-integration).

## Spec Sync Summary

The main spec did not exist prior to this change — the delta was promoted to a canonical capability
spec fresh (ADDED requirements copied verbatim; requirement/scenario structure preserved). Following
the established spec-store shape, the delta's `## Purpose` block was **not** carried into the
canonical spec (matching the existing `level-enemy-integration` / `player-protagonist-integration` /
`level-background-art` specs, which are Requirements-only); the purpose lives in the change proposal.

| Domain | Action | Details |
|--------|--------|---------|
| `gameplay-hud-art` | **Created** | 6 requirements added (R1–R6), 11 scenarios. Source: delta `specs/gameplay-hud-art/spec.md`. |

No MODIFIED or REMOVED requirements — pure addition. **No destructive merge** (project
`rules.archive` guard not triggered; nothing removed).

## Warnings (non-blocking) — carried from verify

1. **W1 — `Tutorial.unity` diff also flushes unrelated `GameLoopController` fields.** Beyond the three
   documented HUD edits, the Tutorial diff (`+115/−7`) re-serializes `playerPrefab: {fileID: 0}`,
   `playerScale: 0.5`, `foregroundRenderer: {fileID: 0}` to their **C# defaults**, plus a TMP
   `objective:` line-wrap. `Prototype.unity` already serialized these at HEAD; `Tutorial.unity` did not,
   so opening/saving flushed them. Behaviour is unchanged (`if (playerPrefab == null || playerView ==
   null) return;`). **Acknowledge at review/commit time** — the Tutorial diff exceeds "same three edits".
2. **W2 — Tutorial intro dialogue bubble transiently over the `PatienceBar` cluster.** For the first
   seconds of Tutorial Play, the Llorona intro speech bubble (world-space, **not** a Canvas child) sits
   in the new top-right patience zone, reducing legibility while shown. Transient; the Screen-Space-
   Overlay HUD renders above it, no HUD element is occluded, and it is not a `HUDArt` element — **not a
   spec violation**. Cosmetic only.

## Accepted Deviations (from verify)

- **Refill glyph = only the `_10` half** of the two-arrow loop (`_9`/`_10` are halves; `_10` is the
  larger with the arc + arrowhead). Spec allows `_9`/`_10`; composite is a known follow-up.
- **Fade coverage verified by forcing `FadeOverlay` on**, not a real round-end. Visual cover proven
  (full black); the `FadeController` round-end *trigger* path is unexercised (mechanism unchanged).
- **Ultrawide checked at 2560×1080 (21:9) only.** Ratios beyond 21:9 (e.g. 32:9) untested; corner
  clusters should still pin via absolute inset margins, but the top-centre stone's clearance decreases.

## Files Shipped

| File | Action | Detail |
|------|--------|--------|
| `Assets/Prefabs/HUDArt.prefab` (+ `.meta`, guid `a8f04f699e5d36b48acba64163692aba`) | Created | ~945-line machine-generated uGUI prefab |
| `Assets/Scenes/Prototype.unity` | Modified | +106 / −3 |
| `Assets/Scenes/Tutorial.unity` | Modified | +115 / −7 (incl. W1 drift) |
| `Assets/_sprites/Escene UI.png.meta` | Not changed | slices referenced by internalID |
| `Assets/Scripts/**` | Not changed | no C# diff |

Screenshots (`Assets/Screenshots/proto_*`, `tutorial_*`, `verify_*`) are gitignored evidence.

## Rollback

```bash
git checkout -- Assets/Scenes/Prototype.unity Assets/Scenes/Tutorial.unity
rm Assets/Prefabs/HUDArt.prefab Assets/Prefabs/HUDArt.prefab.meta
```
Revert scenes **before** deleting the prefab (else the scenes hold missing references). No script,
asset, or data migration — rollback is total.

## Carry-Forward / Known Items (non-blocking)

1. **Fade trigger path untested** — a real win/lose round-end (FadeController enabling the overlay) was
   not exercised; the visual cover is proven and the overlay is confirmed last with an opaque image.
2. **Aspect ratios beyond 21:9** untested (see accepted deviations).
3. **Tutorial intro bubble** (W2) — transient cosmetic crowding; no action taken.
4. **Tutorial serialization drift** (W1) — benign, but signals the Tutorial scene was out of sync with
   `GameLoopController.cs`; if `playerPrefab` was ever expected set in Tutorial, that is a separate
   (pre-existing) gap.
5. **Patience-bar coloured fill** — slices not received; the bar is frame + cap only (numeric label
   remains the source of truth).
6. **`_3` (brushstroke) / `_11` (noise)** — intentionally unplaced (`_3` reserved for a future banner).
7. **Legend / newspaper art** — deferred to the separate `legend-newspaper-art` change.

## Archive Contents

- proposal.md ✅
- explore.md ✅
- specs/ (gameplay-hud-art) ✅
- design.md ✅
- tasks.md ✅ (20/20 complete)
- apply-progress.md ✅
- verify-report.md ✅
- archive-report.md ✅ (this file)

## Source of Truth Updated

- `openspec/specs/gameplay-hud-art/spec.md` — created (6 requirements, 11 scenarios).

## SDD Cycle Complete

The change was proposed, explored, specified, designed, tasked, implemented, verified, and archived.
Ready for the next change.
