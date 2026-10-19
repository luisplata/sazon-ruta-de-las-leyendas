using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Market scene controller: reads the current level's MarketConfig (via
/// RecipeDatabase.levels[GameSession.currentLevelIndex]) and spawns one buy
/// button per catalog item from an inactive scene template. Buying deducts
/// coins, grants the item's QUANTITY of card copies to GameSession.purchasedCards
/// and marks the card sold-out for the rest of the run (one purchase per item).
/// LISTO routes to Prototype, where StartDeal appends the purchased copies.
/// </summary>
public class MarketController : MonoBehaviour
{
    [Tooltip("Global level catalog; market read from levels[currentLevelIndex].")]
    [SerializeField] RecipeDatabase database;

    [Tooltip("Coins label (TMP), e.g. 'MONEDAS: 20'.")]
    [SerializeField] TMP_Text coinsLabel;

    [Tooltip("Inactive buy-button template; one instance is spawned per catalog item (into the matching product slot, or catalogRoot as fallback).")]
    [SerializeField] Button buyButtonTemplate;

    [Tooltip("Root transform the spawned buy buttons are parented to (fallback when no productSlots are set).")]
    [SerializeField] Transform catalogRoot;

    [Tooltip("Optional art-driven product slots. When set, the i-th catalog product is spawned as a child of slots[i] and stretched to fill its rect. Empty/unassigned → fall back to catalogRoot.")]
    [SerializeField] List<RectTransform> productSlots = new();

    [Tooltip("Full-screen black overlay; the market reveals behind the fade-in.")]
    [SerializeField] FadeController fade;

    /// <summary>
    /// Scene loaded by LISTO. The real Market routes to Prototype; the tutorial
    /// copy (TutorialMarket) overrides this to Tutorial so the guided chain
    /// keeps moving into the coached match.
    /// </summary>
    [SerializeField] string nextScene = Scenes.Prototype;

    /// <summary>Spawned buy button per catalog item (Refresh() toggles each one's interactable state).</summary>
    readonly Dictionary<MarketConfig.MarketItem, Button> _buttons = new();

    /// <summary>
    /// Spawns one buy button per catalog item, refreshes the coins label and
    /// button states, and fades in. A null or empty MarketConfig → Listo-only
    /// market (no buttons).
    /// </summary>
    void Start()
    {
        var config = CurrentMarket();
        var items = config != null ? config.items : null;

        if (items != null)
        {
            bool useSlots = productSlots != null && productSlots.Count > 0;

            // Overflow guard: never spawn more products than there are slots.
            if (useSlots)
            {
                int validCount = 0;
                foreach (var item in items)
                    if (item != null && item.card != null) validCount++;

                if (validCount > productSlots.Count)
                    Debug.LogWarning($"[MarketController] {validCount} catalog items but only " +
                                     $"{productSlots.Count} product slots — spawning {productSlots.Count}, " +
                                     $"dropping {validCount - productSlots.Count}.");
            }

            int slotIndex = 0;
            foreach (var item in items)
            {
                if (item == null || item.card == null) continue;
                if (useSlots && slotIndex >= productSlots.Count) break; // leftover slots stay empty
                SpawnButton(item, slotIndex);
                slotIndex++;
            }
        }

        Refresh();
        if (fade != null) fade.FadeIn();
    }

    /// <summary>LISTO: leave the Market and start the level in nextScene (Prototype by default).</summary>
    public void Done()
    {
        SceneManager.LoadScene(nextScene);
    }

    /// <summary>
    /// Buy: reject when the card is already sold out this run or coins are
    /// insufficient (no-op — the button is also disabled in that state). On
    /// accept: deduct the price, grant `quantity` copies to purchasedCards,
    /// mark the card sold-out, then refresh coins + button states.
    /// </summary>
    public void Buy(MarketConfig.MarketItem item)
    {
        if (item == null || item.card == null) return;
        if (GameSession.soldOut.Contains(item.card)) return;
        if (GameSession.coins < item.price) return;

        GameSession.coins -= item.price;
        for (int i = 0; i < item.quantity; i++)
            GameSession.purchasedCards.Add(item.card);
        GameSession.soldOut.Add(item.card);
        Refresh();
    }

    /// <summary>
    /// Instantiates one buy button from the inactive template and labels it
    /// "{displayName} {icon} x{quantity} ${price}". When <paramref name="slotIndex"/>
    /// maps to a configured product slot, the instance is parented to that slot and
    /// stretched to fill its rect (anchorMin=(0,0), anchorMax=(1,1), sizeDelta=0,
    /// anchoredPosition=0) so the designer controls size by sizing the slot.
    /// Otherwise it falls back to catalogRoot (stacked, unchanged legacy behaviour).
    /// No-op when both the slot and the fallback root are unwired.
    /// </summary>
    void SpawnButton(MarketConfig.MarketItem item, int slotIndex)
    {
        if (buyButtonTemplate == null) return;

        RectTransform slot = (productSlots != null && slotIndex >= 0 && slotIndex < productSlots.Count)
            ? productSlots[slotIndex]
            : null;

        Transform parent = slot != null ? slot : catalogRoot;
        if (parent == null) return;

        var button = Instantiate(buyButtonTemplate, parent);
        button.gameObject.SetActive(true);

        if (slot != null && button.transform is RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = $"{item.card.displayName}  x{item.quantity}  ${item.price}";

        var itemCopy = item; // stable closure
        button.onClick.AddListener(() => Buy(itemCopy));
        _buttons[item] = button;
    }

    /// <summary>
    /// Refreshes the coins label ("MONEDAS: N") and each spawned button's
    /// interactable state: enabled only while the item is NOT sold out this run
    /// AND the player can afford it. Also guards the template itself so a
    /// repurchase can never be clicked while the economy blocks it.
    /// </summary>
    void Refresh()
    {
        if (coinsLabel != null) coinsLabel.text = $"MONEDAS: {GameSession.coins}";
        foreach (var pair in _buttons)
        {
            if (pair.Key == null || pair.Key.card == null || pair.Value == null) continue;
            bool canBuy = !GameSession.soldOut.Contains(pair.Key.card)
                          && GameSession.coins >= pair.Key.price;
            pair.Value.interactable = canBuy;
        }
    }

    /// <summary>
    /// Current level's MarketConfig, bounds-guarded (null when unwired/out of
    /// range). Tutorial override (design D1, same as HandController.Awake): a
    /// live GameSession.tutorialLevel beats the database pick, so the tutorial
    /// chain shows the tutorial catalog (Llorona's marketConfig) instead of the
    /// RecipeDatabase level's market. ResetRun clears it before real runs, so
    /// the real flow is untouched.
    /// </summary>
    MarketConfig CurrentMarket()
    {
        if (GameSession.tutorialLevel != null)
            return GameSession.tutorialLevel.marketConfig;
        if (database == null || database.levels == null) return null;
        int index = GameSession.currentLevelIndex;
        if (index < 0 || index >= database.levels.Count) return null;
        var level = database.levels[index];
        return level != null ? level.marketConfig : null;
    }
}