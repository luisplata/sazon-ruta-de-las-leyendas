# Exploration — `legend-newspaper`

> Investigation only. **No scene, prefab, script, or asset was modified.** All state was read
> from the `.unity` / `.asset` / `.meta` YAML on disk (authoritative) plus pixel analysis of
> `Assets/_sprites/news paper.png` (1920×1080) with Pillow + NumPy. Reuses the prior survey in
> `openspec/changes/new-art-integration/explore.md` and goes deeper on the Legend flow.
> Unity 6000.3.10f1, URP 17.3.0.

## Overview / intent

Turn the **Legend** scene into the designed **NEWSPAPER page** using the already-imported
`news paper.png`. The approved shape:

- A **new ScriptableObject `LegendPage`** holds the newspaper copy as separate fields (13 slots).
- **`LevelConfig.legendPage`** replaces the current `legendTitle` + `legendText` (a 1+1 pair that
  cannot express a newspaper).
- **One `LegendPage` asset per level**: El Ahuizotl (Nivel 1 / México) and La Llorona (tutorial).
- `LegendController` reads `legendPage` and pushes each slot into a TMP rect laid over the art.

**Single biggest current-state finding:** the newspaper art is **already visible in `Legend.unity`**
— commit `bc62390` assigned `news paper_0` (guid `d9a0f9bd9e41a9a45ae394e23c51cef3`) to the
world-space `Backdrop` **SpriteRenderer** (`Legend.unity:781`). `TutorialLegend.unity` has **no
`Backdrop`** at all. So the work is *slotting text*, not *introducing the art*.

---

## 1. Current-state inventory (re-read; editor was changed by teammates)

### `Assets/Scenes/Legend.unity`

Canvas `&763798952` / RectTransform `&763798953` (`:398-502`): Screen Space – Overlay,
CanvasScaler `m_UiScaleMode: 1`, **`m_ReferenceResolution: {x: 1920, y: 1080}`**,
`m_MatchWidthOrHeight: 0.5` (`:457-479`). Children, in order:

| GO | fileID | Type | RectTransform | Notes |
|---|---|---|---|---|
| `Title` | `&1689384301` (TMP `&1689384303`) | TMP | stretch-top (`anchorMin 0,1 / max 1,1`), `sizeDelta {0,140}`, pivot top (`:961-979`) | `fontSize 72`, centre, `VerticalAlignment 1024` (top); text empty |
| `Body` | `&1591467180` (TMP `&1591467182`) | TMP | centre (`0.5,0.5`), `sizeDelta {1500, 665.17}`, `anchoredPosition y 67.585` (`:824-842`) | `fontSize 40`, `enableAutoSizing 1`, `fontSizeMin 10`, centre |
| `ContinuarButton` | `&1030593017` (Image `&1030593020`, Button `&1030593019`) | Button+Image | centre, `sizeDelta {400,110}`, `anchoredPosition y -320` (`:522-541`) | Image colour `(0.85,0.35,0.15)` orange, sprite-less; child `Label &548223585` TMP "CONTINUAR" (`fontSize 44`); `onClick → LegendController.Continue` |
| `FadeOverlay` | `&122139368` (Image `&122139369`) | Image | stretch (`0,0 / 1,1`) (`:178-196`) | black, `raycastTarget 1` |

`Legend` GO `&347008879`: `LegendController &347008881` (`:229-245`) → `titleText &1689384303`,
`bodyText &1591467182`, `fade &347008880`, `nextScene: Market`; + `FadeController &347008880`.
`Main Camera &2088059824`, `EventSystem &1271579383`.

**`Backdrop` `&1288732629`** (root, `:715-805`) — this is the key fact:
- `SpriteRenderer &1288732630`, **`m_Sprite: {fileID: 7268870845823818854, guid: d9a0f9bd9e41a9a45ae394e23c51cef3, type: 3}`** = `news paper_0` (`:781`).
- Transform `&1288732631`: position `(0,0,0)`, **`m_LocalScale 0.95581`** (constrained), `m_SortingOrder -100`, layer 0.
- Material = URP `Sprite-Lit-Default` (`guid a97c105638bdf8b4a8650670310a4cd3`) — lit, but the scene has **no Light2D**, so it renders flat.
- Sprite is 1920×1080 @ PPU 100 → 19.2×10.8 units; ×0.9558 → **18.35×10.32 units**. Camera ortho size 5 → 16:9 view = 17.78×10 units → the paper slightly over-fills the frame. **It is a WORLD object, not a Canvas Image.**

### `Assets/Scenes/TutorialLegend.unity`

Byte-identical to `Legend.unity` **except**: `LegendController &347008881` has
`nextScene: TutorialMarket` (`:245`); **there is NO `Backdrop` root** (`SceneRoots` = camera,
canvas, Legend, EventSystem only, `:1082-1088`). Same Canvas `&763798953`, same `Title`/`Body`/
`ContinuarButton`/`FadeOverlay` fileIDs. So the tutorial Legend currently shows **no newspaper**.

### Scripts

- `Assets/Scripts/Legend/LegendController.cs` (76 lines) — `Start()` reads `CurrentLevel()`;
  `if (level == null || string.IsNullOrEmpty(level.legendText)) LoadScene(nextScene)` (auto-forward,
  `:41-45`); else pushes `legendTitle → titleText`, `legendText → bodyText` (`:47-48`), plays
  `audioNarration` (null-safe), `fade.FadeIn()`. `CurrentLevel()` prefers `GameSession.tutorialLevel`
  over `database.levels[GameSession.currentLevelIndex]` (`:68-75`).
- `Assets/Scripts/Cards/LevelConfig.cs` — `legendTitle` (`:26`) + `[TextArea] legendText` (`:30`),
  plus `levelName, regionName, requiredIngredient, List<RecipeData> recipes, audioNarration,
  marketConfig, background, midground, enemyArt, enemyPrefab, foreground`.
- `Assets/Scripts/Cards/RecipeData.cs` — `dishName, string icon (emoji), Sprite art, RecipeKind kind,
  List<CardData> ingredients, reactionText`.
- `Assets/Scripts/Cards/CardData.cs` — `CardRole role, string displayName, Sprite art`.
- `Assets/Scripts/GameLoop/Scenes.cs` — `Legend`, `TutorialLegend`, `TutorialMarket`, … (all names centralized).
- `Assets/Scripts/Tutorial/TutorialController.cs` — exists, **does not reference legend fields** (grep clean).

### Data

- `RecipeDatabase.asset` `levels` = **[`LevelConfig_Level1Mexico` (guid `d0617c51…`), `LevelConfig_Level2Colombia` (guid `80eb5b2c…`)]**. So **El Ahuizotl = database index 0 = "Nivel 1"**; **`LevelConfig_Level0Llorona` (`c70fabdb…`) is NOT in the database** — it is loaded only via `GameSession.tutorialLevel`.
- `LevelConfig_Level1Mexico` — `legendTitle: El Ahuizotl` + long `legendText` (already a bulleted recipe list!); `recipes` = 6 guids; `background` = `BG.png`.
- `LevelConfig_Level0Llorona` — `legendTitle: La Llorona`; `recipes` = **1** (`2811a330…` = `RecipeData_TacosDeChapulines`).
- `LevelConfig_Level2Colombia` — `legendTitle: El Hombre Caimán` + long `legendText`; `recipes` = 6. **Also consumes `legendTitle`/`legendText`.**

---

## 2. Newspaper slot geometry (the high-value output)

Measured from `Assets/_sprites/news paper.png` (1920×1080) by dark-pixel run detection
(threshold luminance < 100). **Measured structural anchors:**

| anchor | image px (top-left origin) |
|---|---|
| dark outer frame | left cols `0-54`, right cols `1865-1919` (+ torn edge `1859-1863`), top rows `0-46` (no bottom border — paper runs to y=1080) |
| light paper area | x `61-1858`, y `47-1080` |
| rule 1 (thin) | rows `236-241`, x ≈ `138-1781` |
| rule 2 (thin) | rows `296-301`, x ≈ `141-1778` |
| black header bar A | rows `360-410` (h 51), x `709-1461` |
| black header bar B | rows `655-705` (h 51), x `709-1461` |
| column divider (vertical) | cols `1472-1477`, y `359-1079` |
| illustration box (borders) | x `229-601`, y `547-907` |
| sidebar rules | rows `513-517` (x 1490-1809), `779-784` (x 1495-1806), `1050-1055` (x 1509-1823) |

The art contains **no baked text** — only 2 rules, 2 black bars, 1 divider, 1 woodcut, 3 sidebar
rules. Because the sprite is 1:1 with the 1920×1080 Canvas reference, image px ≈ Canvas px.

### Per-slot anchor boxes

`anchorMin`/`anchorMax` are in Unity UI normalized space (**bottom-left origin**) — directly
copy-pasteable into a RectTransform. `px` is given top-left-origin for eyeballing.

| # | slot | px rect (L,T,R,B) | anchorMin (x,y) | anchorMax (x,y) | confidence |
|---|---|---|---|---|---|
| — | page (full paper) | 61,47,1858,1080 | `0.0318, 0.0000` | `0.9677, 0.9565` | HIGH |
| 1 | masthead | 61,47,1858,233 | `0.0318, 0.7843` | `0.9677, 0.9565` | HIGH (bounded below by rule 1) |
| 2 | dateline | 61,244,1858,293 | `0.0318, 0.7287` | `0.9677, 0.7741` | HIGH (bounded by rules 1 & 2) |
| 3 | headline (left col, above woodcut) | 61,305,690,425 | `0.0318, 0.6065` | `0.3594, 0.7176` | **MEDIUM-LOW** (free placement) |
| 4 | subhead (left col) | 61,430,690,540 | `0.0318, 0.5000` | `0.3594, 0.6019` | **MEDIUM-LOW** (free placement) |
| 5 | reportHeader (left col) | 61,912,690,965 | `0.0318, 0.1065` | `0.3594, 0.1556` | MEDIUM-LOW (free) |
| 6 | reportBody (left col) | 61,970,690,1075 | `0.0318, 0.0046` | `0.3594, 0.1019` | MEDIUM-LOW (free) |
| 7 | illustration | 229,547,601,907 | `0.1193, 0.1602` | `0.3130, 0.4935` | HIGH (box measured; **already baked**) |
| 8 | demandsHeader (center) | 709,360,1461,410 | `0.3693, 0.6204` | `0.7609, 0.6667` | HIGH (black bar A) |
| 9 | demandsList (center) | 709,415,1461,650 | `0.3693, 0.3981` | `0.7609, 0.6157` | HIGH (between bars) |
| 10 | adviceHeader (center) | 709,655,1461,705 | `0.3693, 0.3472` | `0.7609, 0.3935` | HIGH (black bar B) |
| 11 | adviceBody (center) | 709,710,1461,1075 | `0.3693, 0.0046` | `0.7609, 0.3426` | HIGH (below bar B) |
| 12a | sidebar block 1 (SE BUSCA) | 1478,359,1858,510 | `0.7698, 0.5278` | `0.9677, 0.6676` | HIGH (divider + rule) |
| 12b | sidebar block 2 (HORÓSCOPO) | 1478,520,1858,777 | `0.7698, 0.2806` | `0.9677, 0.5185` | HIGH |
| 12c | sidebar block 3 (OPORTUNIDAD) | 1478,787,1858,1045 | `0.7698, 0.0324` | `0.9677, 0.2713` | HIGH |
| 13 | footer | 61,1040,1858,1080 | `0.0318, 0.0000` | `0.9677, 0.0370` | **LOW** (no drawn anchor; see note) |

**Reading the layout:** masthead occupies the large band **above rule 1**; dateline sits **between
the two rules**; the whole content area below rule 2 is split by the vertical divider (`x≈1475`)
into a **main area** (left column + center column, no drawn divider between them — the left column
is defined by the woodcut/bar x-extents) and a **right sidebar** with 4 cells (3 usable + a tiny
bottom sliver).

**Ambiguity flagged (must be confirmed visually in apply):** the only *free* slots are
**3, 4, 5, 6, 13** — the art does not draw boxes for them. I placed headline/subhead in the left
column *above* the woodcut (y 305-540) and reportHeader/reportBody *below* it (y 912-1075), because
the center column is fully claimed by bars A/B (y 360 / 655). The alternative reading (headline
full-width in the 58-px band between rule 2 and bar A) is too thin to be intended. **Footer (13)**
has no drawn strip; the cheapest interpretation is that the existing **`ContinuarButton` *is* the
footer** — restyle it as bottom text ("PÁGINA SIGUIENTE →") rather than adding a new object.

---

## 3. Illustration-slot verdict (`ahuizolt newpaper.png` vs baked woodcut)

**Definitive: slot 7 is ALREADY satisfied by the backdrop for El Ahuizotl; do NOT place
`ahuizolt newpaper.png` as a separate `Image`.**

Evidence:
- `news paper.png` has the Ahuizotl woodcut **baked** into the left box: a bordered rectangle at
  x `229-601`, y `547-907` → **373×361 px** (measured).
- `ahuizolt newpaper.png` (guid `72cc892b55d22b54ab3f2dd9b73534f5`) is **374×362 px** (`:134-135`)
  — the *same drawing at exactly the box size*. Placing it as an overlay would **double-draw the
  identical woodcut** (visible edge/blur), not add anything.
- The art has only ONE illustration box; a *different* monster art is impossible without either a
  blank newspaper base or a cover patch — **neither exists**.

Consequence for **La Llorona** (`TutorialLegend` + `LevelConfig_Level0Llorona`): the shared paper
shows the **Ahuizotl** woodcut → narrative mismatch, and there is no Llorona drawing. Options:
(A) accept the mismatch (zero work); (B) cover the box (x 229-601, y 547-907) with a parchment-tinted
patch + text placeholder in `TutorialLegend`; (C) skip the paper entirely in `TutorialLegend`.
Recommend **(A) for v1** (low-stakes tutorial screen), escalate to (B) only if the mismatch reads badly.

---

## 4. `demandsList` generation approach

**Yes — `LegendController` can build the list from `level.recipes`.** Each `RecipeData` carries
`dishName` and `List<CardData> ingredients` (`CardData.displayName`). Resolved contents of
`LevelConfig_Level1Mexico.recipes` (guid → asset → dish → ingredients):

| RecipeData asset | dishName | ingredients (CardData.displayName) |
|---|---|---|
| `RecipeData_TacosDeChapulines` (`2811a330…`) | Tacos de Chapulines | Maíz + Insectos + Chile Piquín (3) |
| `RecipeData_EloteAsado` (`e22fd31e…`) | Elote Asado | Maíz (1) |
| `RecipeData_EloteConChile` (`44463d62…`) | Elote con Chile | Maíz + Chile Piquín (2) |
| `RecipeData_ElotePicante` (`61b65eaa…`) | Elote Picante | Maíz + Tomate + Chile Piquín (3) |
| `RecipeData_EsquitesClasicos` (`f10e0b77…`) | Esquites Clásicos | Maíz + Cebolla + Limón (3) |
| `RecipeData_EsquitesSencillos` (`065aa033…`) | Esquites Sencillos | Maíz + Cebolla (2) |

Proposed generated text (one row per recipe, emoji-free):
`• {dishName}: {ing1} + {ing2} + …` → e.g. `• Tacos de Chapulines: Maíz + Insectos + Chile Piquín`.
Six rows at ~0.2176 of screen height (≈235 px) → ~35-39 px/row → fits at ~26-30 px font.

**Per-row "plate icon" (mockup shows one):** *do not implement in v1.* `RecipeData.icon` is an
**emoji string** (`🌮`, `🌽`, `🌶`), and the Legend TMP uses the default TMP font
(`guid 8f586378b4e144a9851e7b34d9b748ee`, `LiberationSans`), which has **no emoji glyphs** →
renders as tofu. `RecipeData.art` is a Sprite but is **inconsistent** (present on
TacosAsado/Esquites*, **absent** on `EloteConChile` and `ElotePicante`). So a per-row sprite grid
would have holes. Keep the demands list **text-only**; revisit icons only if a real sprite set arrives.

---

## 5. La Llorona page (`TutorialLegend`)

- `LevelConfig_Level0Llorona.recipes` = **1** (`TacosDeChapulines`). The newspaper layout **works**
  with a 1-item list: `demandsList` simply renders one row (the slot is bottom-anchored enough to
  not look empty; the woodcut + sidebar still fill the page). No layout change required.
- `TutorialLegend` **does need its own copy** (Llorona lore ≠ Ahuizotl): it should point at its own
  `LevelPage` asset sourced from `LevelConfig_Level0Llorona`. Its `LegendController` already resolves
  `GameSession.tutorialLevel` first (`LegendController.cs:70`), so **the same controller code serves
  both** — only the `LevelConfig` asset differs.
- Open design call: does `TutorialLegend` show the *tutorial* advice variant? Simplest: reuse the
  same 13 slot fields, with a Llorona-specific `adviceBody` ("El tutorial te enseña a prepararlo
  paso a paso"). **Illustration remains the Ahuizotl** (see §3 verdict).

---

## 6. Migration — every `legendTitle` / `legendText` reference

Exhaustive grep over `Assets/` (14 hits, no others):

| file | lines | what breaks when the pair is replaced by `legendPage` |
|---|---|---|
| `Assets/Scripts/Cards/LevelConfig.cs` | `26` (`public string legendTitle;`), `30` (`[TextArea] public string legendText;`) | fields removed → replaced by `public LegendPage legendPage;` |
| `Assets/Scripts/Legend/LegendController.cs` | `6,7` (doc), `41` (`IsNullOrEmpty(level.legendText)`), `47` (`titleText.text = level.legendTitle`), `48` (`bodyText.text = level.legendText`) | rewrite: empty-check becomes `level.legendPage == null` (preserve auto-forward); fill code pushes each slot |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level1Mexico.asset` | `25,26` | drop both keys, add `legendPage: {fileID/guid}` |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level0Llorona.asset` | `20,21` | same |
| `Assets/ScriptableObjects/Cards/LevelConfig_Level2Colombia.asset` | `25,26` | **same — NOT in the brief.** Colombia (database index 1) currently shows a legend. If it gets no `LegendPage`, the Legend scene will **auto-forward to Market** = a regression |

No other consumer (`TutorialController`, `MapController`, `MenuController`, `Market*`, `DishView`)
references the pair. `Scenes.Legend` / `Scenes.TutorialLegend` loading is unaffected.

**Action required:** create a **third** `LegendPage` for `El Hombre Caimán` (Colombia), or
explicitly accept the Colombia regression. Recommend creating it (index 1 = the next real level after
El Ahuizotl) to keep the flow intact.

---

## 7. Proposed model (for the design phase — not built here)

`LegendPage : ScriptableObject` with 13 authored fields, e.g.:
`masthead, dateline, headline, subhead, reportHeader, reportBody, demandsHeader, adviceHeader,
adviceBody, sidebarBlock1, sidebarBlock2, sidebarBlock3, footer` (+ optional
`Sprite illustration` if a blank base ever appears; null in v1 = use the baked woodcut).
`demandsList` is the ONE generated field — built by `LegendController` from `level.recipes`.
`LevelConfig` drops `legendTitle`/`legendText`, adds `public LegendPage legendPage;`.

---

## 8. Risks

1. **World backdrop vs Canvas overlay drift (highest).** The paper is a **world-space
   `SpriteRenderer`** while the text is a **Screen-Space Canvas** with a
   `ScaleWithScreenSize` scaler (`match 0.5`). On any aspect ≠ 16:9 the two scale differently and the
   text slots **won't line up with the printed boxes**. **Fix (recommended):** convert the paper to a
   Canvas `Image` (stretch, `sprite = news paper_0`, `preserveAspect 0`) as the Canvas's first child
   (above `FadeOverlay`, below the text) and disable/delete the world `Backdrop`. Then artwork and
   slots share one scaler → the normalized boxes above hold at any aspect.
2. **TMP overflow on the long slots.** `adviceBody` is the longest free text (slot 11 ≈ 365 px tall,
   ≈ 752 px wide) and `demandsList` holds 6 generated rows. The current `Body` uses autosize
   (`min 10`, `max 72`); per-slot autosize + `overflowMode` and a smaller `fontSizeMax` will be
   needed. Verify visually (play mode + screenshot), not by YAML.
3. **Fixed 1920×1080 art vs non-16:9 screens.** The torn-paper edges are painted to the frame; if the
   Canvas `Image` stretches (not preserveAspect) the frame distorts; if preserveAspect it letterboxes.
   Pick stretch (matches reference res) and accept mild distortion on exotic aspects.
4. **Baked Ahuizotl on a shared base** → La Llorona/TutorialLegend mismatch (see §3).
5. **Scene churn / fileID remap.** Hand-editing `.unity` YAML for ~15 TMP objects is fragile; prefer
   Unity MCP with the correct scene loaded (`set_active_instance`, open target scene first).
6. **Two-scene duplication** (`Legend` + `TutorialLegend`) doubles churn and drift.
7. **Colombia regression** if it gets no `LegendPage` (§6).
8. **Concurrent change merge-order**: `scene-lighting-integration` touches other scenes; no overlap
   with Legend scenes, but `new-art-integration` also plans Legend work — coordinate who owns the
   scene edits.
9. **Legacy `LegendController` references in scenes.** `titleText`/`bodyText` serialized refs remain
   in both `.unity` files; removing/renaming the fields will null them (Unity keeps unknown YAML
   keys harmlessly, but the new slot fields must be wired in-scene).

---

## 9. 10x-simplest approach (jam scale) + chaining recommendation

**Approach (no architecture, no event bus):**
1. Add `LegendPage.cs` (13 string fields; `demandsList` NOT stored — generated).
2. `LevelConfig`: replace `legendTitle`/`legendText` with `legendPage`; add a `LegendPage` asset for
   **Mexico, Llorona, and Colombia**.
3. `LegendController`: keep the auto-forward guard (`legendPage == null`), push each slot via a
   small `[SerializeField]` set of `TMP_Text` refs, and build `demandsList` with a
   `StringBuilder` over `level.recipes`.
4. In each Legend scene: convert the paper to a Canvas `Image` (or add one), then place ~10-13 TMP
   rects at the anchor boxes above. **Reuse the existing `ContinuarButton` as the footer**; **skip a
   separate illustration `Image`** (baked).

**Review-budget forecast (400-line budget):**
- PR1 (code+data): `LegendPage.cs` + `LevelConfig.cs` + `LegendController.cs` + 3 assets ≈ **250-350
  lines → under budget.**
- A single Legend scene with ~13 TMP objects ≈ **~130 YAML lines per TMP object → ~1,600-2,000
  lines** for one scene; mirroring doubles it. **Well over 400 → chaining required.**

**Recommended chaining (deliverable work units):**
- **PR1 — model + data (no scenes):** `LegendPage` SO, `LevelConfig.legendPage`, `LegendController`
  rewrite + demand-list generator, 3 `LegendPage` assets (Llorona/Mexico/Colombia). Autonomous,
  **<400 lines**, verifiable by compile + one EditMode/PlayMode assertion on the generated text.
- **PR2 — newspaper panel, authored once as a `LegendPanel.prefab`** (paper Canvas `Image` + the 13
  TMP slots) and **instanced into `Legend.unity`**. One focused artifact; scene diff stays small.
  (Prefab creation itself is large → expect a `size:exception` or split by column group.)
- **PR3 — `TutorialLegend.unity`:** instance the same prefab + wire its `LegendController` to the
  Llorona `LegendPage`. **Tiny diff.**

Rationale: a shared prefab is one source of truth (kills the Legend/TutorialLegend drift risk and
halves the YAML), and PR1 (logic/data) is fully verifiable before any scene is touched.

---

## Evidence index

- **Scene `Legend.unity`:** Canvas `&763798952`/`&763798953` `:398-502`; `Title &1689384301` `:943-1079`;
  `Body &1591467180` `:806-942`; `ContinuarButton &1030593017` `:503-635`; `FadeOverlay &122139368`
  `:122-196`; `LegendController &347008881` `:229-245`; **`Backdrop &1288732629` `:715-805`
  (`m_Sprite` `:781` = `news paper_0`)**; `SceneRoots` `:1172-1180`.
- **Scene `TutorialLegend.unity`:** `LegendController &347008881` `:229-245` (`nextScene: TutorialMarket`);
  no `Backdrop` (`SceneRoots` `:1082-1088`).
- **Sprites:** `news paper.png.meta` (`guid d9a0f9bd9e41a9a45ae394e23c51cef3`; `news paper_0`
  `7268870845823818854`, rect 1920×1080, `:125-164`); `ahuizolt newpaper.png.meta`
  (`guid 72cc892b55d22b54ab3f2dd9b73534f5`; `ahuizolt newpaper_0`, 374×362, `:129-145`).
- **Geometry:** pixel-run analysis of `news paper.png` (rules rows 236/296; bars rows 360-410 &
  655-705 @ x 709-1461; divider cols 1472-1477; woodcut x 229-601 y 547-907; sidebar rules rows
  513/779/1050).
- **Scripts:** `LegendController.cs:38-52,68-75`; `LevelConfig.cs:25-30`; `RecipeData.cs:28-46`;
  `CardData.cs:20-35`; `Scenes.cs`; `RecipeResolver.cs:48-136`.
- **Data:** `LevelConfig_Level1Mexico.asset` `:15-40`; `LevelConfig_Level0Llorona.asset` `:15-29`;
  `LevelConfig_Level2Colombia.asset` `:15-37`; `RecipeDatabase.asset` (levels = Mexico `d0617c51…`,
  Colombia `80eb5b2c…`); 6 `RecipeData_*` assets under `Assets/ScriptableObjects/Cards/Recipes/`.
- **Migration grep:** `legendTitle|legendText` → 14 hits across `LevelConfig.cs`, `LegendController.cs`,
  and the three `LevelConfig_*.asset` files only.
- **Prior survey reused:** `openspec/changes/new-art-integration/explore.md` (Legend section, §B).
