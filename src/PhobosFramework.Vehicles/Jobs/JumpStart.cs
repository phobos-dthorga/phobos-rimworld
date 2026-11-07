using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>Finding a donor and a place to stand for a jump-start (the service the float menu calls).</summary>
public static class JumpStarts
{
    /// <summary>The nearest of the player's vehicles within the system's reach that can give a jump-start, or null.</summary>
    public static VehiclePawn? FindDonor(VehiclePawn flat, PhobosVehicleSystemDef system)
    {
        if (!flat.Spawned || system.jumpStart == null) return null;
        VehiclePawn? best = null;
        float bestDistance = float.MaxValue;
        foreach (Pawn pawn in flat.Map.mapPawns.SpawnedPawnsInFaction(flat.Faction))
        {
            if (pawn == flat || pawn is not VehiclePawn candidate) continue;
            float distance = candidate.Position.DistanceTo(flat.Position);
            if (distance > system.jumpStart.reach || distance >= bestDistance) continue;
            if (candidate.GetComp<CompPhobosVehicleSystems>()?.CanDonate(system) != true) continue;
            best = candidate;
            bestDistance = distance;
        }
        return best;
    }

    /// <summary>A standable cell beside the flat vehicle the helper can reach, or Invalid.</summary>
    public static IntVec3 WorkCell(Pawn helper, VehiclePawn flat) =>
        flat.SurroundingCells.Where(c => c.InBounds(flat.Map) && c.Standable(flat.Map) && helper.CanReach(c, PathEndMode.OnCell, Danger.Some))
            .OrderBy(c => c.DistanceToSquared(helper.Position)).FirstOrFallback(IntVec3.Invalid);

    public static void Order(Pawn helper, VehiclePawn flat, VehiclePawn donor, IntVec3 cell)
    {
        Job job = JobMaker.MakeJob(VehicleSystemsJobDefOf.PhobosVehicleSystems_JumpStart, flat, donor, cell);
        helper.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }
}

/// <summary>
/// "Jump-start the truck from the jeep" on a vehicle whose battery is too low to start it, for a colonist who can use their hands.
/// Every refusal says why.
/// </summary>
public class FloatMenuOptionProvider_PhobosJumpStart : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context) =>
        pawn is not VehiclePawn && pawn.RaceProps.Humanlike && base.SelectedPawnValid(pawn, context);

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (clickedPawn is not VehiclePawn flat || flat.Faction != Faction.OfPlayer) return null!;
        SystemState? need = flat.GetComp<CompPhobosVehicleSystems>()?.JumpStartNeed();
        JumpStartSpec? spec = need?.Def.jumpStart;
        if (need == null || spec == null) return null!;
        Pawn helper = context.FirstSelectedPawn;
        VehiclePawn? donor = JumpStarts.FindDonor(flat, need.Def);
        if (donor == null)
        {
            return new FloatMenuOption("PhobosVehicleSystems_CannotJumpStartNoDonor".Translate(flat.LabelShort.Named("VEHICLE"),
                spec.reach.ToString("0").Named("REACH")), null);
        }
        IntVec3 cell = JumpStarts.WorkCell(helper, flat);
        if (!cell.IsValid)
        {
            return new FloatMenuOption("PhobosVehicleSystems_CannotJumpStartNoPath".Translate(flat.LabelShort.Named("VEHICLE")), null);
        }
        string label = "PhobosVehicleSystems_JumpStartOption".Translate(flat.LabelShort.Named("VEHICLE"), donor.LabelShort.Named("DONOR"));
        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, () => JumpStarts.Order(helper, flat, donor, cell)), helper, flat);
    }
}

/// <summary>Walks to the flat vehicle, connects the leads and cranks it, then moves the charge. Fails if the donor leaves.</summary>
public class JobDriver_PhobosJumpStart : JobDriver
{
    const TargetIndex FlatIndex = TargetIndex.A;
    const TargetIndex DonorIndex = TargetIndex.B;
    const TargetIndex CellIndex = TargetIndex.C;

    VehiclePawn Flat => (VehiclePawn)job.GetTarget(FlatIndex).Thing;
    VehiclePawn Donor => (VehiclePawn)job.GetTarget(DonorIndex).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(FlatIndex);
        this.FailOnDespawnedOrNull(DonorIndex);
        this.FailOn(() => Flat.GetComp<CompPhobosVehicleSystems>()?.JumpStartNeed() == null);
        yield return Toils_Goto.GotoCell(CellIndex, PathEndMode.OnCell);
        int work = Flat.GetComp<CompPhobosVehicleSystems>()?.JumpStartNeed()?.Def.jumpStart?.work ?? 300;
        Toil connect = Toils_General.Wait(work, FlatIndex);
        connect.WithProgressBarToilDelay(FlatIndex);
        connect.FailOn(() => Donor.Position.DistanceTo(Flat.Position) > (Flat.GetComp<CompPhobosVehicleSystems>()?.JumpStartNeed()?.Def.jumpStart?.reach ?? 8f));
        yield return connect;
        yield return new Toil
        {
            initAction = () =>
            {
                CompPhobosVehicleSystems? flat = Flat.GetComp<CompPhobosVehicleSystems>();
                CompPhobosVehicleSystems? donor = Donor.GetComp<CompPhobosVehicleSystems>();
                if (flat == null || donor == null || !flat.ReceiveJumpStart(donor, pawn))
                {
                    Messages.Message("PhobosVehicleSystems_JumpStartFailed".Translate(pawn.LabelShort.Named("HELPER"), Flat.LabelShort.Named("VEHICLE"),
                        Donor.LabelShort.Named("DONOR")), Flat, MessageTypeDefOf.RejectInput, historical: false);
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant,
        };
    }
}
