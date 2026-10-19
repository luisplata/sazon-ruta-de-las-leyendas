using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Boot scene controller: reveals the menu with a fade-in, and starts a fresh
/// run on JUGAR — resets the run state (GameSession.ResetRun → México) and
/// loads the Map. The scene starts opaque black (FadeController.Awake), so
/// Start plays the intro reveal like the game scene does.
/// </summary>
public class MenuController : MonoBehaviour
{
    [Tooltip("Full-screen black overlay for the intro reveal.")]
    [SerializeField] FadeController fade;

    /// <summary>
    /// SALIR button. Hidden on WebGL in Start: a browser page cannot close its
    /// own tab, so Application.Quit() is a no-op there and a dead button is
    /// worse than none. In the Editor the platform is WindowsEditor, so the
    /// button stays visible for testing.
    /// </summary>
    [SerializeField] Button salirButton;

    /// <summary>
    /// Intro reveal: black → transparent. Runs in Start so
    /// FadeController.Awake has already forced the overlay opaque first.
    /// </summary>
    void Start()
    {
        if (fade != null) fade.FadeIn();

        if (Application.platform == RuntimePlatform.WebGLPlayer && salirButton != null)
            salirButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// JUGAR: reset the run to México and move to the Map.
    /// Wired to the button's onClick in the Menu scene.
    /// </summary>
    public void StartRun()
    {
        GameSession.ResetRun();
        SceneManager.LoadScene(Scenes.Map);
    }

    /// <summary>
    /// CRÉDITOS: loads the Credits scene. Wired to the button's onClick.
    /// </summary>
    public void GoToCredits()
    {
        SceneManager.LoadScene(Scenes.Credits);
    }

    /// <summary>
    /// SALIR: closes the game. No-op in the Editor and on WebGL (the button is
    /// hidden there in Start).
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }
}