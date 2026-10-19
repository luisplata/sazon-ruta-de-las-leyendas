# Apply Progress — pause-to-menu

## Change summary
The two gameplay scenes (`Prototype`, `Tutorial`) now pause on **Escape** and offer
**REANUDAR** / **SALIR AL MENÚ**. `PauseController` freezes the game with
`Time.timeScale = 0`, shows a shared full-screen `PausePanel` prefab, and exits to the
Menu by restoring `timeScale = 1` FIRST, calling `GameSession.ResetRun()`, then loading
`Scenes.Menu`. One prefab works unchanged in both scenes because the panel's buttons are
wired at runtime in `Awake()`.

## Mode
**Standard** (not Strict TDD). Pure Unity presentation + scene wiring; the only testable
seam is the toggle/timeScale state, and Unity has no in-repo test assembly for this flow.
Acceptance was validated live in Play mode in BOTH scenes with screenshots (see below).

## Resume context — what the cancelled run had ALREADY done
Confirmed by reading the artifacts — no rework was required:

| Artifact | State found | Verdict |
|---|---|---|
| `Assets/Scripts/GameLoop/PauseController.cs` (+`.meta`) | Complete: `Update()` reads `Keyboard.current.escapeKey.wasPressedThisFrame` (guarded `Keyboard.current != null`), `Toggle()`, `Pause()`, `Resume()`, `ExitToMenu()`, `Awake()` wires `onClick` from serialized refs, hides panel on start | ✅ correct, unchanged |
| `Assets/Prefabs/PausePanel.prefab` (+`.meta`) | Root full-screen `RectTransform`, `m_IsActive: 0`; children `Dim` (black 65%), `Title` (`PAUSA`, 96pt), `ResumeButton` (`REANUDAR`, green), `ExitButton` (`SALIR AL MENÚ`, red) | ✅ correct, unchanged |
| `Assets/Scenes/Prototype.unity` | `PausePanel` prefab instance parented to Canvas (`1839082321`) as its **LAST** child (after `FadeOverlay`); `PauseController` on the `GameLoop` root GO with `pausePanel`/`resumeButton`/`exitButton` all resolving | ✅ correct, unchanged |
| `Assets/Scenes/Tutorial.unity` | Same as Prototype: last Canvas child, `PauseController` on `GameLoop` with resolved refs | ✅ correct, unchanged |

**Nothing was broken and nothing needed fixing.** The cancelled run had completed the
implementation and scene wiring; the only missing piece was this progress report (and the
end-to-end Play-mode proof), which this run produced.

## What this run did
1. Static audit of `PauseController.cs`, `PausePanel.prefab`, and both scenes' YAML
   (component refs, Canvas child order, prefab-instance stripped mappings).
2. `refresh_unity` (force, compile) → clean, no recompilation errors.
3. Play mode on **Prototype**: probe → Escape → screenshot → Resume → Escape → Exit.
4. Play mode on **Tutorial**: same sequence.
5. Confirmed `activeInputHandler: 1` (Input System only) and that no TMP fallback asset was
   re-dirtied (`LiberationSans SDF - Fallback.asset` is absent from `git status`).

## Validation evidence

### Compile / console
- `refresh_unity scope=all compile=request` → `ready_for_tools: true`.
- `read_console` errors → **0 log entries**. Only unrelated MCP bridge warnings
  ("Port 6402 not released yet… falling back to port 6400").

### Play mode — Prototype.unity
| Step | Observed |
|---|---|
| Fresh play | `IsPaused=False timeScale=1 panelActive=False resumeWired=True exitWired=True` — panel starts hidden |
| Escape (Input System state event queued) | `IsPaused=True timeScale=0 panelActive=True` — game frozen, panel shown |
| Screenshot | `openspec/changes/pause-to-menu/screenshots/pause-prototype.png` (dim + PAUSA + REANUDAR + SALIR AL MENÚ) |
| `ResumeButton.onClick.Invoke()` | `IsPaused=False timeScale=1 panelActive=False` |
| Escape (re-pause) | `IsPaused=True timeScale=0` |
| `ExitButton.onClick.Invoke()` | `timeScale=1` immediately, then `scene=Menu timeScale=1` — Menu fades in normally (screenshot `after-exit-prototype-menu.png`) |

### Play mode — Tutorial.unity
| Step | Observed |
|---|---|
| Fresh play | `scene=Tutorial IsPaused=False timeScale=1 panelActive=False resumeWired=True exitWired=True` |
| Escape | `IsPaused=True timeScale=0 panelActive=True` |
| Screenshot | `openspec/changes/pause-to-menu/screenshots/pause-tutorial.png` |
| `ResumeButton.onClick.Invoke()` | `IsPaused=False timeScale=1 panelActive=False` |
| Escape (re-pause) | `wasPaused=True tsBefore=0` |
| `ExitButton.onClick.Invoke()` | `timeScale=1` immediately, then `scene=Menu timeScale=1` |

Note: `Button.onClick.GetPersistentEventCount()` is `0` by design — listeners are added at
runtime in `Awake()` (`AddListener`), which is exactly what makes the SHARED prefab work in
both scenes without per-scene onClick wiring.

## Files
| File | Action | What |
|---|---|---|
| `Assets/Scripts/GameLoop/PauseController.cs` (+`.meta`) | Pre-existing (cancelled run) | Pause overlay controller — verified, unchanged |
| `Assets/Prefabs/PausePanel.prefab` (+`.meta`) | Pre-existing (cancelled run) | Shared pause panel — verified, unchanged |
| `Assets/Scenes/Prototype.unity` | Pre-existing (cancelled run) | PausePanel instance + PauseController wiring — verified, unchanged |
| `Assets/Scenes/Tutorial.unity` | Pre-existing (cancelled run) | Same — verified, unchanged |
| `openspec/changes/pause-to-menu/apply-progress.md` | Created (this run) | This report |
| `openspec/changes/pause-to-menu/screenshots/*.png` | Created (this run) | Play-mode evidence |

## Deliberately left out / scope
- **No code or scene changes made** — the partial work was already correct and minimal.
- Did not touch card / refill / market / legend logic, `Assembly-CSharp` layout, or add any
  asmdef / input-action asset (Input System API is auto-available in the default assembly).
- Did not commit (per task instructions).
- Escape-to-pause state is not persisted across scene loads (each gameplay scene starts
  unpaused) — matches the required behaviour.
