using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// Claiming another faction's vehicle (owner, 11 Nov 2026): one left unattended, with nobody aboard and none of its faction's armed people
/// standing near, may be made ours by a colonist working on it for a while, longer for bigger vehicles. A vehicle of a faction that is not
/// hostile may be taken too, at a cost in goodwill, as the player chooses. Vehicle Framework has a claim rule of its own, but no vehicle
/// Def is claimable by the game's Claim order, so it never runs (docs/development/vehicle-other-factions-audit.md). Once ours, a vehicle
/// is on the player's settings at its next look, and the sling and the hitch can take it.
/// </summary>
public static class VehicleClaims
{
    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>Whether to offer the order at all: another faction's vehicle, or one with no faction, on a map.</summary>
    public static bool Offered(VehiclePawn vehicle) => vehicle.Spawned && vehicle.Faction != Faction.OfPlayer && !vehicle.Dead;

    /// <summary>Why it cannot be claimed now, or null.</summary>
    public static string? Refusal(VehiclePawn vehicle)
    {
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        if (vehicle.Faction != null && vehicle.Faction == Faction.OfMechanoids) return "PhobosVehicleClaim_Mechanoid".Translate(name).Resolve();
        if (vehicle.AllPawnsAboard.Count > 0) return "PhobosVehicleClaim_Aboard".Translate(name).Resolve();
        if (Guarded(vehicle)) return "PhobosVehicleClaim_Guarded".Translate(name).Resolve();
        return null;
    }

    /// <summary>One of the vehicle's own faction's armed people within the guard distance: a tool user who could still fight.</summary>
    static bool Guarded(VehiclePawn vehicle)
    {
        if (vehicle.Faction == null) return false;
        float reach = Rules.claimGuardCells;
        foreach (Pawn pawn in vehicle.Map.mapPawns.SpawnedPawnsInFaction(vehicle.Faction))
        {
            if (pawn == vehicle || pawn is VehiclePawn || !pawn.RaceProps.ToolUser) continue;
            if (pawn.Position.InHorDistOf(vehicle.Position, reach) && GenHostility.IsPotentialThreat(pawn)) return true;
        }
        return false;
    }

    /// <summary>True when taking it angers a faction that is not hostile to us.</summary>
    public static bool Angers(VehiclePawn vehicle) => vehicle.Faction != null && !vehicle.Faction.HostileTo(Faction.OfPlayer);

    /// <summary>The work in ticks, from the cells it covers (ClaimRule).</summary>
    public static int WorkTicks(VehiclePawn vehicle)
    {
        IntVec2 size = vehicle.def.size;
        double hours = ClaimRule.Hours(size.x * size.z, Rules.claimBaseHours, Rules.claimHoursPerCell, Rules.claimMaxHours);
        return System.Math.Max(1, (int)(hours * GenDate.TicksPerHour));
    }

    /// <summary>Makes it ours, angers its owner if they were not hostile, and says so.</summary>
    public static void Claim(VehiclePawn vehicle, Pawn claimer)
    {
        if (Refusal(vehicle) is string why)
        {
            Messages.Message(why, vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Faction? owner = vehicle.Faction;
        bool angers = Angers(vehicle);
        vehicle.SetFaction(Faction.OfPlayer);
        foreach (IntVec3 cell in vehicle.OccupiedRect()) FleckMaker.ThrowMetaPuffs(new TargetInfo(cell, vehicle.Map));
        Messages.Message("PhobosVehicleClaim_Done".Translate(claimer.LabelShort.Named("CLAIMER"), vehicle.LabelShort.Named("VEHICLE")),
            vehicle, MessageTypeDefOf.PositiveEvent);
        if (angers && owner != null)
        {
            owner.TryAffectGoodwillWith(Faction.OfPlayer, -Rules.claimGoodwillLoss, canSendMessage: true, canSendHostilityLetter: true,
                VehicleClaimDefOf.PhobosVehicleTaken, vehicle);
        }
    }
}

/// <summary>The right-click order to claim another faction's vehicle. Says why not when it cannot be done.</summary>
public class FloatMenuOptionProvider_PhobosClaimVehicle : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context) =>
        pawn is not VehiclePawn && pawn.RaceProps.Humanlike && pawn.Faction == Faction.OfPlayer && base.SelectedPawnValid(pawn, context);

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (clickedPawn is not VehiclePawn vehicle || !VehicleClaims.Offered(vehicle)) return null!;
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        if (VehicleClaims.Refusal(vehicle) is string why) return new FloatMenuOption(why, null);
        Pawn worker = context.FirstSelectedPawn;
        if (!worker.CanReach(vehicle, PathEndMode.Touch, Danger.Deadly))
        {
            return new FloatMenuOption("PhobosVehicleClaim_NoPath".Translate(name), null);
        }
        string label = VehicleClaims.Angers(vehicle)
            ? "PhobosVehicleClaim_OptionAngers".Translate(name, vehicle.Faction!.Name.Named("FACTION"))
            : "PhobosVehicleClaim_Option".Translate(name);
        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, () =>
        {
            Job job = JobMaker.MakeJob(VehicleClaimDefOf.PhobosVehicleSystems_ClaimVehicle, vehicle);
            worker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }), worker, vehicle);
    }
}

/// <summary>Walks to the vehicle, works on it for the claim's time, then makes it ours. Stops if someone boards it or a guard comes near.</summary>
public class JobDriver_PhobosClaimVehicle : JobDriver
{
    const TargetIndex VehicleIndex = TargetIndex.A;

    VehiclePawn Vehicle => (VehiclePawn)job.GetTarget(VehicleIndex).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(Vehicle, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(VehicleIndex);
        this.FailOn(() => !VehicleClaims.Offered(Vehicle) || VehicleClaims.Refusal(Vehicle) != null);
        yield return Toils_Goto.GotoThing(VehicleIndex, PathEndMode.Touch);
        Toil work = Toils_General.Wait(VehicleClaims.WorkTicks(Vehicle), VehicleIndex);
        work.WithProgressBarToilDelay(VehicleIndex);
        yield return work;
        yield return new Toil { initAction = () => VehicleClaims.Claim(Vehicle, pawn), defaultCompleteMode = ToilCompleteMode.Instant };
    }
}

[DefOf]
public static class VehicleClaimDefOf
{
    public static JobDef PhobosVehicleSystems_ClaimVehicle = null!;
    public static HistoryEventDef PhobosVehicleTaken = null!;

    static VehicleClaimDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VehicleClaimDefOf));
}
