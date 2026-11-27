using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using Verse;

namespace Phobos.Dosing.Comps;

/// <summary>A vaccine's condition: which class of disease it primes the body against, and how far up immunity starts.</summary>
public class HediffCompProperties_PhobosPrimedImmunity : HediffCompProperties
{
    public PhobosDiseaseClassDef? diseaseClass;
    /// <summary>Where immunity starts when a disease of the class takes hold: a third, the owner's card of 27 Nov 2026.</summary>
    public float headStart = 0.33f;
    /// <summary>How often the body is checked for a new disease of the class. Authored: a few seconds of game time, against days of disease.</summary>
    public int checkIntervalTicks = 250;

    public HediffCompProperties_PhobosPrimedImmunity() => compClass = typeof(HediffComp_PhobosPrimedImmunity);
}

/// <summary>
/// While the vaccine lasts, a disease of its class that takes hold has its immunity record raised to the head start once (Medical
/// 0.35.0). The game's own record and race go on from there: nothing else changes, and no Harmony is needed. Remembers which diseases it
/// has primed, so a bout is helped once.
/// </summary>
public class HediffComp_PhobosPrimedImmunity : HediffComp
{
    List<HediffDef> primed = new();

    public HediffCompProperties_PhobosPrimedImmunity Props => (HediffCompProperties_PhobosPrimedImmunity)props;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        if (Props.diseaseClass == null || !Pawn.IsHashIntervalTick(Props.checkIntervalTicks, delta)) return;
        List<Hediff> hediffs = Pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            HediffDef disease = hediffs[i].def;
            if (disease == parent.def || primed.Contains(disease) || DiseaseClasses.Of(disease) != Props.diseaseClass) continue;
            ImmunityRecord? record = Pawn.health.immunity.GetImmunityRecord(disease);
            if (record == null) continue;
            float raised = PrimedImmunityRule.Raised(record.immunity, Props.headStart);
            if (raised > record.immunity) record.immunity = raised;
            primed.Add(disease);
            if (Medical.Seen.Wants("medical.vaccine-head-start", Pawn)) Medical.Seen.Note("medical.vaccine-head-start", Pawn, disease.defName + " from " + parent.def.defName);
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Collections.Look(ref primed, "PhobosPrimedImmunity_Primed", LookMode.Def);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            primed ??= new List<HediffDef>();
            primed.RemoveAll(def => def == null);
        }
    }
}
