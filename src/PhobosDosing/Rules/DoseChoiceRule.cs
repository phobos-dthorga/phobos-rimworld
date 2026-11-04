using System.Collections.Generic;

namespace Phobos.Dosing.Rules;

/// <summary>How much each side effect counts against relief (authored).</summary>
public readonly struct DoseWeights
{
    public readonly float Consciousness;
    public readonly float Breathing;
    public readonly float Bleed;
    public readonly float Addiction;

    public DoseWeights(float consciousness, float breathing, float bleed, float addiction)
    {
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
/// Which dose to give: the strongest relief net of its side effects. Painkillers are ranked first; a sedative is chosen only
/// when sedation is wanted and no painkiller scores above zero. The score does not depend on how bad the pain is, so the
/// choice is the same every time and easy to explain. Game-free.
/// </summary>
public static class DoseChoiceRule
{
    public static float Score(DoseEffect effect, bool patientBleeding, DoseWeights weights)
    {
        float bleedWeight = patientBleeding ? weights.Bleed : weights.Bleed * 0.25f;
        return effect.PainRelief
            - (effect.Sedative ? 0f : weights.Consciousness * effect.ConsciousnessCost)
            - weights.Breathing * effect.BreathingCost
            - bleedWeight * effect.BleedIncrease
            - weights.Addiction * effect.Addictiveness;
    }

    /// <summary>The index of the chosen candidate, or -1 when none may be given.</summary>
    public static int Choose(IReadOnlyList<DoseCandidate> candidates, DoseNeed need, bool patientBleeding, DoseWeights weights)
    {
        int best = Best(candidates, false, patientBleeding, weights);
        if (best < 0 && need == DoseNeed.Sedation) best = Best(candidates, true, patientBleeding, weights);
        return best;
    }

    static int Best(IReadOnlyList<DoseCandidate> candidates, bool sedatives, bool patientBleeding, DoseWeights weights)
    {
        int best = -1;
        float bestScore = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            DoseCandidate candidate = candidates[i];
            if (!candidate.Allowed || candidate.Effect.Sedative != sedatives) continue;
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
