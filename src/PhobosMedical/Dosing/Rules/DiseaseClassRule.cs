using System.Collections.Generic;

namespace Phobos.Dosing.Rules;

/// <summary>
/// Which drugs act on which diseases (Medical 0.33.0; docs/development/medical-care-round-two.md): a disease has a class (bacterial,
/// viral, protozoan, worms, mechanites), and a drug for fighting disease may name the classes it acts on. Game-free: classes are their
/// Defs' names.
/// </summary>
public static class DiseaseClassRule
{
    /// <summary>
    /// Whether a drug acts on a disease. A drug that names no class acts on every disease, as every drug did before classes; a disease
    /// nobody has classed takes any drug (the owner's card of 27 Nov 2026: "Any drug counts"), so a new mod's disease is never left
    /// untreated because we had not read it.
    /// </summary>
    public static bool Acts(IReadOnlyList<string> drugClasses, string? diseaseClass)
    {
        if (drugClasses.Count == 0 || diseaseClass == null) return true;
        for (int i = 0; i < drugClasses.Count; i++)
        {
            if (drugClasses[i] == diseaseClass) return true;
        }
        return false;
    }
}
