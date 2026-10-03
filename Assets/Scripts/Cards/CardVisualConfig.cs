using UnityEngine;

/// <summary>
/// All tunable visual parameters for the card-game visual base: hand size,
/// deal origin, parabola shape, re-layout, drag feel, discard animations,
/// colors, and the card prefab reference. Values are in canvas units (px)
/// for a 1920×1080 reference resolution (CanvasScaler ScaleWithScreenSize,
/// match 0.5); all X positions stay within ±320 to remain inside the
/// CameraViewportFitter's guaranteed-visible 16:9 band at 21:9.
/// </summary>
[CreateAssetMenu(fileName = "CardVisualConfig", menuName = "Cards/Card Visual Config")]
public class CardVisualConfig : ScriptableObject
{
    [Header("Hand")] [Tooltip("Maximum cards the hand can hold. No card is added at capacity.")]
    public int maxHandSize = 5;

    [Header("Card")] [Tooltip("Card size in canvas px (pivot 0.5).")]
    public Vector2 cardSize = new Vector2(140f, 200f);

    [Header("Deal")] [Tooltip("Spawn point in canvas px (bottom-right corner).")]
    public Vector2 dealOrigin = new Vector2(280f, -380f);

    [Tooltip("Seconds for a card to travel from dealOrigin to its slot.")]
    public float dealDuration = 0.4f;

    [Tooltip("Easing for the deal-in move (normalized t -> progress 0..1).")]
    public AnimationCurve dealEasingCurve = EaseOut01();

    [Header("Parabola (quadratic Bezier)")] [Tooltip("Left end of the fan.")]
    public Vector2 parabolaStart = new Vector2(-280f, -300f);

    [Tooltip("Apex; pulls the fan up/down (height).")]
    public Vector2 parabolaControl = new Vector2(0f, -150f);

    [Tooltip("Right end of the fan (spread = Start <-> End).")]
    public Vector2 parabolaEnd = new Vector2(280f, -300f);

    [Header("Re-layout")] [Tooltip("Seconds for remaining cards to settle after a discard.")]
    public float reLayoutDuration = 0.25f;

    [Tooltip("Easing for the re-layout move.")]
    public AnimationCurve reLayoutEasingCurve = EaseInOut01();

    [Header("Zones (parking)")] [Tooltip("Maximum cards the cook zone (center) can hold as a visible queue.")]
    public int maxCook = 3;

    [Tooltip(
        "Corner trash zone capacity (max parked cards at once). The per-game scarce resource is REFILLS (maxRefillsPerGame), not ingredients — parking is gated only by this capacity.")]
    public int maxTrashPerGame = 2;

    [Tooltip(
        "Maximum number of times the trash zone can be DUMPED (REFILL button uses) per game. Parking is unlimited (capacity-gated only); the refill action itself is the limited valve. Empty-trash REFILL does not consume one.")]
    public int maxRefillsPerGame = 2;

    [Tooltip("Scale of parked cards inside zones (both cook and trash).")]
    public float parkedScale = 0.55f;

    [Tooltip("Horizontal spacing in canvas px between parked-card slots.")]
    public float parkedSpacing = 80f;

    [Tooltip("Seconds for a card to park into its zone slot.")]
    public float parkDuration = 0.3f;

    [Tooltip("Easing for the park move (position and scale).")]
    public AnimationCurve parkEasingCurve = EaseInOut01();

    [Header("Drag")] [Tooltip("Scale multiplier while a card is dragged.")]
    public float dragLiftScale = 1.12f;

    [Tooltip("Offset in canvas px applied above the pointer while dragging.")]
    public Vector2 dragLiftOffset = new Vector2(0f, 40f);

    [Header("Center discard")] [Tooltip("Seconds for the center-zone discard (move + scale to 0).")]
    public float centerDiscardDuration = 1.0f;

    [Tooltip("Easing of position toward the zone center (y: 0..1 progress).")]
    public AnimationCurve centerDiscardMoveCurve = EaseInOut01();

    [Tooltip("Scale over time, 1 -> 0.")] public AnimationCurve centerDiscardScaleCurve = ScaleDown01();

    [Header("Corner discard")] [Tooltip("Seconds for the corner-zone discard (spin + scale to 0).")]
    public float cornerDiscardDuration = 1.0f;

    [Tooltip("Rotation speed in deg/s vs normalized t: linear 120 -> 1080 (~600 deg total).")]
    public AnimationCurve cornerRotationSpeedCurve = RotationSpeedCurve();

    [Tooltip("Scale over time, 1 -> 0.")] public AnimationCurve cornerDiscardScaleCurve = ScaleDown01();

    [Header("Colors")] public Color cardColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    public Color cardTextColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    public Color centerZoneColor = new Color(0.3f, 0.7f, 1.0f, 0.25f);
    public Color cornerZoneColor = new Color(1.0f, 0.4f, 0.3f, 0.25f);

    [Header("Role colors")] [Tooltip("Card background color for Base cards (Rojo).")]
    public Color baseColor = new Color(0.85f, 0.2f, 0.2f, 1f);

    [Tooltip("Card background color for Complemento cards (Verde).")]
    public Color complementoColor = new Color(0.2f, 0.7f, 0.3f, 1f);

    [Tooltip("Card background color for Sazon cards (Amarillo).")]
    public Color sazonColor = new Color(0.95f, 0.8f, 0.15f, 1f);

    [Header("Card prefab")] [Tooltip("Prefab instantiated by the hand. Assigned in the config asset.")]
    public GameObject cardPrefab;

    public Sprite bgSpriteFotRoleBase;
    public Sprite bgSpriteFotRoleComplemento;
    public Sprite bgSpriteFotRoleSazon;

    /// <summary>Ease-out 0 -> 1: fast start, decelerating to flat at 1.</summary>
    static AnimationCurve EaseOut01()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(1f, 1f, 0f, 0f));
    }

    /// <summary>Ease-in-out 0 -> 1 (S-curve).</summary>
    static AnimationCurve EaseInOut01()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f));
    }

    /// <summary>Scale-down 1 -> 0 (ease-in-out).</summary>
    static AnimationCurve ScaleDown01()
    {
        return new AnimationCurve(
            new Keyframe(0f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, 0f, 0f));
    }

    /// <summary>
    /// Corner rotation speed curve: linear from 120 to 1080 deg/s, so the
    /// spin accelerates slow -> fast and totals ~600 degrees over 1 second.
    /// </summary>
    static AnimationCurve RotationSpeedCurve()
    {
        const float slope = 1080f - 120f;
        return new AnimationCurve(
            new Keyframe(0f, 120f, slope, slope),
            new Keyframe(1f, 1080f, slope, slope));
    }

    /// <summary>
    /// Card background color for a role: Base = red, Complemento = green,
    /// Sazon = yellow. Card color comes ONLY from the role.
    /// </summary>
    public Color GetRoleColor(CardRole role)
    {
        switch (role)
        {
            case CardRole.Base: return baseColor;
            case CardRole.Complemento: return complementoColor;
            default: return sazonColor;
        }
    }

    public Sprite GetBgSpriteForRole(CardRole dataRole)
    {
        switch (dataRole)
        {
            case CardRole.Base: return bgSpriteFotRoleBase;
            case CardRole.Complemento: return bgSpriteFotRoleComplemento;
            default: return bgSpriteFotRoleSazon;
        }
    }
}