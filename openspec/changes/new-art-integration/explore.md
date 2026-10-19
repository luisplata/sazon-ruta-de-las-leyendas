# Exploration — `new-art-integration`

> Investigation only. **No scene, prefab, or script was modified.** All state was read from
> the `.unity`/`.meta` YAML on disk (authoritative) plus the Unity MCP live instance
> `GameJamUni2@9eee0fc1`. NOTE: the live editor's active scene was **`Menu.unity`** at
> investigation time (not `Prototype.unity` as the brief stated) — the YAML on disk was used
> as the source of truth. Unity 6000.3.10f1, URP 17.3.0.

## Overview

Four PNGs landed in `Assets/_sprites/` (commit `e1e013c`) and are referenced **nowhere** in
`Assets/Scenes`, `Assets/Prefabs`, or `Assets/ScriptableObjects` (verified by GUID grep).
The job is to place them as **presentation only** — the `GameLoopController` runtime rules
(lives / patience / coins / win-lose) must not change.

Three of the four are already accounted for: the cave/lake backdrop, the Sazón pot, and
`Creature talk.png` (the enemy phrase-bubble art) are **done** — `Creature talk.png`
(`guid ceaf2db3554f0aa4da7f367a985c62be`, `747×246`, `spriteMode: 2`) is already the sprite
on the gameplay `Bubble` Image (`Prototype.unity:919`). So the real intake is the UI sheet,
the newspaper, and the Ahuizotl woodcut.

The central finding for scoping: **the gameplay HUD currently has essentially no art.** Every
`Image`/`Button` graphic in the HUD is sprite-less (`m_Sprite: {fileID: 0}`), and lives /
patience / coins are pure TMP text (`VIDAS: 3`, `PACIENCIA: 3`, `MONEDAS: 0`). The approved
mockup therefore is mostly **NEW objects**, not re-skins — with one exception: the REFILL
arrows have an existing home (`RefillButton`). Two structurally identical gameplay scenes
(`Prototype.unity` + `Tutorial.unity`) and two identical Legend scenes (`Legend.unity` +
`TutorialLegend.unity`) must each receive the art, which doubles scene churn.

---

## The 4 PNGs — inventory, dimensions, destination

| PNG | File / guid | Pixels | Import | Destination | Status |
|---|---|---|---|---|---|
| `Escene UI.png` | `4d1c34e75cfa48b4499594399e05d055` | **1226×1080** | `spriteMode: 2` (Multiple), PPU 100, 13 slices | Gameplay HUD art (slices, see table) | **NEW** — referenced nowhere; sheet not used |
| `news paper.png` | `d9a0f9bd9e41a9a45ae394e23c51cef3` | **1920×1080** | `spriteMode: 2`, 1 sprite `news paper_0` (`fileID 7268870845823818854`) | Legend panel/backdrop | **NEW** |
| `ahuizolt newpaper.png` | `72cc892b55d22b54ab3f2dd9b73534f5` | **374×362** | `spriteMode: 2`, 1 sprite `ahuizolt newpaper_0` (`fileID -4686447789690588556`) | Legend per-level illustration (Ahuizotl) | **NEW** (but **already composited** into `news paper.png`) |
| `Creature talk.png` | `ceaf2db3554f0aa4da7f367a985c62be` | 747×246 | `spriteMode: 2` | Enemy speech-bubble backdrop | **DONE** (live on `Bubble` Image) |

Key fit fact: **`news paper.png` is exactly `1920×1080`** — identical to every scene's
`CanvasScaler.m_ReferenceResolution`. A full-screen Legend `Image` using it is **1:1, no
stretching**, at reference resolution. `ahuizolt newpaper.png` (374×362) ≈ the baked
illustration box in the newspaper (~368×357 measured) → an overlay aligns near-1:1.

### `Escene UI.png` verified slice rects (from `.meta`, bottom-left origin)

Sub-asset references for uGUI `Image.sprite` are `{fileID: <id>, guid: 4d1c34e75cfa48b4499594399e05d055, type: 3}`.

| slice | sub-asset fileID | w×h | Read from pixels | Mockup element |
|---|---|---|---|---|
| `Escene UI_0` | `-3305728652142534457` | 115×68 | tan/orange **sombrero** | **LIFE icon** (top-left, ×3 stacked) |
| `Escene UI_1` | `7438932503285152898` | 147×137 | black **creature head with bared teeth** (red mouth) | bottom cap of patience bar |
| `Escene UI_2` | `-5881899963768527644` | 466×299 | brown **open book / folded leather** (perspective) | recipe-book panel/tab (bottom-right) |
| `Escene UI_3` | `-4742025654892972584` | 328×993 | large **black brushstroke** | title banner / divider (not in mockup) |
| `Escene UI_4` | `280097415015255549` | 160×161 | red/brown **circle with black X** | exit/close button (bottom-left) |
| `Escene UI_5` | `917242437539518481` | 93×430 | white/lilac **vertical bar frame** (empty) | enemy patience bar container (top-right) |
| `Escene UI_6` | `7861356603297831984` | 153×136 | **gold Aztec stone** relief | required-ingredient indicator (top-center) |
| `Escene UI_7` | `2788912981022761824` | 160×161 | green circle w/ book + **"New"** | new-recipe / notes button |
| `Escene UI_8` | `-8992848258393318884` | 160×161 | gold circle w/ **back arrow** | back/return button |
| `Escene UI_9` | `4214963178974104771` | 100×102 | left half of **loop/refresh arrows** | REFILL button (art) |
| `Escene UI_10` | `-7074593693958136706` | 119×106 | right half of **loop/refresh arrows** | REFILL button (art) |
| `Escene UI_11` | `3327507871601162226` | 14×8 | auto-slice noise | **ignore** |
| `Escene UI_12` | `-1945782980059101193` | 527×348 | **orange corner arc** | decorative corner around exit button |

Correction to the brief: `_9` + `_10` are **two adjacent auto-slices of ONE loop-arrow glyph**
(`x 11–111` and `x 118–237`, same vertical band). Placing both side-by-side reproduces the
whole refill glyph; neither alone is the full icon.

---

## A) Gameplay HUD — current state (`Prototype.unity`, mirrored in `Tutorial.unity`)

Canvas (`Prototype.unity:2699` GO `&1839082317`, RectTransform `&1839082321`):
layer 5, `m_RenderMode: 0` (**Screen Space – Overlay**), `CanvasScaler` `m_UiScaleMode: 1`
(ScaleWithScreenSize), **`m_ReferenceResolution: {x: 1920, y: 1080}`**, `m_MatchWidthOrHeight: 0.5`,
`m_ReferencePixelsPerUnit: 100`, sorting order 0. Same scaler in `Tutorial.unity:2873` and both
Legend scenes.

Canvas children (order): `Hand` → `CenterDropZone` → `CornerDropZone` → `TopBar` → `RefillLabel`
→ `DishSlot` → `FadeOverlay`.

`TopBar` (`&870988178`, `HorizontalLayoutGroup`, stretch anchors, does **not** control children
width) children in order: `LivesLabel` → `PatienceLabel` → `CoinsLabel` → `RefillButton` (child
`RefillText`) → `PrepareButton` (child `Preparar`).

Component reality:
- **uGUI everywhere.** Labels are `TMPro.TextMeshProUGUI`; buttons are `UnityEngine.UI.Button`
  on an `Image`; UI graphics are `UnityEngine.UI.Image`.
- **No HUD art today.** Non-null `m_Sprite` in `Prototype.unity` UI: only `Bubble` (Creature
  talk) and the three world `SpriteRenderer`s (`Backdrop` BG.png, Player/Enemy `Visual`
  WhiteSprite). All four HUD Button/Image graphics are `m_Sprite: {fileID: 0}`.
- **Counts are text only.** `GameLoopController.UpdateHUD()` (`GameLoopController.cs:376-381`)
  writes only `livesText.text`, `patienceText.text`, `coinsText.text`. Wired refs
  (`Prototype.unity:2164-2166`): `livesText &158123648` (LivesLabel), `patienceText &810844229`
  (PatienceLabel), `coinsText &129699251` (CoinsLabel).

### Per-element HUD mapping table

| Mockup element | Slice | Existing receiver? | Where it goes | Verdict |
|---|---|---|---|---|
| Life icons (×3 hats) | `_0` | **No** (`LivesLabel` is TMP text only) | Top-left; new icon row inside/next to `TopBar` | **NEW** objects (3 Images) |
| Patience bar container | `_5` | **No** | Top-right; new `Image` 93×430 | **NEW** |
| Patience bar bottom cap | `_1` | **No** | Below/over the bar frame | **NEW** |
| Recipe-book panel/tab | `_2` | **No** | Bottom-right; new panel `Image` | **NEW** |
| Exit / close button | `_4` | **No** | Bottom-left; new `Button`+`Image` | **NEW** |
| Corner arc decoration | `_12` | **No** | Behind exit button | **NEW** |
| Required-ingredient stone | `_6` | **No** | Top-center; new `Image` (LevelConfig.requiredIngredient exists but no HUD slot) | **NEW** |
| New-recipe / notes button | `_7` | **No** | Near book panel | **NEW** |
| Back / return button | `_8` | **No** | Near book panel | **NEW** |
| REFILL arrows | `_9`+`_10` | **YES** — `RefillButton` (`&1765695987`, `Image` `&1765695990`, 160×60, `m_Sprite 0`) | Assign sprite to existing `Image` | **EXISTING** |
| Title banner / divider | `_3` | **No** | Not in this mockup (reserve for Legend/other) | **NEW** (unused here) |
| auto-slice noise | `_11` | — | — | **IGNORE** |

Net: **10 of 12 usable slices are NEW HUD objects; only the refill arrows reuse an existing GO.**
`LivesLabel`/`PatienceLabel`/`CoinsLabel` map to the mockup's *positions*, but the mockup
renders icons/bars where the code renders text — so the art **adds** objects; it does not
re-skin the labels. Keeping `GameLoopController` untouched means either (v1) the icons/bar are
static decor with the existing numeric labels still visible, or a tiny **presentation-only**
view reads the same ints to light N hats / fill the bar (the bar fill is blocked anyway).

### Sprite-assignment mechanics

- Assign via `Image.sprite` (and `Button.targetGraphic` already points at the same `Image`).
  Sub-sprites are directly usable: `{fileID: <slice id>, guid: 4d1c34e75cfa48b4499594399e05d055, type: 3}`.
- `spritePixelsToUnits: 100` on the sheet does **not** govern uGUI sizing; set explicit
  `RectTransform.sizeDelta` per slice (native px) + `preserveAspect` for the non-square
  slices (`_2`, `_5`, `_12`). At the 1920×1080 reference res, native px = 1:1 UI px.
- `TopBar` is a `HorizontalLayoutGroup`; adding art siblings inside it will **reflow** the
  labels/buttons. Prefer a separate anchored overlay layer over the Canvas (or disable the
  layout) rather than fighting the layout group.

---

## B) Legend scene — current state (`Legend.unity`, mirrored in `TutorialLegend.unity`)

Canvas (`Legend.unity:398`, `&763798952`) Screen Space – Overlay, CanvasScaler 1920×1080
match 0.5. Children:
- `Title` (`&1689384301`): TMP, top-stretch (`AnchorMin 0,1 / Max 1,1`), `sizeDelta {0,140}`,
  pivot top, `fontSize 72`, centre-aligned.
- `Body` (`&1591467180`): TMP, centre, `sizeDelta {1500, 665.17}`, `anchoredPosition y 67.585`,
  `fontSize 40` with `enableAutoSizing: 1` (min 10).
- `ContinuarButton` (`&1030593017`): centre, pos `(0,-320)`, 400×110, `Image` colour
  `(0.85,0.35,0.15)` (orange), sprite-less.
- `FadeOverlay` (`&122139368`): black `Image`, stretch.

`Legend` GO (`&347008879`): `LegendController` (`LegendController.cs`) with
`titleText &1689384303`, `bodyText &1591467182`, `fade &347008880`, `nextScene: Market`.
`TutorialLegend` is byte-identical except `nextScene: TutorialMarket` and **no `Backdrop` GO**.

`Backdrop` (`&1288732629`, only in `Legend.unity`): `SpriteRenderer` on `WhiteSprite`, tint
`(0.65,0.48,0.3)`, scale `40×24`, sorting `-100`.

`LegendController` fill logic (`LegendController.cs:38-52`): reads `CurrentLevel()` →
`GameSession.tutorialLevel` if set, else `database.levels[currentLevelIndex]`, and pushes
`legendTitle` → Title and `legendText` → Body. Empty text auto-forwards to next scene.
**No illustration slot exists.** Levels today: `LevelConfig_Level0Llorona` (La Llorona),
`_Level1Mexico` (El Ahuizotl), `_Level2Colombia` (Colombia).

### Legend plan

1. Add a full-screen `Image` (name e.g. `Paper`) as the Canvas **first child** (behind Title/
   Body, above `FadeOverlay`), sprite = `news paper_0`, anchored stretch → 1:1 at 1920×1080.
2. Move/reshape `Title` into the newspaper **masthead band** (top ~15%) and `Body` into the
   **left column** (the box sits inside the left column; the right column holds the article
   rules). Body is `1500` wide today and holds a long bulleted recipe list → narrow it to the
   column and rely on autosize (min may need lowering) or the text will overflow the column art.
3. Keep `ContinuarButton`; optionally restyle with `_4`/`_12` corner art (not required).
4. Mirror steps 1-3 into `TutorialLegend.unity`.

### CRITICAL — the per-level illustration question

`news paper.png` **already contains the Ahuizotl woodcut baked into its left box**, and the
standalone `ahuizolt newpaper.png` is the same drawing. Consequences:

- The real Legend level (El Ahuizotl / México) is served **perfectly by `news paper.png`
  alone** — zero extra work, zero code.
- `TutorialLegend` shows **La Llorona**, but the shared newspaper would display an Ahuizotl
  drawing → narrative mismatch. There is **no blank newspaper base** to put a different
  illustration in, and no Llorona drawing exists.

Options:
| # | Approach | Pros | Cons | Effort |
|---|---|---|---|---|
| A | **`news paper.png` for both scenes; do NOT add a per-level slot.** Accept the Ahuizotl drawing on the tutorial (or skip the paper in `TutorialLegend`). | Zero code, zero asset edits; matches the only art we have | Tutorial shows the wrong monster (minor, low-stakes screen) | **Low** |
| B | **Add `Sprite legendArt` to `LevelConfig` + an `Image legendArt` in `Legend.unity` + 3 lines in `LegendController`**; assign `ahuizolt newpaper` to Mexico. | Real per-level pipeline; future-proof | Needs a **cover** over the baked Ahuizotl for non-Ahuizotl levels (art we don't have) → otherwise draw-doubling/duplicate; 3 code+asset files touched | Medium |

**Recommendation: A for v1.** Per-level art is only worth it once a *blank* newspaper base
and/or a Llorona drawing exist, or a parchment cover is authored. Escalate to B only if the
tutorial mismatch is unacceptable. Either way, **do not re-scope** the three already-done assets.

---

## Out of scope (explicit)

- **Do NOT re-scope:** cave/lake backdrop, Sazón pot, `Creature talk.png` — all confirmed done.
- **Enemy patience bar FILL** — the colored fill slices were **not received** (the sheet has
  only the empty frame `_5` + cap `_1`). v1 can place the frame/cap only; the bar cannot fill.
  Do not promise a working patience meter.
- No changes to `GameLoopController` rules (lives/patience/coins/win-lose) or `GameLoopConfig`.
- No new DI/event-bus/architecture; no new assemblies.
- `Escene UI_3` and `_11` are not placed by this change (`_3` reserved, `_11` noise).
- No gameplay/interaction wiring for the new "New"/back/exit buttons in v1 (art only), unless
  a slice explicitly wires one.

---

## Risks

1. **Scene churn / fileID remap.** Hand-editing `.unity` YAML is fragile; adding UI means new
   GO + RectTransform + CanvasRenderer + Image blocks per element, across **4 scenes**.
   Prefer Unity MCP (`manage_gameobject`/`manage_ui`/`manage_components`) with the *correct*
   scene loaded. The editor was on `Menu.unity`, not `Prototype.unity` — the operator must
   `set_active_instance` then open the target scene before editing.
2. **Sprite scale / aspect.** `_2` (466×299), `_5` (93×430), `_12` (527×348) are non-square;
   without `preserveAspect` + explicit native `sizeDelta` they distort. Anchors must be used
   (not absolute px) because `MatchWidthOrHeight 0.5` scales differently per aspect ratio.
3. **TMP layout vs art grid.** `Body` (1500×665, autosize min 10) holds a long bulleted list;
   the newspaper's columns are a fixed painted grid. Expect overflow/clipping → narrow the
   column and re-tune autosize; verify visually (play mode + screenshot).
4. **Baked Ahuizotl** in the shared newspaper → tutorial mismatch (see Legend plan).
5. **Patience fill missing** → bar is a frame only (blocked item).
6. **`TopBar` HorizontalLayoutGroup** will reflow if art is added as a sibling (see A).
7. **Two-scene duplication** (`Prototype`+`Tutorial`, `Legend`+`TutorialLegend`) doubles churn
   and drift risk — keep the two copies in lockstep or extract a shared prefab.
8. **`Sprite-Lit` vs UI:** all HUD/Legend `Image`s are Screen Space – Overlay (unlit/unaffected
   by 2D lights) — no material work needed, but don't accidentally move art into world space.
9. **Concurrent change:** `scene-lighting-integration` touches `Prototype.unity` (the
   `Luces escena` prefab instance is already at `Prototype.unity:1097`) — merge-order risk.

---

## Split recommendation: **TWO changes** (not one)

Judged by the **400-line review budget** and **scene independence**:

- **HUD art** touches `Prototype.unity` + `Tutorial.unity`; **Legend art** touches
  `Legend.unity` + `TutorialLegend.unity`. Different scenes, different scripts (Legend may
  touch `LevelConfig`/`LegendController`; the HUD touches neither). No shared code.
- Each scene is already close to the budget on its own: one new UI `Image` ≈ 60–90 YAML lines,
  and the HUD needs ~10 new elements → **well past 400 lines for the HUD scene alone**, before
  duplicating into `Tutorial.unity`.

→ Two independent changes: **`gameplay-hud-art`** and **`legend-newspaper-art`**.

### Recommended slice order

1. **`legend-newspaper-art` / slice 1 — `Legend.unity`.** Full-screen `news paper_0` Image +
   Title/Body reposition. Smallest, safest, 1:1 sprite, highest visual payoff, no layout-group
   hazard. (Fresh scene to open; independent of the HUD.)
2. **`legend-newspaper-art` / slice 2 — `TutorialLegend.unity`** mirror (+ decide per-level
   illustration A vs B here).
3. **`gameplay-hud-art` / slice 1 — `Prototype.unity`, static art only:** assign `_9`+`_10` to
   `RefillButton`; add exit `_4` + corner `_12`, ingredient `_6`, back `_8`, new `_7`, book
   panel `_2` as anchored overlay Images at native size. No logic wiring.
4. **`gameplay-hud-art` / slice 2 — `Prototype.unity`, meter frames:** patience bar frame `_5`
   + cap `_1`; lives hats `_0` (×3). **Frame/cap only** (fill blocked). Keep the numeric labels.
5. **`gameplay-hud-art` / slice 3 — mirror slices 1-2 into `Tutorial.unity`** (identical HUD).

### 10x-simplest approach (jam scale)

- **No HUD framework, no new behaviour.** Add plain `Image`s at native size, anchored to the
  Canvas corners, sprite = slice by fileID. Leave `GameLoopController` and the TMP labels
  untouched — numbers keep showing; art is decor in v1.
- **Only reuse the one existing receiver:** `RefillButton` ← `_9`+`_10`.
- **Skip the patience fill** (blocked) and **skip button interactivity** for the new book/exit/
  back/new buttons.
- **Legend = one full-screen Image + nudge Title/Body.** Do **not** add a per-level slot
  (Option A) unless the Llorona mismatch is deemed unacceptable.
- If duplication across `Prototype`/`Tutorial` (and `Legend`/`TutorialLegend`) gets heavy,
  extract a single **HUD prefab** / **Legend panel prefab** and instance it in both scenes —
  one source of truth, halves the YAML and the drift risk.

---

## Evidence index

- **Sprites:** `Escene UI.png.meta` (guid `4d1c34e75cfa48b4499594399e05d055`; 13 slices,
  rects/fileIDs at `:164-449`, `nameFileIdTable` `:463-476`), `news paper.png.meta`
  (`d9a0f9bd9e41a9a45ae394e23c51cef3`; `news paper_0` `:129-149`), `ahuizolt newpaper.png.meta`
  (`72cc892b55d22b54ab3f2dd9b73534f5`; `ahuizolt newpaper_0` `:129-149`),
  `Creature talk.png.meta` (`ceaf2db3554f0aa4da7f367a985c62be`).
- **GUID grep (referenced nowhere):** `Escene UI` `4d1c…`, `news paper` `d9a0…`,
  `ahuizolt` `72cc…` → 0 hits in `Assets/Scenes|Prefabs|ScriptableObjects`. `Creature talk`
  `ceaf…` → already on `Prototype.unity:919` `Bubble` Image.
- **Prototype HUD:** Canvas `&1839082317`/`&1839082321` `:2699-2806`, CanvasScaler `:2735-2757`;
  `TopBar &870988178` `:1473-1539`; `LivesLabel` `:271-295`; `PatienceLabel` `:1257-1281`;
  `CoinsLabel` `:134-158`; `RefillButton &1765695987`/Image `&1765695990` `:2488-2599`;
  `PrepareButton &1237249286` `:1752-1876`; `RefillText` `:972-996`; `DishSlot` `:2450-2474`;
  `Bubble &687604047` (WorldSpace canvas) `:873-959`; `GameLoopController` refs `:2135-2173`.
- **Tutorial HUD:** same names/comps; `GameLoopController` refs `:2086-2092`; CanvasScaler
  `:2873-2876`; also has `ObjectiveLabel` `:2188` and `Midground` `:3095`.
- **Legend:** Canvas `&763798952`/`&763798953` `:398-502`, CanvasScaler `:457-479`;
  `Title &1689384301` `:943-1079`; `Body &1591467180` `:806-942`; `ContinuarButton
  &1030593017` `:503-635`; `FadeOverlay &122139368` `:122-196`; `LegendController
  &347008881` `:229-245` (`nextScene: Market`); `Backdrop &1288732629` `:715-805`.
- **TutorialLegend:** identical; `LegendController &347008881` `:229-245`
  (`nextScene: TutorialMarket`); no `Backdrop`; CanvasScaler `:469-472`.
- **Scripts:** `GameLoopController.cs:376-381` (text-only HUD), `:97-118` `StartGame`,
  `LegendController.cs:38-52` (Title/Body fill, no illustration),
  `LevelConfig.cs:38-51` (art slots: `background`, `midground`, `enemyArt`, `enemyPrefab`,
  `foreground` — **no legend illustration**), `GameLoopConfig.cs` (no presentation fields).
- **Assets:** `LevelConfig_Level0Llorona.asset` (La Llorona), `LevelConfig_Level1Mexico.asset`
  (El Ahuizotl; `background` = BG.png guid `e19d0ec9…`), `LevelConfig_Level2Colombia.asset`;
  `RecipeDatabase.asset`; `EditorBuildSettings.asset` (all 4 scenes enabled in build).
- **MCP:** live instance `GameJamUni2@9eee0fc1`; active scene at investigation = `Menu.unity`.
