using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>Outcome category mapped 1:1 to Enemy.controller trigger parameters.</summary>
public enum EnemyReaction { Win, Patience, Bite, Eaten }

/// <summary>
/// World-space enemy view: Animator wrapper (Idle + one-shot reactions) plus a
/// phrase system with a world-space TMP bubble. Content-agnostic — an artist
/// swaps clips in the controller without touching this script.
/// Trigger names MUST match the Enemy.controller parameter names (case-sensitive).
/// </summary>
public class EnemyView : MonoBehaviour
{
    [Tooltip("Required. Null = animation calls no-op.")]
    [SerializeField] Animator animator;

    [Tooltip("Optional placeholder tint.")]
    [SerializeField] SpriteRenderer sprite;

    [Tooltip("Optional real-art override. When set, replaces the placeholder square and bypasses the tint.")]
    [SerializeField] Sprite art;

    /// <summary>
    /// Runtime hook for the art override (set by GameLoopController from the
    /// level's art slot). Assigning re-renders immediately: real art replaces
    /// the placeholder square and bypasses the tint; null keeps the tint.
    /// </summary>
    public Sprite Art
    {
        get => art;
        set { art = value; ApplyArt(); }
    }

    [Tooltip("Red-ish placeholder tint.")]
    [SerializeField] Color tint = new Color(0.95f, 0.5f, 0.45f);

    [Tooltip("World-space bubble root (Canvas), inactive by default.")]
    [SerializeField] GameObject bubble;

    [Tooltip("TMP text inside the bubble.")]
    [SerializeField] TMP_Text bubbleText;

    [Tooltip("How long the bubble stays visible before hiding.")]
    [SerializeField] float bubbleHideDelay = 2f;

    [Header("Phrase pools")]
    [SerializeField] string[] winPhrases;
    [SerializeField] string[] patiencePhrases;
    [SerializeField] string[] bitePhrases;
    [SerializeField] string[] eatenPhrases;

    void Awake()
    {
        ApplyArt();
    }

    /// <summary>
    /// Renders the current art state: real art replaces the placeholder square
    /// and renders un-tinted; an empty slot keeps the tinted placeholder.
    /// Called from Awake and whenever Art is assigned at runtime.
    /// </summary>
    void ApplyArt()
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

    /// <summary>Fires the reaction trigger and shows a phrase bubble (win prefers the recipe reactionText).</summary>
    public void PlayReaction(EnemyReaction reaction, string preferredPhrase = null)
    {
        if (animator != null) animator.SetTrigger(reaction.ToString());
        ShowBubble(Pick(reaction, preferredPhrase));
    }

    /// <summary>Bubble only, no animation trigger.</summary>
    public void SayPhrase(EnemyReaction category)
    {
        ShowBubble(Pick(category, null));
    }

    /// <summary>
    /// Scripted narration: shows <paramref name="phrase"/> in the bubble without
    /// any animation trigger or phrase-pool pick (design D2). Used by the
    /// tutorial's step ladder for exact, ordered dialogue.
    /// </summary>
    public void Say(string phrase)
    {
        ShowBubble(phrase);
    }

    /// <summary>Hard-returns to Idle, guarding interrupted one-shots (called behind the round-end fade).</summary>
    public void ResetToIdle()
    {
        if (animator != null) animator.Play("Idle", 0, 0f);
    }

    string Pick(EnemyReaction r, string pref)
    {
        if (r == EnemyReaction.Win && !string.IsNullOrWhiteSpace(pref)) return pref;
        string[] pool = r switch
        {
            EnemyReaction.Win => winPhrases,
            EnemyReaction.Patience => patiencePhrases,
            EnemyReaction.Bite => bitePhrases,
            _ => eatenPhrases
        };
        return pool != null && pool.Length > 0 ? pool[Random.Range(0, pool.Length)] : null;
    }

    void ShowBubble(string phrase)
    {
        if (bubble == null || bubbleText == null || string.IsNullOrWhiteSpace(phrase))
        {
            if (bubble != null) bubble.SetActive(false);
            return;
        }
        bubbleText.text = phrase;
        bubble.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideAfter(bubbleHideDelay));
    }

    IEnumerator HideAfter(float d)
    {
        yield return new WaitForSeconds(d);
        if (bubble != null) bubble.SetActive(false);
    }
}
