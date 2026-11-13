using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A yes or no about the vehicle or the world. Subclass it in any mod; <c>invert</c> turns it round.</summary>
public abstract class VehicleSystemCondition
{
    public bool invert;

    public bool Holds(SystemContext context) => HoldsRaw(context) != invert;

    protected abstract bool HoldsRaw(SystemContext context);
}

public class When_Always : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => true;
}

/// <summary>Every listed condition holds.</summary>
public class When_All : VehicleSystemCondition
{
    public List<VehicleSystemCondition> conditions = new();

    protected override bool HoldsRaw(SystemContext context)
    {
        foreach (VehicleSystemCondition condition in conditions)
        {
            if (!condition.Holds(context)) return false;
        }
        return true;
    }
}

/// <summary>At least one listed condition holds.</summary>
public class When_Any : VehicleSystemCondition
{
    public List<VehicleSystemCondition> conditions = new();

    protected override bool HoldsRaw(SystemContext context)
    {
        foreach (VehicleSystemCondition condition in conditions)
        {
            if (condition.Holds(context)) return true;
        }
        return false;
    }
}

public class When_EngineRunning : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Comp.EngineRunning;
}

/// <summary>The tank has just run dry with the engine turning (cross-links round two, T6; Framework 0.106.0): the engine was running at the
/// last look while there was still fuel, and the tank is empty now. A tank drained on purpose while parked does not count.</summary>
public class When_RanDry : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Comp.RanDry;
}

public class When_Moving : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Comp.Moving;
}

/// <summary>Night by the clock where the vehicle is (the hours are in the rules Def).</summary>
public class When_Night : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => IsNight(context.Vehicle);

    public static bool IsNight(Thing thing)
    {
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        int hour = GenLocalDate.HourOfDay(thing);
        return rules.nightStartsHour > rules.nightEndsHour
            ? hour >= rules.nightStartsHour || hour < rules.nightEndsHour
            : hour >= rules.nightStartsHour && hour < rules.nightEndsHour;
    }
}

/// <summary>
/// Poor visibility for flying (Framework 0.110.0, avionics A1 and A2): night by the clock where the vehicle is, or, on a map, weather whose
/// accuracy multiplier is below the rules' figure (fog and heavy rain in the base game), read from what the weather does, never its name.
/// Off a map there is no weather, so only night counts there (a limit).
/// </summary>
public class When_PoorVisibility : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => Poor(context.Vehicle);

    public static bool Poor(VehiclePawn vehicle)
    {
        if (When_Night.IsNight(vehicle)) return true;
        return vehicle.Spawned && vehicle.Map.weatherManager.CurWeatherAccuracyMultiplier < PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().poorVisibilityAccuracy;
    }
}

/// <summary>While the vehicle is set up to work.</summary>
public class When_SetUp : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.AllComps.OfType<CompPhobosSetUp>().Any(c => c.Deployed);
}

/// <summary>While one of the vehicle's stats is above a value: what an upgrade has fitted.</summary>
public class When_StatAbove : VehicleSystemCondition
{
    public VehicleStatDef? stat;
    public float value;

    protected override bool HoldsRaw(SystemContext context) => stat != null && context.Vehicle.GetStatValue(stat) > value;
}

/// <summary>While the survey is drawing on the truck's own electrics for this: "Console" or "Array".</summary>
public class When_SurveyDrawing : VehicleSystemCondition
{
    public string what = "";

    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosSurveyTruck>()?.Drawing(what) == true;
}

public class When_AmbientBelow : VehicleSystemCondition
{
    public float celsius;
    protected override bool HoldsRaw(SystemContext context) => context.Comp.Ambient < celsius;
}

public class When_AmbientAbove : VehicleSystemCondition
{
    public float celsius;
    protected override bool HoldsRaw(SystemContext context) => context.Comp.Ambient > celsius;
}

public class When_QuantityBelow : VehicleSystemCondition
{
    public string quantity = "";
    public float value;
    protected override bool HoldsRaw(SystemContext context) => context.Comp.TryGetQuantity(quantity, out float current) && current < value;
}

public class When_QuantityAbove : VehicleSystemCondition
{
    public string quantity = "";
    public float value;
    protected override bool HoldsRaw(SystemContext context) => context.Comp.TryGetQuantity(quantity, out float current) && current > value;
}

/// <summary>Some running system on the vehicle has this quantity (an engine with coolant: <c>coolantTemperature</c>); <c>invert</c>
/// for one without.</summary>
public class When_HasQuantity : VehicleSystemCondition
{
    public string quantity = "";
    protected override bool HoldsRaw(SystemContext context) => context.Comp.TryGetQuantity(quantity, out _);
}

/// <summary>A player's setting of this name (settings: fuelGelling).</summary>
/// <summary>Whether one of this system's parameters, after the loadout's overrides, is below a value (a boiler without a fusible
/// plug: fusiblePlugFitted 0).</summary>
public class When_ParameterBelow : VehicleSystemCondition
{
    public string parameter = "";
    public float value;
    protected override bool HoldsRaw(SystemContext context) => context.Parameter(parameter) < value;
}

public class When_Setting : VehicleSystemCondition
{
    public string setting = "";
    protected override bool HoldsRaw(SystemContext context) => VehicleSystemsMod.Settings.FlagOn(setting);
}

/// <summary>The cab has automatic climate control fitted (the vehicle, its loadout, its tech level or the player's setting).</summary>
public class When_ClimateControl : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Comp.ClimateControlFitted;
}

/// <summary>An electric vehicle is charging from the grid (Vehicle Framework connects it to a powered network with stored energy).</summary>
public class When_Charging : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.CompFueledTravel is { } fuel && fuel.Props.ElectricPowered && fuel.Charging;
}

/// <summary>The vehicle is in a caravan resting for the night on the world map (its engine is off at camp).</summary>
public class When_CaravanResting : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetVehicleCaravan()?.NightResting == true;
}

/// <summary>One of the loadout's electrical loads is on: switched on by the player (if it has a switch) and its own condition holds.</summary>
/// <summary>While a fitting on the vehicle grants this ability, its part standing (Framework 0.103.0): the work lamps' switch shows once the
/// lamps are fitted.</summary>
public class When_Granted : VehicleSystemCondition
{
    public string ability = "";
    protected override bool HoldsRaw(SystemContext context) => context.Comp.Fittings.Grants(ability);
}

public class When_LoadActive : VehicleSystemCondition
{
    public string load = "";
    protected override bool HoldsRaw(SystemContext context) => context.Comp.LoadActive(load);
}
