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

    /// <summary>Whether the stage stops the vehicle or harms it (a refusal, damage, an explosion, a forced stop): a red lamp on the dashboard, and
    /// the warning chime when a lamp lights. One rule for both, read by the presentation and the sounds alike.</summary>
    public bool Severe
    {
        get
        {
            foreach (VehicleSystemEffect effect in effects)
            {
                if (effect is Effect_RefuseDraft or Effect_RefuseMove or Effect_DamagePart or Effect_Explosion or Effect_ForceUndraft) return true;
            }
            return false;
        }
    }
}

/// <summary>One state variable of a system: a temperature, a charge, a level, a count of hours.</summary>
public class QuantitySpec
{
    public string name = "";
    /// <summary>The dashboard picture for this quantity's warnings: one white shape the dashboard tints. Without a picture the warning shows in words.</summary>
    public string? telltale;
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
    /// <summary>Shown as a gauge on the vehicle's Dashboard tab.</summary>
    public bool showOnDashboard;
    /// <summary>A slow reading that only rises between services (tread, brakes, hours since service): kept hourly for fifteen days and
    /// projected to its next stage worth a look on the Health tab (Framework 0.86.0).</summary>
    public bool wearTrend;
    /// <summary>For a quantity that counts what is used up (tread wear): the label for what is left ("tread"). The tabs' gauges then
    /// show what is left, full when new, as the battery and oil gauges do; the inspect line keeps the quantity's own label.</summary>
    [MustTranslate] public string? remainingLabel;
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

/// <summary>
/// What a hit on a part playing a role does: a quantity changes (a holed radiator loses coolant at once), or the part gets a condition
/// (it goes on leaking). The filters limit the rule to kinds of damage; with none, any hit counts. A hit whose kind is unknown (one
/// the systems only noticed in the part's health) counts only for a rule without filters.
/// </summary>
public class PartHitRule
{
    public string role = "";
    public string quantity = "";
    /// <summary>The change per hit point the part loses (negative to drain); for a condition, the severity per hit point.</summary>
    public float perHealthLost;
    /// <summary>The change per share of the part's whole health lost (1 is all of it), so a small part and a big one agree. Adds to <see cref="perHealthLost"/>.</summary>
    public float perShareLost;
    public PhobosVehicleConditionDef? condition;
    /// <summary>Only damage of these armour categories (Sharp, Blunt, Heat).</summary>
    public List<DamageArmorCategoryDef>? armorCategories;
    /// <summary>Only these kinds of damage (EMP).</summary>
    public List<DamageDef>? damageDefs;
    /// <summary>Never these kinds of damage: Scratch, which Vehicle Framework's hand brakes use on wheels, and animals' bites.</summary>
    public List<DamageDef>? exceptDamageDefs;

    bool Filtered => armorCategories != null || damageDefs != null;

    /// <summary>What losing this much of the part's health moves the quantity or adds to the condition.</summary>
    public float Amount(float healthLost, float maxHealth) => (float)Phobos.Framework.Vehicles.Rules.PartHitMath.Amount(perHealthLost, perShareLost, healthLost, maxHealth);

    public bool Matches(DamageDef? damage)
    {
        if (damage == null) return !Filtered && exceptDamageDefs == null;
        if (exceptDamageDefs != null && exceptDamageDefs.Contains(damage)) return false;
        if (!Filtered) return true;
        return (damageDefs != null && damageDefs.Contains(damage))
               || (armorCategories != null && damage.armorCategory != null && armorCategories.Contains(damage.armorCategory));
    }
}

/// <summary>A quantity set back when the vehicle is serviced.</summary>
public class QuantityRestore
{
    public string quantity = "";
    public float value;
}

/// <summary>
/// A further reason a service falls due: a quantity of any system on the vehicle past a threshold (the engine service when the oil is
/// low), optionally only while a player's setting is on. A quantity the vehicle does not have never makes it due.
/// </summary>
public class ServiceDueRule
{
    public string quantity = "";
    public float above = float.NaN;
    public float below = float.NaN;
    /// <summary>A player's setting (VehicleSystemsSettings.FlagOn) that must be on for this rule to count; none means always.</summary>
    public string? setting;
}

/// <summary>
/// What a mechanic does to service the system: the supplies loaded into the vehicle's cargo first, then work at the vehicle with
/// the Construction skill, then the restores. Due when the named quantity passes its threshold, when one of <c>alsoDue</c> holds, or
/// whenever the player asks.
/// </summary>
public class ServiceSpec
{
    [MustTranslate] public string label = "";
    public string dueQuantity = "";
    public float dueAbove = float.NaN;
    public float dueBelow = float.NaN;
    public List<ServiceDueRule> alsoDue = new();
    public List<QuantityRestore> restores = new();
    public List<ThingDefCountClass> supplies = new();
    /// <summary>What the service took before its kit existed (S4, Framework 0.74.0): taken instead of <c>supplies</c> while the colony
    /// still accepts raw materials (<see cref="GameComponent_PhobosServiceKits"/>, on for saves from before kits). Empty for a service
    /// that has no kit.</summary>
    public List<ThingDefCountClass> rawSupplies = new();
    /// <summary>Supplies used as well only when nothing provides water (without Dubs Bad Hygiene): chemfuel in place of a coolant's
    /// water (owner, 8 Nov 2026). With a water source, the service draws its water and needs none of these.</summary>
    public List<ThingDefCountClass> suppliesWithoutWaterSource = new();
    List<ThingDefCountClass>? suppliesWithFallback;
    List<ThingDefCountClass>? rawWithFallback;

    /// <summary>What the service takes from the cargo as things stand now.</summary>
    public List<ThingDefCountClass> SuppliesNow
    {
        get
        {
            bool raw = rawSupplies.Count > 0 && GameComponent_PhobosServiceKits.RawAccepted;
            List<ThingDefCountClass> basis = raw ? rawSupplies : supplies;
            if (VehicleWater.HasSource || suppliesWithoutWaterSource.Count == 0) return basis;
            return raw
                ? rawWithFallback ??= rawSupplies.Concat(suppliesWithoutWaterSource).ToList()
                : suppliesWithFallback ??= supplies.Concat(suppliesWithoutWaterSource).ToList();
        }
    }

    /// <summary>Conditions the service clears on every part of the vehicle; the service is due while the vehicle has one of them.</summary>
    public List<PhobosVehicleConditionDef> clearsConditions = new();
    /// <summary>Litres of water the service draws when it is done: from Dubs Bad Hygiene's plumbing beside the vehicle when that mod is
    /// active, otherwise free, as the base game treats water (owner, 8 Nov 2026).</summary>
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
    /// <summary>The system stands down on a vehicle with an open cab (the Cabin system).</summary>
    public bool needsClosedCab;
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
        foreach (PartHitRule rule in onPartHit)
        {
            if (string.IsNullOrEmpty(rule.role)) yield return "a part-hit rule names no role";
            if (string.IsNullOrEmpty(rule.quantity) && rule.condition == null) yield return "the part-hit rule on " + rule.role + " moves no quantity and gives no condition";
            if (rule.condition != null && rule.perHealthLost <= 0f && rule.perShareLost <= 0f) yield return "the part-hit rule giving " + rule.condition.defName + " adds no severity";
        }
        if (service != null)
        {
            if (QuantityIndex(service.dueQuantity) < 0) yield return "the service is due on an unknown quantity " + service.dueQuantity;
            if (float.IsNaN(service.dueAbove) && float.IsNaN(service.dueBelow)) yield return "the service never falls due";
            foreach (ServiceDueRule rule in service.alsoDue)
            {
                if (rule.quantity.NullOrEmpty()) yield return "a service due rule names no quantity";
                if (float.IsNaN(rule.above) && float.IsNaN(rule.below)) yield return "the service due rule on " + rule.quantity + " never trips";
            }
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
    /// <summary>The hit weight under Combat Extended, which sorts the other way (a smaller weight is hit more); NaN inverts hitWeight.</summary>
    public float hitWeightCE = float.NaN;
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
    /// <summary>Whether this vehicle has the load fitted at all (climate control only on vehicles built with it): while it does not
    /// hold, the load never runs and its switch is not shown. Null: always fitted.</summary>
    public VehicleSystemCondition? available;
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

/// <summary>Whether a loadout's vehicles have a closed cab that shelters everyone aboard (the Cabin system) or an open one.</summary>
public class CabinSpec
{
    public bool closed = true;
    /// <summary>The vehicles have automatic climate control fitted (Framework 0.22.0): it holds the cab near a set temperature.</summary>
    public bool climateControl;
}

/// <summary>On a VehicleDef, in any mod's XML: whether this vehicle's cab is closed, overriding its loadout's cabin block.</summary>
public class PhobosVehicleCabinExtension : DefModExtension
{
    public bool closed = true;
    /// <summary>This vehicle has automatic climate control fitted, whatever its loadout says (Framework 0.22.0).</summary>
    public bool climateControl;
}

/// <summary>
/// A vehicle's own figures, for a design whose Vehicle Framework Mass says little about its real weight (VF's Mass figures are not
/// comparable between mods): its empty (kerb) mass in kilograms and, where the class rule of power per tonne would be far off (a
/// motorcycle), its engine's rated power in kilowatts. Each wins over a loadout's or drivetrain's figure and over the per-Mass and
/// per-tonne rules; the player's tuning of empty mass in Vehicle Framework's window wins over it. Content mods add it with
/// PatchOperationAddModExtension (docs/vehicle-systems-modding.md).
/// </summary>
public class PhobosVehicleFiguresExtension : DefModExtension
{
    public float emptyKilograms = float.NaN;
    public float ratedKilowatts = float.NaN;

    public static bool Given(float value) => !float.IsNaN(value) && value > 0;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (!Given(emptyKilograms) && !Given(ratedKilowatts)) yield return "give emptyKilograms or ratedKilowatts, as a positive figure";
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
    /// <summary>Closed or open cab (closed by default); a vehicle's PhobosVehicleCabinExtension overrides it.</summary>
    public CabinSpec cabin = new();
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
    /// <summary>The share of full health a replaced part comes back with; ordinary repair does the rest.</summary>
    public float replacedPartHealth = 0.25f;
    /// <summary>The same, for a part fitted within reach of a working tuning rig, whose hoist and instruments do the job properly.</summary>
    public float replacedPartHealthAtRig = 0.6f;
    /// <summary>What the work of fitting a new part is multiplied by within reach of a working tuning rig.</summary>
    public float rigReplaceWorkFactor = 0.7f;
    /// <summary>Work in ticks for a mechanic to diagnose a vehicle at a tuning rig.</summary>
    public float diagnoseWork = 1500f;
    /// <summary>Below this share of their hydraulic supply, a working vehicle's hydraulic work stops (a drill mast, a tipper body).</summary>
    public float hydraulicWorkFloor = 0.2f;
    /// <summary>Work in ticks to patch a leak or a short in the field.</summary>
    public float fieldFixWork = 600f;
    /// <summary>The chance another faction's vehicle is found with a set of fittings and a tune (authored).</summary>
    public float enemyFitSetChance = 0.25f;
    /// <summary>What a field fix uses up.</summary>
    public List<ThingDefCountClass> fieldFixSupplies = new();
    /// <summary>Work at a vehicle that gets nowhere for this many ticks is stopped (the repair watchdog).</summary>
    public int repairStallTicks = 2500;
    /// <summary>Steps further apart than this are separate attempts, not one that stalled.</summary>
    public int repairStepGapTicks = 600;
    /// <summary>How long repairs to that vehicle then wait before they are tried again.</summary>
    public int repairPauseTicks = 60000;
    /// <summary>What the whole vehicle model may cost, simulation side, all maps, on average per tick (docs/development/vehicle-health-design.md).</summary>
    public float budgetMsPerTick = 0.05f;
    /// <summary>The most one call of the vehicle model may cost.</summary>
    public float budgetMsSingleCall = 0.5f;
    /// <summary>A danger clock is shown only within this many hours.</summary>
    public float dangerClockHours = 72f;
}
