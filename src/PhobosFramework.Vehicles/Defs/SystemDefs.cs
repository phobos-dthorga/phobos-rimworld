using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Named figures in XML as plain elements, &lt;batteryAmpHours&gt;45&lt;/batteryAmpHours&gt;, the way the game reads stat lists. A system
/// declares its parameters with their defaults; a loadout overrides some of them for its vehicles.
/// </summary>
public class ParameterSet
{
    public readonly Dictionary<string, float> values = new();

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        foreach (XmlNode child in xmlRoot.ChildNodes)
        {
            if (child is not XmlElement element) continue;
            values[element.Name] = ParseHelper.FromString<float>(element.InnerText);
        }
    }

    public IEnumerable<KeyValuePair<string, double>> AsDoubles()
    {
        foreach (KeyValuePair<string, float> pair in values) yield return new KeyValuePair<string, double>(pair.Key, pair.Value);
    }
}

/// <summary>Where a quantity starts on a vehicle that has never run the system.</summary>
public enum QuantityStart
{
    Ambient,
    Full,
    Empty,
    Value,
}

/// <summary>How a quantity is shown. The unit changes only the words around the number, never the number.</summary>
public enum QuantityUnit
{
    Plain,
    Celsius,
    Fraction,
    Hours,
}

/// <summary>A range of a quantity with a name and what happens while the quantity is in it.</summary>
public class StageSpec
{
    public float from = float.NaN;
    public float to = float.NaN;
    [MustTranslate] public string label = "";
    /// <summary>Whether Plain mode names this stage on the inspect line (a normal stage stays quiet there).</summary>
    public bool notable;
    public List<VehicleSystemEffect> effects = new();
}

/// <summary>One state variable of a system: a temperature, a charge, a level, a count of hours.</summary>
public class QuantitySpec
{
    public string name = "";
    [MustTranslate] public string label = "";
    public QuantityUnit unit = QuantityUnit.Plain;
    public float min;
    public float max = 1f;
    public QuantityStart startsAt = QuantityStart.Full;
    public float startValue;
    /// <summary>When set, the quantity is not integrated: each interval it is the product of these inputs.</summary>
    public List<VehicleSystemInput>? derivedFrom;
    /// <summary>Hidden quantities work as usual but never appear on the inspect line.</summary>
    public bool hidden;
    public List<StageSpec> stages = new();

    public bool Derived => derivedFrom != null;
}

/// <summary>A rate rule that moves one quantity: in, out, or toward a target.</summary>
public class FlowSpec
{
    public string name = "";
    public string quantity = "";
    public FlowKind kind = FlowKind.Into;
    /// <summary>For a Toward flow, what the quantity moves toward (the air, another quantity).</summary>
    public VehicleSystemInput? target;
    /// <summary>Multiplied together, then by the factor.</summary>
    public List<VehicleSystemInput> inputs = new();
    public float factor = 1f;
    public string? factorParameter;
    public string? divideByParameter;
    public float openAbove = float.NaN;
    public string? openAboveParameter;
    public float openBelow = float.NaN;
    public string? openBelowParameter;
    public float band;
    /// <summary>The flow runs only while this holds.</summary>
    public VehicleSystemCondition? when;
}

/// <summary>Effects that run once when one of Vehicle Framework's events happens to the vehicle (ignition, stopping, refuelling).</summary>
public class EventRule
{
    public VehicleEventDef? eventDef;
    public List<VehicleSystemEffect> effects = new();
}

/// <summary>A quantity that changes when a part playing a role is hit: a holed radiator loses coolant.</summary>
public class PartHitRule
{
    public string role = "";
    public string quantity = "";
    /// <summary>The change per hit point the part loses (negative to drain).</summary>
    public float perHealthLost;
}

/// <summary>A quantity set back when the vehicle is serviced.</summary>
public class QuantityRestore
{
    public string quantity = "";
    public float value;
}

/// <summary>
/// What a mechanic does to service the system: the supplies loaded into the vehicle's cargo first, then work at the vehicle with
/// the Construction skill, then the restores. Due when the named quantity passes its threshold, or whenever the player asks.
/// </summary>
public class ServiceSpec
{
    [MustTranslate] public string label = "";
    public string dueQuantity = "";
    public float dueAbove = float.NaN;
    public float dueBelow = float.NaN;
    public List<QuantityRestore> restores = new();
    public List<ThingDefCountClass> supplies = new();
    /// <summary>Litres of water the service draws when it is done: from Dubs Bad Hygiene's plumbing beside the vehicle when that mod is
    /// active, otherwise free, as the base game treats water (owner, 9 Nov 2026).</summary>
    public float waterLitres;
    /// <summary>A parameter of the system holding the litres instead (so each loadout sets its own: a locomotive's tender).</summary>
    public string? waterLitresParameter;
    /// <summary>Draw only the share the due quantity is short of its restored value (a half-empty tender takes half its litres).</summary>
    public bool waterForShortfall;
    /// <summary>A parameter holding a scaled-down figure used instead, unless the player chose realistic water (the setting named by
    /// <see cref="realisticWaterSetting"/>): a steam tender's real 68,000 L would empty most colonies' towers.</summary>
    public string? scaledWaterLitresParameter;
    public string realisticWaterSetting = "realisticFeedWater";
    /// <summary>Work in ticks for a mechanic of construction speed 1.</summary>
    public float work = 1200f;
}

/// <summary>
/// How a colonist jump-starts a vehicle from another: when the quantity is below <c>raiseTo</c>, a donor vehicle of the player's
/// within <c>reach</c> cells whose same quantity is at least <c>donorMinimum</c> (or whose engine runs) gives it enough to reach
/// <c>raiseTo</c>. The charge moved is worked out in the unit of <c>capacityParameter</c> (ampere-hours), so a big battery takes
/// more from a small one. A donor with its engine running gives without losing charge: its alternator covers it.
/// </summary>
public class JumpStartSpec
{
    public string quantity = "";
    public string capacityParameter = "";
    public float raiseTo = 0.3f;
    public float donorMinimum = 0.5f;
    public float reach = 8f;
    /// <summary>Work in ticks: fetching the leads, connecting, cranking.</summary>
    public int work = 300;
}

/// <summary>
/// One kind of vehicle system (electrical, cooling, lubrication, wear, running gear, brakes and more): quantities moved by flows,
/// stages with effects, events, part hits and a service. Everything is data, so another mod adds a system or changes one in XML;
/// inputs, conditions and effects are classes another mod can add to (docs/vehicle-systems-modding.md).
/// </summary>
public class PhobosVehicleSystemDef : Def
{
    public System.Type workerClass = typeof(VehicleSystemWorker);
    public ParameterSet parameters = new();
    public List<QuantitySpec> quantities = new();
    public List<FlowSpec> flows = new();
    public List<EventRule> onEvents = new();
    public List<PartHitRule> onPartHit = new();
    public ServiceSpec? service;
    public JumpStartSpec? jumpStart;
    /// <summary>The part roles the system reads; the schema check warns when a loadout binds none of them.</summary>
    public List<string> roles = new();

    [Unsaved] VehicleSystemWorker? worker;

    public VehicleSystemWorker Worker => worker ??= (VehicleSystemWorker)System.Activator.CreateInstance(workerClass)!;

    public int QuantityIndex(string name)
    {
        for (int i = 0; i < quantities.Count; i++)
        {
            if (quantities[i].name == name) return i;
        }
        return -1;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (workerClass == null || !typeof(VehicleSystemWorker).IsAssignableFrom(workerClass)) yield return "workerClass must be a VehicleSystemWorker";
        var names = new HashSet<string>();
        foreach (QuantitySpec quantity in quantities)
        {
            if (quantity.name.NullOrEmpty()) yield return "a quantity has no name";
            else if (!names.Add(quantity.name)) yield return "two quantities are called " + quantity.name;
            if (quantity.max < quantity.min) yield return quantity.name + ": max is below min";
            var from = new List<double>();
            var to = new List<double>();
            foreach (StageSpec stage in quantity.stages)
            {
                from.Add(stage.from);
                to.Add(stage.to);
            }
            if (!StageRule.Ordered(from, to)) yield return quantity.name + ": stages overlap or are out of order";
        }
        foreach (FlowSpec flow in flows)
        {
            if (QuantityIndex(flow.quantity) < 0) yield return "flow " + flow.name + " names no quantity of this system (" + flow.quantity + ")";
            else if (quantities[QuantityIndex(flow.quantity)].Derived) yield return "flow " + flow.name + " moves a derived quantity";
            if (flow.kind == FlowKind.Toward && flow.target == null) yield return "flow " + flow.name + " moves toward nothing";
            foreach (string? parameter in new[] { flow.factorParameter, flow.divideByParameter, flow.openAboveParameter, flow.openBelowParameter })
            {
                if (parameter != null && !parameters.values.ContainsKey(parameter)) yield return "flow " + flow.name + " names an undeclared parameter " + parameter;
            }
        }
        foreach (EventRule rule in onEvents)
        {
            if (rule.eventDef == null) yield return "an event rule names no event";
        }
        if (service != null)
        {
            if (QuantityIndex(service.dueQuantity) < 0) yield return "the service is due on an unknown quantity " + service.dueQuantity;
            if (float.IsNaN(service.dueAbove) && float.IsNaN(service.dueBelow)) yield return "the service never falls due";
        }
        if (jumpStart != null)
        {
            if (QuantityIndex(jumpStart.quantity) < 0) yield return "the jump-start names an unknown quantity " + jumpStart.quantity;
            if (!parameters.values.ContainsKey(jumpStart.capacityParameter)) yield return "the jump-start names an undeclared parameter " + jumpStart.capacityParameter;
        }
    }
}

/// <summary>How a loadout binds a role to a vehicle's parts: the first finder that matches wins (or every match, for "all").</summary>
public class PartFinder
{
    public string? key;
    /// <summary>A hint by name, for parts that carry no effect to find them by (Vanilla Vehicles Expanded's alternator).</summary>
    public string? keyContains;
    /// <summary>Hints by name, any of which will do: a role made of several kinds of part (a helicopter's transmissions and rotor
    /// masts, bound together with "all").</summary>
    public List<string>? keyContainsAny;
    public List<VehicleStatDef>? categories;
    public VehicleComponent.VehiclePartDepth depth = VehicleComponent.VehiclePartDepth.Undefined;
    public Match priority = Match.Any;
    public string? tag;

    public bool Matches(VehicleComponentProperties part)
    {
        if (key != null && part.key != key) return false;
        if (keyContains != null && (part.key == null || part.key.IndexOf(keyContains, System.StringComparison.OrdinalIgnoreCase) < 0)) return false;
        if (keyContainsAny != null && (part.key == null || !keyContainsAny.Any(hint => part.key.IndexOf(hint, System.StringComparison.OrdinalIgnoreCase) >= 0))) return false;
        if (depth != VehicleComponent.VehiclePartDepth.Undefined && part.depth != depth) return false;
        if (priority != Match.Any && part.priorityStatEfficiency != (priority == Match.Yes)) return false;
        if (tag != null && (part.tags == null || !part.tags.Contains(tag))) return false;
        if (categories != null)
        {
            foreach (VehicleStatDef category in categories)
            {
                if (part.categories == null || !part.categories.Contains(category)) return false;
            }
        }
        return true;
    }
}

/// <summary>A part the loadout appends when nothing on the vehicle plays the role.</summary>
public class PartToAdd
{
    public string key = "";
    [MustTranslate] public string label = "";
    public int health = 20;
    public VehicleComponent.VehiclePartDepth depth = VehicleComponent.VehiclePartDepth.Internal;
    public float hitWeight = 0.3f;
    public float efficiencyWeight = 1f;
    public List<IntVec2>? cells;
    /// <summary>Take the cells (and the armour, unless given) of the part bound to this role, so Combat Extended can reach it.</summary>
    public string? cellsOf;
    public List<VehicleStatDef>? categories;
    public List<StatModifier>? armor;
    public List<string>? tags;
}

public class PartRoleSpec
{
    public string role = "";
    /// <summary>Bind every matching part rather than the first: wheels, tracks.</summary>
    public bool all;
    public List<PartFinder> find = new();
    public PartToAdd? add;
}

/// <summary>An electrical consumer: amperes drawn while its condition holds (and, for a switch, while it is switched on).</summary>
public class ElectricalLoadSpec
{
    public string name = "";
    [MustTranslate] public string label = "";
    [MustTranslate] public string description = "";
    public float amps;
    public VehicleSystemCondition? when;
    public bool toggle;
    public bool defaultOn = true;
    public string? iconPath;
}

/// <summary>Three-way XML switch for a selector: match either way, only yes, only no.</summary>
public enum Match
{
    Any,
    Yes,
    No,
}

/// <summary>Which vehicles a loadout claims. Every condition given must hold.</summary>
public class LoadoutSelector
{
    public List<string>? vehicleDefs;
    public List<string>? packageIds;
    public List<VehicleType>? vehicleTypes;
    public Match fuelled = Match.Any;
    public Match electricPowered = Match.Any;
    public Match flies = Match.Any;
    public List<ThingDef>? fuelTypes;
    public List<ThingDef>? excludeFuelTypes;
    public float massMin = float.NaN;
    public float massMax = float.NaN;
    public List<string>? partTags;
    /// <summary>Some part's key ends with one of these (Track: Vanilla Vehicles Expanded's tracked designs). A hint by name, because
    /// nothing else marks a tracked vehicle.</summary>
    public List<string>? partKeySuffixes;

    public bool Matches(VehicleDef def)
    {
        if (vehicleDefs != null && !vehicleDefs.Contains(def.defName)) return false;
        if (packageIds != null)
        {
            string? package = def.modContentPack?.PackageId;
            bool any = false;
            foreach (string id in packageIds)
            {
                if (package != null && string.Equals(package, id, System.StringComparison.OrdinalIgnoreCase)) { any = true; break; }
            }
            if (!any) return false;
        }
        if (vehicleTypes != null && !vehicleTypes.Contains(def.type)) return false;
        CompProperties_FueledTravel? fuel = def.GetCompProperties<CompProperties_FueledTravel>();
        if (!Fits(fuelled, fuel != null)) return false;
        if (!Fits(electricPowered, fuel != null && fuel.electricPowered)) return false;
        bool flying = def.GetCompProperties<CompProperties_VehicleLauncher>() != null;
        if (!Fits(flies, flying)) return false;
        if (fuelTypes != null && (fuel?.fuelType == null || !fuelTypes.Contains(fuel.fuelType))) return false;
        if (excludeFuelTypes != null && fuel?.fuelType != null && excludeFuelTypes.Contains(fuel.fuelType)) return false;
        if (!float.IsNaN(massMin) || !float.IsNaN(massMax))
        {
            // By name, not VehicleStatDefOf: this runs while Defs are still being resolved.
            float mass = BaseStat(def, DefDatabase<VehicleStatDef>.GetNamedSilentFail("Mass"));
            if (!float.IsNaN(massMin) && mass < massMin) return false;
            if (!float.IsNaN(massMax) && mass > massMax) return false;
        }
        if (partKeySuffixes != null)
        {
            bool found = def.components != null && def.components.Any(part => part.key != null
                && partKeySuffixes.Any(suffix => part.key.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase)));
            if (!found) return false;
        }
        if (partTags != null)
        {
            foreach (string tag in partTags)
            {
                bool found = false;
                if (def.components != null)
                {
                    foreach (VehicleComponentProperties part in def.components)
                    {
                        if (part.tags != null && part.tags.Contains(tag)) { found = true; break; }
                    }
                }
                if (!found) return false;
            }
        }
        return true;
    }

    static bool Fits(Match match, bool value) => match == Match.Any || (match == Match.Yes) == value;

    public static float BaseStat(VehicleDef def, VehicleStatDef? stat)
    {
        if (stat == null || def.vehicleStats == null) return 0f;
        foreach (VehicleStatModifier modifier in def.vehicleStats)
        {
            if (modifier.statDef == stat) return modifier.value;
        }
        return stat.defaultBaseValue;
    }
}

/// <summary>One system in a loadout, with the loadout's overrides of its parameters.</summary>
public class LoadoutSystem
{
    public PhobosVehicleSystemDef? def;
    public ParameterSet? set;
}

/// <summary>
/// Binds systems to vehicles: which vehicles (by name, by mod or by what they do), which systems with which figures, how each part
/// role maps to the vehicle's own parts or to an appended one, and the electrical loads. The highest priority match wins.
/// </summary>
public class PhobosVehicleLoadoutDef : Def
{
    public LoadoutPriority priority = LoadoutPriority.Effect;
    /// <summary>Breaks ties within a priority, higher winning: the tracked loadout beats the plain land ones.</summary>
    public int rank;
    /// <summary>The engine and drivetrain (drivetrain design), or an aircraft's engine (turbine design).</summary>
    public LoadoutDrivetrain? drivetrain;
    /// <summary>An aircraft's flight phases and the demand each puts on its engines.</summary>
    public PhobosVehicleFlightProfileDef? flightProfile;
    /// <summary>Groups loadouts in the settings window: CombustionLand, Aircraft, Boat, Electric, Rail, Heavy.</summary>
    public string kind = "General";
    public LoadoutSelector selector = new();
    public List<LoadoutSystem> systems = new();
    public List<PartRoleSpec> partRoles = new();
    public List<ElectricalLoadSpec> loads = new();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        var roles = new HashSet<string>();
        var added = new HashSet<string>();
        foreach (PartRoleSpec role in partRoles)
        {
            if (role.role.NullOrEmpty()) yield return "a part role has no name";
            else if (!roles.Add(role.role)) yield return "the role " + role.role + " is bound twice";
            if (role.add != null)
            {
                if (role.add.key.NullOrEmpty()) yield return "the part added for " + role.role + " has no key";
                else if (!added.Add(role.add.key)) yield return "two added parts are called " + role.add.key;
                if (role.add.health <= 0) yield return "the part added for " + role.role + " has no health";
                if (role.add.efficiencyWeight == 0f) yield return "the part added for " + role.role + " has an efficiency weight of 0";
            }
        }
        foreach (LoadoutSystem system in systems)
        {
            if (system.def == null) { yield return "a system entry names no system"; continue; }
            if (system.set == null) continue;
            foreach (string name in system.set.values.Keys)
            {
                if (!system.def.parameters.values.ContainsKey(name)) yield return system.def.defName + " has no parameter " + name;
            }
        }
        var loadNames = new HashSet<string>();
        foreach (ElectricalLoadSpec load in loads)
        {
            if (load.name.NullOrEmpty() || !loadNames.Add(load.name)) yield return "a load has no name or a repeated one (" + load.name + ")";
        }
    }
}

/// <summary>The service's own constants. One instance, patchable in XML; player preferences are mod settings.</summary>
public class PhobosVehicleRulesDef : PhobosRulesDef
{
    /// <summary>The most game hours a vehicle catches up on at once, after a long time unspawned or switched off.</summary>
    public float maxCatchUpHours = 24f;
    /// <summary>The longest integration step in hours.</summary>
    public float stepHours = 0.025f;
    /// <summary>A stage's message is not repeated within this many hours, so a value hovering on a threshold stays quiet.</summary>
    public float messageCooldownHours = 6f;
    /// <summary>A letter is not repeated within this many hours.</summary>
    public float letterCooldownHours = 48f;
    /// <summary>The hour the night's loads (lights) come on and the hour they go off.</summary>
    public int nightStartsHour = 19;
    public int nightEndsHour = 6;
    /// <summary>How far, in cells, a parked vehicle can reach a powered building to charge from it.</summary>
    public int shorePowerReach = 1;
}
