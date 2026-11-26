using System.Collections.Generic;

namespace Phobos.Dosing.Rules;

/// <summary>One outcome condition's stage at the dose, as the index reads it from the Def. Game-free.</summary>
public readonly struct StageEffect
{
    /// <summary>The stage's pain factor; 1 changes nothing.</summary>
    public readonly float PainFactor;
    /// <summary>The stage's pain offset; negative relieves.</summary>
    public readonly float PainOffset;
    /// <summary>Consciousness offset; negative costs.</summary>
    public readonly float ConsciousnessOffset;
    /// <summary>Consciousness ceiling the stage sets; 1 or more means none.</summary>
    public readonly float ConsciousnessSetMax;
    /// <summary>Breathing offset; negative costs.</summary>
    public readonly float BreathingOffset;
    /// <summary>The stage's total bleed factor; 1 changes nothing.</summary>
    public readonly float BleedFactor;
    /// <summary>What the stage adds to the speed of gaining immunity; 0 changes nothing.</summary>
    public readonly float ImmunityGain;
    /// <summary>The stage's mean days between vomits; 0 or less means it causes none.</summary>
    public readonly float VomitMtbDays;
    /// <summary>What the stage adds to the game's toxic resistance and toxic environment resistance, and to vacuum resistance (0.30.0).</summary>
    public readonly float ToxicResistance;
    public readonly float ToxicEnvironmentResistance;
    public readonly float VacuumResistance;

    public StageEffect(float painFactor, float painOffset, float consciousnessOffset, float consciousnessSetMax, float breathingOffset, float bleedFactor, float immunityGain = 0f,
        float vomitMtbDays = 0f, float toxicResistance = 0f, float toxicEnvironmentResistance = 0f, float vacuumResistance = 0f)
    {
        ToxicResistance = toxicResistance;
        ToxicEnvironmentResistance = toxicEnvironmentResistance;
        VacuumResistance = vacuumResistance;
        VomitMtbDays = vomitMtbDays;
        ImmunityGain = immunityGain;
        PainFactor = painFactor;
        PainOffset = painOffset;
        ConsciousnessOffset = consciousnessOffset;
        ConsciousnessSetMax = consciousnessSetMax;
        BreathingOffset = breathingOffset;
        BleedFactor = bleedFactor;
    }
}

/// <summary>What one dose of an item does, combined over every condition it gives. Game-free.</summary>
public readonly struct DoseEffect
{
    /// <summary>Share of the patient's pain the dose takes away, 0 to 1.</summary>
    public readonly float PainRelief;
    /// <summary>Consciousness taken by offsets, 0 to 1 (a ceiling is not a cost: it cannot kill).</summary>
    public readonly float ConsciousnessCost;
    /// <summary>The lowest consciousness ceiling any condition sets, 1 when none.</summary>
    public readonly float ConsciousnessCap;
    /// <summary>Breathing taken by offsets, 0 to 1.</summary>
    public readonly float BreathingCost;
    /// <summary>How much faster the patient bleeds, 0 when not at all.</summary>
    public readonly float BleedIncrease;
    public readonly float Addictiveness;
    /// <summary>A social or hard drug, not a medical one.</summary>
    public readonly bool Recreational;
    /// <summary>Knocks the patient out: only ever given when sedation is wanted.</summary>
    public readonly bool Sedative;
    /// <summary>How much faster the patient gains immunity, 0 when no faster.</summary>
    public readonly float ImmunityGain;
    /// <summary>How often the dose makes the patient vomit, per day, over every condition it gives; 0 when never. Counted only for the
    /// care needs of Medical 0.28.0 (a cure, prevention): the pain and immunity scores are as they were.</summary>
    public readonly float VomitsPerDay;
    /// <summary>Breathing added by offsets, over every condition the dose gives; 0 when none (Medical 0.29.0). A cost elsewhere in
    /// the same dose is kept apart in <see cref="BreathingCost"/>: the two are weighed against each other, never netted here.</summary>
    public readonly float BreathingGain;
    /// <summary>The share of toxic buildup the dose keeps out, 0 to 1 (Medical 0.30.0). The game multiplies what a person takes in by
    /// one less each of the two toxic resistances (ToxicUtility.DoPawnToxicDamage), so the share is one less that product.</summary>
    public readonly float ToxicCut;
    /// <summary>Vacuum resistance added by the dose, 0 when none (Medical 0.30.0).</summary>
    public readonly float VacuumGain;

    public DoseEffect(float painRelief, float consciousnessCost, float consciousnessCap, float breathingCost, float bleedIncrease, float addictiveness, bool recreational, bool sedative, float immunityGain = 0f,
        float vomitsPerDay = 0f, float breathingGain = 0f, float toxicCut = 0f, float vacuumGain = 0f)
    {
        ToxicCut = toxicCut;
        VacuumGain = vacuumGain;
        BreathingGain = breathingGain;
        VomitsPerDay = vomitsPerDay;
        ImmunityGain = immunityGain;
        PainRelief = painRelief;
        ConsciousnessCost = consciousnessCost;
        ConsciousnessCap = consciousnessCap;
        BreathingCost = breathingCost;
        BleedIncrease = bleedIncrease;
        Addictiveness = addictiveness;
        Recreational = recreational;
        Sedative = sedative;
    }

    public bool RelievesPain => PainRelief > 0f;
}

/// <summary>
/// Turns what the Defs say a dose does into one effect. Pain factors multiply and offsets add, the way the game applies them;
/// consciousness and breathing offsets add as costs; the lowest consciousness ceiling is kept. A dose is a sedative when its
/// ceiling leaves the patient unconscious (authored mark); an offset, however large, is a cost, not a sedation. Game-free.
/// </summary>
public static class DoseEffectRule
{
    public static DoseEffect Combine(IReadOnlyList<StageEffect> stages, float addictiveness, bool recreational, float sedativeCeiling)
    {
        float painFactor = 1f;
        float painOffset = 0f;
        float consciousnessCost = 0f;
        float consciousnessCap = 1f;
        float breathingCost = 0f;
        float bleedFactor = 1f;
        float immunityGain = 0f;
        float vomitsPerDay = 0f;
        float breathingGain = 0f;
        float toxicResistance = 0f;
        float toxicEnvironmentResistance = 0f;
        float vacuumResistance = 0f;
        for (int i = 0; i < stages.Count; i++)
        {
            StageEffect stage = stages[i];
            painFactor *= stage.PainFactor;
            painOffset += stage.PainOffset;
            if (stage.ConsciousnessOffset < 0f) consciousnessCost -= stage.ConsciousnessOffset;
            if (stage.ConsciousnessSetMax < consciousnessCap) consciousnessCap = stage.ConsciousnessSetMax;
            if (stage.BreathingOffset < 0f) breathingCost -= stage.BreathingOffset;
            else breathingGain += stage.BreathingOffset;
            bleedFactor *= stage.BleedFactor;
            immunityGain += stage.ImmunityGain;
            if (stage.VomitMtbDays > 0f) vomitsPerDay += 1f / stage.VomitMtbDays;
            toxicResistance += stage.ToxicResistance;
            toxicEnvironmentResistance += stage.ToxicEnvironmentResistance;
            vacuumResistance += stage.VacuumResistance;
        }
        float relief = Clamp01(1f - painFactor + (painOffset < 0f ? -painOffset : 0f));
        bool sedative = consciousnessCap <= sedativeCeiling;
        return new DoseEffect(relief, Clamp01(consciousnessCost), consciousnessCap, Clamp01(breathingCost), bleedFactor > 1f ? bleedFactor - 1f : 0f,
            addictiveness < 0f ? 0f : addictiveness, recreational, sedative, immunityGain > 0f ? immunityGain : 0f, vomitsPerDay, breathingGain,
            1f - (1f - Clamp01(toxicResistance)) * (1f - Clamp01(toxicEnvironmentResistance)), vacuumResistance > 0f ? vacuumResistance : 0f);
    }

    static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
}
