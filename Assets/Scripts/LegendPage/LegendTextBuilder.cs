using System.Collections.Generic;

/// <summary>Which rendering path the Legend scene takes for a level.</summary>
public enum LegendMode
{
    /// <summary>A non-empty LegendPage: fill the newspaper slots.</summary>
    Page,
    /// <summary>No page: fall back to the legacy legendTitle/legendText pair.</summary>
    Legacy,
    /// <summary>Nothing to show: auto-forward to nextScene.</summary>
    Forward
}

/// <summary>
/// One recipe flattened to display text: the dish name plus its ingredient
/// display names (null/blank names dropped, authored order kept).
/// </summary>
public readonly struct LegendRecipeText
{
    public readonly string DishName;
    public readonly string[] Ingredients;

    public LegendRecipeText(string dishName, string[] ingredients)
    {
        DishName = dishName;
        Ingredients = ingredients ?? System.Array.Empty<string>();
    }

    /// <summary>Builds a row's data, dropping null/blank ingredient names.</summary>
    public static LegendRecipeText From(string dishName, IEnumerable<string> ingredients)
    {
        var kept = new List<string>();
        if (ingredients != null)
            foreach (var name in ingredients)
                if (!string.IsNullOrWhiteSpace(name)) kept.Add(name);
        return new LegendRecipeText(dishName, kept.ToArray());
    }
}

/// <summary>
/// Pure legend resolution + formatting rules. No MonoBehaviour: unit-testable
/// from EditMode without a scene or an asset load.
/// </summary>
public static class LegendTextBuilder
{
    /// <summary>
    /// Page when a non-empty page exists; else Legacy when legacy copy exists;
    /// else Forward. A null page and an all-blank page both fold into Legacy.
    /// </summary>
    public static LegendMode Select(LegendPage page, string legacyText)
    {
        if (page != null && !page.IsEmpty()) return LegendMode.Page;
        if (!string.IsNullOrEmpty(legacyText)) return LegendMode.Legacy;
        return LegendMode.Forward;
    }

    /// <summary>
    /// One line per recipe, joined with '\n'. Format:
    /// `• {dish}: {a} + {b}` — the `: ...` suffix is omitted when a recipe has
    /// no ingredients. A blank dish name renders as `(sin nombre)`. No recipes →
    /// the empty string.
    /// </summary>
    public static string BuildDemandsList(IReadOnlyList<LegendRecipeText> recipes)
    {
        if (recipes == null || recipes.Count == 0) return string.Empty;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < recipes.Count; i++)
        {
            var dish = string.IsNullOrWhiteSpace(recipes[i].DishName)
                ? "(sin nombre)"
                : recipes[i].DishName;

            if (sb.Length > 0) sb.Append('\n');
            sb.Append("• ").Append(dish);

            var ingredients = recipes[i].Ingredients;
            if (ingredients != null && ingredients.Length > 0)
                sb.Append(": ").Append(string.Join(" + ", ingredients));
        }
        return sb.ToString();
    }
}
