using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
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
/// Dark where the vehicle is (Framework 0.173.2; the owner's screenshot of 19 Nov 2026: headlights "waiting for dark" at 17h during an eclipse).
/// On a map, the sky's light there is below <c>darkBelow</c> (the game's own sense of dark for work, 0.3): night, dusk, an eclipse or a
/// darkening storm all count, read from the sky alone, so the vehicle's own lights never switch themselves off. Off a map, where there is
/// no sky to read, night by the clock (<see cref="When_Night"/>). A limit: under a thick roof at midday the sky still reads bright.
/// </summary>
public class When_Dark : VehicleSystemCondition
{
    public float darkBelow = DefaultDarkBelow;

    /// <summary>The game's own threshold for dark (its work-speed and darkness rules use 0.3).</summary>
    public const float DefaultDarkBelow = 0.3f;

    protected override bool HoldsRaw(SystemContext context) => IsDark(context.Vehicle, darkBelow);

    public static bool IsDark(Thing thing, float darkBelow = DefaultDarkBelow)
    {
        Map? map = thing.MapHeld;
        if (map == null || !thing.SpawnedOrAnyParentSpawned) return When_Night.IsNight(thing);
        return map.skyManager.CurSkyGlow < darkBelow;
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

/// <summary>While the vehicle carries a weapon with a powered drive (Framework 0.115.0, T3): what the turret drive load needs to be fitted.</summary>
public class When_HasPoweredWeapon : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => TurretPower.AnyPowered(context.Vehicle, manned: false);
}

/// <summary>While someone mans a weapon with a powered drive (Framework 0.115.0, T3): when the turret drive draws on the battery.</summary>
public class When_PoweredWeaponManned : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => TurretPower.AnyPowered(context.Vehicle, manned: true);
}

/// <summary>While a fitting that grants this ability is on a part that is not destroyed (Framework 0.115.0, W1): a load fitted with it.</summary>
public class When_FittingGrants : VehicleSystemCondition
{
    public string ability = "";
    protected override bool HoldsRaw(SystemContext context) => context.Comp.Fittings.Grants(ability);
}

/// <summary>While a submersible's instrument is at work (Framework 0.116.0, W2): "Lab" researching, "Sonar" manned in the water, "Listening"
/// someone listening in the water. When its battery load draws.</summary>
public class When_InstrumentWorking : VehicleSystemCondition
{
    public string instrument = "";

    protected override bool HoldsRaw(SystemContext context) => instrument switch
    {
        "Lab" => context.Vehicle.GetComp<CompPhobosFieldLab>()?.Researching == true,
        "Sonar" => context.Vehicle.GetComp<CompPhobosSonar>()?.Manned == true,
        "Listening" => context.Vehicle.GetComp<CompPhobosListening>()?.Manned == true,
        _ => false,
    };
}

/// <summary>While the tanker's electric pump is fitted (Framework 0.116.0, W3).</summary>
public class When_ElectricPumpFitted : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosTank>()?.ElectricPump == true;
}

/// <summary>While the tanker's pump is set up and working (Framework 0.116.0, W3).</summary>
public class When_TankPumping : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosTank>()?.PumpWorking == true;
}

/// <summary>While the vehicle's recovery winch is pulling (Framework 0.115.0, W1).</summary>
public class When_Winching : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosHitch>()?.Winching != null;
}

/// <summary>While the aircraft's rescue hoist is running (Framework 0.155.0): its electrical load.</summary>
public class When_Hoisting : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosFlight>()?.Hoisting == true;
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

/// <summary>The cruising height between tiles governs this aircraft (Framework 0.141.0): its setting is on and the aircraft has cruising
/// figures. With <c>invert</c>, for fixed figures that stand only where it does not (avionics A14's low flight).</summary>
public class When_CruiseHeight : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => CruiseHeight.Governs(context.Comp.Vehicle);
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

/// <summary>
/// Someone is asleep in the vehicle's cab bunk while it stands on a map (Framework 0.216.0): the night heater burns for them as it does at
/// a caravan's night camp, so a parked truck is the warm bed its record promised. One comp read on the systems' own update.
/// </summary>
public class When_BunkSleeper : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosCamp>() is { SleeperAsleep: true };
}

/// <summary>
/// Someone warms up in the parked cab (Framework 0.289.0, people use parked vehicles, PV3): the night heater runs for them, as it does for
/// the bunk's sleeper. One short list read on the systems' own update.
/// </summary>
public class When_WarmingVisitor : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => VehicleVisits.WarmingUp(context.Vehicle);
}

/// <summary>
/// Someone is in the cab bunk (Framework 0.265.0, the bunk's sleep accelerator): asleep in it on a map, or a rider whose caravan bed it
/// is, on the road or at a night camp. One comp read and a look at the few people aboard, on the systems' own update.
/// </summary>
public class When_BunkInUse : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosCamp>() is { BunkInUse: true };
}

/// <summary>Someone lies on one of the vehicle's litters (Framework 0.265.0, the litters' vitals monitor draws only then).</summary>
public class When_LitterInUse : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosLitter>() is { WantsTicks: true };
}

/// <summary>One of the loadout's electrical loads is on: switched on by the player (if it has a switch) and its own condition holds.</summary>
/// <summary>While a fitting on the vehicle grants this ability, its part standing (Framework 0.103.0): the work lamps' switch shows once the
/// lamps are fitted.</summary>
public class When_Granted : VehicleSystemCondition
{
    public string ability = "";
    protected override bool HoldsRaw(SystemContext context) => context.Comp.Fittings.Grants(ability);
}

/// <summary>The vehicle is at recovery work (Framework 0.326.0: its beacon flashes): its winch is pulling a vehicle, or it has an ordinary
/// vehicle, not a trailer, on its tow bar.</summary>
public class When_Recovering : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) =>
        context.Comp.Hitch is { } hitch
        && (hitch.Winching != null || hitch.Trailer is { } towed && towed.GetComp<CompPhobosTrailer>() == null);
}

/// <summary>The design's engine is turbocharged as built (Framework 0.320.0: a built-in turbocharger that fails takes the engine down to a
/// naturally aspirated one's power, where a fitted one only gives back what it added).</summary>
public class When_TurbochargedAsBuilt : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => Turbochargers.BuiltIn(context.Comp.Props);
}

/// <summary>This tune is the one set on its axis now (Framework 0.320.0: the high boost tune wears the turbocharger faster). A tune
/// standing down, as a boost tune does with its turbocharger gone, does not hold.</summary>
public class When_Tune : VehicleSystemCondition
{
    public PhobosVehicleTuneDef? tune;
    protected override bool HoldsRaw(SystemContext context) => tune != null && context.Comp.Fittings.Current(tune.axis) == tune
        && (tune.axis != VehicleTuneAxis.Boost || context.Comp.Fittings.HasBoost);
}

/// <summary>
/// Someone of the vehicle's own faction stands within <c>radius</c> cells of it, or did within the last <c>holdHours</c> (Framework 0.171.0:
/// the work lamps switching themselves). People and mechanoids count, animals do not; those inside a vehicle are not on the map and do not.
/// Read once per look of the systems (the automatic switches' cache), never by a button or tab.
/// </summary>
public class When_PeopleNear : VehicleSystemCondition
{
    public float radius = 8f;
    public float holdHours = 1f;

    protected override bool HoldsRaw(SystemContext context)
    {
        CompPhobosVehicleSystems comp = context.Comp;
        VehiclePawn vehicle = context.Vehicle;
        if (!vehicle.Spawned || vehicle.Faction == null) return false;
        int now = Find.TickManager.TicksGame;
        IntVec3 at = vehicle.Position;
        IReadOnlyList<Pawn> pawns = vehicle.Map.mapPawns.SpawnedPawnsInFaction(vehicle.Faction);
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (pawn == vehicle || pawn is VehiclePawn || !(pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid)) continue;
            IntVec3 cell = pawn.Position;
            if (!AutoLightRule.Within(cell.x - at.x, cell.z - at.z, radius)) continue;
            comp.PeopleNearTick = now;
            return true;
        }
        return AutoLightRule.Held(comp.PeopleNearTick, now, (int)(holdHours * GenDate.TicksPerHour));
    }
}

/// <summary>A grid carries the parked vehicle's loads through its hook-up (Framework 0.171.0, for the work lamps' battery reserve).</summary>
public class When_OnGrid : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Comp.GridCarriesLoads;
}

public class When_LoadActive : VehicleSystemCondition
{
    public string load = "";
    protected override bool HoldsRaw(SystemContext context) => context.Comp.LoadActive(load);
}

/// <summary>
/// The aircraft is off the ground (Framework 0.119.0; avionics L3): in flight on the world map, or on a map in a flight phase the rules Def lists
/// as airborne (on station, climbing away, lifting off, settling to land). Phases come from each flight framework's own state, read by name.
/// </summary>
public class When_Airborne : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => Airborne(context.Comp);

    public static bool Airborne(CompPhobosVehicleSystems comp, bool live = false)
    {
        if (comp.Vehicle.CompVehicleLauncher?.inFlight == true) return true;
        // With live, the phase is read from the aircraft now (Framework 0.120.5, fault-read A5): a button asks between rare ticks, when the
        // sampled phase can be up to one behind.
        string? phase = live ? comp.Powertrain?.PhaseNow(comp.Vehicle) : comp.Powertrain?.Phase;
        return phase != null && PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().airbornePhases.Contains(phase);
    }
}

/// <summary>The aircraft is in one of these flight phases (Framework 0.119.0; avionics L3), by the names the flight profiles use: WorldFlight while
/// in flight on the world map, otherwise the phase its flight framework reports.</summary>
public class When_FlightPhase : VehicleSystemCondition
{
    public List<string> phases = new();

    protected override bool HoldsRaw(SystemContext context)
    {
        if (context.Vehicle.CompVehicleLauncher?.inFlight == true) return phases.Contains("WorldFlight");
        string? phase = context.Comp.Powertrain?.Phase;
        return phase != null && phases.Contains(phase);
    }
}

/// <summary>The cab's glass is gone (Framework 0.238.0, VG4): the cab is open to the air.</summary>
public class When_GlassGone : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => Glass.Gone(context.Comp.Vehicle);
}

/// <summary>The cab's windows are down (Framework 0.233.0): its air meets the outside faster, and smoke clears.</summary>
public class When_WindowsDown : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Comp.WindowsDown;
}
