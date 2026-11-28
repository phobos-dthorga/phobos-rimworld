using System.Collections.Generic;
using Phobos.Radio.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Work;

/// <summary>
/// The watch's job, looked up when first wanted rather than as a DefOf: the radar's Defs load from their own folder only once its pictures
/// are in (Radio 0.74.0), and a DefOf would complain while they are not.
/// </summary>
public static class RadarWatchDefOf
{
    private static JobDef? watch;

    public static JobDef? PhobosRadio_RadarWatch => watch ??= DefDatabase<JobDef>.GetNamedSilentFail("PhobosRadio_RadarWatch");
}

/// <summary>
/// The radar watch (Radio 0.74.0; the owner's card of 28 Nov 2026: "power alone; crew helps"): while something on the colony's radar is
/// still unknown, a colonist at a radar console identifies it sooner, by their Intellectual skill. The "Radar watch" work type, so the player
/// chooses who; there is work only while something is unknown, so nobody sits there all day. It reads the radar picture only.
/// </summary>
public class WorkGiver_RadarWatch : WorkGiver_Scanner
{
    /// <summary>How long one stint on watch lasts before the colonist looks again at what else needs them (authored).</summary>
    private const int StintTicks = 2000;

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
        RadarWatchDefOf.PhobosRadio_RadarWatch == null || !MapComponent_PhobosRadar.For(pawn.Map).WantsWatch();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => MapComponent_PhobosRadar.For(pawn.Map).ConsoleThings;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) =>
        t.TryGetComp<CompRadarConsole>() is { Working: true } && !t.IsForbidden(pawn) && pawn.CanReserve(t, 1, -1, null, forced) && RadioSeats.CanSit(pawn, t);

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var job = JobMaker.MakeJob(RadarWatchDefOf.PhobosRadio_RadarWatch!, t);
        job.expiryInterval = StintTicks;
        job.checkOverrideOnExpire = true;
        return job;
    }
}

/// <summary>A colonist on watch at a radar console (Radio 0.74.0): seated as at a radio desk, while anything is still unknown.</summary>
public class JobDriver_RadarWatch : JobDriver
{
    private IntVec3 seat = IntVec3.Invalid;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) && RadioSeats.Claim(pawn, job, job.targetA.Thing, ref seat, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => job.targetA.Thing.TryGetComp<CompRadarConsole>() is not { Working: true } || !MapComponent_PhobosRadar.For(pawn.Map).WantsWatch());
        yield return RadioSeats.GoTo(job.targetA.Thing, () => seat);
        var watch = ToilMaker.MakeToil("PhobosRadio_RadarWatch");
        watch.initAction = () =>
        {
            RadioSeats.NoteSeated(pawn, job.targetA.Thing);
            MapComponent_PhobosRadar.For(pawn.Map).NoteWatch(pawn);
            if (Seen.Wants("radio.radar-watch", pawn)) Seen.Note("radio.radar-watch", pawn, "at " + job.targetA.Thing.LabelShort);
        };
        watch.handlingFacing = true;
        watch.tickIntervalAction = delta =>
        {
            RadioSeats.AtDesk(pawn, job.targetA.Thing, delta);
            if (pawn.IsHashIntervalTick(250, delta)) MapComponent_PhobosRadar.For(pawn.Map).NoteWatch(pawn);
            pawn.skills?.Learn(SkillDefOf.Intellectual, 0.02f * delta);
        };
        watch.defaultCompleteMode = ToilCompleteMode.Never;
        yield return watch;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref seat, "PhobosRadio_Seat", IntVec3.Invalid);
    }
}
