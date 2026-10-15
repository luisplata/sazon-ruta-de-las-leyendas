using UnityEngine;

/// <summary>
/// Thin world-space wrapper over the Player Mecanim AnimatorController.
/// Each gameplay event maps 1:1 to an Animator trigger; the one-shot state
/// plays and auto-returns to Idle. Content-agnostic: an artist swaps clips in
/// the controller without touching this script.
/// Trigger names MUST match the Player.controller parameter names (case-sensitive).
/// </summary>
public class PlayerView : MonoBehaviour
{
    // Trigger names — MUST match Player.controller parameters exactly.
    const string GrabCard = "GrabCard";
    const string ParkCook = "ParkCook";
    const string ParkTrash = "ParkTrash";
    const string Cook = "Cook";
    const string Discard = "Discard";
    const string LosePatience = "LosePatience";
    const string LoseLife = "LoseLife";
    const string Win = "Win";
    const string GameOver = "GameOver";

    [Tooltip("Required. Null = all animation calls no-op.")]
    [SerializeField] Animator animator;

    [Tooltip("Optional placeholder tint.")]
    [SerializeField] SpriteRenderer sprite;

    [Tooltip("Optional real-art override. When set, replaces the placeholder square and bypasses the tint.")]
    [SerializeField] Sprite art;

    /// <summary>Runtime hook for the art override (set by GameLoopController from the level's art slot).</summary>
    public Sprite Art { get => art; set => art = value; }

    /// <summary>
    /// Runtime hook for the player visual (set by GameLoopController when the
    /// protagonist prefab is spawned). Mirrors <see cref="EnemyView.Animator"/>:
    /// null keeps the placeholder's authored animator.
    /// </summary>
    public Animator Animator
    {
        get => animator;
        set => animator = value;
    }

    [Tooltip("Blue-ish placeholder tint.")]
    [SerializeField] Color tint = new Color(0.45f, 0.65f, 0.95f);

    void Awake()
    {
        if (sprite == null) return;
        if (art != null)
        {
            sprite.sprite = art;
            sprite.color = Color.white; // real art renders un-tinted
        }
        else
        {
            sprite.color = tint;
        }
    }

    public void OnGrabCard() { Fire(GrabCard); }
    public void OnParkCook() { Fire(ParkCook); }
    public void OnParkTrash() { Fire(ParkTrash); }
    public void OnCook() { Fire(Cook); }
    public void OnDiscard() { Fire(Discard); }
    public void OnLosePatience() { Fire(LosePatience); }
    public void OnLoseLife() { Fire(LoseLife); }
    public void OnWin() { Fire(Win); }
    public void OnGameOver() { Fire(GameOver); }

    /// <summary>
    /// Hides the tinted placeholder square when a real protagonist prefab is
    /// spawned (its parts sort above the square, which would otherwise peek
    /// through). Safe to call when the sprite is unwired.
    /// </summary>
    public void HidePlaceholder()
    {
        if (sprite != null) sprite.enabled = false;
    }

    /// <summary>
    /// Hard-returns to Idle, guarding interrupted one-shots (called behind the
    /// round-end fade). Only plays when the animator actually has an "Idle"
    /// state, so a controller without one (e.g. Abue__Idle) does not log a
    /// missing-state warning.
    /// </summary>
    public void ResetToIdle()
    {
        if (animator != null && animator.HasState(0, Animator.StringToHash("Idle")))
            animator.Play("Idle", 0, 0f);
    }

    /// <summary>
    /// Plays a trigger ONLY when the current animator actually exposes a
    /// parameter of that name. SetTrigger on a missing parameter logs a warning
    /// on every call, so the pure <see cref="AnimParams.HasParam"/> guard keeps
    /// the console clean for controllers without these triggers (e.g.
    /// Abue.controller, which has zero parameters). A missing trigger is a
    /// silent no-op — the animator state is left unchanged.
    /// </summary>
    void Fire(string trigger)
    {
        if (animator == null) return;
        if (!AnimParams.HasParam(animator.parameters, trigger)) return;
        animator.SetTrigger(trigger);
    }
}
