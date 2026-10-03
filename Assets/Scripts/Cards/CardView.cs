using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One card in the hand. Owns its own movement (deal-in, re-layout,
/// return-to-slot) and discard animations; the discard coroutines live here
/// because the animation lifecycle belongs to the moving object. Drag uses
/// the UGUI drag handler family, whose pointerDrag capture keeps delivering
/// events to this card even as it moves under the pointer.
/// </summary>
public class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] RectTransform rect;
    [SerializeField] CanvasGroup group;
    [SerializeField] TMPro.TMP_Text nameText;
    [SerializeField] private Image border;
    [SerializeField] private Image icon;

    HandController _hand;
    CardVisualConfig _config;
    DropZone _center;
    DropZone _corner;
    int _slotIndex;
    PlayerView _playerView;
    bool _dragging;
    bool _discarding;
    bool _parked;
    DropZone _zone;
    Coroutine _moveRoutine;

    /// <summary>
    /// The data-driven identity assigned by SetData. Read by the cooking
    /// flow (HandController/RecipeResolver) to resolve what was cooked.
    /// </summary>
    public CardData Data { get; private set; }

    /// <summary>
    /// Called once by HandController at spawn. Caches refs, applies the
    /// configured card size and colors, and records the initial slot.
    /// </summary>
    public void Init(HandController hand, CardVisualConfig config, DropZone center, DropZone corner, int slotIndex,
        PlayerView playerView = null)
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (group == null) group = GetComponent<CanvasGroup>();

        _hand = hand;
        _config = config;
        _center = center;
        _corner = corner;
        _slotIndex = slotIndex;
        _playerView = playerView;
        if (border == null) border = GetComponent<Image>();
        if (icon == null) icon = GetComponent<Image>();

        rect.sizeDelta = config.cardSize;
        if (icon != null) icon.color = config.cardColor;
        if (nameText != null) nameText.color = config.cardTextColor;
    }

    /// <summary>
    /// Applies this card's data-driven identity: display name, emoji icon,
    /// and role color on the background. Called by HandController right
    /// after Init, before DealIn. Color comes ONLY from the role.
    /// </summary>
    public void SetData(CardData data)
    {
        Data = data;
        if (nameText != null) nameText.text = data.displayName;
        if (icon != null) icon.sprite = data.art;
        if (border != null) border.sprite = _config.GetBgSpriteForRole(data.role);
    }

    /// <summary>
    /// Animates this card from the deal origin to its assigned parabola slot.
    /// Targets the FINAL hand size (maxHandSize), not the partial count seen
    /// mid-deal: StartDeal/Refill add cards one at a time, so _hand.Count is
    /// still growing when this runs — using it would pile later cards at the
    /// parabola end instead of fanning them (see spec: fan without overlap).
    /// </summary>
    public void DealIn()
    {
        StopMove();
        _moveRoutine = StartCoroutine(
            MoveTo(_config.dealOrigin,
                _hand.GetSlotPosition(_slotIndex, _config.maxHandSize),
                _config.dealDuration,
                _config.dealEasingCurve));
    }

    /// <summary>
    /// Reassigns the slot index and animates to the new parabola position
    /// (used by dynamic re-layout after a card leaves the hand, and by Refill
    /// to slide pre-existing cards onto the full fan). No-op only when the
    /// target position is already reached — the same index can map to a
    /// different position when the hand size changes (Refill).
    /// </summary>
    public void SetSlot(int index)
    {
        if (_parked) return; // committed card — the hand no longer moves it
        Vector2 target = _hand.GetSlotPosition(index, _hand.Count);
        if (index == _slotIndex && (rect.anchoredPosition - target).sqrMagnitude < 0.01f) return;
        _slotIndex = index;
        // Mid-drag: keep the index (so ReturnToSlot lands on the right slot)
        // but defer the animation until release. Mid-discard: the card is
        // leaving the hand and its discard animation owns the transform, so a
        // re-layout must never start a competing move on it.
        if (_dragging || _discarding) return;
        StopMove();
        _moveRoutine = StartCoroutine(
            MoveTo(rect.anchoredPosition, target,
                _config.reLayoutDuration,
                _config.reLayoutEasingCurve));
    }

    /// <summary>
    /// Animates back to the current slot (drop outside all zones; the hand
    /// is unchanged per spec).
    /// </summary>
    public void ReturnToSlot()
    {
        if (_parked || _discarding) return;
        StopMove();
        _moveRoutine = StartCoroutine(
            MoveTo(rect.anchoredPosition,
                _hand.GetSlotPosition(_slotIndex, _hand.Count),
                _config.reLayoutDuration,
                _config.reLayoutEasingCurve));
    }

    /// <summary>
    /// Parks this card into <paramref name="zone"/>: animates from its current
    /// (hand-space) position to the zone's next parked slot while shrinking to
    /// the parked scale, then re-parents under the zone and locks the card
    /// (blocksRaycasts=false — parked cards are committed, never retrievable).
    /// </summary>
    public void PlaceInZone(DropZone zone)
    {
        _parked = true;
        _zone = zone;
        StopMove();
        _moveRoutine = StartCoroutine(ParkTo(zone));
    }

    /// <summary>
    /// Plays the consume animation for this card's zone (reuses the discard
    /// coroutines; the card is already parked under the zone, so the center
    /// effect converges on the zone-local origin). Destroys on completion.
    /// </summary>
    public void ConsumeFromZone()
    {
        _discarding = true;
        StopMove();
        group.blocksRaycasts = false;
        if (_zone != null && _zone.Kind == DropZone.ZoneKind.Center) StartCoroutine(DiscardCenter());
        else StartCoroutine(DiscardCorner());
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (_discarding || _parked) return;
        // Any destination drag starts the grab reaction (design D1: grab fires
        // once at drag begin, regardless of where it ends).
        _playerView?.OnGrabCard();
        // Kill any in-flight move (deal-in / re-layout / return) BEFORE dragging:
        // the coroutine writes anchoredPosition every frame and would otherwise
        // fight OnDrag, keeping the card glued to the hand arc and making the
        // release animation start from the arc (the visible "pop").
        StopMove();
        _dragging = true;
        rect.localScale = Vector3.one * _config.dragLiftScale;
        // pointerDrag already captured this card; unblocking raycasts is safe
        // and keeps other UI (buttons) clickable while dragging.
        group.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (_discarding || _parked) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_hand.HandRoot, e.position, null, out var local))
            rect.anchoredPosition = local + _config.dragLiftOffset;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_discarding || _parked) return;
        _dragging = false;
        rect.localScale = Vector3.one;
        group.blocksRaycasts = true;

        // Zone check by pointer position on release: the dragged card is under
        // the pointer and would block zone raycasts (see DropZone). A zone
        // with space accepts the park; a full zone rejects and the card
        // returns to its slot. Dropping outside all zones returns too.
        if (_center != null && _center.Contains(e.position))
        {
            if (!_hand.TryParkInZone(this, _center)) ReturnToSlot();
            return;
        }

        if (_corner != null && _corner.Contains(e.position))
        {
            if (!_hand.TryParkInZone(this, _corner)) ReturnToSlot();
            return;
        }

        ReturnToSlot();
    }

    /// <summary>
    /// Park animation: move (hand space -> zone slot) and scale (1 -> parked)
    /// eased over parkDuration; on completion re-parent under the zone so the
    /// card's coordinates become zone-local, then lock it.
    /// </summary>
    IEnumerator ParkTo(DropZone zone)
    {
        Vector2 startPos = rect.anchoredPosition;
        Vector2 slot = zone.GetParkedSlot(zone.Count - 1);
        Vector2 target = zone.Rect.anchoredPosition + slot;
        float duration = _config.parkDuration;

        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.0001f))
        {
            float p = Mathf.Clamp01(t);
            rect.anchoredPosition = Vector2.Lerp(startPos, target, _config.parkEasingCurve.Evaluate(p));
            rect.localScale = Vector3.one * Mathf.Lerp(1f, _config.parkedScale, _config.parkEasingCurve.Evaluate(p));
            yield return null;
        }

        rect.anchoredPosition = target;
        rect.localScale = Vector3.one * _config.parkedScale;

        transform.SetParent(zone.Rect, true);
        rect.anchoredPosition = slot;
        group.blocksRaycasts = false;
    }

    /// <summary>
    /// Center consume: move to the zone-local center (0,0 — the card is already
    /// re-parented under the zone) and scale from the parked scale to 0, both
    /// eased over the configured duration. On completion: release + destroy.
    /// </summary>
    IEnumerator DiscardCenter()
    {
        Vector2 startPos = rect.anchoredPosition;
        float startScale = rect.localScale.x; // parkedScale — no scale pop
        Vector2 target = Vector2.zero;
        float duration = _config.centerDiscardDuration;

        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.0001f))
        {
            float p = Mathf.Clamp01(t);
            rect.anchoredPosition = Vector2.Lerp(startPos, target, _config.centerDiscardMoveCurve.Evaluate(p));
            rect.localScale = Vector3.one * (startScale * _config.centerDiscardScaleCurve.Evaluate(p));
            yield return null;
        }

        rect.anchoredPosition = target;
        rect.localScale = Vector3.zero;

        _zone.Release(this);
        Destroy(gameObject);
    }

    /// <summary>
    /// Corner consume: DIFFERENT effect — rotation speeds up slow -> fast
    /// (angular velocity integrated from the curve, clockwise) while the
    /// scale eases from the parked scale to 0, over the configured duration.
    /// Then release + destroy.
    /// </summary>
    IEnumerator DiscardCorner()
    {
        float startScale = rect.localScale.x; // parkedScale — no scale pop
        float duration = _config.cornerDiscardDuration;

        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.0001f))
        {
            float p = Mathf.Clamp01(t);
            float speed = _config.cornerRotationSpeedCurve.Evaluate(p);
            rect.localRotation *= Quaternion.Euler(0f, 0f, -speed * Time.deltaTime);
            rect.localScale = Vector3.one * (startScale * _config.cornerDiscardScaleCurve.Evaluate(p));
            yield return null;
        }

        rect.localScale = Vector3.zero;

        _zone.Release(this);
        Destroy(gameObject);
    }

    /// <summary>Generic eased move of anchoredPosition from start to target.</summary>
    IEnumerator MoveTo(Vector2 start, Vector2 target, float duration, AnimationCurve easing)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.0001f))
        {
            rect.anchoredPosition = Vector2.Lerp(start, target, easing.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }

        rect.anchoredPosition = target;
    }

    void StopMove()
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }
    }
}