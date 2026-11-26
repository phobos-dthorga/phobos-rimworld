namespace Phobos.Dosing.Rules;

/// <summary>How much is at stake in a care need: what cost, and what stock, it may spend.</summary>
public enum CareSeriousness
{
    /// <summary>Nothing is wrong yet, or little: prevention.</summary>
    Minor,
    /// <summary>The person is suffering or will be worse off: severe pain, a disease that does not kill.</summary>
    Serious,
    /// <summary>The person may die of it: a disease that can kill, a disease that is winning.</summary>
    LifeThreatening,
}

/// <summary>Who may act on a care need: the player's choice for each need.</summary>
public enum CareMode
{
    Off,
    /// <summary>Doctors give it to patients who are down or in bed.</summary>
    DoctorsOnly,
    /// <summary>Doctors as above, and people on their feet take their own.</summary>
    DoctorsAndSelf,
}

/// <summary>The player's switch for a need, asked two ways. Game-free.</summary>
public static class CareModeRule
{
    public static bool DoctorsMay(CareMode mode) => mode != CareMode.Off;

    public static bool SelfMay(CareMode mode) => mode == CareMode.DoctorsAndSelf;
}

/// <summary>
/// What the care needs add to the safety rule (Medical 0.28.0; docs/development/medical-care-needs.md, the owner's cards of 26 Nov
/// 2026), checked after <see cref="DoseSafetyRule"/> has passed the dose: never begin a dependency that kills when it stops; an
/// addictive item only for a need that is serious or worse; a rare item only to save a life. Game-free.
/// </summary>
public static class CareSafetyRule
{
    public static DoseRefusal Check(CareSeriousness seriousness, float addictiveness, bool beginsLethalDependency, bool rareItem)
    {
        if (beginsLethalDependency) return DoseRefusal.LethalDependency;
        if (addictiveness > 0f && seriousness < CareSeriousness.Serious) return DoseRefusal.AddictiveForMinorNeed;
        if (rareItem && seriousness < CareSeriousness.LifeThreatening) return DoseRefusal.RareItemKept;
        return DoseRefusal.None;
    }

    /// <summary>Whether an item counts as rare: worth the mark or more. A mark of zero or less switches the rule off.</summary>
    public static bool Rare(float marketValue, float rareFromMarketValue) => rareFromMarketValue > 0f && marketValue >= rareFromMarketValue;

    /// <summary>How serious a cure's need is: by whether the condition it removes can kill.</summary>
    public static CareSeriousness CureSeriousness(bool conditionCanKill) => conditionCanKill ? CareSeriousness.LifeThreatening : CareSeriousness.Serious;
}

/// <summary>
/// The reserve (the owner's card of 26 Nov 2026): a need that keeps one may take an item only while more than the reserve is in
/// stock, so it never takes the stock below what the player keeps back. A reserve of zero keeps nothing. Game-free.
/// </summary>
public static class StockReserveRule
{
    public static bool MayTake(int inStock, int reserve) => inStock > (reserve < 0 ? 0 : reserve);
}

/// <summary>
/// Who a breathing rescue is for (Medical 0.29.0; the owner's card of 26 Nov 2026: breathing at 30% or less): someone at or under the
/// level whose breathing is not held there by a ceiling. A stimulant adds to breathing; it cannot lift a ceiling (a blocked airway,
/// a machine breathing for the patient), so under one it would be spent for nothing. Game-free.
/// </summary>
public static class BreathingRescueRule
{
    /// <param name="lowestCeiling">The lowest ceiling any of the patient's conditions sets on breathing; 1 or more when none.</param>
    public static bool NeedsRescue(float breathing, float rescueLevel, float lowestCeiling) => breathing <= rescueLevel && lowestCeiling > rescueLevel;
}

/// <summary>
/// Whether toxic exposure is under way (Medical 0.30.0; the owner's card of 26 Nov 2026, "while exposure is under way"): the buildup
/// has begun, and its source is still around the person: polluted ground, ground that builds toxins by itself, tox gas, or open sky
/// while toxic fallout or toxic weather lasts (the sources the game itself deals toxic buildup from). Someone the game already keeps
/// fully from it (both resistances add up to nothing taken in) is left alone. Game-free.
/// </summary>
public static class ExposureRule
{
    public static bool ToxicUnderWay(float buildup, float buildupFrom, bool sourceNow, float intakeFactor) =>
        buildup >= buildupFrom && sourceNow && intakeFactor > 0.001f;

    /// <summary>What the game lets through of a toxic source, from the two resistances it reads.</summary>
    public static float IntakeFactor(float toxicResistance, float toxicEnvironmentResistance) =>
        Clamp01(1f - toxicResistance) * Clamp01(1f - toxicEnvironmentResistance);

    /// <summary>Caught in vacuum: standing where the game harms the person (half vacuum or more) with less resistance than keeps it out.</summary>
    public static bool CaughtInVacuum(float vacuumHere, float vacuumResistance) => vacuumHere >= 0.5f && vacuumResistance < 1f;

    static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
}

/// <summary>
/// Who the long-term needs are for (Medical 0.31.0; docs/development/medical-care-needs.md, slice E, the owner's cards of 26 Nov 2026).
/// Game-free: the service reads the figures, these say what they mean.
/// </summary>
public static class LongTermRule
{
    /// <summary>Mental strain: mood below the major break threshold, and not in a break already (no one can be dosed mid-break).</summary>
    public static bool Strained(float mood, float majorBreakThreshold, bool inMentalState) => !inMentalState && mood < majorBreakThreshold;

    /// <summary>
    /// An addiction to treat: one the player is making the person quit (their drug policy feeds it with no drug of its chemical), and
    /// never a dependency that kills when it stops (luciferium), which is not withdrawn from by rule.
    /// </summary>
    public static bool QuitWanted(bool policyFeedsIt, bool lethalWhenUnmet) => !policyFeedsIt && !lethalWhenUnmet;

    /// <summary>An old wound or chronic condition that matters: it hurts, or it takes from a capacity or from a part that serves one.</summary>
    public static bool OldWoundMatters(float pain, bool takesFromACapacity, float partEfficiency) =>
        pain > 0.001f || takesFromACapacity || partEfficiency < 0.999f;

    /// <summary>A reservoir wants a refill once a whole dose fits: what the item adds is not spilled.</summary>
    public static bool ReservoirWantsRefill(float level, float capacity, float dose) => dose > 0f && capacity > 0f && level <= capacity - dose;
}

/// <summary>
/// Who prevention is for: someone the disease could still strike. The game's own figure is the chance factor of catching it: 0 for
/// someone immune by a condition or a gene, or who has it already; below 1 for someone with immunity left over from an earlier bout;
/// 1 for everyone else. Game-free.
/// </summary>
public static class PreventionRule
{
    public static bool Unprotected(float contractChanceFactor, float minimumRisk) => contractChanceFactor > 0f && contractChanceFactor >= minimumRisk;
}
