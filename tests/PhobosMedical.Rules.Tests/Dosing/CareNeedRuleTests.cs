using Phobos.Dosing.Rules;
using Xunit;

namespace Phobos.Dosing.Rules.Tests;

/// <summary>
/// The care needs of D'thorga Medical 0.28.0 (docs/development/medical-care-needs.md): what they add to the safety rule, the reserve,
/// who prevention is for, and how a cure or a preventive is scored. Stage figures are synthetic, after the owner's mods as read on
/// 26 Nov 2026: an antiserum whose only outcome removes a condition gives no stage at all; a vaccine's stage changes nothing but
/// immunity; an emetic's stage brings a vomit every 0.125 days.
/// </summary>
static class CareFixtures
{
    /// <summary>The dosing rules' weights, with a vomit a day counting a quarter.</summary>
    public static readonly DoseWeights Weights = new(0.5f, 0.5f, 1f, 0.5f, 0.25f, 0.25f);

    /// <summary>An item with no stage of its own: nothing but the cure.</summary>
    public static DoseEffect Antiserum => DoseEffectRule.Combine(new StageEffect[0], 0f, false, 0.1f);
    /// <summary>A vaccine: a stage that touches nothing the index scores.</summary>
    public static DoseEffect Vaccine => DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f) }, 0f, false, 0.1f);
    /// <summary>An emetic that also makes immune: eight vomits a day.</summary>
    public static DoseEffect Emetic => DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0f, 0.125f) }, 0f, false, 0.1f);
    /// <summary>A cure that costs 0.3 consciousness and a vomit every half day.</summary>
    public static DoseEffect HarshCure => DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, -0.3f, 1f, 0f, 1f, 0f, 0.5f) }, 0f, false, 0.1f);
}

public class CareSafetyRuleTests
{
    [Fact]
    public void ADependencyThatKillsIsNeverBegunWhateverTheNeed()
    {
        Assert.Equal(DoseRefusal.LethalDependency, CareSafetyRule.Check(CareSeriousness.LifeThreatening, 1f, true, false));
        Assert.Equal(DoseRefusal.None, CareSafetyRule.Check(CareSeriousness.LifeThreatening, 1f, false, false));
    }

    [Fact]
    public void AnAddictiveItemNeedsASeriousNeed()
    {
        Assert.Equal(DoseRefusal.AddictiveForMinorNeed, CareSafetyRule.Check(CareSeriousness.Minor, 0.02f, false, false));
        Assert.Equal(DoseRefusal.None, CareSafetyRule.Check(CareSeriousness.Minor, 0f, false, false));
        Assert.Equal(DoseRefusal.None, CareSafetyRule.Check(CareSeriousness.Serious, 0.02f, false, false));
        Assert.Equal(DoseRefusal.None, CareSafetyRule.Check(CareSeriousness.LifeThreatening, 0.02f, false, false));
    }

    [Fact]
    public void ARareItemIsKeptForSavingALife()
    {
        Assert.Equal(DoseRefusal.RareItemKept, CareSafetyRule.Check(CareSeriousness.Minor, 0f, false, true));
        Assert.Equal(DoseRefusal.RareItemKept, CareSafetyRule.Check(CareSeriousness.Serious, 0f, false, true));
        Assert.Equal(DoseRefusal.None, CareSafetyRule.Check(CareSeriousness.LifeThreatening, 0f, false, true));
    }

    [Fact]
    public void RareIsWorthTheMarkOrMoreAndAMarkOfZeroSwitchesItOff()
    {
        Assert.False(CareSafetyRule.Rare(299f, 300f));
        Assert.True(CareSafetyRule.Rare(300f, 300f));
        Assert.False(CareSafetyRule.Rare(5000f, 0f));
    }

    [Fact]
    public void ACureIsLifeThreateningWhenTheConditionCanKill()
    {
        Assert.Equal(CareSeriousness.LifeThreatening, CareSafetyRule.CureSeriousness(true));
        Assert.Equal(CareSeriousness.Serious, CareSafetyRule.CureSeriousness(false));
    }

    [Fact]
    public void TheSwitchSaysWhoMayAct()
    {
        Assert.False(CareModeRule.DoctorsMay(CareMode.Off));
        Assert.True(CareModeRule.DoctorsMay(CareMode.DoctorsOnly));
        Assert.False(CareModeRule.SelfMay(CareMode.DoctorsOnly));
        Assert.True(CareModeRule.SelfMay(CareMode.DoctorsAndSelf));
    }
}

public class StockReserveRuleTests
{
    [Fact]
    public void TheReserveIsNeverTakenBelow()
    {
        Assert.True(StockReserveRule.MayTake(6, 5));
        Assert.False(StockReserveRule.MayTake(5, 5));
        Assert.False(StockReserveRule.MayTake(2, 5));
    }

    [Fact]
    public void AReserveOfZeroKeepsNothingButStillNeedsOneInStock()
    {
        Assert.True(StockReserveRule.MayTake(1, 0));
        Assert.False(StockReserveRule.MayTake(0, 0));
        Assert.True(StockReserveRule.MayTake(1, -3));
    }
}

public class PreventionRuleTests
{
    [Fact]
    public void PreventionIsForSomeoneTheDiseaseCouldStillStrike()
    {
        Assert.True(PreventionRule.Unprotected(1f, 0.5f));
        Assert.True(PreventionRule.Unprotected(0.5f, 0.5f));
        // Immunity left over from an earlier bout: under half the risk, so left alone.
        Assert.False(PreventionRule.Unprotected(0.4f, 0.5f));
        // Immune already, or has the disease: the game answers zero.
        Assert.False(PreventionRule.Unprotected(0f, 0.5f));
        Assert.False(PreventionRule.Unprotected(0f, 0f));
    }

    [Fact]
    public void ThePolicyDecidesOnlyWhereItCanSpeak()
    {
        // A plain medicine has no tick in a drug policy: the need's own setting is the gate.
        Assert.True(SelfDoseRule.PreventionPolicyAllows(false, false));
        // Something addictive or taken for pleasure: the policy decides, as for a painkiller.
        Assert.False(SelfDoseRule.PreventionPolicyAllows(true, false));
        Assert.True(SelfDoseRule.PreventionPolicyAllows(true, true));
    }
}

public class CareScoreTests
{
    [Fact]
    public void VomitsAddUpPerDayAndAStageWithNoneAddsNothing()
    {
        Assert.Equal(0f, CareFixtures.Vaccine.VomitsPerDay);
        Assert.Equal(8f, CareFixtures.Emetic.VomitsPerDay, 4);
        DoseEffect two = DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0f, 0.5f), new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0f, -1f) }, 0f, false, 0.1f);
        Assert.Equal(2f, two.VomitsPerDay, 4);
    }

    [Fact]
    public void ACureOrAPreventiveIsWorthItsWholeBenefitLessItsCosts()
    {
        Assert.Equal(1f, DoseChoiceRule.Score(CareFixtures.Antiserum, DoseNeed.Cure, false, CareFixtures.Weights), 4);
        Assert.Equal(1f, DoseChoiceRule.Score(CareFixtures.Vaccine, DoseNeed.Prevention, false, CareFixtures.Weights), 4);
        // 0.5 x 0.3 consciousness, and two vomits a day at a quarter each.
        Assert.Equal(1f - 0.15f - 0.5f, DoseChoiceRule.Score(CareFixtures.HarshCure, DoseNeed.Cure, false, CareFixtures.Weights), 4);
    }

    [Fact]
    public void AnEmeticIsOnNeitherList()
    {
        Assert.Equal(0f, DoseChoiceRule.Score(CareFixtures.Emetic, DoseNeed.Prevention, false, CareFixtures.Weights), 4);
        Assert.False(DoseChoiceRule.WorthwhileForCare(CareFixtures.Emetic, DoseNeed.Prevention, CareFixtures.Weights));
        Assert.True(DoseChoiceRule.WorthwhileForCare(CareFixtures.Vaccine, DoseNeed.Prevention, CareFixtures.Weights));
        Assert.True(DoseChoiceRule.WorthwhileForCare(CareFixtures.Antiserum, DoseNeed.Cure, CareFixtures.Weights));
    }

    [Fact]
    public void ASedativeOrARecreationalDrugIsNeverACureOrAPreventive()
    {
        Assert.False(DoseChoiceRule.WorthwhileForCare(Fixtures.Ketamine, DoseNeed.Cure, CareFixtures.Weights));
        Assert.False(DoseChoiceRule.WorthwhileForCare(Fixtures.Flake, DoseNeed.Prevention, CareFixtures.Weights));
    }

    [Fact]
    public void PainAndImmunityScoresDoNotCountVomiting()
    {
        // The pain score is as it was before Medical 0.28.0, whatever the vomit weight: a painkiller that makes the patient sick keeps its place.
        DoseEffect sickening = DoseEffectRule.Combine(new[] { new StageEffect(0.2f, 0f, -0.4f, 1f, -0.2f, 1f, 0f, 0.5f) }, 0f, false, 0.1f);
        Assert.Equal(DoseChoiceRule.Score(Fixtures.Morphine, false, Fixtures.Weights), DoseChoiceRule.Score(sickening, false, CareFixtures.Weights), 4);
        DoseEffect gain = DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0.2f, 0.5f) }, 0f, false, 0.1f);
        Assert.Equal(0.2f, DoseChoiceRule.Score(gain, DoseNeed.Immunity, false, CareFixtures.Weights), 4);
    }

    [Fact]
    public void TheGentlerOfTwoCuresIsChosen()
    {
        var both = new[] { new DoseCandidate(CareFixtures.HarshCure, true), new DoseCandidate(CareFixtures.Antiserum, true) };
        Assert.Equal(1, DoseChoiceRule.Choose(both, DoseNeed.Cure, false, CareFixtures.Weights));
        var onlyHarsh = new[] { new DoseCandidate(CareFixtures.HarshCure, true), new DoseCandidate(CareFixtures.Antiserum, false) };
        Assert.Equal(0, DoseChoiceRule.Choose(onlyHarsh, DoseNeed.Cure, false, CareFixtures.Weights));
    }
}

public class CareSafetyOrderTests
{
    [Fact]
    public void ACureNeedNotRelievePainAndDoesNotWaitForBleeding()
    {
        var limits = new DoseLimits(0.25f, 0.3f, 0.15f, false, false, 0.3f);
        var bleeding = new PatientState(0.8f, 1f, 0f, true, 0.6f);
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(CareFixtures.Antiserum, DoseNeed.Cure, 0f, false, bleeding, limits));
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(CareFixtures.Vaccine, DoseNeed.Prevention, 0f, false, bleeding, limits));
        // The same item asked for as a painkiller is still refused, as before.
        Assert.Equal(DoseRefusal.NoPainRelief, DoseSafetyRule.Check(CareFixtures.Antiserum, DoseNeed.PainRelief, 0f, false, bleeding, limits));
    }

    [Fact]
    public void TheDoseItselfIsStillCheckedForACure()
    {
        var limits = new DoseLimits(0.25f, 0.3f, 0.15f, false, false, 0.3f);
        Assert.Equal(DoseRefusal.PatientWillNotTake, DoseSafetyRule.Check(CareFixtures.Antiserum, DoseNeed.Cure, 0f, false, new PatientState(0.8f, 1f, 0f, false), limits));
        Assert.Equal(DoseRefusal.Overdosed, DoseSafetyRule.Check(CareFixtures.Antiserum, DoseNeed.Cure, 0f, false, new PatientState(0.8f, 1f, 0.5f, true), limits));
        Assert.Equal(DoseRefusal.DangerousDose, DoseSafetyRule.Check(CareFixtures.Antiserum, DoseNeed.Cure, 0f, true, new PatientState(0.8f, 1f, 0f, true), limits));
        Assert.Equal(DoseRefusal.Consciousness, DoseSafetyRule.Check(CareFixtures.HarshCure, DoseNeed.Cure, 0f, false, new PatientState(0.4f, 1f, 0f, true), limits));
        Assert.Equal(DoseRefusal.StillActive, DoseSafetyRule.Check(CareFixtures.Vaccine, DoseNeed.Prevention, 0.5f, false, new PatientState(0.8f, 1f, 0f, true), limits));
    }
}

/// <summary>
/// The breathing rescue of D'thorga Medical 0.29.0: who it is for, what counts as a help to breathing, and the one place the safety
/// rule differs for it. The smelling salts stage is Medicines+' as read on 26 Nov 2026: breathing +0.2, consciousness +0.1, pain x0.9.
/// </summary>
public class BreathingRescueTests
{
    static readonly StageEffect SaltsStage = new(0.9f, 0f, 0.1f, 1f, 0.2f, 1f);
    static DoseEffect Salts => DoseEffectRule.Combine(new[] { SaltsStage }, 0f, false, 0.1f);
    static readonly DoseLimits Limits = new(0.25f, 0.3f, 0.15f, false, false, 0.3f);

    [Fact]
    public void ItIsForSomeoneAtTheLevelOrUnderWhoseBreathingIsNotCapped()
    {
        Assert.True(BreathingRescueRule.NeedsRescue(0.3f, 0.3f, 1f));
        Assert.True(BreathingRescueRule.NeedsRescue(0.05f, 0.3f, 1f));
        Assert.False(BreathingRescueRule.NeedsRescue(0.31f, 0.3f, 1f));
        // A ceiling at or under the level: a stimulant adds to breathing and cannot lift it, so it would be spent for nothing.
        Assert.False(BreathingRescueRule.NeedsRescue(0.2f, 0.3f, 0.2f));
        Assert.False(BreathingRescueRule.NeedsRescue(0.3f, 0.3f, 0.3f));
        // A ceiling above the level leaves room for the gain to count.
        Assert.True(BreathingRescueRule.NeedsRescue(0.2f, 0.3f, 0.5f));
    }

    [Fact]
    public void BreathingGainedAndBreathingCostAreKeptApart()
    {
        Assert.Equal(0.2f, Salts.BreathingGain, 4);
        Assert.Equal(0f, Salts.BreathingCost);
        DoseEffect mixed = DoseEffectRule.Combine(new[] { SaltsStage, new StageEffect(1f, 0f, 0f, 1f, -0.3f, 1f) }, 0f, false, 0.1f);
        Assert.Equal(0.2f, mixed.BreathingGain, 4);
        Assert.Equal(0.3f, mixed.BreathingCost, 4);
        Assert.Equal(0f, Fixtures.Morphine.BreathingGain);
    }

    [Fact]
    public void AHelpToBreathingNeedsARealGainLeftAfterItsCosts()
    {
        Assert.True(DoseChoiceRule.WorthwhileForBreathing(Salts, 0.1f, CareFixtures.Weights));
        DoseEffect slight = DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0.05f, 1f) }, 0f, false, 0.1f);
        Assert.False(DoseChoiceRule.WorthwhileForBreathing(slight, 0.1f, CareFixtures.Weights));
        DoseEffect givesAndTakes = DoseEffectRule.Combine(new[] { SaltsStage, new StageEffect(1f, 0f, 0f, 1f, -0.15f, 1f) }, 0f, false, 0.1f);
        Assert.False(DoseChoiceRule.WorthwhileForBreathing(givesAndTakes, 0.1f, CareFixtures.Weights));
        DoseEffect recreational = DoseEffectRule.Combine(new[] { SaltsStage }, 0.05f, true, 0.1f);
        Assert.False(DoseChoiceRule.WorthwhileForBreathing(recreational, 0.1f, CareFixtures.Weights));
        Assert.False(DoseChoiceRule.WorthwhileForBreathing(Fixtures.Morphine, 0.1f, CareFixtures.Weights));
    }

    [Fact]
    public void TheScoreIsTheGainLessTheCosts()
    {
        Assert.Equal(0.2f, DoseChoiceRule.Score(Salts, DoseNeed.Breathing, false, CareFixtures.Weights), 4);
        // Half the consciousness cost and a quarter for a vomit a day count against it.
        DoseEffect harsh = DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, -0.2f, 1f, 0.4f, 1f, 0f, 1f) }, 0f, false, 0.1f);
        Assert.Equal(0.4f - 0.1f - 0.25f, DoseChoiceRule.Score(harsh, DoseNeed.Breathing, false, CareFixtures.Weights), 4);
        var both = new[] { new DoseCandidate(harsh, true), new DoseCandidate(Salts, true) };
        Assert.Equal(1, DoseChoiceRule.Choose(both, DoseNeed.Breathing, false, CareFixtures.Weights));
    }

    [Fact]
    public void TheBreathingFloorIsNotAskedOfARescueAndStillIsOfEverythingElse()
    {
        var suffocating = new PatientState(0.6f, 0.1f, 0f, true);
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(Salts, DoseNeed.Breathing, 0f, false, suffocating, Limits));
        // Asked for as a painkiller, with no breathing cost at all, the same patient is still under the floor: as before 0.29.0.
        Assert.Equal(DoseRefusal.Breathing, DoseSafetyRule.Check(Fixtures.Thornweed, DoseNeed.PainRelief, 0f, false, suffocating, Limits));
    }

    [Fact]
    public void AnItemThatTakesTheBreathingItGivesIsRefusedAndTheOtherChecksStand()
    {
        var suffocating = new PatientState(0.6f, 0.1f, 0f, true);
        DoseEffect givesAndTakes = DoseEffectRule.Combine(new[] { SaltsStage, new StageEffect(1f, 0f, 0f, 1f, -0.2f, 1f) }, 0f, false, 0.1f);
        Assert.Equal(DoseRefusal.NoBreathingGain, DoseSafetyRule.Check(givesAndTakes, DoseNeed.Breathing, 0f, false, suffocating, Limits));
        Assert.Equal(DoseRefusal.NoBreathingGain, DoseSafetyRule.Check(Fixtures.Thornweed, DoseNeed.Breathing, 0f, false, suffocating, Limits));
        Assert.Equal(DoseRefusal.StillActive, DoseSafetyRule.Check(Salts, DoseNeed.Breathing, 0.5f, false, suffocating, Limits));
        Assert.Equal(DoseRefusal.Overdosed, DoseSafetyRule.Check(Salts, DoseNeed.Breathing, 0f, false, new PatientState(0.6f, 0.1f, 0.5f, true), Limits));
        // A hard bleed holds a painkiller, never a breath.
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(Salts, DoseNeed.Breathing, 0f, false, new PatientState(0.6f, 0.1f, 0f, true, 0.6f), Limits));
    }
}

/// <summary>
/// Protection while exposed, D'thorga Medical 0.30.0. AntiTox's stage is Medicines+' as read on 26 Nov 2026 (toxic resistance +0.5,
/// toxic environment resistance +0.6); vacsop's is Complementary Odyssey's (vacuum resistance +0.5).
/// </summary>
public class ExposureTests
{
    static readonly StageEffect AntiToxStage = new(1f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0.5f, 0.6f);
    static readonly StageEffect VacsopStage = new(1f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 0.5f);
    static DoseEffect AntiTox => DoseEffectRule.Combine(new[] { AntiToxStage }, 0f, false, 0.1f);
    static DoseEffect Vacsop => DoseEffectRule.Combine(new[] { VacsopStage }, 0f, false, 0.1f);
    static readonly DoseLimits Limits = new(0.25f, 0.3f, 0.4f, false, false, 0.3f);
    static readonly PatientState Working = new(1f, 1f, 0f, true);

    [Fact]
    public void TheTwoToxicResistancesMultiplyAsTheGameAppliesThem()
    {
        // The game takes in (1 - 0.5) x (1 - 0.6) = 0.2 of the toxins: 80% kept out.
        Assert.Equal(0.8f, AntiTox.ToxicCut, 4);
        Assert.Equal(0.2f, ExposureRule.IntakeFactor(0.5f, 0.6f), 4);
        Assert.Equal(0f, ExposureRule.IntakeFactor(1f, 0f), 4);
        Assert.Equal(1f, ExposureRule.IntakeFactor(-0.2f, 0f), 4);
        Assert.Equal(0f, Fixtures.Morphine.ToxicCut);
    }

    [Fact]
    public void ResistancesAddAcrossConditionsAndAreHeldToOne()
    {
        DoseEffect two = DoseEffectRule.Combine(new[] { AntiToxStage, new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0.7f, 0f) }, 0f, false, 0.1f);
        // Toxic resistance 0.5 + 0.7 is held at 1: nothing gets through.
        Assert.Equal(1f, two.ToxicCut, 4);
        Assert.Equal(0.5f, Vacsop.VacuumGain, 4);
    }

    [Fact]
    public void ExposureIsUnderWayOnlyWithBuildupBegunASourceNowAndSomethingGettingThrough()
    {
        Assert.True(ExposureRule.ToxicUnderWay(0.04f, 0.04f, true, 1f));
        Assert.False(ExposureRule.ToxicUnderWay(0.03f, 0.04f, true, 1f));
        // Out of the fallout, off the polluted ground: the buildup alone is not exposure.
        Assert.False(ExposureRule.ToxicUnderWay(0.5f, 0.04f, false, 1f));
        // Fully kept out already (a hazard suit): nothing to add.
        Assert.False(ExposureRule.ToxicUnderWay(0.5f, 0.04f, true, 0f));
    }

    [Fact]
    public void CaughtInVacuumIsWhereTheGameHarmsAndResistanceFallsShort()
    {
        Assert.True(ExposureRule.CaughtInVacuum(1f, 0f));
        Assert.True(ExposureRule.CaughtInVacuum(0.5f, 0.9f));
        Assert.False(ExposureRule.CaughtInVacuum(0.49f, 0f));
        Assert.False(ExposureRule.CaughtInVacuum(1f, 1f));
    }

    [Fact]
    public void ProtectionIsWorthTheShareItKeepsOutLessItsCosts()
    {
        Assert.Equal(0.8f, DoseChoiceRule.Score(AntiTox, DoseNeed.Toxic, false, CareFixtures.Weights), 4);
        Assert.Equal(0.5f, DoseChoiceRule.Score(Vacsop, DoseNeed.Vacuum, false, CareFixtures.Weights), 4);
        Assert.True(DoseChoiceRule.WorthwhileForExposure(AntiTox, DoseNeed.Toxic, 0.25f, CareFixtures.Weights));
        Assert.True(DoseChoiceRule.WorthwhileForExposure(Vacsop, DoseNeed.Vacuum, 0.25f, CareFixtures.Weights));
        // Each is protection against its own hazard only.
        Assert.False(DoseChoiceRule.WorthwhileForExposure(AntiTox, DoseNeed.Vacuum, 0.25f, CareFixtures.Weights));
        Assert.False(DoseChoiceRule.WorthwhileForExposure(Vacsop, DoseNeed.Toxic, 0.25f, CareFixtures.Weights));
        Assert.False(DoseChoiceRule.WorthwhileForExposure(Fixtures.Morphine, DoseNeed.Toxic, 0.25f, CareFixtures.Weights));
        // A minimum of zero would index everything: refused.
        Assert.False(DoseChoiceRule.WorthwhileForExposure(AntiTox, DoseNeed.Toxic, 0f, CareFixtures.Weights));
    }

    [Fact]
    public void ARecreationalDrugIsNeverProtection()
    {
        DoseEffect recreational = DoseEffectRule.Combine(new[] { AntiToxStage }, 0.05f, true, 0.1f);
        Assert.False(DoseChoiceRule.WorthwhileForExposure(recreational, DoseNeed.Toxic, 0.25f, CareFixtures.Weights));
    }

    [Fact]
    public void TheSafetyRuleAsksForProtectionAndKeepsItsOtherChecks()
    {
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(AntiTox, DoseNeed.Toxic, 0f, false, Working, Limits));
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(Vacsop, DoseNeed.Vacuum, 0f, false, Working, Limits));
        Assert.Equal(DoseRefusal.NoProtection, DoseSafetyRule.Check(Vacsop, DoseNeed.Toxic, 0f, false, Working, Limits));
        Assert.Equal(DoseRefusal.NoProtection, DoseSafetyRule.Check(Fixtures.Thornweed, DoseNeed.Vacuum, 0f, false, Working, Limits));
        // A dose still working is not doubled: AntiTox is taken again only as it wears off.
        Assert.Equal(DoseRefusal.StillActive, DoseSafetyRule.Check(AntiTox, DoseNeed.Toxic, 0.5f, false, Working, Limits));
        // A hard bleed holds a painkiller, never protection.
        Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(AntiTox, DoseNeed.Toxic, 0f, false, new PatientState(1f, 1f, 0f, true, 0.6f), Limits));
        Assert.Equal(DoseRefusal.PatientWillNotTake, DoseSafetyRule.Check(AntiTox, DoseNeed.Toxic, 0f, false, new PatientState(1f, 1f, 0f, false), Limits));
    }

    [Fact]
    public void ProtectionIsAMinorNeedSoNothingAddictiveAndNothingRare()
    {
        Assert.Equal(DoseRefusal.AddictiveForMinorNeed, CareSafetyRule.Check(CareSeriousness.Minor, 0.02f, false, false));
        Assert.Equal(DoseRefusal.RareItemKept, CareSafetyRule.Check(CareSeriousness.Minor, 0f, false, true));
        // Caught in vacuum is life-threatening: a rare item may be used there.
        Assert.Equal(DoseRefusal.None, CareSafetyRule.Check(CareSeriousness.LifeThreatening, 0f, false, true));
    }
}

/// <summary>
/// The long-term needs, D'thorga Medical 0.31.0. Figures after the owner's mods as read on 26 Nov 2026: The Dead Man's Switch's biogel
/// holds 100 and Neuroglue adds 25; the addiction remover costs 0.3 consciousness and a vomit every half day; addictol is 5% addictive.
/// </summary>
public class LongTermTests
{
    static DoseEffect Remover => DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, -0.3f, 1f, 0f, 1f, 0f, 0.5f) }, 0f, false, 0.1f);
    static DoseEffect Addictol => DoseEffectRule.Combine(new[] { new StageEffect(1.1f, 0f, 0f, 1f, 0f, 1f) }, 0.05f, false, 0.1f);
    static readonly DoseLimits Limits = new(0.25f, 0.3f, 0.4f, false, false, 0.3f);

    [Fact]
    public void StrainIsMoodBelowTheMajorThresholdOutsideABreak()
    {
        Assert.True(LongTermRule.Strained(0.2f, 0.25f, false));
        Assert.False(LongTermRule.Strained(0.25f, 0.25f, false));
        Assert.False(LongTermRule.Strained(0.1f, 0.25f, true));
    }

    [Fact]
    public void AnAddictionIsTreatedOnlyWhenBeingQuitAndNeverWhenItKills()
    {
        Assert.True(LongTermRule.QuitWanted(false, false));
        Assert.False(LongTermRule.QuitWanted(true, false));
        Assert.False(LongTermRule.QuitWanted(false, true));
    }

    [Fact]
    public void AnOldWoundMattersWhenItHurtsOrWeakens()
    {
        Assert.True(LongTermRule.OldWoundMatters(0.05f, false, 1f));
        Assert.True(LongTermRule.OldWoundMatters(0f, true, 1f));
        Assert.True(LongTermRule.OldWoundMatters(0f, false, 0.8f));
        Assert.False(LongTermRule.OldWoundMatters(0f, false, 1f));
    }

    [Fact]
    public void AReservoirIsRefilledOnceAWholeDoseFits()
    {
        Assert.True(LongTermRule.ReservoirWantsRefill(75f, 100f, 25f));
        Assert.False(LongTermRule.ReservoirWantsRefill(76f, 100f, 25f));
        Assert.True(LongTermRule.ReservoirWantsRefill(0f, 100f, 25f));
        Assert.False(LongTermRule.ReservoirWantsRefill(10f, 100f, 0f));
    }

    [Fact]
    public void EveryLongTermNeedIsMatchedByTheIndexAndAsksNoPainRelief()
    {
        foreach (DoseNeed need in new[] { DoseNeed.MentalStrain, DoseNeed.Addiction, DoseNeed.OldWounds, DoseNeed.MissingPart, DoseNeed.Reservoir })
        {
            Assert.True(DoseNeedRule.MatchedByIndex(need));
            Assert.Equal(DoseRefusal.None, DoseSafetyRule.Check(CareFixtures.Antiserum, need, 0f, false, new PatientState(1f, 1f, 0f, true), Limits));
        }
        Assert.False(DoseNeedRule.MatchedByIndex(DoseNeed.PainRelief));
        Assert.Equal(DoseRefusal.NoPainRelief, DoseSafetyRule.Check(CareFixtures.Antiserum, DoseNeed.PainRelief, 0f, false, new PatientState(1f, 1f, 0f, true), Limits));
    }

    [Fact]
    public void TheGentlerAddictionTreatmentIsChosenFirst()
    {
        // The remover: 1 - 0.5 x 0.3 - 2 vomits a day x 0.25 = 0.35. Addictol: 1 - 0.5 x 0.05 = 0.975.
        Assert.Equal(0.35f, DoseChoiceRule.Score(Remover, DoseNeed.Addiction, false, CareFixtures.Weights), 4);
        Assert.Equal(0.975f, DoseChoiceRule.Score(Addictol, DoseNeed.Addiction, false, CareFixtures.Weights), 4);
        var both = new[] { new DoseCandidate(Remover, true), new DoseCandidate(Addictol, true) };
        Assert.Equal(1, DoseChoiceRule.Choose(both, DoseNeed.Addiction, false, CareFixtures.Weights));
    }

    [Fact]
    public void AnAddictiveTreatmentNeedsASeriousNeedAndEveryLongTermNeedIsOne()
    {
        // The long-term needs are serious in their Defs: addictol's 5% is allowed there, never for a minor need.
        Assert.Equal(DoseRefusal.None, CareSafetyRule.Check(CareSeriousness.Serious, 0.05f, false, false));
        Assert.Equal(DoseRefusal.AddictiveForMinorNeed, CareSafetyRule.Check(CareSeriousness.Minor, 0.05f, false, false));
        // Luciferium heals old wounds too, and is refused for the dependency it begins.
        Assert.Equal(DoseRefusal.LethalDependency, CareSafetyRule.Check(CareSeriousness.Serious, 0.05f, true, false));
    }
}
