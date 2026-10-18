using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sole owner of the hand state. Spawns cards from the deal origin onto a
/// parabola (quadratic Bezier), re-layouts the remaining cards whenever one
/// is parked (removed from the hand synchronously at drop-accept), and owns
/// the game flow: PrepareFood consumes the cook queue, Refill discards the
/// WHOLE hand (cards fly left to the discard target, then a full hand is
/// re-dealt from the draw cursor) and also flushes any parked trash for free,
/// StartDeal resets zones + refill counter. The hand-cycle REFILL is the
/// scarce valve, not ingredients.
/// </summary>
public class HandController : MonoBehaviour
{
    [SerializeField] CardVisualConfig config;
    [SerializeField] DropZone centerZone;
    [SerializeField] DropZone cornerZone;
    [SerializeField] RectTransform handRoot;
    [SerializeField] DeckData deck;
    [SerializeField] TMPro.TMP_Text refillLabel;

    [Tooltip("World-space player view; park/cook/discard triggers. Null = no-op.")]
    [SerializeField] PlayerView playerView;

    /// <summary>Current level's recipe config; loaded dynamically so the same scene serves both levels.</summary>
    [SerializeField] LevelConfig currentLevel;

    /// <summary>Global catalog of all levels; the resolver matches against it so cross-level recipes surface as Presented (blue).</summary>
    [SerializeField] RecipeDatabase recipeDatabase;

    /// <summary>Result-card prefab spawned on a successful 3-card cook (kind-colored, not draggable).</summary>
    [SerializeField] DishView dishCardPrefab;

    /// <summary>Slot (right of center) the dish result card is parented to.</summary>
    [SerializeField] RectTransform dishRoot;

    /// <summary>Live dish result card, destroyed on the next cook and on START.</summary>
    DishView _dishCard;

    readonly List<CardView> _cards = new();

    /// <summary>
    /// Materialized, shuffled draw order for this deal. Built ONLY in StartDeal
    /// (deck.Expand() then Fisher-Yates). Refill/TopUp continue reading from it
    /// with a wrap-around cursor — never re-shuffled mid-game.
    /// </summary>
    List<CardData> _drawOrder;

    /// <summary>
    /// Next position in _drawOrder. Reset to 0 ONLY in StartDeal (after the
    /// shuffle); Refill/TopUp reuse the cursor so they continue the sequence
    /// instead of re-dealing.
    /// </summary>
    int _dealIndex;

    /// <summary>
    /// Hand-cycle (REFILL) uses THIS GAME. Incremented ONLY when Refill()
    /// actually discards at least one hand card; an empty-hand refill does NOT
    /// consume one. Reset to 0 in StartDeal. Gates Refill() itself; the corner
    /// park is NOT gated (the corner flush is free, so a parked card can never
    /// get stranded).
    /// </summary>
    int _refillsUsed;

    /// <summary>True while a RefillRoutine is running; further presses are ignored.</summary>
    bool _refilling;

    /// <summary>Live refill coroutine, stopped by StartDeal so a restart never deals a partial hand.</summary>
    Coroutine _refillRoutine;

    /// <summary>Number of cards currently in the hand (parked cards excluded).</summary>
    public int Count => _cards.Count;

    /// <summary>Root that spawned cards are parented to (drag coordinate space).</summary>
    public RectTransform HandRoot => handRoot;

    /// <summary>The current level's config — the enemy identity for the game loop (levelName + recipes).</summary>
    public LevelConfig CurrentLevel => currentLevel;

    /// <summary>Number of levels in the recipe database (last-level check for the game-flow win routing).</summary>
    public int LevelCount => recipeDatabase != null ? recipeDatabase.levels.Count : 0;

    /// <summary>
    /// The live dish result card (null between cooks). Exposed so the game loop
    /// can trigger the per-kind serve reaction AFTER resolution, when the dish
    /// card already exists in the scene.
    /// </summary>
    public DishView LiveDish => _dishCard;

    /// <summary>
    /// Kind of the last resolved cook (null until the first cook). Exposed so
    /// the tutorial can observe a Presented (blue) resolution to advance its
    /// patience-teaching step — the same global-observer pattern as its zone
    /// tracking.
    /// </summary>
    public RecipeKind? LastResolutionKind { get; private set; }

    /// <summary>
    /// START: clears both zone queues and the refill counter, aborts any
    /// in-flight refill, destroys all existing hand cards, then deals a full
    /// hand (each card appears at the deal origin and animates to its parabola
    /// slot).
    /// </summary>
    public void StartDeal()
    {
        // A mid-refill restart must not deal a partial hand later: kill the
        // routine and drop the guard BEFORE the reset below.
        if (_refillRoutine != null)
        {
            StopCoroutine(_refillRoutine);
            _refillRoutine = null;
        }
        _refilling = false;

        // Shuffle happens HERE and only here: expand the parametrized deck, mix
        // in the run's Market purchases, and Fisher-Yates it into the draw
        // order. Seeded (deck.RandomSeed >= 0) for reproducible debugging,
        // unseeded otherwise. purchasedCards are appended BEFORE the shuffle so
        // they mix into the draw order like any other card.
        _drawOrder = deck != null ? deck.Expand() : null;
        if (_drawOrder != null
            && GameSession.purchasedCards != null
            && GameSession.purchasedCards.Count > 0)
            _drawOrder.AddRange(GameSession.purchasedCards);
        if (_drawOrder != null) ShuffleFisherYates(_drawOrder);

        _dealIndex = 0;
        _refillsUsed = 0;
        ClearZone(centerZone);
        ClearZone(cornerZone);
        ClearDish();
        UpdateRefillLabel();

        for (int i = _cards.Count - 1; i >= 0; i--)
        {
            if (_cards[i] != null) Destroy(_cards[i].gameObject);
        }
        _cards.Clear();

        for (int i = 0; i < config.maxHandSize; i++)
            DealOne(i);
    }

    /// <summary>
    /// REFILL: the limited valve — at most maxRefillsPerGame hand-cycle uses
    /// per game. Ignores presses while a refill is already running (so a
    /// double-click can never deal two hands). See <see cref="RefillRoutine"/>
    /// for the sequence.
    /// </summary>
    public void Refill()
    {
        if (_refilling) return;
        _refilling = true;
        _refillRoutine = StartCoroutine(RefillRoutine());
    }

    /// <summary>
    /// The REFILL sequence: (1) flush any parked corner/trash cards — free and
    /// unconditional, so it never charges a use and never blocks; (2) if the
    /// hand is non-empty and budget remains, charge a use, clear the hand
    /// BEFORE flying its cards LEFT to the discard target, wait out the fly,
    /// then re-deal maxHandSize cards from the existing draw cursor (no
    /// re-shuffle); an empty hand just tops up for free. Pot/center cards are
    /// never touched. Clears the re-entrancy guard last.
    /// </summary>
    IEnumerator RefillRoutine()
    {
        if (cornerZone.Count > 0)
        {
            playerView?.OnDiscard();
            ConsumeZone(cornerZone);
        }

        if (_cards.Count > 0 && _refillsUsed < config.maxRefillsPerGame)
        {
            _refillsUsed++;
            UpdateRefillLabel();
            playerView?.OnDiscard();

            // Clear the hand BEFORE the fly: Count/re-layout must never see the
            // departing cards, and the new hand must not mix with them.
            var discarding = new List<CardView>(_cards);
            _cards.Clear();
            foreach (var card in discarding)
                if (card != null) card.FlyLeftTo(config.discardTargetPosition);

            // Wait out the fly, plus one frame for the fly loop's exit, before
            // dealing — so no re-layout fights the departing cards.
            yield return new WaitForSeconds(config.cornerDiscardDuration);
            yield return null;

            for (int i = 0; i < config.maxHandSize; i++)
                DealOne(i);
        }
        else if (_cards.Count == 0)
        {
            TopUpHand();
        }

        _refilling = false;
        _refillRoutine = null;
    }

    /// <summary>
    /// PREPARAR COMIDA: returns null with an empty cook queue (confirmed
    /// behavior change — nothing cooked → the game loop no-ops without
    /// punishment). Otherwise gathers the cooked CardData from the center zone
    /// BEFORE it is consumed (consume destroys the cards), clears the previous
    /// dish, resolves the cook against the current level's config plus the
    /// global catalog (cross-level exact matches surface as Presented/blue),
    /// spawns the dish result card right of center, then consumes the cook
    /// queue and tops the hand up. The trash queue is NOT touched — it is only
    /// flushed by REFILL. The returned result lets the game loop apply the
    /// life/patience mapping.
    /// </summary>
    public ResolutionResult? PrepareFood()
    {
        if (centerZone.Count == 0) return null;

        var cooked = new List<CardData>(centerZone.Count);
        foreach (var card in centerZone.Held)
            if (card != null && card.Data != null) cooked.Add(card.Data);

        ClearDish();
        var result = RecipeResolver.Resolve(cooked, currentLevel, recipeDatabase);
        LastResolutionKind = result.Kind;
        SpawnDish(result);
        playerView?.OnCook();

        ConsumeZone(centerZone);
        TopUpHand();
        return result;
    }

    /// <summary>
    /// Spawns the dish result card under dishRoot (kind-colored by the
    /// resolution result). No-op when the prefab or slot is unwired, so an
    /// unwired scene still cooks normally (display-only feature).
    /// </summary>
    void SpawnDish(ResolutionResult result)
    {
        if (dishCardPrefab == null || dishRoot == null) return;
        var view = Instantiate(dishCardPrefab, dishRoot);
        var rt = (RectTransform)view.transform;
        rt.anchoredPosition = Vector2.zero;
        view.SetDish(result.DishName, result.Icon, result.Color, result.Art, result.Kind);
        _dishCard = view;
    }

    /// <summary>Destroys the live dish result card (next cook and START).</summary>
    void ClearDish()
    {
        if (_dishCard != null)
        {
            Destroy(_dishCard.gameObject);
            _dishCard = null;
        }
    }

    /// <summary>
    /// Top the hand up to maxHandSize: deal missing cards onto full-fan slots
    /// and slide pre-existing cards into place (single re-layout point).
    /// </summary>
    void TopUpHand()
    {
        int missing = config.maxHandSize - _cards.Count;
        if (missing <= 0) return;

        int existing = _cards.Count;
        for (int i = 0; i < missing; i++)
            DealOne(_cards.Count);

        // Existing cards keep their indices, but the hand size grew: slide
        // them onto the full fan (SetSlot no-ops if already in place).
        for (int i = 0; i < existing; i++)
            _cards[i].SetSlot(i);
    }

    /// <summary>
    /// Level pick-up from the run session: the same Prototype scene serves both
    /// levels, so Awake selects the level via GameSession.currentLevelIndex
    /// (0 = México, 1 = Colombia, 0-based into RecipeDatabase.levels).
    /// Bounds-guarded — an out-of-range index keeps the serialized currentLevel
    /// so standalone Prototype testing still works.
    /// </summary>
    void Awake()
    {
        // Tutorial override (design D1): a live GameSession.tutorialLevel beats
        // the database pick, so the Tutorial scene can teach La Llorona without
        // touching the run's level index. ResetRun clears it before real runs.
        if (GameSession.tutorialLevel != null)
        {
            currentLevel = GameSession.tutorialLevel;
            return;
        }
        if (recipeDatabase != null
            && GameSession.currentLevelIndex >= 0
            && GameSession.currentLevelIndex < recipeDatabase.levels.Count)
            currentLevel = recipeDatabase.levels[GameSession.currentLevelIndex];
    }

    /// <summary>
    /// Pushes the initial refill state into the HUD label at startup, so the
    /// label is correct even before the first START.
    /// </summary>
    void Start()
    {
        UpdateRefillLabel();
    }

    /// <summary>
    /// Refreshes the on-screen refill counter (e.g. "REFILLS (MANO): 1/2"),
    /// refreshed on every consume so the HUD is honest while the whole hand is
    /// flying. No-op when the label reference is unwired.
    /// </summary>
    void UpdateRefillLabel()
    {
        if (refillLabel == null) return;
        refillLabel.text = $"REFILLS (MANO): {_refillsUsed}/{config.maxRefillsPerGame}";
    }

    /// <summary>
    /// Tries to park <paramref name="card"/> in <paramref name="zone"/>.
    /// Rejected (false, hand unchanged) when the zone is locked or at capacity.
    /// The corner is NEVER rejected for budget reasons: REFILL always flushes it
    /// for free, so a parked card can never be stranded. On accept: removes the
    /// card from the hand SYNCHRONOUSLY, re-layouts the hand NOW, and starts the
    /// park animation.
    /// </summary>
    public bool TryParkInZone(CardView card, DropZone zone)
    {
        if (!zone.Accept(card)) return false;

        // Player reacts to the park destination (design D1 mapping).
        if (zone == centerZone) playerView?.OnParkCook();
        else if (zone == cornerZone) playerView?.OnParkTrash();

        _cards.Remove(card);
        for (int i = 0; i < _cards.Count; i++) _cards[i].SetSlot(i); // re-layout NOW
        card.PlaceInZone(zone);
        return true;
    }

    /// <summary>
    /// Detaches all parked cards from <paramref name="zone"/> and starts their
    /// consume animation. Consumes are visual-only: cards already left the
    /// hand at park, so the hand state is untouched here.
    /// </summary>
    void ConsumeZone(DropZone zone)
    {
        foreach (var card in zone.ReleaseAll())
            if (card != null) card.ConsumeFromZone();
    }

    /// <summary>
    /// Destroys all parked cards in <paramref name="zone"/> (used by
    /// StartDeal to reset the board without playing the consume animation).
    /// </summary>
    void ClearZone(DropZone zone)
    {
        foreach (var card in zone.ReleaseAll())
            if (card != null) Destroy(card.gameObject);
    }

    /// <summary>
    /// Public clean-room for the cook queue: destroys every parked card in the
    /// center zone (e.g. the tutorial's patience step starts from an empty
    /// center so the step-3 prop card can't pollute the blue recipe match).
    /// </summary>
    public void ClearCookQueue() => ClearZone(centerZone);

    /// <summary>
    /// Parabola slot position for <paramref name="index"/> of <paramref name="count"/>
    /// cards. Quadratic Bezier through parabolaStart/Control/End is a parabola;
    /// t = i/(N-1) fans N cards symmetrically along it (N=1 -> apex).
    /// </summary>
    public Vector2 GetSlotPosition(int index, int count)
    {
        float t = count <= 1 ? 0.5f : (float)index / (count - 1f);
        Vector2 p0 = config.parabolaStart;
        Vector2 p1 = config.parabolaControl;
        Vector2 p2 = config.parabolaEnd;
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    void DealOne(int slotIndex)
    {
        CardData data = NextCard();
        if (data == null)
        {
            Debug.LogWarning("HandController: deck is null or empty; no card dealt.", this);
            return;
        }

        GameObject go = Instantiate(config.cardPrefab, handRoot);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = config.dealOrigin;

        var card = go.GetComponent<CardView>();
        card.Init(this, config, centerZone, cornerZone, slotIndex, playerView);
        card.SetData(data);
        _cards.Add(card);
        card.DealIn();
    }

    /// <summary>
    /// Next CardData from the pre-built draw order, advancing the wrap-around
    /// pointer. Returns null (and does not advance) when the draw order is
    /// null or empty (deck unwired or no entries). Order exhaustion wraps the
    /// SAME shuffled order — no mid-game re-shuffle.
    /// </summary>
    CardData NextCard()
    {
        if (_drawOrder == null || _drawOrder.Count == 0) return null;
        CardData data = _drawOrder[_dealIndex];
        _dealIndex = (_dealIndex + 1) % _drawOrder.Count;
        return data;
    }

    /// <summary>
    /// In-place Fisher-Yates shuffle. Uses deck.randomSeed when >= 0
    /// (deterministic, for debugging), System.Random() otherwise.
    /// </summary>
    void ShuffleFisherYates(List<CardData> list)
    {
        System.Random rng = deck != null && deck.RandomSeed >= 0
            ? new System.Random(deck.RandomSeed)
            : new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}