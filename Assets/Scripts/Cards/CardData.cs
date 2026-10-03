using UnityEngine;

/// <summary>
/// Role of a card in the recipe. Drives the card color via
/// CardVisualConfig.GetRoleColor — color is NEVER a per-card field.
/// </summary>
public enum CardRole
{
    Base,
    Complemento,
    Sazon
}

/// <summary>
/// One ingredient card's identity: role, display name, and emoji icon.
/// The 30-card deck is composed by referencing 10 of these assets (each
/// 1-4x) from a DeckData. Card color comes from the role, not from here.
/// </summary>
[CreateAssetMenu(fileName = "CardData", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    [Tooltip("Role of this ingredient; drives the card color.")]
    public CardRole role;

    [Tooltip("Name shown on the card (e.g. 'Maíz').")]
    public string displayName;

    [Tooltip("Optional art sprite (future-proof; not used by the visual base).")]
    public Sprite art;

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            Debug.LogWarning($"CardData '{name}': displayName is empty.", this);
    }
}