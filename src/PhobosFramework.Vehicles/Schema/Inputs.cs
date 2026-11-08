using Phobos.Framework.Vehicles.Rules;
using RimWorld;
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
            Reading.LifePerHour => context.Comp.EngineRunning ? (float)TurbineModel.LifePerHour(state.OverContinuous) : 0f,
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

/// <summary>Whether the vehicle is moving.</summary>
public class Input_Moving : VehicleSystemInput
{
    public float stopped;
    public float moving = 1f;
    protected override float Raw(SystemContext context) => context.Comp.Moving ? moving : stopped;
}

/// <summary>The efficiency of the parts bound to a role, averaged, 0 to 1; <c>missing</c> when the vehicle has none.</summary>
public class Input_PartEfficiency : VehicleSystemInput
{
    public string role = "";
    public float missing = 1f;
    protected override float Raw(SystemContext context) => context.Comp.PartEfficiency(role, missing);
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

    protected override float Raw(SystemContext context)
    {
        double supply = context.Comp.EngineRunning ? context.Parameter(supplyParameter) * context.Comp.PartEfficiency(supplyRole, 1f) : 0;
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
