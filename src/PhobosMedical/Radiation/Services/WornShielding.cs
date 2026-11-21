using System.Collections.Generic;
using Phobos.Medical;
using Phobos.Radiation.Rules;
using RimWorld;
using Verse;

namespace Phobos.Radiation.Services;

/// <summary>
/// What a person wears shields them a little from radiation from outside (D'thorga Medical 0.24.0; docs/development/radiation-model.md,
/// RM17; owner, 22 Nov 2026). Reads each worn garment's mass as the game has it (so Combat Extended's patched masses count) and the
/// groups it covers, for <see cref="ShieldingRule"/>. Used only for doses from outside: never for dust breathed or swallowed.
/// </summary>
public static class WornShielding
{
    /// <summary>The share of an outside dose that reaches this person through what they wear; 1 with the setting off.</summary>
    public static float Pass(Pawn pawn)
    {
        if (!RadiationPart.Settings.wornShielding) return 1f;
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn == null || worn.Count == 0) return 1f;
        var layers = new List<WornLayer>(worn.Count);
        for (int i = 0; i < worn.Count; i++)
        {
            Apparel apparel = worn[i];
            List<BodyPartGroupDef>? groups = apparel.def.apparel?.bodyPartGroups;
            if (groups == null || groups.Count == 0) continue;
            var names = new string[groups.Count];
            for (int g = 0; g < groups.Count; g++) names[g] = groups[g].defName;
            layers.Add(new WornLayer(apparel.GetStatValue(StatDefOf.Mass), names));
        }
        float pass = (float)ShieldingRule.Pass(layers);
        if (pass < 0.99f && Seen.Wants("radiation.shielded", pawn)) Seen.Note("radiation.shielded", pawn, ((1f - pass) * 100f).ToString("0") + "% cut");
        return pass;
    }
}
