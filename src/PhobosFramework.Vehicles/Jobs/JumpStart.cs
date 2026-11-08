using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Items;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// Jump-starts: finding a donor, a kit and a place to stand, on a map or in a caravan (the service the float menu and the caravan
/// gizmo call). Two player settings (owner, 9 Nov 2026): the leads must be a crafted jump-start kit (default on), and every vehicle
/// may be assumed to carry a booster pack, so no donor is needed (default off).
/// </summary>
public static class JumpStarts
{
    public const string KitDefName = "PhobosVehicleSystems_JumpStartKit";

    public static bool NeedsKit => VehicleSystemsMod.Settings.FlagOn("jumpStartNeedsKit");
    public static bool DonorOptional => VehicleSystemsMod.Settings.FlagOn("assumeBoosterPack");
    public static ThingDef? KitDef => DefDatabase<ThingDef>.GetNamedSilentFail(KitDefName);

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

    /// <summary>Another vehicle of the caravan that can give the jump-start, or null.</summary>
    public static VehiclePawn? FindCaravanDonor(VehicleCaravan caravan, VehiclePawn flat, PhobosVehicleSystemDef system) =>
        caravan.VehiclesListForReading.FirstOrDefault(v => v != flat && v.GetComp<CompPhobosVehicleSystems>()?.CanDonate(system) == true);

    /// <summary>The nearest jump-start kit the helper can fetch, or null (always null when the setting says the leads are free).</summary>
    public static Thing? FindKit(Pawn helper) => KitDef is { } def ? ItemFinder.FindNearest(helper, def) : null;

    /// <summary>A jump-start kit aboard the caravan, or null.</summary>
    public static Thing? FindKitInCaravan(Caravan caravan) => KitDef is { } def ? ItemFinder.FindInCaravan(caravan, def) : null;

    /// <summary>A standable cell beside the flat vehicle the helper can reach, or Invalid.</summary>
    public static IntVec3 WorkCell(Pawn helper, VehiclePawn flat) =>
        flat.SurroundingCells.Where(c => c.InBounds(flat.Map) && c.Standable(flat.Map) && helper.CanReach(c, PathEndMode.OnCell, Danger.Some))
            .OrderBy(c => c.DistanceToSquared(helper.Position)).FirstOrFallback(IntVec3.Invalid);

    /// <summary>Orders the job: A the flat vehicle, B the kit (or nothing), C the cell; the donor (or nothing) rides in the first queue.</summary>
    public static void Order(Pawn helper, VehiclePawn flat, VehiclePawn? donor, IntVec3 cell, Thing? kit)
    {
        Job job = JobMaker.MakeJob(VehicleSystemsJobDefOf.PhobosVehicleSystems_JumpStart, flat, kit != null ? kit : LocalTargetInfo.Invalid, cell);
        if (donor != null) job.targetQueueA = new List<LocalTargetInfo> { donor };
        helper.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }

    /// <summary>
    /// The caravan's jump-start button for a vehicle that needs one, disabled with the reason when it cannot be had: no kit aboard,
    /// or no other vehicle in the caravan with the charge (unless every vehicle is assumed to carry a booster pack). Null when the
    /// vehicle needs none. Caravans have no jobs, so the start is instant (agent default).
    /// </summary>
    public static Gizmo? CaravanGizmo(VehicleCaravan caravan, CompPhobosVehicleSystems flat)
    {
        SystemState? need = flat.JumpStartNeed();
        if (need == null) return null;
        VehiclePawn? donor = FindCaravanDonor(caravan, flat.Vehicle, need.Def);
        bool boosted = donor == null && DonorOptional;
        NamedArgument vehicle = flat.Vehicle.LabelShort.Named("VEHICLE");
        var command = new Command_Action
        {
            defaultLabel = boosted
                ? "PhobosVehicleSystems_CaravanJumpStartBooster".Translate(vehicle).Resolve()
                : "PhobosVehicleSystems_CaravanJumpStart".Translate(vehicle, (donor?.LabelShort ?? "").Named("DONOR")).Resolve(),
            defaultDesc = "PhobosVehicleSystems_CaravanJumpStartDesc".Translate(vehicle).Resolve(),
            icon = SystemIcons.For("Phobos/VehicleSystems/Things/JumpStartKit"),
        };
        Pawn? helper = caravan.PawnsListForReading.FirstOrDefault(p => p is not VehiclePawn && p.IsColonist && !p.Downed);
        if (NeedsKit && FindKitInCaravan(caravan) == null)
        {
            command.Disable("PhobosVehicleSystems_CannotJumpStartNoKitCaravan".Translate(vehicle).Resolve());
        }
        else if (donor == null && !DonorOptional)
        {
            command.Disable("PhobosVehicleSystems_CannotJumpStartNoDonorCaravan".Translate(vehicle).Resolve());
        }
        else if (helper == null)
        {
            command.Disable("PhobosVehicleSystems_CannotJumpStartNoHelper".Translate(vehicle).Resolve());
        }
        command.action = () =>
        {
            CompPhobosVehicleSystems? donorComp = donor?.GetComp<CompPhobosVehicleSystems>();
            if (helper == null || !flat.ReceiveJumpStart(donorComp, helper))
            {
                Messages.Message("PhobosVehicleSystems_JumpStartFailed".Translate((helper?.LabelShort ?? "").Named("HELPER"), vehicle,
                    (donor?.LabelShort ?? "").Named("DONOR")), caravan, MessageTypeDefOf.RejectInput, historical: false);
            }
        };
        return command;
    }
}

/// <summary>
/// "Jump-start the truck from the jeep" on a vehicle whose battery is too low to start it, for a colonist who can use their hands.
/// Every refusal says why: no kit in stock (when the leads must be crafted), no donor near (unless a booster pack is assumed), nowhere
/// to stand.
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
        NamedArgument vehicle = flat.LabelShort.Named("VEHICLE");
        Thing? kit = null;
        if (JumpStarts.NeedsKit)
        {
            kit = JumpStarts.FindKit(helper);
            if (kit == null) return new FloatMenuOption("PhobosVehicleSystems_CannotJumpStartNoKit".Translate(vehicle), null);
        }
        VehiclePawn? donor = JumpStarts.FindDonor(flat, need.Def);
        if (donor == null && !JumpStarts.DonorOptional)
        {
            return new FloatMenuOption("PhobosVehicleSystems_CannotJumpStartNoDonor".Translate(vehicle, spec.reach.ToString("0").Named("REACH")), null);
        }
        IntVec3 cell = JumpStarts.WorkCell(helper, flat);
        if (!cell.IsValid)
        {
            return new FloatMenuOption("PhobosVehicleSystems_CannotJumpStartNoPath".Translate(vehicle), null);
        }
        string label = donor != null
            ? "PhobosVehicleSystems_JumpStartOption".Translate(vehicle, donor.LabelShort.Named("DONOR"))
            : "PhobosVehicleSystems_JumpStartOptionBooster".Translate(vehicle);
        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, () => JumpStarts.Order(helper, flat, donor, cell, kit)), helper, flat);
    }
}

/// <summary>
/// Fetches the kit when one is needed, walks to the flat vehicle, connects the leads and cranks it, then moves the charge (from the
/// donor, or from the assumed booster pack) and puts the kit down beside the vehicle, never used up. Fails if the donor leaves.
/// </summary>
public class JobDriver_PhobosJumpStart : JobDriver
{
    const TargetIndex FlatIndex = TargetIndex.A;
    const TargetIndex KitIndex = TargetIndex.B;
    const TargetIndex CellIndex = TargetIndex.C;

    VehiclePawn Flat => (VehiclePawn)job.GetTarget(FlatIndex).Thing;
    VehiclePawn? Donor => job.targetQueueA != null && job.targetQueueA.Count > 0 ? job.targetQueueA[0].Thing as VehiclePawn : null;
    bool HasKit => job.GetTarget(KitIndex).HasThing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        !HasKit || pawn.Reserve(job.GetTarget(KitIndex), job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(FlatIndex);
        this.FailOn(() => Flat.GetComp<CompPhobosVehicleSystems>()?.JumpStartNeed() == null);
        this.FailOn(() => Donor != null && (!Donor.Spawned || Donor.Destroyed));
        if (HasKit)
        {
            this.FailOnDespawnedNullOrForbidden(KitIndex);
            yield return Toils_Goto.GotoThing(KitIndex, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(KitIndex);
        }
        yield return Toils_Goto.GotoCell(CellIndex, PathEndMode.OnCell);
        int work = Flat.GetComp<CompPhobosVehicleSystems>()?.JumpStartNeed()?.Def.jumpStart?.work ?? 300;
        Toil connect = Toils_General.Wait(work, FlatIndex);
        connect.WithProgressBarToilDelay(FlatIndex);
        connect.FailOn(() => Donor != null && Donor.Position.DistanceTo(Flat.Position) > (Flat.GetComp<CompPhobosVehicleSystems>()?.JumpStartNeed()?.Def.jumpStart?.reach ?? 8f));
        yield return connect;
        yield return new Toil
        {
            initAction = () =>
            {
                CompPhobosVehicleSystems? flat = Flat.GetComp<CompPhobosVehicleSystems>();
                CompPhobosVehicleSystems? donor = Donor?.GetComp<CompPhobosVehicleSystems>();
                if (flat == null || !flat.ReceiveJumpStart(donor, pawn))
                {
                    Messages.Message("PhobosVehicleSystems_JumpStartFailed".Translate(pawn.LabelShort.Named("HELPER"), Flat.LabelShort.Named("VEHICLE"),
                        (Donor?.LabelShort ?? "").Named("DONOR")), Flat, MessageTypeDefOf.RejectInput, historical: false);
                }
                if (pawn.carryTracker.CarriedThing != null) pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
            },
            defaultCompleteMode = ToilCompleteMode.Instant,
        };
    }
}
