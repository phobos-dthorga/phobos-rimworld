namespace Phobos.Dosing.Rules;

public enum DoseNeed
{
    None,
    /// <summary>Pain past the pain band: a painkiller, never a sedative.</summary>
    PainRelief,
    /// <summary>Pain past the sedation band: a painkiller first, a sedative if nothing lighter can be given.</summary>
    Sedation,
    /// <summary>A disease is winning the race against the patient's immunity: a drug that speeds immunity up.</summary>
    Immunity,
    /// <summary>The patient has a condition an item removes outright (Medical 0.28.0, care needs).</summary>
    Cure,
    /// <summary>A disease is on the map and this person is unprotected: an item that makes them immune to it (Medical 0.28.0).</summary>
    Prevention,
    /// <summary>The patient can barely breathe: an item that raises breathing, as a bridge to oxygen or surgery (Medical 0.29.0).</summary>
    Breathing,
    /// <summary>Toxic buildup has begun and its source is still around the person: an item that cuts what they take in (Medical 0.30.0).</summary>
    Toxic,
    /// <summary>The person is caught in vacuum and harmed by it: an item that raises vacuum resistance (Medical 0.30.0).</summary>
    Vacuum,
}

/// <summary>
/// Who needs a dose: a resting patient (downed or in bed) whose pain is past the authored bands. A pawn up and working in
/// pain is left alone: a painkiller's consciousness cost could drop them where they stand. Game-free.
/// </summary>
public static class DoseNeedRule
{
    public static DoseNeed Need(float pain, bool resting, float painThreshold, float sedationThreshold)
    {
        if (!resting || pain < painThreshold) return DoseNeed.None;
        return pain >= sedationThreshold ? DoseNeed.Sedation : DoseNeed.PainRelief;
    }
}
