using Phobos.Medical;
using Phobos.Radiation.Rules;
using RimWorld;
using Verse;

namespace Phobos.Radiation.Services;

/// <summary>
/// A person's Dubs Rimatomics radiation conditions become a dose in our model, once (D'thorga Medical 0.21.0; owner, 22 Nov 2026: "convert
/// once at load"). Its sickness counts 9 Gy a unit (the record's conversion); its fatal exposure as a dose past the lethal range; its
/// buildup, a tenth of every dose it gave, into the lifetime dose. Each is removed as it is converted, so this never runs twice for one
/// person. The course starts two days back, so a converted dose does not bring on early sickness again. One-way: switching the model off
/// later does not bring Rimatomics' conditions back. Robots keep theirs.
/// </summary>
public static class RimatomicsConversion
{
    static HediffDef? sickness;
    static HediffDef? fatal;
    static HediffDef? buildup;
    static bool looked;

    static void Look()
    {
        if (looked) return;
        looked = true;
        sickness = DefDatabase<HediffDef>.GetNamedSilentFail("RimatomicsRadiation");
        fatal = DefDatabase<HediffDef>.GetNamedSilentFail("FatalRad");
        buildup = DefDatabase<HediffDef>.GetNamedSilentFail("RadiationIncurable");
    }

    /// <summary>Whether this condition is one of Rimatomics' radiation conditions.</summary>
    public static bool IsRimatomics(HediffDef def)
    {
        Look();
        return def != null && (def == sickness || def == fatal || def == buildup);
    }

    /// <summary>Whether this person carries one of Rimatomics' radiation conditions, so the giver looks at them.</summary>
    public static bool HasAny(Pawn pawn)
    {
        Look();
        HediffSet set = pawn.health.hediffSet;
        return (sickness != null && set.HasHediff(sickness)) || (fatal != null && set.HasHediff(fatal)) || (buildup != null && set.HasHediff(buildup));
    }

    public static void ConvertOnce(Pawn pawn)
    {
        if (!HasAny(pawn) || !pawn.RaceProps.IsFlesh) return;
        HediffSet set = pawn.health.hediffSet;
        float grays = 0f, lifetime = 0f;
        if (sickness != null && set.GetFirstHediffOfDef(sickness) is { } s)
        {
            grays += s.Severity * (float)RadiationRule.GraysPerRimatomicsSickness;
            pawn.health.RemoveHediff(s);
        }
        if (fatal != null && set.GetFirstHediffOfDef(fatal) is { } f)
        {
            grays = System.Math.Max(grays, (float)((0.9 + f.Severity) * RadiationRule.GraysPerRimatomicsSickness));
            pawn.health.RemoveHediff(f);
        }
        if (buildup != null && set.GetFirstHediffOfDef(buildup) is { } b)
        {
            lifetime += b.Severity * 10f * (float)RadiationRule.GraysPerRimatomicsSickness;
            pawn.health.RemoveHediff(b);
        }
        if (grays <= 0f && lifetime <= 0f) return;
        Hediff_PhobosRadiationDose dose = RadiationService.DoseOf(pawn, create: true)!;
        int today = Find.TickManager.TicksGame / GenDate.TicksPerDay;
        if (grays > 0f) dose.AddDose(today, grays);
        dose.lifetime += lifetime;
        dose.lastSource = "converted";
        if (grays >= RadiationRule.CourseFrom)
        {
            dose.Severity = grays;
            dose.StartCourse(Find.TickManager.TicksGame - 2 * GenDate.TicksPerDay, vomits: false);
            dose.peak = grays;
        }
        if (Seen.Wants("radiation.converted", pawn)) Seen.Note("radiation.converted", pawn, grays.ToString("0.0") + " Gy, lifetime +" + lifetime.ToString("0.0"));
    }
}
