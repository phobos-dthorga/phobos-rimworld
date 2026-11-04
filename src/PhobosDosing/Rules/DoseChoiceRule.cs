using System.Collections.Generic;

namespace Phobos.Dosing.Rules;

/// <summary>How much each side effect counts against relief (authored).</summary>
public readonly struct DoseWeights
{
    public readonly float Consciousness;
    public readonly float Breathing;
    public readonly float Bleed;
    public readonly float Addiction;
    /// <summary>The share of the bleed weight that counts while the patient is not bleeding.</summary>
    public readonly float BleedWhenNotBleeding;

    public DoseWeights(float consciousness, float breathing, float bleed, float addiction, float bleedWhenNotBleeding)
    {
        BleedWhenNotBleeding = bleedWhenNotBleeding;
        Consciousness = consciousness;
        Breathing = breathing;
        Bleed = bleed;
        Addiction = addiction;
    }
}

/// <summary>One item the doctor could give: its effect and whether the safety rule allows it.</summary>
public readonly struct DoseCandidate
{
    public readonly DoseEffect Effect;
    public readonly bool Allowed;

    public DoseCandidate(DoseEffect effect, bool allowed)
    {
        Effect = effect;
        Allowed = allowed;
    }
}

/// <summary>
/// Which dose to give: the strongest relief net of its side effects, in tiers. Medical painkillers first; then, when sedation
/// is wanted, a sedative; recreational painkillers only as the last resort, because a clean score must not put an addictive
/// high ahead of a medical drug. The score does not depend on how bad the pain is, so the choice is the same every time and
/// easy to explain. Game-free.
/// </summary>
public static class DoseChoiceRule
{
    /// <summary>
    /// Whether an item is worth indexing at all: it must take away a real share of the pain (a drug whose relief is a side
    /// effect, such as an adrenaline shot kept for cardiac arrest, must not be spent on pain) and score above zero.
    /// </summary>
    public static bool Worthwhile(DoseEffect effect, float minPainRelief, DoseWeights weights) =>
        effect.PainRelief >= minPainRelief && Score(effect, false, weights) > 0f;

    public static float Score(DoseEffect effect, bool patientBleeding, DoseWeights weights)
    {
        float bleedWeight = patientBleeding ? weights.Bleed : weights.Bleed * weights.BleedWhenNotBleeding;
        return effect.PainRelief
            - (effect.Sedative ? 0f : weights.Consciousness * effect.ConsciousnessCost)
            - weights.Breathing * effect.BreathingCost
            - bleedWeight * effect.BleedIncrease
            - weights.Addiction * effect.Addictiveness;
    }

    /// <summary>The index of the chosen candidate, or -1 when none may be given.</summary>
    public static int Choose(IReadOnlyList<DoseCandidate> candidates, DoseNeed need, bool patientBleeding, DoseWeights weights)
    {
        int best = Best(candidates, false, false, patientBleeding, weights);
        if (best < 0 && need == DoseNeed.Sedation) best = Best(candidates, true, null, patientBleeding, weights);
        if (best < 0) best = Best(candidates, false, true, patientBleeding, weights);
        return best;
    }

    /// <summary>The best allowed candidate of one kind; a null <paramref name="recreational"/> takes either.</summary>
    static int Best(IReadOnlyList<DoseCandidate> candidates, bool sedatives, bool? recreational, bool patientBleeding, DoseWeights weights)
    {
        int best = -1;
        float bestScore = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            DoseCandidate candidate = candidates[i];
            if (!candidate.Allowed || candidate.Effect.Sedative != sedatives) continue;
            if (recreational.HasValue && candidate.Effect.Recreational != recreational.Value) continue;
            float score = Score(candidate.Effect, patientBleeding, weights);
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return best;
    }
}
