using System.Collections.Generic;
using System.Linq;
using Phobos.Medical.Comps;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>One pass of a treating building: who it treated, and who it found in its beds but left alone because they don't need it.
/// The second list is what lets the inspect text say why a patient gets nothing. Derived, so never saved.</summary>
public sealed class TreatmentPass
{
    public static readonly TreatmentPass None = new(new List<Pawn>(), new List<Pawn>());

    public readonly List<Pawn> Treated;
    public readonly List<Pawn> NotNeeding;

    public TreatmentPass(List<Pawn> treated, List<Pawn> notNeeding)
    {
        Treated = treated;
        NotNeeding = notNeeding;
    }

    public static string Names(List<Pawn> pawns) => string.Join(", ", pawns.Select(p => p.LabelShort));
}

/// <summary>Starting or keeping up a treatment condition. It lapses by itself once nothing refreshes it.</summary>
public static class MedicalTreatment
{
    public static void GiveOrRefresh(Pawn patient, HediffDef treatment)
    {
        Hediff? condition = patient.health.hediffSet.GetFirstHediffOfDef(treatment);
        condition ??= patient.health.AddHediff(treatment);
        condition?.TryGetComp<HediffComp_PhobosTreatment>()?.Refresh();
    }
}
