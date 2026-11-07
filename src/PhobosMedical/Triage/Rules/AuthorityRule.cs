namespace Phobos.Triage.Rules;

/// <summary>How far the doctors may go for a patient without being asked. Saved on the pawn by index: never reorder.</summary>
public enum TriageAuthority
{
    None,
    LifeSaving,
    LifeSavingAndCrippling,
    AnyRepair,
}

/// <summary>What a condition does to a patient, read from its effects. Ordered from harmless to deadly.</summary>
public enum ConditionClass
{
    Other,
    Crippling,
    Lethal,
}

/// <summary>The one place that says which class of condition an authority level lets the doctors operate on.</summary>
public static class AuthorityRule
{
    public static bool Allows(TriageAuthority authority, ConditionClass conditionClass) => authority switch
    {
        TriageAuthority.None => false,
        TriageAuthority.LifeSaving => conditionClass == ConditionClass.Lethal,
        TriageAuthority.LifeSavingAndCrippling => conditionClass >= ConditionClass.Crippling,
        TriageAuthority.AnyRepair => true,
        _ => false,
    };
}
