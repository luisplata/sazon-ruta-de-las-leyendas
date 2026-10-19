using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Legend scene controller. Resolution ladder (LegendTextBuilder.Select):
/// a non-empty LegendPage fills the 14 newspaper slots (all authored except
/// demandsList, generated from level.recipes); otherwise the legacy
/// legendTitle/legendText pair; otherwise auto-forward to nextScene. Narration
/// audio is null-safe. Legacy fields are kept — never removed.
/// </summary>
public class LegendController : MonoBehaviour
{
    [Tooltip("Global level catalog; legend fields read from levels[currentLevelIndex].")]
    [SerializeField] RecipeDatabase database;

    [Tooltip("Legacy legend title label (fallback when no LegendPage is wired).")]
    [SerializeField] TMP_Text titleText;

    [Tooltip("Legacy legend body label (fallback when no LegendPage is wired).")]
    [SerializeField] TMP_Text bodyText;

    [Header("LegendPage slots (newspaper)")]
    [SerializeField] TMP_Text mastheadText;
    [SerializeField] TMP_Text datelineText;
    [SerializeField] TMP_Text headlineText;
    [SerializeField] TMP_Text subheadText;
    [SerializeField] TMP_Text reportHeaderText;
    [SerializeField] TMP_Text reportBodyText;
    [SerializeField] TMP_Text demandsHeaderText;
    [SerializeField] TMP_Text demandsListText;
    [SerializeField] TMP_Text adviceHeaderText;
    [SerializeField] TMP_Text adviceBodyText;
    [SerializeField] TMP_Text sidebarBlock1Text;
    [SerializeField] TMP_Text sidebarBlock2Text;
    [SerializeField] TMP_Text sidebarBlock3Text;

    [Tooltip("ContinuarButton label, reused as the page footer.")]
    [SerializeField] TMP_Text footerText;

    [Tooltip("Full-screen black overlay; the legend reveals behind the fade-in.")]
    [SerializeField] FadeController fade;

    /// <summary>
    /// Scene loaded by CONTINUAR (and by the empty-legend auto-forward). The
    /// real Legend routes to the Market; the tutorial copy (TutorialLegend)
    /// overrides this to TutorialMarket so the guided chain keeps moving.
    /// </summary>
    [SerializeField] string nextScene = Scenes.Market;

    /// <summary>
    /// Resolves the current level's legend. Page ⇒ fill every newspaper slot;
    /// Legacy ⇒ fill title/body; Forward ⇒ immediately load nextScene (no UI, no
    /// null-ref). Plays narration if present (null-safe) and fades in.
    /// </summary>
    void Start()
    {
        var level = CurrentLevel();
        var page = level != null ? level.legendPage : null;

        switch (LegendTextBuilder.Select(page, level != null ? level.legendText : null))
        {
            case LegendMode.Page:
                FillPage(page, level);
                break;
            case LegendMode.Legacy:
                if (titleText != null) titleText.text = level.legendTitle;
                if (bodyText != null) bodyText.text = level.legendText;
                break;
            default: // Forward
                SceneManager.LoadScene(nextScene);
                return;
        }

        if (level.audioNarration != null)
            AudioSource.PlayClipAtPoint(level.audioNarration, Vector3.zero);
        if (fade != null) fade.FadeIn();
    }

    /// <summary>Fills every newspaper slot from the page; demandsList is generated.</summary>
    void FillPage(LegendPage page, LevelConfig level)
    {
        Set(mastheadText, page.masthead);
        Set(datelineText, page.dateline);
        Set(headlineText, page.headline);
        Set(subheadText, page.subhead);
        Set(reportHeaderText, page.reportHeader);
        Set(reportBodyText, page.reportBody);
        Set(demandsHeaderText, page.demandsHeader);
        Set(demandsListText, LegendTextBuilder.BuildDemandsList(ToRecipeTexts(level.recipes)));
        Set(adviceHeaderText, page.adviceHeader);
        Set(adviceBodyText, page.adviceBody);
        Set(sidebarBlock1Text, page.sidebarBlock1);
        Set(sidebarBlock2Text, page.sidebarBlock2);
        Set(sidebarBlock3Text, page.sidebarBlock3);
        Set(footerText, page.footer);
    }

    static void Set(TMP_Text slot, string value)
    {
        if (slot != null) slot.text = value;
    }

    /// <summary>RecipeData → LegendRecipeText (null recipes skipped; order kept).</summary>
    static List<LegendRecipeText> ToRecipeTexts(List<RecipeData> recipes)
    {
        var rows = new List<LegendRecipeText>();
        if (recipes == null) return rows;

        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

            var names = new List<string>();
            if (recipe.ingredients != null)
                foreach (var ingredient in recipe.ingredients)
                    names.Add(ingredient != null ? ingredient.displayName : null);

            rows.Add(LegendRecipeText.From(recipe.dishName, names));
        }
        return rows;
    }

    /// <summary>CONTINUAR: proceed to nextScene (the Market for this level).</summary>
    public void Continue()
    {
        SceneManager.LoadScene(nextScene);
    }

    /// <summary>
    /// Current level's config, bounds-guarded (null when unwired/out of range).
    /// Tutorial override (design D1, same as HandController.Awake): a live
    /// GameSession.tutorialLevel beats the database pick, so the tutorial chain
    /// (Menu → TutorialLegend → TutorialMarket → Tutorial) shows La Llorona's
    /// legend instead of the RecipeDatabase level. ResetRun clears it before
    /// any real run, so the real flow is untouched.
    /// </summary>
    LevelConfig CurrentLevel()
    {
        if (GameSession.tutorialLevel != null) return GameSession.tutorialLevel;
        if (database == null || database.levels == null) return null;
        int index = GameSession.currentLevelIndex;
        if (index < 0 || index >= database.levels.Count) return null;
        return database.levels[index];
    }
}
