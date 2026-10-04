using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-level cooking configuration: display identity, the ingredient that must
/// be present to avoid AutoFail, and the exact recipes (Star + Normal only)
/// that resolve in this level. Loaded dynamically by HandController so the
/// same scene serves both levels.
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Cards/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Tooltip("Level display name (e.g. 'El Ahuizotl').")]
    public string levelName;

    [Tooltip("Region display name (e.g. 'México').")]
    public string regionName;

    [Tooltip("Ingredient that must be present in a cook to avoid AutoFail.")]
    public CardData requiredIngredient;

    [Tooltip("Exact recipes resolvable in this level (Star + Normal only).")]
    public List<RecipeData> recipes;

    [Tooltip("Legend scene title (e.g. 'El Ahuizotl').")]
    public string legendTitle;

    [Tooltip("Legend scene body text. Empty ⇒ the Legend scene is skipped (auto-forwards to Market).")]
    [TextArea(3, 8)]
    public string legendText;

    [Tooltip("Optional narration clip played on the Legend scene. Null ⇒ silent.")]
    public AudioClip audioNarration;

    [Tooltip("Market catalog offered before this level. Null ⇒ Listo-only Market.")]
    public MarketConfig marketConfig;

    [Tooltip("Full-scene backdrop art. Null = tinted placeholder square (per-level fallback color).")]
    public Sprite background;

    [Tooltip("Midground layer art, in front of the backdrop and behind the characters. Null = tinted placeholder square.")]
    public Sprite midground;

    [Tooltip("Enemy character art. Null = tinted placeholder square.")]
    public Sprite enemyArt;
}