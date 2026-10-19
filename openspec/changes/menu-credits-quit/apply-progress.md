# Apply Progress — menu-credits-quit

## Change summary
The **Menu** now offers two secondary actions under the existing JUGAR / TUTORIAL
buttons:

- **CRÉDITOS** → `MenuController.GoToCredits()` → `SceneManager.LoadScene(Scenes.Credits)`
- **SALIR** → `MenuController.QuitGame()` → `Application.Quit()` (hidden on WebGL)

JUGAR and TUTORIAL are untouched. The Credits scene was not modified.

## Mode
**Standard** (not Strict TDD). Pure Unity UI + scene wiring; the only logic is a
platform guard and two one-line actions. Validated live in Play mode on `Menu.unity`.

## What changed

### `Assets/Scripts/Menu/MenuController.cs` (modified)
| Addition | Detail |
|---|---|
| `using UnityEngine.UI;` | Needed for the serialized `Button` reference |
| `[SerializeField] Button salirButton;` | Inspector ref to the SALIR button |
| `Start()` guard | `if (Application.platform == RuntimePlatform.WebGLPlayer && salirButton != null) salirButton.gameObject.SetActive(false);` |
| `public void GoToCredits()` | `SceneManager.LoadScene(Scenes.Credits);` — uses the constant, no raw string |
| `public void QuitGame()` | `Application.Quit();` |

`Scenes.Credits` from `Assets/Scripts/GameLoop/Scenes.cs` is used; no raw scene name.
`Start()` still runs `fade.FadeIn()` exactly as before, so the intro reveal is unchanged.

### `Assets/Scenes/Menu.unity` (modified)
Two new Canvas children (`Canvas` > `CreditosButton`, `SalirButton`), inserted **before**
`FadeOverlay` so the fade overlay remains the LAST sibling and keeps rendering on top.

| Button | Label | Anchored pos | Size | Font | Image color |
|---|---|---|---|---|---|
| `CreditosButton` | `CRÉDITOS` | (-110, -330) | 200×70 | LiberationSans SDF, 28 | RGBA(0.72, 0.30, 0.13) |
| `SalirButton` | `SALIR` | (110, -330) | 200×70 | LiberationSans SDF, 28 | RGBA(0.72, 0.30, 0.13) |

Each was cloned from `JugarButton` (same TMP font asset, same button colours/highlight
behaviour, same `Label` child structure), then the cloned persistent listener was removed
before wiring the new one.

### Placement choice
**Secondary row centered under the two main buttons** at y = -330 (JUGAR at -80,
TUTORIAL at -210). The two small 200×70 buttons sit side by side (±110 px) at 28 pt,
versus 400×110 at 44 pt for the primary actions. Same orange family (slightly darker) so
they read as secondary without introducing a new palette. This keeps the vertical
hierarchy title → primary actions → secondary actions and leaves the bottom area free for
the backdrop art.

### Wiring (persistent `onClick`, verified in the saved scene)
| Button | Target | Method | Persistent calls |
|---|---|---|---|
| `CreditosButton` | `Menu` (`MenuController`) | `GoToCredits` | 1 |
| `SalirButton` | `Menu` (`MenuController`) | `QuitGame` | 1 |

`MenuController.salirButton` serialized field = `SalirButton (UnityEngine.UI.Button)`.

## Validation evidence

### Compile / console
- `refresh_unity` scope=scripts, compile=request → no compile errors.
- `read_console` errors/warnings after the script change: **0 entries**.
- After the Play-mode session the only warning is unrelated infrastructure:
  `Account API did not become accessible within 30 seconds. This may be due to network
  issues or editor focus.` — no script/scene errors.
- `git status` shows only `Assets/Scenes/Menu.unity` and
  `Assets/Scripts/Menu/MenuController.cs`. `LiberationSans SDF - Fallback.asset` was **not**
  re-dirtied.

### Play mode — `Menu.unity`
| Step | Observed |
|---|---|
| Enter play | Menu reveals after the 1 s fade; both new buttons visible |
| Screenshot | `openspec/changes/menu-credits-quit/screenshots/menu-final.png` — JUGAR / TUTORIAL / CRÉDITOS / SALIR all legible, secondary row centred |
| `CreditosButton.onClick.Invoke()` | active scene changed `Menu` → **`Credits`** |
| Credits scene inspection | `CreditsBody` text present (see below); `VolverButton` persistent call = `GoToMenu` (unchanged) |
| Back to Menu | `SceneManager.LoadScene(Scenes.Menu)` → active scene `Menu` |
| `SalirButton` state | `activeSelf=True`, `activeInHierarchy=True` (Editor platform = `WindowsEditor`, so the button correctly shows) |
| `Application.platform` | `WindowsEditor` |
| `SalirButton.onClick.Invoke()` | still `isPlaying=True`, scene `Menu` — method ran without exception |

> Note: clicks were exercised by invoking the button's persistent `onClick` event
> (equivalent to the listener the UI raycast would fire). The `EventSystem` raycast path
> itself was not driven with a synthetic pointer event.

### SALIR caveat — Editor no-op (explicit)
`Application.Quit()` is a **NO-OP in the Unity Editor**. Calling it (directly and through
`SalirButton.onClick`) left Play mode running on `Menu`; the Editor stays alive. The
observable proof of correct wiring is therefore the persistent listener
(`SalirButton` → `Menu (MenuController).QuitGame`) plus a clean invocation with no
exception — **not** an actual application exit. Real quit behaviour only applies in a
standalone player build.

### WebGL hide — code path
Not runtime-verified (no WebGL player build in this session). The Editor reports
`Application.platform == WindowsEditor`, so the `RuntimePlatform.WebGLPlayer` branch is
not exercised. The guard is null-safe on `salirButton` and only hides the button; it does
not affect JUGAR/TUTORIAL or the fade.

## State of the Credits scene text
`CreditsBody` (TMP, 40 pt, 1200×300) currently reads:

```
Sazón: Ruta de las Leyendas

Gracias por jugar
```

It is **not empty**, but it is effectively a placeholder: no team names, roles, or asset
credits. Reported only — the Credits scene was not modified per the task constraints.

## Files changed
| File | Action | What |
|---|---|---|
| `Assets/Scripts/Menu/MenuController.cs` | Modified | `salirButton` ref + WebGL hide; `GoToCredits()`, `QuitGame()` |
| `Assets/Scenes/Menu.unity` | Modified | `CreditosButton`, `SalirButton`, listeners, `salirButton` inspector ref |
| `openspec/changes/menu-credits-quit/apply-progress.md` | Created | This report |
| `openspec/changes/menu-credits-quit/screenshots/menu-final.png` | Created | Play-mode screenshot of the updated menu |

Not committed (per instructions).
