using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

public class StatFactorSpec
{
    public VehicleStatDef? stat;
    public float factor = 1f;
}

/// <summary>A factor on a system's parameter, by the parameter's name (radiatorRate, alternatorAmps). Name the system where several
/// have a parameter of that name: heatAtFullLoad is the engine's, and the gearbox's too.</summary>
public class ParameterFactorSpec
{
    /// <summary>The system's defName; empty for every system with this parameter.</summary>
    public string system = "";
    public string parameter = "";
    public float factor = 1f;
    /// <summary>What the parameter is, in a player's words, for the Engineer display mode's figures; a factor without one is not listed.</summary>
    [MustTranslate] public string label = "";
}

/// <summary>The least a system's parameter may be while the fitting's part stands (Framework, aircraft service-life upgrades): a kit that
/// adds a capability raises its figure from 0 to 1, where a factor could not. The highest floor among the fitted parts counts.</summary>
public class ParameterFloorSpec
{
    /// <summary>The system's defName; empty for every system with this parameter.</summary>
    public string system = "";
    public string parameter = "";
    public float value = 1f;
}

/// <summary>
/// An improvement bolted onto one part of one vehicle (docs/development/vehicle-tuning-and-upgrades.md): which roles it goes on, what it
/// costs, and which figures it moves. One to a part, lost with the part. Another mod adds its own in XML.
/// </summary>
public class PhobosVehicleFittingDef : Def
{
    /// <summary>Its picture, beside its name in the fittings window and the Health tab (Framework 0.87.0); words alone without one.</summary>
    public string? iconPath;

    public UnityEngine.Texture2D? Icon => SystemIcons.IfExistsCached(iconPath);

    public List<string> roles = new();
    /// <summary>
    /// Fitted to every part of its roles together, and working only once every one of them carries it (Framework 0.246.0; owner, 23 Nov
    /// 2026: a twin-engined jet re-engined one engine at a time "wouldn't fly very stable"). Ordering, cancelling or taking it off one part
    /// does so on all of them; each part is still its own job and takes its own materials. Until the set is whole it moves no figure.
    /// </summary>
    public bool matchedSet;
    public List<ThingDefCountClass> materials = new();
    /// <summary>Work in ticks for a mechanic of construction speed 1.</summary>
    public float work = 1200f;
    /// <summary>The Construction skill the job needs; a mechanic below it is refused and told why.</summary>
    public int minSkill;
    public ResearchProjectDef? researchPrerequisite;
    public List<StatFactorSpec> statFactors = new();
    public List<ParameterFactorSpec> parameterFactors = new();
    public float powerFactor = 1f;
    public float fuelFactor = 1f;
    /// <summary>A factor on the vehicle's fuel capacity (the long range family, Framework 0.103.0): extra tanks. Applied through Vehicle
    /// Framework's own capacity offset while fitted, never saved (VehicleFittings.SyncFuelCapacity).</summary>
    public float fuelCapacityFactor = 1f;
    /// <summary>Abilities the fitting gives the vehicle, by name (Framework 0.92.0): "CargoHook" lets a helicopter lift a vehicle on a sling.
    /// A fitting that grants something need move no figure.</summary>
    public List<string> grants = new();
    /// <summary>What the fitting needs to work (Framework 0.115.0, W4): an electrical load, a quantity it draws on, the engine. Null: it needs
    /// nothing (an awning, a roof rack).</summary>
    public FeedSpec? feed;
    /// <summary>Wear quantities the fitting renews on the parts it goes on (Framework 0.105.2): new tyres renew "treadWear". Each part fitted
    /// takes away its share of the wear (FittingRule.WearAfterRenewing), so a whole set takes all of it.</summary>
    public List<string> renews = new();
    /// <summary>Only piston engines, or everything but.</summary>
    public Match piston = Match.Any;
    /// <summary>Only tracked vehicles, or only those with wheels.</summary>
    public Match tracked = Match.Any;
    /// <summary>Only where more engine power reaches the road, or only where it does not.</summary>
    public Match powerCounts = Match.Any;
    public float minKilograms = float.NaN;
    public float maxKilograms = float.NaN;
    /// <summary>Electrical loads the fitting brings while fitted (Framework 0.129.0, vehicle accessories): a fridge, a power outlet. They join
    /// the loadout's own, with a switch where they have one, so a powered accessory is one Def that works on every loadout. Names must not
    /// repeat a loadout's load.</summary>
    public List<ElectricalLoadSpec> loads = new();
    /// <summary>Kilograms the fitting takes from the vehicle's cargo capacity while fitted (Framework 0.129.0, vehicle accessories: a flat
    /// weight rather than a share, so a 20 kg fridge costs a road train what it costs a ute). No drag or speed change (owner).</summary>
    public float kilograms;
    /// <summary>What taking the fitting off gives back into the cargo (owner, 13 Nov 2026: a removed accessory gives its kit back; brackets
    /// and wiring are not returned). Empty: nothing comes back, as before.</summary>
    public List<ThingDefCountClass> givesBack = new();
    /// <summary>Only vehicles on one of these loadouts (aircraft service-life upgrades: the Harrier's engine fits only a Harrier). Empty: any.</summary>
    public List<PhobosVehicleLoadoutDef> loadouts = new();
    /// <summary>Only vehicles whose Def says they have a flat deck (PhobosFlatDeckExtension; Framework 0.217.0, the deck tent).</summary>
    public bool flatDeckOnly;
    /// <summary>Only vehicles whose loadout is of one of these kinds ("Aircraft"). Empty: any.</summary>
    public List<string> kinds = new();
    /// <summary>Raised figures while the part stands: an avionics kit's capability (aircraft service-life upgrades).</summary>
    public List<ParameterFloorSpec> parameterFloors = new();
    /// <summary>A factor on a turbine's rated thrust and on its output against demand (aircraft service-life upgrades: a re-engining).
    /// Shared across several engines by the share-per-part rule.</summary>
    public float thrustFactor = 1f;
    /// <summary>A factor on how much output a turbine loses for each degree above 15 °C: below 1, a hotter engine that keeps more in hot air.</summary>
    public float hotDerateFactor = 1f;
    /// <summary>A factor on the reach of the vehicle's electronic jamming (Framework 0.224.0; the EA-6B Prowler's ICAP upgrades): its own mod's
    /// jamming field, and ours, are this much wider while the part stands. Several such fittings multiply.</summary>
    public float jamRadiusFactor = 1f;
    /// <summary>A factor on the countermeasure dispensers' magazine: more dispensers.</summary>
    public float cartridgeFactor = 1f;

    /// <summary>Whether the fitting may go on a vehicle with this loadout, by its loadouts and kinds lists.</summary>
    public bool SuitsLoadout(PhobosVehicleLoadoutDef? loadout) =>
        (loadouts.Count == 0 || (loadout != null && loadouts.Contains(loadout)))
        && (kinds.Count == 0 || (loadout != null && kinds.Contains(loadout.kind)));

    /// <summary>Whether it moves a turbine's figures.</summary>
    public bool MovesTurbine => thrustFactor != 1f || hotDerateFactor != 1f;

    public bool Suits(double kilograms, bool hasPiston, bool isTracked, bool powerReachesRoad) =>
        (piston == Match.Any || (piston == Match.Yes) == hasPiston)
        && (powerCounts == Match.Any || (powerCounts == Match.Yes) == powerReachesRoad)
        && (tracked == Match.Any || (tracked == Match.Yes) == isTracked)
        && (float.IsNaN(minKilograms) || kilograms >= minKilograms)
        && (float.IsNaN(maxKilograms) || kilograms < maxKilograms);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (roles.Count == 0) yield return "names no roles";
        if (work <= 0f) yield return "work must be above 0";
        if (statFactors.Count == 0 && parameterFactors.Count == 0 && powerFactor == 1f && fuelFactor == 1f && fuelCapacityFactor == 1f && grants.Count == 0
            && loads.Count == 0 && kilograms == 0f && parameterFloors.Count == 0 && !MovesTurbine && cartridgeFactor == 1f)
            yield return "moves no figure and grants nothing";
        if (thrustFactor <= 0f || hotDerateFactor < 0f || cartridgeFactor <= 0f) yield return "thrustFactor and cartridgeFactor must be above 0, hotDerateFactor not below 0";
        foreach (ParameterFloorSpec spec in parameterFloors)
        {
            if (spec.parameter.NullOrEmpty()) yield return "a parameter floor names no parameter";
        }
        if (kilograms < 0f) yield return "kilograms must not be below 0";
        if (fuelCapacityFactor <= 0f) yield return "fuelCapacityFactor must be above 0";
        foreach (StatFactorSpec spec in statFactors)
        {
            if (spec.stat == null) yield return "a stat factor names no stat";
        }
        foreach (ParameterFactorSpec spec in parameterFactors)
        {
            if (spec.parameter.NullOrEmpty()) yield return "a parameter factor names no parameter";
        }
    }
}

public enum VehicleTuneAxis
{
    Engine,
    Gearing,
    /// <summary>The turbocharger's boost (Framework 0.123.0; owner, 17 Nov 2026): offered only while a fitting that grants "Boost" is fitted.</summary>
    Boost,
}

/// <summary>One named setting of an engine or a gearbox, saved per vehicle: three to an axis (owner), the one marked default as built.</summary>
public class PhobosVehicleTuneDef : Def
{
    /// <summary>Its picture, beside its name in the fittings window and the Health tab (Framework 0.87.0); words alone without one.</summary>
    public string? iconPath;

    public UnityEngine.Texture2D? Icon => SystemIcons.IfExistsCached(iconPath);

    public VehicleTuneAxis axis;
    /// <summary>The engine as it was built: no figure moves, and choosing it again puts the vehicle back.</summary>
    public bool isDefault;
    /// <summary>Left to right in the window.</summary>
    public int order;
    public List<ThingDefCountClass> materials = new();
    public float work = 1000f;
    public int minSkill;
    public float powerFactor = 1f;
    public float fuelFactor = 1f;
    /// <summary>The fuel it saves or costs when fuel does not follow the engine model; the model works this out for itself when it does.</summary>
    public float flatFuelFactor = 1f;
    public float finalDriveFactor = 1f;
    public List<ParameterFactorSpec> parameterFactors = new();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (work <= 0f) yield return "work must be above 0";
        if (!isDefault && powerFactor == 1f && fuelFactor == 1f && finalDriveFactor == 1f && parameterFactors.Count == 0) yield return "moves no figure";
    }
}

/// <summary>
/// How well a part is made (owner, 9 Nov 2026): a salvaged part is cheap, frail and fault-prone and holds no fitting, a heavy-duty one the
/// reverse. Chosen when a destroyed part is replaced, or by a refit of a working one. The one marked default is a part as built.
/// </summary>
public class PhobosVehiclePartGradeDef : Def
{
    /// <summary>Its picture, beside its name in the fittings window and the Health tab (Framework 0.87.0); words alone without one.</summary>
    public string? iconPath;

    public UnityEngine.Texture2D? Icon => SystemIcons.IfExistsCached(iconPath);

    public bool isDefault;
    /// <summary>Top to bottom in the menu.</summary>
    public int order;
    public float costFactor = 1f;
    public float healthFactor = 1f;
    public float faultFactor = 1f;
    public bool holdsFitting = true;
    /// <summary>A replacement at this grade may be paid with salvaged parts from a hull instead of new materials (Framework 0.187.0, EM3).</summary>
    public bool paidWithSalvage;
    public int minSkill;
    public ResearchProjectDef? researchPrerequisite;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (costFactor <= 0f) yield return "costFactor must be above 0";
        if (healthFactor <= 0f) yield return "healthFactor must be above 0";
        if (faultFactor < 0f) yield return "faultFactor is below 0";
    }
}

/// <summary>A set of fittings and tunes another faction's vehicle may be found with, by its faction's tech level (owner, 9 Nov 2026).</summary>
public class PhobosVehicleFitSetDef : Def
{
    public TechLevel minTech = TechLevel.Undefined;
    public TechLevel maxTech = TechLevel.Archotech;
    public float weight = 1f;
    public List<PhobosVehicleFittingDef> fittings = new();
    public List<PhobosVehicleTuneDef> tunes = new();

    public bool Suits(TechLevel tech) => tech >= minTech && tech <= maxTech;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (fittings.Count == 0 && tunes.Count == 0) yield return "holds nothing";
        if (weight <= 0f) yield return "weight must be above 0";
    }
}

/// <summary>
/// A family of fittings the player can order in one go or part by part (Framework 0.103.0; owner, 12 Nov 2026: "both";
/// docs/development/vehicle-upgrades-round-two.md). Which vehicles a family suits follows from its fittings alone: it is offered wherever any
/// of them suits a part, by role and by what the vehicle is, never by a list of vehicles. Ordering the family orders each fitting on every
/// part of this vehicle it suits and does not already carry, and switches on the electrical switches it names. Another mod adds its own in XML.
/// </summary>
public class PhobosVehicleFamilyDef : Def
{
    /// <summary>Its picture on the family's button in the fittings window.</summary>
    public string? iconPath;

    public UnityEngine.Texture2D? Icon => SystemIcons.IfExistsCached(iconPath);

    /// <summary>Top to bottom in the window.</summary>
    public int order;
    public List<PhobosVehicleFittingDef> fittings = new();
    /// <summary>Electrical switches, by the load's name, turned on when the family is ordered (the night heater for a cold-weather pack);
    /// one the vehicle does not have is passed over.</summary>
    public List<string> switchOn = new();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (fittings.Count == 0) yield return "holds no fittings";
        if (fittings.Any(f => f == null)) yield return "names a fitting that does not exist";
    }
}
