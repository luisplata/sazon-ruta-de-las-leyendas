using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Owns the round rules: player lives, enemy patience (the enemy identity is
/// the current LevelConfig), win/lose, and the fade + scene-reload flow. Serves
/// as the bridge between the UI (PREPARAR button) and HandController.
/// The game starts AUTOMATICALLY on scene load (Start → StartGame): the intro
/// fade reveal + deal happens without any button press.
///
/// Mapping (user-validated, single switch in ServeFood):
///   Green (Normal) / Gold (Star)  → WIN        → fade → load next scene
///   Blue (Presented) / Purple (Cursed) / Gray (Filler) → patience −1 (benign retry)
///   Red (Fail "Comida Cruda")     → BITE       → life −1, patience refills
///   Patience 0 → BITE (life −1, patience refills); lives 0 → GAME OVER → fade → load next scene.
///
/// Animation wiring (all null-guarded — unwired views no-op): PlayerView/
/// EnemyView receive per-outcome triggers; win/game-over reactions fire in
/// ServeFood/Bite BEFORE the round-end fade (DelayedFade) so they stay visible
/// for ReactionShowDelay seconds. The scene reload destroys the old views; the
/// fresh scene instance starts with new Idle views.
///
/// All runtime state lives HERE on the MonoBehaviour — never in the config SO.
/// </summary>
public class GameLoopController : MonoBehaviour
{
    [Tooltip("Static tuning: maxLives / maxPatience / fadeDuration.")]
    [SerializeField] GameLoopConfig config;

    [Tooltip("Owns the cook/trash/deal mechanics; PrepareFood returns the resolution.")]
    [SerializeField] HandController handController;

    [Tooltip("Full-screen black overlay for the intro reveal and round-end cover.")]
    [SerializeField] FadeController fadeController;

    [Tooltip("HUD label showing lives (e.g. 'VIDAS: 3').")]
    [SerializeField] TMP_Text livesText;

    [Tooltip("HUD label showing enemy patience (e.g. 'PACIENCIA: 3').")]
    [SerializeField] TMP_Text patienceText;

    [Tooltip("HUD label showing the run's coins (e.g. 'MONEDAS: 15'). Null = no-op.")]
    [SerializeField] TMP_Text coinsText;

    [Tooltip("World-space player view (idle + one-shot anims). Null = no-op.")]
    [SerializeField] PlayerView playerView;

    [Tooltip("World-space enemy view (idle + reactions + phrase bubble). Null = no-op.")]
    [SerializeField] EnemyView enemyView;

    [Tooltip("Scene backdrop layer (SpriteRenderer). Null = layer skipped (wired in slice 2).")]
    [SerializeField] SpriteRenderer backdropRenderer;

    [Tooltip("Scene midground layer (SpriteRenderer). Null = layer skipped (wired in slice 2).")]
    [SerializeField] SpriteRenderer midgroundRenderer;

    /// <summary>
    /// How long win/game-over reactions stay visible before the round-end fade
    /// covers them (animations fire first, fade is delayed by this).
    /// </summary>
    const float ReactionShowDelay = 0.5f;

    int _lives;
    int _patience;
    bool _roundActive;

    /// <summary>Guards against loading the next scene twice (defense-in-depth: the fade overlay already blocks input).</summary>
    bool _loadingScene;

    /// <summary>The enemy identity: the current level's config (levelName + recipes = gustos).</summary>
    public LevelConfig Enemy => handController != null ? handController.CurrentLevel : null;

    /// <summary>
    /// Auto-start: the game begins by itself on scene load — the intro fade
    /// reveal + deal happens without any button. Runs in Start (not Awake) so
    /// FadeController.Awake has already forced the overlay opaque black first.
    /// </summary>
    void Start()
    {
        StartGame();
    }

    /// <summary>
    /// Seed the round state, refresh the HUD, then fade the black overlay out
    /// (reveal) and deal at the fade end.
    /// </summary>
    public void StartGame()
    {
        ApplyLevelArt();
        _roundActive = true;
        _lives = MaxLives();
        _patience = MaxPatience();
        UpdateHUD();
        FadeIn(() =>
        {
            if (handController != null) handController.StartDeal();
        });
    }

    /// <summary>
    /// Runtime art hook: pushes the current level's assignable art slots into
    /// the enemy view and the scene backdrop/midground layers. Real art replaces
    /// the placeholder; an empty slot keeps the tinted square with a per-level
    /// fallback color. All refs null-guarded — the layer renderers are wired in
    /// slice 2 and may be null right now.
    /// </summary>
    void ApplyLevelArt()
    {
        LevelConfig level = Enemy;
        if (level == null) return;
        if (enemyView != null) enemyView.Art = level.enemyArt;
        Color tint = FallbackTint(level);
        ApplyLayer(backdropRenderer, level.background, tint);
        ApplyLayer(midgroundRenderer, level.midground, Darkened(tint));
    }

    /// <summary>
    /// Assigns art to a scene layer, or tints the placeholder square when the
    /// slot is empty. Real art REPLACES the placeholder sprite and renders
    /// un-tinted; an empty slot keeps the scene's placeholder sprite (WhiteSprite)
    /// and only swaps the tint — never nulls the sprite.
    /// </summary>
    void ApplyLayer(SpriteRenderer layer, Sprite art, Color fallback)
    {
        if (layer == null) return;
        if (art != null)
        {
            layer.sprite = art;
            layer.color = Color.white;
        }
        else
        {
            layer.color = fallback;
        }
    }

    /// <summary>Same hue, darker band — keeps the midground readable as a distinct layer above the backdrop.</summary>
    Color Darkened(Color c) => new Color(c.r * 0.65f, c.g * 0.65f, c.b * 0.65f, 1f);

    /// <summary>Per-level placeholder tint: Mexico terracotta, Colombia green, Llorona blue-gray (default Mexico).</summary>
    Color FallbackTint(LevelConfig level)
    {
        if (level != null && level.regionName == "Colombia") return new Color(0.24f, 0.55f, 0.32f);
        if (level != null && !string.IsNullOrEmpty(level.levelName) && level.levelName.Contains("Llorona"))
            return new Color(0.42f, 0.48f, 0.56f);
        return new Color(0.72f, 0.40f, 0.28f);
    }

    /// <summary>
    /// PREPARAR: resolve the cook and apply the result mapping. No-ops (no
    /// punishment) when the round is inactive (fading / between rounds) or the
    /// cook queue is empty (PrepareFood returns null — nothing was cooked).
    /// </summary>
    public void ServeFood()
    {
        if (!_roundActive || handController == null) return;
        ResolutionResult? result = handController.PrepareFood();
        if (result == null) return;

        switch (result.Value.Kind)
        {
            // Green (Normal) / Gold (Star): the enemy is satisfied → round won.
            case RecipeKind.Normal:
            case RecipeKind.Star:
                // Dish + player + enemy reactions fire BEFORE the fade (D6):
                // the win is visible for ReactionShowDelay, then covered.
                handController?.LiveDish?.PlayReaction(result.Value.Kind);
                playerView?.OnWin();
                enemyView?.PlayReaction(EnemyReaction.Win, result.Value.Reaction);
                GrantCoins(result.Value.Kind);
                WinRound();
                break;

            // Blue (Presented) / Purple (Cursed) / Gray (Filler): benign retry —
            // patience drops; at 0 the enemy bites.
            case RecipeKind.Presented:
            case RecipeKind.Cursed:
            case RecipeKind.Filler:
                handController?.LiveDish?.PlayReaction(result.Value.Kind);
                playerView?.OnLosePatience();
                enemyView?.PlayReaction(EnemyReaction.Patience);
                _patience--;
                UpdateHUD();
                if (_patience <= 0) Bite();
                break;

            // Red (Fail, "Comida Cruda"): the only grave insult → bite.
            case RecipeKind.Fail:
                handController?.LiveDish?.PlayReaction(result.Value.Kind);
                playerView?.OnLoseLife();
                enemyView?.PlayReaction(EnemyReaction.Bite);
                Bite();
                break;
        }
    }

    /// <summary>
    /// Round won: stop input, cover to black, then load the next scene.
    /// </summary>
    void WinRound()
    {
        _roundActive = false;
        StartCoroutine(DelayedFade(ReactionShowDelay, () => LoadNextScene(true)));
    }

    /// <summary>
    /// Coin grant on a round win: Normal (green) +coinsNormal, Star (gold)
    /// +coinsStar, falling back to 10/15 when the config is unwired. Runs
    /// BEFORE the fade so the MONEDAS HUD shows the updated balance.
    /// </summary>
    void GrantCoins(RecipeKind kind)
    {
        GameSession.coins += kind == RecipeKind.Star
            ? (config != null ? config.coinsStar : 15)
            : (config != null ? config.coinsNormal : 10);
        UpdateHUD();
    }

    /// <summary>
    /// Enemy bite: −1 life, patience refills to max, HUD updated. At 0 lives →
    /// GAME OVER: the eat reaction fires BEFORE the fade, stop input, cover to
    /// black (delayed), then load the next scene.
    /// </summary>
    void Bite()
    {
        _lives--;
        _patience = MaxPatience();
        UpdateHUD();
        if (_lives <= 0)
        {
            _roundActive = false;
            playerView?.OnGameOver();
            enemyView?.PlayReaction(EnemyReaction.Eaten);
            StartCoroutine(DelayedFade(ReactionShowDelay, () => LoadNextScene(false)));
        }
    }

    /// <summary>
    /// Round end (runs behind the black cover): load the scene for the round
    /// outcome. WIN: advance the run to the next level (bounded — a last-level
    /// win loads Credits and NEVER writes an out-of-range index), then load the
    /// Map; LOSE: reload the SAME level (index unchanged, map position kept).
    /// Falls back to "Map"/"Prototype" when the config field is empty/null.
    /// The fresh scene instance AUTO-STARTS (Start → StartGame → FadeIn reveal
    /// → StartDeal), so no in-memory reset is needed.
    /// </summary>
    void LoadNextScene(bool won)
    {
        if (_loadingScene) return;
        _loadingScene = true;
        string sceneName;
        if (won)
        {
            int lastLevel = handController != null ? handController.LevelCount - 1 : 0;
            if (GameSession.currentLevelIndex < lastLevel)
            {
                GameSession.currentLevelIndex++; // never writes index 2
                sceneName = config != null && !string.IsNullOrEmpty(config.nextSceneName)
                    ? config.nextSceneName
                    : Scenes.Map;
            }
            else sceneName = Scenes.Credits; // last level won → credits, index stays
        }
        else
        {
            sceneName = config != null && !string.IsNullOrEmpty(config.nextSceneOnLose)
                ? config.nextSceneOnLose
                : Scenes.Prototype;
        }
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Lets a round-end reaction play for <paramref name="delay"/> seconds
    /// BEFORE the fade cover starts (win/game-over visibility, D6).
    /// </summary>
    IEnumerator DelayedFade(float delay, Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        FadeOut(onComplete);
    }

    /// <summary>Refreshes the VIDAS / PACIENCIA / MONEDAS HUD labels (no-op when unwired).</summary>
    void UpdateHUD()
    {
        if (livesText != null) livesText.text = $"VIDAS: {_lives}";
        if (patienceText != null) patienceText.text = $"PACIENCIA: {_patience}";
        if (coinsText != null) coinsText.text = $"MONEDAS: {GameSession.coins}";
    }

    void FadeIn(Action onComplete = null)
    {
        if (fadeController != null) fadeController.FadeTo(0f, GetFadeDuration(), onComplete);
        else onComplete?.Invoke();
    }

    void FadeOut(Action onComplete = null)
    {
        if (fadeController != null) fadeController.FadeTo(1f, GetFadeDuration(), onComplete);
        else onComplete?.Invoke();
    }

    int MaxLives() => config != null ? config.maxLives : 3;
    int MaxPatience() => config != null ? config.maxPatience : 3;
    float GetFadeDuration() => config != null ? config.fadeDuration : 1f;
}