using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A turbine rating as written in XML (see <see cref="TurbineRating"/>).</summary>
public class TurbineRatingXml
{
    public string name = "";
    public float share = 1f;
    public float speedPercent = 100f;
    public float temperatureLimit = 775f;
    public float minutes;
    public float thrustKilonewtons;
    public bool wet;
}

public class WaterInjectionXml
{
    public float tankKilograms = 227f;
    public float kilogramsPerSecond = 2.65f;
}

public class TurbineXml
{
    public float idleSpeedPercent = 60f;
    public float idleTemperatureAboveAmbient = 363f;
    public float deratePerDegree = 0.007f;
    public float startSpeedPercent = 15f;
    public List<TurbineRatingXml> ratings = new();
    public WaterInjectionXml? waterInjection;
    /// <summary>What the rotor drive accepts, as a share of all engines' combined maximum-continuous output (NaN: no limit). Its
    /// RotorDrive parts' condition scales it; demand above it is over-torque.</summary>
    public float transmissionLimit = float.NaN;
    /// <summary>Thrust-lifted craft: thrust on every thruster as a multiple of maximum gross weight (empty, full cargo, full fuel). Sets
    /// every rating's thrust (NaN: use thrustKilonewtons).</summary>
    public float liftToWeightAtMaxGross = float.NaN;
}

/// <summary>What a piston engine burns, for what it sounds like (a failed start, a knock). Nothing else reads it yet.</summary>
public enum EngineFuel { Diesel, Petrol }

public class EngineXml
{
    public EngineKind kind = EngineKind.Piston;
    public EngineFuel fuel = EngineFuel.Diesel;
    public float idleRpm = 700f;
    public float launchRpm = 1100f;
    public float ratedRpm = 3000f;
    public float kilowattsPerTonne = 30f;
    /// <summary>A real engine's rated power in kW, used instead of kilowattsPerTonne (NaN: from the mass).</summary>
    public float ratedKilowatts = float.NaN;
    public SimpleCurve? torqueCurve;
    public float frictionConstant = 0.03f;
    public float frictionLinear = 0.04f;
    public float frictionQuadratic = 0.08f;
    public float indicatedEfficiency = 0.40f;
    public float coolantHeatShare = 0.28f;
    public float idleFuelShare;
    public TurbineXml? turbine;
}

public class TransmissionXml
{
    public TransmissionKind kind = TransmissionKind.Automatic;
    public List<float> gears = new();
    public float finalDrive = 4.1f;
    public float upshiftRpm = 2400f;
    public float downshiftRpm = 1300f;
    public float efficiency = 0.88f;
    /// <summary>Diesel-electric: the traction motors' most pull (starting tractive effort), kN; 0 for no cap.</summary>
    public float maxTractiveEffortKilonewtons;
    /// <summary>Diesel-electric: the pull the motors hold indefinitely (continuous tractive effort), kN; above it they heat.</summary>
    public float continuousTractiveEffortKilonewtons;
}

public class RunningXml
{
    public float wheelRadius = 0.38f;
    public float drivenShare = 0.5f;
    /// <summary>Running gear on its own surface (steel wheels on rails): rolling resistance and grip that ignore the terrain (NaN: read
    /// the terrain).</summary>
    public float fixedRollingResistance = float.NaN;
    public float fixedGrip = float.NaN;
    public float dragArea = 2.4f;
}

public class MassXml
{
    /// <summary>Kilograms of vehicle per unit of Vehicle Framework's Mass (authored: 12), unless an empty mass is given.</summary>
    public float kilogramsPerVehicleMass = 12f;
    /// <summary>The real empty mass in kilograms, for a vehicle with a source (the Harrier's).</summary>
    public float emptyKilograms = float.NaN;
    /// <summary>Fuel mass with a full tank, kilograms (authored where unknown).</summary>
    public float fuelKilogramsFull;
}

/// <summary>
/// The engine, transmission and running gear of one kind of vehicle (design: docs/development/vehicle-drivetrain-design.md), or the
/// engine of an aircraft (vehicle-turbine-design.md). Named in a loadout's <c>drivetrain</c>; nothing it produces is saved.
/// </summary>
public class PhobosVehicleDrivetrainDef : Def
{
    public EngineXml engine = new();
    public TransmissionXml transmission = new();
    public RunningXml running = new();
    public MassXml mass = new();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (transmission.kind == TransmissionKind.Automatic)
        {
            if (transmission.gears.Count == 0) yield return "an automatic transmission has no gears";
            for (int i = 1; i < transmission.gears.Count; i++)
            {
                if (transmission.gears[i] >= transmission.gears[i - 1]) yield return "gears must fall from first to top";
            }
            if (transmission.upshiftRpm <= transmission.downshiftRpm) yield return "the upshift rpm must be above the downshift rpm";
        }
        if (engine.kind == EngineKind.Piston && (engine.launchRpm < engine.idleRpm || engine.launchRpm > engine.ratedRpm))
        {
            yield return "the launch rpm must lie between idle and rated";
        }
        if (engine.torqueCurve != null && engine.torqueCurve.Points.Count > 0 && engine.torqueCurve.Points[engine.torqueCurve.Points.Count - 1].y != 0f)
        {
            yield return "the torque curve must end at zero";
        }
        if (engine.turbine != null)
        {
            if (engine.turbine.ratings.Count == 0) yield return "a turbine has no ratings";
            for (int i = 1; i < engine.turbine.ratings.Count; i++)
            {
                TurbineRatingXml rating = engine.turbine.ratings[i];
                if (rating.share < engine.turbine.ratings[i - 1].share) yield return "turbine ratings must rise";
                if (rating.minutes <= 0f) yield return "the rating " + rating.name + " above maximum continuous needs a time limit";
            }
            if (engine.turbine.ratings.Any(r => r.wet) && engine.turbine.waterInjection == null) yield return "a wet rating needs water injection";
        }
    }
}

/// <summary>A drivetrain in a loadout, with overrides by name (kilowattsPerTonne, drivenShare, wheelRadius, dragArea,
/// kilogramsPerVehicleMass, emptyKilograms, fuelKilogramsFull, idleFuelShare, efficiency).</summary>
public class LoadoutDrivetrain
{
    public PhobosVehicleDrivetrainDef? def;
    public ParameterSet? set;
}

/// <summary>One flight phase and what it asks of the engines.</summary>
public class FlightPhaseSpec
{
    public string phase = "";
    /// <summary>What the inspect line calls it ("on station", "vertical landing").</summary>
    [MustTranslate] public string label = "";
    /// <summary>Demand as a share of maximum continuous output.</summary>
    public float demand;
    /// <summary>The demand is the hover check (weight against hover thrust) rather than a figure.</summary>
    public bool hover;
    /// <summary>Whether the engines run in this phase (false for parked).</summary>
    public bool running = true;
    /// <summary>Whether the aircraft is moving in this phase (false for idling on the pad), for the systems' airflow and wear.</summary>
    public bool moving = true;
}

/// <summary>
/// Maps a flight framework's phases to engine demand. Its reader names the members to look at by name, so no Phobos assembly
/// references another mod's (the Harrier's fork, rabiosus's VTOL Vehicle framework, or stock Vehicle Framework aircraft).
/// </summary>
public class PhobosVehicleFlightProfileDef : Def
{
    public PhaseReader? reader;
    public List<FlightPhaseSpec> phases = new();
    /// <summary>For helicopters: hover power at maximum gross weight in sea-level air at 15 °C, as a share of all engines' maximum
    /// continuous power (authored per aircraft).</summary>
    public float hoverPowerAtMaxGross = 0.9f;
    /// <summary>
    /// For aircraft that lift off through Vehicle Framework's own launch (its stock helicopters and pods): check the engines can lift
    /// the aircraft before it launches, under the player's hover-limit setting.
    /// </summary>
    public bool liftCheckAtLaunch;
    /// <summary>Bleed air for reaction control (puffer jets) while hovering, as an extra share of the engine's work (Framework 0.24.0;
    /// BleedRule). 0 for none: a helicopter steers with its rotors.</summary>
    public float reactionControlBleed;
    /// <summary>How much gusty air raises the reaction control bleed, as a share of it at full gusts (0.5: half as much again).</summary>
    public float gustBleedFactor = 0.5f;
    /// <summary>Bleed air for cabin heat and air conditioning while the cab heater or climate control runs, as an extra share of the
    /// engine's work. 0 for none.</summary>
    public float cabinBleed;

    public FlightPhaseSpec? Phase(string? name) => name == null ? null : phases.FirstOrDefault(p => p.phase == name);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (reader == null) yield return "a flight profile needs a reader";
        else
        {
            foreach (string phase in reader.Phases().Distinct())
            {
                if (Phase(phase) == null) yield return "the reader can report the phase " + phase + ", which the profile does not list";
            }
        }
    }
}

/// <summary>Reads which flight phase an aircraft is in. Subclass it for a framework the member reader cannot describe.</summary>
public abstract class PhaseReader
{
    public abstract string? Read(VehiclePawn vehicle);

    public virtual IEnumerable<string> Phases() => Enumerable.Empty<string>();
}

/// <summary>One rule: if the member (on the vehicle, or on its comp of the named type) matches, the aircraft is in this phase.</summary>
public class PhaseRule
{
    public string phase = "";
    /// <summary>The comp's type name to look on (CompHarrier); empty for the vehicle itself.</summary>
    public string? comp;
    /// <summary>A dotted path of fields or properties, public or private: FlightState, flightController.FixedWingAttackRunActive.</summary>
    public string member = "";
    /// <summary>Matches when the value's text equals this (an enum name, True).</summary>
    public string? equals;
    /// <summary>Matches when the value is a number at least this.</summary>
    public float atLeast = float.NaN;
}

/// <summary>
/// Reads the phase from members by name, the first rule that matches winning. Accessors are built once per type and path, so a read
/// is a few cached field loads. A member that does not exist reads as no match, never an error.
/// </summary>
public class PhaseReader_Members : PhaseReader
{
    public List<PhaseRule> rules = new();

    static readonly Dictionary<(Type, string), Func<object, object?>?> accessors = new();

    public override IEnumerable<string> Phases() => rules.Select(r => r.phase);

    public override string? Read(VehiclePawn vehicle)
    {
        foreach (PhaseRule rule in rules)
        {
            object? root = vehicle;
            if (!rule.comp.NullOrEmpty())
            {
                root = vehicle.AllComps.FirstOrDefault(c => c.GetType().Name == rule.comp);
                if (root == null) continue;
            }
            object? value = Value(root, rule.member);
            if (value == null) continue;
            if (rule.equals != null && value.ToString() == rule.equals) return rule.phase;
            if (!float.IsNaN(rule.atLeast) && value is IConvertible number)
            {
                try
                {
                    if (Convert.ToSingle(number) >= rule.atLeast) return rule.phase;
                }
                catch (Exception)
                {
                    // Not a number: no match.
                }
            }
        }
        return null;
    }

    static object? Value(object root, string path)
    {
        Type type = root.GetType();
        if (!accessors.TryGetValue((type, path), out Func<object, object?>? accessor))
        {
            accessor = Build(type, path);
            accessors[(type, path)] = accessor;
        }
        try
        {
            return accessor?.Invoke(root);
        }
        catch (Exception)
        {
            return null;
        }
    }

    static Func<object, object?>? Build(Type type, string path)
    {
        var steps = new List<Func<object, object?>>();
        Type? current = type;
        foreach (string name in path.Split('.'))
        {
            if (current == null) return null;
            FieldInfo? field = AccessTools.Field(current, name);
            if (field != null)
            {
                steps.Add(o => field.GetValue(o));
                current = field.FieldType;
                continue;
            }
            PropertyInfo? property = AccessTools.Property(current, name);
            if (property == null || property.GetGetMethod(true) == null) return null;
            steps.Add(o => property.GetValue(o));
            current = property.PropertyType;
        }
        return root =>
        {
            object? value = root;
            foreach (Func<object, object?> step in steps)
            {
                if (value == null) return null;
                value = step(value);
            }
            return value;
        };
    }
}
