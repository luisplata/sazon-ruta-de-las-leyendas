# Design: Legend Newspaper (Text-Only)

## Context

`Legend.unity` already renders the newspaper art (`news paper.png` → world `Backdrop`
`SpriteRenderer`, commit `bc62390`), but the copy still rides on the 1+1
`legendTitle`/`legendText` pair. That pair cannot express a 14-slot newspaper page. This change
adds a `LegendPage` SO (13 authored strings), the one generated slot (`demandsList`), and the
`LegendController` slot-fill/fallback ladder. **Text only** — the paper stays a world sprite; no
image/Canvas/sprite work (see `explore.md` §1, §3).

Load flow: `Menu/Map → Scenes.Legend` → `LegendController.Start()` reads
`GameSession.tutorialLevel` else `database.levels[currentLevelIndex]` → renders or auto-forwards.

## Goals / Non-Goals

**Goals**: 14 named slots bound to real TMP refs; `demandsList` always derived from
`level.recipes`; three authored pages (Llorona/Mexico/Colombia); zero regression for legacy
configs; the pure text logic unit-testable.
**Non-Goals**: any image/backdrop/sprite change; `TutorialLegend.unity` layout; a per-row plate
icon (`RecipeData.icon` is emoji → tofu in LiberationSans; `art` is inconsistent — `explore.md` §4);
deleting `legendTitle`/`legendText`.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|---|---|---|---|
| Page shape | New `LegendPage : ScriptableObject`, 13 `string` fields, no `demandsList` field | 13 fields on `LevelConfig`; a 13-string array | One reusable authoring unit per page; keeps `LevelConfig` about cooking rules; fields are named so a designer can't mis-slot them (array indexes would). |
| Where the page lives | New `LevelConfig.legendPage` reference | Move/rename legacy pair | Additive; the legacy pair is never removed. |
| Legacy pair | **Kept** as fallback (`legendTitle`/`legendText`) | Delete them | Deleting auto-forwards any config without a page (Colombia = real regression) and breaks `TutorialLegend` (out of scope). Fallback = zero-risk migration. |
| `demandsList` | **Generated** in `LegendController` from `level.recipes` | Author it in the SO | Authored copy drifts from recipes; generation is always correct and free. |
| Slot geometry | 13 new TMP nodes at the explore anchor boxes; `ContinuarButton` **doubles as the footer** | 14th TMP `Footer` node; keep button where it is | The button's current rect (x 760-1160, y 165-275 bottom-origin) **collides with `adviceBody`** (box y 5-370). Moving it into the footer band (y 10-100) both clears the paragraph and gives the footer a natural home (explore §2 recommendation). |
| Testability | Tiny new asmdef `Sazon.LegendPage` holding `LegendPage` + pure `LegendTextBuilder` | Put them in `Assembly-CSharp` | An asmdef **cannot reference `Assembly-CSharp`**, so `Assembly-CSharp` logic is unreachable from `EditModeTests`. `Sazon.Anim` (`Assets/Scripts/Anim/Sazon.Anim.asmdef`) is the existing precedent — follow it. `Assembly-CSharp` auto-references the new asmdef, so `LegendController`/`LevelConfig` use it one-way. |
| Empty/unknown state | No `SetActive` toggling anywhere | Hide legacy nodes in page mode | Empty TMP renders nothing; skipping toggles removes state and code. |

## Sequence Diagram — one Legend load

```
Actor        SceneManager        LegendController            LevelConfig   LegendPage   TMP slots
  |  LoadScene(Scenes.Legend)         |                          |            |            |
  |---------------->|  Start()        |                          |            |            |
  |                 |---------------->| CurrentLevel()           |            |            |
  |                 |                 |------------------------->|            |            |
  |                 |                 |<---- LevelConfig / null -|            |            |
  |                 |                 | mode = Select(page, legendText)       |            |
  |                 |                 |-- Forward -------------> LoadScene(nextScene); return
  |                 |                 |-- Page ----------------> FillSlots(page) ----------->|
  |                 |                 |                         demandsList = Build(recipes) ->|
  |                 |                 |                         footer -> ContinuarButton label
  |                 |                 |-- Legacy --------------> titleText/bodyText = legacy ->|
  |                 |                 | AudioSource.PlayClipAtPoint(narration) (null-safe)     |
  |                 |                 | fade.FadeIn()                                          |
  |  click CONTINUAR |<----------------| Continue() -> LoadScene(nextScene)                     |
```

`Select` ladder (pure, `LegendTextBuilder.Select(page, legacyText)`):
`page != null && !page.IsEmpty()` → **Page**; else `!string.IsNullOrEmpty(legacyText)` → **Legacy**;
else → **Forward** (preserves today's guard; `level == null` folds into Forward).

## Data Contracts

```csharp
// Assets/Scripts/LegendPage/LegendPage.cs  (asmdef Sazon.LegendPage)
public class LegendPage : ScriptableObject {
    public string masthead, dateline, headline, subhead, reportHeader, reportBody,
                  demandsHeader, adviceHeader, adviceBody,
                  sidebarBlock1, sidebarBlock2, sidebarBlock3, footer;
    public bool IsEmpty();   // true ⇔ every field null/whitespace
}

// Assets/Scripts/LegendPage/LegendTextBuilder.cs
public enum LegendMode { Page, Legacy, Forward }
public readonly struct LegendRecipeText { public readonly string DishName; public readonly string[] Ingredients;
    public static LegendRecipeText From(string dishName, IEnumerable<string> ingredients); } // drops null/blank
public static class LegendTextBuilder {
    public static LegendMode Select(LegendPage page, string legacyText);
    public static string BuildDemandsList(IReadOnlyList<LegendRecipeText> recipes);
}
```

**`demandsList` line format** — one row per `level.recipes` entry, rows joined with `\n`:

```
row  = "• " + dishName + (ingredients.Length > 0 ? ": " + string.Join(" + ", ingredients) : "")
```

Rules: `null` `RecipeData` skipped; `null`/blank ingredient names dropped by
`LegendRecipeText.From`; blank `dishName` → `(sin nombre)`; zero rows → `""`.
Mexico renders exactly the 6 rows listed in `explore.md` §4 (bullet + `Dish: A + B + C`, no emoji).

## TMP Slot Layout (`Legend.unity`)

Anchors are the explore's, copy-pasteable (bottom-left origin, 1920×1080 reference). 13 **new**
TMP nodes + the existing `ContinuarButton` label as slot 14.

| # | Node | AnchorMin (x,y) | AnchorMax (x,y) | SizeDelta | fontSize / autosize | Source | Conf. |
|---|---|---|---|---|---|---|---|
| 1 | Masthead | `0.0318, 0.7843` | `0.9677, 0.9565` | 0,0 | 40-64 / centre | `masthead` | HIGH |
| 2 | Dateline | `0.0318, 0.7287` | `0.9677, 0.7741` | 0,0 | 18-24 / centre | `dateline` | HIGH |
| 3 | Headline | `0.0318, 0.6065` | `0.3594, 0.7176` | 0,0 | 28-48 / centre | `headline` | MED-LOW* |
| 4 | Subhead | `0.0318, 0.5000` | `0.3594, 0.6019` | 0,0 | 20-30 / centre | `subhead` | MED-LOW* |
| 5 | ReportHeader | `0.0318, 0.1065` | `0.3594, 0.1556` | 0,0 | 20-26 / top | `reportHeader` | MED-LOW* |
| 6 | ReportBody | `0.0318, 0.0046` | `0.3594, 0.1019` | 0,0 | 16-22 / top | `reportBody` | MED-LOW* |
| 7 | DemandsHeader | `0.3693, 0.6204` | `0.7609, 0.6667` | 0,0 | 20-26 / centre | `demandsHeader` | HIGH (bar A) |
| 8 | DemandsList | `0.3693, 0.3981` | `0.7609, 0.6157` | 0,0 | 18-26 / top | **generated** | HIGH |
| 9 | AdviceHeader | `0.3693, 0.3472` | `0.7609, 0.3935` | 0,0 | 20-26 / centre | `adviceHeader` | HIGH (bar B) |
| 10 | AdviceBody | `0.3693, 0.0926`† | `0.7609, 0.3426` | 0,0 | 16-24 / top | `adviceBody` | HIGH (raised) |
| 11 | Sidebar1 | `0.7698, 0.5278` | `0.9677, 0.6676` | 0,0 | 12-20 / top-left | `sidebarBlock1` | HIGH |
| 12 | Sidebar2 | `0.7698, 0.2806` | `0.9677, 0.5185` | 0,0 | 12-20 / top-left | `sidebarBlock2` | HIGH |
| 13 | Sidebar3 | `0.7698, 0.0324` | `0.9677, 0.2713` | 0,0 | 12-20 / top-left | `sidebarBlock3` | HIGH |
| 14 | **ContinuarButton** (existing `&1030593017`, label `&548223585`) → re-anchored `0.3693, 0.0093` / `0.7609, 0.0926`, `sizeDelta 0,0` | — | — | 0,0 | 32-44 / centre | `footer` | LOW→redefined |

\* Free-placement slots (art draws no box) — expect visual correction in PR2.
† Raised from explore's `0.0046` so the paragraph clears the button bar.
Every slot: `enableAutoSizing = 1` with the min/max above, `overflowMode = Truncate`, TMP default
font (LiberationSans) — no emoji. Illustration (slot 7) is the **baked** Ahuizotl woodcut: no node.

## Initial Asset Copy (author into the three assets during apply)

```yaml
LegendPage_Level0Llorona:      # La Llorona / tutorial
  masthead: EL PREGONERO PUEBLERO
  dateline: "Año XIII - No. 3 - Santa María de los Maíces - Precio: 1 Real"
  headline: ¿QUIÉN CALMA A LA LLORONA?
  subhead: "La mujer del río no llora por sus hijos... ¡llora de hambre!"
  reportHeader: "REPORTE LOCAL: LLANTO EN EL RÍO"
  reportBody: "Vecinos aseguran que el llanto de esta noche es distinto: la Llorona busca un plato que le devuelva la calma."
  demandsHeader: EXIGENCIAS CULINARIAS DEL MONSTRUO
  adviceHeader: CONSEJO DE COCINA DEL CURANDERO
  adviceBody: "Dicen que con un solo plato bien preparado basta. Arrastra las cartas a la olla (zona azul) y pulsa PREPARAR: el tutorial te guía paso a paso."
  sidebarBlock1: "SE BUSCA: Aprendices de cocina sin miedo al llanto."
  sidebarBlock2: "EL HORÓSCOPO RÍO-PUEBLO: Piscis - Escuche el río, no lo cruce."
  sidebarBlock3: "OPORTUNIDAD: Lote de 3 cartas de maíz, poco uso. Preguntar por Don Pedro."
  footer: PÁGINA SIGUIENTE →

LegendPage_Level1Mexico:       # El Ahuizotl (approved)
  masthead: EL PREGONERO PUEBLERO
  dateline: "Año XIII - No. 4 - Santa María de los Maíces - Precio: 1 Real"
  headline: ¡LA CRIATURA DEL RÍO EXIGE TACOS!
  subhead: "El Ahuizotl depone las armas... ¡pero pide antojitos!"
  reportHeader: "REPORTE LOCAL: AVISTAMIENTO CONFIRMADO"
  reportBody: "Unos arrieros afirman haber visto al mítico perro del agua con un hambre insólita."
  demandsHeader: EXIGENCIAS CULINARIAS DEL MONSTRUO
  adviceHeader: CONSEJO DE COCINA DEL CURANDERO
  adviceBody: "Para calmar sus apetitos, se dice que mezclar tres ingredientes clave (representados por cartas de arriero, ¡juega tus cartas!) en una olla especial, y \"prepararla\" con paciencia y oración (o simplemente un buen \"Preparar\") podría salvarnos a todos. ¡Un buen plato calma al monstruo!"
  sidebarBlock1: "SE BUSCA: Cocineros valientes con experiencia en antojitos de maíz."
  sidebarBlock2: "EL HORÓSCOPO RÍO-PUEBLO: Piscis - Cuidado al cruzar el vado."
  sidebarBlock3: "OPORTUNIDAD: Lote de 3 cartas de ingrediente de maíz, poco uso. Preguntar por Don Pedro."
  footer: PÁGINA SIGUIENTE →

LegendPage_Level2Colombia:     # El Hombre Caimán (draft)
  masthead: EL PREGONERO PUEBLERO
  dateline: "Año XIII - No. 5 - Santa Cruz del Magdalena - Precio: 1 Real"
  headline: ¡EL HOMBRE CAIMÁN QUIERE SER HUMANO OTRA VEZ!
  subhead: "El monstruo del Magdalena dejará el río... ¡si le cocinan un plato bien criollo!"
  reportHeader: "REPORTE LOCAL: CAIMÁN BIEN EDUCADO"
  reportBody: "Pescadores del Magdalena juran que el caimán les habló con voz de hombre y les pidió un plato de la región."
  demandsHeader: EXIGENCIAS CULINARIAS DEL MONSTRUO
  adviceHeader: CONSEJO DE COCINA DEL CURANDERO
  adviceBody: "Para romper el hechizo, se dice que hay que mezclar tres ingredientes clave (¡cartas de arriero!) en la olla especial y \"prepararla\". Un buen plato devuelve la forma humana... ¡o al menos calma el hambre!"
  sidebarBlock1: "SE BUSCA: Cocineros con mano para el pescado y el coco."
  sidebarBlock2: "EL HORÓSCOPO RÍO-PUEBLO: Cáncer - No cruce el río a medianoche."
  sidebarBlock3: "OPORTUNIDAD: Lote de 3 cartas de pescado, poco uso. Preguntar por Don Pedro."
  footer: PÁGINA SIGUIENTE →
```

## File Changes

| File | Action | Description |
|---|---|---|
| `Assets/Scripts/LegendPage/Sazon.LegendPage.asmdef` | Create | Pure asmdef (engine refs only) so the logic is testable; mirrors `Sazon.Anim`. |
| `Assets/Scripts/LegendPage/LegendPage.cs` | Create | 13 string fields + `IsEmpty()`. (Proposal said `Assets/Scripts/Cards/`; relocated for the asmdef boundary.) |
| `Assets/Scripts/LegendPage/LegendTextBuilder.cs` | Create | `LegendMode`, `LegendRecipeText`, `Select`, `BuildDemandsList`. |
| `Assets/Scripts/Cards/LevelConfig.cs` | Modify | Add `public LegendPage legendPage;`; keep `legendTitle`/`legendText`. |
| `Assets/Scripts/Legend/LegendController.cs` | Modify | +14 `TMP_Text` refs; `Select` ladder; `RecipeData`→`LegendRecipeText` adapter; `FillSlots`. |
| `Assets/ScriptableObjects/Cards/LegendPage_Level0Llorona.asset` | Create | Llorona copy. |
| `Assets/ScriptableObjects/Cards/LegendPage_Level1Mexico.asset` | Create | Ahuizotl copy. |
| `Assets/ScriptableObjects/Cards/LegendPage_Level2Colombia.asset` | Create | El Hombre Caimán copy. |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level0Llorona.asset` | Modify | Add `legendPage:` ref; keep legacy keys. |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level1Mexico.asset` | Modify | Same. |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level2Colombia.asset` | Modify | Same. |
| `Assets/Tests/EditMode/EditModeTests.asmdef` | Modify | Add `Sazon.LegendPage` to `references`. |
| `Assets/Tests/EditMode/LegendPageTests.cs` | Create | Pure tests (see below). |
| `Assets/Scenes/Legend.unity` | Modify | 13 TMP nodes at the table above + move `ContinuarButton`. No sprite/backdrop change. |

Each new file also adds its `.meta`. `TutorialLegend.unity` is **not** touched.

## Edge Cases

| Case | Behaviour |
|---|---|
| `level == null` | `Select(null, null)` → Forward → `LoadScene(nextScene)`. |
| `legendPage == null` | Legacy branch (`legendTitle`/`legendText`), as today. |
| `legendPage != null` but `IsEmpty()` | Treated as no page → Legacy → else Forward (no blank newspaper). |
| Both empty | Forward (today's guard preserved). |
| Llorona, 1 recipe | `demandsList` = one row. Slot 8 is bottom-anchored between the bars; woodcut + sidebars fill the page, so it reads complete. No layout change. |
| `adviceBody` overflow (752×270 px) | Autosize min 16 + `Truncate`; screenshot-gate; trim copy only if clipped. |
| 6 demand rows in ~235 px | Autosize min 18 (≈39 px/row) — fits. |
| `RecipeData` with blank `dishName` / null ingredient | Row keeps the recipe, blank name → `(sin nombre)`, ingredient dropped. |
| Non-16:9 aspect | Paper is a **world** `SpriteRenderer`; slots are a Screen-Space-Overlay canvas (`match 0.5`) → drift off-16:9. Accepted for v1 (Canvas conversion is image work, out of scope); verify at 1920×1080. |
| Footer duplication | Footer text lands on the orange CTA; reads as a page-next button. Accepted (restyling the button Image is image work). |
| `TutorialLegend` | Untouched → legacy path renders the Llorona `legendText` as today. |

## Testing / Validation

**strict TDD OFF** (jam, standard mode — note `openspec/config.yaml` still says `strict_tdd: true`;
this change runs standard mode, tests written beside the code, not test-first).

| Layer | What | Approach |
|---|---|---|
| Unit (EditMode) | `Select` (3 branches + null page + empty page), `LegendPage.IsEmpty()`, `BuildDemandsList` (Mexico 6 rows exact string, Llorona 1 row, null ingredient dropped, empty list → `""`, blank dish → `(sin nombre)`) | `UnityMCP run_tests(mode="EditMode")`. `LegendPage`/`LegendRecipeText` are created in-test via `ScriptableObject.CreateInstance` — no scene, no asset load. |
| Integration (Play) | `Legend.unity`, Mexico level: every slot non-empty, `demandsList` == 6 rows, `read_console` clean | Play mode + `read_console`. |
| Layout (Play + screenshot) | Slot alignment vs the printed rules/bars/divider; no clipping in `adviceBody`/`demandsList` | `manage_camera(screenshot)` at 1920×1080; adjust anchors/autosize only if the screenshot shows clipping. |
| Regression (Play) | Legacy fallback (temporarily null `legendPage` on a LevelConfig → `Title`/`Body`). Auto-forward (null `legendPage` **and** `legendText` → lands on `Market`). | Play mode, same scene. |

The 4-line `Start()` loop (the `RecipeData`→`LegendRecipeText` adapter) is deliberately not
unit-tested — the formatting and resolution logic is; cost/benefit at jam scale.

## Migration / Rollout

No data migration. `legendPage` defaults `null` → the legacy branch, so a partially-wired rollout
is safe. Rollout is asset-additive: wiring `LevelConfig_*.asset → LegendPage_*.asset` can land
per-level.

## Chaining Forecast (400-line budget)

- **PR1 — model + logic + data + tests (no scene):** 3 new code files + asmdef + 3 assets + 3
  `LevelConfig` edits + `LegendPageTests.cs` + asmdef edit ≈ **~390 lines → at the budget edge.**
  If it overflows, split the tests + `LegendPageTests.cs` into PR1b.
- **PR2 — `Legend.unity` layout:** 13 TMP nodes + the `ContinuarButton` move ≈ **~1,700-1,900 YAML
  lines → way over 400 → `size:exception`.**
- **Verdict: chained (2 PRs)**, as the proposal forecast. PR3 (`TutorialLegend`) stays out of scope.

## Rollback

`git checkout -- Assets/Scripts/Cards/LevelConfig.cs
Assets/Scripts/Legend/LegendController.cs Assets/Tests/EditMode/EditModeTests.asmdef
Assets/Scenes/Legend.unity Assets/ScriptableObjects/Cards/LevelConfig_Level*.asset` then delete
`Assets/Scripts/LegendPage/` (+ `.meta`), `Assets/Tests/EditMode/LegendPageTests.cs` (+ `.meta`) and
the three `LegendPage_*.asset` (+ `.meta`). The legacy pair is never removed, so the prior
Title/Body rendering and the auto-forward guard return intact.

## Open Questions

- Footers/handles: slot 13 has no drawn strip in the art — if the CTA-as-footer reading looks odd in
  the screenshot, fall back to a plain TMP `Footer` node at `0.0318,0.0000 / 0.9677,0.0370` and
  leave `ContinuarButton` where it was only if `adviceBody`'s box is re-tightened (`explore.md` §2).
- Should `adviceBody`'s raise to `0.0926` be a permanent art-compatible value or trimmed further
  after the first screenshot? (verification-gated, not blocking)
