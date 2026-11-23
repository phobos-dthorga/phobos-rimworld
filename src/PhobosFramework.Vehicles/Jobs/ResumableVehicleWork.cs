using System.Collections.Generic;
using Phobos.Framework.Persistence;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// Work at a vehicle that is picked up where it was left (Framework 0.274.0; the owner's report of 24 Nov 2026: setting up the Billy began
/// again from nothing each time a colonist came back to it). Vehicle Framework's JobDriver_WorkVehicle keeps the work left on the job
/// itself and sets it back to the whole in ResetWork, as the work step starts, so any interruption (a meal, sleep, a raid, a draft) lost
/// it, and it saved none, so a load finished the job at once (read from Vehicle Framework's source, JobDriver_WorkVehicle, 24 Nov 2026).
/// Here the work left is kept on the vehicle through <see cref="ResumableWork"/> when the job ends early, read back when the work starts
/// again, forgotten once it is finished, and saved with the job.
/// </summary>
public abstract class PhobosResumableVehicleWork : JobDriver_WorkVehicle
{
    float work;
    float total;
    string? resumeKey;

    protected override float Work
    {
        get => work;
        set => work = value;
    }

    /// <summary>Which order of this job the vehicle waits for, where it can wait for more than one (set up or pack up, one repair or
    /// another): each keeps its own work. Empty where there is only ever one.</summary>
    protected virtual string ResumeOrder => "";

    /// <summary>Whether the order this job works on still stands as the job ends. An order the player called off keeps no work (the owner,
    /// 24 Nov 2026: "keeping progress on cancel is maybe a bit much"); one interrupted by a meal, sleep or a raid does.</summary>
    protected virtual bool OrderStands => true;

    static string KeyFor(VehiclePawn vehicle, JobDef job, string order) => ResumableWork.Key(vehicle, job.defName + "|" + order);

    /// <summary>The player called an order off: its kept work is forgotten, so giving it again starts afresh. Called from each order's
    /// cancel; a job at work on it then ends without keeping any (<see cref="OrderStands"/>).</summary>
    public static void ForgetOrder(VehiclePawn? vehicle, JobDef job, string order = "")
    {
        if (vehicle != null) ResumableWork.Forget(KeyFor(vehicle, job, order));
    }

    protected override void ResetWork()
    {
        resumeKey = KeyFor(Vehicle, job.def, ResumeOrder);
        total = TotalWork;
        work = ResumableWork.WorkLeft(resumeKey, total);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // Added again whenever the toils are made, which the game also does for a job loaded from a save.
        AddFinishAction(_ => Remember());
        foreach (Toil toil in base.MakeNewToils()) yield return toil;
    }

    /// <summary>The job ended, finished or not: keep what is left for the next person, or forget work finished or called off.</summary>
    void Remember()
    {
        if (resumeKey == null) return;
        if (work <= 0f || !OrderStands) ResumableWork.Forget(resumeKey);
        else ResumableWork.Keep(resumeKey, work, total);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        // The first key is the one PhobosVehicleWorkDriver saved before, so its jobs saved mid-work load as they were.
        Scribe_Values.Look(ref work, "PhobosVehicleSystems_Work");
        Scribe_Values.Look(ref total, "PhobosVehicleSystems_WorkTotal");
        Scribe_Values.Look(ref resumeKey, "PhobosVehicleSystems_ResumeKey");
    }
}
