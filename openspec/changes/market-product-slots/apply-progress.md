# apply-progress — market-product-slots

Change: `market-product-slots` (lean apply, no proposal/spec/tasks ceremony).
Goal: spawn Market products into **predefined scene slots** (art-driven positions) instead of stacking them via a `VerticalLayoutGroup` on `CatalogRoot`. Up to 6 slots, configurable.

Mode: **Standard** — not Strict TDD. This is a UI-placement + scene-wiring change (MonoBehaviour + scene serialization) with no test assembly/runner for it; the config's own testing note says gameplay feel is validated via PlayMode/screenshots. No pure logic was introduced that would benefit from an EditMode test. (Deviation from `strict_tdd: true` recorded here deliberately, per the lean-apply instruction.)

---

## 1. Code — `Assets/Scripts/Market/MarketController.cs`

| # | Change | Detail |
|---|--------|--------|
| 1 | New field `[SerializeField] List<RectTransform> productSlots = new();` | Configurable in the Inspector (6 entries wired). Empty/null → legacy fallback. |
| 2 | `Start()` rewritten | Collects valid items, computes `useSlots`, **overflow guard** (`Debug.LogWarning` when valid items > slot count), then spawns the i-th valid product with `slotIndex = i`. Loop `break`s at `productSlots.Count`, so leftover slots stay empty. |
| 3 | `SpawnButton(item, slotIndex)` (was `SpawnButton(item)`) | Parent = `productSlots[slotIndex]` when mapped, else `catalogRoot`. When a slot is used, the instance is stretched to fill it: `anchorMin=(0,0)`, `anchorMax=(1,1)`, `pivot=(0.5,0.5)`, `anchoredPosition=0`, `sizeDelta=0`. |
| 4 | `catalogRoot` fallback preserved | When `productSlots` is empty/null, every product parents to `catalogRoot` with the template's original rect — the legacy path. The guard changed from `if (template==null || catalogRoot==null) return;` to `if (template==null) return;` + parent-null check, so the slot path no longer requires `catalogRoot`. |

**Unchanged** (per req 5): `Buy`/coins/sold-out logic, label format `"{displayName}  x{quantity}  ${price}"`, `Refresh()`, `CurrentMarket()`, tutorial override, `MarketConfig`, `nextScene`, `Done()`.

Tooltip on `buyButtonTemplate` updated to mention slots (cosmetic).

---

## 2. Scene — `Assets/Scenes/Market.unity`

### 2.1 `CatalogRoot` — `VerticalLayoutGroup` DISABLED (req 6)
`CatalogRoot` keeps its `VerticalLayoutGroup` component but `m_Enabled: 0`. Chosen "disable" over "remove" per the task preference (reversible, one checkbox, and the component still documents intent).

### 2.2 New container `ProductSlots` (child of `Canvas`)
Full-stretch RectTransform: `anchorMin=(0,0)`, `anchorMax=(1,1)`, `offsetMin=offsetMax=(0,0)`, `pivot=(0.5,0.5)`. No `Graphic`, so it never blocks raycasts. Six empty children, each a plain `RectTransform` (no `Image`), anchor/pivot centre.

### 2.3 Slot size & positions

**Size choice: 330 × 110** (as suggested). Rationale from measuring `Store.png` (2535×1344; backdrop scale 0.8, PPU 100 → ≈20.3×10.75 world units; CanvasScaler reference 1920×1080, match 0.5 → ~108 UI px per world unit):

- Left shelf columns are delimited by the inner wood dividers at ≈ image-fraction x 0.31 and 0.505 → column width ≈ 340–425 UI px. 330 keeps a small gutter and fits all three columns inside the shelf frame (≈ x 360–1260 of 1920).
- Two usable shelf levels (above the top board ≈ UI-y 168; between top and middle board ≈ UI-y 157 … −105). A 3-col × 2-row grid maps 6 slots onto "3 compartments × 2 shelves".

Positions (canvas-centre coords, `anchoredPosition`):

| Slot | anchoredPosition | Size | Roughly over |
|------|------------------|------|--------------|
| `ProductSlot_1` | (−586, 293) | 330×110 | upper shelf, left compartment |
| `ProductSlot_2` | (−203, 293) | 330×110 | upper shelf, middle compartment |
| `ProductSlot_3` | (219, 293) | 330×110 | upper shelf, right compartment |
| `ProductSlot_4` | (−586, 26) | 330×110 | lower shelf, left |
| `ProductSlot_5` | (−203, 26) | 330×110 | lower shelf, middle |
| `ProductSlot_6` | (219, 26) | 330×110 | lower shelf, right |

These are **rough** (centred in each shelf compartment, not resting on the boards) — the user fine-tunes.

### 2.4 Wiring (req 8)
`MarketController.productSlots` populated with `ProductSlot_1` … `ProductSlot_6` in order. `catalogRoot` remains wired (`CatalogRoot`) as the fallback.

`BuyButtonTemplate` (prefab/look), `Title`, `CoinsLabel`, `ListoButton`, `FadeOverlay`, `Backdrop`, `Main Camera` — **untouched**.

---

## 3. Validation

### Console
`refresh_unity` (compile) + `read_console(error|warning)` → **0 entries**. Only unrelated entry seen during the run: `Account API did not become accessible within 30 seconds…` (network/focus, not project code). No compile errors.

### Play mode on `Market.unity` (index 0 = México = El Ahuizotl, 4 items)
- **AFTER (slots):** the 4 items land AT `ProductSlot_1..4` (Insectos / Chile Piquín / Cebolla on the upper shelf, Tomate on the lower-left); **slots 5–6 empty**; NOT stacked. `Assets/Screenshots/final_after.png` (also `slots_after-3.png`).
- **BEFORE (legacy stacked):** with `productSlots` temporarily emptied and the `VerticalLayoutGroup` re-enabled, the same 4 items stack vertically — reproduces the original look. `Assets/Screenshots/before_stacked-3.png`.
- Reverted the probe afterward (no probe state saved).

### Fallback probe (`productSlots` emptied) — **finding**
- With the VLG **disabled** (current final state) and slots emptied, all 4 products parent to `CatalogRoot` but **overlap at the root's centre** (they no longer auto-stack). Evidence: `Assets/Screenshots/fallback_empty_slots-3.png`.
- Proof that this is caused purely by the VLG toggle, not by the slot code: with the VLG **enabled** and the 6 slots wired, products still land exactly at the slots (`Assets/Screenshots/vlg_enabled_slots.png`, identical to the AFTER shot). ⇒ the `CatalogRoot` `VerticalLayoutGroup` only lays out its **direct** children; slot-parented products are grandchildren of `Canvas`, so it cannot affect them.
- **Consequence:** req 6 (disable VLG) and req 4 ("catalogRoot fallback behaves exactly as today" = vertical stack) are in tension. Kept the VLG **disabled** as explicitly instructed; the empty-slots fallback therefore spawns into `catalogRoot` but no longer stacks. **One-line fix if the stacked fallback is wanted: re-check `VerticalLayoutGroup` on `CatalogRoot` — it has zero effect on the slot path.**

### TMP font
`LiberationSans SDF - Fallback.asset` was re-dirtied by TMP during the Play sessions; **reverted** via `git checkout --` after each run (final `git status` shows it clean).

---

## 4. Left for the user
- **Fine-tune the 6 `ProductSlot_*` rects** onto the actual shelf boards (positions are rough; `ProductSlots` sits just under the Canvas). Sizing a slot controls the product size.
- **Restyle `BuyButtonTemplate`** (current semi-transparent teal fill). Its look is inherited by every spawned product; the instance only stretches to fill its slot.
- Decide the VLG question above (keep disabled = current, or re-enable for a stacked fallback).

## 5. Files changed
- `Assets/Scripts/Market/MarketController.cs` — Modified (slots + overflow guard + fallback).
- `Assets/Scenes/Market.unity` — Modified (VLG disabled, `ProductSlots` + 6 slots, `productSlots` wired).
- `openspec/changes/market-product-slots/apply-progress.md` — Created.
- Screenshots (gitignored `Assets/Screenshots/`): `before_stacked-3.png`, `slots_after-3.png`, `fallback_empty_slots-3.png`, `vlg_enabled_slots.png`, `final_after.png`.
