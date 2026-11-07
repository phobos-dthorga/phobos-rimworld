using Phobos.Dosing.Rules;
using Xunit;

namespace Phobos.Dosing.Rules.Tests;

/// <summary>Stage figures as the owner's mods define them (read 5 Nov 2026), used as synthetic fixtures.</summary>
static class Fixtures
{
    // More Injuries' morphine at a 0.35 dose: "maximum safe dose" stage.
    public static readonly StageEffect MorphineStage = new(0.2f, 0f, -0.4f, 1f, -0.2f, 1f);
    // The game's anesthetic at 1.2 (capped at 1): "sedated".
    public static readonly StageEffect AnestheticStage = new(0f, 0f, 0f, 0.01f, 0f, 1f);
    // More Injuries' ketamine buildup at 0.35: no pain or capacity change at that stage.
    public static readonly StageEffect KetamineBuildupStage = new(1f, 0f, 0f, 1f, 0f, 1f);
    // Medicines+ thornweed serum at 0.5: "moderately analgesic".
    public static readonly StageEffect ThornweedStage = new(0.6f, 0f, 0f, 1f, 0f, 1f);
    // A flake-like high: pain halved, addictive, recreational.
    public static readonly StageEffect FlakeStage = new(0.5f, 0f, 0f, 1f, 0f, 1f);

    // More Injuries' epinephrine at 0.75: pain x0.875, offset -0.1. Relief as a side effect.
    public static readonly StageEffect AdrenalineStage = new(0.875f, -0.1f, 0f, 1f, 0f, 1f);
    // Go-juice at 0.5: pain x0.1, lightly addictive, a hard drug.
    public static readonly StageEffect GoJuiceStage = new(0.1f, 0f, 0f, 1f, 0f, 1f);

    public static DoseEffect Epinephrine => DoseEffectRule.Combine(new[] { AdrenalineStage }, 0f, false, 0.1f);
    public static DoseEffect GoJuice => DoseEffectRule.Combine(new[] { GoJuiceStage }, 0.03f, true, 0.1f);
    public static DoseEffect Morphine => DoseEffectRule.Combine(new[] { MorphineStage }, 0f, false, 0.1f);
    public static DoseEffect Ketamine => DoseEffectRule.Combine(new[] { AnestheticStage, KetamineBuildupStage }, 0f, false, 0.1f);
    public static DoseEffect Thornweed => DoseEffectRule.Combine(new[] { ThornweedStage }, 0f, false, 0.1f);
    public static DoseEffect Flake => DoseEffectRule.Combine(new[] { FlakeStage }, 0.08f, true, 0.1f);
    public static readonly DoseWeights Weights = new(0.5f, 0.5f, 1f, 0.5f, 0.25f);
    public static readonly DoseLimits Limits = new(0.25f, 0.3f, 0.15f, false, false);
    public static readonly PatientState Awake = new(0.8f, 1f, 0f, true);
}

public class DoseEffectRuleTests
{
    [Fact]
    public void AMorphineLikeStageIsAStrongPainkillerWithCosts()
    {
        DoseEffect e = Fixtures.Morphine;
        Assert.Equal(0.8f, e.PainRelief, 4);
        Assert.Equal(0.4f, e.ConsciousnessCost, 4);
        Assert.Equal(0.2f, e.BreathingCost, 4);
        Assert.Equal(1f, e.ConsciousnessCap);
        Assert.False(e.Sedative);
        Assert.False(e.Recreational);
    }

    [Fact]
    public void AnAnestheticCeilingMakesASedativeAndTwoConditionsCombine()
    {
        DoseEffect e = Fixtures.Ketamine;
        Assert.Equal(1f, e.PainRelief, 4);
        Assert.Equal(0.01f, e.ConsciousnessCap, 4);
        Assert.Equal(0f, e.ConsciousnessCost);
        Assert.True(e.Sedative);
    }

    [Fact]
    public void PainFactorsMultiplyAndOffsetsAddAsTheGameAppliesThem()
    {
        DoseEffect e = DoseEffectRule.Combine(new[] { new StageEffect(0.5f, 0f, -0.1f, 1f, 0f, 1.5f), new StageEffect(0.5f, -0.1f, -0.25f, 1f, 0f, 1f) }, 0f, false, 0.1f);
        Assert.Equal(0.85f, e.PainRelief, 4);
        Assert.Equal(0.35f, e.ConsciousnessCost, 4);
        Assert.Equal(0.5f, e.BleedIncrease, 4);
        Assert.False(e.Sedative);
    }

    [Fact]
    public void AStageThatDoesNotTouchPainRelievesNothing()
    {
        Assert.False(DoseEffectRule.Combine(new[] { Fixtures.KetamineBuildupStage }, 0f, false, 0.1f).RelievesPain);
    }
}

public class DoseNeedRuleTests
{
    [Fact]
    public void OnlyARestingPatientPastTheBandsNeedsADose()
    {
        Assert.Equal(DoseNeed.None, DoseNeedRule.Need(0.7f, false, 0.6f, 0.8f));
        Assert.Equal(DoseNeed.None, DoseNeedRule.Need(0.59f, true, 0.6f, 0.8f));
        Assert.Equal(DoseNeed.PainRelief, DoseNeedRule.Need(0.6f, true, 0.6f, 0.8f));
        Assert.Equal(DoseNeed.Sedation, DoseNeedRule.Need(0.8f, true, 0.6f, 0.8f));
    }
}

public class ImmunityTests
{
    static DoseEffect Tyrox => DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0.5f) }, 0f, false, 0.1f);
    static DoseEffect Antibiotics => DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0.1f) }, 0.005f, false, 0.1f);
    static DoseEffect RoseTea => DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0.02f) }, 0f, true, 0.1f);
    static DoseEffect Yuenyeung => DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0.15f) }, 0.0075f, true, 0.1f);

    [Fact]
    public void ADiseaseWinsWhenImmunityWouldArriveTooLate()
    {
        // Lethal in 2 days, immune in about 3.3: losing.
        Assert.True(ImmunityRule.Losing(0.2f, 1f, 0.4f, 0f, 0.3f, 1f));
        // Lethal in 8 days, immune in 2: winning, with room to spare.
        Assert.False(ImmunityRule.Losing(0.2f, 1f, 0.1f, 0f, 0.5f, 1f));
        // Inside the margin: immune in 2 days, lethal in 2.5.
        Assert.True(ImmunityRule.Losing(0.5f, 1f, 0.2f, 0f, 0.5f, 1f));
    }

    [Fact]
    public void TwoDaysOfMarginCatchWhatOneDayMissed()
    {
        // Immune in 2 days, lethal in 3.5 (owner's two-day margin, 7 Nov 2026): one day calls it winning, two days losing.
        Assert.False(ImmunityRule.Losing(0.3f, 1f, 0.2f, 0f, 0.5f, 1f));
        Assert.True(ImmunityRule.Losing(0.3f, 1f, 0.2f, 0f, 0.5f, 2f));
    }

    [Fact]
    public void ADiseaseThatIsNotGettingWorseOrCannotKillIsNotWinning()
    {
        Assert.False(ImmunityRule.Losing(0.5f, 1f, 0f, 0f, 0f, 1f));
        Assert.False(ImmunityRule.Losing(0.5f, 1f, -0.2f, 0f, 0.1f, 1f));
        Assert.False(ImmunityRule.Losing(0.5f, 0f, 0.4f, 0f, 0.1f, 1f));
        Assert.False(ImmunityRule.Losing(0.5f, 1f, 0.4f, 1f, 0.1f, 1f));
        Assert.True(ImmunityRule.Losing(0.5f, 1f, 0.1f, 0.2f, 0f, 1f));
    }

    [Fact]
    public void OnlyARealGainFromAMedicalDrugCounts()
    {
        Assert.True(DoseChoiceRule.WorthwhileForImmunity(Tyrox, 0.1f, Fixtures.Weights));
        Assert.True(DoseChoiceRule.WorthwhileForImmunity(Antibiotics, 0.1f, Fixtures.Weights));
        Assert.False(DoseChoiceRule.WorthwhileForImmunity(RoseTea, 0.1f, Fixtures.Weights));
        Assert.False(DoseChoiceRule.WorthwhileForImmunity(Yuenyeung, 0.1f, Fixtures.Weights));
        Assert.False(DoseChoiceRule.WorthwhileForImmunity(Fixtures.Morphine, 0.1f, Fixtures.Weights));
        // All cost and little gain: addictiveness 1 against a gain of 0.2.
        DoseEffect addictive = DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0.2f) }, 1f, false, 0.1f);
        Assert.False(DoseChoiceRule.WorthwhileForImmunity(addictive, 0.1f, Fixtures.Weights));
    }

    [Fact]
    public void TheStrongerGainIsChosenAndAPainkillerIsNotADiseaseDrug()
    {
        var both = new[] { new DoseCandidate(Antibiotics, true), new DoseCandidate(Tyrox, true) };
        Assert.Equal(1, DoseChoiceRule.Choose(both, DoseNeed.Immunity, false, Fixtures.Weights));
        Assert.Equal(DoseRefusal.NoImmunityGain, DoseSafetyRule.Check(Fixtures.Morphine, DoseNeed.Immunity, 0f, false, Fixtures.Awake, Fixtures.Limits));
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(Tyrox, DoseNeed.Immunity, 0f, false, Fixtures.Awake, Fixtures.Limits));
        Assert.Equal(DoseRefusal.NoPainRelief, DoseSafetyRule.Check(Tyrox, DoseNeed.PainRelief, 0f, false, Fixtures.Awake, Fixtures.Limits));
    }
}

public class BleedingHoldTests
{
    static readonly DoseLimits Hold = new(0.25f, 0.3f, 0.15f, false, false, 0.3f);
    static PatientState Bleeding(float rate) => new(0.8f, 1f, 0f, true, rate);

    [Fact]
    public void APainkillerWaitsWhileThePatientBleedsHard()
    {
        Assert.Equal(DoseRefusal.BleedingHold, DoseSafetyRule.Check(Fixtures.Morphine, DoseNeed.PainRelief, 0f, false, Bleeding(0.9f), Hold));
        Assert.Equal(DoseRefusal.BleedingHold, DoseSafetyRule.Check(Fixtures.Morphine, DoseNeed.PainRelief, 0f, false, Bleeding(0.3f), Hold));
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(Fixtures.Morphine, DoseNeed.PainRelief, 0f, false, Bleeding(0.29f), Hold));
    }

    [Fact]
    public void ASedativeAndADiseaseDrugDoNotWait()
    {
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(Fixtures.Ketamine, DoseNeed.Sedation, 0f, false, Bleeding(0.9f), Hold));
        DoseEffect tyrox = DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0.5f) }, 0f, false, 0.1f);
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(tyrox, DoseNeed.Immunity, 0f, false, Bleeding(0.9f), Hold));
    }
}

public class SelfDoseRuleTests
{
    [Fact]
    public void ATasteForDrugsLowersTheThreshold()
    {
        Assert.Equal(0.4f, SelfDoseRule.Threshold(0.4f, 0, 0.1f, 0.2f), 4);
        Assert.Equal(0.4f, SelfDoseRule.Threshold(0.4f, -1, 0.1f, 0.2f), 4);
        Assert.Equal(0.3f, SelfDoseRule.Threshold(0.4f, 1, 0.1f, 0.2f), 4);
        Assert.Equal(0.2f, SelfDoseRule.Threshold(0.4f, 2, 0.1f, 0.2f), 4);
        Assert.Equal(0f, SelfDoseRule.Threshold(0.1f, 2, 0.1f, 0.2f));
    }

    [Fact]
    public void ThePolicyGatesItUnlessThePawnHasATasteForDrugs()
    {
        Assert.True(SelfDoseRule.PolicyAllows(true, false, 0));
        Assert.True(SelfDoseRule.PolicyAllows(false, true, 0));
        Assert.False(SelfDoseRule.PolicyAllows(false, false, 0));
        Assert.False(SelfDoseRule.PolicyAllows(false, false, -1));
        Assert.True(SelfDoseRule.PolicyAllows(false, false, 1));
    }

    [Fact]
    public void AGuestWithNoPolicyTakesMedicalDrugsOnly()
    {
        Assert.True(SelfDoseRule.NoPolicyAllows(true, false));
        Assert.False(SelfDoseRule.NoPolicyAllows(true, true));
        Assert.False(SelfDoseRule.NoPolicyAllows(false, false));
    }

    [Fact]
    public void TheNeedIsPainReliefNeverSedation()
    {
        Assert.Equal(DoseNeed.None, SelfDoseRule.Need(0.39f, 0.4f));
        Assert.Equal(DoseNeed.PainRelief, SelfDoseRule.Need(0.4f, 0.4f));
        Assert.Equal(DoseNeed.PainRelief, SelfDoseRule.Need(0.95f, 0.4f));
    }

    [Fact]
    public void ASedativeIsRefusedAndAPolicyRefusalIsNotADoctorsWord()
    {
        Assert.Equal(DoseRefusal.SedativeNotWanted, DoseSafetyRule.Check(Fixtures.Ketamine, DoseNeed.PainRelief, 0f, false, Fixtures.Awake, new DoseLimits(0.25f, 0.3f, 0.15f, true, false)));
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(Fixtures.Flake, DoseNeed.PainRelief, 0f, false, Fixtures.Awake, new DoseLimits(0.25f, 0.3f, 0.15f, true, false)));
    }
}

public class DoseSafetyRuleTests
{
    [Fact]
    public void EveryRefusalFiresInOrder()
    {
        DoseEffect morphine = Fixtures.Morphine;
        Assert.Equal(DoseRefusal.NoPainRelief, DoseSafetyRule.Check(DoseEffectRule.Combine(new[] { Fixtures.KetamineBuildupStage }, 0f, false, 0.1f), DoseNeed.PainRelief, 0f, false, Fixtures.Awake, Fixtures.Limits));
        Assert.Equal(DoseRefusal.SedativeNotWanted, DoseSafetyRule.Check(Fixtures.Ketamine, DoseNeed.PainRelief, 0f, false, Fixtures.Awake, Fixtures.Limits));
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(Fixtures.Ketamine, DoseNeed.Sedation, 0f, false, Fixtures.Awake, Fixtures.Limits));
        Assert.Equal(DoseRefusal.RecreationalOff, DoseSafetyRule.Check(Fixtures.Flake, DoseNeed.PainRelief, 0f, false, Fixtures.Awake, Fixtures.Limits));
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(Fixtures.Flake, DoseNeed.PainRelief, 0f, false, Fixtures.Awake, new DoseLimits(0.25f, 0.3f, 0.15f, true, false)));
        var refuses = new PatientState(0.8f, 1f, 0f, false);
        Assert.Equal(DoseRefusal.PatientWillNotTake, DoseSafetyRule.Check(morphine, DoseNeed.PainRelief, 0f, false, refuses, Fixtures.Limits));
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(morphine, DoseNeed.PainRelief, 0f, false, refuses, new DoseLimits(0.25f, 0.3f, 0.15f, false, true)));
        Assert.Equal(DoseRefusal.PatientWillNotTake, DoseSafetyRule.Check(Fixtures.Flake, DoseNeed.PainRelief, 0f, false, refuses, new DoseLimits(0.25f, 0.3f, 0.15f, true, true)));
        Assert.Equal(DoseRefusal.StillActive, DoseSafetyRule.Check(morphine, DoseNeed.PainRelief, 0.25f, false, Fixtures.Awake, Fixtures.Limits));
        Assert.Equal(DoseRefusal.Overdosed, DoseSafetyRule.Check(morphine, DoseNeed.PainRelief, 0f, false, new PatientState(0.8f, 1f, 0.3f, true), Fixtures.Limits));
        Assert.Equal(DoseRefusal.DangerousDose, DoseSafetyRule.Check(morphine, DoseNeed.PainRelief, 0f, true, Fixtures.Awake, Fixtures.Limits));
        Assert.Equal(DoseRefusal.Consciousness, DoseSafetyRule.Check(morphine, DoseNeed.PainRelief, 0f, false, new PatientState(0.5f, 1f, 0f, true), Fixtures.Limits));
        Assert.Equal(DoseRefusal.Breathing, DoseSafetyRule.Check(morphine, DoseNeed.PainRelief, 0f, false, new PatientState(0.8f, 0.3f, 0f, true), Fixtures.Limits));
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(morphine, DoseNeed.PainRelief, 0.2f, false, Fixtures.Awake, Fixtures.Limits));
    }

    [Fact]
    public void ASedativeCeilingIsNotAConsciousnessCost()
    {
        // The game's anesthetic sets a ceiling of 0.01 and never kills; a patient at 0.2 consciousness may still be sedated.
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(Fixtures.Ketamine, DoseNeed.Sedation, 0f, false, new PatientState(0.2f, 1f, 0f, true), Fixtures.Limits));
    }
}

public class DoseChoiceRuleTests
{
    [Fact]
    public void ReliefAsASideEffectDoesNotMakeAPainkiller()
    {
        Assert.Equal(0.225f, Fixtures.Epinephrine.PainRelief, 4);
        Assert.False(DoseChoiceRule.Worthwhile(Fixtures.Epinephrine, 0.3f, Fixtures.Weights));
        Assert.True(DoseChoiceRule.Worthwhile(Fixtures.Morphine, 0.3f, Fixtures.Weights));
        Assert.True(DoseChoiceRule.Worthwhile(Fixtures.Thornweed, 0.3f, Fixtures.Weights));
        Assert.True(DoseChoiceRule.Worthwhile(Fixtures.Ketamine, 0.3f, Fixtures.Weights));
        // Strong relief cannot buy off an item that is all addiction: luciferium-like, relief 0.2 at addictiveness 1.
        DoseEffect addictive = DoseEffectRule.Combine(new[] { new StageEffect(0.4f, 0f, 0f, 1f, 0f, 1f) }, 1.5f, false, 0.1f);
        Assert.False(DoseChoiceRule.Worthwhile(addictive, 0.3f, Fixtures.Weights));
    }

    [Fact]
    public void RecreationalDrugsAreTheLastResortHoweverCleanTheirScore()
    {
        Assert.True(DoseChoiceRule.Score(Fixtures.GoJuice, false, Fixtures.Weights) > DoseChoiceRule.Score(Fixtures.Morphine, false, Fixtures.Weights));
        var all = new[] { new DoseCandidate(Fixtures.GoJuice, true), new DoseCandidate(Fixtures.Morphine, true), new DoseCandidate(Fixtures.Ketamine, true) };
        Assert.Equal(1, DoseChoiceRule.Choose(all, DoseNeed.PainRelief, false, Fixtures.Weights));
        var noMorphine = new[] { new DoseCandidate(Fixtures.GoJuice, true), new DoseCandidate(Fixtures.Morphine, false), new DoseCandidate(Fixtures.Ketamine, true) };
        Assert.Equal(2, DoseChoiceRule.Choose(noMorphine, DoseNeed.Sedation, false, Fixtures.Weights));
        Assert.Equal(0, DoseChoiceRule.Choose(noMorphine, DoseNeed.PainRelief, false, Fixtures.Weights));
    }

    [Fact]
    public void MorphineBeatsThornweedWhichBeatsNothingAndKetamineWaitsForSedation()
    {
        Assert.Equal(0.5f, DoseChoiceRule.Score(Fixtures.Morphine, false, Fixtures.Weights), 4);
        Assert.Equal(0.4f, DoseChoiceRule.Score(Fixtures.Thornweed, false, Fixtures.Weights), 4);
        var all = new[] { new DoseCandidate(Fixtures.Thornweed, true), new DoseCandidate(Fixtures.Morphine, true), new DoseCandidate(Fixtures.Ketamine, true) };
        Assert.Equal(1, DoseChoiceRule.Choose(all, DoseNeed.PainRelief, false, Fixtures.Weights));
        Assert.Equal(1, DoseChoiceRule.Choose(all, DoseNeed.Sedation, false, Fixtures.Weights));
        var noMorphine = new[] { new DoseCandidate(Fixtures.Thornweed, true), new DoseCandidate(Fixtures.Morphine, false), new DoseCandidate(Fixtures.Ketamine, true) };
        Assert.Equal(0, DoseChoiceRule.Choose(noMorphine, DoseNeed.Sedation, false, Fixtures.Weights));
        var onlySedative = new[] { new DoseCandidate(Fixtures.Thornweed, false), new DoseCandidate(Fixtures.Morphine, false), new DoseCandidate(Fixtures.Ketamine, true) };
        Assert.Equal(-1, DoseChoiceRule.Choose(onlySedative, DoseNeed.PainRelief, false, Fixtures.Weights));
        Assert.Equal(2, DoseChoiceRule.Choose(onlySedative, DoseNeed.Sedation, false, Fixtures.Weights));
    }

    [Fact]
    public void BleedingPatientsWeighABleedIncreaseInFull()
    {
        DoseEffect bleeder = DoseEffectRule.Combine(new[] { new StageEffect(0.5f, 0f, 0f, 1f, 0f, 1.6f) }, 0f, false, 0.1f);
        Assert.Equal(0.5f - 0.15f, DoseChoiceRule.Score(bleeder, false, Fixtures.Weights), 4);
        Assert.Equal(0.5f - 0.6f, DoseChoiceRule.Score(bleeder, true, Fixtures.Weights), 4);
        Assert.Equal(-1, DoseChoiceRule.Choose(new[] { new DoseCandidate(bleeder, true) }, DoseNeed.PainRelief, true, Fixtures.Weights));
    }

    [Fact]
    public void ADisallowedCandidateIsNeverChosen()
    {
        Assert.Equal(-1, DoseChoiceRule.Choose(new[] { new DoseCandidate(Fixtures.Morphine, false) }, DoseNeed.Sedation, false, Fixtures.Weights));
    }
}
