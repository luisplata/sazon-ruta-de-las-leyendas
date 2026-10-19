# Apply Progress — HUD lives (hats) + patience (3-segment colored bar)

**Change**: `hud-lives-patience` (lean apply — no proposal/spec/design/tasks artifacts; this file is the only SDD artifact)
**Mode**: Standard (edit + Play-mode validation). `strict_tdd: true` is configured, but **no test assemblies exist** and this change introduces **no unit-testable logic** (it only adds serialized UI references + two `SetActive` loops to an existing MonoBehaviour). Per the task scope, validation is Play-mode + screenshots, not edits to `Assets/Tests`.
**Status**: done

---

## 1. What changed

### 1.1 Script — `Assets/Scripts/GameLoop/GameLoopController.cs`

> NOTE: the task said `RefreshHud()`; the actual method is **`UpdateHUD()`** — that is the single HUD push point and where the loops were added.

- Added `using UnityEngine.UI;`.
- Added two serialized arrays (with tooltips) on the controller:
  ```csharp
  [SerializeField] Image[] livesHats;
  [SerializeField] Image[] patienceSegments;
  ```
- Extended `UpdateHUD()` (the existing HUD push point, called from `StartGame`, `GrantCoins`, `Bite`, and the patience-loss branch) with the verbatim rule:
  ```csharp
  for (int i = 0; i < livesHats.Length; i++)
      if (livesHats[i] != null) livesHats[i].gameObject.SetActive(i < _lives);
  for (int i = 0; i < patienceSegments.Length; i++)
      if (patienceSegments[i] != null) patienceSegments[i].gameObject.SetActive(i < _patience);
  ```
- Documented the behaviour in the XML summary. **No other rule touched** — bite, refill, fade, coin, win/lose, card, market, legend and pause logic are unchanged.

### 1.2 Prefab — `Assets/Prefabs/HUDArt.prefab`

Added **three plain colored `Image`s** (NO sprite, `raycastTarget = 0`) as children of the `PatienceBar` cluster, between the `Frame` (renders below them) and the creature-head `Cap` (renders above them):

| Name | Visual position | Color (RGBA) | RectTransform (rel. to PatienceBar) |
|------|-----------------|--------------|--------------------------------------|
| `PatienceSeg_1` | top | green `(0.2, 0.7, 0.3, 1)` | anchor (0.5,1) pivot (0.5,1), pos `(0,-16)`, size `65×126` |
| `PatienceSeg_2` | middle | yellow `(0.95, 0.8, 0.15, 1)` | pos `(0,-152)`, size `65×126` |
| `PatienceSeg_3` | bottom | red `(0.85, 0.2, 0.2, 1)` | pos `(0,-288)`, size `65×126` |

Frame interior is 93×430 → segments are 14px inset per side and end 16px above the frame bottom (frame spans y=0…-430). Confirmed inside the frame and above the cap in Play mode (see screenshots).

### 1.3 Scenes — wired in BOTH `Prototype.unity` and `Tutorial.unity`

Set the two arrays on each scene's `GameLoopController` (`GameLoop` GameObject). Array order (see decision below):
- `livesHats` = `[Hat1 (top), Hat2 (mid), Hat3 (bottom)]`
- `patienceSegments` = `[PatienceSeg_3 (red/bottom), PatienceSeg_2 (yellow/mid), PatienceSeg_1 (green/top)]`

References are stored as scene-local *stripped* prefab-instance child objects (Unity's normal form for referencing prefab-instance children), e.g. Prototype `patienceSegments[0] → stripped 1550142822 → source 7412000000000000034 (PatienceSeg_3)`.

---

## 2. Segment order / colours (as implemented)

Visual top → bottom: **green, yellow, red**. Array `patienceSegments` (index 0 first): **[red, yellow, green]**.

---

## 3. FLAGGED DECISION — patience drains TOP → BOTTOM

**Decision encoded**: patience is lost from the **top down** — green disappears first, then yellow, and at `patience == 1` **only the RED segment remains**; at `patience == 0` none (the enemy bites).

**Why the array is `[red, yellow, green]` (not "green first")** — the task text is internally inconsistent:
- It states the visual order top→bottom is `[green, yellow, red]`, AND
- it states `patienceSegments[i]` is active while `i < _patience`, AND
- it states that at patience 1 **only RED remains**.

Those three cannot all hold. With the **verbatim** loop `SetActive(i < _patience)` and a **"green first"** array, the LAST element drops first, so at patience 1 the **GREEN** would remain (bottom-up loss) — contradicting the stated outcome and looking wrong (danger-red would only show at full patience).

To honour the **behavioural decision** ("top-down / at 1 only RED remains") while keeping the loop **exactly** as specified, `patienceSegments` is assigned **bottom→top = [red, yellow, green]**. Verified in Play mode (patience 3 = 3 segments, patience 1 = red only).

**One-line flip if the team prefers the opposite (bottom-up / green survives at 1):** swap the `patienceSegments` assignment order in both scenes (assign `[green, yellow, red]` instead), OR reverse the index in the loop. Flagging for team confirmation.

---

## 4. Play-mode evidence (Prototype.unity)

Runtime state read via reflection on the live controller; screenshots in `Assets/Screenshots/`.

| State | `lives` / `patience` | Hats active | Segments active | Screenshot |
|-------|----------------------|-------------|-----------------|------------|
| Start | 3 / 3 | Hat1 ON, Hat2 ON, Hat3 ON | red ON, yellow ON, green ON | `hud_start_3x3.png` |
| Patience loss | 3 / **1** | all ON | **PatienceSeg_3 (red) ON**, seg2/seg1 off | `hud_patience1_red.png` |
| Bite | **2** / 3 (refilled) | Hat1 ON, Hat2 ON, **Hat3 off** | red ON, yellow ON, green ON (refilled) | `hud_bite_2lives_refill.png` |

Screenshots visually confirm: 3 hats (top-left), green/yellow/red stacked inside the patience frame; at patience 1 only the red segment remains; after a simulated bite VIDAS: 2 with 2 hats and the bar refilled to 3.

States were driven by setting the controller's private `_lives` / `_patience` and invoking `UpdateHUD()` (reflection) — exactly the "drive it by setting the fields and calling the refresh" path the task allowed.

### Both scenes
Refs confirmed assigned in **Prototype.unity** (lines ~2319) and **Tutorial.unity** (lines ~2219) — `livesHats` × 3 and `patienceSegments` × 3 each (verified in scene YAML + via reflection at runtime in Prototype).

---

## 5. Console / health

- `refresh_unity` + compile: **clean** (0 errors, 0 warnings from the change).
- `read_console` after Play mode: only one unrelated entry — `"Account API did not become accessible within 30 seconds…"` (Unity services noise, not from this change).
- `LiberationSans SDF - Fallback.asset` was **not** re-dirtied (no revert needed).
- `git status`: only the 4 intended files modified (screenshots are gitignored). No commit made.

---

## 6. Files changed

| File | Action |
|------|--------|
| `Assets/Scripts/GameLoop/GameLoopController.cs` | Modified — `using UnityEngine.UI;`, `livesHats` + `patienceSegments` fields, two `SetActive` loops in `UpdateHUD()`, doc comments |
| `Assets/Prefabs/HUDArt.prefab` | Modified — 3 plain colored segment `Image`s under `PatienceBar` |
| `Assets/Scenes/Prototype.unity` | Modified — arrays wired (+ stripped prefab-child refs) |
| `Assets/Scenes/Tutorial.unity` | Modified — arrays wired (+ stripped prefab-child refs) |
| `Assets/Screenshots/hud_start_3x3.png` etc. | Created — Play-mode evidence (gitignored) |

---

## 7. Deviations from the literal task text

1. `RefreshHud()` → implemented in the real method **`UpdateHUD()`**.
2. `patienceSegments` assigned **bottom→top [red, yellow, green]** instead of "green first", to honour the stated top-down decision with the verbatim loop (see §3). **Flagged for team decision.**
3. `livesHats` removal direction was unspecified; array is top→bottom `[Hat1, Hat2, Hat3]`, so the **bottom** hat drops first (stack shrinks upward). Not flagged as a problem, noted for awareness.
