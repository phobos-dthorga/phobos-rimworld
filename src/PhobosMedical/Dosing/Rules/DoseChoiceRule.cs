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
    /// <summary>What one vomit a day counts against a cure or a preventive (Medical 0.28.0); pain relief and immunity do not count it.</summary>
    public readonly float Vomit;

    public DoseWeights(float consciousness, float breathing, float bleed, float addiction, float bleedWhenNotBleeding, float vomit = 0f)
    {
        Vomit = vomit;
        BleedWhenNotBleeding = bleedWhenNotBleeding;
        Consciousness = consciousness;
        Breathing = breathing;
        Bleed = bleed;
        Addiction = addiction;
    }
}

/// <summary>
/// How much pain there is and where it should be brought under (Medical 0.33.0): with these, a painkiller is chosen as the mildest that
/// does the job rather than the best score.
/// </summary>
public readonly struct PainLadder
{
    public readonly float Pain;
    public readonly float Target;

    public PainLadder(float pain, float target)
    {
        Pain = pain;
        Target = target;
    }

    /// <summary>Whether a dose taking away this share of the pain brings it under the target. An approximation: the game multiplies pain by
    /// a factor and adds an offset, and relief is read as one share of the whole.</summary>
    public bool Enough(float relief) => Pain * (1f - relief) < Target;
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

    /// <summary>
    /// Whether an item is worth indexing as a drug for immunity: a real gain, its costs below that gain, and never a sedative or a
    /// recreational drug, because this is a doctor's prescription.
    /// </summary>
    public static bool WorthwhileForImmunity(DoseEffect effect, float minImmunityGain, DoseWeights weights) =>
        effect.ImmunityGain >= minImmunityGain && !effect.Sedative && !effect.Recreational && Score(effect, DoseNeed.Immunity, false, weights) > 0f;

    /// <summary>
    /// Whether an item is worth indexing as a cure or a preventive (Medical 0.28.0): its costs must leave something of the benefit, and
    /// it is never a sedative or a recreational drug. An item that mostly harms (it makes the patient vomit all day, or takes most of
    /// their consciousness) scores zero or less and is on no list, without being named.
    /// </summary>
    public static bool WorthwhileForCare(DoseEffect effect, DoseNeed need, DoseWeights weights) =>
        !effect.Sedative && !effect.Recreational && Score(effect, need, false, weights) > 0f;

    /// <summary>
    /// Whether an item is worth indexing as a help to breathing (Medical 0.29.0): a real gain left after any breathing it costs, its
    /// other costs below that gain, and never a sedative or a recreational drug.
    /// </summary>
    public static bool WorthwhileForBreathing(DoseEffect effect, float minBreathingGain, DoseWeights weights) =>
        effect.BreathingGain - effect.BreathingCost >= minBreathingGain && !effect.Sedative && !effect.Recreational
        && Score(effect, DoseNeed.Breathing, false, weights) > 0f;

    /// <summary>
    /// Whether an item is worth indexing as protection against toxins or vacuum (Medical 0.30.0): it must keep out at least the given
    /// share of toxic buildup, or add at least the given vacuum resistance, its costs below that, and never be a sedative or a
    /// recreational drug.
    /// </summary>
    public static bool WorthwhileForExposure(DoseEffect effect, DoseNeed need, float minimum, DoseWeights weights) =>
        (need == DoseNeed.Toxic ? effect.ToxicCut : need == DoseNeed.Vacuum ? effect.VacuumGain : 0f) >= minimum && minimum > 0f
        && !effect.Sedative && !effect.Recreational && Score(effect, need, false, weights) > 0f;

    public static float Score(DoseEffect effect, bool patientBleeding, DoseWeights weights) => Score(effect, DoseNeed.PainRelief, patientBleeding, weights);

    /// <summary>What the dose is worth for this need, net of its side effects.</summary>
    public static float Score(DoseEffect effect, DoseNeed need, bool patientBleeding, DoseWeights weights)
    {
        float bleedWeight = patientBleeding ? weights.Bleed : weights.Bleed * weights.BleedWhenNotBleeding;
        // A cure removes the condition and a preventive stops it: the whole benefit, the same for every item that does it, so the
        // choice between two of them is by their costs alone.
        float vomit = CountsVomit(need) ? weights.Vomit * effect.VomitsPerDay : 0f;
        return Benefit(effect, need)
            - (vomit > 1f ? 1f : vomit)
            - (effect.Sedative ? 0f : weights.Consciousness * effect.ConsciousnessCost)
            - weights.Breathing * effect.BreathingCost
            - bleedWeight * effect.BleedIncrease
            - weights.Addiction * effect.Addictiveness;
    }

    /// <summary>What the dose is for: the whole of it for a cure or a preventive, the share it gives for every other need.</summary>
    static float Benefit(DoseEffect effect, DoseNeed need) => need switch
    {
        DoseNeed.Cure or DoseNeed.Prevention => 1f,
        DoseNeed.MentalStrain or DoseNeed.Addiction or DoseNeed.OldWounds or DoseNeed.MissingPart or DoseNeed.Reservoir or DoseNeed.Vaccination => 1f,
        DoseNeed.Immunity => effect.ImmunityGain,
        DoseNeed.Breathing => effect.BreathingGain,
        DoseNeed.Toxic => effect.ToxicCut,
        DoseNeed.Vacuum => effect.VacuumGain > 1f ? 1f : effect.VacuumGain,
        _ => effect.PainRelief,
    };

    /// <summary>Vomiting counts against the care needs of 0.28.0 and later; the pain and immunity scores are as they were before.</summary>
    static bool CountsVomit(DoseNeed need) =>
        need is DoseNeed.Cure or DoseNeed.Prevention or DoseNeed.Breathing or DoseNeed.Toxic or DoseNeed.Vacuum or DoseNeed.MentalStrain
            or DoseNeed.Addiction or DoseNeed.OldWounds or DoseNeed.MissingPart or DoseNeed.Reservoir or DoseNeed.Vaccination;

    /// <summary>The index of the chosen candidate, or -1 when none may be given.</summary>
    public static int Choose(IReadOnlyList<DoseCandidate> candidates, DoseNeed need, bool patientBleeding, DoseWeights weights) =>
        Choose(candidates, need, patientBleeding, weights, null);

    /// <summary>
    /// The index of the chosen candidate, or -1 when none may be given. With a <paramref name="ladder"/>, a painkiller is the mildest that
    /// does the job (Medical 0.33.0; the owner's card of 27 Nov 2026): within each tier, of the items that bring the pain under its
    /// target, the one with the least cost, and the gentler of two that cost the same; when none can, the strongest. The shape of the
    /// World Health Organization's analgesic ladder, mild medicines first and strong ones as pain demands, named as the model only.
    /// </summary>
    public static int Choose(IReadOnlyList<DoseCandidate> candidates, DoseNeed need, bool patientBleeding, DoseWeights weights, PainLadder? ladder)
    {
        PainLadder? forPain = need == DoseNeed.PainRelief || need == DoseNeed.Sedation ? ladder : null;
        int best = Best(candidates, need, false, false, patientBleeding, weights, forPain);
        if (best < 0 && need == DoseNeed.Sedation) best = Best(candidates, need, true, null, patientBleeding, weights, null);
        if (best < 0) best = Best(candidates, need, false, true, patientBleeding, weights, forPain);
        return best;
    }

    /// <summary>What a dose costs, net of nothing: its benefit for the need less its score.</summary>
    static float Cost(DoseEffect effect, DoseNeed need, bool patientBleeding, DoseWeights weights) =>
        Benefit(effect, need) - Score(effect, need, patientBleeding, weights);

    /// <summary>The mildest of one tier that does the job, or the strongest when none can; only items scoring above zero.</summary>
    static int Ladder(IReadOnlyList<DoseCandidate> candidates, DoseNeed need, bool sedatives, bool? recreational, bool patientBleeding, DoseWeights weights, PainLadder ladder)
    {
        int mildest = -1, strongest = -1;
        float mildestCost = float.MaxValue;
        for (int i = 0; i < candidates.Count; i++)
        {
            DoseCandidate candidate = candidates[i];
            if (!candidate.Allowed || candidate.Effect.Sedative != sedatives) continue;
            if (recreational.HasValue && candidate.Effect.Recreational != recreational.Value) continue;
            if (Score(candidate.Effect, need, patientBleeding, weights) <= 0f) continue;
            float relief = candidate.Effect.PainRelief;
            if (strongest < 0 || relief > candidates[strongest].Effect.PainRelief) strongest = i;
            if (!ladder.Enough(relief)) continue;
            float cost = Cost(candidate.Effect, need, patientBleeding, weights);
            if (mildest < 0 || cost < mildestCost - 1e-5f || (cost <= mildestCost + 1e-5f && relief < candidates[mildest].Effect.PainRelief))
            {
                mildest = i;
                mildestCost = cost;
            }
        }
        return mildest >= 0 ? mildest : strongest;
    }

    /// <summary>The best allowed candidate of one kind; a null <paramref name="recreational"/> takes either.</summary>
    static int Best(IReadOnlyList<DoseCandidate> candidates, DoseNeed need, bool sedatives, bool? recreational, bool patientBleeding, DoseWeights weights, PainLadder? ladder)
    {
        if (ladder.HasValue && !sedatives) return Ladder(candidates, need, sedatives, recreational, patientBleeding, weights, ladder.Value);
        int best = -1;
        float bestScore = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            DoseCandidate candidate = candidates[i];
            if (!candidate.Allowed || candidate.Effect.Sedative != sedatives) continue;
            if (recreational.HasValue && candidate.Effect.Recreational != recreational.Value) continue;
            float score = Score(candidate.Effect, need, patientBleeding, weights);
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return best;
    }
}
