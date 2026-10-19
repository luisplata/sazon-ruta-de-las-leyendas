using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for the pure legend rules: LegendPage.IsEmpty,
/// LegendTextBuilder.Select (Page/Legacy/Forward) and BuildDemandsList
/// formatting. No scene, no asset load — instances are created in-test.
/// </summary>
public class LegendPageTests
{
    static LegendPage PageWith(string field, string value)
    {
        var page = ScriptableObject.CreateInstance<LegendPage>();
        typeof(LegendPage).GetField(field).SetValue(page, value);
        return page;
    }

    static LegendRecipeText Row(string dish, params string[] ingredients)
    {
        return LegendRecipeText.From(dish, ingredients);
    }

    // ---- IsEmpty -----------------------------------------------------------

    [Test]
    public void IsEmpty_NewPage_ReturnsTrue()
    {
        var page = ScriptableObject.CreateInstance<LegendPage>();

        Assert.IsTrue(page.IsEmpty());
    }

    [Test]
    public void IsEmpty_WhitespaceOnlyFields_ReturnsTrue()
    {
        var page = PageWith("masthead", "   ");
        page.footer = "";

        Assert.IsTrue(page.IsEmpty());
    }

    [Test]
    public void IsEmpty_AnyAuthoredField_ReturnsFalse()
    {
        var page = PageWith("headline", "¡LA CRIATURA DEL RÍO EXIGE TACOS!");

        Assert.IsFalse(page.IsEmpty());
    }

    // ---- Select ------------------------------------------------------------

    [Test]
    public void Select_NonEmptyPage_ReturnsPage()
    {
        var page = PageWith("masthead", "EL PREGONERO PUEBLERO");

        Assert.AreEqual(LegendMode.Page, LegendTextBuilder.Select(page, "legacy body"));
    }

    [Test]
    public void Select_EmptyPageWithLegacy_ReturnsLegacy()
    {
        var page = ScriptableObject.CreateInstance<LegendPage>();

        Assert.AreEqual(LegendMode.Legacy, LegendTextBuilder.Select(page, "legacy body"));
    }

    [Test]
    public void Select_NullPageWithLegacy_ReturnsLegacy()
    {
        Assert.AreEqual(LegendMode.Legacy, LegendTextBuilder.Select(null, "legacy body"));
    }

    [Test]
    public void Select_NullPageNullLegacy_ReturnsForward()
    {
        Assert.AreEqual(LegendMode.Forward, LegendTextBuilder.Select(null, null));
    }

    [Test]
    public void Select_EmptyPageEmptyLegacy_ReturnsForward()
    {
        var page = ScriptableObject.CreateInstance<LegendPage>();

        Assert.AreEqual(LegendMode.Forward, LegendTextBuilder.Select(page, ""));
    }

    // ---- LegendRecipeText.From ---------------------------------------------

    [Test]
    public void From_DropsNullAndBlankIngredients()
    {
        var row = LegendRecipeText.From("Elote", new[] { "Maíz", null, "   ", "Chile Piquín" });

        Assert.AreEqual(2, row.Ingredients.Length);
        Assert.AreEqual("Maíz", row.Ingredients[0]);
        Assert.AreEqual("Chile Piquín", row.Ingredients[1]);
    }

    [Test]
    public void From_NullIngredients_ProducesEmptyArray()
    {
        var row = LegendRecipeText.From("Elote", null);

        Assert.IsEmpty(row.Ingredients);
    }

    // ---- BuildDemandsList ---------------------------------------------------

    [Test]
    public void BuildDemandsList_MexicoSixRows_ExactText()
    {
        var recipes = new List<LegendRecipeText>
        {
            Row("Tacos de Chapulines", "Maíz", "Insectos", "Chile Piquín"),
            Row("Elote Asado", "Maíz"),
            Row("Elote con Chile", "Maíz", "Chile Piquín"),
            Row("Elote Picante", "Maíz", "Tomate", "Chile Piquín"),
            Row("Esquites Clásicos", "Maíz", "Cebolla", "Limón"),
            Row("Esquites Sencillos", "Maíz", "Cebolla"),
        };

        const string expected =
            "• Tacos de Chapulines: Maíz + Insectos + Chile Piquín\n" +
            "• Elote Asado: Maíz\n" +
            "• Elote con Chile: Maíz + Chile Piquín\n" +
            "• Elote Picante: Maíz + Tomate + Chile Piquín\n" +
            "• Esquites Clásicos: Maíz + Cebolla + Limón\n" +
            "• Esquites Sencillos: Maíz + Cebolla";

        Assert.AreEqual(expected, LegendTextBuilder.BuildDemandsList(recipes));
    }

    [Test]
    public void BuildDemandsList_SingleRow_HasNoTrailingSeparator()
    {
        var recipes = new List<LegendRecipeText> { Row("Elote Asado", "Maíz") };

        Assert.AreEqual("• Elote Asado: Maíz", LegendTextBuilder.BuildDemandsList(recipes));
    }

    [Test]
    public void BuildDemandsList_EmptyList_ReturnsEmptyString()
    {
        Assert.AreEqual(string.Empty, LegendTextBuilder.BuildDemandsList(new List<LegendRecipeText>()));
        Assert.AreEqual(string.Empty, LegendTextBuilder.BuildDemandsList(null));
    }

    [Test]
    public void BuildDemandsList_BlankDishName_UsesPlaceholder()
    {
        var recipes = new List<LegendRecipeText> { Row("  ", "Maíz") };

        Assert.AreEqual("• (sin nombre): Maíz", LegendTextBuilder.BuildDemandsList(recipes));
    }

    [Test]
    public void BuildDemandsList_RowWithoutIngredients_OmitsColonSuffix()
    {
        var recipes = new List<LegendRecipeText> { Row("Plato Misterioso") };

        Assert.AreEqual("• Plato Misterioso", LegendTextBuilder.BuildDemandsList(recipes));
    }
}
