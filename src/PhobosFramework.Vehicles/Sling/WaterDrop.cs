using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

[DefOf]
public static class WaterDropDefOf
{
    public static ThingDef PhobosVehicleSystems_FireBucket = null!;
    public static ThingDef PhobosVehicleSystems_WaterRun = null!;

    static WaterDropDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(WaterDropDefOf));
}

/// <summary>
/// A helicopter's water bucket (Framework 0.101.0, batch 3's signatures; docs/development/vehicle-upgrades-round-two.md). With a cargo hook
/// fitted and a fire bucket aboard, the player clicks a fire; the helicopter flies a circuit through open water to the fire and back, as a
/// stand-in of our own (PhobosWaterRun), and the water puts out the fires it covers. This is the one place that decides whether a run may
/// go and what it does; the button only asks here.
/// </summary>
public static class WaterDrops
{
    /// <summary>How far up the screen the helicopter flies at full height, and how far below it the bucket hangs, in cells (authored; the
    /// map shows height as distance up the screen, as the sling's picture does).</summary>
    public const float CruiseCells = 4f;
    public const float StropCells = 3f;

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    public static bool HasBucket(VehiclePawn helicopter) =>
        helicopter.inventory?.innerContainer.Any(t => t.def == WaterDropDefOf.PhobosVehicleSystems_FireBucket) == true;

    /// <summary>The litres the bucket carries today: its size, or what the hook lifts in today's heat if that is less.</summary>
    public static double Litres(CompPhobosSling sling) => WaterDropRule.Litres(Rules.waterBucketLitres, sling.CapacityKilograms(sling.Vehicle.Map));

    /// <summary>The radius, in cells, the water puts out.</summary>
    public static float Radius(double litres) => (float)WaterDropRule.Radius(litres, Rules.waterDropLitresPerCell);

    static int Ticks(float seconds) => Mathf.RoundToInt(seconds * GenTicks.TicksPerRealSecond);

    /// <summary>Why this helicopter cannot fly a water drop now, or null.</summary>
    public static string? Refusal(CompPhobosSling sling)
    {
        VehiclePawn helicopter = sling.Vehicle;
        if (!sling.HookFitted) return "PhobosSling_NoHook".Translate().Resolve();
        if (!HasBucket(helicopter)) return "PhobosWaterDrop_NoBucket".Translate().Resolve();
        if ((sling.Carried ?? sling.Hooked) is { } load) return "PhobosWaterDrop_HasLoad".Translate(load.LabelShort.Named("LOAD")).Resolve();
        if (!helicopter.Spawned) return "PhobosWaterDrop_NotHere".Translate().Resolve();
        if (helicopter.vehiclePather is { Moving: true }) return "PhobosWaterDrop_Moving".Translate().Resolve();
        if (!helicopter.CanMoveFinal) return "PhobosWaterDrop_NoCrew".Translate().Resolve();
        if (helicopter.CompFueledTravel is { EmptyTank: true }) return "PhobosWaterDrop_NoFuel".Translate().Resolve();
        if (helicopter.OccupiedRect().Any(cell => cell.Roofed(helicopter.Map))) return "PhobosWaterDrop_Roofed".Translate().Resolve();
        return CompPhobosSling.WeatherRefusal(helicopter.Map);
    }

    /// <summary>Why a run to this cell cannot go, or null, with the water it would dip from.</summary>
    public static string? TargetRefusal(CompPhobosSling sling, IntVec3 target, out IntVec3 water)
    {
        water = IntVec3.Invalid;
        VehiclePawn helicopter = sling.Vehicle;
        Map map = helicopter.Map;
        float reach = Reach;
        if (!target.InBounds(map)) return "PhobosWaterDrop_NothingBurning".Translate().Resolve();
        if (target.DistanceTo(helicopter.Position) > reach) return "PhobosWaterDrop_TooFar".Translate(reach.Named("CELLS")).Resolve();
        if (target.Roofed(map)) return "PhobosWaterDrop_TargetRoofed".Translate().Resolve();
        if (!AnyFire(map, target, Mathf.Max(1.5f, Radius(Litres(sling))))) return "PhobosWaterDrop_NothingBurning".Translate().Resolve();
        if (!TryFindWater(helicopter, target, reach, out water)) return "PhobosWaterDrop_NoWater".Translate(reach.Named("CELLS")).Resolve();
        WaterRunPlan plan = PlanFor(helicopter, water, target);
        if (helicopter.CompFueledTravel is { } fuel && fuel.Fuel < FuelFor(helicopter, plan))
        {
            return "PhobosWaterDrop_NotEnoughFuel".Translate().Resolve();
        }
        return null;
    }

    /// <summary>How far a fire and its water may be from the helicopter, kept within the game's radial cell pattern.</summary>
    static float Reach => Mathf.Min(Rules.waterDropReachCells, GenRadial.MaxRadialPatternRadius - 1f);

    static bool AnyFire(Map map, IntVec3 centre, float radius)
    {
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(centre, radius, useCenter: true))
        {
            if (!cell.InBounds(map) || cell.Roofed(map)) continue;
            if (cell.GetThingList(map).Any(t => t is Fire)) return true;
        }
        return false;
    }

    /// <summary>The open water within reach of the helicopter that makes the shortest circuit to this fire. Read once, when the order is
    /// given.</summary>
    static bool TryFindWater(VehiclePawn helicopter, IntVec3 fire, float reach, out IntVec3 found)
    {
        Map map = helicopter.Map;
        IntVec3 pad = helicopter.Position;
        found = IntVec3.Invalid;
        float best = float.MaxValue;
        int count = GenRadial.NumCellsInRadius(reach);
        for (int i = 0; i < count; i++)
        {
            IntVec3 cell = pad + GenRadial.RadialPattern[i];
            if (!cell.InBounds(map) || cell.Roofed(map) || cell.GetTerrain(map) is not { IsWater: true }) continue;
            float circuit = cell.DistanceTo(pad) + cell.DistanceTo(fire);
            if (circuit >= best) continue;
            best = circuit;
            found = cell;
        }
        return found.IsValid;
    }

    /// <summary>The run's stages, from the helicopter's centre to the water and the fire.</summary>
    public static WaterRunPlan PlanFor(VehiclePawn helicopter, IntVec3 water, IntVec3 fire)
    {
        PhobosVehicleRulesDef rules = Rules;
        Vector3 pad = ((Thing)helicopter).DrawPos;
        Vector3 w = water.ToVector3Shifted(), f = fire.ToVector3Shifted();
        return WaterDropRule.Plan(w.x - pad.x, w.z - pad.z, f.x - pad.x, f.z - pad.z, rules.waterDropCellsPerTick,
            Ticks(rules.waterDropClimbSeconds), Ticks(rules.waterDropDipSeconds), Ticks(rules.waterDropPourSeconds));
    }

    /// <summary>The fuel a run burns: the helicopter's own rate for its length, more with a load hanging when that setting is on.</summary>
    public static float FuelFor(VehiclePawn helicopter, WaterRunPlan plan)
    {
        if (helicopter.CompFueledTravel is not { } fuel) return 0f;
        float slung = Settings.slingSlowFlight ? Rules.slingFuelPerTileFactor : 1f;
        return fuel.ConsumptionRatePerTick * plan.Total * slung;
    }

    /// <summary>Sends the helicopter on a run to this fire, or says why not.</summary>
    public static void Order(CompPhobosSling sling, IntVec3 target)
    {
        VehiclePawn helicopter = sling.Vehicle;
        string? refusal = Refusal(sling) ?? TargetRefusal(sling, target, out _);
        if (refusal != null)
        {
            Messages.Message(refusal, helicopter, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        TargetRefusal(sling, target, out IntVec3 water);
        WaterRunPlan plan = PlanFor(helicopter, water, target);
        double litres = Litres(sling);
        helicopter.CompFueledTravel?.ConsumeFuel(FuelFor(helicopter, plan));
        PhobosWaterRun.Start(helicopter, water, target, plan, litres, Radius(litres));
        Messages.Message("PhobosWaterDrop_Started".Translate(helicopter.LabelShort.Named("HELICOPTER"), ((float)litres).ToString("N0").Named("LITRES")).Resolve(),
            new LookTargets(target, helicopter.Map), MessageTypeDefOf.TaskCompletion, historical: false);
    }

    /// <summary>Puts out the fires on the open cells within the radius, and the fires burning on things there. Returns how many went out.</summary>
    public static int Douse(Map map, IntVec3 centre, float radius)
    {
        int doused = 0;
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(centre, Mathf.Max(1f, radius), useCenter: true))
        {
            if (!cell.InBounds(map) || cell.Roofed(map)) continue;
            foreach (Thing thing in cell.GetThingList(map).ToList())
            {
                Fire? fire = thing as Fire ?? thing.GetAttachment(ThingDefOf.Fire) as Fire;
                if (fire == null || fire.Destroyed) continue;
                fire.Destroy();
                doused++;
            }
            if (Rand.Chance(0.35f)) FleckMaker.WaterSplash(cell.ToVector3Shifted(), map, Rand.Range(1.2f, 2.2f), Rand.Range(1f, 3f));
        }
        return doused;
    }

    /// <summary>The Water drop button. Presentation only: the order goes to Order, which decides.</summary>
    public static Command Gizmo(CompPhobosSling sling)
    {
        double litres = Litres(sling);
        var command = new Command_Action
        {
            defaultLabel = "PhobosWaterDrop_Gizmo".Translate(),
            defaultDesc = "PhobosWaterDrop_GizmoDesc".Translate(((float)litres).ToString("N0").Named("LITRES"), Reach.Named("CELLS")),
            icon = SystemIcons.For("Phobos/VehicleSystems/Sling/WaterDrop"),
            action = () => Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetLocations = true,
                canTargetFires = true,
                canTargetPawns = false,
                canTargetBuildings = false,
            }, target => Order(sling, target.Cell)),
        };
        if (Refusal(sling) is { } refusal) command.Disable(refusal);
        return command;
    }
}

/// <summary>
/// A helicopter flying a water drop run (Framework 0.101.0): our own stand-in, as Vehicle Framework flies its own take-offs, holding the
/// helicopter (and its crew) while it climbs from where it stood, dips its bucket in open water, empties it over the fire and comes back
/// down. It can be selected and says what it is doing. It ticks and draws only while a run is in the air, for half a minute or so.
/// </summary>
public class PhobosWaterRun : Thing, IThingHolder
{
    ThingOwner<VehiclePawn> held;
    IntVec3 padCell;
    Rot4 padRotation;
    Vector3 pad, water, fire;
    IntVec3 fireCell;
    int started;
    int climb, toWater, dip, toFire, pour, home, descend;
    double litres;
    float radius;
    bool poured;
    int doused;
    Rot8 facing = Rot8.North;
    Graphic? bucketGraphic;

    public PhobosWaterRun() => held = new ThingOwner<VehiclePawn>(this, oneStackOnly: true);

    VehiclePawn? Helicopter => held.Count > 0 ? held[0] : null;

    WaterRunPlan Plan => new(climb, toWater, dip, toFire, pour, home, descend);

    int Elapsed => Find.TickManager.TicksGame - started;

    /// <summary>Lifts the helicopter off the map into a new run, and puts the run where it stood.</summary>
    public static void Start(VehiclePawn helicopter, IntVec3 water, IntVec3 fire, WaterRunPlan plan, double litres, float radius)
    {
        Map map = helicopter.Map;
        var run = (PhobosWaterRun)ThingMaker.MakeThing(WaterDropDefOf.PhobosVehicleSystems_WaterRun);
        run.padCell = helicopter.Position;
        run.padRotation = helicopter.Rotation;
        run.pad = ((Thing)helicopter).DrawPos;
        run.water = water.ToVector3Shifted();
        run.fire = fire.ToVector3Shifted();
        run.fireCell = fire;
        run.climb = plan.Climb;
        run.toWater = plan.ToWater;
        run.dip = plan.Dip;
        run.toFire = plan.ToFire;
        run.pour = plan.Pour;
        run.home = plan.Home;
        run.descend = plan.Descend;
        run.litres = litres;
        run.radius = radius;
        run.facing = helicopter.FullRotation;
        run.started = Find.TickManager.TicksGame;
        // Patients on litters stay aboard through the run; anything else that takes a vehicle off the map sets them down first.
        helicopter.GetComp<CompPhobosLitter>()?.HoldThroughDespawn();
        helicopter.vehiclePather?.StopDead();
        helicopter.DeSpawn();
        if (!run.held.TryAdd(helicopter, canMergeWithExistingStacks: false))
        {
            GenSpawn.Spawn(helicopter, run.padCell, map, run.padRotation);
            return;
        }
        GenSpawn.Spawn(run, run.padCell, map);
    }

    WaterRunPoint Point(int tick) => WaterDropRule.At(Plan, tick, water.x - pad.x, water.z - pad.z, fire.x - pad.x, fire.z - pad.z);

    protected override void Tick()
    {
        base.Tick();
        if (Helicopter == null)
        {
            Destroy();
            return;
        }
        int elapsed = Elapsed;
        WaterRunPoint point = Point(elapsed);
        var ground = new IntVec3(Mathf.FloorToInt(pad.x + (float)point.X), 0, Mathf.FloorToInt(pad.z + (float)point.Z));
        if (ground.InBounds(Map) && ground != Position) Position = ground;
        if (point.Stage == WaterRunStage.Dip && this.IsHashIntervalTick(20))
        {
            FleckMaker.WaterSplash(water, Map, Rand.Range(1.5f, 2.5f), Rand.Range(1f, 2f));
        }
        if (!poured && elapsed >= Plan.PourEnds)
        {
            poured = true;
            doused = WaterDrops.Douse(Map, fireCell, radius);
        }
        if (elapsed >= Plan.Total) Land();
    }

    /// <summary>Sets the helicopter back down where it stood (or the nearest clear ground) and ends the run.</summary>
    void Land()
    {
        VehiclePawn? helicopter = Helicopter;
        Map map = Map;
        if (helicopter == null || map == null) return;
        held.Remove(helicopter);
        IntVec3 cell = padCell;
        if (!SlingPlacement.Fits(helicopter.VehicleDef, padRotation, map, padCell, CellRect.Empty)
            && SlingPlacement.TryFindClear(helicopter.VehicleDef, padRotation, map, padCell, 30f, CellRect.Empty, out IntVec3 clear))
        {
            cell = clear;
        }
        Destroy();
        GenSpawn.Spawn(helicopter, cell, map, padRotation);
        Messages.Message("PhobosWaterDrop_Back".Translate(helicopter.LabelShort.Named("HELICOPTER"), doused.Named("COUNT")).Resolve(), helicopter,
            doused > 0 ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NeutralEvent);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        // Never lose the helicopter to a run that ends early: it comes down where it stood.
        if (Helicopter is { } helicopter && Spawned)
        {
            Map map = Map;
            held.Remove(helicopter);
            base.Destroy(mode);
            GenSpawn.Spawn(helicopter, padCell, map, padRotation);
            return;
        }
        base.Destroy(mode);
    }

    // ---- What the player sees ----

    Vector3 HelicopterAt(WaterRunPoint point) => new(pad.x + (float)point.X, AltitudeLayer.Skyfaller.AltitudeFor(), pad.z + (float)point.Z + (float)point.Height * WaterDrops.CruiseCells);

    public override Vector3 DrawPos => HelicopterAt(Point(Elapsed));

    public override string LabelNoCount => Helicopter is { } helicopter
        ? "PhobosWaterDrop_RunLabel".Translate(helicopter.LabelShort.Named("HELICOPTER")).Resolve()
        : base.LabelNoCount;

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        VehiclePawn? helicopter = Helicopter;
        if (helicopter == null) return;
        int elapsed = Elapsed;
        WaterRunPoint point = Point(elapsed);
        WaterRunPoint ahead = Point(elapsed + 5);
        double dx = ahead.X - point.X, dz = ahead.Z - point.Z;
        if (dx * dx + dz * dz > 0.0001) facing = FacingOf(System.Math.Atan2(dx, dz) * 180.0 / System.Math.PI);
        Vector3 at = HelicopterAt(point);
        if (!SlingDrawing.DrawVehicle(helicopter, at, facing)) return;
        if (point.Height <= 0.2) return;
        // The bucket hangs below on its strop, one layer under the helicopter, never below the ground under it.
        float hang = Mathf.Min(WaterDrops.StropCells, (float)point.Height * WaterDrops.CruiseCells);
        var bucket = new Vector3(at.x, at.y - Altitudes.AltInc, at.z - hang);
        bucketGraphic ??= WaterDropDefOf.PhobosVehicleSystems_FireBucket.graphic;
        bucketGraphic?.Draw(bucket, Rot4.North, this);
        float between = at.y - Altitudes.AltInc / 2f;
        SlingDrawing.DrawStrop(new Vector3(at.x, between, at.z), new Vector3(bucket.x, between, bucket.z + 0.3f));
    }

    static readonly Rot8[] Facings = { Rot8.North, Rot8.NorthEast, Rot8.East, Rot8.SouthEast, Rot8.South, Rot8.SouthWest, Rot8.West, Rot8.NorthWest };

    /// <summary>The nearest of the eight facings to a heading in degrees (0 north, 90 east).</summary>
    static Rot8 FacingOf(double heading)
    {
        double degrees = heading % 360;
        if (degrees < 0) degrees += 360;
        return Facings[Mathf.RoundToInt((float)(degrees / 45.0)) % 8];
    }

    public override string GetInspectString()
    {
        WaterRunPoint point = Point(Elapsed);
        string stage = point.Stage switch
        {
            WaterRunStage.Climb => "PhobosWaterDrop_StageClimb",
            WaterRunStage.ToWater => "PhobosWaterDrop_StageToWater",
            WaterRunStage.Dip => "PhobosWaterDrop_StageDip",
            WaterRunStage.ToFire => "PhobosWaterDrop_StageToFire",
            WaterRunStage.Pour => "PhobosWaterDrop_StagePour",
            WaterRunStage.Home => "PhobosWaterDrop_StageHome",
            _ => "PhobosWaterDrop_StageDescend",
        };
        return "PhobosWaterDrop_Inspect".Translate(stage.Translate().Named("STAGE"), ((float)litres).ToString("N0").Named("LITRES")).Resolve();
    }

    // ---- Holding and saving ----

    public ThingOwner GetDirectlyHeldThings() => held;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, held);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref held, "PhobosWaterRun_Held", this);
        Scribe_Values.Look(ref padCell, "PhobosWaterRun_PadCell");
        Scribe_Values.Look(ref padRotation, "PhobosWaterRun_PadRotation");
        Scribe_Values.Look(ref pad, "PhobosWaterRun_Pad");
        Scribe_Values.Look(ref water, "PhobosWaterRun_Water");
        Scribe_Values.Look(ref fire, "PhobosWaterRun_Fire");
        Scribe_Values.Look(ref fireCell, "PhobosWaterRun_FireCell");
        Scribe_Values.Look(ref started, "PhobosWaterRun_Started");
        Scribe_Values.Look(ref climb, "PhobosWaterRun_Climb");
        Scribe_Values.Look(ref toWater, "PhobosWaterRun_ToWater");
        Scribe_Values.Look(ref dip, "PhobosWaterRun_Dip");
        Scribe_Values.Look(ref toFire, "PhobosWaterRun_ToFire");
        Scribe_Values.Look(ref pour, "PhobosWaterRun_Pour");
        Scribe_Values.Look(ref home, "PhobosWaterRun_Home");
        Scribe_Values.Look(ref descend, "PhobosWaterRun_Descend");
        Scribe_Values.Look(ref litres, "PhobosWaterRun_Litres");
        Scribe_Values.Look(ref radius, "PhobosWaterRun_Radius");
        Scribe_Values.Look(ref poured, "PhobosWaterRun_Poured");
        Scribe_Values.Look(ref doused, "PhobosWaterRun_Doused");
        if (Scribe.mode == LoadSaveMode.PostLoadInit) held ??= new ThingOwner<VehiclePawn>(this, oneStackOnly: true);
    }
}
