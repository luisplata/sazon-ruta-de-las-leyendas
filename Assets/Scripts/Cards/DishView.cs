using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal static result view for a resolved dish: a background Image tinted
/// by the dish kind color, a TMP dish name, and a TMP icon. Deliberately NOT
/// draggable and free of CardView's drag/move state machine — the cooking
/// result is display-only.
///
/// On serve, PlayReaction plays a per-RecipeKind placeholder animation
/// (scale/rotation/position/color lerp on the Image + RectTransform, CardView
/// coroutine style). Content-agnostic: an artist swaps the feel per kind
/// without touching the game loop. Base color/scale are cached at start and
/// restored on completion.
/// </summary>
public class DishView : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text iconText;

    /// <summary>Optional dish-art sprite slot; sits ABOVE the kind-tinted bg, below the name.</summary>
    [SerializeField] Image artImage;

    /// <summary>Card-frame Image (same visual role as CardView.border): tinted by the dish kind color.</summary>
    [SerializeField] Image border;

    [Header("Frame per result kind (one slot per color; same sprite for now)")]
    [SerializeField] Sprite frameStar;
    [SerializeField] Sprite frameNormal;
    [SerializeField] Sprite frameCursed;
    [SerializeField] Sprite frameFiller;
    [SerializeField] Sprite frameFail;
    [SerializeField] Sprite framePresented;

    /// <summary>
    /// Applies the resolved dish's identity and kind color. When art is
    /// present (and the artImage slot is wired) the sprite shows instead of
    /// the emoji icon; a null/unwired art falls back to the emoji.
    /// </summary>
    public void SetDish(string dishName, string icon, Color color, Sprite art = null, RecipeKind kind = RecipeKind.Normal)
    {
        if (image != null) image.color = color;
        if (border != null)
        {
            border.sprite = FrameForKind(kind);
            border.color = color;
        }
        if (nameText != null) nameText.text = dishName;

        if (artImage != null)
        {
            artImage.sprite = art;
            artImage.preserveAspect = true;
            artImage.enabled = art != null;
        }

        // Emoji is the fallback: shown when there is no art, or when the art
        // slot is unwired (artImage == null) so a partially wired prefab still
        // displays something.
        bool showIcon = art == null || artImage == null;
        if (iconText != null)
        {
            iconText.text = icon;
            iconText.enabled = showIcon;
        }
    }

    /// <summary>Frame sprite for a result kind (per-color slots; same sprite for now).</summary>
    Sprite FrameForKind(RecipeKind kind)
    {
        switch (kind)
        {
            case RecipeKind.Star: return frameStar;
            case RecipeKind.Cursed: return frameCursed;
            case RecipeKind.Filler: return frameFiller;
            case RecipeKind.Fail: return frameFail;
            case RecipeKind.Presented: return framePresented;
            default: return frameNormal;
        }
    }

    /// <summary>
    /// Plays the per-kind serve reaction (placeholder). Null-guards: unwired
    /// Image/rect no-op. Kills any in-flight reaction before starting.
    /// </summary>
    public void PlayReaction(RecipeKind kind)
    {
        if (image == null || rect == null) return;
        StopAllCoroutines();
        StartCoroutine(ReactRoutine(kind));
    }

    IEnumerator ReactRoutine(RecipeKind kind)
    {
        // Cache base state ONCE — restored at the end so the card returns to
        // its identity look regardless of which reaction ran.
        Vector3 baseScale = rect.localScale;
        Vector3 baseEuler = rect.localEulerAngles;
        Vector2 basePos = rect.anchoredPosition;
        Color baseColor = image.color;
        const float duration = 0.6f;

        switch (kind)
        {
            // Win kinds: elastic scale punch (Star stronger + gold flash).
            case RecipeKind.Star:
                yield return LerpLoop(duration, t => {
                    rect.localScale = baseScale * ElasticPunch(t, 1.35f);
                    image.color = Color.Lerp(baseColor, Gold, Flash(t));
                });
                break;

            case RecipeKind.Normal:
                yield return LerpLoop(duration, t => {
                    rect.localScale = baseScale * ElasticPunch(t, 1.2f);
                });
                break;

            // Cursed: z-rotation wiggle ±12°.
            case RecipeKind.Cursed:
                yield return LerpLoop(duration, t => {
                    float angle = Mathf.Sin(t * Mathf.PI * 4f) * 12f * (1f - t);
                    rect.localEulerAngles = baseEuler + new Vector3(0f, 0f, angle);
                });
                break;

            // Filler: gentle pulse 1 → 1.08.
            case RecipeKind.Filler:
                yield return LerpLoop(duration, t => {
                    rect.localScale = baseScale * (1f + 0.08f * Mathf.Sin(t * Mathf.PI));
                });
                break;

            // Fail: x-shake ±0.06 + red flash.
            case RecipeKind.Fail:
                yield return LerpLoop(duration, t => {
                    float shake = Mathf.Sin(t * Mathf.PI * 8f) * 0.06f * (1f - t);
                    rect.anchoredPosition = basePos + new Vector2(shake, 0f);
                    image.color = Color.Lerp(baseColor, Red, Flash(t));
                });
                break;

            // Presented: grow 1 → 1.15 + blue flash.
            case RecipeKind.Presented:
                yield return LerpLoop(duration, t => {
                    rect.localScale = baseScale * Mathf.Lerp(1f, 1.15f, t);
                    image.color = Color.Lerp(baseColor, Blue, Flash(t));
                });
                break;
        }

        // Restore base state exactly (guards partial/early interruption).
        rect.localScale = baseScale;
        rect.localEulerAngles = baseEuler;
        rect.anchoredPosition = basePos;
        image.color = baseColor;
    }

    /// <summary>
    /// CardView-style lerp loop: advances t by real time over <paramref name="duration"/>
    /// and invokes <paramref name="apply"/> each frame with the clamped progress.
    /// </summary>
    IEnumerator LerpLoop(float duration, System.Action<float> apply)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.0001f))
        {
            apply(Mathf.Clamp01(t));
            yield return null;
        }
    }

    /// <summary>Elastic punch: 1 → punch (overshoot) → back to 1 over progress.</summary>
    static float ElasticPunch(float t, float punch)
    {
        // Rise to the peak on the first half, ease back down on the second.
        float peak = 1f + (punch - 1f) * Mathf.Sin(t * Mathf.PI);
        return peak;
    }

    /// <summary>Flash envelope: quick in, slow out (1 at start → 0 at end).</summary>
    static float Flash(float t) => Mathf.Pow(1f - t, 2f);

    static readonly Color Gold = new Color(1f, 0.84f, 0f);
    static readonly Color Red = new Color(0.85f, 0.2f, 0.2f);
    static readonly Color Blue = new Color(0.25f, 0.55f, 0.95f);

    RectTransform rect
    {
        get
        {
            if (_rect == null) _rect = (RectTransform)transform;
            return _rect;
        }
    }
    RectTransform _rect;
}