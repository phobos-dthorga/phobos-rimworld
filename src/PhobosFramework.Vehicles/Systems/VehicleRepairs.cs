using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Defs;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

public enum RepairKind
{
    ReplacePart,
    ConditionService,
    FieldFix,
    Fitting,
}

/// <summary>A repair waiting to be done: what it is, the work it takes and the ways of paying for it, ours first.</summary>
public sealed class RepairWork
{
    /// <summary>Stays the same across rebuilds of the list, so a mechanic's job can tell whether its work is still wanted.</summary>
    public string Key = "";
    public RepairKind Kind;
    public string Label = "";
    public float Work;
    public readonly List<List<(ThingDef def, int count)>> Options = new();
    public VehicleComponent? Part;
    public VehicleCondition? Condition;
    public FitOrder? Order;
    /// <summary>The Construction skill the work needs.</summary>
    public int MinSkill;
    /// <summary>True for a tune or a change of grade, which are worked only within reach of a tuning rig.</summary>
    public bool NeedsRig;
}

/// <summary>
/// The repairs of one player's vehicle that need more than Vehicle Framework's free repair (vehicle health, slice 3;
/// docs/development/vehicle-health-design.md): replacing destroyed parts, serious conditions that need a service, and field
/// fixes. Owned by the systems comp, which hauls their supplies and runs their work the way it does a service.
/// </summary>
public sealed class VehicleRepairs
{
    readonly CompPhobosVehicleSystems comp;
    List<RepairWork>? cached;
    int cachedVersion = -1;
    int cachedCostStamp = -1;
    Dictionary<string, string>? roleOfKey;
    bool fieldFixRequested;
    // Tracks and wheels re-fitted in the field (Framework 0.116.0, M3): they stay waiting for a proper replacement, so Vehicle Framework's
    // free repair cannot heal one past its field fit and a single component never stands in for a whole track. Saved: it is the player's work.
    HashSet<string> refitted = new();

    public VehicleRepairs(CompPhobosVehicleSystems comp)
    {
        this.comp = comp;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref fieldFixRequested, "PhobosVehicleSystems_FieldFixRequested", false);
        Scribe_Collections.Look(ref refitted, "PhobosVehicleSystems_FieldRefitted", LookMode.Value);
        refitted ??= new HashSet<string>();
    }

    /// <summary>A destroyed track or wheel a mechanic can re-fit in the field (Framework 0.116.0; owner, 17 Nov 2026: back to a quarter).</summary>
    bool RefittableGear(VehicleComponent part) =>
        part.Health <= 0f && (comp.Props.trackKeys.Contains(part.props.key) || part.props.tags?.Contains("Wheel") == true);

    /// <summary>Whether there is field work to ask for: a condition to patch or a track or wheel to re-fit.</summary>
    bool AnyFieldWork() =>
        comp.Conditions.All.Any(CanFieldFix) || (comp.Vehicle.statHandler?.components.Any(RefittableGear) ?? false);

    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;
    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    bool PlayersVehicle => comp.Usable && comp.Vehicle.Faction == Faction.OfPlayer;

    bool HasTurbine => comp.Powertrain?.Turbine != null;

    internal double Kilograms => comp.Powertrain?.KerbKg ?? Powertrain.KerbFor(comp.Vehicle.VehicleDef, null, null);

    string? RoleOf(string key)
    {
        if (roleOfKey == null)
        {
            roleOfKey = new Dictionary<string, string>();
            foreach (KeyValuePair<string, List<string>> pair in comp.Props.roleKeys)
            {
                foreach (string partKey in pair.Value)
                {
                    if (!roleOfKey.ContainsKey(partKey)) roleOfKey[partKey] = pair.Key;
                }
            }
        }
        return roleOfKey.TryGetValue(key, out string? role) ? role : null;
    }

    /// <summary>What this part does and what losing it costs, in a sentence, or null for a part with no role (hull, armour).</summary>
    public string? NoteFor(VehicleComponent part) => RoleOf(part.props.key) is { } role ? SupplyFor(part)?.NoteFor(role) : null;

    PhobosVehiclePartSupplyDef? SupplyFor(VehicleComponent part)
    {
        string? role = RoleOf(part.props.key);
        bool turbine = HasTurbine;
        PhobosVehiclePartSupplyDef? fallback = null;
        foreach (PhobosVehiclePartSupplyDef def in DefDatabase<PhobosVehiclePartSupplyDef>.AllDefsListForReading)
        {
            if (def.Fits(role, turbine)) return def;
            if (def.isDefault) fallback = def;
        }
        return fallback;
    }

    static List<(ThingDef def, int count)> OwnPayment(PhobosVehiclePartSupplyDef def, VehicleComponent part, float share, float costFactor, double kilograms)
    {
        var payment = new List<(ThingDef def, int count)>();
        // A heavier vehicle takes more of the one engine we make, as a build of it does (Framework 0.95.0).
        int items = def.OwnItemCountAt(kilograms);
        // A whole part is one item; a salvaged or heavy-duty one is costed in what it is made of.
        if (def.item != null && def.wholePart && CostShareRule.WantsWhole(share) && costFactor == 1f && !(def.kit && GameComponent_PhobosServiceKits.RawAccepted))
        {
            payment.Add((def.item, items));
            return payment;
        }
        IEnumerable<ThingDefCountClass> full = def.materials.Count > 0 ? def.materials
            : def.item?.CostList ?? Enumerable.Empty<ThingDefCountClass>();
        foreach (ThingDefCountClass material in full)
        {
            int count = CostShareRule.Count(material.count * (def.materials.Count > 0 ? def.itemCount : items), share * costFactor);
            if (count > 0) payment.Add((material.thingDef, count));
        }
        float health = VehicleFittings.UngradedHealth(part);
        int steel = CostShareRule.PerHealth(def.steelPerHealth, health, share * costFactor);
        if (steel > 0) payment.Add((ThingDefOf.Steel, steel));
        int components = CostShareRule.PerHealth(def.componentsPerHealth, health, share * costFactor);
        if (components > 0) payment.Add((ThingDefOf.ComponentIndustrial, components));
        // An item with no recorded cost still costs itself, rounded up.
        if (payment.Count == 0 && def.item != null && share > 0f) payment.Add((def.item, CostShareRule.Count(items, share * costFactor)));
        return payment;
    }

    /// <summary>The ways of paying for a destroyed part, ours first; none when the player's share makes it free.</summary>
    List<List<(ThingDef def, int count)>> ReplacementOptions(VehicleComponent part, out float work, PhobosVehiclePartGradeDef? grade = null)
    {
        grade ??= comp.Fittings.GradeForReplacement(part.props.key);
        var options = new List<List<(ThingDef def, int count)>>();
        PhobosVehiclePartSupplyDef? def = SupplyFor(part);
        work = def?.work ?? 1500f;
        if (def == null) return options;
        float share = def.wholePart ? Settings.wholePartShare : Settings.otherPartShare;
        if (share <= 0f) return options;
        double kilograms = Kilograms;
        List<(ThingDef def, int count)> own = OwnPayment(def, part, share, grade.costFactor, kilograms);
        if (own.Count == 0) return options;
        options.Add(own);
        if (!grade.isDefault) return options;    // another mod's part is a standard part
        foreach (Phobos.Framework.Bills.RoleSubstitute substitute in def.substitutes)
        {
            if (substitute.Suits(kilograms)) options.Add(new List<(ThingDef def, int count)> { (substitute.thing!, substitute.count) });
        }
        return options;
    }

    /// <summary>Whether this part has a replacement cost at all, and so can be made in another grade.</summary>
    public bool CanBeGraded(VehicleComponent part) => SupplyFor(part) != null;

    /// <summary>What a part of this grade costs, in words, for the menu.</summary>
    public string CostLine(VehicleComponent part, PhobosVehiclePartGradeDef grade)
    {
        List<List<(ThingDef def, int count)>> options = ReplacementOptions(part, out _, grade);
        return options.Count == 0 ? "PhobosVehicleSystems_GradeFree".Translate().Resolve() : options[0].Select(line => line.def.label + " x" + line.count).ToCommaList(useAnd: true);
    }

    /// <summary>
    /// Whether a part is a field re-fit still waiting for its proper replacement. Framework 0.120.5 (fault-read K6): one brought back to full
    /// health some other way (free repair while destroyed parts needed no supplies) is whole again, so its mark goes; it had stayed, and
    /// turning that setting back on listed a sound part for replacement.
    /// </summary>
    bool Refitted(VehicleComponent part)
    {
        if (!refitted.Contains(part.props.key)) return false;
        if (part.HealthPercent < 1f) return true;
        refitted.Remove(part.props.key);
        return false;
    }

    /// <summary>Whether a destroyed part waits for supplies, so Vehicle Framework's free repair must leave it alone.</summary>
    public bool PartBlocked(VehicleComponent part)
    {
        if ((part.Health > 0f && !Refitted(part)) || !Settings.destroyedPartsNeedSupplies || !PlayersVehicle) return false;
        return ReplacementOptions(part, out _).Count > 0;
    }

    public IReadOnlyList<RepairWork> Pending
    {
        get
        {
            if (cached == null || cachedVersion != comp.HealthViewVersion || cachedCostStamp != VehicleFittings.CostStamp)
            {
                cached = Build();
                cachedVersion = comp.HealthViewVersion;
                cachedCostStamp = VehicleFittings.CostStamp;
            }
            return cached;
        }
    }

    public void Invalidate() => cached = null;

    /// <summary>The work a repair takes now: fitting a new part within reach of a working tuning rig is quicker (S2, Framework 0.73.0).
    /// Read when the work starts, since the vehicle may have moved since the list was built.</summary>
    public float WorkFor(RepairWork repair) =>
        repair.Kind == RepairKind.ReplacePart && comp.Fittings.NearRig() ? repair.Work * Rules.rigReplaceWorkFactor : repair.Work;

    List<RepairWork> Build()
    {
        var pending = new List<RepairWork>();
        if (!PlayersVehicle || comp.Vehicle.statHandler == null) return pending;
        _ = comp.States;    // the conditions' stages are worked out with the systems'
        // A request outlives nothing it was for: once no condition can be patched, the next one must wait to be asked for.
        if (fieldFixRequested && !AnyFieldWork()) fieldFixRequested = false;
        if (Settings.destroyedPartsNeedSupplies)
        {
            foreach (VehicleComponent part in comp.Vehicle.statHandler.components)
            {
                // A part re-fitted in the field still waits for its proper replacement (M3).
                if (part.Health > 0f && !Refitted(part)) continue;
                List<List<(ThingDef def, int count)>> options = ReplacementOptions(part, out float work);
                if (options.Count == 0) continue;
                PhobosVehiclePartGradeDef grade = comp.Fittings.GradeForReplacement(part.props.key);
                var repair = new RepairWork
                {
                    Key = "replace/" + part.props.key, Kind = RepairKind.ReplacePart, Part = part, Work = work, MinSkill = grade.isDefault ? 0 : grade.minSkill,
                    Label = grade.isDefault
                        ? "PhobosVehicleSystems_RepairReplace".Translate(part.props.label.Named("PART")).Resolve()
                        : "PhobosVehicleSystems_RepairReplaceGraded".Translate(part.props.label.Named("PART"), grade.label.Named("GRADE")).Resolve(),
                };
                repair.Options.AddRange(options);
                pending.Add(repair);
            }
        }
        foreach (VehicleCondition condition in comp.Conditions.All)
        {
            PhobosVehicleConditionDef def = condition.def!;
            VehicleComponent? part = comp.Vehicle.statHandler.GetComponent(condition.partKey);
            if (def.service != null && def.NeedsService(condition.severity))
            {
                // NeedsRig (cross-links S3, Framework 0.124.0): machining a warped head waits for a tuning rig.
                var repair = new RepairWork { Key = "service/" + def.defName + "/" + condition.partKey, Kind = RepairKind.ConditionService, Condition = condition, Part = part, Work = def.service.work, Label = def.service.label, NeedsRig = def.service.needsRig };
                repair.Options.Add(def.service.SuppliesNow.Select(s => (s.thingDef, s.count)).ToList());
                pending.Add(repair);
            }
            if (Settings.fieldFixes && CanFieldFix(condition) && (fieldFixRequested || !comp.Vehicle.Spawned))
            {
                var repair = new RepairWork
                {
                    Key = "fix/" + def.defName + "/" + condition.partKey, Kind = RepairKind.FieldFix, Condition = condition, Part = part, Work = Rules.fieldFixWork,
                    Label = "PhobosVehicleSystems_RepairFieldFix".Translate((part?.props.label ?? condition.partKey).Named("PART")).Resolve(),
                };
                repair.Options.Add(Rules.fieldFixSupplies.Select(s => (s.thingDef, s.count)).ToList());
                pending.Add(repair);
            }
        }
        // A destroyed track or wheel re-fitted in the field, enough to drive home on (Framework 0.116.0, M3).
        if (Settings.fieldFixes && (fieldFixRequested || !comp.Vehicle.Spawned))
        {
            foreach (VehicleComponent part in comp.Vehicle.statHandler.components)
            {
                if (!RefittableGear(part)) continue;
                var repair = new RepairWork
                {
                    Key = "refit/" + part.props.key, Kind = RepairKind.FieldFix, Part = part,
                    Work = Rules.fieldFixWork * PhobosRulesDef.Current<PhobosKnockOutRulesDef>().fieldRefitWorkFactor,
                    Label = "PhobosVehicleSystems_RepairFieldRefit".Translate(part.props.label.Named("PART")).Resolve(),
                };
                repair.Options.Add(Rules.fieldFixSupplies.Select(s => (s.thingDef, s.count)).ToList());
                pending.Add(repair);
            }
        }
        foreach (FitOrder order in comp.Fittings.Orders)
        {
            VehicleFittings fittings = comp.Fittings;
            VehicleComponent? orderedPart = string.IsNullOrEmpty(order.partKey) ? null : comp.Vehicle.statHandler.GetComponent(order.partKey);
            // A grade asked for on a destroyed part is the replacement's own choice, above, and no work of its own.
            if (order.grade != null && (orderedPart == null || orderedPart.Health <= 0f)) continue;
            int skill = fittings.SkillOf(order);
            string label = fittings.LabelOf(order);
            var repair = new RepairWork
            {
                Key = "fit/" + (order.tune != null ? order.tune.axis.ToString() : order.partKey) + "/" + (order.tune?.defName ?? order.fitting?.defName ?? order.grade?.defName ?? "off"),
                Kind = RepairKind.Fitting, Order = order, Work = fittings.WorkOf(order), MinSkill = skill,
                NeedsRig = order.tune != null || order.grade != null,
                Part = orderedPart,
                Label = skill > 0 ? "PhobosVehicleSystems_FitWorkSkilled".Translate(label.Named("WORK"), skill.Named("SKILL")).Resolve() : label,
            };
            if (order.grade != null)
            {
                // A refit costs what replacing the part with one of that grade costs; the old part is used up.
                repair.Options.AddRange(ReplacementOptions(orderedPart!, out float refitWork, order.grade));
                repair.Work = refitWork;
                if (repair.Options.Count == 0) repair.Options.Add(new List<(ThingDef def, int count)>());
            }
            else
            {
                repair.Options.Add(fittings.MaterialsOf(order).Select(m => (m.thingDef, m.count)).ToList());
            }
            pending.Add(repair);
        }
        return pending;
    }

    /// <summary>A condition a colonist can patch: a kind that can be, not yet patched, and above its mildest stage.</summary>
    public static bool CanFieldFix(VehicleCondition condition) => condition.def!.fieldFixable && !condition.fieldFixed && condition.stage > 0;

    public bool AnyFieldFixable => Settings.fieldFixes && PlayersVehicle && AnyFieldWork();

    public bool FieldFixRequested => fieldFixRequested;

    public void SetFieldFixRequested(bool requested)
    {
        fieldFixRequested = requested;
        Invalidate();
    }

    bool Affordable(List<(ThingDef def, int count)> option) => option.All(line => comp.CountInCargo(line.def) >= line.count);

    List<(ThingDef def, int count)>? AffordableOption(RepairWork repair) => repair.Options.FirstOrDefault(Affordable);

    /// <summary>Whether this work could start now: paid for, and within reach of a tuning rig if it needs one.</summary>
    bool Workable(RepairWork repair) => AffordableOption(repair) != null && (!repair.NeedsRig || comp.Fittings.AtRig());

    /// <summary>The first repair that could start now, or null.</summary>
    public RepairWork? Ready => Pending.FirstOrDefault(Workable);

    /// <summary>The first repair that could start now and this mechanic has the skill for, or null.</summary>
    public RepairWork? ReadyFor(Pawn mechanic)
    {
        // A mechanoid has no skills to read: the game gives it a fixed level instead.
        int skill = mechanic.skills?.GetSkill(SkillDefOf.Construction)?.Level ?? (mechanic.RaceProps.IsMechanoid ? mechanic.RaceProps.mechFixedSkillLevel : 0);
        return Pending.FirstOrDefault(r => FittingRule.SkilledEnough(skill, r.MinSkill) && Workable(r));
    }

    /// <summary>The repair of this key, if it is still wanted and could still start.</summary>
    public RepairWork? Find(string key) => Pending.FirstOrDefault(r => r.Key == key && Workable(r));

    /// <summary>What still has to be brought: our own payment for each repair that nothing aboard can pay for.</summary>
    public IEnumerable<IEnumerable<(ThingDef thingDef, int count)>> UnmetPayments()
    {
        foreach (RepairWork repair in Pending)
        {
            if (AffordableOption(repair) == null) yield return repair.Options[0].Select(line => (line.def, line.count));
        }
    }

    /// <summary>Another mod's part that would do instead of the thing the haulers cannot find.</summary>
    public IEnumerable<(ThingDef def, int count)> AlternativesFor(ThingDef wanted)
    {
        foreach (RepairWork repair in Pending)
        {
            if (AffordableOption(repair) != null || !repair.Options[0].Any(line => line.def == wanted)) continue;
            foreach (List<(ThingDef def, int count)> option in repair.Options.Skip(1))
            {
                if (option.Count == 1) yield return option[0];
            }
        }
    }

    /// <summary>The one thing the repairs wait for, for the inspect line; null with none pending.</summary>
    public string? WaitingLine()
    {
        if (comp.RepairsPaused) return "PhobosVehicleSystems_RepairPausedLine".Translate().Resolve();
        // A patch nobody asked for is on offer in a caravan, but the vehicle is not waiting for it.
        List<RepairWork> waiting = Pending.Where(r => r.Kind != RepairKind.FieldFix || fieldFixRequested).ToList();
        if (waiting.Count == 0) return null;
        RepairWork? ready = waiting.FirstOrDefault(Workable);
        if (ready != null) return "PhobosVehicleSystems_RepairWaitingMechanic".Translate(ready.Label.CapitalizeFirst().Named("WORK")).Resolve();
        return NeedsLine(waiting[0]);
    }

    public string NeedsLine(RepairWork repair)
    {
        if (repair.NeedsRig && !comp.Fittings.AtRig()) return "PhobosVehicleSystems_RepairNeedsRig".Translate(repair.Label.CapitalizeFirst().Named("WORK"), VehicleFittings.RigReach.Named("CELLS")).Resolve();
        string own = repair.Options[0].Select(line => line.def.label + " x" + line.count).ToCommaList(useAnd: true);
        if (repair.Options.Count == 1) return "PhobosVehicleSystems_RepairNeeds".Translate(repair.Label.CapitalizeFirst().Named("WORK"), own.Named("SUPPLIES")).Resolve();
        string others = repair.Options.Skip(1).Select(o => o[0].def.label).ToCommaList(useAnd: false);
        return "PhobosVehicleSystems_RepairNeedsOr".Translate(repair.Label.CapitalizeFirst().Named("WORK"), own.Named("SUPPLIES"), others.Named("OTHERS")).Resolve();
    }

    /// <summary>Does the first ready repair, using its supplies from the cargo, and says so. False when none is ready.</summary>
    public bool CompleteReady(Pawn mechanic)
    {
        comp.Fittings.Sweep();
        RepairWork? repair = ReadyFor(mechanic);
        return repair != null && Do(repair, mechanic);
    }

    /// <summary>Finishes the repair a job set out to do. False when it is no longer wanted: cancelled, or its part has gone.</summary>
    public bool Complete(string key, Pawn mechanic)
    {
        comp.Fittings.Sweep();
        RepairWork? repair = Find(key);
        return repair != null && Do(repair, mechanic);
    }

    bool Do(RepairWork repair, Pawn mechanic)
    {
        List<(ThingDef def, int count)>? payment = AffordableOption(repair);
        if (VehicleSystemsMod.RepairsLog.DebugEnabled)
        {
            VehicleSystemsMod.RepairsLog.Debug(comp.Vehicle.LabelShort + " (" + comp.Vehicle.ThingID + ") " + repair.Kind + " '" + repair.Label + "' by " + mechanic.LabelShort + ": "
                + (payment == null ? "supplies not aboard, not done" : "done, using " + (payment.Count == 0 ? "nothing" : string.Join(", ", payment.ConvertAll(p => p.count + " " + p.def.defName)))));
        }
        if (payment == null) return false;
        foreach ((ThingDef def, int count) in payment) comp.TakeFromCargo(def, count);
        NamedArgument who = mechanic.LabelShort.Named("MECHANIC");
        NamedArgument vehicle = comp.Vehicle.LabelShort.Named("VEHICLE");
        string text;
        switch (repair.Kind)
        {
            case RepairKind.ReplacePart:
                comp.Fittings.FinishReplacement(repair.Part!);
                comp.Conditions.RemoveOn(repair.Part!.props.key);    // a new part has none of the old one's troubles
                refitted.Remove(repair.Part!.props.key);             // nor the field fit's
                // At a tuning rig, its hoist and instruments fit the part properly (S2, Framework 0.73.0); in the field it is a rough fit.
                comp.RestorePart(repair.Part!, comp.Fittings.NearRig() ? Rules.replacedPartHealthAtRig : Rules.replacedPartHealth);
                // Framework 0.178.0 (row 252): which part a destroyed one was replaced with, by the first thing paid (a reduction gearbox, a flight computer).
                if (payment.Count > 0) Seen.Note("vehicles.part-replaced", comp.Vehicle, repair.Part!.props.label, payment[0].def.defName);
                NotifyRepairable();
                text = "PhobosVehicleSystems_RepairReplaced".Translate(who, vehicle, repair.Part!.props.label.Named("PART")).Resolve();
                break;
            case RepairKind.ConditionService:
                comp.Conditions.Remove(repair.Condition!);
                text = "PhobosVehicleSystems_Serviced".Translate(who, vehicle, repair.Label.Named("SERVICE")).Resolve();
                if (repair.NeedsRig) Seen.Note("vehicles.rig-service-done", comp.Vehicle, repair.Label);
                break;
            case RepairKind.Fitting:
                text = comp.Fittings.Complete(repair.Order!, mechanic);
                break;
            default:
                if (repair.Condition == null && repair.Part != null)
                {
                    // A track or wheel re-fitted in the field (M3): back to the owner's quarter, still waiting for a proper replacement.
                    comp.RestorePart(repair.Part, PhobosRulesDef.Current<PhobosKnockOutRulesDef>().fieldRefitHealth);
                    refitted.Add(repair.Part.props.key);
                    Seen.Note("vehicles.field-refit", comp.Vehicle, repair.Part.props.label);
                    NotifyRepairable();
                    text = "PhobosVehicleSystems_RepairFieldRefitted".Translate(who, vehicle, repair.Part.props.label.Named("PART")).Resolve();
                    break;
                }
                comp.Conditions.FieldFix(repair.Condition!);
                text = "PhobosVehicleSystems_RepairFieldFixed".Translate(who, vehicle, (repair.Part?.props.label ?? "").Named("PART")).Resolve();
                break;
        }
        comp.AfterRepair();
        // Framework 0.181.0 (EM23, EM13): the logbook, the mechanic's record, and a tale for one done at a convoy's halt.
        VehicleStory.Repaired(comp, mechanic, repair.Label, service: repair.Kind == RepairKind.ConditionService);
        if (!comp.Vehicle.Spawned && comp.Vehicle.GetCaravan() != null) Seen.Note("vehicles.convoy-repair", comp.Vehicle, repair.Label);
        Invalidate();
        if (fieldFixRequested && !AnyFieldWork()) fieldFixRequested = false;
        VehicleNotices.Send(text, comp.Vehicle.Spawned ? comp.Vehicle : LookTargets.Invalid, MessageTypeDefOf.TaskCompletion);
        return true;
    }

    /// <summary>Vehicle Framework keeps a list of vehicles to repair: a replaced part can be mended again, so the vehicle rejoins it.</summary>
    internal void NotifyRepairable()
    {
        if (comp.Vehicle.Spawned) MapComponentCache<ListerVehiclesRepairable>.GetComponent(comp.Vehicle.Map).NotifyVehicleTookDamage(comp.Vehicle);
    }

    /// <summary>How soon the condition's leak reaches the next bad level, as the Health tab says it; null when it has no such clock.</summary>
    public string? DangerClock(VehicleCondition condition)
    {
        StageSpec? stage = condition.CurrentStage;
        if (stage == null) return null;
        foreach (VehicleSystemEffect effect in stage.effects)
        {
            if (effect is not Effect_RateQuantity { perHour: < 0f } drain) continue;
            foreach (SystemState state in comp.States)
            {
                int q = state.Def.QuantityIndex(drain.quantity);
                if (q < 0 || state.Def.quantities[q].unit != QuantityUnit.Fraction) continue;
                string? text = ClockFor(state, q, drain.perHour);
                if (text != null) return text;
            }
        }
        return null;
    }

    string? ClockFor(SystemState state, int q, float declaredPerHour)
    {
        QuantitySpec quantity = state.Def.quantities[q];
        double value = state.values[q];
        double target = quantity.min;
        string where = "PhobosVehicleSystems_HealthClockEmpty".Translate();
        foreach (StageSpec stage in quantity.stages)
        {
            if (!stage.notable || float.IsNaN(stage.to) || stage.to >= value || stage.to <= target) continue;
            target = stage.to;
            where = stage.label;
        }
        double hours = DangerClockRule.Hours(value, target, MeasuredRate(state, q) ?? declaredPerHour);
        if (hours > Rules.dangerClockHours) return null;
        string time = hours < 1 ? "PhobosVehicleSystems_HealthClockUnderHour".Translate().Resolve()
            : "PhobosVehicleSystems_HealthClockHours".Translate(hours.ToString("0").Named("HOURS")).Resolve();
        return "PhobosVehicleSystems_HealthClock".Translate(quantity.label.CapitalizeFirst().Named("QUANTITY"), where.Named("STAGE"), time.Named("TIME")).Resolve();
    }

    /// <summary>The quantity's real rate over the last game hour, alternators and all, once a full hour of its history exists.</summary>
    double? MeasuredRate(SystemState state, int q)
    {
        HistoryRing? ring = comp.History.For(state, q);
        int samples = ElapsedRule.TicksPerHour / QuantityHistory.StrideTicks;
        if (ring == null || ring.Count <= samples) return null;
        return DangerClockRule.Rate(ring[ring.Count - 1 - samples], ring.Newest, 1);
    }
}
