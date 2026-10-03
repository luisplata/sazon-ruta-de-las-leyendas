using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kind of a resolved dish. Star/Normal/Cursed are authored on RecipeData
/// assets; Filler/Fail/Presented are generated at runtime by RecipeResolver and
/// must never be authored on an asset (OnValidate warns). Presented (5) is
/// appended at the END so existing serialized int values stay stable.
/// </summary>
public enum RecipeKind
{
    Star,
    Normal,
    Cursed,
    Filler,
    Fail,
    Presented
}

/// <summary>
/// One dish's data-driven identity: display name, emoji icon, kind, and the
/// exact ingredient set (order-independent, reference equality) that cooks it.
/// References CardData assets, so a recipe is market-ready and stays inert
/// until every referenced card exists in the deck. The optional reaction text
/// is carried through resolution for future HUD use (display-only).
/// </summary>
[CreateAssetMenu(fileName = "RecipeData", menuName = "Cards/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Tooltip("Dish name shown on the result card (e.g. 'Tacos de Chapulines').")]
    public string dishName;

    [Tooltip("Emoji glyph shown on the result card (e.g. '🌮').")]
    public string icon;

    [Tooltip("Optional art sprite shown on the dish result card.")]
    public Sprite art;

    [Tooltip("Kind of dish: Star/Normal/Cursed. Filler/Fail are runtime-only and warned on.")]
    public RecipeKind kind;

    [Tooltip("Exact ingredient set for this dish (order-independent match).")]
    public List<CardData> ingredients;

    [Tooltip("Optional flavor/reaction text (display-only; not shown by DishView yet).")]
    [TextArea] public string reactionText;

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(dishName))
            Debug.LogWarning($"RecipeData '{name}': dishName is empty.", this);
        if (kind == RecipeKind.Filler || kind == RecipeKind.Fail || kind == RecipeKind.Presented)
            Debug.LogWarning($"RecipeData '{name}': kind {kind} is generated at runtime by RecipeResolver and should not be authored on an asset.", this);

        // Strict-role rule: no recipe SHALL have two ingredients of the same
        // role (Base/Complemento/Sazón). Editor-time warning only.
        if (ingredients != null)
        {
            var roles = new HashSet<CardRole>();
            foreach (var ing in ingredients)
            {
                if (ing == null) continue;
                if (!roles.Add(ing.role))
                    Debug.LogWarning($"RecipeData '{name}': duplicate role {ing.role} — no recipe SHALL have two same-role ingredients.", this);
            }
        }
    }
}