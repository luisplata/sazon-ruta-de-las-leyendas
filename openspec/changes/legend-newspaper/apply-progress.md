# Apply Progress — `legend-newspaper`

**Status:** implementation complete (all 18 tasks verified). **Commits intentionally withheld** per
the apply instruction (`DO NOT COMMIT`) — the commit steps inside 1.11 / 2.7 are left to the
orchestrator.
**Mode:** Standard (`strict_tdd` OFF — tests written beside the code, not test-first).
**Delivery:** 2 chained PRs, `stacked-to-main`, plus the design's **PR1b** contingency split.
PR2 runs under the **maintainer-approved `size:exception`**.

## Task Status

### Phase 1 — PR1 / PR1b (model + logic + data + tests)

- [x] 1.1 `Assets/Scripts/LegendPage/Sazon.LegendPage.asmdef` (mirrors `Sazon.Anim`, engine refs
      only, `autoReferenced: true`)
- [x] 1.2 `LegendPage.cs` — 13 named `string` fields + `IsEmpty()`
- [x] 1.3 `LegendTextBuilder.cs` — `LegendMode`, `LegendRecipeText.From`, `Select`,
      `BuildDemandsList`
- [x] 1.4 `LevelConfig.legendPage` added; `legendTitle`/`legendText` **kept**
- [x] 1.5 `LegendController` — 14 slot refs, Page/Legacy/Forward ladder, `RecipeData` adapter,
      `FillPage`, legacy title/body preserved
- [x] 1.6 Three assets authored **verbatim** from `design.md` §Initial Asset Copy
- [x] 1.7 `legendPage` wired on all three `LevelConfig_Level*.asset` (legacy keys retained)
- [x] 1.8 `EditModeTests.asmdef` references `Sazon.LegendPage`
- [x] 1.9 `LegendPageTests.cs` — 15 tests
- [x] 1.10 `refresh_unity` + `read_console` clean
- [x] 1.11 `run_tests(EditMode)` **22/22 passed** (7 pre-existing + 15 new)

### Phase 2 — PR2 (`Legend.unity` layout, `size:exception` APPROVED)

- [x] 2.1 13 TMP nodes created at the design anchors/sizeDelta, autosize min/max, Truncate,
      LiberationSans, no emoji
- [x] 2.2 `ContinuarButton` re-anchored `0.3693,0.0093 → 0.7609,0.0926`, `sizeDelta 0,0`; label
      becomes the footer slot
- [x] 2.3 `adviceBody` authored at `anchorMin.y = 0.0926` (clears the footer band)
- [x] 2.4 Play (Mexico): every slot non-empty, `demandsList` = 6 rows, console clean
- [x] 2.5 1920×1080 screenshot inspected; free-placement slots accepted; **header colour corrected**
      (see Deviations)
- [x] 2.6 Regression: null `legendPage` → legacy Title/Body; null page + empty `legendText` →
      auto-forward to `Market`
- [x] 2.7 Diff review: only `Legend.unity` touched; no `m_Sprite`/backdrop change;
      `TutorialLegend.unity` untouched

## Files Created / Changed

| File | Action | Notes |
|---|---|---|
| `Assets/Scripts/LegendPage/Sazon.LegendPage.asmdef` | Created | New asmdef |
| `Assets/Scripts/LegendPage/LegendPage.cs` | Created | 13 fields + `IsEmpty()` |
| `Assets/Scripts/LegendPage/LegendTextBuilder.cs` | Created | Pure resolve/format rules |
| `Assets/Scripts/Cards/LevelConfig.cs` | Modified | `+ legendPage` (legacy pair kept) |
| `Assets/Scripts/Legend/LegendController.cs` | Modified | Slot fill + ladder + adapter |
| `Assets/ScriptableObjects/Cards/LegendPage_Level0Llorona.asset` | Created | Verbatim copy |
| `Assets/ScriptableObjects/Cards/LegendPage_Level1Mexico.asset` | Created | Verbatim copy |
| `Assets/ScriptableObjects/Cards/LegendPage_Level2Colombia.asset` | Created | Verbatim copy |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level0Llorona.asset` | Modified | `legendPage` ref |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level1Mexico.asset` | Modified | `legendPage` ref |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level2Colombia.asset` | Modified | `legendPage` ref |
| `Assets/Tests/EditMode/EditModeTests.asmdef` | Modified | `Sazon.LegendPage` reference |
| `Assets/Tests/EditMode/LegendPageTests.cs` | Created | 15 EditMode tests |
| `Assets/Scenes/Legend.unity` | Modified | 13 TMP nodes + button move + 14 refs |
| `openspec/changes/legend-newspaper/tasks.md` | Modified | All tasks `[x]` |

Each new file also has its `.meta`. `TutorialLegend.unity` untouched.

## PR Boundaries & Line Counts

| Unit | Contents | Lines |
|---|---|---|
| **PR1** | model + logic + 3 assets + 3 LevelConfig edits + asmdef (+ metas) | **~392** (at the 400 edge) |
| **PR1b** | `LegendPageTests.cs` (+meta) + `EditModeTests.asmdef` reference | **~169** |
| **PR2** | `Legend.unity` (`size:exception`) | **2,134** diff lines (1,971+/163−) |

**PR1 split justification (required by the design):** PR1 *including* the tests measured **~561**
changed lines, over the 400 budget. With the tests moved to **PR1b**, PR1 lands at **~392** — right at
the forecast's edge. The split follows `design.md` §Chaining Forecast exactly
("If it overflows, split the tests + `LegendPageTests.cs` into PR1b").

PR2's 2,134-line diff is higher than the 1,700–1,900 forecast because Unity re-serializes the scene
(object ordering churn); the semantic change is 13 TMP nodes + one button rect + 14 serialized refs.
`size:exception` is approved.

## Validation Evidence

**Compile / console**
- `refresh_unity` → `read_console`: **0 errors, 0 warnings**.
- One unrelated Unity Services message appeared once in Play ("Account API did not become
  accessible…"); no legend-scene error/warning was logged.

**EditMode tests** — `run_tests(mode="EditMode")` → **22 total, 22 passed, 0 failed, 0 skipped**.
Covers `IsEmpty` (3), `Select` (5), `LegendRecipeText.From` (2), `BuildDemandsList` (5) including the
exact 6-row Mexico string, single row, empty list, blank dish, no-ingredient row.

**Play mode (Legend.unity, Mexico, 1920×1080)**
- `screen=1920x1080`.
- All 13 new slots non-empty and equal to their `LegendPage` fields.
- `demandsList` = **6 rows** (in `recipes` order):
  `• Elote Asado: Maíz` … `• Tacos de Chapulines: Maíz + Insectos + Chile Piquín`.
- `Label` = `PÁGINA SIGUIENTE →`; button sits in the footer band and does **not** cover `adviceBody`.
- Screenshot: `Assets/Screenshots/legend-newspaper_mexico_1920x1080_v3.png` (gitignored dir).

**Regression**
- Mexico with `legendPage = null` → stays on `Legend`, `Title = "El Ahuizotl"`,
  `Body` = legacy `legendText` (540 chars), `Masthead` empty. ✔
- Mexico with `legendPage = null` **and** `legendText = ""` → auto-forwards; only `Market` remains
  loaded. ✔
- All three `LevelConfig` assets: `legendPage` non-null, `!IsEmpty()`, `Select → Page`, legacy pair
  retained, recipe counts 1 / 6 / 6. ✔

## Deviations from Design

1. **PR1 split into PR1 + PR1b** — PR1 with tests exceeded 400 lines; the design's own contingency.
   No behavioural change, only delivery grouping.
2. **Header slot colour.** `DemandsHeader` and `AdviceHeader` are rendered **white**; all other slots
   black. The design table specifies size/alignment but not colour; the screenshot proved black text
   is illegible on the **black** header bars. This is a text-only correction (no image work), found by
   task 2.5's screenshot gate.
3. **`FadeOverlay` moved to the last Canvas sibling.** The 13 new nodes were appended above the fade,
   so text popped in before the reveal. Restoring the overlay to the top keeps the existing
   fade-in behaviour (and matches the legacy `Title`/`Body` layering).
4. **Task 2.3 wording.** There was no pre-existing `adviceBody` node to "raise"; the new slot was
   authored directly at `anchorMin.y = 0.0926`, which satisfies the intent (clears the footer band).
5. **Unity serialized two extra null fields** (`enemyPrefab`, `foreground`) on
   `LevelConfig_Level0Llorona` / `_Level2Colombia` when re-saving. Harmless null defaults.
6. **Scene diff churn.** Unity reordered object blocks on save, inflating the diff (163 deletions are
   moved blocks, not removed content). Verified no `m_Sprite` line changed and `TutorialLegend`
   is untouched.
7. **TMP fallback-font side effect (reverted).** Rendering the verbatim `→` (U+2192) in the footer
   made TMP's dynamic fallback bake that one glyph into
   `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`
   (plus a TMP serialization upgrade). That file is **outside** the design's file table and is atlas
   data, so it was **reverted** (`git checkout` + refresh); the working tree is clean for it. Any
   editor Play session that renders `→` will re-bake it — see Risks.

## Notes / Risks

- Slots 3–6 and 13 are free placement (art draws no box). The screenshot shows them unclipped;
  `reportHeader` sits tight under the woodcut but does not overlap it.
- Non-16:9 aspect drift is the design-accepted v1 limitation (world sprite vs Screen-Space canvas).
- Pre-existing, **unrelated** working-tree changes (`Assets/Scenes/Prototype.unity`,
  `Assets/Scenes/Tutorial.unity`, `Assets/Prefabs/HUDArt.prefab`) were already dirty before this
  change and are **not** part of this work.
- Screenshots live in `Assets/Screenshots/`, which is gitignored, so they do not enter either PR.
- **TMP fallback atlas will re-dirty on Play.** The `→` in the design's verbatim footer is not in the
  primary LiberationSans SDF, so TMP adds glyph U+2192 to the dynamic fallback asset whenever the
  Legend page renders. It does not affect builds (runtime atlasing is in-memory), but an editor
  Play/screenshot before committing will show
  `Assets/TextMesh Pro/…/LiberationSans SDF - Fallback.asset` as modified. **Exclude it from PR2**
  (`git checkout --` it right before staging) or explicitly accept the one-glyph atlas update.
- **Verify-phase reminder:** re-running Play on `Legend.unity` will re-bake the fallback glyph; this is
  expected TMP behaviour, not a regression.

---

## Follow-up: default copy baked into `Legend.unity` (editor WYSIWYG)

**Request:** the newspaper's DEFAULT (Mexico / El Ahuizotl) copy must live in the scene so it is visible
and editable directly in the Unity editor, instead of only being injected at runtime from the
`LegendPage` SO.

**What was touched:** `Assets/Scenes/Legend.unity` — the `m_text` field of the 14 TMP slots only.
No fonts, sizes, anchors, colours, sprites, scripts, or other assets were changed. `TutorialLegend.unity`
is untouched (loaded once for validation, never saved).

Slots and values now serialized in the scene (from `LegendPage_Level1Mexico.asset`; `demandsList`
generated exactly as the runtime would, from `LevelConfig_Level1Mexico.recipes` via
`LegendTextBuilder.BuildDemandsList`):

| Slot node | Default text (abridged where long) | Colour |
|---|---|---|
| `Masthead` | `EL PREGONERO PUEBLERO` | black |
| `Dateline` | `Año XIII - No. 4 - Santa María de los Maíces - Precio: 1 Real` | black |
| `Headline` | `¡LA CRIATURA DEL RÍO EXIGE TACOS!` | black |
| `Subhead` | `El Ahuizotl depone las armas... ¡pero pide antojitos!` | black |
| `ReportHeader` | `REPORTE LOCAL: AVISTAMIENTO CONFIRMADO` | black |
| `ReportBody` | `Unos arrieros afirman haber visto al mítico perro del agua con un hambre insólita.` | black |
| `DemandsHeader` | `EXIGENCIAS CULINARIAS DEL MONSTRUO` | **white** |
| `DemandsList` | 6 rows, `• {dish}: {a} + {b}` (see below) | black |
| `AdviceHeader` | `CONSEJO DE COCINA DEL CURANDERO` | **white** |
| `AdviceBody` | `Para calmar sus apetitos, se dice que mezclar tres ingredientes clave … ¡Un buen plato calma al monstruo!` | black |
| `Sidebar1` | `SE BUSCA: Cocineros valientes con experiencia en antojitos de maíz.` | black |
| `Sidebar2` | `EL HORÓSCOPO RÍO-PUEBLO: Piscis - Cuidado al cruzar el vado.` | black |
| `Sidebar3` | `OPORTUNIDAD: Lote de 3 cartas de ingrediente de maíz, poco uso. Preguntar por Don Pedro.` | black |
| `Label` (footer slot on `ContinuarButton`) | `PÁGINA SIGUIENTE →` | white (as left by PR2) |

`DemandsList` — identical to the string the runtime builds for Mexico:

```
• Elote Asado: Maíz
• Elote con Chile: Maíz + Chile Piquín
• Esquites Sencillos: Maíz + Cebolla
• Esquites Clásicos: Maíz + Cebolla + Limón
• Elote Picante: Maíz + Tomate + Chile Piquín
• Tacos de Chapulines: Maíz + Insectos + Chile Piquín
```

**Caveat (REPORT ONLY — deliberately not "fixed"):** `LegendController.Start()` still calls `FillPage`
and overwrites every slot from the `LegendPage` SO at runtime. So these scene values are a **static
editor preview**: editing the text in the scene does NOT change the running game. For the running game
the edits must go to `Assets/ScriptableObjects/Cards/LegendPage_Level1Mexico.asset` (or the controller
must be changed to stop overwriting). Decision left to the user.

**Evidence**
- Applied with Unity MCP (`manage_components action=set_property`, component `TextMeshProUGUI`,
  property `m_text`) against live instance `GameJamUni2@9eee0fc1`, scene `Legend.unity` open; then
  `manage_scene action=save`.
- Read-back from the live editor: 14/14 slots non-empty and equal to the source values; `DemandsList`
  = 6 lines (len 239); colours confirmed unchanged (`DemandsHeader`/`AdviceHeader` = white (1,1,1),
  rest = black (0,0,0), `Label` white as before).
- On-disk YAML spot-checks in `Legend.unity` confirm the strings persisted (e.g. line 948
  `m_text: "P\xC1GINA SIGUIENTE \u2192"`, lines 1085–1088 the 6-row `DemandsList`).
- `refresh_unity` + `read_console`: no errors/warnings (only the pre-existing Unity Services message
  and two MCP-internal `TransformHandle` serializer warnings unrelated to the scene).
- `Legend.unity` reloaded, then `TutorialLegend.unity` loaded (no errors) and `Legend.unity` re-loaded:
  **0 errors**. `TutorialLegend.unity` remains unmodified.
- The TMP fallback atlas (`LiberationSans SDF - Fallback.asset`) was re-dirtied by the `→` glyph during
  the edit and was **reverted again** with `git checkout --`; the working tree is clean for it.
- Screenshot attempt: `Assets/Screenshots/legend-newspaper_scene-baked-text_v2.png` (gitignored). It
  shows the newspaper layout but **no text** — TMP meshes do not render through this edit-mode capture
  path (the prior PR2 1920×1080 proof was taken in Play mode). Not a regression; visual confirmation of
  the baked text in edit mode could not be obtained via the MCP screenshot tool.

**Files touched by this follow-up:** `Assets/Scenes/Legend.unity` only (`m_text` × 14).
