# Exploration — `gameplay-hud-art`

> Investigation only. **No scene, prefab, or script was modified.** State was read from the
> `.unity`/`.meta` YAML on disk (authoritative) plus the Unity MCP live instance
> `GameJamUni2@9eee0fc1`. The live editor's active scene was **`Menu.unity`** (not
> `Prototype.unity`) at investigation time; the YAML on disk was the source of truth.
> Unity 6000.3.10f1, URP 17.3.0.
>
> This change deepens the **HUD half** of `openspec/changes/new-art-integration/explore.md`.
> The Legend/newspaper half is a SEPARATE change (`legend-newspaper-art`) and is not
> re-surveyed here.

## Intent

Place `Assets/_sprites/Escene UI.png` (13-slice sheet, guid `4d1c34e75cfa48b4499594399e05d055`,
`spriteMode: 2`, PPU 100, `alphaIsTransparency: 1`) as **presentation-only art** into the
gameplay HUD of the two mirrored scenes `Assets/Scenes/Prototype.unity` and
`Assets/Scenes/Tutorial.unity`. The runtime rules (lives / patience / coins / win-lose / refill
budget) MUST NOT change. The sheet is referenced **nowhere** today (verified by GUID grep across
`Assets/`), and every HUD `Image`/`Button` graphic is sprite-less (`m_Sprite: {fileID: 0}`) —
so this change is **mostly NEW objects**, with exactly one existing receiver (`RefillButton`).

---

## 1. Exact current HUD inventory

### 1.1 Canvas + scaler (identical in both scenes)

| | `Prototype.unity` | `Tutorial.unity` |
|---|---|---|
| Canvas GO | `&1839082317` (RT `&1839082321`, layer 5) | `&1839082317` (RT `&1839082321`) |
| Render mode | `m_RenderMode: 0` (Screen Space – Overlay) | same |
| CanvasScaler | `&1839082319`, `m_UiScaleMode: 1` (ScaleWithScreenSize), `m_ReferenceResolution {1920,1080}`, `m_MatchWidthOrHeight: 0.5`, `m_ReferencePixelsPerUnit: 100` | `&1839082319`, identical |
| Canvas RT | anchors `(0,0)-(0,0)`, `sizeDelta 0`, `pivot (0,0)` (driven by screen) | identical |

Canvas children, **same order in both scenes** (this is also the render/raycast order, back = first):

```
0  Hand           &1909901233   HandController (fe377c6c…)   stretch anchors
1  CenterDropZone &860907242    DropZone kind 0 + Image       (centre, pos 0,0 … see below)
2  CornerDropZone &789115917    DropZone kind 1 + Image       anchors(0.5,0.5) pos(-717,-310) 260×260
3  TopBar         &870988179    HorizontalLayoutGroup          stretch anchors, sizeDelta(-683.034,0)
4  RefillLabel    &1638346006   TMP 'REFILLS: 0/2'            anchors(0.5,0.5) pos(-717,-140) 300×44
5  DishSlot       &1710024343   (plain RT)                    anchors(0.5,0.5) pos(200,0) 150×200
6  FadeOverlay    &1152620044   Image (black, stretch)        m_Enabled 0 (FadeController drives it)
```

**IMPORTANT render-order rule:** `FadeOverlay` is the LAST child → it renders on top and is the
round-end black cover. Every existing interactive element (RefillButton, PrepareButton) is *below*
it. Any NEW HUD art/buttons MUST therefore be inserted **before `FadeOverlay`** (i.e. at index 6,
pushing FadeOverlay to 7), or the black cover will not hide them.

### 1.2 `TopBar` — the reflow hazard

`TopBar` (`&870988178`, RT `&870988179`) has a `HorizontalLayoutGroup`
(`m_ChildAlignment: 0` = UpperLeft, `m_Spacing: 0`, **`m_ChildForceExpandWidth: 1`**,
`m_ChildControlWidth: 0`, `ChildForceExpandHeight: 1`). It spans nearly the full canvas
(stretch anchors, `sizeDelta.x = -683.034`). Children are laid out left→right along the top strip:

**Prototype `TopBar` children (order = layout order):**

| # | child | fileID | component | sizeDelta |
|---|---|---|---|---|
| 1 | `LivesLabel` | `&158123647` | TMP `'VIDAS: 3'` | 140×44 |
| 2 | `PatienceLabel` | `&810844228` | TMP `'PACIENCIA: 3'` | 287.192×44 |
| 3 | `CoinsLabel` | `&129699250` | TMP `'MONEDAS: 0'` | 200×44 |
| 4 | `RefillButton` | `&1765695988` | Button + Image (sprite-less) | 160×60 |
| 5 | `PrepareButton` | `&1237249287` | Button + Image (sprite-less) | 160×60 |

`RefillButton` child: `RefillText` (`&712268014`, TMP `'Refill'`, stretch).
`PrepareButton` child: `Preparar` (`&475776561`, TMP `'Preparar Comida'`, stretch, bold-ish).

**Tutorial `TopBar` differs** — SAME GO/RT ids (`&870988178`/`&870988179`) but a **different child
set, order and sizes**: `sizeDelta.x = -627.341`; children =
`ObjectiveLabel`(`&1499845951`, TMP empty, 200×300, fontSize 30) → `CoinsLabel` → `RefillButton`
→ `PrepareButton` → `LivesLabel` → `PatienceLabel`, with labels **120** tall and buttons **~120–150**
tall (vs Prototype's 44/60). The two scenes are **NOT byte-identical** — the mirror step must not
blindly copy.

**Reflow hazard:** because `ChildForceExpandWidth = 1`, the group ignores the children's absolute
`anchoredPosition` and redistributes width across its direct children. Adding (or removing) a
direct child of `TopBar` changes the width of every other child. **Conclusion: never add HUD art as
a direct child of `TopBar`.** (Adding a child *inside* `RefillButton`/`PrepareButton` is safe — the
group only lays out its direct children.)

### 1.3 Script wiring (current, unchanged)

`GameLoop` GO `&1490598337`, `GameLoopController` `&1490598339`:
`livesText &158123648`, `patienceText &810844229`, `coinsText &129699251`, `handController
&1909901232`, `fadeController &1490598338`, `config` = GameLoopConfig guid `d1c0b17a…`.
`HandController` `&1909901232`: `refillLabel &1638346007`, `cornerZone &789115914`,
`centerZone &860907239`.
`RefillButton` onClick → `HandController.Refill` (`:2558-2560`). `PrepareButton` onClick →
`GameLoopController.ServeFood` (`:1835-1837`).

---

## 2. Per-element mapping (element → receiver → placement → slice → data → behaviour)

Sub-asset sprite reference form: `{fileID: <slice id>, guid: 4d1c34e75cfa48b4499594399e05d055, type: 3}`.

| # | Mockup element | Slice (px) | fileID | Existing receiver? | Placement (parent → anchors) | Data source today | Behaviour |
|---|---|---|---|---|---|---|---|
| 1 | Life icon (sombrero ×3, stacked) | `_0` 115×68 | `-3305728652142534457` | **NO** (`LivesLabel` is text-only) | NEW `LivesHats` container under `HUDArt`; anchor top-left `(0,1)`, pivot `(0,1)`, pos `(24,-24)`; 3 hat `Image`s stacked (y 0/−76/−152) | `GameLoopController._lives` → text `VIDAS: n` only | none (decor) |
| 2 | Required-ingredient aztec stone | `_6` 153×136 | `7861356603297831984` | **NO** | NEW `IngredientStone` under `HUDArt`; top-centre `(0.5,1)`, pivot `(0.5,1)` — place **below** the TopBar strip (y ≈ −170, see §3) | `LevelConfig.requiredIngredient` (CardData) EXISTS; **no HUD/code reads it**. `CardData.art` exists but unused | none (decor) |
| 3 | REFILL loop arrows | `_9` 100×102 / `_10` 119×106 | `4214963178974104771` / `-7074593693958136706` | **YES** — `RefillButton` Image `&1765695990` | assign sprite to existing `Image`; `preserveAspect: 1` | `HandController._refillsUsed` / `maxRefillsPerGame` → `RefillLabel` | existing `onClick → HandController.Refill` |
| 4 | Patience bar FRAME | `_5` 93×430 | `917242437539518481` | **NO** | NEW `PatienceBar` container `HUDArt`; top-right `(1,1)`, pivot `(1,1)`, pos `(−24,−24)`; frame centred in container | `_patience` → text `PACIENCIA: n` only; **no fill slices exist** | none (decor) |
| 5 | Patience bar BOTTOM CAP (creature teeth) | `_1` 147×137 | `7438932503285152898` | **NO** | NEW `Image` inside `PatienceBar`, below the frame (y ≈ −430) | — | none (decor) |
| 6 | Exit / close X button | `_4` 160×161 | `280097415015255549` | **NO** | NEW `ExitButton` under `HUDArt`; bottom-left `(0,0)`, pivot `(0,0)`, pos `(40,40)` | **NONE** | **NONE** (see §4) |
| 7 | Orange corner arc (around exit) | `_12` 527×348 | `-1945782980059101193` | **NO** | NEW `ExitCornerArc` under `HUDArt`; bottom-left `(0,0)`, pivot `(0,0)`, pos `(0,0)` — BEHIND `ExitButton` (earlier sibling) | — | none (decor) |
| 8 | Back / return button | `_8` 160×161 | `-8992848258393318884` | **NO** | NEW `BackButton` under `HUDArt`; bottom-right `(1,0)`, pivot `(1,0)`, pos `(−200,40)` | **NONE** | **NONE** |
| 9 | Green book + "New" button | `_7` 160×161 | `2788912981022761824` | **NO** | NEW `NewButton` under `HUDArt`; bottom-right `(1,0)`, pivot `(1,0)`, pos `(−40,180)` | **NONE** | **NONE** |
| 10 | Open book panel/tab | `_2` 466×299 | `-5881899963768527644` | **NO** | NEW `BookPanel` under `HUDArt`; bottom-right `(1,0)`, pivot `(1,0)`, pos `(0,0)` — BEHIND the two bottom-right buttons | — | none (decor) |
| 11 | Large black brushstroke | `_3` 328×993 | `-4742025654892972584` | **NO** | not placed (reserved) | — | — |
| 12 | auto-slice noise | `_11` 14×8 | `3327507871601162226` | — | ignore | — | — |

**Net: 10 of 12 usable slices are NEW HUD objects; only the REFILL glyph reuses an existing GO.**

---

## 3. Layout plan (1920×1080 reference; corner-anchored, native `sizeDelta`)

Because the scaler is `ScaleWithScreenSize @ 1920×1080, Match 0.5`, the Canvas **reference area
changes with aspect** (see §5). Corner/centre anchors are therefore mandatory; absolute px are not.

**Proposed container.** Add ONE empty `RectTransform` named `HUDArt` as a Canvas child **inserted
at index 6, immediately before `FadeOverlay`** (FadeOverlay moves to 7). All new decor + the new
decorative buttons live under it. Sibling order inside `HUDArt` (back → front):

```
1  BookPanel        _2    bottom-right  (1,0) pivot(1,0) pos(0,0)       466×299
2  ExitCornerArc    _12   bottom-left   (0,0) pivot(0,0) pos(0,0)       527×348
3  LivesHats        _0×3  top-left      (0,1) pivot(0,1) pos(24,-24)    115×(3·68+2·8)=115×220
4  PatienceBar      _5+_1 top-right     (1,1) pivot(1,1) pos(-24,-24)   147×567
5  IngredientStone  _6    top-centre    (0.5,1) pivot(0.5,1) pos(0,-170) 153×136
6  ExitButton       _4    bottom-left   (0,0) pivot(0,0) pos(40,40)     160×161
7  BackButton       _8    bottom-right  (1,0) pivot(1,0) pos(-200,40)   160×161
8  NewButton        _7    bottom-right  (1,0) pivot(1,0) pos(-40,180)   160×161
```

- **`IngredientStone` y ≈ −170** (not −24): `TopBar` is a full-height stretch RT whose layout
  places its children in the **top strip** (UpperLeft). A stone at y −24 would sit under
  `PatienceLabel`/`CoinsLabel`. Sink it below the strip, or narrow `TopBar` — flag for visual pass.
- `RefillButton` keeps its TopBar slot (no reflow); only its `Image.sprite` changes (§2 row 3).
- All new `Image`s: `m_Type: 0` (Simple), **`m_PreserveAspect: 1`**, `m_RaycastTarget` 0 for decor
  (1 only if a Button is later wired).

## 4. Button behaviour — none exists (flag)

Exhaustive search of `Assets/Scripts/` + both scenes: the **only** scene loads are
`GameLoopController.LoadNextScene` (win→`Scenes.Map`/Credits, lose→`Scenes.Prototype`),
`MenuController`(Menu→Map, Menu→TutorialLegend), `MapController`(→Legend),
`LegendController`(→Market), `MarketController`(→Prototype), `CreditsController`(→Menu).
There is **no pause, no in-game menu, no exit, no back, no recipe-list/notes UI, and no code
that reads a "recipe list"** anywhere.

→ **Exit X (`_4`), Back (`_8`) and Book/New (`_7`) have NO behaviour to attach to.** v1
recommendation: place them as **static decoration** (no `Button` component, or a `Button` with an
empty `onClick`). If the user wants Exit→`Scenes.Map` (the run hub) or a notes panel, that is a
**small new wiring decision outside "presentation only"** and must be approved explicitly.
The `BookPanel` (`_2`) is just the backdrop for those two buttons.

## 5. Scale / aspect

- At **16:9** (1920×1080) `scaleFactor = 1` → native px = 1:1 UI px, corner anchors land exactly.
- At a **wider aspect** (e.g. 2560×1080, 21:9) `Match 0.5` gives `scaleFactor ≈ 1.1547`, so the
  Canvas **reference area shrinks** to ≈ 2217×935 ref units. Consequences: corner elements stay
  pinned, but top- and bottom-pinned elements get *relatively* larger and can approach/overlap in
  the middle. Keep elements compact; do not span top→bottom with one element.
- Non-square slices (`_2` 466×299, `_5` 93×430, `_12` 527×348, `_1` 147×137) **must** use
  `preserveAspect: 1` **and** a native `sizeDelta` or they distort when the rect changes with aspect.
- `spritePixelsToUnits: 100` on the sheet does **not** govern uGUI sizing — the `Image` uses the
  sprite rect; set `sizeDelta` explicitly.

## 6. Scene-churn risk (confirmed)

`git status`: **`Assets/Scenes/Prototype.unity` is MODIFIED (uncommitted)**; `Tutorial.unity` is
clean. The pending diff is **mid-flight `scene-lighting-integration`** (its `design.md` targets
exactly `Prototype.unity` + `Tutorial.unity`: "Delete no-op global; add `Luces escena` prefab
instance"). The working-tree diff shows:
`Global Light 2D` (`&619394800`) `m_IsActive: 1→0`; `Bubble` Image sprite ← `Creature talk.png`
(`:919`); a root `Luces escena` PrefabInstance added (`&757798897`); `BubbleText` font size + margin
tweaks. `Tutorial.unity` has **not** been touched yet by that change.

**Collision is real and on BOTH scenes.** `scene-lighting-integration` and `gameplay-hud-art`
edit the same two scene files.

**Sequencing recommendation:** finish + commit `scene-lighting-integration` (both scenes) FIRST,
then branch `gameplay-hud-art` off that commit. Do not edit a scene that has uncommitted,
unrelated changes — hand-merging Unity YAML for root order + prefab instances is where fileIDs get
corrupted.

---

## Out of scope (explicit)

- **Patience-bar FILL:** the coloured fill slices were **not received** (only empty frame `_5` +
  cap `_1`). The bar is **frame + cap only** and cannot fill. The numeric `PACIENCIA: n` label
  keeps showing the real value.
- **Not re-scoped (already done):** cave/lake backdrop, Sazón pot, `Creature talk.png`.
- **`legend-newspaper-art`** (`Legend.unity` + `TutorialLegend.unity`, `news paper.png`) is a
  SEPARATE change.
- `_3` (brushstroke) and `_11` (noise) are **not** placed (`_3` reserved for a future banner).
- No changes to `GameLoopController`, `GameLoopConfig`, `HandController`, or any runtime rule.
- No new MonoBehaviour / HUD framework / DI / event-bus / assembly. No button behaviour wiring in
  v1 (decor only) unless the user approves Exit→Map.

## Risks

1. **Uncommitted `Prototype.unity`** (scene-lighting mid-flight) — highest risk; sequence the two
   changes (§6) before any HUD edit.
2. **`TopBar` `HorizontalLayoutGroup` reflow** — never add art as a direct `TopBar` child (§1.2).
3. **Render order vs `FadeOverlay`** — new art MUST sit before it, or the round-end cover fails.
4. **Non-square slices distort** without `preserveAspect` + native `sizeDelta` (`_2`,`_5`,`_12`,`_1`).
5. **Aspect-dependent canvas area** (Match 0.5) → top/bottom-pinned elements can crowd at ultrawide (§5).
6. **`IngredientStone` top-centre overlaps `TopBar`** → sink below the strip; verify by screenshot.
7. **Two-scene drift** — `Tutorial.unity`'s `TopBar` child set/order/sizes already differ → mirror
   by hand, then screenshot both.
8. **`_9`+`_10` are two halves of ONE glyph** — one slice alone is not the full loop icon. v1
   assigns one slice (`_10`), accept the partial glyph, OR add the other half as a *child* of
   `RefillButton` (safe from reflow) with pixel-exact offsets — a follow-up polish.
9. **No data source for 4 elements** (ingredient stone, exit, back, book/New) → decor-only or the
   user makes a wiring decision.
10. Hand-editing `.unity` YAML is fragile — prefer Unity MCP with the **correct scene loaded**
    (`set_active_instance GameJamUni2@9eee0fc1` then open the target scene; editor is on `Menu.unity`).

---

## 10x-simplest implementation approach (jam scale)

- **No HUD framework, no new behaviour, no data wiring.** Add plain `Image`s at native size,
  corner/centre-anchored, `sprite = slice by fileID`. Leave `GameLoopController` /
  `HandController` and every TMP label untouched — numbers keep showing; art is decor.
- **One container** `HUDArt` (Canvas child, inserted before `FadeOverlay`) groups every new element.
- **Only reuse the one existing receiver:** `RefillButton.Image` ← `_10` (or `_9`).
- **Skip** the patience fill (blocked) and all new-button interactivity.
- **`IngredientStone`** = static `_6`; optional later: overlay `LevelConfig.requiredIngredient.art`.
- If duplication across `Prototype`/`Tutorial` gets heavy, extract `HUDArt` as a **prefab** and
  instance it in both scenes (one source of truth; halves YAML + drift).

### Suggested apply-slice order

1. **`Prototype.unity` — bottom corners (safest static decor):** `HUDArt` container +
   `BookPanel _2`, `ExitCornerArc _12`, `ExitButton _4`, `NewButton _7`, `BackButton _8`. Screenshot.
2. **`Prototype.unity` — top strip:** `LivesHats _0`×3, `IngredientStone _6`, `PatienceBar _5`+`_1`.
   Screenshot (check stone vs `TopBar` overlap).
3. **`Prototype.unity` — refill:** `RefillButton.Image` ← `_10`, `preserveAspect 1`. Screenshot +
   `read_console`.
4. **`Tutorial.unity` — mirror 1–3** (mind the different `TopBar` layout). Screenshot + `read_console`.
5. *(optional)* extract `HUDArt` as a prefab instanced in both scenes.

---

## Evidence index

- **Sprite sheet:** `Assets/_sprites/Escene UI.png.meta` — guid `4d1c34e75cfa48b4499594399e05d055`,
  `spriteMode: 2`, PPU 100, `alphaIsTransparency: 1`; slice rects `:165-449`;
  `nameFileIdTable` `:463-476` (`_0 -3305728652142534457`, `_1 7438932503285152898`,
  `_2 -5881899963768527644`, `_3 -4742025654892972584`, `_4 280097415015255549`,
  `_5 917242437539518481`, `_6 7861356603297831984`, `_7 2788912981022761824`,
  `_8 -8992848258393318884`, `_9 4214963178974104771`, `_10 -7074593693958136706`,
  `_11 3327507871601162226`, `_12 -1945782980059101193`).
- **GUID grep:** `4d1c34e…` → only its own `.meta`; referenced nowhere in `Assets/`.
- **Prototype HUD:** Canvas `&1839082317`/RT `&1839082321` `:2699-2806`; CanvasScaler `:2735-2757`;
  children list `:2792-2799`; `TopBar` `&870988178`/RT `&870988179` `:1473-1539`; `LivesLabel`
  `:259-395`; `PatienceLabel` `:1245-1381`; `CoinsLabel` `:122-258`; `RefillButton`/`RefillText`
  `:2475-2607`; `PrepareButton`/`Preparar` `:1752-1884`; `RefillLabel` `:2189-2325`; `DishSlot`
  `:2440-2474`; `FadeOverlay` `:1679-1751`; `GameLoopController` refs `&1490598339` `:2149-2173`;
  `HandController` `&1909901232` `:2824-2846`.
- **Tutorial HUD:** TopBar `&870988178` RT `&870988179` `:1338-1367` (children order + `-627.341`);
  `ObjectiveLabel` `&1499845951` `:2188-2330`; CanvasScaler/children `:2838-2946`.
- **Diff (uncommitted):** `git diff Assets/Scenes/Prototype.unity` — `Global Light 2D`
  `&619394800` `m_IsActive 1→0`; `Bubble` sprite ← `Creature talk` `:919`; `Luces escena`
  PrefabInstance `&757798897`; `BubbleText` font/margin; SceneRoots `:2971`.
- **Scripts:** `GameLoopController.cs:376-381` (text-only HUD), `:338-363` (scene routing);
  `GameLoopConfig.cs:12-31`; `HandController.cs:318-322` (`UpdateRefillLabel`), `:152-204` (Refill);
  `LevelConfig.cs:19-23` (`requiredIngredient`, `recipes`); `CardData.cs:28-29` (`art`, unused);
  `Scenes.cs` (all scene names); `Menu/Map/Market/Legend/Credits` controllers (scene loads only).
- **Level assets:** `Assets/ScriptableObjects/Cards/LevelConfig_Level1Mexico.asset:17`
  (`requiredIngredient` = CardData guid `73ddc3817602f5f408165551adaec29f`).
- **MCP:** live instance `GameJamUni2@9eee0fc1`; active scene = `Menu.unity`, not playing.
