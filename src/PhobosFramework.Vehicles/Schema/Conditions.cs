using System.Collections.Generic;
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

public class When_Moving : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Comp.Moving;
}

/// <summary>Night by the clock where the vehicle is (the hours are in the rules Def).</summary>
public class When_Night : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context)
    {
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        int hour = GenLocalDate.HourOfDay(context.Vehicle);
        return rules.nightStartsHour > rules.nightEndsHour
            ? hour >= rules.nightStartsHour || hour < rules.nightEndsHour
            : hour >= rules.nightStartsHour && hour < rules.nightEndsHour;
    }
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
public class When_LoadActive : VehicleSystemCondition
{
    public string load = "";
    protected override bool HoldsRaw(SystemContext context) => context.Comp.LoadActive(load);
}
