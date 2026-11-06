namespace Phobos.Triage.Rules;

/// <summary>The triage tag a patient wears. The order is the order doctors take them, worst first.</summary>
public enum TriageCategory
{
    /// <summary>Dying, and nothing the colony has can be done: no operation possible, no field care that holds.</summary>
    Expectant,
    Minor,
    /// <summary>Crippled, not dying.</summary>
    Delayed,
    /// <summary>Dying, with time.</summary>
    Urgent,
    /// <summary>Dying within hours.</summary>
    Immediate,
}

/// <summary>A patient as the planner read them.</summary>
public readonly struct PatientReading
{
    /// <summary>Hours until the worst condition kills; infinity when no clock applies.</summary>
    public readonly float HoursToDeath;
    public readonly ConditionClass WorstClass;
    /// <summary>The patient's lowest vital capacity (consciousness, breathing, blood pumping).</summary>
    public readonly float LowestVitalLevel;
    /// <summary>The worst condition needs an operation (field care cannot hold it).</summary>
    public readonly bool OperationNeeded;
    /// <summary>An operation for it can be queued right now.</summary>
    public readonly bool OperationFeasible;

    public PatientReading(float hoursToDeath, ConditionClass worstClass, float lowestVitalLevel, bool operationNeeded, bool operationFeasible)
    {
        HoursToDeath = hoursToDeath;
        WorstClass = worstClass;
        LowestVitalLevel = lowestVitalLevel;
        OperationNeeded = operationNeeded;
        OperationFeasible = operationFeasible;
    }
}

/// <summary>The bands from the rules Def.</summary>
public readonly struct TriageBands
{
    public readonly float ImmediateHours;
    public readonly float UrgentHours;
    /// <summary>A vital capacity at or under this makes a dying patient immediate whatever the clock says.</summary>
    public readonly float ImmediateVitalLevel;

    public TriageBands(float immediateHours, float urgentHours, float immediateVitalLevel)
    {
        ImmediateHours = immediateHours;
        UrgentHours = urgentHours;
        ImmediateVitalLevel = immediateVitalLevel;
    }
}

/// <summary>
/// The triage tag and the score doctors order their work by. Scores are banded so the tag always outranks the hours: an
/// immediate case scores above every urgent one, and within a band the one with less time scores higher.
/// </summary>
public static class TriageRule
{
    public const float BandWidth = 100f;

    public static TriageCategory Categorise(in PatientReading r, in TriageBands b)
    {
        if (r.WorstClass == ConditionClass.Lethal)
        {
            if (r.OperationNeeded && !r.OperationFeasible) return TriageCategory.Expectant;
            if (r.HoursToDeath <= b.ImmediateHours || r.LowestVitalLevel <= b.ImmediateVitalLevel) return TriageCategory.Immediate;
            return TriageCategory.Urgent;
        }
        return r.WorstClass == ConditionClass.Crippling ? TriageCategory.Delayed : TriageCategory.Minor;
    }

    /// <summary>The band's base plus up to a band width less one for little time left; a patient with no clock sits at the base.</summary>
    public static float Score(TriageCategory category, float hoursToDeath)
    {
        float baseScore = category switch
        {
            TriageCategory.Immediate => 400f,
            TriageCategory.Urgent => 300f,
            TriageCategory.Delayed => 200f,
            TriageCategory.Minor => 100f,
            _ => 50f,
        };
        // An expectant patient gets no urgency: nothing can be done for them, so no clock lifts them over a minor case.
        if (category == TriageCategory.Expectant || float.IsInfinity(hoursToDeath) || hoursToDeath < 0f) return baseScore;
        float urgency = (BandWidth - 1f) / (1f + hoursToDeath / 24f);
        return baseScore + urgency;
    }
}
