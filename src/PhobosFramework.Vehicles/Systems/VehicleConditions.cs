using System.Collections.Generic;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// One condition on one part of one vehicle: its Def, the part's key (never its index: Vehicle Framework saves parts by position, and
/// appended parts move), the role the part plays, the system whose figures its effects read, and the severity. Saved; the stage is
/// worked out again on load.
/// </summary>
public sealed class VehicleCondition : IExposable
{
    public PhobosVehicleConditionDef? def;
    public string partKey = "";
    public string role = "";
    public string system = "";
    public float severity;
    [Unsaved] public int stage = -1;

    public StageSpec? CurrentStage => def != null && stage >= 0 && stage < def.stages.Count ? def.stages[stage] : null;

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref partKey, "part", "");
        Scribe_Values.Look(ref role, "role", "");
        Scribe_Values.Look(ref system, "system", "");
        Scribe_Values.Look(ref severity, "severity");
    }
}

/// <summary>
/// The conditions of one vehicle's parts (docs/development/vehicle-health-design.md, slice 2), owned by its systems comp: given by
/// hits and causes, advanced on the comp's update (fading, stages and their effects), cleared by a repair to full health, by fading
/// out or by a service. Their stage effects join the systems' own in the stat factors, refusals and tabs, through the comp.
/// </summary>
public sealed class VehicleConditions
{
    readonly CompPhobosVehicleSystems comp;
    List<VehicleCondition> list = new();

    public VehicleConditions(CompPhobosVehicleSystems comp)
    {
        this.comp = comp;
    }

    public IReadOnlyList<VehicleCondition> All => list;
    public int Count => list.Count;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref list, "PhobosVehicleSystems_Conditions", LookMode.Deep);
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        list ??= new List<VehicleCondition>();
        int dropped = list.RemoveAll(c => c == null || c.def == null || string.IsNullOrEmpty(c.partKey));
        if (dropped > 0) Log.Message(VehicleSystemsMod.LogPrefix + " Dropped " + dropped + " saved vehicle condition(s) whose kind no longer exists.");
        foreach (VehicleCondition condition in list) condition.stage = -1;
    }

    /// <summary>The conditions on one part, for the Health tab.</summary>
    public IEnumerable<VehicleCondition> On(string partKey)
    {
        foreach (VehicleCondition condition in list)
        {
            if (condition.partKey == partKey) yield return condition;
        }
    }

    /// <summary>Whether the part has the condition.</summary>
    public bool Holds(PhobosVehicleConditionDef def, string partKey) => Find(def, partKey) != null;

    /// <summary>After a load: each condition's stage from its severity, without running the stage's effects again (as the systems'
    /// quantities do), so a loaded game sends no messages for stages that had already begun.</summary>
    public void RestoreStages()
    {
        foreach (VehicleCondition condition in list)
        {
            StageBounds bounds = StageBounds.Of(condition.def!.Severity);
            condition.stage = StageRule.StageIndex(condition.severity, bounds.from, bounds.to);
        }
    }

    /// <summary>Adds severity to the condition on the part, giving the part the condition if it has none. Stages follow on the next update.</summary>
    public void Add(PhobosVehicleConditionDef def, VehicleComponent part, string role, string system, float amount)
    {
        if (amount <= 0f || part?.props?.key == null) return;
        VehicleCondition? condition = Find(def, part.props.key);
        if (condition == null)
        {
            condition = new VehicleCondition { def = def, partKey = part.props.key, role = role, system = system };
            list.Add(condition);
        }
        condition.severity = (float)SeverityRule.Next(condition.severity, amount, 0, 0);
    }

    /// <summary>Sets the condition on the part to a severity, giving or removing it (the dev action).</summary>
    public void Set(PhobosVehicleConditionDef def, VehicleComponent part, string role, string system, float severity)
    {
        VehicleCondition? condition = Find(def, part.props.key);
        if (severity <= 0f)
        {
            if (condition != null) RemoveAt(list.IndexOf(condition));
            return;
        }
        if (condition == null) Add(def, part, role, system, severity);
        else condition.severity = severity > 1f ? 1f : severity;
    }

    /// <summary>A part's health rose (Vehicle Framework's repair, a caravan's repair): conditions that clear on repair go once it is full.</summary>
    public void OnHealthRose(VehicleComponent part, float before, float after)
    {
        string key = part.props.key;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            VehicleCondition condition = list[i];
            if (condition.partKey == key && SeverityRule.ClearedByRepair(condition.def!.clearsOnRepair, before, after, part.MaxHealth)) RemoveAt(i);
        }
    }

    /// <summary>Removes every condition the service clears. Returns how many went.</summary>
    public int ClearBy(ServiceSpec service)
    {
        if (service.clearsConditions.Count == 0) return 0;
        int removed = 0;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (!service.clearsConditions.Contains(list[i].def!)) continue;
            RemoveAt(i);
            removed++;
        }
        return removed;
    }

    public bool AnyClearedBy(ServiceSpec service)
    {
        if (service.clearsConditions.Count == 0) return false;
        foreach (VehicleCondition condition in list)
        {
            if (service.clearsConditions.Contains(condition.def!)) return true;
        }
        return false;
    }

    /// <summary>
    /// The hours pass: a condition whose part is gone is dropped, the rest fade, and each enters, leaves and applies its stages as a
    /// system's quantity does.
    /// </summary>
    public void Advance(float hours)
    {
        VehiclePawn vehicle = comp.Vehicle;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            VehicleCondition condition = list[i];
            if (vehicle.statHandler?.GetComponent(condition.partKey) == null)
            {
                Log.Message(VehicleSystemsMod.LogPrefix + " Dropped the condition " + condition.def!.defName + " of the " + vehicle.LabelShort
                            + ": it was on the part " + condition.partKey + ", which the vehicle no longer has.");
                RemoveAt(i);
                continue;
            }
            condition.severity = (float)SeverityRule.Next(condition.severity, 0, condition.def!.fadePerHour, hours);
            if (SeverityRule.IsGone(condition.severity))
            {
                RemoveAt(i);
                continue;
            }
            Evaluate(condition, hours);
        }
    }

    /// <summary>Works the stages out again without time passing (after a load, a hit or a service).</summary>
    public void EvaluateAll()
    {
        foreach (VehicleCondition condition in list) Evaluate(condition, 0f);
    }

    void Evaluate(VehicleCondition condition, float hours)
    {
        SystemContext? context = ContextFor(condition);
        if (context == null) return;
        PhobosVehicleConditionDef def = condition.def!;
        StageBounds bounds = StageBounds.Of(def.Severity);
        int index = StageRule.StageIndex(condition.severity, bounds.from, bounds.to);
        if (index != condition.stage)
        {
            Run(condition, context, entering: false);
            condition.stage = index;
            Run(condition, context, entering: true);
        }
        StageSpec? current = condition.CurrentStage;
        if (current == null || hours <= 0f) return;
        context.StageKey = StageKey(condition);
        foreach (VehicleSystemEffect effect in current.effects)
        {
            if (effect.Active(context)) effect.Apply(context, hours);
        }
    }

    void Run(VehicleCondition condition, SystemContext context, bool entering)
    {
        StageSpec? stage = condition.CurrentStage;
        if (stage == null) return;
        context.StageKey = StageKey(condition);
        foreach (VehicleSystemEffect effect in stage.effects)
        {
            if (!effect.Active(context)) continue;
            if (entering) effect.OnEnter(context);
            else effect.OnExit(context);
        }
    }

    void RemoveAt(int index)
    {
        VehicleCondition condition = list[index];
        SystemContext? context = ContextFor(condition);
        if (context != null) Run(condition, context, entering: false);
        list.RemoveAt(index);
    }

    VehicleCondition? Find(PhobosVehicleConditionDef def, string partKey)
    {
        foreach (VehicleCondition condition in list)
        {
            if (condition.def == def && condition.partKey == partKey) return condition;
        }
        return null;
    }

    /// <summary>The context a condition's effects run in: the system that gave it, so they read its figures; else the first system.
    /// Null while the system that gave it is switched off (its conditions stand down with it) or the vehicle runs no systems.</summary>
    public SystemContext? ContextFor(VehicleCondition condition)
    {
        IReadOnlyList<SystemState> states = comp.States;
        if (states.Count == 0) return null;
        foreach (SystemState state in states)
        {
            if (state.Def.defName == condition.system) return comp.IsEnabled(state) ? new SystemContext(comp, state) : null;
        }
        return new SystemContext(comp, states[0]);
    }

    public static string StageKey(VehicleCondition condition) => "condition/" + condition.def!.defName + "/" + condition.partKey + "/" + condition.stage;

    public void Clear() => list.Clear();
}
