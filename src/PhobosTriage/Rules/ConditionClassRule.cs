namespace Phobos.Triage.Rules;

/// <summary>A condition as its effects describe it, read from the game by the caller. Capacity figures are 0 to 1.</summary>
public readonly struct ConditionReading
{
    /// <summary>The game's own flag on the current stage.</summary>
    public readonly bool LifeThreateningNow;
    /// <summary>The current stage's death clock in days; zero or less for none.</summary>
    public readonly float DeathMtbDays;
    /// <summary>The severity at which the condition kills; zero or less for none.</summary>
    public readonly float LethalSeverity;
    public readonly float Severity;
    public readonly float SeverityPerDay;
    /// <summary>The Def says the condition is bad (an injury or disease rather than a treatment or a drug's effect).</summary>
    public readonly bool IsBad;
    /// <summary>It goes away on its own: a disappearing comp, or a falling severity.</summary>
    public readonly bool Clears;
    /// <summary>The most negative offset this condition puts on consciousness, breathing or blood pumping; zero when none.</summary>
    public readonly float VitalOffset;
    /// <summary>The patient's current level of the vital capacity this condition pushes down hardest; 1 when it pushes none.</summary>
    public readonly float VitalLevel;
    /// <summary>The lowest cap this condition puts on any capacity; 1 when none.</summary>
    public readonly float CapacityCap;
    /// <summary>The most negative offset this condition puts on any capacity; zero when none.</summary>
    public readonly float CapacityOffset;
    /// <summary>The current stage's part efficiency offset; zero when none.</summary>
    public readonly float PartEfficiencyOffset;

    public ConditionReading(bool lifeThreateningNow, float deathMtbDays, float lethalSeverity, float severity, float severityPerDay, bool isBad, bool clears,
        float vitalOffset, float vitalLevel, float capacityCap, float capacityOffset, float partEfficiencyOffset)
    {
        LifeThreateningNow = lifeThreateningNow;
        DeathMtbDays = deathMtbDays;
        LethalSeverity = lethalSeverity;
        Severity = severity;
        SeverityPerDay = severityPerDay;
        IsBad = isBad;
        Clears = clears;
        VitalOffset = vitalOffset;
        VitalLevel = vitalLevel;
        CapacityCap = capacityCap;
        CapacityOffset = capacityOffset;
        PartEfficiencyOffset = partEfficiencyOffset;
    }
}

/// <summary>The thresholds from the rules Def.</summary>
public readonly struct ClassThresholds
{
    /// <summary>A vital capacity at or under this, pushed down by the condition, is lethal.</summary>
    public readonly float LethalCapacityLevel;
    /// <summary>A capacity capped at or under this cripples.</summary>
    public readonly float CripplingCapacityLevel;
    /// <summary>A part efficiency or capacity offset at or under this cripples.</summary>
    public readonly float CripplingOffset;

    public ClassThresholds(float lethalCapacityLevel, float cripplingCapacityLevel, float cripplingOffset)
    {
        LethalCapacityLevel = lethalCapacityLevel;
        CripplingCapacityLevel = cripplingCapacityLevel;
        CripplingOffset = cripplingOffset;
    }
}

/// <summary>
/// Classes a condition by what it does, never by its name. Lethal: the game says it threatens life now, it carries a death
/// clock, its severity is climbing toward a lethal one, or it is a bad condition that will not clear and is holding a vital
/// capacity at the floor. A cap alone never kills (the game kills at zero, and every sedative caps consciousness), so a cap
/// counts as crippling at most. Crippling: a lasting cap, offset or part efficiency loss past the thresholds.
/// </summary>
public static class ConditionClassRule
{
    public static ConditionClass Classify(in ConditionReading r, in ClassThresholds t)
    {
        if (r.LifeThreateningNow || r.DeathMtbDays > 0f) return ConditionClass.Lethal;
        if (r.LethalSeverity > 0f && r.SeverityPerDay > 0f) return ConditionClass.Lethal;
        if (r.IsBad && !r.Clears && r.VitalOffset < 0f && r.VitalLevel <= t.LethalCapacityLevel) return ConditionClass.Lethal;
        if (!r.Clears && (r.PartEfficiencyOffset <= t.CripplingOffset || r.CapacityOffset <= t.CripplingOffset || r.CapacityCap <= t.CripplingCapacityLevel))
            return ConditionClass.Crippling;
        return ConditionClass.Other;
    }
}
