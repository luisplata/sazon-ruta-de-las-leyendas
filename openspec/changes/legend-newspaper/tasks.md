# Tasks: Legend Newspaper (Text-Only)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | PR1 ~390 (at the edge) · PR2 ~1,700–1,900 YAML · total ~2,100–2,300 |
| 400-line budget risk | PR1 **Medium** (at edge) · PR2 **High** (exception required) |
| Chained PRs recommended | Yes (2, stacked-to-main) |
| Suggested split | PR1 → model+logic+data+tests (no scene) · PR2 → `Legend.unity` layout |
| Delivery strategy | ask-on-risk |
| Chain strategy | stacked-to-main |
| Files touched | PR1 = 13 (3 new code, 1 asmdef, 3 assets, 3 asset edits, 2 test files) · PR2 = 1 scene |

```text
Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: High
```

PR1 sits at the 400-line edge: if the forecast exceeds, split tests + `LegendPageTests.cs` into
**PR1b**. PR2 (`Legend.unity`, 13 TMP nodes + `ContinuarButton` move) is **far over budget and
REQUIRES `size:exception` — PENDING MAINTAINER APPROVAL; do not assume it is granted.**

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Legend model + logic + data + EditMode tests | PR 1 (📍) | Autonomous; testable before any scene edit. |
| 2 | `Legend.unity` newspaper layout | PR 2 | Needs `size:exception`; depends on PR 1 merged. |

## Phase 1: PR1 — Model + Logic + Data + Tests

- [x] 1.1 Create `Assets/Scripts/LegendPage/Sazon.LegendPage.asmdef` mirroring `Sazon.Anim` (engine refs only, `autoReferenced: true`).
- [x] 1.2 Create `Assets/Scripts/LegendPage/LegendPage.cs` — `ScriptableObject` with the 13 named `string` fields + `IsEmpty()`.
- [x] 1.3 Create `Assets/Scripts/LegendPage/LegendTextBuilder.cs` — `LegendMode`, `LegendRecipeText.From`, `Select(page, legacyText)`, `BuildDemandsList` (row `• {dish}: {a} + {b}`, blank dish → `(sin nombre)`, drop null/blank ingredients).
- [x] 1.4 Modify `Assets/Scripts/Cards/LevelConfig.cs` — add `public LegendPage legendPage;`; keep `legendTitle`/`legendText`.
- [x] 1.5 Modify `Assets/Scripts/Legend/LegendController.cs` — add 14 `TMP_Text` refs; `Select` ladder (Page/Legacy/Forward); `RecipeData`→`LegendRecipeText` adapter; `FillSlots`; footer → `ContinuarButton` label; keep audio/fade null-safe.
- [x] 1.6 Create the 3 assets from `design.md` §Initial Asset Copy verbatim: `LegendPage_Level0Llorona`, `LegendPage_Level1Mexico`, `LegendPage_Level2Colombia`.
- [x] 1.7 Wire `legendPage` on the 3 `LevelConfig_Level*.asset` (keep legacy keys).
- [x] 1.8 Modify `Assets/Tests/EditMode/EditModeTests.asmdef` — add `Sazon.LegendPage` to `references`.
- [x] 1.9 Create `Assets/Tests/EditMode/LegendPageTests.cs` — `Select` 3 branches + null/empty page; `IsEmpty`; `BuildDemandsList` (Mexico 6 rows exact, Llorona 1, null ingredient dropped, empty list `""`, blank dish `(sin nombre)`).
- [x] 1.10 `refresh_unity`; `read_console` clean.
- [x] 1.11 [verify] `run_tests(mode="EditMode")` green. Commit work unit 1.

## Phase 2: PR2 — Legend.unity Layout (`size:exception` PENDING)

- [x] 2.1 [scene] `manage_scene(load, "Assets/Scenes/Legend.unity")`. Add 13 TMP nodes (slots 1–13) from `design.md` §TMP Slot Layout: exact anchors/sizeDelta, `enableAutoSizing=1` with min/max, `overflowMode=Truncate`, LiberationSans (no emoji).
- [x] 2.2 [scene] Re-anchor `ContinuarButton` (`&1030593017`, label `&548223585`) → footer band `0.3693,0.0093` / `0.7609,0.0926`, `sizeDelta 0,0`; raises out of `adviceBody`.
- [x] 2.3 [scene] Raise `adviceBody.anchorMin.y` to `0.0926`. Save; `refresh_unity`.
- [x] 2.4 [verify] Play Mexico: every slot non-empty, `demandsList` == 6 rows, `read_console` clean.
- [x] 2.5 [verify] Screenshot 1920×1080; correct free-placement slots (3,4,5,6,13) from the shot; adjust anchors/autosize only if clipped.
- [x] 2.6 [verify] Regression: null `legendPage` → legacy Title/Body; null page+`legendText` → auto-forward to Market.
- [x] 2.7 [verify] `git diff`: only `Assets/Scenes/Legend.unity`; no sprite/backdrop/image; `TutorialLegend.unity` untouched. Commit work unit 2.

## Review Workload Forecast

- Estimated changed lines: PR1 ~390 (edge) · PR2 ~1,700–1,900 · total ~2,100–2,300
- `Files touched`: PR1 = 13 · PR2 = 1
- `400-line budget risk`: PR1 **Medium** (edge) · PR2 **High**
- `Chained PRs recommended`: Yes (2, stacked-to-main)
- `size:exception`: **PENDING for PR2** (PR1 at the edge — split PR1b if over)
- `Decision needed before apply`: Yes (the PR2 exception)
- Dependency diagram: PR1 📍 (model+logic+data+tests) → PR2 (Legend.unity layout, needs `size:exception`)

```text
Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: High
```
