using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A fitting on one part, by the part's key: Vehicle Framework saves parts by position, and appended parts move.</summary>
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

/// <summary>Work the player has asked for: fit or take off a fitting, or set a tune.</summary>
public sealed class FitOrder : IExposable
{
    public string partKey = "";
    public PhobosVehicleFittingDef? fitting;
    public PhobosVehicleTuneDef? tune;
    public bool remove;

    public bool Valid => remove ? !partKey.NullOrEmpty() : fitting != null || tune != null;

    public void ExposeData()
    {
        Scribe_Values.Look(ref partKey, "part", "");
        Scribe_Defs.Look(ref fitting, "fitting");
        Scribe_Defs.Look(ref tune, "tune");
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
    bool setRolled;

    Dictionary<VehicleStatDef, float>? statFactors;
    Dictionary<string, float>? parameterFactors;
    float power = 1f;
    float fuel = 1f;
    float finalDrive = 1f;

    public VehicleFittings(CompPhobosVehicleSystems comp)
    {
        this.comp = comp;
    }

    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    public IReadOnlyList<FitOrder> Orders => orders ??= new List<FitOrder>();

    bool Nothing => (fitted == null || fitted.Count == 0) && (tunes == null || tunes.Count == 0);

    public void ExposeData()
    {
        Scribe_Collections.Look(ref fitted, "PhobosVehicleSystems_Fitted", LookMode.Deep);
        Scribe_Collections.Look(ref tunes, "PhobosVehicleSystems_Tunes", LookMode.Def);
        Scribe_Collections.Look(ref orders, "PhobosVehicleSystems_FitOrders", LookMode.Deep);
        Scribe_Values.Look(ref setRolled, "PhobosVehicleSystems_FitSetRolled", false);
        // A save from before fittings existed has no entry for them, and the loader then leaves the lists null, not empty. The
        // stats are read before loading ends (Vehicle Framework spawns the vehicle first), so they are made whole in every mode,
        // never only at the end (0.40.0 left every vehicle unspawnable on an older save; 0.40.2).
        fitted ??= new List<FittedPart>();
        tunes ??= new List<PhobosVehicleTuneDef>();
        orders ??= new List<FitOrder>();
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        // A Def another mod took away: the fitting goes with it, and the rest of the vehicle is untouched.
        fitted.RemoveAll(f => f == null || f.def == null || f.partKey.NullOrEmpty());
        tunes.RemoveAll(t => t == null);
        orders.RemoveAll(o => o == null || !o.Valid);
        statFactors = null;
        parameterFactors = null;
    }

    public void Clear()
    {
        fitted.Clear();
        tunes.Clear();
        orders.Clear();
        statFactors = null;
        parameterFactors = null;
    }

    // ---- The figures ----

    void Recache()
    {
        if (statFactors != null) return;
        var stats = new Dictionary<VehicleStatDef, float>();
        var parameters = new Dictionary<string, float>();
        double p = 1, f = 1, drive = 1;
        foreach (FittedPart part in fitted)
        {
            PhobosVehicleFittingDef def = part.def!;
            int parts = PartsOf(def);
            foreach (StatFactorSpec spec in def.statFactors)
            {
                if (spec.stat != null) stats[spec.stat] = (stats.TryGetValue(spec.stat, out float before) ? before : 1f) * (float)FittingRule.PartShare(spec.factor, parts);
            }
            foreach (ParameterFactorSpec spec in def.parameterFactors) Multiply(parameters, spec.parameter, FittingRule.PartShare(spec.factor, parts));
            p *= FittingRule.PartShare(def.powerFactor, parts);
            f *= FittingRule.PartShare(def.fuelFactor, parts);
        }
        foreach (PhobosVehicleTuneDef tune in tunes)
        {
            foreach (ParameterFactorSpec spec in tune.parameterFactors) Multiply(parameters, spec.parameter, spec.factor);
            p *= tune.powerFactor;
            f *= tune.fuelFactor;
            drive *= tune.finalDriveFactor;
        }
        statFactors = stats;
        parameterFactors = parameters;
        power = (float)p;
        fuel = (float)f;
        finalDrive = (float)drive;
    }

    static void Multiply(Dictionary<string, float> parameters, string name, double factor) =>
        parameters[name] = (parameters.TryGetValue(name, out float before) ? before : 1f) * (float)factor;

    public float StatFactor(VehicleStatDef stat)
    {
        if (Nothing) return 1f;
        Recache();
        return statFactors!.TryGetValue(stat, out float factor) ? factor : 1f;
    }

    public float ParameterFactor(string name)
    {
        if (Nothing) return 1f;
        Recache();
        return parameterFactors!.TryGetValue(name, out float factor) ? factor : 1f;
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

    // ---- What can be done ----

    bool HasPiston => comp.Powertrain?.Spec.Engine == EngineKind.Piston;

    /// <summary>A hint by name, because nothing else marks a tracked vehicle (the loadout selector reads it the same way).</summary>
    bool IsTracked => comp.Vehicle.VehicleDef.components.Any(part => part.key != null && part.key.IndexOf("track", System.StringComparison.OrdinalIgnoreCase) >= 0);

    /// <summary>The parts that can take a fitting, each once.</summary>
    public IEnumerable<VehicleComponent> FittableParts()
    {
        var seen = new HashSet<string>();
        foreach (KeyValuePair<string, List<string>> pair in comp.Props.roleKeys)
        {
            foreach (string key in pair.Value)
            {
                if (!seen.Add(key)) continue;
                VehicleComponent? part = comp.Vehicle.statHandler?.GetComponent(key);
                if (part != null && OptionsFor(part).Any()) yield return part;
            }
        }
    }

    /// <summary>The fittings that suit this part on this vehicle, whether or not they can be done yet.</summary>
    public IEnumerable<PhobosVehicleFittingDef> OptionsFor(VehicleComponent part)
    {
        double kilograms = comp.Repairs.Kilograms;
        bool piston = HasPiston;
        bool tracked = IsTracked;
        foreach (PhobosVehicleFittingDef def in DefDatabase<PhobosVehicleFittingDef>.AllDefsListForReading)
        {
            if (def.roles.Any(role => comp.PartPlays(part, role)) && def.Suits(kilograms, piston, tracked)) yield return def;
        }
    }

    /// <summary>Why this fitting cannot be ordered now; null when it can.</summary>
    public string? Refusal(PhobosVehicleFittingDef def, VehicleComponent part)
    {
        if (def.researchPrerequisite != null && !def.researchPrerequisite.IsFinished)
        {
            return "PhobosVehicleSystems_FitNoResearch".Translate(def.researchPrerequisite.LabelCap.Named("RESEARCH")).Resolve();
        }
        return part.Health <= 0f ? "PhobosVehicleSystems_FitPartDestroyed".Translate().Resolve() : null;
    }

    public PhobosVehicleFittingDef? FittedOn(string partKey) => fitted.FirstOrDefault(f => f.partKey == partKey)?.def;

    public FitOrder? OrderOn(string partKey) => orders.FirstOrDefault(o => o.partKey == partKey);

    public FitOrder? OrderFor(VehicleTuneAxis axis) => orders.FirstOrDefault(o => o.tune != null && o.tune.axis == axis);

    /// <summary>Whether this axis can be tuned here: a piston engine for the engine, and a gearbox for the gearing, while the model runs.</summary>
    public bool CanTune(VehicleTuneAxis axis)
    {
        Powertrain? train = comp.Powertrain;
        if (train == null || train.Spec.Engine != EngineKind.Piston || Settings.drivetrainMode == DrivetrainMode.Off) return false;
        return axis == VehicleTuneAxis.Engine || train.Spec.Transmission != TransmissionKind.None;
    }

    public static IEnumerable<PhobosVehicleTuneDef> TunesOf(VehicleTuneAxis axis) =>
        DefDatabase<PhobosVehicleTuneDef>.AllDefsListForReading.Where(t => t.axis == axis).OrderBy(t => t.order);

    public PhobosVehicleTuneDef? Current(VehicleTuneAxis axis) => tunes.FirstOrDefault(t => t.axis == axis) ?? TunesOf(axis).FirstOrDefault(t => t.isDefault);

    // ---- Orders ----

    public void OrderFitting(VehicleComponent part, PhobosVehicleFittingDef def)
    {
        orders.RemoveAll(o => o.partKey == part.props.key);
        Add(new FitOrder { partKey = part.props.key, fitting = def });
    }

    public void OrderRemoval(VehicleComponent part)
    {
        orders.RemoveAll(o => o.partKey == part.props.key);
        Add(new FitOrder { partKey = part.props.key, remove = true });
    }

    public void OrderTune(PhobosVehicleTuneDef tune)
    {
        orders.RemoveAll(o => o.tune != null && o.tune.axis == tune.axis);
        Add(new FitOrder { tune = tune });
    }

    void Add(FitOrder order)
    {
        orders.Add(order);
        comp.Repairs.Invalidate();
        Messages.Message("PhobosVehicleSystems_FitOrderedMessage".Translate(LabelOf(order).CapitalizeFirst().Named("WORK"), comp.Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput, historical: false);
    }

    public void Cancel(FitOrder order)
    {
        if (!orders.Remove(order)) return;
        comp.Repairs.Invalidate();
        Messages.Message("PhobosVehicleSystems_FitCancelledMessage".Translate(LabelOf(order).CapitalizeFirst().Named("WORK"), comp.Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput, historical: false);
    }

    string PartLabel(string partKey) => comp.Vehicle.statHandler?.GetComponent(partKey)?.props.label ?? partKey;

    public string LabelOf(FitOrder order)
    {
        if (order.tune != null)
        {
            return "PhobosVehicleSystems_TuneWork".Translate(order.tune.label.Named("SETTING")).Resolve();
        }
        NamedArgument part = PartLabel(order.partKey).Named("PART");
        if (order.remove) return "PhobosVehicleSystems_FitRemoveWork".Translate((FittedOn(order.partKey)?.label ?? "").Named("FITTING"), part).Resolve();
        return "PhobosVehicleSystems_FitWork".Translate(order.fitting!.label.Named("FITTING"), part).Resolve();
    }

    public float WorkOf(FitOrder order) => order.tune?.work ?? (order.remove ? (FittedOn(order.partKey)?.work ?? 600f) * RemovalWorkShare : order.fitting!.work);

    /// <summary>Taking a fitting off is half the work of putting it on (authored).</summary>
    const float RemovalWorkShare = 0.5f;

    public int SkillOf(FitOrder order) => order.tune?.minSkill ?? order.fitting?.minSkill ?? 0;

    public IEnumerable<ThingDefCountClass> MaterialsOf(FitOrder order) =>
        order.tune?.materials ?? order.fitting?.materials ?? Enumerable.Empty<ThingDefCountClass>();

    /// <summary>Does the order, says what was done, and returns the sentence. Materials are the repair's to take.</summary>
    public string Complete(FitOrder order, Pawn mechanic)
    {
        NamedArgument who = mechanic.LabelShort.Named("MECHANIC");
        NamedArgument vehicle = comp.Vehicle.LabelShort.Named("VEHICLE");
        string text;
        if (order.tune != null)
        {
            tunes.RemoveAll(t => t.axis == order.tune.axis);
            if (!order.tune.isDefault) tunes.Add(order.tune);
            text = "PhobosVehicleSystems_TuneDone".Translate(who, vehicle, order.tune.label.Named("SETTING")).Resolve();
        }
        else if (order.remove)
        {
            string label = FittedOn(order.partKey)?.label ?? "";
            fitted.RemoveAll(f => f.partKey == order.partKey);
            text = "PhobosVehicleSystems_FitRemovedDone".Translate(who, label.Named("FITTING"), vehicle, PartLabel(order.partKey).Named("PART")).Resolve();
        }
        else
        {
            fitted.RemoveAll(f => f.partKey == order.partKey);
            fitted.Add(new FittedPart { partKey = order.partKey, def = order.fitting });
            text = "PhobosVehicleSystems_FitDone".Translate(who, order.fitting!.label.Named("FITTING"), vehicle, PartLabel(order.partKey).Named("PART")).Resolve();
        }
        orders.Remove(order);
        Changed();
        return text;
    }

    void Changed()
    {
        statFactors = null;
        parameterFactors = null;
        comp.Notify_FittingsChanged();
    }

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
                Messages.Message("PhobosVehicleSystems_FitLost".Translate(comp.Vehicle.LabelShort.Named("VEHICLE"), fitted[i].def!.label.Named("FITTING"), part.props.label.Named("PART")), comp.Vehicle, MessageTypeDefOf.NegativeEvent);
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
            if (player)
            {
                Messages.Message("PhobosVehicleSystems_FitOrderLost".Translate(LabelOf(order).CapitalizeFirst().Named("WORK"), comp.Vehicle.LabelShort.Named("VEHICLE")), comp.Vehicle, MessageTypeDefOf.NegativeEvent);
            }
            orders.RemoveAt(i);
            orderLost = true;
        }
        if (changed) Changed();
        else if (orderLost) comp.Repairs.Invalidate();
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
        if (!DefDatabase<PhobosVehicleFitSetDef>.AllDefsListForReading.Where(s => s.Suits(tech)).TryRandomElementByWeight(s => s.weight, out PhobosVehicleFitSetDef set)) return;
        Apply(set);
    }

    /// <summary>Puts a set on the vehicle: each fitting on every free part it suits (one axle of four would be a sliver of the effect), and each tune the vehicle can take.</summary>
    public void Apply(PhobosVehicleFitSetDef set)
    {
        double kilograms = comp.Repairs.Kilograms;
        bool piston = HasPiston;
        bool tracked = IsTracked;
        foreach (PhobosVehicleFittingDef def in set.fittings)
        {
            if (!def.Suits(kilograms, piston, tracked)) continue;
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
