# Proposal: Legend Newspaper (Text-Only)

## Intent

`Legend.unity` already shows the `news paper.png` backdrop, but the copy still rides on the
1+1 `legendTitle`/`legendText` pair — too thin for a newspaper page. This change adds a
`LegendPage` ScriptableObject (13 text slots) and rewrites `LegendController` to fill each
slot, generating the demands list from `level.recipes`. **Text only.**

## Scope

### In Scope
- New `LegendPage` SO: 13 separate text fields (masthead, dateline, headline, subhead,
  reportHeader, reportBody, demandsHeader, adviceHeader, adviceBody, sidebarBlock1/2/3, footer).
- **Three `LegendPage` assets** with authored copy: `LegendPage_Level0Llorona` (La Llorona),
  `LegendPage_Level1Mexico` (El Ahuizotl) and `LegendPage_Level2Colombia` (El Hombre Caimán) —
  all three authored so the team can tune their texts.
- `LevelConfig.legendPage` wired on **all three** LevelConfigs; **`legendTitle`/`legendText` kept
  as legacy fallback** (used when `legendPage == null`).
- `LegendController` rewrite: read `level.legendPage`, fill every TMP slot, and **generate
  `demandsList` from `level.recipes`** (`dishName` + `CardData.displayName`s) so it never goes stale.
- `Legend.unity`: add TMP text objects at the explore's normalized anchor boxes (rough ok). TEXT ONLY.
- Verify: Play mode + screenshot of the Ahuizotl legend; console clean; demandsList == 6 recipes.

### Out of Scope
- **All image work** — no Backdrop→Canvas `Image` conversion, no `ahuizolt newpaper.png`
  placement (woodcut already baked), no sprite/art changes of any kind. We only have the one
  newspaper art, so the copy must fit its existing slots.
- **`TutorialLegend.unity` scene layout** — La Llorona's page asset IS authored, but that scene has
  no newspaper backdrop/layout yet (image work); rendering it there is a later change.
- Pending uncommitted HUD work; removing `legendTitle`/`legendText`.

## Capabilities

### New Capabilities
- `legend-page`: a per-level newspaper page model (13 text slots) and its slot rendering.

### Modified Capabilities
- `legend-page` rendering — `LegendController` fills every slot and derives `demandsList`
  from `level.recipes`.

## Approach

One scene, plain MonoBehaviour, direct refs (no DI/event bus). `LegendPage` = 13 strings.
`LegendController` keeps the auto-forward guard (`legendPage == null && legendText empty`) and
builds `demandsList` with a `StringBuilder` over `level.recipes` (`• {dishName}: {ing1} + …`).
Add ~10 TMP objects to `Legend.unity` at the measured anchors. Reuse `ContinuarButton` as the
footer. No illustration slot (baked).

### Alternatives Considered
- **13 fields on `LevelConfig` vs a separate SO**: a separate SO keeps the level config about
  cooking rules; page copy stays one reusable authoring unit and the model is testable in isolation.
- **Keep legacy `legendTitle`/`legendText`**: deleting them would auto-forward Colombia and
  La Llorona to Market (a regression). Fallback = zero-risk migration.
- **Generate `demandsList`**: authored copy drifts from `recipes`; generation is always correct.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Assets/Scripts/Cards/LegendPage.cs` | New | 13 string fields SO. |
| `Assets/Scripts/Cards/LevelConfig.cs` | Modified | Add `legendPage`; keep legacy pair. |
| `Assets/Scripts/Legend/LegendController.cs` | Modified | Slot fill + demand-list generator + fallback. |
| `Assets/ScriptableObjects/Cards/LegendPage_Level0Llorona.asset` | New | La Llorona copy. |
| `Assets/ScriptableObjects/Cards/LegendPage_Level1Mexico.asset` | New | Ahuizotl copy. |
| `Assets/ScriptableObjects/Cards/LegendPage_Level2Colombia.asset` | New | El Hombre Caimán copy. |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level0Llorona.asset` | Modified | Point at `legendPage`. |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level1Mexico.asset` | Modified | Point at `legendPage`. |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level2Colombia.asset` | Modified | Point at `legendPage`. |
| `Assets/Scenes/Legend.unity` | Modified | ~10 TMP slot objects. |

## Assumptions

1. All three levels get an authored page; the legacy pair survives as a safety net.
2. `news paper.png` backdrop stays world-space (no Canvas conversion).
3. Anchor boxes from explore are "good enough"; positions corrected visually.
4. `demandsList` text-only (no emoji/sprites — default TMP font lacks glyphs).

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Scene YAML churn (~10 TMP objects) | High | Unity MCP with the correct scene loaded; rough anchors, visual fix. |
| TMP overflow (long `adviceBody`, 6 demand rows) | Med | Per-slot autosize + small `fontSizeMax`; screenshot-verify. |
| Legacy fallback masks a wiring miss on Mexico | Low | Test asserts the Mexico page is non-null and slots fill. |
| World backdrop vs Canvas text drift on non-16:9 | Med | Accepted for v1 (positions corrected manually). |

## Rollback Plan

`git checkout -- Assets/Scripts/Cards/LevelConfig.cs Assets/Scripts/Legend/LegendController.cs
Assets/Scenes/Legend.unity` and delete `LegendPage.cs` + `LegendPage_Level1Mexico.asset`.
Legacy fields are never removed, so the prior Title/Body rendering returns intact.

## Dependencies

- Unity Editor live via MCP (`set_active_instance`, open `Legend.unity` first).
- `Assets/_sprites/news paper.png` already in scene (commit `bc62390`) — no asset work.

## Chaining Forecast (400-line budget)

- **PR1 — model + data (no scene):** `LegendPage.cs` + `LevelConfig.cs` + `LegendController.cs`
  + three `LegendPage_*.asset` files ≈ **300-450 lines → watch the budget; split assets out if over.**
- **PR2 — `Legend.unity` layout:** ~10 TMP objects ≈ **~1,300-1,800 YAML lines → over 400.**
- **Verdict: chained (2 PRs).** PR1 autonomous and testable before any scene edit; PR2 as a
  focused layout artifact (expect `size:exception` or split by column group). Explore's
  PR3 (`TutorialLegend`) is deferred out of scope.

## Success Criteria

- [ ] `LegendPage` SO + three assets (Llorona, Mexico, Colombia) exist with all slots filled.
- [ ] `LevelConfig.legendPage` wired on all three levels; legacy pair retained.
- [ ] `LegendController` fills every TMP slot and generates `demandsList` from `recipes`.
- [ ] Play mode shows the Ahuizotl legend; screenshot captured; console clean.
- [ ] `demandsList` lists all 6 Mexico recipes; Colombia/La Llorona still render via fallback.
