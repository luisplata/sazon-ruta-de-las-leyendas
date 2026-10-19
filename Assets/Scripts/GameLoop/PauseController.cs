using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Pause overlay for the gameplay scenes (Prototype + Tutorial). Escape (new
/// Input System — the project uses Input System ONLY) toggles a shared pause
/// panel. The panel's REANUDAR / SALIR AL MENÚ buttons are wired in Awake from
/// the serialized refs, so ONE PausePanel prefab works unchanged in every scene
/// that instances it.
///
/// Pausing freezes the game with Time.timeScale = 0 (card coroutines run on
/// scaled time, so they stop). Escape still works while paused because the new
/// Input System reads raw device state, independent of timeScale.
///
/// Exiting restores Time.timeScale = 1 FIRST (a scene loaded while timeScale is
/// 0 would start frozen — including the Menu's own fade), resets the run to a
/// clean state, then loads the Menu.
/// </summary>
public class PauseController : MonoBehaviour
{
    [Tooltip("Root of the pause overlay. Starts inactive; Pause() shows it, Resume() hides it.")]
    [SerializeField] GameObject pausePanel;

    [Tooltip("REANUDAR button — wired in Awake to Resume().")]
    [SerializeField] Button resumeButton;

    [Tooltip("SALIR AL MENÚ button — wired in Awake to ExitToMenu().")]
    [SerializeField] Button exitButton;

    bool _paused;

    /// <summary>True while the game is paused (timeScale frozen, panel shown).</summary>
    public bool IsPaused => _paused;

    /// <summary>
    /// Cache the input/keyboard, hide the panel (scene starts unpaused), and
    /// hook the panel buttons. Runs before any button can fire, so the shared
    /// prefab needs no per-scene wiring.
    /// </summary>
    void Awake()
    {
        _paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (exitButton != null) exitButton.onClick.AddListener(ExitToMenu);
    }

    /// <summary>Escape toggles pause. Guardian: no keyboard (headless) = no-op.</summary>
    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Toggle();
    }

    /// <summary>Pause if running, resume if paused.</summary>
    public void Toggle()
    {
        if (_paused) Resume();
        else Pause();
    }

    /// <summary>Show the panel and freeze the game (scaled time stops — cards, fades).</summary>
    public void Pause()
    {
        if (_paused) return;
        _paused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    /// <summary>Hide the panel and restore normal time.</summary>
    public void Resume()
    {
        if (!_paused) return;
        _paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Leave to the Menu. timeScale is restored FIRST so the Menu does not load
    /// frozen, the run is reset (clean wallet/level), then the Menu scene loads.
    /// </summary>
    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        GameSession.ResetRun();
        SceneManager.LoadScene(Scenes.Menu);
    }
}
