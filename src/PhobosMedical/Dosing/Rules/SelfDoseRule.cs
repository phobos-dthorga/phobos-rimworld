namespace Phobos.Dosing.Rules;

/// <summary>
/// When a pawn who is up and about goes looking for a painkiller, and which ones they may take. The drug policy gates it (owner's
/// choice, 5 Nov 2026), with the game's own exception: a pawn with a taste for drugs ignores the policy, as in its joy givers.
/// Game-free.
/// </summary>
public static class SelfDoseRule
{
    /// <summary>The pain from which this pawn looks for relief; a taste for drugs lowers it. Trait degree 1 is interest, 2 fascination.</summary>
    public static float Threshold(float baseThreshold, int drugDesireDegree, float interestOffset, float fascinationOffset)
    {
        float threshold = drugDesireDegree >= 2 ? baseThreshold - fascinationOffset : drugDesireDegree == 1 ? baseThreshold - interestOffset : baseThreshold;
        return threshold < 0f ? 0f : threshold;
    }

    /// <summary>Whether the pawn's drug policy lets them take this on their own: for joy or on a schedule, or any drug with a taste for them.</summary>
    public static bool PolicyAllows(bool allowedForJoy, bool allowScheduled, int drugDesireDegree) =>
        allowedForJoy || allowScheduled || drugDesireDegree > 0;

    /// <summary>
    /// A pawn with no drug policy. The game gives policies only to the player's own faction, so a guest has nothing to gate by: they may
    /// take medical painkillers and never recreational ones. Anyone else with no policy takes nothing.
    /// </summary>
    public static bool NoPolicyAllows(bool isGuest, bool recreational) => isGuest && !recreational;

    /// <summary>
    /// Whether a person may take a preventive on their own (Medical 0.28.0). A drug policy has a tick only for a drug taken for pleasure
    /// or one that addicts; a plain medicine has a schedule and nothing else, so the policy cannot say yes or no to it. Where the policy
    /// can speak, it decides, as for a painkiller; where it cannot, the need's own setting is the gate. Agent's reading of the owner's
    /// card of 26 Nov 2026 ("by their drug policy"), for the owner to revise.
    /// </summary>
    public static bool PreventionPolicyAllows(bool policyCanSpeak, bool policyAllows) => !policyCanSpeak || policyAllows;

    /// <summary>Pain relief only: a pawn never sedates themself.</summary>
    public static DoseNeed Need(float pain, float threshold) => pain >= threshold ? DoseNeed.PainRelief : DoseNeed.None;
}
