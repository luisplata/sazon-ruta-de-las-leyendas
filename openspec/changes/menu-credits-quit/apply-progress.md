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

---

# Batch 2 — Credits body text (`CreditsBody`)

Filled the placeholder credits text in `Assets/Scenes/Credits.unity`. **Text only** on the
`CreditsBody` TMP component — no new objects, no anchors moved, `VolverButton` and `Title`
untouched.

## Previous text (before)

```
Sazón: Ruta de las Leyendas

Gracias por jugar
```

(Not empty, but a placeholder: no team names or roles. Font `LiberationSans SDF`, 40 pt,
white, centered, rect 1200×300, `Overflow` mode, `lineSpacing` 0.)

## New text (after)

```
Sazón: Ruta de las Leyendas

Gracias por jugar

Enier Garcia
Director de arte y animador

Danet
Art Concept artist

PeryLoth
Programación

Camilo
Música y SFX
```

Raw serialized value (`\n` = real newline, plain-text fallback):

```
Sazón: Ruta de las Leyendas\n\nGracias por jugar\n\nEnier Garcia\nDirector de arte y animador\n\nDanet\nArt Concept artist\n\nPeryLoth\nProgramación\n\nCamilo\nMúsica y SFX
```

## Rich text applied?

**Yes.** The component already had `richText: true`, so the four names are bolded and the
role lines are plain:

- `<b>Enier Garcia</b>` / `Director de arte y animador`
- `<b>Danet</b>` / `Art Concept artist`
- `<b>PeryLoth</b>` / `Programación`
- `<b>Camilo</b>` / `Música y SFX`

Serialized text field stores the `<b>…</b>` tags. Spelling corrected as instructed:
**arte**, **Programación**, **Música**; "Art Concept artist" kept as-is (English role title).
Font / size / colour unchanged (LiberationSans SDF, 40, white).

## Deviation — `lineSpacing` 0 → −34 (required to stop the overflow)

The mandated content is 15 lines. At 40 pt with default spacing the block measures
**688.6 px**, but there are only ~575 px between the `Title` glyph block (bottom ≈ y+804 in
canvas space) and the `VolverButton` (top ≈ y+215). Measured clearances:

| `lineSpacing` | block height | gap to Title | gap to Button | verdict |
|---|---|---|---|---|
| 0 (default) | 688.6 | **−80.1 (collides)** | **−19.3 (collides)** | fails |
| −25 | 548.6 | −10.1 (collides) | +50.7 | fails |
| −30 | 520.6 | +3.9 | +64.7 | too tight |
| **−34 (applied)** | **498.2** | **+15.1** | **+75.9** | legible |
| −40 | 464.6 | +31.9 | +92.7 | over-compressed |

Evidence: `screenshots/credits-overflow-check.png` (default spacing — first line sits on top
of the `CRÉDITOS` heading and "Música y SFX" is behind the button) vs
`screenshots/credits-final.png` (with −34 — fully legible, no overlap).

Font size, font, colour, anchors, rect and `VolverButton` were **not** changed. `lineSpacing`
is a typographic property of the same TMP component (no object/asset added), and it is a
one-field, easily reversible value. Flagged here because the task said "text only".

## Screenshot

`screenshots/credits-final.png` — Play mode on `Credits.unity` (960×540): `CRÉDITOS`
heading, game title, "Gracias por jugar", the four bolded names + roles, and the
`VOLVER AL MENÚ` button all legible with clear separation.
`screenshots/credits-overflow-check.png` — kept as the "before" evidence for the spacing fix.

## Font-asset status — `LiberationSans SDF - Fallback.asset`

**Re-dirtied: YES — but NOT by the credits text.**

- The credits accents (`ó` in *Sazón* / *Programación*, `ú` in *Música*) render from the
  **base static** `LiberationSans SDF` asset (mode 0 = Static); no new glyphs were baked for
  them.
- The only glyph added to the dynamic fallback (mode 1) is **`→` U+2192 / `m_Unicode: 8594`**
  — it comes from other project content (`Assets/Scenes/Legend.unity` and the three
  `Assets/ScriptableObjects/Cards/LegendPage_*.asset` files), baked during a Play-mode
  session in this batch, not by the credits change.
- The asset also has `m_ClearDynamicDataOnBuild: 1`, so this pre-baked glyph data is
  discarded on player build and rebaked at runtime anyway — the diff has no build value.
- **Left re-dirtied** (not reverted) because it is outside this change's scope and reverting
  could clobber a concurrent session's in-flight work. **The user decides whether to commit
  it.** To drop it: `git checkout -- "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"`.

## Scope of the scene diff

`git diff Assets/Scenes/Credits.unity` = **2 hunks, both on the `CreditsBody`
`TextMeshProUGUI` MonoBehaviour**: `m_text` and `m_lineSpacing`. A TMP material *instance*
that leaked into the scene during editing was restored to the original shared font material
(`m_sharedMaterial` → `{fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee}`,
`m_fontMaterial` → null), so no stray `Material` object remains.

## Validation

- `refresh_unity` (scope=all) + `read_console` → **0 errors / 0 warnings** from the change.
  The single remaining entry is the pre-existing infrastructure notice:
  `Account API did not become accessible within 30 seconds…` (unrelated to this change).
- Play mode on `Credits.unity`: rendered correctly **both** before and after restoring the
  shared material (identical output).
- No compile errors; no scripts touched.
- `git status` → only `Assets/Scenes/Credits.unity` + the two screenshots (untracked) + the
  unrelated fallback font asset above.

## Files changed

| File | Action | What |
|---|---|---|
| `Assets/Scenes/Credits.unity` | Modified | `CreditsBody.m_text` = full credits; `m_lineSpacing` = −34; leaked TMP material instance reverted to the shared font material |
| `openspec/changes/menu-credits-quit/screenshots/credits-final.png` | Created | Play-mode screenshot, final credits |
| `openspec/changes/menu-credits-quit/screenshots/credits-overflow-check.png` | Created | Play-mode screenshot, default spacing overflow (evidence) |
| `openspec/changes/menu-credits-quit/apply-progress.md` | Modified | This section |

Not committed (per instructions).
