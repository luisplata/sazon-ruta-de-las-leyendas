# Apply Progress — cut-tutorial

Status: **done** (lean apply — tutorial taken off the Menu; scenes/guards left orphaned on purpose).
Scope: Menu scene object deletion + dead MenuController wiring removal. No gameplay code touched.

## 1. What was removed

### `Assets/Scenes/Menu.unity`
Deleted GameObject **`TutorialButton`** — path `Canvas/Buttons animations/TutorialButton` — and its child
`Label`, including every component block:

| fileID | Component |
|---|---|
| 954539211 | GameObject `TutorialButton` |
| 954539212 | RectTransform (`TutorialButton`) |
| 954539213 | `UnityEngine.UI.Button` (onClick → `MenuController.StartTutorial`) |
| 954539214 | `Image` (orange, `m_Sprite: 0`) |
| 954539215 | CanvasRenderer |
| 1856227607 | GameObject `Label` |
| 1856227608 | RectTransform (`Label`) |
| 1856227609 | `TMPro.TextMeshProUGUI` (`m_text: TUTORIAL`) |
| 1856227610 | CanvasRenderer |

Also removed the now-dangling child entry `{fileID: 954539212}` from `Buttons animations` (1939692740)
`m_Children`, and the two serialized `MenuController` fields `tutorialConfig` + `tutorialStartCoins`
(component 2132263056) — these were dropped automatically on recompile + scene save because the script
no longer declares them (Unity discards unknown serialized fields).

**Untouched (verified still present):** `JugarButton`, `CreditosButton`, `SalirButton`, `Backdrop`,
`Canvas`, the `Buttons animations` Animator, `EventSystem`, `Main Camera`, and the whole layout.
The 2 pre-existing **uncommitted** `m_TextStyleHashCode: -1183493901` lines (teammate edit) were preserved.

### `Assets/Scripts/Menu/MenuController.cs`
- Removed field `tutorialConfig` (`LevelConfig`) + its summary comment.
- Removed field `tutorialStartCoins` + its summary comment.
- Removed method `StartTutorial()` + its summary comment.
- **Kept:** `fade`, `salirButton`, `Start()`, `StartRun()`, `GoToCredits()`, `QuitGame()`. File is now 62 lines.

## 2. Grep result (whole `Assets/`)

Before removal, `StartTutorial|tutorialConfig|tutorialStartCoins` matched **only two files**:
- `Assets/Scripts/Menu/MenuController.cs` — the definitions.
- `Assets/Scenes/Menu.unity` — the button's `onClick.m_MethodName: StartTutorial` (line 755) and the
  serialized fields `tutorialConfig` / `tutorialStartCoins` (lines 2138–2139).

After removal: **ZERO matches in `Assets/`.** Nothing else referenced them → safe to delete, not a guess.

## 3. Orphaned bits left behind (deliberately NOT deleted — out of scope)

- **`GameSession.tutorialLevel`** (`Assets/Scripts/GameLoop/GameSession.cs:35`) is now set by **nobody**.
  It is still *read* by the runtime guards at `HandController.cs:293`, `LegendController.cs:141`,
  `MarketController.cs:180` and documented at `TutorialController.cs:14`. Those guards are now dead
  branches (always `null`) but harmless.
- Scenes `Tutorial.unity`, `TutorialLegend.unity`, `TutorialMarket.unity` and the `Scenes.TutorialLegend`
  / `Scenes.TutorialMarket` constants (`Assets/Scripts/GameLoop/Scenes.cs:16-17`) are now unreachable —
  the Menu was their only entry point.
- **Recommendation:** the team should deliberately delete the three tutorial scenes + `GameSession.tutorialLevel`
  + the three guards in a follow-up. Not done here (per brief).

## 4. Animation dangling-path finding (reported, not fixed)

- `Assets/mk/Main menu buttons.anim` still contains **8 curves** bound to the missing object:
  `path: TutorialButton` (m_IsActive, Image.m_Enabled, Image.m_Color.a) and
  `path: TutorialButton/Label` (TMP.m_fontColor.a).
- **Unity logs NOTHING.** After entering Play mode and letting the full clip (2.333 s) play to the end,
  `read_console` (all types) returned only the MCP bridge port messages and the pre-existing
  `Account API did not become accessible within 30 seconds` line. **No** missing-binding / Animator warning.
- **The clip still plays `JugarButton` correctly.** The `Buttons animations` Animator is enabled and holds
  the clip's final frame; `JugarButton.m_IsActive` animates 0→1 and its Image + Label alpha animate 0→1,
  so JUGAR appears and settles visible (confirmed live: `activeInHierarchy=True`, Image `color.a=1`,
  TMP alpha=1, text `JUGAR`). The missing TutorialButton bindings are silently ignored.
- **Do NOT edit the `.anim`** (constraint). The dangling paths are harmless; the teammate should prune
  them when convenient.

## 5. Play-mode evidence

- Entered Play mode through the live MCP bridge. Screenshot
  **`Assets/Screenshots/cut-tutorial-menu-2.png`** (1280×720):
  Sazón logo card + **JUGAR** (orange, centred) + **CRÉDITOS** (bottom-left) + **SALIR** (bottom-right),
  and **no tutorial button**.
  `cut-tutorial-menu.png` (first capture) caught an early reveal frame with the menu not yet drawn; it was
  misleading and has been deleted — `-2` is the settled state and matches the live query.
- Live object check: `TutorialButton` → **gone**; `JugarButton`, `CreditosButton`, `SalirButton` → all
  active with alpha=1.
- `JugarButton.onClick` → 1 persistent listener → target `MenuController`, method `StartRun`.
  Invoking it loaded **`Assets/Scenes/Map.unity`** (`activeScene=Map`) → JUGAR still works.
- Animator on `Buttons animations` enabled, playing the clip through to completion (final frame held).

## 6. Console / compile

- `refresh_unity` + compile: **clean** (0 errors, 0 warnings).
- `read_console` (errors + warnings) after the edits: only the MCP bridge port fallback messages
  (`6401` → `6400`) and the pre-existing `Account API…` line. No missing-script / missing-reference errors.
- Deleting the button removed its `onClick` UnityEvent target in the same pass as removing the method, so no
  dangling UnityEvent reference was left behind.
- `LiberationSans SDF - Fallback.asset` was **not** re-dirtied (absent from `git status`).

## 7. Files changed

- `Assets/Scenes/Menu.unity` — deleted `TutorialButton` + `Label` (9 object/component blocks) and dropped
  the 2 serialized tutorial fields.
- `Assets/Scripts/Menu/MenuController.cs` — removed `tutorialConfig`, `tutorialStartCoins`, `StartTutorial()`.
- `Assets/Screenshots/cut-tutorial-menu-2.png` — evidence (gitignored).

No commits made. The teammate's `.anim` / AnimatorController were **not** modified.
