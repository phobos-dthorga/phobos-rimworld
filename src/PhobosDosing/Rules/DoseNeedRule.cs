namespace Phobos.Dosing.Rules;

public enum DoseNeed
{
    None,
    /// <summary>Pain past the pain band: a painkiller, never a sedative.</summary>
    PainRelief,
    /// <summary>Pain past the sedation band: a painkiller first, a sedative if nothing lighter can be given.</summary>
    Sedation,
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
