using System;
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
/// Jump-starts: finding a donor, a kit, a booster pack and a place to stand, on a map or in a caravan (the service the float menu and
/// the caravan gizmo call). Two player settings (owner, 9 Nov 2026): the leads must be a crafted jump-start kit (default on), and every
/// vehicle may be assumed to carry a booster pack, so no donor is needed (default off). A crafted booster pack with a start left serves
/// without either (docs/development/vehicle-crew-and-ground-design.md).
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

    public const string PackDefName = "PhobosVehicleSystems_BoosterPack";
    public static ThingDef? PackDef => DefDatabase<ThingDef>.GetNamedSilentFail(PackDefName);

    static bool Charged(Thing thing) => thing.TryGetComp<CompPhobosCharges>()?.CanUse == true;

    /// <summary>The nearest booster pack with a start left the helper can fetch, or null.</summary>
    public static Thing? FindPack(Pawn helper) => PackDef is { } def ? ItemFinder.FindNearest(helper, def, validator: Charged) : null;

    /// <summary>A booster pack with a start left aboard the caravan, or null.</summary>
    public static Thing? FindPackInCaravan(Caravan caravan) => PackDef is { } def ? ItemFinder.FindInCaravan(caravan, def, Charged) : null;

    /// <summary>
    /// How a jump-start would be had, best first: from a donor vehicle with the leads (a kit, when the setting asks for one); from a
    /// booster pack with a start left (it carries its own clamps, so no kit); from the booster pack every vehicle is assumed to carry
    /// (the setting). When none is possible, the refusal says what is missing: the kit when a donor is there, else a donor or a pack.
    /// </summary>
    public static JumpStartRoute Plan(VehiclePawn? donor, Thing? kit, Func<Thing?> findPack)
    {
        if (donor != null && (!NeedsKit || kit != null)) return new JumpStartRoute { Donor = donor, Kit = NeedsKit ? kit : null };
        Thing? pack = findPack();
        if (pack != null) return new JumpStartRoute { Pack = pack };
        if (DonorOptional) return new JumpStartRoute { Assumed = true };
        return new JumpStartRoute { Missing = donor != null ? JumpStartMissing.Kit : JumpStartMissing.Donor };
    }

    /// <summary>A standable cell beside the flat vehicle the helper can reach, or Invalid.</summary>
    public static IntVec3 WorkCell(Pawn helper, VehiclePawn flat) =>
        flat.SurroundingCells.Where(c => c.InBounds(flat.Map) && c.Standable(flat.Map) && helper.CanReach(c, PathEndMode.OnCell, Danger.Some))
            .OrderBy(c => c.DistanceToSquared(helper.Position)).FirstOrFallback(IntVec3.Invalid);

    /// <summary>Orders the job: A the flat vehicle, B what the helper carries (the kit or the booster pack, or nothing), C the cell; the
    /// donor (or nothing) rides in the first queue.</summary>
    public static void Order(Pawn helper, VehiclePawn flat, JumpStartRoute route, IntVec3 cell)
    {
        Thing? carried = route.Pack ?? route.Kit;
        Job job = JobMaker.MakeJob(VehicleSystemsJobDefOf.PhobosVehicleSystems_JumpStart, flat, carried != null ? carried : LocalTargetInfo.Invalid, cell);
        // The game's carry toil takes job.count items and throws on the default of -1.
        job.count = 1;
        if (route.Donor != null) job.targetQueueA = new List<LocalTargetInfo> { route.Donor };
        helper.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }

    /// <summary>
    /// The caravan's jump-start button for a vehicle that needs one, disabled with the reason when it cannot be had (no kit aboard for
    /// the vehicle that could give the charge, no vehicle with the charge and no booster pack with a start, nobody able). Null when the
    /// vehicle needs none. Caravans have no jobs, so the start is instant (agent default).
    /// </summary>
    public static Gizmo? CaravanGizmo(VehicleCaravan caravan, CompPhobosVehicleSystems flat)
    {
        SystemState? need = flat.JumpStartNeed();
        if (need == null) return null;
        VehiclePawn? donor = FindCaravanDonor(caravan, flat.Vehicle, need.Def);
        JumpStartRoute route = Plan(donor, NeedsKit ? FindKitInCaravan(caravan) : null, () => FindPackInCaravan(caravan));
        NamedArgument vehicle = flat.Vehicle.LabelShort.Named("VEHICLE");
        string label = route.Donor != null
            ? "PhobosVehicleSystems_CaravanJumpStart".Translate(vehicle, route.Donor.LabelShort.Named("DONOR")).Resolve()
            : route.Pack != null
                ? "PhobosVehicleSystems_CaravanJumpStartPack".Translate(vehicle).Resolve()
                : "PhobosVehicleSystems_CaravanJumpStartBooster".Translate(vehicle).Resolve();
        var command = new Command_Action
        {
            defaultLabel = label,
            defaultDesc = "PhobosVehicleSystems_CaravanJumpStartDesc".Translate(vehicle).Resolve(),
            icon = SystemIcons.For(route.Pack != null ? "Phobos/VehicleSystems/Things/BoosterPack" : "Phobos/VehicleSystems/Things/JumpStartKit"),
        };
        Pawn? helper = caravan.PawnsListForReading.FirstOrDefault(p => p is not VehiclePawn && p.IsColonist && !p.Downed);
        if (route.Missing == JumpStartMissing.Kit)
        {
            command.Disable("PhobosVehicleSystems_CannotJumpStartNoKitCaravan".Translate(vehicle).Resolve());
        }
        else if (route.Missing == JumpStartMissing.Donor)
        {
            command.Disable("PhobosVehicleSystems_CannotJumpStartNoDonorCaravan".Translate(vehicle).Resolve());
        }
        else if (helper == null)
        {
            command.Disable("PhobosVehicleSystems_CannotJumpStartNoHelper".Translate(vehicle).Resolve());
        }
        command.action = () =>
        {
            CompPhobosVehicleSystems? donorComp = route.Donor?.GetComp<CompPhobosVehicleSystems>();
            if (helper == null || !flat.ReceiveJumpStart(donorComp, helper, route.Pack))
            {
                Messages.Message("PhobosVehicleSystems_JumpStartFailed".Translate((helper?.LabelShort ?? "").Named("HELPER"), vehicle,
                    (route.Donor?.LabelShort ?? "").Named("DONOR")), caravan, MessageTypeDefOf.RejectInput, historical: false);
            }
        };
        return command;
    }
}

/// <summary>What a jump-start would be missing, when it cannot be had.</summary>
public enum JumpStartMissing
{
    None,
    Kit,
    Donor,
}

/// <summary>How a jump-start would be had: from a donor (with a kit or not), a booster pack, or the assumed pack; or what is missing.</summary>
public struct JumpStartRoute
{
    public VehiclePawn? Donor;
    public Thing? Kit;
    public Thing? Pack;
    public bool Assumed;
    public JumpStartMissing Missing;
}

/// <summary>
/// "Jump-start the truck from the jeep" on a vehicle whose battery is too low to start it, for a colonist who can use their hands.
/// Every refusal says why: no kit in stock for the donor near (when the leads must be crafted), no donor near and no booster pack with
/// a start (unless one is assumed), nowhere to stand.
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
        VehiclePawn? donor = JumpStarts.FindDonor(flat, need.Def);
        JumpStartRoute route = JumpStarts.Plan(donor, JumpStarts.NeedsKit && donor != null ? JumpStarts.FindKit(helper) : null, () => JumpStarts.FindPack(helper));
        if (route.Missing == JumpStartMissing.Kit) return new FloatMenuOption("PhobosVehicleSystems_CannotJumpStartNoKit".Translate(vehicle), null);
        if (route.Missing == JumpStartMissing.Donor)
        {
            return new FloatMenuOption("PhobosVehicleSystems_CannotJumpStartNoDonor".Translate(vehicle, spec.reach.ToString("0").Named("REACH")), null);
        }
        IntVec3 cell = JumpStarts.WorkCell(helper, flat);
        if (!cell.IsValid)
        {
            return new FloatMenuOption("PhobosVehicleSystems_CannotJumpStartNoPath".Translate(vehicle), null);
        }
        string label = route.Donor != null
            ? "PhobosVehicleSystems_JumpStartOption".Translate(vehicle, route.Donor.LabelShort.Named("DONOR"))
            : route.Pack != null
                ? "PhobosVehicleSystems_JumpStartOptionPack".Translate(vehicle)
                : "PhobosVehicleSystems_JumpStartOptionBooster".Translate(vehicle);
        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, () => JumpStarts.Order(helper, flat, route, cell)), helper, flat);
    }
}

/// <summary>
/// Fetches the kit or the booster pack when one is used, walks to the flat vehicle, connects the leads and cranks it, then moves the
/// charge (from the donor, the booster pack, or the assumed pack) and puts what it carried down beside the vehicle (a kit is never used
/// up; a pack spends a start). Fails if the donor leaves.
/// </summary>
public class JobDriver_PhobosJumpStart : JobDriver
{
    const TargetIndex FlatIndex = TargetIndex.A;
    const TargetIndex KitIndex = TargetIndex.B;
    const TargetIndex CellIndex = TargetIndex.C;

    VehiclePawn Flat => (VehiclePawn)job.GetTarget(FlatIndex).Thing;
    VehiclePawn? Donor => job.targetQueueA != null && job.targetQueueA.Count > 0 ? job.targetQueueA[0].Thing as VehiclePawn : null;
    bool HasKit => job.GetTarget(KitIndex).HasThing;
    /// <summary>The booster pack the helper carries, when that is what B is (a kit has no charges).</summary>
    Thing? Pack => job.GetTarget(KitIndex).Thing is { } thing && thing.TryGetComp<CompPhobosCharges>() != null ? thing : null;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        !HasKit || pawn.Reserve(job.GetTarget(KitIndex), job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(FlatIndex);
        this.FailOn(() => Flat.GetComp<CompPhobosVehicleSystems>()?.JumpStartNeed() == null);
        this.FailOn(() => Donor != null && (!Donor.Spawned || Donor.Destroyed));
        if (HasKit)
        {
            // Only on the walk to it: once carried the thing is no longer spawned, and a job-wide despawn check would end the job at
            // the pick-up (the game's own haul toils attach it the same way).
            yield return Toils_Goto.GotoThing(KitIndex, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(KitIndex)
                .FailOn(() => Pack != null && Pack.TryGetComp<CompPhobosCharges>()?.CanUse != true);
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
                if (flat == null || !flat.ReceiveJumpStart(donor, pawn, Pack))
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
