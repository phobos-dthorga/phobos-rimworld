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

    Dictionary<VehicleStatDef, float>? statFactors;
    Dictionary<(string system, string name), float>? parameterFactors;
    float power = 1f;
    float fuel = 1f;
    float flatFuel = 1f;
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
        Scribe_Collections.Look(ref graded, "PhobosVehicleSystems_Grades", LookMode.Deep);
        Scribe_Values.Look(ref setRolled, "PhobosVehicleSystems_FitSetRolled", false);
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
    }

    public void Clear()
    {
        foreach (GradedPart part in graded) comp.Vehicle.statHandler?.GetComponent(part.partKey)?.AddHealthModifiers.Remove(GradeHealthKey);
        graded.Clear();
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
        var parameters = new Dictionary<(string system, string name), float>();
        double p = 1, f = 1, drive = 1, flat = 1;
        foreach (FittedPart part in fitted)
        {
            PhobosVehicleFittingDef def = part.def!;
            int parts = PartsOf(def);
            foreach (StatFactorSpec spec in def.statFactors)
            {
                if (spec.stat != null) stats[spec.stat] = (stats.TryGetValue(spec.stat, out float before) ? before : 1f) * (float)FittingRule.PartShare(spec.factor, parts);
            }
            foreach (ParameterFactorSpec spec in def.parameterFactors) Multiply(parameters, spec, FittingRule.PartShare(spec.factor, parts));
            p *= FittingRule.PartShare(def.powerFactor, parts);
            f *= FittingRule.PartShare(def.fuelFactor, parts);
        }
        foreach (PhobosVehicleTuneDef tune in tunes)
        {
            foreach (ParameterFactorSpec spec in tune.parameterFactors) Multiply(parameters, spec, spec.factor);
            p *= tune.powerFactor;
            f *= tune.fuelFactor;
            flat *= tune.flatFuelFactor;
            drive *= tune.finalDriveFactor;
        }
        statFactors = stats;
        parameterFactors = parameters;
        power = (float)p;
        fuel = (float)f;
        flatFuel = (float)flat;
        finalDrive = (float)drive;
    }

    static void Multiply(Dictionary<(string system, string name), float> parameters, ParameterFactorSpec spec, double factor)
    {
        var key = (spec.system, spec.parameter);
        parameters[key] = (parameters.TryGetValue(key, out float before) ? before : 1f) * (float)factor;
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

    // ---- What can be done ----

    bool HasPiston => comp.Powertrain?.Spec.Engine == EngineKind.Piston;

    bool? tracked;

    /// <summary>A hint by name, because nothing else marks a tracked vehicle (the loadout selector reads it the same way).</summary>
    bool IsTracked => tracked ??= comp.Vehicle.VehicleDef.components.Any(part => part.key != null && part.key.IndexOf("track", System.StringComparison.OrdinalIgnoreCase) >= 0);

    /// <summary>Whether more engine power reaches the road: a piston engine driving the ground through the full model. A propeller's speed
    /// and an aircraft's flight phase do not follow it, so more power there would only cost heat and fuel.</summary>
    bool PowerCounts => Settings.drivetrainMode == DrivetrainMode.Full && comp.Powertrain is { } train && train.Spec.Engine == EngineKind.Piston
        && train.Spec.Transmission is TransmissionKind.Automatic or TransmissionKind.Direct or TransmissionKind.DieselElectric;

    /// <summary>Whether this fitting would change anything on this vehicle: a loadout can drop a system (a turbine tank has no coolant) and keep its part.</summary>
    bool MovesSomethingHere(PhobosVehicleFittingDef def)
    {
        if (def.statFactors.Count > 0 || def.powerFactor != 1f || def.fuelFactor != 1f) return true;
        return def.parameterFactors.Any(spec => comp.Props.systems.Any(system =>
            (spec.system.Length == 0 || system.def.defName == spec.system) && system.parameters.ContainsKey(spec.parameter)));
    }

    bool Suits(PhobosVehicleFittingDef def) => def.Suits(comp.Repairs.Kilograms, HasPiston, IsTracked, PowerCounts) && MovesSomethingHere(def);

    /// <summary>The parts that can take a fitting or be given a grade.</summary>
    public IEnumerable<VehicleComponent> ListedParts()
    {
        if (comp.Vehicle.statHandler == null) yield break;
        foreach (VehicleComponent part in comp.Vehicle.statHandler.components)
        {
            if (comp.Repairs.CanBeGraded(part) || OptionsFor(part).Any()) yield return part;
        }
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
        if (part.Health <= 0f) return "PhobosVehicleSystems_FitPartDestroyed".Translate().Resolve();
        PhobosVehiclePartGradeDef grade = GradeOf(part.props.key);
        return grade.holdsFitting ? null : "PhobosVehicleSystems_FitGradeHoldsNone".Translate(grade.label.Named("GRADE")).Resolve();
    }

    public PhobosVehicleFittingDef? FittedOn(string partKey) => fitted.FirstOrDefault(f => f.partKey == partKey)?.def;

    public FitOrder? OrderOn(string partKey) => orders.FirstOrDefault(o => o.partKey == partKey);

    public FitOrder? OrderFor(VehicleTuneAxis axis) => orders.FirstOrDefault(o => o.tune != null && o.tune.axis == axis);

    /// <summary>Lets the self-test work a tune on its throwaway vehicle, which stands on no map and so is near no rig.</summary>
    internal static bool AssumeRigForTest;

    static List<ThingDef>? rigDefs;

    static List<ThingDef> RigDefs => rigDefs ??= DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.GetCompProperties<CompProperties_PhobosTuningRig>() != null).ToList();

    /// <summary>The furthest any tuning rig reaches, for what the player reads.</summary>
    public static int RigReach => RigDefs.Count == 0 ? 0 : RigDefs.Max(d => d.GetCompProperties<CompProperties_PhobosTuningRig>().reach);

    /// <summary>Whether a tune or a change of grade could be worked here and now: within reach of a working rig of the player's, or the setting is off.</summary>
    public bool AtRig()
    {
        if (!Settings.tuningNeedsRig || AssumeRigForTest) return true;
        VehiclePawn vehicle = comp.Vehicle;
        if (!vehicle.Spawned) return false;
        CellRect area = vehicle.OccupiedRect();
        foreach (ThingDef def in RigDefs)
        {
            foreach (Building rig in vehicle.Map.listerBuildings.AllBuildingsColonistOfDef(def))
            {
                CompPhobosTuningRig? station = rig.GetComp<CompPhobosTuningRig>();
                if (station != null && station.Working && station.Reaches(area)) return true;
            }
        }
        return false;
    }

    /// <summary>Whether this vehicle's window should say that tunes and grades will wait.</summary>
    public bool RigMissing => Settings.tuningNeedsRig && !AtRig();

    /// <summary>Whether this axis can be tuned here: an engine whose power reaches the road, and gears the model actually reads.</summary>
    public bool CanTune(VehicleTuneAxis axis)
    {
        if (!PowerCounts) return false;
        return axis == VehicleTuneAxis.Engine || comp.Powertrain!.Spec.Transmission is TransmissionKind.Automatic or TransmissionKind.Direct;
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

    /// <summary>Asks for a part of this grade: a refit of a working part, or what a destroyed part is replaced with.</summary>
    public void OrderGrade(VehicleComponent part, PhobosVehiclePartGradeDef grade)
    {
        orders.RemoveAll(o => o.partKey == part.props.key);
        Add(new FitOrder { partKey = part.props.key, grade = grade });
    }

    void Add(FitOrder order)
    {
        orders.Add(order);
        comp.Repairs.Invalidate();
        VehicleNotices.Send("PhobosVehicleSystems_FitOrderedMessage".Translate(LabelOf(order).CapitalizeFirst().Named("WORK"), comp.Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput, historical: false);
    }

    public void Cancel(FitOrder order)
    {
        if (!orders.Remove(order)) return;
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

    public float WorkOf(FitOrder order) => order.tune?.work ?? (order.remove ? (FittedOn(order.partKey)?.work ?? 600f) * RemovalWorkShare : order.fitting?.work ?? 1500f);

    /// <summary>Taking a fitting off is half the work of putting it on (authored).</summary>
    const float RemovalWorkShare = 0.5f;

    public int SkillOf(FitOrder order) => order.tune?.minSkill ?? order.fitting?.minSkill ?? order.grade?.minSkill ?? 0;

    public IEnumerable<ThingDefCountClass> MaterialsOf(FitOrder order) =>
        order.tune?.materials ?? order.fitting?.materials ?? Enumerable.Empty<ThingDefCountClass>();

    /// <summary>Does the order, says what was done, and returns the sentence. Materials are the repair's to take.</summary>
    public string Complete(FitOrder order, Pawn mechanic)
    {
        NamedArgument who = mechanic.LabelShort.Named("MECHANIC");
        NamedArgument vehicle = comp.Vehicle.LabelShort.Named("VEHICLE");
        string text;
        if (order.grade != null)
        {
            VehicleComponent part = comp.Vehicle.statHandler.GetComponent(order.partKey);
            SetGrade(part, order.grade);
            if (!order.grade.holdsFitting) fitted.RemoveAll(f => f.partKey == order.partKey);
            comp.Conditions.RemoveOn(order.partKey);    // a new part has none of the old one's troubles
            comp.RestorePart(part, 1f);
            text = "PhobosVehicleSystems_GradeDone".Translate(who, vehicle, part.props.label.Named("PART"), order.grade.label.Named("GRADE")).Resolve();
        }
        else if (order.tune != null)
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
        if (changed) Changed();
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
        Changed();
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
