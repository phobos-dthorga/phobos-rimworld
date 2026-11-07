using System.Collections.Generic;
using System.Linq;
using Phobos.Triage.Services;
using RimWorld;
using Verse;

namespace Phobos.Triage.Jobs;

/// <summary>
/// The game's own operation giver, told whom to go to first: a prioritised scan over the patients in bed with a bill, scored
/// by triage, so an idle doctor goes to the worst case rather than the nearest. Everything else (the bed, the skill, the
/// medicine, the reservation, the job) is the game's. It sits just above the game's operate giver in the Doctor work type and
/// yields nothing when triage ordering is off, so the game's giver then does what it always did.
/// </summary>
public class WorkGiver_PhobosTriageOperate : WorkGiver_DoBill
{
    public override bool Prioritized => true;

    public override float GetPriority(Pawn pawn, TargetInfo t) =>
        t.Thing is Pawn patient ? MapComponent_PhobosTriage.For(patient.Map)?.ScoreOf(patient) ?? 0f : 0f;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) =>
        MapComponent_PhobosTriage.For(pawn.Map)?.PatientsWithBills ?? Enumerable.Empty<Thing>();

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (!TriageMod.Settings.triageOrdering) return true;
        MapComponent_PhobosTriage? triage = MapComponent_PhobosTriage.For(pawn.Map);
        return triage == null || triage.PatientsWithBills.Count == 0;
    }
}
