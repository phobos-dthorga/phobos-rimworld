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

    /// <summary>
    /// For gases only sealed headgear stops. Combat Extended's gas masks carry the same smoke-sensitivity offset as its sealed
    /// helmets, so the sensitivity is rebuilt from the worn items that carry no gas-mask tag (0.2.1 fix: a filter mask stopped
    /// carbon monoxide).
    /// </summary>
    public static float Unfiltered(Pawn pawn) =>
        MaskRule.Unfiltered(MaskRule.IsSealed(CombatExtendedSmoke.Active, SensitivityFromSealingGear(pawn), ToxicEnvironmentResistance(pawn)));

    static readonly List<float> offsets = new();

    static float SensitivityFromSealingGear(Pawn pawn)
    {
        if (!CombatExtendedSmoke.Active) return 1f;
        StatDef? stat = OptionalMods.StatDefNamed(CombatExtendedSmoke.SmokeSensitivityStatName);
        if (stat == null) return 1f;
        offsets.Clear();
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn != null)
        {
            for (int i = 0; i < worn.Count; i++)
            {
                ThingDef def = worn[i].def;
                List<string>? tags = def.apparel?.tags;
                if (tags != null && tags.Contains(GasMaskTag)) continue;
                if (def.equippedStatOffsets != null) offsets.Add(def.equippedStatOffsets.GetStatOffsetFromList(stat));
            }
        }
        return MaskRule.SensitivityFromSealingGear(stat.defaultBaseValue, offsets);
    }

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
