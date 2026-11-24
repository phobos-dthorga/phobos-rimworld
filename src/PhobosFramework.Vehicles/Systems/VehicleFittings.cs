using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A fitting on one part, by the part's key: Vehicle Framework saves parts by position, and appended parts move.</summary>
/// <summary>
/// What a set of fittings and tunes does to a vehicle's figures, as the drive applies them (Framework 0.88.0): one rule for the vehicle's
/// own cache and for the fittings window's lines and previews, so what the window says is what the vehicle gets. A fitting on one part of
/// several gives its share (FittingRule.PartShare); a tune applies whole.
/// </summary>
public sealed class FitFigures
{
    public double Power = 1, Fuel = 1, FlatFuel = 1, FinalDrive = 1;
    /// <summary>The factor on the fuel capacity (extra tanks; Framework 0.103.0).</summary>
    public double FuelCapacity = 1;
    /// <summary>A turbine's thrust and output, its loss in hot air, and the dispensers' magazine (aircraft service-life upgrades).</summary>
    public double Thrust = 1, HotDerate = 1, Cartridges = 1;
    /// <summary>The least each system parameter may be, the highest floor of any fitting (a kit's capability; not shared across parts).</summary>
    public readonly Dictionary<(string system, string name), float> Floors = new();
    public readonly Dictionary<VehicleStatDef, float> Stats = new();
    public readonly Dictionary<(string system, string name), float> Parameters = new();
    /// <summary>A parameter's player label, where a Def gives one (the first that does).</summary>
    public readonly Dictionary<(string system, string name), string> ParameterLabels = new();

    /// <summary>The fuel against Vehicle Framework's own rate: the flat factor too unless fuel follows the engine model.</summary>
    public double TunedFuel => Fuel * (VehicleFittings.FuelFollowsModel ? 1 : FlatFuel);

    public static FitFigures Of(IEnumerable<(PhobosVehicleFittingDef def, int parts)> fittings, IEnumerable<PhobosVehicleTuneDef> tunes)
    {
        var figures = new FitFigures();
        foreach ((PhobosVehicleFittingDef def, int parts) in fittings)
        {
            foreach (StatFactorSpec spec in def.statFactors)
            {
                if (spec.stat != null) figures.Stats[spec.stat] = (figures.Stats.TryGetValue(spec.stat, out float before) ? before : 1f) * (float)FittingRule.PartShare(spec.factor, parts);
            }
            foreach (ParameterFactorSpec spec in def.parameterFactors) figures.Multiply(spec, FittingRule.PartShare(spec.factor, parts));
            figures.Power *= FittingRule.PartShare(def.powerFactor, parts);
            figures.Fuel *= FittingRule.PartShare(def.fuelFactor, parts);
            figures.FuelCapacity *= FittingRule.PartShare(def.fuelCapacityFactor, parts);
            figures.Thrust *= FittingRule.PartShare(def.thrustFactor, parts);
            figures.HotDerate *= FittingRule.PartShare(def.hotDerateFactor, parts);
            figures.Cartridges *= FittingRule.PartShare(def.cartridgeFactor, parts);
            foreach (ParameterFloorSpec spec in def.parameterFloors)
            {
                var key = (spec.system, spec.parameter);
                if (!figures.Floors.TryGetValue(key, out float before) || spec.value > before) figures.Floors[key] = spec.value;
            }
        }
        foreach (PhobosVehicleTuneDef tune in tunes)
        {
            foreach (ParameterFactorSpec spec in tune.parameterFactors) figures.Multiply(spec, spec.factor);
            figures.Power *= tune.powerFactor;
            figures.Fuel *= tune.fuelFactor;
            figures.FlatFuel *= tune.flatFuelFactor;
            figures.FinalDrive *= tune.finalDriveFactor;
        }
        return figures;
    }

    void Multiply(ParameterFactorSpec spec, double factor)
    {
        var key = (spec.system, spec.parameter);
        Parameters[key] = (Parameters.TryGetValue(key, out float before) ? before : 1f) * (float)factor;
        if (!spec.label.NullOrEmpty() && !ParameterLabels.ContainsKey(key)) ParameterLabels[key] = spec.label;
    }
}

public sealed class FittedPart : IExposable
{
    public string partKey = "";
    public PhobosVehicleFittingDef? def;

    public void ExposeData()
    {
        Scribe_Values.Look(ref partKey, "part", "");
        Scribe_Defs.Look(ref def, "def");
    }
}

/// <summary>A part's grade, by the part's key. A part of the default grade has no entry.</summary>
public sealed class GradedPart : IExposable
{
    public string partKey = "";
    public PhobosVehiclePartGradeDef? def;

    public void ExposeData()
    {
        Scribe_Values.Look(ref partKey, "part", "");
        Scribe_Defs.Look(ref def, "def");
    }
}

/// <summary>Work the player has asked for: fit or take off a fitting, set a tune, or change a part's grade.</summary>
public sealed class FitOrder : IExposable
{
    public string partKey = "";
    public PhobosVehicleFittingDef? fitting;
    public PhobosVehicleTuneDef? tune;
    public PhobosVehiclePartGradeDef? grade;
    public bool remove;

    public bool Valid => remove ? !partKey.NullOrEmpty() : fitting != null || tune != null || grade != null;

    public void ExposeData()
    {
        Scribe_Values.Look(ref partKey, "part", "");
        Scribe_Defs.Look(ref fitting, "fitting");
        Scribe_Defs.Look(ref tune, "tune");
        Scribe_Defs.Look(ref grade, "grade");
        Scribe_Values.Look(ref remove, "remove", false);
    }
}

/// <summary>
/// One vehicle's fittings and tunes (docs/development/vehicle-tuning-and-upgrades.md), owned by its systems comp: what is fitted and what
/// is ordered, the figures they move, and the work they ask for. The figures are cached and rebuilt only when something changes, so
/// nothing here runs per tick, for any faction's vehicle. Mechanics do the orders through <see cref="VehicleRepairs"/>.
/// </summary>
public sealed class VehicleFittings
{
    readonly CompPhobosVehicleSystems comp;
    List<FittedPart> fitted = new();
    List<PhobosVehicleTuneDef> tunes = new();
    List<FitOrder> orders = new();
    List<GradedPart> graded = new();
    bool setRolled;
    /// <summary>Whether this vehicle has had its loadout's fittedOnOlderSaves, or never needed them: true for a vehicle made in this version,
    /// false for one loaded from a save that has no entry for it.</summary>
    bool olderSaveFitsSettled = true;

    Dictionary<VehicleStatDef, float>? statFactors;
    Dictionary<(string system, string name), float>? parameterFactors;
    /// <summary>The figures were worked out before the vehicle's parts had their properties, so they are not kept (see Recache).</summary>
    bool provisional;
    float power = 1f;
    float fuel = 1f;
    float flatFuel = 1f;
    float finalDrive = 1f;
    float fuelCapacity = 1f;
    float thrust = 1f, hotDerate = 1f, cartridges = 1f;
    Dictionary<(string system, string name), float>? floors;
    /// <summary>What this vehicle's tanks now add to Vehicle Framework's capacity offset. Not saved: Vehicle Framework does not save its
    /// offsets either, so both start from nothing after a load and SyncFuelCapacity puts ours back.</summary>
    float appliedFuelOffset;

    public VehicleFittings(CompPhobosVehicleSystems comp)
    {
        this.comp = comp;
    }

    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;
    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>Counts every change of the cost settings, so work already waiting is priced again.</summary>
    public static int CostStamp;

    /// <summary>A cost setting changed: work waiting is priced again, and so are our vehicles' upgrade nodes (UpgradePricing).</summary>
    public static void CostsChanged()
    {
        CostStamp++;
        UpgradePricing.Reprice();
    }

    /// <summary>
    /// What fitting and tuning cost on this vehicle against the listed figures (Framework 0.91.0; owner, 11 Nov 2026: "I want these
    /// upgrades and tunes to feel earned"): the player's presets, and the vehicle's weight when that setting is on (FitCostRule).
    /// </summary>
    double CostFactor(double exponent, float preset)
    {
        PhobosVehicleRulesDef rules = Rules;
        double weight = Settings.fitCostScalesWithWeight
            ? FitCostRule.WeightFactor(comp.Repairs.Kilograms, rules.fitCostReferenceKilograms, exponent, rules.fitWeightFactorMin, rules.fitWeightFactorMax)
            : 1;
        return weight * preset;
    }

    /// <summary>The materials a fitting or tune takes on this vehicle.</summary>
    public List<ThingDefCountClass> MaterialsFor(IEnumerable<ThingDefCountClass> listed)
    {
        double factor = CostFactor(Rules.fitMaterialWeightExponent, Settings.fitMaterialFactor);
        // A whole machine (a building named as a material, fitted packed: Framework 0.265.0) is the same one machine on any vehicle; so is a
        // part counted in ones (Framework 0.272.0; the owner's A-10 of 23 Nov 2026 asked two turbine assemblies for each engine): only
        // bulk materials grow with the vehicle, by the rule upgrade nodes already priced by (FitCostRule.IsBulk).
        int bulkAbove = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().upgradeBulkStackAbove;
        return listed.Select(m => new ThingDefCountClass(m.thingDef,
            Phobos.Framework.Items.PackedThings.Packs(m.thingDef) || !FitCostRule.IsBulk(m.thingDef.stackLimit, bulkAbove) ? m.count
                : FitCostRule.Count(m.count, factor))).ToList();
    }

    /// <summary>The work in ticks a fitting or tune takes on this vehicle.</summary>
    public float WorkFor(float listed) => FitCostRule.Work(listed, CostFactor(Rules.fitWorkWeightExponent, Settings.fitTimeFactor));

    public IReadOnlyList<FitOrder> Orders => orders ??= new List<FitOrder>();

    bool Nothing => (fitted == null || fitted.Count == 0) && (tunes == null || tunes.Count == 0);

    public void ExposeData()
    {
        Scribe_Collections.Look(ref fitted, "PhobosVehicleSystems_Fitted", LookMode.Deep);
        Scribe_Collections.Look(ref tunes, "PhobosVehicleSystems_Tunes", LookMode.Def);
        Scribe_Collections.Look(ref orders, "PhobosVehicleSystems_FitOrders", LookMode.Deep);
        Scribe_Collections.Look(ref graded, "PhobosVehicleSystems_Grades", LookMode.Deep);
        Scribe_Values.Look(ref setRolled, "PhobosVehicleSystems_FitSetRolled", false);
        Scribe_Values.Look(ref olderSaveFitsSettled, "PhobosVehicleSystems_OlderSaveFitsSettled", false);
        // A save from before fittings existed has no entry for them, and the loader then leaves the lists null, not empty. The
        // stats are read before loading ends (Vehicle Framework spawns the vehicle first), so they are made whole in every mode,
        // never only at the end (0.40.0 left every vehicle unspawnable on an older save; 0.40.2).
        fitted ??= new List<FittedPart>();
        tunes ??= new List<PhobosVehicleTuneDef>();
        orders ??= new List<FitOrder>();
        graded ??= new List<GradedPart>();
        if (Scribe.mode == LoadSaveMode.Saving) return;
        graded.RemoveAll(g => g == null || g.def == null || g.partKey.NullOrEmpty());
        // A Def another mod took away: the fitting goes with it, and the rest of the vehicle is untouched. Not left to the end of
        // loading: Vehicle Framework runs PostLoad, and with it our powertrain, before a comp's own last pass.
        fitted.RemoveAll(f => f == null || f.def == null || f.partKey.NullOrEmpty());
        tunes.RemoveAll(t => t == null);
        orders.RemoveAll(o => o == null || !o.Valid);
        statFactors = null;
        parameterFactors = null;
        countedKeys = null;
    }

    public void Clear()
    {
        foreach (GradedPart part in graded) comp.Vehicle.statHandler?.GetComponent(part.partKey)?.AddHealthModifiers.Remove(GradeHealthKey);
        graded.Clear();
        fitted.Clear();
        tunes.Clear();
        orders.Clear();
        DropDisplayCaches();
        statFactors = null;
        parameterFactors = null;
        countedKeys = null;
        SyncFuelCapacity(FuelExcess.Keep);
    }

    // ---- The figures ----

    void Recache()
    {
        if (statFactors != null && !provisional) return;
        // Framework 0.266.1 (the owner's log of 23 Nov 2026: seven vehicles threw while PG20 loaded): during a load, Vehicle Framework's
        // acceleration asks for a stat in its own post-load pass, which can come before its stat handler's pass gives the parts their
        // properties. Fittings cannot be counted against blank parts (a matched set reads its parts' keys), so until then the figures
        // hold the tunes alone and are worked out again at the next ask; PostSpawnSetup marks every stat dirty once the parts are ready.
        provisional = !PartsReady();
        IEnumerable<(PhobosVehicleFittingDef, int)> counted = provisional
            ? Enumerable.Empty<(PhobosVehicleFittingDef, int)>()
            : fitted.Where(Counts).Select(part => (part.def!, PartsOf(part.def!)));
        FitFigures figures = FitFigures.Of(counted, ActiveTunes());
        statFactors = figures.Stats;
        parameterFactors = figures.Parameters;
        power = (float)figures.Power;
        fuel = (float)figures.Fuel;
        flatFuel = (float)figures.FlatFuel;
        finalDrive = (float)figures.FinalDrive;
        fuelCapacity = (float)figures.FuelCapacity;
        thrust = (float)figures.Thrust;
        hotDerate = (float)figures.HotDerate;
        cartridges = (float)figures.Cartridges;
        floors = figures.Floors;
    }

    /// <summary>The roles whose part rows carry a tune's changes in the fittings window: an engine tune on the engines, the gearing on the
    /// transmission (owner, 11 Nov 2026: only those rows, so the window stays quiet).</summary>
    public VehicleTuneAxis? AxisOf(VehicleComponent part)
    {
        foreach (string role in comp.Props.roleKeys.Keys)
        {
            if (!comp.PartPlays(part, role)) continue;
            if (role.StartsWith("Engine")) return VehicleTuneAxis.Engine;
            if (role == "Transmission") return VehicleTuneAxis.Gearing;
        }
        return null;
    }

    /// <summary>
    /// What a part's row says it gives (Framework 0.88.0): the fitting on it, at its share among its role's parts, together with the tune
    /// whose changes land on that part. The window passes the current fitting and tune, or the one the mouse rests on as a preview.
    /// </summary>
    public FitFigures RowFigures(PhobosVehicleFittingDef? fitting, PhobosVehicleTuneDef? tune, int fittedParts = 1)
    {
        var fittings = new List<(PhobosVehicleFittingDef, int)>();
        // An axle's row counts every wheel of it that has the fitting: each gives its share, as on the vehicle.
        // A matched set's row shows the whole set's effect: it gives nothing until the set is whole, and all of it then.
        if (fitting is { matchedSet: true, matchedPerAxle: false } && fittedParts > 0) fittedParts = PartsOf(fitting);
        for (int i = 0; fitting != null && i < fittedParts; i++) fittings.Add((fitting, PartsOf(fitting)));
        return FitFigures.Of(fittings, tune == null ? Enumerable.Empty<PhobosVehicleTuneDef>() : new[] { tune });
    }

    public float StatFactor(VehicleStatDef stat)
    {
        if (Nothing) return 1f;
        Recache();
        return statFactors!.TryGetValue(stat, out float factor) ? factor : 1f;
    }

    /// <summary>The factor on a system's parameter: what names that system, and what names no system at all.</summary>
    public float ParameterFactor(string system, string name)
    {
        if (Nothing) return 1f;
        Recache();
        float everywhere = parameterFactors!.TryGetValue(("", name), out float any) ? any : 1f;
        return parameterFactors.TryGetValue((system, name), out float own) ? everywhere * own : everywhere;
    }

    /// <summary>The least a system's parameter may be by what is fitted (a kit's capability), or NaN with no floor: what names that system,
    /// and what names no system at all, the higher of the two.</summary>
    public float ParameterFloor(string system, string name)
    {
        if (Nothing) return float.NaN;
        Recache();
        float floor = float.NaN;
        if (floors!.TryGetValue(("", name), out float any)) floor = any;
        if (floors.TryGetValue((system, name), out float own) && (float.IsNaN(floor) || own > floor)) floor = own;
        return floor;
    }

    /// <summary>The factor on a turbine's thrust and output (a re-engining), across every engine fitted.</summary>
    public float ThrustFactor
    {
        get
        {
            if (Nothing) return 1f;
            Recache();
            return thrust;
        }
    }

    /// <summary>The factor on a turbine's loss of output in hot air.</summary>
    public float HotDerateFactor
    {
        get
        {
            if (Nothing) return 1f;
            Recache();
            return hotDerate;
        }
    }

    /// <summary>The factor on the countermeasure dispensers' magazine.</summary>
    public float CartridgeFactor
    {
        get
        {
            if (Nothing) return 1f;
            Recache();
            return cartridges;
        }
    }

    public float PowerFactor
    {
        get
        {
            if (Nothing) return 1f;
            Recache();
            return power;
        }
    }

    public float FuelFactor
    {
        get
        {
            if (Nothing) return 1f;
            Recache();
            return fuel;
        }
    }

    /// <summary>What fittings and tunes do to the fuel burned against Vehicle Framework's own rate, as the drive applies it: the flat
    /// factor too unless fuel follows the engine model (one rule for the burn and for the range on the world map).</summary>
    public float TunedFuelFactor => FuelFactor * (FuelFollowsModel ? 1f : FlatFuelFactor);

    /// <summary>What the tunes do to the fuel burned when fuel does not follow the engine model, which otherwise works it out itself.</summary>
    public float FlatFuelFactor
    {
        get
        {
            if (Nothing) return 1f;
            Recache();
            return flatFuel;
        }
    }

    public float FinalDriveFactor
    {
        get
        {
            if (Nothing) return 1f;
            Recache();
            return finalDrive;
        }
    }

    /// <summary>The fittings behind a stat's factor, for the stat card's explanation.</summary>
    public IEnumerable<string> StatFactorReasons(VehicleStatDef stat)
    {
        foreach (FittedPart part in fitted)
        {
            foreach (StatFactorSpec spec in part.def!.statFactors)
            {
                if (spec.stat == stat) yield return part.def.LabelCap + " x" + ((float)FittingRule.PartShare(spec.factor, PartsOf(part.def))).ToStringByStyle(ToStringStyle.FloatTwo);
            }
        }
    }

    int PartsOf(PhobosVehicleFittingDef def)
    {
        var keys = new HashSet<string>();
        foreach (string role in def.roles)
        {
            if (comp.Props.roleKeys.TryGetValue(role, out List<string>? roleKeys)) keys.UnionWith(roleKeys);
        }
        return keys.Count;
    }

    // ---- Matched sets (Framework 0.246.0; owner, 23 Nov 2026: both engines of a twin-engined jet together) ----

    /// <summary>
    /// The groups a matched set must fill: every listed part playing one of its roles together, or each axle's wheels apart for a set
    /// matched per axle (Framework 0.247.0, per-engine realism step 3: tyres and springs).
    /// </summary>
    List<List<VehicleComponent>> SetGroups(PhobosVehicleFittingDef def)
    {
        List<VehicleComponent> playing = ListedParts().Where(p => def.roles.Any(role => comp.PartPlays(p, role))).ToList();
        if (!def.matchedPerAxle) return new List<List<VehicleComponent>> { playing };
        // Framework 0.250.0: the parts grouped by where they sit along the vehicle (AxleRule, as the window's axle rows group wheels), so
        // parts that sit at the wheels without being wheels (a brake at each wheel) pair by axle too. A part with no hit area stays alone.
        var placed = new List<VehicleComponent>();
        var along = new List<float>();
        var groups = new List<List<VehicleComponent>>();
        foreach (VehicleComponent part in playing)
        {
            if (part.props.hitbox?.Hitbox is { Count: > 0 } cells)
            {
                placed.Add(part);
                along.Add((float)cells.Average(cell => cell.z));
            }
            else groups.Add(new List<VehicleComponent> { part });
        }
        foreach (List<int> axle in AxleRule.Group(along, AxleTolerance)) groups.Add(axle.Select(i => placed[i]).ToList());
        return groups;
    }

    /// <summary>The fitted parts of matched sets whose group is whole, kept until what is fitted changes. Derived, never saved.</summary>
    HashSet<string>? countedKeys;
    int countedVersion = -1;

    /// <summary>Whether a fitted part's fitting counts: an ordinary fitting always; a matched set's only once its group is whole.</summary>
    bool Counts(FittedPart part)
    {
        if (part.def == null) return false;
        if (!part.def.matchedSet) return true;
        if (countedKeys == null || countedVersion != Version)
        {
            countedKeys = WholeSetKeys();
            countedVersion = Version;
        }
        return countedKeys.Contains(part.partKey);
    }

    HashSet<string> WholeSetKeys()
    {
        var keys = new HashSet<string>();
        foreach (PhobosVehicleFittingDef def in fitted.Select(f => f.def).Where(d => d is { matchedSet: true }).Distinct().Cast<PhobosVehicleFittingDef>())
        {
            foreach (List<VehicleComponent> group in SetGroups(def))
            {
                int carrying = group.Count(p => FittedOn(p.props.key) == def);
                if (FittingRule.PartsCounted(carrying, group.Count, matchedSet: true) > 0) keys.UnionWith(group.Select(p => p.props.key));
            }
        }
        return keys;
    }

    /// <summary>Whether this part's group of a matched set is whole: every part of it carries the fitting.</summary>
    public bool GroupWhole(PhobosVehicleFittingDef def, string partKey)
    {
        List<VehicleComponent>? group = SetGroups(def).FirstOrDefault(g => g.Any(p => p.props.key == partKey));
        return group != null && group.All(p => FittedOn(p.props.key) == def);
    }

    /// <summary>How many parts of a matched set still lack it in groups where some carry it; 0 for whole groups or an ordinary fitting.</summary>
    public int SetPartsMissing(PhobosVehicleFittingDef def)
    {
        if (!def.matchedSet) return 0;
        int missing = 0;
        foreach (List<VehicleComponent> group in SetGroups(def))
        {
            int carrying = group.Count(p => FittedOn(p.props.key) == def);
            if (carrying > 0) missing += group.Count - carrying;
        }
        return missing;
    }

    /// <summary>How many parts a matched set goes on together, for what the player reads: the whole set, or the largest axle.</summary>
    public int SetSize(PhobosVehicleFittingDef def) => SetGroups(def).Select(g => g.Count).DefaultIfEmpty(1).Max();

    /// <summary>The parts a fitting goes on as a matched set: the group holding this part. One part for an ordinary fitting.</summary>
    List<VehicleComponent> SetPartsFor(VehicleComponent part, PhobosVehicleFittingDef def) =>
        def.matchedSet ? SetGroups(def).FirstOrDefault(g => g.Any(p => p.props.key == part.props.key)) ?? new List<VehicleComponent> { part }
            : new List<VehicleComponent> { part };

    // ---- What can be done ----

    bool HasPiston => comp.Powertrain?.Spec.Engine == EngineKind.Piston;

    bool? tracked;

    bool IsTracked => tracked ??= TrackedDesign(comp.Vehicle.VehicleDef);

    /// <summary>Whether a design runs on tracks: a hint by name, because nothing else marks a tracked vehicle. Shared with burning tyres
    /// (Framework 0.245.0, FR11), which a tracked vehicle has none of.</summary>
    internal static bool TrackedDesign(VehicleDef def) =>
        def.components?.Any(part => part.key != null && part.key.IndexOf("track", System.StringComparison.OrdinalIgnoreCase) >= 0) == true;

    /// <summary>Whether more engine power reaches the road: a piston engine driving the ground through the full model. A propeller's speed
    /// and an aircraft's flight phase do not follow it, so more power there would only cost heat and fuel.</summary>
    bool PowerCounts => Settings.drivetrainMode == DrivetrainMode.Full && comp.Powertrain is { } train && train.Spec.Engine == EngineKind.Piston
        && train.Spec.Transmission is TransmissionKind.Automatic or TransmissionKind.Direct or TransmissionKind.DieselElectric;

    /// <summary>Whether this fitting would change anything on this vehicle: a loadout can drop a system (a turbine tank has no coolant) and keep its part.</summary>
    bool MovesSomethingHere(PhobosVehicleFittingDef def)
    {
        if (def.statFactors.Count > 0 || def.powerFactor != 1f || def.fuelFactor != 1f || def.grants.Count > 0) return true;
        // Aircraft service-life upgrades: an engine upgrade needs a turbine, more dispensers need dispensers, and a kit must raise a figure
        // the vehicle lacks (an aircraft built with a night sight is offered no night-sight kit, nor a second one).
        if (def.MovesTurbine && comp.Powertrain?.Turbine != null) return true;
        if (def.cartridgeFactor != 1f && comp.Vehicle.GetComp<CompPhobosDispensers>() != null) return true;
        if (def.parameterFloors.Any(spec => comp.Props.systems.Any(system =>
                (spec.system.Length == 0 || system.def.defName == spec.system)
                && system.parameters.TryGetValue(spec.parameter, out double built) && built < spec.value
                && !(ParameterFloor(system.def.defName, spec.parameter) >= spec.value)))) return true;
        return def.parameterFactors.Any(spec => comp.Props.systems.Any(system =>
            (spec.system.Length == 0 || system.def.defName == spec.system) && system.parameters.ContainsKey(spec.parameter)));
    }

    bool Suits(PhobosVehicleFittingDef def) => def.Suits(comp.Repairs.Kilograms, HasPiston, IsTracked, PowerCounts, comp.EngineFuel == EngineFuel.Diesel)
        && def.SuitsLoadout(comp.Props.loadout)
        && (!def.flatDeckOnly || comp.Vehicle.def.HasModExtension<PhobosFlatDeckExtension>())
        && MovesSomethingHere(def);

    /// <summary>How long an answer a screen asks for every frame is kept, in seconds of real time (authored: too short to notice, long
    /// enough that a selected vehicle is not walked part by part sixty times a second; the performance pass, Framework 0.94.0).</summary>
    public const double DisplayCacheSeconds = 0.5;

    Expiring<bool> anythingToFit;
    Expiring<bool> atRigShown;

    /// <summary>Counts every change to what is fitted, tuned or graded, so a screen holding rows built from it knows to build again.</summary>
    public int Version { get; private set; }

    void DropDisplayCaches()
    {
        anythingToFit.Drop();
        atRigShown.Drop();
        Version++;
    }

    /// <summary>Whether the fittings window has anything to offer this vehicle: a part to fit or grade, or an engine or gearbox to tune.
    /// Kept for a moment: buttons ask every frame.</summary>
    public bool AnythingToFit => anythingToFit.TryGet(RealTime.LastRealTime, out bool found) ? found
        : anythingToFit.Set(CanTune(VehicleTuneAxis.Engine) || CanTune(VehicleTuneAxis.Gearing) || ListedParts().Any(), RealTime.LastRealTime, DisplayCacheSeconds);

    /// <summary><see cref="AtRig"/> for a button drawn every frame, kept for a moment; the work itself asks <see cref="AtRig"/>.</summary>
    public bool AtRigShown => atRigShown.TryGet(RealTime.LastRealTime, out bool found) ? found : atRigShown.Set(AtRig(), RealTime.LastRealTime, DisplayCacheSeconds);

    /// <summary>The parts that can take a fitting or be given a grade.</summary>
    public IEnumerable<VehicleComponent> ListedParts()
    {
        if (comp.Vehicle.statHandler == null) yield break;
        foreach (VehicleComponent part in comp.Vehicle.statHandler.components)
        {
            if (part?.props == null) continue;    // not yet given its properties during a load (Recache)
            if (comp.Repairs.CanBeGraded(part) || OptionsFor(part).Any()) yield return part;
        }
    }

    /// <summary>Whether Vehicle Framework has given every part its properties: not yet, early in a load (Framework 0.266.1; Recache).</summary>
    bool PartsReady()
    {
        List<VehicleComponent>? parts = comp.Vehicle.statHandler?.components;
        if (parts == null) return false;
        foreach (VehicleComponent part in parts)
        {
            if (part?.props == null) return false;
        }
        return true;
    }

    /// <summary>How far apart along the vehicle two wheels may sit and still share an axle, in cells (authored).</summary>
    public const float AxleTolerance = 0.75f;

    /// <summary>
    /// The listed parts as the fittings window's rows (Framework 0.89.0; owner, 11 Nov 2026): the wheels grouped into axles (AxleRule, by
    /// where their hit areas sit along the vehicle), every other part on its own, in the list's order with the axles where the first wheel
    /// was. A wheel with no hit area of its own stays on its own, and wheels that make no pair stay as they were.
    /// </summary>
    public List<List<VehicleComponent>> PartRows()
    {
        List<VehicleComponent> parts = ListedParts().ToList();
        var wheels = new List<VehicleComponent>();
        var along = new List<float>();
        foreach (VehicleComponent part in parts)
        {
            if (!comp.PartPlays(part, "Wheels") || part.props.hitbox?.Hitbox is not { Count: > 0 } cells) continue;
            wheels.Add(part);
            along.Add((float)cells.Average(cell => cell.z));
        }
        List<List<int>> axles = AxleRule.Group(along, AxleTolerance);
        bool grouped = axles.Any(axle => axle.Count > 1);
        var rows = new List<List<VehicleComponent>>();
        bool axlesPlaced = false;
        foreach (VehicleComponent part in parts)
        {
            if (!grouped || !wheels.Contains(part))
            {
                rows.Add(new List<VehicleComponent> { part });
                continue;
            }
            if (axlesPlaced) continue;
            axlesPlaced = true;
            foreach (List<int> axle in axles) rows.Add(axle.Select(i => wheels[i]).ToList());
        }
        return rows;
    }

    /// <summary>The fittings that suit this part on this vehicle, whether or not they can be done yet.</summary>
    public IEnumerable<PhobosVehicleFittingDef> OptionsFor(VehicleComponent part)
    {
        foreach (PhobosVehicleFittingDef def in DefDatabase<PhobosVehicleFittingDef>.AllDefsListForReading)
        {
            if (def.roles.Any(role => comp.PartPlays(part, role)) && Suits(def)) yield return def;
        }
    }

    /// <summary>Why this fitting cannot be ordered now; null when it can.</summary>
    public string? Refusal(PhobosVehicleFittingDef def, VehicleComponent part)
    {
        if (def.researchPrerequisite != null && !def.researchPrerequisite.IsFinished)
        {
            return "PhobosVehicleSystems_FitNoResearch".Translate(def.researchPrerequisite.LabelCap.Named("RESEARCH")).Resolve();
        }
        if (def.matchedSet)
        {
            // A matched set goes on every part of its roles or none: any one that cannot take it refuses the set, naming the part.
            foreach (VehicleComponent setPart in SetPartsFor(part, def))
            {
                if (setPart == part || FittedOn(setPart.props.key) == def) continue;
                if (PartRefusal(setPart) is { } other)
                {
                    return "PhobosVehicleSystems_FitSetRefused".Translate(setPart.props.label.Named("PART"), other.Named("REASON")).Resolve();
                }
            }
        }
        return PartRefusal(part);
    }

    /// <summary>Why this part cannot take a fitting now, research aside; null when it can.</summary>
    string? PartRefusal(VehicleComponent part)
    {
        if (part.Health <= 0f) return "PhobosVehicleSystems_FitPartDestroyed".Translate().Resolve();
        PhobosVehiclePartGradeDef grade = GradeOf(part.props.key);
        return grade.holdsFitting ? null : "PhobosVehicleSystems_FitGradeHoldsNone".Translate(grade.label.Named("GRADE")).Resolve();
    }

    /// <summary>Whether this fitting is fitted on any part, working or not (its picture shows either way, as a destroyed part's would).</summary>
    public bool Has(PhobosVehicleFittingDef def) => fitted.Any(f => f.def == def);

    public PhobosVehicleFittingDef? FittedOn(string partKey) => fitted.FirstOrDefault(f => f.partKey == partKey)?.def;

    /// <summary>The kilograms the fittings take from the cargo capacity (Framework 0.129.0, vehicle accessories), wrecked or not: a smashed
    /// fridge still weighs what it weighs until it is taken off.</summary>
    public float KilogramsFitted
    {
        get
        {
            float kilograms = 0f;
            foreach (FittedPart part in fitted) kilograms += part.def?.kilograms ?? 0f;
            return kilograms;
        }
    }

    /// <summary>Puts what a fitting gives back when taken off into the vehicle's cargo (owner, 13 Nov 2026: a removed accessory gives its kit
    /// back), and returns the sentence saying so, or null when it gives nothing.</summary>
    string? GiveBack(PhobosVehicleFittingDef? def)
    {
        if (def == null || def.givesBack.Count == 0) return null;
        var names = new List<string>();
        foreach (ThingDefCountClass back in def.givesBack)
        {
            if (back.thingDef == null || back.count <= 0) continue;
            // A building comes back packed, one thing each (PackedThings, Framework 0.265.0); anything else as one stack.
            int things = Phobos.Framework.Items.PackedThings.Packs(back.thingDef) ? back.count : 1;
            for (int i = 0; i < things; i++) comp.Vehicle.AddOrTransfer(Phobos.Framework.Items.PackedThings.Make(back.thingDef, back.count));
            names.Add(back.Label);
        }
        return names.Count == 0 ? null : "PhobosVehicleSystems_FitKitReturned".Translate(names.ToCommaList(useAnd: true).Named("KIT")).Resolve();
    }

    /// <summary>
    /// Whether a fitting that grants this ability is on the vehicle, working or not (Framework 0.284.0): its part may be shot away. A kit
    /// that is carried but does not work is a fault to report, not a kit taken off.
    /// </summary>
    public bool Carries(string ability) => fitted.Any(f => f.def != null && f.def.grants.Contains(ability) && Counts(f));

    /// <summary>Whether a fitting on this vehicle grants this ability (a cargo hook), its part still standing.</summary>
    public bool Grants(string ability) => fitted.Any(f => f.def != null && f.def.grants.Contains(ability) && Counts(f)
        && comp.Vehicle.statHandler?.GetComponent(f.partKey) is { Health: > 0f });

    /// <summary>The factor on the vehicle's jamming reach from every fitting whose part still stands (Framework 0.224.0): 1 with none.</summary>
    public float JamRadiusFactor
    {
        get
        {
            float factor = 1f;
            foreach (FittedPart part in fitted)
            {
                if (part.def == null || part.def.jamRadiusFactor == 1f || !Counts(part)) continue;
                if (comp.Vehicle.statHandler?.GetComponent(part.partKey) is { Health: > 0f }) factor *= part.def.jamRadiusFactor;
            }
            return factor;
        }
    }

    /// <summary>The part whose fitting grants this ability (the cargo hook's mount), still standing; null with none (Framework 0.185.0).</summary>
    public VehicleComponent? PartGranting(string ability)
    {
        foreach (FittedPart part in fitted)
        {
            if (part.def == null || !part.def.grants.Contains(ability) || !Counts(part)) continue;
            if (comp.Vehicle.statHandler?.GetComponent(part.partKey) is { Health: > 0f } standing) return standing;
        }
        return null;
    }

    /// <summary>The fittings on parts that are not destroyed, as <see cref="Grants"/> counts them.</summary>
    public IEnumerable<PhobosVehicleFittingDef> FittedDefs() => fitted
        .Where(f => f.def != null && Counts(f) && comp.Vehicle.statHandler?.GetComponent(f.partKey) is { Health: > 0f })
        .Select(f => f.def!);

    public FitOrder? OrderOn(string partKey) => orders.FirstOrDefault(o => o.partKey == partKey);

    public FitOrder? OrderFor(VehicleTuneAxis axis) => orders.FirstOrDefault(o => o.tune != null && o.tune.axis == axis);

    /// <summary>Lets the self-test work a tune on its throwaway vehicle, which stands on no map and so is near no rig.</summary>
    internal static bool AssumeRigForTest;

    static List<ThingDef>? rigDefs;

    static List<ThingDef> RigDefs => rigDefs ??= DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.GetCompProperties<CompProperties_PhobosTuningRig>() != null).ToList();

    /// <summary>The furthest any tuning rig reaches, for what the player reads.</summary>
    public static int RigReach => RigDefs.Count == 0 ? 0 : RigDefs.Max(d => d.GetCompProperties<CompProperties_PhobosTuningRig>().reach);

    /// <summary>Whether a tune or a change of grade could be worked here and now: within reach of a working rig of the player's, or the setting is off.</summary>
    public bool AtRig() => !Settings.tuningNeedsRig || AssumeRigForTest || NearRig();

    /// <summary>Whether the vehicle stands within reach of a working rig of the player's, whatever the setting: what a diagnosis needs, and
    /// what makes a new part's fitting better (Framework 0.73.0).</summary>
    public bool NearRig()
    {
        if (AssumeRigForTest) return true;
        VehiclePawn vehicle = comp.Vehicle;
        if (!vehicle.Spawned) return false;
        // Framework 0.130.0 (step S8): a repair job asks every tick, so the answer is kept for a second of game time while the vehicle stands
        // where it stood (paused, it is kept: the inspect pane asks every frame), and the rigs come from the map's own list.
        int now = Find.TickManager.TicksGame;
        if (now - nearRigTick < NearRigTicks && vehicle.Position == nearRigCell && vehicle.Rotation == nearRigRotation && vehicle.Map == nearRigMap) return nearRig;
        CellRect area = DrawnArea(vehicle);
        bool found = false;
        foreach (CompPhobosTuningRig station in CompPhobosTuningRig.PlayersOn(vehicle.Map))
        {
            // A dyno bay a working service yard runs takes its vehicles in turn (Framework 0.210.0): standing in its reach is not enough.
            if (station.Working && station.Reaches(area) && !ServiceYards.RunsLine(station))
            {
                found = true;
                break;
            }
        }
        // The vehicle whose turn it is on a yard's dyno counts as at the dyno where it stands ("with no moving", owner, 20 Nov 2026).
        if (!found) found = ServiceYards.HasTurn(vehicle);
        nearRig = found;
        nearRigTick = now;
        nearRigCell = vehicle.Position;
        nearRigRotation = vehicle.Rotation;
        nearRigMap = vehicle.Map;
        return found;
    }

    /// <summary>How long the rig answer is kept: a second of game time (authored). Derived, never saved.</summary>
    const int NearRigTicks = 60;
    bool nearRig;
    int nearRigTick = int.MinValue / 2;
    IntVec3 nearRigCell;
    Rot4 nearRigRotation;
    Map? nearRigMap;

    /// <summary>The cells the vehicle's picture covers, not only those it occupies (Framework 0.77.0): an aircraft's wings and rotor spread
    /// far past its footprint, and a rig placed clear of them was out of reach. Turned east or west, the picture turns with it.</summary>
    internal static CellRect DrawnArea(VehiclePawn vehicle)
    {
        CellRect rect = vehicle.OccupiedRect();
        Vector2 drawn = vehicle.VehicleDef.graphicData?.drawSize ?? Vector2.zero;
        IntVec2 size = vehicle.VehicleDef.Size;
        bool turned = vehicle.Rotation.IsHorizontal;
        int marginX = ReachRule.Margin(turned ? drawn.y : drawn.x, turned ? size.z : size.x);
        int marginZ = ReachRule.Margin(turned ? drawn.x : drawn.y, turned ? size.x : size.z);
        return new CellRect(rect.minX - marginX, rect.minZ - marginZ, rect.Width + 2 * marginX, rect.Height + 2 * marginZ);
    }

    /// <summary>Whether this vehicle's window should say that tunes and grades will wait.</summary>
    public bool RigMissing => Settings.tuningNeedsRig && !AtRig();

    /// <summary>Whether this axis can be tuned here: an engine whose power reaches the road, and gears the model actually reads.</summary>
    public bool CanTune(VehicleTuneAxis axis)
    {
        if (!PowerCounts) return false;
        return axis switch
        {
            VehicleTuneAxis.Engine => true,
            // Framework 0.123.0: boost needs something to boost (the turbocharger grants it).
            VehicleTuneAxis.Boost => HasBoost,
            _ => comp.Powertrain!.Spec.Transmission is TransmissionKind.Automatic or TransmissionKind.Direct,
        };
    }

    /// <summary>The ability a fitting grants that opens the boost axis (the turbocharger).</summary>
    public const string BoostAbility = "Boost";

    /// <summary>Whether a fitting on the vehicle grants boost (Framework 0.123.0), counted from what is fitted, so a boost tune stands down the
    /// moment its turbocharger is taken off or lost.</summary>
    bool HasBoost => fitted.Any(f => f.def != null && f.def.grants.Contains(BoostAbility) && Counts(f));

    /// <summary>The tunes that act now: all of them, but a boost tune only while its turbocharger is fitted.</summary>
    IEnumerable<PhobosVehicleTuneDef> ActiveTunes() => tunes.Where(t => t.axis != VehicleTuneAxis.Boost || HasBoost);

    public static IEnumerable<PhobosVehicleTuneDef> TunesOf(VehicleTuneAxis axis) =>
        DefDatabase<PhobosVehicleTuneDef>.AllDefsListForReading.Where(t => t.axis == axis).OrderBy(t => t.order);

    public PhobosVehicleTuneDef? Current(VehicleTuneAxis axis) => tunes.FirstOrDefault(t => t.axis == axis) ?? TunesOf(axis).FirstOrDefault(t => t.isDefault);

    /// <summary>True when the engine model works out fuel itself (the player's setting); otherwise each tune's flat fuel factor applies.</summary>
    public static bool FuelFollowsModel => VehicleSystemsMod.Settings.loadDependentFuel;

    /// <summary>What one tune does to the fuel burned, as <see cref="Powertrain.ApplyFuel"/> applies it: its own factor, and its flat factor
    /// only when fuel does not follow the engine model.</summary>
    public static float TuneFuelFactor(PhobosVehicleTuneDef tune) => tune.fuelFactor * (FuelFollowsModel ? 1f : tune.flatFuelFactor);

    /// <summary>This vehicle's final drive ratio as built, before any gearing tune; NaN without a drivetrain model.</summary>
    public float FinalDriveAsBuilt => comp.Powertrain is { } train && FinalDriveFactor > 0f ? (float)train.Spec.FinalDrive / FinalDriveFactor : float.NaN;

    // ---- Orders ----

    public void OrderFitting(VehicleComponent part, PhobosVehicleFittingDef def) => OrderFitting(part, def, quiet: false);

    void OrderFitting(VehicleComponent part, PhobosVehicleFittingDef def, bool quiet)
    {
        if (!def.matchedSet)
        {
            orders.RemoveAll(o => o.partKey == part.props.key);
            Add(new FitOrder { partKey = part.props.key, fitting = def }, quiet);
            return;
        }
        // A matched set is ordered on every part of its roles that does not carry it or wait for it already, one job a part.
        int ordered = 0;
        foreach (VehicleComponent setPart in SetPartsFor(part, def))
        {
            string key = setPart.props.key;
            if (FittedOn(key) == def || OrderOn(key)?.fitting == def) continue;
            orders.RemoveAll(o => o.partKey == key);
            Add(new FitOrder { partKey = key, fitting = def }, quiet: true);
            ordered++;
        }
        if (quiet || ordered == 0) return;
        VehicleNotices.Send("PhobosVehicleSystems_FitSetOrdered".Translate(def.label.Named("FITTING"), comp.Vehicle.LabelShort.Named("VEHICLE"),
            ordered.Named("COUNT")), MessageTypeDefOf.SilentInput, historical: false);
    }

    // ---- Families (Framework 0.103.0; docs/development/vehicle-upgrades-round-two.md) ----

    /// <summary>The families of fittings this vehicle can take, in their order: those with a fitting that suits one of its parts.</summary>
    public IEnumerable<PhobosVehicleFamilyDef> Families() =>
        DefDatabase<PhobosVehicleFamilyDef>.AllDefsListForReading.Where(SuitsFamily).OrderBy(f => f.order);

    bool SuitsFamily(PhobosVehicleFamilyDef family) =>
        ListedParts().Any(part => OptionsFor(part).Any(family.fittings.Contains));

    /// <summary>One piece of a family's work on this vehicle: a fitting on a part.</summary>
    public readonly struct FamilyStep
    {
        public readonly VehicleComponent Part;
        public readonly PhobosVehicleFittingDef Fitting;
        /// <summary>The fitting the step takes off the part to make room, or null.</summary>
        public readonly PhobosVehicleFittingDef? Replaces;

        public FamilyStep(VehicleComponent part, PhobosVehicleFittingDef fitting, PhobosVehicleFittingDef? replaces)
        {
            Part = part;
            Fitting = fitting;
            Replaces = replaces;
        }
    }

    /// <summary>
    /// What ordering a family would do here: each of its fittings on every part it suits that does not carry it and is not already waiting
    /// for it, one fitting to a part, the family's own order first. A part another fitting holds is fitted over, and the step says which.
    /// Parts refused (a destroyed part, a salvaged grade, unfinished research) are left out, and the first such reason is returned.
    /// </summary>
    public List<FamilyStep> FamilyPlan(PhobosVehicleFamilyDef family, out string? firstRefusal)
    {
        firstRefusal = null;
        var steps = new List<FamilyStep>();
        var taken = new HashSet<string>();
        foreach (PhobosVehicleFittingDef fitting in family.fittings)
        {
            foreach (VehicleComponent part in ListedParts())
            {
                string key = part.props.key;
                if (taken.Contains(key) || !OptionsFor(part).Contains(fitting)) continue;
                PhobosVehicleFittingDef? now = FittedOn(key);
                if (now == fitting || OrderOn(key)?.fitting == fitting)
                {
                    // Framework 0.123.0: a part that already carries (or awaits) one of the family's fittings is taken by it, so a later fitting
                    // of the same family for the same part (the battery warmer after the deep-cycle battery) never fits over it, and back.
                    taken.Add(key);
                    continue;
                }
                if (Refusal(fitting, part) is { } refusal)
                {
                    firstRefusal ??= refusal;
                    continue;
                }
                taken.Add(key);
                steps.Add(new FamilyStep(part, fitting, now));
            }
        }
        return steps;
    }

    /// <summary>Why the family cannot be ordered on this vehicle now; null when it can.</summary>
    public string? FamilyRefusal(PhobosVehicleFamilyDef family)
    {
        if (FamilyPlan(family, out string? refusal).Count > 0) return null;
        if (refusal != null) return refusal;
        return "PhobosVehicleSystems_FamilyAllDone".Translate(family.label.Named("FAMILY")).Resolve();
    }

    /// <summary>Whether every part the family suits on this vehicle already carries its fitting.</summary>
    public bool FamilyFitted(PhobosVehicleFamilyDef family) =>
        FamilyPlan(family, out string? refusal).Count == 0 && refusal == null
        && family.fittings.Any(f => ListedParts().Any(p => FittedOn(p.props.key) == f));

    /// <summary>
    /// Orders the whole family (owner, 12 Nov 2026: a family in one order, or part by part): one job a part, worked by a mechanic as any
    /// fitting is, and the family's switches turned on. Says what was ordered, or why nothing was.
    /// </summary>
    public void OrderFamily(PhobosVehicleFamilyDef family)
    {
        List<FamilyStep> steps = FamilyPlan(family, out string? refusal);
        if (steps.Count == 0)
        {
            VehicleNotices.Send(FamilyRefusal(family) ?? refusal ?? "", MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        foreach (FamilyStep step in steps) OrderFitting(step.Part, step.Fitting, quiet: true);
        int switched = 0;
        foreach (string name in family.switchOn)
        {
            ElectricalLoadSpec? load = comp.LoadNamed(name);
            if (load is { toggle: false }) load = null;
            if (load == null || comp.IsOn(load)) continue;
            comp.SetSwitch(load, true);
            switched++;
        }
        string text = "PhobosVehicleSystems_FamilyOrdered".Translate(family.label.Named("FAMILY"), comp.Vehicle.LabelShort.Named("VEHICLE"),
            steps.Count.Named("COUNT")).Resolve();
        if (switched > 0) text += " " + "PhobosVehicleSystems_FamilySwitchedOn".Translate().Resolve();
        if (refusal != null) text += " " + "PhobosVehicleSystems_FamilyPartlyRefused".Translate(refusal.Named("REASON")).Resolve();
        VehicleNotices.Send(text, MessageTypeDefOf.TaskCompletion, historical: false);
    }

    public void OrderRemoval(VehicleComponent part)
    {
        // A matched set comes off every part together, as it went on.
        PhobosVehicleFittingDef? on = FittedOn(part.props.key);
        List<VehicleComponent> parts = on is { matchedSet: true } ? SetPartsFor(part, on).Where(p => FittedOn(p.props.key) == on).ToList()
            : new List<VehicleComponent> { part };
        foreach (VehicleComponent setPart in parts)
        {
            orders.RemoveAll(o => o.partKey == setPart.props.key);
            Add(new FitOrder { partKey = setPart.props.key, remove = true }, quiet: setPart != part);
        }
    }

    public void OrderTune(PhobosVehicleTuneDef tune)
    {
        orders.RemoveAll(o => o.tune != null && o.tune.axis == tune.axis);
        Add(new FitOrder { tune = tune });
    }

    /// <summary>Asks for a part of this grade: a refit of a working part, or what a destroyed part is replaced with.</summary>
    public void OrderGrade(VehicleComponent part, PhobosVehiclePartGradeDef grade)
    {
        orders.RemoveAll(o => o.partKey == part.props.key);
        Add(new FitOrder { partKey = part.props.key, grade = grade });
    }

    void Add(FitOrder order, bool quiet = false)
    {
        orders.Add(order);
        comp.Repairs.Invalidate();
        if (quiet) return;
        VehicleNotices.Send("PhobosVehicleSystems_FitOrderedMessage".Translate(LabelOf(order).CapitalizeFirst().Named("WORK"), comp.Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput, historical: false);
    }

    public void Cancel(FitOrder order)
    {
        if (!orders.Remove(order)) return;
        // A matched set's orders go together: half a set ordered would leave the vehicle waiting on a job nobody asked for.
        PhobosVehicleFittingDef? set = order.fitting is { matchedSet: true } ? order.fitting
            : order.remove && FittedOn(order.partKey) is { matchedSet: true } on ? on : null;
        if (set != null)
        {
            orders.RemoveAll(o => order.fitting != null ? o.fitting == set : o.remove && FittedOn(o.partKey) == set);
        }
        comp.Repairs.Invalidate();
        VehicleNotices.Send("PhobosVehicleSystems_FitCancelledMessage".Translate(LabelOf(order).CapitalizeFirst().Named("WORK"), comp.Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput, historical: false);
    }

    string PartLabel(string partKey) => comp.Vehicle.statHandler?.GetComponent(partKey)?.props.label ?? partKey;

    public string LabelOf(FitOrder order)
    {
        if (order.tune != null)
        {
            return "PhobosVehicleSystems_TuneWork".Translate(order.tune.label.Named("SETTING")).Resolve();
        }
        NamedArgument part = PartLabel(order.partKey).Named("PART");
        if (order.grade != null)
        {
            bool destroyed = comp.Vehicle.statHandler?.GetComponent(order.partKey)?.Health <= 0f;
            if (destroyed) return "PhobosVehicleSystems_RepairReplaceGraded".Translate(part, order.grade.label.Named("GRADE")).Resolve();
            return "PhobosVehicleSystems_GradeWork".Translate(part, order.grade.label.Named("GRADE")).Resolve();
        }
        if (order.remove) return "PhobosVehicleSystems_FitRemoveWork".Translate((FittedOn(order.partKey)?.label ?? "").Named("FITTING"), part).Resolve();
        return "PhobosVehicleSystems_FitWork".Translate(order.fitting!.label.Named("FITTING"), part).Resolve();
    }

    public float WorkOf(FitOrder order) => WorkFor(order.tune?.work ?? (order.remove ? (FittedOn(order.partKey)?.work ?? 600f) * RemovalWorkShare : order.fitting?.work ?? 1500f));

    /// <summary>Taking a fitting off is half the work of putting it on (authored).</summary>
    const float RemovalWorkShare = 0.5f;

    public int SkillOf(FitOrder order) => order.tune?.minSkill ?? order.fitting?.minSkill ?? order.grade?.minSkill ?? 0;

    public IEnumerable<ThingDefCountClass> MaterialsOf(FitOrder order) =>
        MaterialsFor(order.tune?.materials ?? order.fitting?.materials ?? Enumerable.Empty<ThingDefCountClass>());

    /// <summary>Does the order, says what was done, and returns the sentence. Materials are the repair's to take.</summary>
    public string Complete(FitOrder order, Pawn mechanic)
    {
        NamedArgument who = mechanic.LabelShort.Named("MECHANIC");
        NamedArgument vehicle = comp.Vehicle.LabelShort.Named("VEHICLE");
        if (VehicleSystemsMod.RepairsLog.DebugEnabled)
        {
            VehicleSystemsMod.RepairsLog.Debug(comp.Vehicle.LabelShort + " (" + comp.Vehicle.ThingID + ") fit order by " + mechanic.LabelShort + ": part " + order.partKey
                + (order.grade != null ? ", grade " + order.grade.defName : "") + (order.fitting != null ? ", fitting " + order.fitting.defName : "")
                + (order.tune != null ? ", tune " + order.tune.defName : "") + (order.remove ? ", removal" : ""));
        }
        string text;
        if (order.grade != null)
        {
            VehicleComponent part = comp.Vehicle.statHandler.GetComponent(order.partKey);
            SetGrade(part, order.grade);
            if (!order.grade.holdsFitting) fitted.RemoveAll(f => f.partKey == order.partKey);
            comp.Conditions.RemoveOn(order.partKey);    // a new part has none of the old one's troubles
            comp.RestorePart(part, 1f);
            text = "PhobosVehicleSystems_GradeDone".Translate(who, vehicle, part.props.label.Named("PART"), order.grade.label.Named("GRADE")).Resolve();
            // Seen in play (Framework 0.293.0, row 9): a part's grade changed, by the grade fitted.
            Seen.Note("vehicles.grade-done", comp.Vehicle, part.props.label, order.grade.defName);
        }
        else if (order.tune != null)
        {
            tunes.RemoveAll(t => t.axis == order.tune.axis);
            if (!order.tune.isDefault) tunes.Add(order.tune);
            text = "PhobosVehicleSystems_TuneDone".Translate(who, vehicle, order.tune.label.Named("SETTING")).Resolve();
            // The key is built from the tune's name (Framework 0.123.0); config/test-evidence.json lists it as a prefix.
            if (Seen.On) Seen.Note("vehicles.tune-done." + order.tune.defName, comp.Vehicle);
        }
        else if (order.remove)
        {
            PhobosVehicleFittingDef? old = FittedOn(order.partKey);
            string label = old?.label ?? "";
            fitted.RemoveAll(f => f.partKey == order.partKey);
            text = "PhobosVehicleSystems_FitRemovedDone".Translate(who, label.Named("FITTING"), vehicle, PartLabel(order.partKey).Named("PART")).Resolve();
            if (GiveBack(old) is { } back) text += " " + back;
        }
        else
        {
            Renew(order.fitting!, order.partKey);
            PhobosVehicleFittingDef? replaced = FittedOn(order.partKey);
            fitted.RemoveAll(f => f.partKey == order.partKey);
            fitted.Add(new FittedPart { partKey = order.partKey, def = order.fitting });
            text = "PhobosVehicleSystems_FitDone".Translate(who, order.fitting!.label.Named("FITTING"), vehicle, PartLabel(order.partKey).Named("PART")).Resolve();
            // A fitting put on over another takes that one off: its kit comes back too (Framework 0.129.0).
            if (replaced != null && replaced != order.fitting && GiveBack(replaced) is { } back) text += " " + back;
            if (Seen.On) Seen.Note("vehicles.fitting-done." + order.fitting.defName, comp.Vehicle, PartLabel(order.partKey));
            // A matched set made whole by this part (Framework 0.246.0): the effect starts now.
            if (Seen.On && order.fitting.matchedSet && GroupWhole(order.fitting, order.partKey))
            {
                if (order.fitting.matchedPerAxle) Seen.Note("vehicles.axle-matched", comp.Vehicle, order.fitting.defName);
                else Seen.Note("vehicles.matched-set-whole", comp.Vehicle, order.fitting.defName);
            }
        }
        orders.Remove(order);
        Changed();
        return text;
    }

    /// <summary>
    /// A fitting that renews wear (new tyres renew the tread) takes away this part's share of it: the parts of its roles that do not yet
    /// carry this fitting share the wear, and this one leaves them. Called before the fitting is recorded on the part.
    /// </summary>
    void Renew(PhobosVehicleFittingDef def, string partKey)
    {
        if (def.renews.Count == 0) return;
        List<string> keys = def.roles.Where(comp.Props.roleKeys.ContainsKey).SelectMany(role => comp.Props.roleKeys[role]).Distinct().ToList();
        if (!keys.Contains(partKey)) return;
        int notYetRenewed = keys.Count(key => key == partKey || FittedOn(key) != def);
        foreach (string quantity in def.renews)
        {
            if (comp.TryGetQuantity(quantity, out float wear)) comp.SetQuantity(quantity, (float)FittingRule.WearAfterRenewing(wear, notYetRenewed));
        }
    }

    void Changed(FuelExcess excess = FuelExcess.Drain)
    {
        DropDisplayCaches();
        statFactors = null;
        parameterFactors = null;
        countedKeys = null;
        SyncFuelCapacity(excess);
        comp.Notify_FittingsChanged();
    }

    // ---- Extra tanks (the long range family, Framework 0.103.0) ----

    /// <summary>The factor the fitted tanks give the fuel capacity.</summary>
    public float FuelCapacityFactor
    {
        get
        {
            if (Nothing) return 1f;
            Recache();
            return fuelCapacity;
        }
    }

    /// <summary>
    /// Puts the tanks' share into Vehicle Framework's fuel capacity offset, or takes it out (VehicleStatHandler.AddStatOffset with its own
    /// FuelCapacity category, the way its upgrade trees raise a tank; no Harmony). Safe to call any number of times: it moves the offset by
    /// the difference from what it last added. Then, if the tank now holds more than it can, the fuel above is dealt with as
    /// <paramref name="excess"/> says: drained into containers beside a vehicle on a map (a tank taken off by a mechanic), lost (a tank
    /// destroyed), or kept until it is burnt (a load).
    /// </summary>
    public void SyncFuelCapacity(FuelExcess excess)
    {
        VehiclePawn? vehicle = comp.Vehicle;
        CompFueledTravel? tank = vehicle?.CompFueledTravel;
        VehicleStatHandler? handler = vehicle?.statHandler;
        if (vehicle == null || tank == null || handler == null) return;
        float wanted = (float)FittingRule.FuelCapacityOffset(tank.FuelCapacity - appliedFuelOffset, FuelCapacityFactor);
        if (Mathf.Abs(wanted - appliedFuelOffset) > 0.001f)
        {
            handler.AddStatOffset(VehicleStatUpgradeCategoryDefOf.FuelCapacity, wanted - appliedFuelOffset);
            appliedFuelOffset = wanted;
        }
        if (excess == FuelExcess.Keep) return;
        double above = FittingRule.FuelAbove(tank.Fuel, tank.FuelCapacity);
        if (above < 0.01) return;
        int drained = 0;
        if (excess == FuelExcess.Drain && vehicle.Spawned && tank.Props.fuelType != null)
        {
            for (int left = FittingRule.WholeUnits(above); left > 0;)
            {
                Thing fuel = ThingMaker.MakeThing(tank.Props.fuelType);
                fuel.stackCount = Mathf.Min(left, tank.Props.fuelType.stackLimit);
                left -= fuel.stackCount;
                drained += fuel.stackCount;
                GenPlace.TryPlaceThing(fuel, vehicle.Position, vehicle.Map, ThingPlaceMode.Near);
            }
        }
        // Vehicle Framework clamps the tank to its capacity whenever it burns fuel; burning nothing does only that.
        tank.ConsumeFuel(0f);
        if (vehicle.Faction != Faction.OfPlayer) return;
        if (drained > 0)
        {
            VehicleNotices.Send("PhobosVehicleSystems_TankDrained".Translate(vehicle.LabelShort.Named("VEHICLE"), drained.Named("COUNT"),
                tank.Props.fuelType!.label.Named("FUEL")), Where, MessageTypeDefOf.NeutralEvent);
        }
        else
        {
            VehicleNotices.Send("PhobosVehicleSystems_TankSpilled".Translate(vehicle.LabelShort.Named("VEHICLE"),
                ((float)above).ToString("0").Named("COUNT")), Where, MessageTypeDefOf.NegativeEvent);
        }
    }

    // ---- Grades ----

    /// <summary>The key of a grade's entry in the part's health modifiers. They are not saved, so they are put back after every load.</summary>
    internal const string GradeHealthKey = "PhobosVehicleSystems_Grade";

    static PhobosVehiclePartGradeDef? defaultGrade;

    public static PhobosVehiclePartGradeDef DefaultGrade => defaultGrade ??= DefDatabase<PhobosVehiclePartGradeDef>.AllDefsListForReading.First(g => g.isDefault);

    public static IEnumerable<PhobosVehiclePartGradeDef> Grades => DefDatabase<PhobosVehiclePartGradeDef>.AllDefsListForReading.OrderBy(g => g.order);

    public PhobosVehiclePartGradeDef GradeOf(string partKey) => graded.FirstOrDefault(g => g.partKey == partKey)?.def ?? DefaultGrade;

    /// <summary>What a destroyed part will be replaced with: what was asked for, else what it was.</summary>
    public PhobosVehiclePartGradeDef GradeForReplacement(string partKey) => OrderOn(partKey)?.grade ?? GradeOf(partKey);

    /// <summary>How hard a fault hits this part; nothing is read when the vehicle has no graded parts.</summary>
    public float FaultFactor(string partKey) => graded.Count == 0 ? 1f : GradeOf(partKey).faultFactor;

    /// <summary>Why this grade cannot be asked for now; null when it can.</summary>
    public string? Refusal(PhobosVehiclePartGradeDef grade, VehicleComponent part)
    {
        if (grade.researchPrerequisite != null && !grade.researchPrerequisite.IsFinished)
        {
            return "PhobosVehicleSystems_FitNoResearch".Translate(grade.researchPrerequisite.LabelCap.Named("RESEARCH")).Resolve();
        }
        if (part.Health <= 0f) return comp.Repairs.PartBlocked(part) ? null : "PhobosVehicleSystems_FitPartDestroyed".Translate().Resolve();
        return !grade.holdsFitting && FittedOn(part.props.key) != null ? "PhobosVehicleSystems_GradeTakeFittingOff".Translate().Resolve() : null;
    }

    void SetGrade(VehicleComponent part, PhobosVehiclePartGradeDef grade)
    {
        graded.RemoveAll(g => g.partKey == part.props.key);
        if (!grade.isDefault) graded.Add(new GradedPart { partKey = part.props.key, def = grade });
        ApplyHealth(part, grade);
        Changed();
    }

    /// <summary>A destroyed part has been replaced: it is now of the grade asked for.</summary>
    public void FinishReplacement(VehicleComponent part)
    {
        PhobosVehiclePartGradeDef grade = GradeForReplacement(part.props.key);
        orders.RemoveAll(o => o.partKey == part.props.key && o.grade != null);
        SetGrade(part, grade);
    }

    static void ApplyHealth(VehicleComponent part, PhobosVehiclePartGradeDef grade)
    {
        double modifier = FittingRule.GradeHealthModifier(part.props.health, grade.healthFactor);
        if (modifier == 0) part.AddHealthModifiers.Remove(GradeHealthKey);
        else part.AddHealthModifiers[GradeHealthKey] = (float)modifier;
    }

    /// <summary>Puts the grades' health back: Vehicle Framework does not save these modifiers. Safe to call more than once, and asks nothing of
    /// what has or has not finished loading beyond the parts themselves.</summary>
    public void ReapplyGrades()
    {
        VehicleStatHandler? handler = comp.Vehicle?.statHandler;
        if (handler == null) return;
        foreach (GradedPart entry in graded)
        {
            VehicleComponent? part = handler.GetComponent(entry.partKey);
            if (part != null) ApplyHealth(part, entry.def!);
        }
    }

    /// <summary>The part's maximum health as a standard part, for what its replacement costs.</summary>
    public static float UngradedHealth(VehicleComponent part) =>
        part.MaxHealth - (part.AddHealthModifiers.TryGetValue(GradeHealthKey, out float modifier) ? modifier : 0f);

    // ---- What happens to them ----

    /// <summary>A fitting is lost with its part (owner), and work ordered on that part goes with it. Both say so on a player's vehicle.</summary>
    public void Sweep()
    {
        if (fitted.Count == 0 && orders.Count == 0) return;
        VehicleStatHandler? handler = comp.Vehicle.statHandler;
        if (handler == null) return;
        bool player = comp.Vehicle.Faction == Faction.OfPlayer;
        bool changed = false;
        for (int i = fitted.Count - 1; i >= 0; i--)
        {
            VehicleComponent? part = handler.GetComponent(fitted[i].partKey);
            if (part != null && part.Health > 0f) continue;
            if (player && part != null)
            {
                TellLost(fitted[i].def!, part);
            }
            fitted.RemoveAt(i);
            changed = true;
        }
        bool orderLost = false;
        for (int i = orders.Count - 1; i >= 0; i--)
        {
            FitOrder order = orders[i];
            if (order.partKey.NullOrEmpty()) continue;
            VehicleComponent? part = handler.GetComponent(order.partKey);
            if (part != null && part.Health > 0f) continue;
            if (order.grade != null && part != null && comp.Repairs.PartBlocked(part)) continue;    // the replacement's own choice: it waits for that job
            if (player)
            {
                VehicleNotices.Send("PhobosVehicleSystems_FitOrderLost".Translate(LabelOf(order).CapitalizeFirst().Named("WORK"), comp.Vehicle.LabelShort.Named("VEHICLE")), Where, MessageTypeDefOf.NegativeEvent);
            }
            orders.RemoveAt(i);
            orderLost = true;
        }
        if (changed) Changed(FuelExcess.Lose);
        else if (orderLost) comp.Repairs.Invalidate();
    }

    /// <summary>Where a message about this vehicle points: nowhere, while it is in a caravan or a garage.</summary>
    LookTargets Where => comp.Vehicle.Spawned ? comp.Vehicle : LookTargets.Invalid;

    void TellLost(PhobosVehicleFittingDef def, VehicleComponent part) =>
        VehicleNotices.Send("PhobosVehicleSystems_FitLost".Translate(comp.Vehicle.LabelShort.Named("VEHICLE"), def.label.Named("FITTING"), part.props.label.Named("PART")), Where, MessageTypeDefOf.NegativeEvent);

    /// <summary>A destroyed part mended somewhere no sweep saw it destroyed (a garage): the fitting went with the old one.</summary>
    public void LoseWith(VehicleComponent part)
    {
        PhobosVehicleFittingDef? lost = FittedOn(part.props.key);
        if (lost == null) return;
        fitted.RemoveAll(f => f.partKey == part.props.key);
        if (comp.Vehicle.Faction == Faction.OfPlayer) TellLost(lost, part);
        Changed(FuelExcess.Lose);
    }

    /// <summary>
    /// A vehicle from a save made before its loadout began without some fittings is given them once, free, on the first part each suits, so
    /// it keeps what it had (aircraft service-life upgrades). Says so to the player for their own vehicles. Runs at spawn and changes only
    /// this vehicle's list of fittings: nothing is spawned.
    /// </summary>
    public void GiveOlderSaveFittings()
    {
        if (olderSaveFitsSettled) return;
        olderSaveFitsSettled = true;
        if (comp.Props.loadout?.fittedOnOlderSaves is not { Count: > 0 } list) return;
        var given = new List<string>();
        foreach (PhobosVehicleFittingDef def in list)
        {
            if (def == null || Has(def)) continue;
            string? key = def.roles.Where(comp.Props.roleKeys.ContainsKey).SelectMany(role => comp.Props.roleKeys[role]).FirstOrDefault(k => FittedOn(k) == null);
            if (key == null) continue;
            fitted.Add(new FittedPart { partKey = key, def = def });
            given.Add(def.label);
        }
        if (given.Count == 0) return;
        Changed();
        if (Seen.Wants("vehicles.older-save-fitted", comp.Vehicle)) Seen.Note("vehicles.older-save-fitted", comp.Vehicle, string.Join(", ", given));
        if (comp.Vehicle.Faction == Faction.OfPlayer)
            Messages.Message("PhobosVehicleSystems_OlderSaveFitted".Translate(comp.Vehicle.LabelShort.Named("VEHICLE"), given.ToCommaList(useAnd: true).Named("FITTINGS")),
                comp.Vehicle, MessageTypeDefOf.NeutralEvent, historical: false);
    }

    /// <summary>The first time another faction's vehicle appears: by chance, a set suited to its faction's tech level (owner, 9 Nov 2026).</summary>
    public void RollEnemySet()
    {
        if (setRolled) return;
        setRolled = true;
        Faction? faction = comp.Vehicle.Faction;
        if (faction == null || faction == Faction.OfPlayer || !Settings.enemyFitSets) return;
        if (!Rand.Chance(PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().enemyFitSetChance)) return;
        TechLevel tech = faction.def.techLevel;
        // Only a set with something for this vehicle (aircraft service-life upgrades, 21 Nov 2026): an aircraft's set rolled for a truck, or a
        // truck's for an aircraft, would fit nothing and leave it bare, taking the chance from the sets that suit it.
        if (!DefDatabase<PhobosVehicleFitSetDef>.AllDefsListForReading.Where(s => s.Suits(tech) && FitsSomething(s))
                .TryRandomElementByWeight(s => s.weight, out PhobosVehicleFitSetDef set)) return;
        Apply(set);
    }

    /// <summary>Puts a set on the vehicle: each fitting on every free part it suits (one axle of four would be a sliver of the effect), and each tune the vehicle can take.</summary>
    /// <summary>Puts a fitting straight on a part, with no order, materials or mechanic: for the self-test, which needs a cargo hook on a
    /// throwaway helicopter.</summary>
    internal void FitForTest(string partKey, PhobosVehicleFittingDef def)
    {
        fitted.RemoveAll(f => f.partKey == partKey);
        fitted.Add(new FittedPart { partKey = partKey, def = def });
        Changed();
    }

    /// <summary>Whether a set has a fitting this vehicle would take, or a tune it can take.</summary>
    bool FitsSomething(PhobosVehicleFitSetDef set) =>
        set.fittings.Any(def => def != null && Suits(def) && def.roles.Any(comp.Props.roleKeys.ContainsKey))
        || set.tunes.Any(tune => tune != null && !tune.isDefault && CanTune(tune.axis));

    public void Apply(PhobosVehicleFitSetDef set)
    {
        foreach (PhobosVehicleFittingDef def in set.fittings)
        {
            if (!Suits(def)) continue;
            foreach (string key in def.roles.Where(comp.Props.roleKeys.ContainsKey).SelectMany(role => comp.Props.roleKeys[role]).Distinct().ToList())
            {
                if (FittedOn(key) == null) fitted.Add(new FittedPart { partKey = key, def = def });
            }
        }
        foreach (PhobosVehicleTuneDef tune in set.tunes)
        {
            if (tune.isDefault || !CanTune(tune.axis)) continue;
            tunes.RemoveAll(t => t.axis == tune.axis);
            tunes.Add(tune);
        }
        Changed();
    }
}

/// <summary>What happens to fuel left above a tank's capacity when extra tanks come off (Framework 0.103.0).</summary>
public enum FuelExcess
{
    /// <summary>A mechanic took the tank off: whole units are drained into containers beside the vehicle; off a map, it is lost.</summary>
    Drain,
    /// <summary>The tank was destroyed: what it held is lost.</summary>
    Lose,
    /// <summary>A load or a reset: nothing is taken; Vehicle Framework trims the tank the next time it burns fuel.</summary>
    Keep,
}
