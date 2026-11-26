using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// A roll-on fire-fighting tank (Framework 0.329.0; docs/development/vehicle-c27j.md, "The upgrade tree"; the owner's card of 26 Nov 2026:
/// "Pump from open water"). Its size is the vehicle's <see cref="AircraftUpgradeDefOf.Phobos_FireTankLitres"/>, which an upgrade node raises;
/// with none fitted the comp does nothing. On the ground, within reach of open water, a colonist runs the portable pump and fills it; in the
/// air, a water drop pass empties it along a line, putting out fires beneath (<see cref="CompPhobosFlight.WaterPass"/>). A store that runs
/// down as it is used and is filled by the player's people, never by itself (the accessories rule).
/// </summary>
public class CompProperties_PhobosFireTank : VehicleCompProperties
{
    /// <summary>How far from the vehicle's edge open water may be for the pump's hose, cells (authored).</summary>
    public int pumpReachCells = 6;
    /// <summary>
    /// The pump's rate, litres a game minute (authored: a portable fire pump moves a few hundred litres a minute; a real roll-on tank is
    /// filled from a hydrant in about 12 minutes, so this is slower).
    /// </summary>
    public float litresPerMinute = 400f;
    /// <summary>How wide a strip a pass wets, cells (authored: a line, not a spot).</summary>
    public int dropWidthCells = 3;

    public CompProperties_PhobosFireTank() => compClass = typeof(CompPhobosFireTank);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (pumpReachCells < 1 || litresPerMinute <= 0f || dropWidthCells < 1) yield return "fire tank: pumpReachCells, litresPerMinute and dropWidthCells must be positive";
    }
}

/// <summary>
/// The water in a vehicle's fire-fighting tank; see <see cref="CompProperties_PhobosFireTank"/>. Its water is a carried load, a kilogram a
/// litre, so a full tank weighs on the take-off roll and the engines as the same weight of cargo would (<see cref="CarriedLoads"/>).
/// </summary>
public class CompPhobosFireTank : VehicleComp, IVehicleCarriedLoad
{
    /// <summary>The game's ticks in a game minute.</summary>
    const float TicksPerMinute = GenDate.TicksPerHour / 60f;

    float litres;

    public CompProperties_PhobosFireTank Props => (CompProperties_PhobosFireTank)props;

    /// <summary>Its size, litres: the fitted node's, else none.</summary>
    public float Capacity => Vehicle.GetStatValue(AircraftUpgradeDefOf.Phobos_FireTankLitres);

    public bool Fitted => Capacity > 0f;

    /// <summary>The water in it now, never more than it holds (a tank taken off leaves none).</summary>
    public float Litres => Mathf.Clamp(litres, 0f, Capacity);

    public bool Full => Fitted && Litres >= Capacity - 0.5f;

    /// <summary>Water weighs a kilogram a litre.</summary>
    public float CarriedKilograms => Litres;

    /// <summary>The litres the pump moves each tick.</summary>
    public float LitresPerTick => Props.litresPerMinute / TicksPerMinute;

    /// <summary>The ticks between the pump's gulps (Framework 0.331.0): half a second, so plumbing and a tanker are drawn on in pieces of a
    /// few hundred litres rather than every tick.</summary>
    public const int GulpTicks = 30;

    /// <summary>The litres its next gulp takes: half a second's pumping, no more than the room left.</summary>
    public float Gulp => Mathf.Max(0f, Mathf.Min(LitresPerTick * GulpTicks, Capacity - Litres));

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref litres, "PhobosFireTank_Litres");
    }

    /// <summary>Adds pumped water, up to its size.</summary>
    public void Pump(float amount)
    {
        litres = Mathf.Min(Capacity, Litres + Mathf.Max(0f, amount));
        CarriedLoads.Changed(Vehicle);
    }

    /// <summary>Takes up to this much water out for a drop; what it took.</summary>
    public float Draw(float amount)
    {
        float taken = Mathf.Min(Litres, Mathf.Max(0f, amount));
        litres = Litres - taken;
        if (taken > 0f) CarriedLoads.Changed(Vehicle);
        return taken;
    }

    /// <summary>
    /// Where the pump draws from now (Framework 0.331.0; the owner's card of 26 Nov 2026, "a bowser or a tanker where those exist"):
    /// plumbing under or beside it when Dubs Bad Hygiene's tie-in gives water, else a tanker or bowser of the colony's parked within its
    /// own hose's reach with water in it, else open water within the pump's reach. The order is the agent's: treated water first, then
    /// water already drawn, then the river.
    /// </summary>
    public FireTankSource SourceNow(float litres, out CompPhobosTank? tanker)
    {
        tanker = null;
        if (!Vehicle.Spawned || litres <= 0f) return FireTankSource.None;
        if (VehicleWater.HasSource && VehicleWater.CanDraw(Vehicle, litres, out _)) return FireTankSource.Plumbing;
        tanker = TankerInReach();
        if (tanker != null) return FireTankSource.Tanker;
        return WaterInReach().IsValid ? FireTankSource.OpenWater : FireTankSource.None;
    }

    /// <summary>Takes up to this much from the source; what it gave. Open water gives all of it; plumbing all or none (Dubs' towers).</summary>
    public float DrawFrom(FireTankSource source, CompPhobosTank? tanker, float litres)
    {
        switch (source)
        {
            case FireTankSource.Plumbing:
                return VehicleWater.TryDraw(Vehicle, litres) ? litres : 0f;
            case FireTankSource.Tanker:
                return tanker != null && tanker.FireWater(out PhobosTankContentsDef? fluid) > 0f && fluid != null ? tanker.TakeOut(fluid, litres, out _) : 0f;
            case FireTankSource.OpenWater:
                return litres;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// A tank of the colony's holding water that puts out fires, parked within its own hose's reach of the aircraft's nearest cell (a
    /// water bowser, a shower trailer, a bulk tanker loaded with water). Its own pump's setting is not asked: the aircraft's pump draws
    /// (a simplification, the agent's).
    /// </summary>
    CompPhobosTank? TankerInReach()
    {
        VehiclePawn v = Vehicle;
        if (v.Faction == null) return null;
        CellRect rect = v.OccupiedRect();
        foreach (Pawn pawn in v.Map.mapPawns.SpawnedPawnsInFaction(v.Faction))
        {
            if (pawn == v || pawn is not VehiclePawn other || other.GetComp<CompPhobosTank>() is not { } tank) continue;
            if (rect.ClosestCellTo(other.Position).DistanceTo(other.Position) > tank.Props.hoseReach) continue;
            if (tank.FireWater(out _) >= 1f) return tank;
        }
        return null;
    }

    /// <summary>Open water the pump's hose reaches from where it stands, or invalid.</summary>
    public IntVec3 WaterInReach()
    {
        VehiclePawn v = Vehicle;
        if (!v.Spawned) return IntVec3.Invalid;
        Map map = v.Map;
        CellRect around = v.OccupiedRect().ExpandedBy(Props.pumpReachCells);
        foreach (IntVec3 cell in around)
        {
            if (cell.InBounds(map) && cell.GetTerrain(map) is { IsWater: true }) return cell;
        }
        return IntVec3.Invalid;
    }

    /// <summary>Why it cannot be filled now, in-world, or null.</summary>
    public string? FillRefusal()
    {
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (!Fitted) return "PhobosFireTank_RefuseNone".Translate(who).Resolve();
        if (Full) return "PhobosFireTank_RefuseFull".Translate(who).Resolve();
        if (!v.Spawned || v.GetComp<CompPhobosFlight>() is { Airborne: true }) return "PhobosFireTank_RefuseNotLanded".Translate(who).Resolve();
        if (SourceNow(Gulp, out _) == FireTankSource.None)
            return (VehicleWater.HasSource ? "PhobosFireTank_RefuseNoWaterPlumbing" : "PhobosFireTank_RefuseNoWater")
                .Translate(who, Props.pumpReachCells.Named("CELLS")).Resolve();
        return null;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (!Fitted || Vehicle.Faction != Faction.OfPlayer || Vehicle.GetComp<CompPhobosFlight>() is { Airborne: true }) yield break;
        var fill = new Command_Action
        {
            defaultLabel = "PhobosFireTank_Fill".Translate(),
            defaultDesc = "PhobosFireTank_FillDesc".Translate(Vehicle.LabelShort.Named("VEHICLE"), Props.pumpReachCells.Named("CELLS")),
            // The tanker's own load picture: the same act, a tank filled.
            icon = SystemIcons.For("Phobos/VehicleSystems/Tank/Load"),
            action = () =>
            {
                if (FireTankSend.OrderNearest(Vehicle) is { } why) Messages.Message(why, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            },
        };
        if (FillRefusal() is { } refused) fill.Disable(refused);
        yield return fill;
    }

    public override string CompInspectStringExtra()
    {
        if (!Fitted) return "";
        return "PhobosFireTank_Inspect".Translate(Mathf.RoundToInt(Litres).ToString("N0").Named("LITRES"),
            Mathf.RoundToInt(Capacity).ToString("N0").Named("CAPACITY")).Resolve();
    }
}

/// <summary>Where a fire-fighting tank's pump draws from (Framework 0.331.0).</summary>
public enum FireTankSource
{
    None,
    Plumbing,
    Tanker,
    OpenWater,
}

[DefOf]
public static class FireTankDefOf
{
    public static JobDef PhobosVehicleSystems_PumpFireTank = null!;

    static FireTankDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(FireTankDefOf));
}

/// <summary>The fill button: the nearest free colonist who can use their hands and reach a place beside the vehicle is sent to run the
/// pump, as the purge button sends one (<see cref="TankPurgeSend"/>). Says why nobody can.</summary>
public static class FireTankSend
{
    public static string? OrderNearest(VehiclePawn vehicle)
    {
        CompPhobosFireTank? tank = vehicle.GetComp<CompPhobosFireTank>();
        if (tank == null) return null;
        if (tank.FillRefusal() is { } refused) return refused;
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        Pawn? worker = Phobos.Framework.Work.SendNearest.Find(vehicle,
            pawn => (pawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) ?? false) && TankPurgeSend.StandCell(vehicle, pawn).IsValid,
            PathEndMode.Touch, draftedToo: true);
        if (worker == null) return "PhobosVehicleSystems_SendNobody".Translate(name).Resolve();
        Phobos.Framework.Work.SendNearest.Order(worker, JobMaker.MakeJob(FireTankDefOf.PhobosVehicleSystems_PumpFireTank, vehicle, TankPurgeSend.StandCell(vehicle, worker)));
        Messages.Message("PhobosVehicleSystems_SentTo".Translate(worker.LabelShort.Named("PAWN"), "PhobosFireTank_SentPump".Translate(name).Named("WORK")),
            worker, MessageTypeDefOf.SilentInput, historical: false);
        return null;
    }
}

/// <summary>Walks to the vehicle and runs the pump, the tank filling as it works, until it is full or the water or the tank is gone. It
/// draws a gulp each half second from whichever source answers then (Framework 0.331.0), so a tanker run dry hands over to the river.</summary>
public class JobDriver_PhobosPumpFireTank : JobDriver
{
    const TargetIndex VehicleIndex = TargetIndex.A;
    const TargetIndex CellIndex = TargetIndex.B;

    VehiclePawn Vehicle => (VehiclePawn)job.GetTarget(VehicleIndex).Thing;
    CompPhobosFireTank? Tank => Vehicle.GetComp<CompPhobosFireTank>();

    /// <summary>The source of the last gulp, for the seen-in-play note when it changes. Not saved: a loaded job notes its source again.</summary>
    FireTankSource drawingFrom;

    static string SourceName(FireTankSource source) => source switch
    {
        FireTankSource.Plumbing => "plumbing",
        FireTankSource.Tanker => "tanker",
        _ => "open-water",
    };

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(VehicleIndex);
        this.FailOn(() => Tank is not { Fitted: true } || Vehicle.GetComp<CompPhobosFlight>() is { Airborne: true });
        yield return Toils_Goto.GotoCell(CellIndex, PathEndMode.OnCell);
        var pump = new Toil { defaultCompleteMode = ToilCompleteMode.Never };
        pump.initAction = () =>
        {
            if (Tank is not { } tank || tank.SourceNow(tank.Gulp, out _) == FireTankSource.None) EndJobWith(JobCondition.Incompletable);
        };
        pump.tickAction = () =>
        {
            CompPhobosFireTank? tank = Tank;
            if (tank == null || !pawn.IsHashIntervalTick(CompPhobosFireTank.GulpTicks)) return;
            // The source is looked for again at every gulp: a frozen lake, a tanker run dry, empty towers or a moved vehicle stop the pump.
            float want = tank.Gulp;
            FireTankSource source = tank.SourceNow(want, out CompPhobosTank? tanker);
            float got = tank.DrawFrom(source, tanker, want);
            if (got <= 0f)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            if (source != drawingFrom)
            {
                drawingFrom = source;
                if (Seen.On) Seen.Note("aircraft.fire-tank-source", Vehicle, SourceName(source));
            }
            tank.Pump(got);
            if (!tank.Full) return;
            Seen.Note("aircraft.fire-tank-filled", Vehicle);
            if (Vehicle.Faction == Faction.OfPlayer)
                Messages.Message("PhobosFireTank_MsgFull".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.TaskCompletion, historical: false);
            EndJobWith(JobCondition.Succeeded);
        };
        pump.WithProgressBar(VehicleIndex, () => Tank is { Capacity: > 0f } t ? t.Litres / t.Capacity : 1f);
        yield return pump;
    }
}
