using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Comps;
using Phobos.BurnsAndSmoke.Services;
using Phobos.Framework.Gases;
using Phobos.Medical;
using Phobos.Radiation.Defs;
using Phobos.Radiation.Rules;
using RimWorld;
using Verse;

namespace Phobos.Radiation.Services;

/// <summary>
/// Radioactive contamination (D'thorga Medical 0.24.0; docs/development/radiation-model.md, RM6; the owner's choices of 22 Nov 2026: on
/// clothes and skin; strip and wash a setting, off by default). Fallout's dose outdoors leaves dust on the clothes (the caustic gas's
/// clothing component, which Dubs Bad Hygiene's washing machine already cleans) and on bare skin; breathed unmasked, it is a dose from
/// inside and iodine to the thyroid, which Dubs Rimatomics' potassium iodide blocks. Carried dust keeps dosing until washed: any wash
/// clears the skin (a safety shower, Dubs Bad Hygiene's shower or bath), and with strip and wash on, a dusty colonist goes to a safety
/// shower and leaves dusty clothes there. <see cref="ContaminationRule"/> holds the arithmetic; the amounts are the rules Def's.
/// </summary>
public static class Contamination
{
    static PhobosRadiationRulesDef Rules => PhobosRadiationRulesDef.Current;

    static bool On => RadiationPart.On && RadiationPart.Settings.contamination;

    /// <summary>The share of an adult's skin, by area, that the groups in <see cref="ShieldingRule.AreaM2"/> sum to.</summary>
    static readonly double BodyArea = Sum(ShieldingRule.AreaM2.Values);

    static double Sum(IEnumerable<double> values)
    {
        double total = 0;
        foreach (double v in values) total += v;
        return total;
    }

    /// <summary>From Rimatomics' fallout, for each dose a person takes outdoors in it: dust on them, and what they breathe.</summary>
    public static void FromFallout(Pawn pawn, float grays)
    {
        if (!On || grays <= 0f || pawn.Dead) return;
        Hediff_PhobosRadiationDose? dose = RadiationService.DoseOf(pawn, create: true);
        if (dose == null) return;
        PhobosRadiationRulesDef rules = Rules;
        float dust = grays * rules.dustPerGray;

        // Dust settles on the clothes by the skin they cover, and on what is left bare.
        double covered = 0;
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        var coveredGroups = new HashSet<string>();
        if (worn != null)
        {
            for (int i = 0; i < worn.Count; i++)
            {
                List<BodyPartGroupDef>? groups = worn[i].def.apparel?.bodyPartGroups;
                if (groups == null) continue;
                double area = 0;
                foreach (BodyPartGroupDef group in groups)
                {
                    if (!ShieldingRule.AreaM2.TryGetValue(group.defName, out double a)) continue;
                    area += a;
                    coveredGroups.Add(group.defName);
                }
                worn[i].TryGetComp<CompPhobosContamination>()?.AddDust(dust * (float)(area / BodyArea));
            }
        }
        foreach (string group in coveredGroups) covered += ShieldingRule.AreaM2[group];
        float bare = dust * (float)ContaminationRule.Bare(covered / BodyArea);
        if (bare > 0f)
        {
            dose.skinDust = SkinDust(dose) + bare;
            dose.skinDustTick = Find.TickManager.TicksGame;
        }

        // Breathed: through the Framework's mask rule; iodine to the thyroid unless potassium iodide is in it already.
        float breathed = grays * MaskFactor.Filtered(pawn);
        dose.inside += breathed * rules.breathedPerGray;
        if (!OnIodide(pawn)) dose.thyroid += breathed * rules.thyroidPerGray;
        if (Seen.Wants("radiation.dust", pawn)) Seen.Note("radiation.dust", pawn, "skin " + dose.skinDust.ToString("0.00") + ", breathed " + breathed.ToString("0.000") + " Gy");
    }

    static bool OnIodide(Pawn pawn) => RadiationDefOf.PotassiumIodide != null && pawn.health.hediffSet.HasHediff(RadiationDefOf.PotassiumIodide);

    /// <summary>Dust on the skin now, faded since it was set.</summary>
    public static float SkinDust(Hediff_PhobosRadiationDose dose) => dose.skinDust <= 0f ? 0f
        : dose.skinDust * (float)ContaminationRule.DustLeft((Find.TickManager.TicksGame - dose.skinDustTick) / (double)GenDate.TicksPerDay);

    /// <summary>Dust in the clothes a person wears now.</summary>
    public static float ClothesDust(Pawn pawn)
    {
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn == null) return 0f;
        float total = 0f;
        for (int i = 0; i < worn.Count; i++) total += worn[i].TryGetComp<CompPhobosContamination>()?.Dust ?? 0f;
        return total;
    }

    /// <summary>Whether a person carries enough dust to want washing off.</summary>
    public static bool Dusty(Pawn pawn) =>
        On && RadiationService.DoseOf(pawn, create: false) is { } dose && (SkinDust(dose) >= Rules.dustToWash || WearsDusty(pawn));

    static bool WearsDusty(Pawn pawn)
    {
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn == null) return false;
        for (int i = 0; i < worn.Count; i++)
        {
            if (worn[i].TryGetComp<CompPhobosContamination>()?.Dusty == true) return true;
        }
        return false;
    }

    /// <summary>
    /// From the step, for <paramref name="days"/> of game time: the dose from carried dust, what is swallowed from the hands, the dose
    /// from inside as the body clears it, and a wash elsewhere (Dubs Bad Hygiene) clearing the skin.
    /// </summary>
    public static void Step(Pawn pawn, Hediff_PhobosRadiationDose dose, double days)
    {
        if (!On || days <= 0) return;
        PhobosRadiationRulesDef rules = Rules;
        float skin = SkinDust(dose);
        if (skin > 0f && CausticService.WashingElsewhere(pawn)) Wash(pawn, dose, strip: false);
        skin = SkinDust(dose);
        float carried = skin + ClothesDust(pawn);
        if (carried > 0.001f) RadiationService.Receive(pawn, (float)(carried * rules.dustDosePerDay * days), "dust");
        if (skin > 0f) dose.inside += (float)(skin * rules.swallowedPerDustDay * days);
        if (dose.inside > 0.0001f)
        {
            float delivered = (float)(dose.inside * ContaminationRule.Delivered(days, HalfLifeInside(pawn)));
            dose.inside -= delivered;
            if (delivered > 0f) RadiationService.Receive(pawn, delivered, "inside");
            if (Seen.Wants("radiation.inside", pawn)) Seen.Note("radiation.inside", pawn, dose.inside.ToString("0.000") + " Gy to come");
        }
    }

    /// <summary>The body's half-life for what is inside; medicines that speed it out shorten it (Medical 0.24.0's Prussian blue).</summary>
    public static float HalfLifeInside(Pawn pawn) => InsideHalfLife?.Invoke(pawn) ?? Rules.insideHalfLifeDays;

    /// <summary>Set by the medicines: the half-life for a person, or null for the body's own.</summary>
    public static System.Func<Pawn, float>? InsideHalfLife;

    /// <summary>A wash: the skin's dust goes; with <paramref name="strip"/>, dusty clothes are dropped where the person stands, forbidden.</summary>
    public static void Wash(Pawn pawn, Hediff_PhobosRadiationDose? dose, bool strip)
    {
        dose ??= RadiationService.DoseOf(pawn, create: false);
        bool had = dose != null && dose.skinDust > 0f;
        if (dose != null) dose.skinDust = 0f;
        int dropped = 0;
        if (strip && pawn.Spawned && pawn.apparel?.WornApparel is { } worn)
        {
            var dusty = new List<Apparel>();
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i].TryGetComp<CompPhobosContamination>()?.Dusty == true) dusty.Add(worn[i]);
            }
            foreach (Apparel apparel in dusty)
            {
                if (pawn.apparel.TryDrop(apparel, out Apparel _, pawn.PositionHeld, forbid: true)) dropped++;
            }
            if (dropped > 0 && pawn.Faction == Faction.OfPlayer)
            {
                Messages.Message("PhobosRadiation_Stripped".Translate(pawn.Named("PAWN"), dropped.Named("COUNT")), pawn, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }
        if ((had || dropped > 0) && Seen.Wants("radiation.washed", pawn, strip ? "stripped" : "washed"))
        {
            Seen.Note("radiation.washed", pawn, dropped + " garments left", strip ? "stripped" : "washed");
        }
    }

    /// <summary>The tooltip's lines for dust carried and a dose from inside, or nothing.</summary>
    public static string Tip(Pawn pawn, Hediff_PhobosRadiationDose dose)
    {
        if (!On) return "";
        string text = "";
        if (SkinDust(dose) >= Rules.dustToWash || WearsDusty(pawn)) text += "\n" + "PhobosRadiation_TipDust".Translate();
        if (dose.inside >= 0.01f) text += "\n" + "PhobosRadiation_TipInside".Translate();
        return text;
    }

    // ---- The safety shower (the owner's choice: strip and wash, a setting, off by default) ----

    /// <summary>Called once at start-up: dusty colonists count as needing a rinse while strip and wash is on, and every rinse washes
    /// the dust off.</summary>
    internal static void Start()
    {
        Phobos.Medical.Services.RinseService.OtherNeeds.Add(pawn => RadiationPart.Settings.stripAndWash && Dusty(pawn));
        Phobos.Medical.Services.RinseService.OtherFinishes.Add(pawn => Wash(pawn, null, strip: RadiationPart.Settings.stripAndWash && On));
    }

    // ---- The thyroid's late effect ----

    /// <summary>From the late effects' judging: a thyroid cancer, from the thyroid's dose, under the Cancer switch.</summary>
    public static void JudgeThyroid(Pawn pawn, Hediff_PhobosRadiationDose dose, double strength, int today, int days)
    {
        if (dose.thyroidCancerDay != Hediff_PhobosRadiationDose.LateNone || dose.thyroid <= 0f) return;
        double chance = ContaminationRule.ThyroidCancerChance(dose.thyroid, pawn.ageTracker?.AgeBiologicalYearsFloat ?? 30f) * strength;
        if (RadiationService.Sensitivity(pawn, ThyroidChance) < chance) dose.thyroidCancerDay = today + days;
    }

    /// <summary>The purposes of each person's fixed thyroid draws. Never change one.</summary>
    public const int ThyroidChance = 14, ThyroidWhen = 15;
}
