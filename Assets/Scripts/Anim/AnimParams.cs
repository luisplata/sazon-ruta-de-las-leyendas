using UnityEngine;

/// <summary>
/// Pure animator-parameter helpers. Kept free of <see cref="Animator"/> instance
/// state so they are trivially unit-testable without a live Animator.
/// Lives in the runtime assembly <c>Sazon.Anim</c> (auto-referenced by
/// Assembly-CSharp) so both gameplay code and the EditMode tests can use it.
/// </summary>
public static class AnimParams
{
    /// <summary>
    /// True when <paramref name="parameters"/> contains a parameter named
    /// <paramref name="name"/> (case-sensitive, matching Animator.SetTrigger
    /// semantics). False for a null/empty array or a null/blank name.
    /// </summary>
    public static bool HasParam(AnimatorControllerParameter[] parameters, string name)
    {
        if (parameters == null || string.IsNullOrEmpty(name)) return false;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter != null && parameter.name == name) return true;
        }

        return false;
    }
}
