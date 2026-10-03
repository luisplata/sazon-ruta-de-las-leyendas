using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result of resolving a 1-3 card cook against a level config and the global
/// recipe catalog. Reaction comes from the matched recipe's reactionText, or
/// is null for generated results.
/// </summary>
public readonly struct ResolutionResult
{
    public readonly RecipeKind Kind;
    public readonly string DishName;
    public readonly string Icon;
    public readonly string Reaction;
    public readonly Color Color;
    public readonly Sprite Art;

    public ResolutionResult(RecipeKind kind, string dishName, string icon, string reaction, Color color, Sprite art = null)
    {
        Kind = kind;
        DishName = dishName;
        Icon = icon;
        Reaction = reaction;
        Color = color;
        Art = art;
    }
}

/// <summary>
/// Pure static resolver mapping the 1-3 cooked CardData to a ResolutionResult.
/// Priority (spec-confirmed): (1) exact set-equality match against the FULL
/// catalog — pass 1 hits the current level's recipes → Winner (recipe kind:
/// Star gold / Normal green), pass 2 hits another level's recipe → Presented
/// (blue); (2) cursed role-patterns (3-card cooks only) beat AutoFail;
/// (3) required ingredient present → Filler, missing → Fail. A 1-2 card cook
/// can never match a recipe (count mismatch → skip) and can never form a
/// 3-card cursed pattern, so it falls straight to the required check.
/// </summary>
public static class RecipeResolver
{
    public static readonly Color StarColor = new Color(1f, 0.84f, 0f, 1f);
    public static readonly Color NormalColor = new Color(0.2f, 0.7f, 0.3f, 1f);
    public static readonly Color CursedColor = new Color(0.6f, 0.25f, 0.8f, 1f);
    public static readonly Color FillerColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public static readonly Color FailColor = new Color(0.85f, 0.2f, 0.2f, 1f);
    public static readonly Color PresentedColor = new Color(0.25f, 0.55f, 0.95f, 1f);

    public static ResolutionResult Resolve(IReadOnlyList<CardData> cooked, LevelConfig currentLevel, RecipeDatabase database)
    {
        // Guard: 1-3 inputs only (PrepareFood no-ops on an empty queue; this is
        // defense-in-depth). Pass 2 needs the catalog; null degrades to v1.
        if (cooked == null || cooked.Count < 1 || cooked.Count > 3 || currentLevel == null)
            return Generated(RecipeKind.Fail, "Comida Cruda", "\U0001F480", FailColor);

        var set = new HashSet<CardData>(cooked);

        // Pass 1: exact set-equality vs the current level's recipes (order- and
        // count-independent reference equality). Any kind (Star or Normal)
        // beats cursed patterns and beats Presented — level-owned wins.
        if (currentLevel.recipes != null)
        {
            foreach (var recipe in currentLevel.recipes)
            {
                if (recipe == null || recipe.ingredients == null || recipe.ingredients.Count != cooked.Count)
                    continue;
                if (set.SetEquals(recipe.ingredients))
                {
                    return new ResolutionResult(
                        kind: recipe.kind,
                        dishName: recipe.dishName,
                        icon: recipe.icon,
                        reaction: recipe.reactionText,
                        color: GetColor(recipe.kind),
                        art: recipe.art);
                }
            }
        }

        // Pass 2: exact set-equality vs recipes owned by OTHER levels → Presented
        // (blue). Skipped with a warning when the catalog is unwired (v1 behavior:
        // cross-level cooks fall to the required check below).
        if (database == null)
        {
            Debug.LogWarning("RecipeResolver: RecipeDatabase is null — skipping cross-level Presented match (v1 behavior).", currentLevel);
        }
        else if (database.levels != null)
        {
            foreach (var level in database.levels)
            {
                if (level == null || level == currentLevel || level.recipes == null)
                    continue;
                foreach (var recipe in level.recipes)
                {
                    if (recipe == null || recipe.ingredients == null || recipe.ingredients.Count != cooked.Count)
                        continue;
                    if (set.SetEquals(recipe.ingredients))
                    {
                        return new ResolutionResult(
                            kind: RecipeKind.Presented,
                            dishName: recipe.dishName,
                            icon: recipe.icon,
                            reaction: recipe.reactionText,
                            color: PresentedColor,
                            art: recipe.art);
                    }
                }
            }
        }

        // 2. Cursed role-patterns (3-card cooks only; beat AutoFail). 1-2 card
        // cooks fall straight through to the required check.
        if (cooked.Count == 3)
        {
            int bases = 0, sazones = 0, complementos = 0;
            foreach (var card in cooked)
            {
                switch (card.role)
                {
                    case CardRole.Base: bases++; break;
                    case CardRole.Sazon: sazones++; break;
                    case CardRole.Complemento: complementos++; break;
                }
            }
            if (bases == 3) return Generated(RecipeKind.Cursed, "El Bodoque de Almidón", "\U0001F95F", CursedColor);
            if (sazones == 3) return Generated(RecipeKind.Cursed, "El Shot de Sazón", "\U0001F964", CursedColor);
            if (complementos == 3) return Generated(RecipeKind.Cursed, "La Ensalada Triste", "\U0001F957", CursedColor);
            if (bases == 1 && sazones == 1 && complementos == 1)
                return Generated(RecipeKind.Cursed, "El Guiso de la Vergüenza", "\U0001F958", CursedColor);
        }

        // 3. Required ingredient present → Filler; missing → AutoFail.
        if (currentLevel.requiredIngredient != null && set.Contains(currentLevel.requiredIngredient))
            return Generated(RecipeKind.Filler, "Plato Raro", "\U0001F37D", FillerColor);

        return Generated(RecipeKind.Fail, "Comida Cruda", "\U0001F480", FailColor);
    }

    /// <summary>Runtime-generated result (cursed/filler/fail): no reaction text.</summary>
    static ResolutionResult Generated(RecipeKind kind, string dishName, string icon, Color color)
    {
        return new ResolutionResult(kind, dishName, icon, null, color);
    }

    /// <summary>Kind color palette (Star gold / Normal green / Cursed purple / Filler gray / Fail red / Presented blue).</summary>
    public static Color GetColor(RecipeKind kind)
    {
        switch (kind)
        {
            case RecipeKind.Star: return StarColor;
            case RecipeKind.Normal: return NormalColor;
            case RecipeKind.Cursed: return CursedColor;
            case RecipeKind.Filler: return FillerColor;
            case RecipeKind.Presented: return PresentedColor;
            default: return FailColor;
        }
    }
}