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
    protected override float Raw(SystemContext context) => context.Comp.EngineRpm(idle, cruise) * multiplier;
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
/// </summary>
public class Input_DarkDriving : VehicleSystemInput
{
    public float darkBelow = 0.3f;

    protected override float Raw(SystemContext context)
    {
        Vehicles.VehiclePawn vehicle = context.Vehicle;
        if (!vehicle.Spawned || !context.Comp.Moving) return 0f;
        if (context.Comp.Beams.On) return 0f;
        return vehicle.Map.glowGrid.GroundGlowAt(vehicle.Position) < darkBelow ? 1f : 0f;
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

    protected override float Raw(SystemContext context)
    {
        double supply = context.Comp.EngineRunning ? context.Parameter(supplyParameter) * context.Comp.PartEfficiency(supplyRole, 1f) : 0;
        if (supply > 0 && drivenByRole != null) supply *= context.Comp.PartEfficiency(drivenByRole, 1f);
        if (supply > 0 && supplyFactor != null) supply *= supplyFactor.Evaluate(context);
        double demand = context.Comp.ActiveLoadAmps();
        return (float)(side == Side.Surplus ? ElectricalBalance.Surplus(supply, demand) : ElectricalBalance.Deficit(supply, demand));
    }
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
