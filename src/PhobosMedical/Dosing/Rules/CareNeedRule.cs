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
/// Who prevention is for: someone the disease could still strike. The game's own figure is the chance factor of catching it: 0 for
/// someone immune by a condition or a gene, or who has it already; below 1 for someone with immunity left over from an earlier bout;
/// 1 for everyone else. Game-free.
/// </summary>
public static class PreventionRule
{
    public static bool Unprotected(float contractChanceFactor, float minimumRisk) => contractChanceFactor > 0f && contractChanceFactor >= minimumRisk;
}
