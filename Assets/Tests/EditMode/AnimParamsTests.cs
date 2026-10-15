using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for the pure <see cref="AnimParams.HasParam"/> guard that lets
/// EnemyView tolerate an Animator lacking a reaction trigger (no console warning
/// spam when the creature controller exposes none of Win/Patience/Bite/Eaten).
/// </summary>
public class AnimParamsTests
{
    static AnimatorControllerParameter Param(string name)
    {
        return new AnimatorControllerParameter { name = name };
    }

    [Test]
    public void HasParam_MatchingName_ReturnsTrue()
    {
        var parameters = new[] { Param("Idle"), Param("Win"), Param("Bite") };

        Assert.IsTrue(AnimParams.HasParam(parameters, "Win"));
    }

    [Test]
    public void HasParam_AbsentName_ReturnsFalse()
    {
        var parameters = new[] { Param("Idle"), Param("Win"), Param("Bite") };

        Assert.IsFalse(AnimParams.HasParam(parameters, "Eaten"));
    }

    [Test]
    public void HasParam_NullArray_ReturnsFalse()
    {
        Assert.IsFalse(AnimParams.HasParam(null, "Win"));
    }

    [Test]
    public void HasParam_EmptyArray_ReturnsFalse()
    {
        var parameters = new AnimatorControllerParameter[0];

        Assert.IsFalse(AnimParams.HasParam(parameters, "Win"));
    }

    [Test]
    public void HasParam_DifferentCase_ReturnsFalse()
    {
        var parameters = new[] { Param("Win") };

        Assert.IsFalse(AnimParams.HasParam(parameters, "win"));
    }

    [Test]
    public void HasParam_NullOrBlankName_ReturnsFalse()
    {
        var parameters = new[] { Param("Win") };

        Assert.IsFalse(AnimParams.HasParam(parameters, null));
        Assert.IsFalse(AnimParams.HasParam(parameters, ""));
    }

    [Test]
    public void HasParam_DefaultCreatureControllerShape_ReturnsFalseForReactions()
    {
        // Mirrors Creature1 1.controller: zero parameters (m_AnimatorParameters: []).
        var parameters = new AnimatorControllerParameter[0];

        Assert.IsFalse(AnimParams.HasParam(parameters, "Win"));
        Assert.IsFalse(AnimParams.HasParam(parameters, "Patience"));
        Assert.IsFalse(AnimParams.HasParam(parameters, "Bite"));
        Assert.IsFalse(AnimParams.HasParam(parameters, "Eaten"));
    }
}
