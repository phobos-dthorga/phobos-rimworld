using System.Collections.Generic;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// Right-click a vehicle of the player's with a colonist selected (Framework 0.90.0; owner, 11 Nov 2026: tuning was too hard to find, and the
/// right-click menu is "simple and unobtrusive"): "Fittings and tunes..." opens the window, and when work waits that this colonist can do,
/// "Do the waiting work" gives them the job at once, as the game prioritises any task. A refusal says why. Presentation only: the job
/// is the repair work giver's own, asked for as a forced job.
/// </summary>
public class FloatMenuOptionProvider_PhobosFittings : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context) =>
        pawn is not VehiclePawn && pawn.RaceProps.Humanlike && pawn.IsColonistPlayerControlled && base.SelectedPawnValid(pawn, context);

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (clickedPawn is not VehiclePawn vehicle || vehicle.Faction != Faction.OfPlayer) yield break;
        if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } comp || !comp.Fittings.AnythingToFit) yield break;
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        yield return new FloatMenuOption("PhobosVehicleSystems_FitMenuOpen".Translate(name), () => Find.WindowStack.Add(new Dialog_PhobosVehicleFittings(comp)));
        if (comp.Fittings.Orders.Count == 0) yield break;

        Pawn mechanic = context.FirstSelectedPawn;
        RepairWork? ready = comp.Repairs.Ready;
        if (ready == null)
        {
            // Ordered, but not ready: say what it waits for, as the window's line does.
            RepairWork? waiting = comp.Repairs.Pending.Count > 0 ? comp.Repairs.Pending[0] : null;
            string reason = waiting != null ? comp.Repairs.NeedsLine(waiting) : comp.Fittings.RigMissing ? "PhobosVehicleSystems_FitNoRigNote".Translate(VehicleFittings.RigReach.Named("CELLS")).Resolve() : "";
            yield return new FloatMenuOption("PhobosVehicleSystems_FitMenuCannot".Translate(name, reason.Named("REASON")), null);
            yield break;
        }
        WorkGiverDef? giverDef = DefDatabase<WorkGiverDef>.GetNamedSilentFail("PhobosVehicleSystems_RepairWithSupplies");
        if (giverDef?.Worker is not WorkGiver_Scanner giver) yield break;
        if (mechanic.WorkTypeIsDisabled(giverDef.workType))
        {
            yield return new FloatMenuOption("PhobosVehicleSystems_FitMenuCannot".Translate(name, "PhobosVehicleSystems_FitMenuNoWorkType".Translate(giverDef.workType.gerundLabel.Named("WORK")).Named("REASON")), null);
            yield break;
        }
        JobFailReason.Clear();
        Job? job = giver.JobOnThing(mechanic, vehicle, forced: true);
        if (job == null)
        {
            string reason = JobFailReason.HaveReason ? JobFailReason.Reason : "PhobosVehicleSystems_FitMenuNoPath".Translate().Resolve();
            yield return new FloatMenuOption("PhobosVehicleSystems_FitMenuCannot".Translate(name, reason.Named("REASON")), null);
            yield break;
        }
        job.workGiverDef = giverDef;
        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PhobosVehicleSystems_FitMenuDoWork".Translate(name, ready.Label.Named("WORK")),
            () => mechanic.jobs.TryTakeOrderedJob(job, JobTag.Misc)), mechanic, vehicle);
    }
}
