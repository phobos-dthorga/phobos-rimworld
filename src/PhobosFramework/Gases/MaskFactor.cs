using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Gases;

/// <summary>Reads what a pawn wears and hands the figures to <see cref="MaskRule"/>.</summary>
public static class MaskFactor
{
    public const string GasMaskTag = "GasMask";

    /// <summary>For gases a filter mask stops.</summary>
    public static float Filtered(Pawn pawn) =>
        MaskRule.Filtered(ToxicEnvironmentResistance(pawn), WearsGasMaskTag(pawn));

    /// <summary>For gases only sealed headgear stops.</summary>
    public static float Unfiltered(Pawn pawn) =>
        MaskRule.Unfiltered(MaskRule.IsSealed(CombatExtendedSmoke.Active, CombatExtendedSmoke.Sensitivity(pawn), ToxicEnvironmentResistance(pawn)));

    public static float For(Pawn pawn, bool filteredByMask) => filteredByMask ? Filtered(pawn) : Unfiltered(pawn);

    static float ToxicEnvironmentResistance(Pawn pawn) => pawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance);

    static bool WearsGasMaskTag(Pawn pawn)
    {
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn == null) return false;
        for (int i = 0; i < worn.Count; i++)
        {
            List<string>? tags = worn[i].def.apparel?.tags;
            if (tags != null && tags.Contains(GasMaskTag)) return true;
        }
        return false;
    }
}
