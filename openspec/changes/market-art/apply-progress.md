# apply-progress — market-art

Change: `market-art` (lean apply, no proposal/spec/tasks ceremony).
Scope: presentation only — make the Market read well over `Assets/_sprites/Store.png`.

## What changed (RectTransform only)

File touched: `Assets/Scenes/Market.unity` — 4 RectTransforms, 24 lines, no scripts, no assets, no new components.

| Object | Before (anchors / pos / size) | After | Why |
|---|---|---|---|
| `Title` (MERCADO) | center (0.5,0.5), pos (0,440), size 1400×120 | top-left (0,1), pivot (0,1), pos (30,−10), size 640×110 | Was sitting ON the art's **SHOP** sign → read as duplicated/broken header. Now sits top-left over the wooden beam, clear of the sign (sign starts ≈x730 of 1920). |
| `CoinsLabel` (MONEDAS: N) | center (0.5,0.5), pos (0,330) | bottom-right (1,0), pivot (1,0), pos (−50,40), size 620×80 | Was stacked under the old title. Now anchored bottom-right directly **under the CASH register**, which is a trivial anchor move and thematically right (coins → till). Corner anchor = stable across aspect ratios. |
| `CatalogRoot` (buy list) | center, pos (0,60), size 900×480 | center, pos (**−150**,60), same size | The list straddled the shelf/merchant boundary (right edge ≈x1410 of 1920, over the merchant's shoulder). Shifted left so the 4 buy buttons float fully over the **shelf boards** (≈x360–1260). Size/vertical ignored — no attempt to align to the 3 shelf boards, as instructed. |
| `ListoButton` | center, pos (0,−420) | center, pos (**−150**,−420) | Same 150 px left shift so it stays aligned under the list column; still over the desk band, reachable and legible. |

Nothing else was touched. `MarketController.cs`, buy-button spawning, item data, sprites, `BuyButtonTemplate` (and therefore the runtime-spawned buttons' own look) are untouched — the layout shift comes purely from the parent `CatalogRoot`.

## Evidence

Screenshots (gitignored `Assets/Screenshots/`):

- BEFORE: `Assets/Screenshots/market_before.png` (960×540, play mode) — MERCADO overlapping the SHOP sign, MONEDAS crowding it, list straddling the merchant.
- AFTER: `Assets/Screenshots/market_after.png` (960×540, play mode) — MERCADO top-left, list cleanly over the shelves, MONEDAS under the CASH register, LISTO centred under the list.

### Console status

`read_console` after the change (play mode entered + exited): **1 entry only** —
`MCP-FOR-UNITY: StdioBridgeHost started on port 6401` — no errors, no warnings.
`LiberationSans SDF - Fallback.asset` was NOT modified (`git status` clean of any TMP font asset) — no revert needed.

## Deliberately left alone

- **No shelf-board alignment.** The 3 shelf boards were not measured/used; the list floats over the backdrop by design (per task).
- **Button fill/alpha.** The buy buttons are a semi-transparent teal; changing fill colour/alpha was out of scope (only rect + at most text colour allowed). They read acceptably over the shelf.
- **Text colours.** Both TMP labels kept white — contrast over the wood beam (title) and the dark floor band (coins) is fine as-is.
- **`ListoButton` size/colour, `FadeOverlay`, `EventSystem`, `Main Camera`, `Backdrop`** — untouched. Backdrop (world-space `SpriteRenderer`, scale 0.8, PPU 100 → ≈20.3×10.75 world units) is wider than the 16:9 ortho view (17.8×10), so the art crops ~7% per side; accepted as-is.
- **Non-16:9 drift** accepted (per task). Title/coins are corner-anchored so they stay put; the centre-anchored list drifts slightly under `CanvasScaler` match=0.5 on very wide/tall aspects.

## Process note (not a code issue)

The Unity MCP bridge dropped for ~8 minutes right after exiting the first play-mode session: MCP-for-Unity disables its bridge while the **Editor window is unfocused**. The first rect edit attempt through `execute_code` therefore failed; the same values were applied directly to `Market.unity` on disk, then Unity was brought to the foreground (bridge returned, domain reload, scene reloaded from disk), and the values were verified in-editor before the after-screenshot. No rework was needed and the on-disk result is identical to the intended MCP edit.
