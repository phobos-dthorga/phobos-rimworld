using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A number a flow or a derived quantity reads from the vehicle or the world. Subclass it in any mod and name it in XML with
/// Class="Namespace.ClassName". The optional curve maps the raw reading; the factor multiplies the result.
/// </summary>
public abstract class VehicleSystemInput
{
    public SimpleCurve? curve;
    public float factor = 1f;

    public float Evaluate(SystemContext context)
    {
        float value = Raw(context);
        if (curve != null) value = curve.Evaluate(value);
        return value * factor;
    }

    protected abstract float Raw(SystemContext context);
}

/// <summary>A fixed number.</summary>
public class Input_Constant : VehicleSystemInput
{
    public float value;
    protected override float Raw(SystemContext context) => value;
}

/// <summary>One of the system's parameters, after the loadout's overrides.</summary>
public class Input_Parameter : VehicleSystemInput
{
    public string parameter = "";
    protected override float Raw(SystemContext context) => context.Parameter(parameter);
}

/// <summary>The temperature around the vehicle in degrees Celsius: its room on a map, the weather on the world map.</summary>
public class Input_Ambient : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Comp.Ambient;
}

/// <summary>
/// The temperature oil settles at with no coolant to follow (a gas turbine, an air-cooled engine): the air while the engine is off,
/// running, from <c>idleCelsius</c> at no load to <c>fullCelsius</c> at full load.
/// </summary>
public class Input_OilWithoutCoolant : VehicleSystemInput
{
    public float idleCelsius = 55f;
    public float fullCelsius = 95f;

    protected override float Raw(SystemContext context) =>
        (float)DriveHeatRules.OilTargetWithoutCoolant(context.Comp.EngineRunning, context.Comp.Ambient, context.Comp.EngineLoad, idleCelsius, fullCelsius);
}

/// <summary>
/// The share of a gearbox's full-work heat being made now: 0 with the engine off, <c>stopped</c> standing with it running; on the move
/// <c>flatOut</c> without the drivetrain model, and with it from <c>cruising</c> at no load to <c>flatOut</c> at full load.
/// </summary>
public class Input_GearboxWork : VehicleSystemInput
{
    public float stopped = 0.1f;
    public float cruising = 0.7f;
    public float flatOut = 1f;

    protected override float Raw(SystemContext context)
    {
        Powertrain? train = context.Comp.Powertrain;
        return (float)DriveHeatRules.GearboxWork(context.Comp.EngineRunning, context.Comp.Moving, train != null && train.Full && train.Ground,
            context.Comp.EngineLoad, stopped, cruising, flatOut);
    }
}

/// <summary>Whether the engine is running: the vehicle is drafted (or its caravan is moving, or it is flying) with fuel aboard.</summary>
public class Input_EngineRunning : VehicleSystemInput
{
    public float running = 1f;
    public float off;
    protected override float Raw(SystemContext context) => context.Comp.EngineRunning ? running : off;
}

/// <summary>
/// How hard the engine works, 0 to 1: 0 off, 0.5 idling, 1 moving or flying. It follows Vehicle Framework's own fuel use, which
/// burns half as much idling.
/// </summary>
public class Input_EngineLoad : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Comp.EngineLoad;
}

/// <summary>
/// Engine speed in rpm from the drivetrain or turbine model, times a <c>multiplier</c> (an alternator's pulley ratio, 2.5 in Delco
/// Remy's example). Without a model it reads <c>idle</c> or <c>cruise</c> while the engine runs, and 0 when it is off.
/// </summary>
public class Input_EngineRpm : VehicleSystemInput
{
    public float multiplier = 1f;
    public float idle = 800f;
    public float cruise = 2000f;
    /// <summary>The alternator's speed instead of the engine's (check C4, Framework 0.129.1): times the engine's own alternator ratio where
    /// the vehicle has a drivetrain, otherwise times <see cref="multiplier"/>.</summary>
    public bool alternatorSpeed;

    protected override float Raw(SystemContext context)
    {
        float rpm = context.Comp.EngineRpm(idle, cruise);
        return alternatorSpeed && context.Comp.Powertrain is { } train ? rpm * (float)train.AlternatorRatio : rpm * multiplier;
    }
}

/// <summary>Engine heat into the coolant as a share of full-load heat, from the drivetrain model; without one, the engine load.</summary>
public class Input_EngineHeat : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Comp.EngineHeatShare();
}

/// <summary>Power at the driven wheels in kW (0 without a drivetrain), for a gearbox's losses.</summary>
public class Input_DrivePower : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Comp.DrivePowerKw();
}

/// <summary>A reading from the turbine model: temperature, degrees over the continuous or top limit, spool speed, hot-section life
/// used per hour, demand, or engines out.</summary>
public class Input_Turbine : VehicleSystemInput
{
    public enum Reading
    {
        Temperature,
        OverContinuous,
        OverTop,
        SpeedPercent,
        LifePerHour,
        Demand,
        EnginesOut,
        /// <summary>How far demand exceeds the rotor drive's limit (demand ÷ limit − 1; −1 with no limit).</summary>
        OverTorque,
    }

    public Reading reading = Reading.Temperature;

    protected override float Raw(SystemContext context)
    {
        Powertrain? train = context.Comp.Powertrain;
        if (train?.Turbine == null)
        {
            return reading switch
            {
                Reading.Temperature => context.Comp.Ambient,
                Reading.OverContinuous or Reading.OverTop => -1000f,
                Reading.OverTorque => -1f,
                Reading.EnginesOut => train == null ? 0f : EnginesOut(train),
                _ => 0f,
            };
        }
        TurbineState state = train.TurbineNow;
        return reading switch
        {
            Reading.Temperature => (float)state.Temperature,
            Reading.OverContinuous => (float)state.OverContinuous,
            Reading.OverTop => (float)state.OverTop,
            Reading.SpeedPercent => (float)state.SpeedPercent,
            // Past a rating's time limit the hot section wears faster (Framework 0.110.0, avionics L1).
            Reading.LifePerHour => context.Comp.EngineRunning
                ? (float)(TurbineModel.LifePerHour(state.OverContinuous)
                    * RatingLimitRule.LifeFactor(train.OverRatingLimit, PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().ratingOverLimitLifeFactor))
                : 0f,
            Reading.Demand => (float)state.Demand,
            Reading.EnginesOut => EnginesOut(train),
            Reading.OverTorque => context.Comp.EngineRunning ? (float)state.OverTorque : -1f,
            _ => 0f,
        };
    }

    static float EnginesOut(Powertrain train)
    {
        (int working, int engines) = train.Engines();
        return engines - working;
    }
}

/// <summary>A diesel-electric's traction motors: their pull ÷ their continuous rating (above 1 they heat), 0 without a rating.</summary>
public class Input_TractionLoad : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => (float)(context.Comp.Powertrain?.TractionMotorLoad ?? 0);
}

/// <summary>Whether the vehicle is moving. <c>stoppedBonusStat</c>, when named, adds the vehicle's value of that stat to <c>stopped</c>, never
/// past <c>moving</c>: a fan that moves air through a parked vehicle's radiator as driving would (owner, 12 Nov 2026, the generator set's
/// heavy-duty fan). The stat defaults to 0, so a vehicle without such an upgrade is unchanged.</summary>
public class Input_Moving : VehicleSystemInput
{
    public float stopped;
    public float moving = 1f;
    public Vehicles.VehicleStatDef? stoppedBonusStat;

    protected override float Raw(SystemContext context)
    {
        if (context.Comp.Moving) return moving;
        if (stoppedBonusStat == null) return stopped;
        return System.Math.Min(moving, stopped + context.Vehicle.GetStatValue(stoppedBonusStat));
    }
}

/// <summary>
/// 1 while the vehicle drives through darkness with no headlights on (cross-links round three, G9; Framework 0.106.0): it is on a map,
/// moving, its headlights (if it has any) are not switched on, and the light on its own cell, as the game's light grid gives it, is below
/// <c>darkBelow</c> (0.3 is the game's own line between dark and lit). Lit roads, lamps and day cost nothing. Read on the rare tick.
/// Since Framework 0.123.0 it is read for other factions' vehicles too (the light tier's quantities): theirs burn their lights at night
/// while they move, so only one with no headlights at all is slowed.
/// </summary>
public class Input_DarkDriving : VehicleSystemInput
{
    public float darkBelow = 0.3f;

    protected override float Raw(SystemContext context)
    {
        Vehicles.VehiclePawn vehicle = context.Vehicle;
        if (!vehicle.Spawned || !context.Comp.MovingNow) return 0f;
        if (context.Comp.Beams.On) return 0f;
        return vehicle.Map.glowGrid.GroundGlowAt(vehicle.Position) < darkBelow ? 1f : 0f;
    }
}

/// <summary>
/// The driver (cross-links round three, G1 with G2; Framework 0.113.0): their fitness to drive, 0 to 1 (<see cref="DriverRule"/>: the game's
/// consciousness, sight and manipulation, and whether they are tired), or 1 while they are tired. The fittest person in a driving seat
/// counts, as the one at the wheel; with nobody there (a trailer, an autonomous vehicle, an empty one) or the engine off it reads fit and
/// rested. Read on the rare tick from the game's own cached capacities. Since Framework 0.123.0: someone in a mental break is passed over
/// (Vehicle Framework already lets nobody in a break drive, VehicleRoleHandler.CanOperateRole, so a vehicle whose only driver breaks
/// cannot move: G3 is Vehicle Framework's own), and another faction's vehicle is read while it moves (the light tier's quantities).
/// </summary>
public class Input_Driver : VehicleSystemInput
{
    public enum Reading
    {
        Fitness,
        Tired,
    }

    public Reading reading = Reading.Fitness;

    protected override float Raw(SystemContext context)
    {
        double best = -1;
        bool tired = false;
        // Only while the engine runs (drafted, a caravan on the move, flying): a parked vehicle with a tired crew aboard says nothing.
        if (context.Comp.BeingDriven && context.Vehicle.PawnsByHandlingType.TryGetValue(HandlingType.Movement, out System.Collections.Generic.List<Pawn>? drivers) && drivers != null)
        {
            foreach (Pawn pawn in drivers)
            {
                if (pawn?.health?.capacities == null || pawn.InMentalState) continue;
                bool isTired = pawn.needs?.rest != null && pawn.needs.rest.CurCategory >= RestCategory.Tired;
                PawnCapacitiesHandler capacities = pawn.health.capacities;
                double fitness = DriverRule.Fitness(capacities.GetLevel(PawnCapacityDefOf.Consciousness), capacities.GetLevel(PawnCapacityDefOf.Sight),
                    capacities.GetLevel(PawnCapacityDefOf.Manipulation), isTired);
                if (fitness <= best) continue;
                best = fitness;
                tired = isTired;
            }
        }
        if (best < 0) return reading == Reading.Fitness ? 1f : 0f;
        return reading == Reading.Fitness ? (float)best : tired ? 1f : 0f;
    }
}

/// <summary>
/// 1 while a land vehicle with a low air intake has its engine running in a river's moving water (cross-links round three, G7; Framework
/// 0.113.0; owner, 13 Nov 2026: river fording with a snorkel fitting), else 0. The fording class is by weight: a vehicle lighter than
/// <c>lowIntakeBelowKg</c> (authored 2,000 kg: cars, light utility vehicles, buggies) breathes low enough to draw water; heavier ones ford
/// the game's shallow rivers unaided, as a HMMWV fords 76 cm without a kit (TM 9-2320-280-10). The system's <c>intakeExposure</c>
/// parameter multiplies it, so the snorkel fitting (×0) closes it off.
/// </summary>
public class Input_Fording : VehicleSystemInput
{
    public float lowIntakeBelowKg = 2000f;

    protected override float Raw(SystemContext context)
    {
        Vehicles.VehiclePawn vehicle = context.Vehicle;
        if (!vehicle.Spawned || !context.Comp.EngineRunning || vehicle.VehicleDef.type != VehicleType.Land) return 0f;
        if (context.Comp.Repairs.Kilograms >= lowIntakeBelowKg) return 0f;
        TerrainDef? terrain = vehicle.Map.terrainGrid.TerrainAt(vehicle.Position);
        return terrain != null && terrain.IsRiver ? context.Parameter("intakeExposure") : 0f;
    }
}

/// <summary>
/// What the move asks of the brakes and the clutch (cross-links round two T4; Framework 0.123.0; BrakeWearRule): <c>Mass</c>, everything
/// aboard and behind the hitch against the empty mass, from 1 up to <c>most</c>; <c>Speed</c>, the square of the speed the vehicle was
/// last moving at against its design speed, between <c>least</c> and <c>mostSpeed</c>; or <c>Work</c>, the two multiplied. Without the
/// drivetrain model all read 1. On the world map a caravan moves at its design speed, so there the speed reads 1.
/// </summary>
public class Input_BrakeWork : VehicleSystemInput
{
    public enum Reading
    {
        Mass,
        Speed,
        /// <summary>Both: the mass's factor times the speed's, what one stop asks of the brakes.</summary>
        Work,
    }

    public Reading reading = Reading.Mass;
    /// <summary>The speed factor's floor.</summary>
    public float least = 0.25f;
    /// <summary>The mass factor's ceiling.</summary>
    public float most = 3f;
    /// <summary>The speed factor's ceiling.</summary>
    public float mostSpeed = 1.5f;

    protected override float Raw(SystemContext context)
    {
        Powertrain? train = context.Comp.Powertrain;
        if (train == null || !train.Ground) return 1f;
        double speed = context.Vehicle.Spawned ? BrakeWearRule.SpeedFactor(train.LastMovingSpeed, train.Spec.DesignSpeed, least, mostSpeed) : 1;
        if (reading == Reading.Speed) return (float)speed;
        double mass = BrakeWearRule.MassFactor(train.MassKg(), context.Comp.Hitch?.TowedKilograms ?? 0, train.KerbKg, most);
        return (float)(reading == Reading.Work ? mass * speed : mass);
    }
}

/// <summary>
/// 1 while a vehicle on a map moves through water its wheels can reach (shallow water, marsh or a river: the game's water terrain; land
/// vehicles keep out of deep water), else 0 (cross-links round three G6, Framework 0.123.0: wet brakes after a ford).
/// </summary>
public class Input_Wading : VehicleSystemInput
{
    protected override float Raw(SystemContext context)
    {
        Vehicles.VehiclePawn vehicle = context.Vehicle;
        if (!vehicle.Spawned || !context.Comp.MovingNow) return 0f;
        TerrainDef? terrain = vehicle.Map.terrainGrid.TerrainAt(vehicle.Position);
        return terrain != null && terrain.IsWater ? 1f : 0f;
    }
}

/// <summary>
/// How dusty the air the engine breathes is, as a multiple of the ordinary (cross-links round three G8, Framework 0.123.0; DustRule): from
/// the place's yearly rainfall (a desert 3, wet country 1) and, on a map, the weather's sand while the vehicle stands in the open. On the
/// world map, the caravan's tile.
/// </summary>
public class Input_Dust : VehicleSystemInput
{
    protected override float Raw(SystemContext context)
    {
        Vehicles.VehiclePawn vehicle = context.Vehicle;
        if (vehicle.Spawned)
        {
            Map map = vehicle.Map;
            return (float)DustRule.Factor(map.TileInfo?.rainfall ?? 1000f, map.weatherManager.SandRate, vehicle.Position.Roofed(map));
        }
        Vehicles.World.VehicleCaravan? caravan = vehicle.GetVehicleCaravan();
        if (caravan == null) return 1f;
        return (float)DustRule.Factor(Find.WorldGrid[caravan.Tile]?.rainfall ?? 1000f, 0, roofed: false);
    }
}

/// <summary>
/// The battery's temperature: the air's, or at least <c>warmTo</c> while a battery warmer is fitted (the system's <c>batteryWarmerAbsent</c>
/// below 0.5) and the parked vehicle has shore power (cross-links round two T8, the battery warmer; Framework 0.123.0). Cold-climate plug-in
/// kits pair a block heater with a battery blanket (general, secondary); warmTo is authored. A simplification: the battery is warm only while
/// plugged in, and cools to the air at once when the vehicle leaves.
/// </summary>
public class Input_BatteryTemperature : VehicleSystemInput
{
    public float warmTo = 5f;

    protected override float Raw(SystemContext context)
    {
        float air = context.Comp.Ambient;
        if (context.Parameter("batteryWarmerAbsent") >= 0.5f || context.Comp.EngineRunning || !context.Comp.ShorePowered()) return air;
        return System.Math.Max(air, warmTo);
    }
}

/// <summary>The efficiency of the parts bound to a role, averaged, 0 to 1; <c>missing</c> when the vehicle has none.</summary>
public class Input_PartEfficiency : VehicleSystemInput
{
    public string role = "";
    public float missing = 1f;
    protected override float Raw(SystemContext context) => context.Comp.PartEfficiency(role, missing);
}

/// <summary>The path cost of the ground under the vehicle: 0 on a road or floor, 2 on soil, 14 in mud or soft sand, more with snow
/// (crosslink 6). With <c>setting</c> named and switched off by the player, it reads as a road.</summary>
public class Input_GroundPathCost : VehicleSystemInput
{
    public string? setting;

    protected override float Raw(SystemContext context) =>
        setting != null && !VehicleSystemsMod.Settings.FlagOn(setting) ? 0f : context.Comp.GroundPathCost;
}

/// <summary>How fiercely the vehicle is burning: its biggest attached fire as a share of a full-grown fire, 0 when it is not burning,
/// under water or off the map (crosslink 4).</summary>
public class Input_Fire : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Comp.FireShare;
}

/// <summary>How worn a role's parts are: their average share of full health (1 sound), or <c>missing</c> when the vehicle has none.
/// Unlike <see cref="Input_PartEfficiency"/>, which follows the part's efficiency curve, this is the health itself.</summary>
public class Input_PartHealth : VehicleSystemInput
{
    public string role = "";
    public float missing = 1f;
    protected override float Raw(SystemContext context) => context.Comp.PartHealthShare(role, missing);
}

/// <summary>Another quantity of this vehicle, from this system or any other; <c>fallback</c> when no system has it.</summary>
public class Input_Quantity : VehicleSystemInput
{
    public string quantity = "";
    public VehicleSystemInput? fallback;

    protected override float Raw(SystemContext context)
    {
        if (context.Comp.TryGetQuantity(quantity, out float value)) return value;
        return fallback?.Evaluate(context) ?? 0f;
    }
}

/// <summary>Amperes drawn by the loads that are on now.</summary>
public class Input_LoadAmps : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Comp.ActiveLoadAmps();
}

/// <summary>
/// The charging system's supply against the loads, in amperes: the alternator's rated output times its part's efficiency while the
/// engine runs, less what the loads draw (Surplus), or what the loads draw beyond it (Deficit).
/// </summary>
public class Input_ElectricalBalance : VehicleSystemInput
{
    public enum Side
    {
        Surplus,
        Deficit,
    }

    public Side side = Side.Surplus;
    public string supplyParameter = "alternatorAmps";
    public string supplyRole = "Alternator";
    /// <summary>Scales the supply: the alternator's output against its rotor speed (nothing below cut-in, full at speed).</summary>
    public VehicleSystemInput? supplyFactor;
    /// <summary>The role that turns the supplier, if any (the drive belt, Framework 0.73.0): its efficiency scales the supply; none reads as sound.</summary>
    public string? drivenByRole;
    /// <summary>Reads the supply's figures in another system (Framework 0.129.0: the house battery charges from the electrical system's
    /// alternator, whose figures a heavy vehicle's loadout sets). Null: this system's own. Not on the vehicle: nothing.</summary>
    public PhobosVehicleSystemDef? fromSystem;

    protected override float Raw(SystemContext context)
    {
        if (fromSystem != null)
        {
            if (context.Comp.ContextOf(fromSystem) is not { } other) return 0f;
            context = other;
        }
        double supply = context.Comp.EngineRunning ? context.Parameter(supplyParameter) * context.Comp.PartEfficiency(supplyRole, 1f) : 0;
        if (supply > 0 && drivenByRole != null) supply *= context.Comp.PartEfficiency(drivenByRole, 1f);
        if (supply > 0 && supplyFactor != null) supply *= supplyFactor.Evaluate(context);
        // The power hook-up (Framework 0.120.0): a parked vehicle on a powered grid has its loads carried up to the hook-up's rating, so only
        // what is beyond it comes from the battery. On the deficit side only: the grid's charge is the shore-charging flow's.
        if (side == Side.Deficit && context.Comp.GridCarriesLoads)
        {
            supply += PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().hookUpRatingAmps;
        }
        double demand = context.Comp.ActiveLoadAmps();
        return (float)(side == Side.Surplus ? ElectricalBalance.Surplus(supply, demand) : ElectricalBalance.Deficit(supply, demand));
    }
}

/// <summary>The outlet's draw in amps: the watts it gives now at the vehicle's voltage, through the inverter (CampRule.OutletAmps).</summary>
public class Input_OutletAmps : VehicleSystemInput
{
    protected override float Raw(SystemContext context)
    {
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        return (float)CampRule.OutletAmps(context.Comp.OutletWatts, context.Comp.ElectricalVolts(), rules.inverterEfficiency);
    }
}

/// <summary>12 divided by the vehicle's voltage (Framework 0.129.0): a load authored in amps at 12 V draws half as many on a 24 V truck for the same watts.</summary>
public class Input_TwelveVoltShare : VehicleSystemInput
{
    protected override float Raw(SystemContext context)
    {
        float volts = context.Comp.ElectricalVolts();
        return volts > 0f ? 12f / volts : 1f;
    }
}

/// <summary>What the camp loads draw from the house battery now, in amps (Framework 0.129.0, Y5).</summary>
public class Input_CampLoadAmps : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Comp.CampLoadAmps;
}

/// <summary>1 when the parked vehicle stands beside a building on a powered grid (a conduit, a battery), else 0.</summary>
public class Input_ShorePower : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Comp.ShorePowered() ? 1f : 0f;
}

/// <summary>1 when the condition holds, else 0.</summary>
public class Input_Condition : VehicleSystemInput
{
    public VehicleSystemCondition? condition;
    protected override float Raw(SystemContext context) => condition == null || condition.Holds(context) ? 1f : 0f;
}

/// <summary>1 when the player's setting of this name is on, else 0 (settings: fuelGelling).</summary>
public class Input_Setting : VehicleSystemInput
{
    public string setting = "";
    protected override float Raw(SystemContext context) => VehicleSystemsMod.Settings.FlagOn(setting) ? 1f : 0f;
}

/// <summary>How heavily the tank's load sits on the vehicle: how full it is, times what that kind of tank weighs.</summary>
public class Input_TankLoad : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Vehicle.GetComp<CompPhobosTank>()?.WeightedLoad ?? 0f;
}

/// <summary>
/// What share of its output the generator gives at the engine's speed (avionics A5, Framework 0.106.0). A turbine's generator is driven
/// from its gearbox and follows the spool speed on <c>turbineSpool</c>, shared among the engines still working; any other engine reads
/// <c>piston</c> (the alternator's rotor speed curve). Before this, a turbine's spool speed read through the piston curve as an rpm far
/// past its top, so an aircraft's generator gave full output at idle.
/// </summary>
public class Input_GeneratorDrive : VehicleSystemInput
{
    public VehicleSystemInput? piston;
    public SimpleCurve? turbineSpool;

    protected override float Raw(SystemContext context)
    {
        Powertrain? train = context.Comp.Powertrain;
        if (train?.Turbine != null && turbineSpool != null)
        {
            (int working, int engines) = train.Engines();
            return (float)ElectricsRule.GeneratorShare(turbineSpool.Evaluate((float)train.TurbineNow.SpeedPercent), working, engines);
        }
        return piston?.Evaluate(context) ?? 1f;
    }
}

/// <summary>
/// The alternator's (or a turbine's generator's) output as a share of its rating now (Framework 0.130.0, step S5): the rules Def's one
/// curve on the alternator's own speed for a piston engine, its spool curve shared among the working engines for a turbine. Use this rather
/// than copying the curves into each system that charges a battery.
/// </summary>
public class Input_AlternatorOutput : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Comp.AlternatorOutputShare();
}

/// <summary>1 while the electrics are shedding loads to save the battery (cross-links G16, Framework 0.106.0), else 0.</summary>
public class Input_LoadShedding : VehicleSystemInput
{
    protected override float Raw(SystemContext context) => context.Comp.ShedDepth > 0 ? 1f : 0f;
}

/// <summary>One of Vehicle Framework's stats on this vehicle, as Vehicle Framework works it out from its parts (Framework 0.106.0).</summary>
public class Input_VehicleStat : VehicleSystemInput
{
    public VehicleStatDef? stat;
    protected override float Raw(SystemContext context) => stat == null ? 0f : context.Vehicle.GetStatValue(stat);
}

/// <summary>
/// Bleed air from the engines (Framework 0.119.0; avionics L3): the share of an aircraft's engines turning while they run, so a flame-out takes
/// its share of the bleed with it, and 1 while they are off (nothing is asked of the bleed, so a parked aircraft reads ready, not failed).
/// What an oxygen generator sieving engine air needs (the AV-8B's, avionics A14). A vehicle with no engine count reads 1.
/// </summary>
public class Input_Bleed : VehicleSystemInput
{
    protected override float Raw(SystemContext context)
    {
        if (!context.Comp.EngineRunning) return 1f;
        Powertrain? train = context.Comp.Powertrain;
        if (train == null) return 1f;
        (int working, int engines) = train.Engines();
        return engines <= 0 ? 1f : (float)working / engines;
    }
}
