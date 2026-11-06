using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class BreathingRuleTests
{
    [Fact]
    public void WithNoOxygenTheSurroundingsDecide() => Assert.Equal(5.3f, BreathingRule.ClearanceHalfLifeHours(5.3f, null));

    [Fact]
    public void OxygenShortensIt() => Assert.Equal(1.25f, BreathingRule.ClearanceHalfLifeHours(5.3f, 1.25f));

    [Fact]
    public void OxygenNeverSlowsClearanceDown() => Assert.Equal(0.5f, BreathingRule.ClearanceHalfLifeHours(0.5f, 1.25f));

    [Fact]
    public void ABadFigureIsIgnored() => Assert.Equal(5.3f, BreathingRule.ClearanceHalfLifeHours(5.3f, 0f));

    [Fact]
    public void TheBetterOfTwoTreatmentsWins()
    {
        Assert.Equal(0.4f, BreathingRule.Shortest(1.25f, 0.4f));
        Assert.Equal(0.4f, BreathingRule.Shortest(0.4f, 1.25f));
        Assert.Equal(1.25f, BreathingRule.Shortest(null, 1.25f));
    }
}

public class GasMixRuleTests
{
    [Fact]
    public void OpeningCoefficientsComeFromTheGamesDoorAndVentFigures()
    {
        // Open door: rate 1 every 34 ticks; closed: every 375; vent: 14 every 250. Scaled to one 60-tick pass.
        Assert.Equal(60f / 34f, GasMixRule.OpeningCoefficient(1f, 34), 4);
        Assert.Equal(60f / 375f, GasMixRule.OpeningCoefficient(1f, 375), 4);
        Assert.Equal(14f * 60f / 250f, GasMixRule.OpeningCoefficient(14f, 250), 4);
        Assert.True(GasMixRule.OpeningCoefficient(1f, 375) < GasMixRule.OpeningCoefficient(1f, 34));
    }

    [Fact]
    public void ExchangeConservesTheAmountAndMovesBothRoomsTowardTheMean()
    {
        // Room A (10 cells) holds 0.8, room B (30 cells) is empty. One open door.
        float moved = GasMixRule.Exchange(0.8f, 10, 0f, 30, GasMixRule.OpeningCoefficient(1f, 34));
        Assert.True(moved < 0f); // gas leaves A
        float a = 0.8f + moved / 10;
        float b = 0f - moved / 30;
        Assert.Equal(0.8f * 10, a * 10 + b * 30, 4); // the same amount in cells of gas
        Assert.True(a < 0.8f && b > 0f);
    }

    [Fact]
    public void ExchangeNeverOvershootsTheSharedMean()
    {
        // A huge coefficient still only reaches the amount-weighted mean, so repeated passes converge.
        float moved = GasMixRule.Exchange(1f, 10, 0f, 10, 1000f);
        float a = 1f + moved / 10;
        float b = 0f - moved / 10;
        Assert.Equal(0.5f, a, 4);
        Assert.Equal(0.5f, b, 4);
    }

    [Fact]
    public void AClosedDoorMovesLessThanAnOpenOne()
    {
        float open = GasMixRule.Exchange(1f, 100, 0f, 100, GasMixRule.OpeningCoefficient(1f, 34));
        float closed = GasMixRule.Exchange(1f, 100, 0f, 100, GasMixRule.OpeningCoefficient(1f, 375));
        Assert.True(-closed < -open);
        Assert.True(closed < 0f);
    }

    [Fact]
    public void VentingHalvesAtTheHalfLife()
    {
        // 600-tick half-life: ten passes of 60 ticks leave half.
        float factor = GasMixRule.DecayFactor(600f);
        float left = 1f;
        for (int i = 0; i < 10; i++) left *= factor;
        Assert.Equal(0.5f, left, 3);
        Assert.Equal(1f, GasMixRule.DecayFactor(0f));
    }

    [Fact]
    public void OpenRoofVentsOnlyTheOpenShare()
    {
        // Half the roof is gone: half the gas fades at the open-roof rate, the rest is held.
        float factor = GasMixRule.OpenRoofFactor(10, 20, 600f);
        float openShareLeft = GasMixRule.DecayFactor(600f);
        Assert.Equal(0.5f + 0.5f * openShareLeft, factor, 4);
        Assert.Equal(1f, GasMixRule.OpenRoofFactor(0, 20, 600f));
    }

    [Fact]
    public void ARebuiltRoomSpreadsTheAmountItsCellsHeld()
    {
        // Two rooms of 10 cells at 0.6 and 0.2 are merged with 20 empty cells: 8 cells of gas over 40 cells.
        float amount = 10 * 0.6f + 10 * 0.2f;
        Assert.Equal(0.2f, GasMixRule.Rebuilt(amount, 40), 4);
        // A room split in two: each half keeps its own value.
        Assert.Equal(0.6f, GasMixRule.Rebuilt(5 * 0.6f, 5), 4);
    }

    [Fact]
    public void VentFractionToOutdoorsIsBoundedByOne()
    {
        Assert.Equal(1f, GasMixRule.VentFraction(1, 1000f));
        Assert.Equal(GasMixRule.OpeningCoefficient(1f, 34) / 50f, GasMixRule.VentFraction(50, GasMixRule.OpeningCoefficient(1f, 34)), 5);
    }
}

public class SourceRuleTests
{
    [Fact]
    public void AutomaticReusesAnExistingGasAndOtherwiseUsesOurs()
    {
        Assert.Equal(GasSource.Existing, SourceRule.Decide(GasSourceMode.Automatic, existingPresent: true, alwaysOwn: false));
        Assert.Equal(GasSource.Own, SourceRule.Decide(GasSourceMode.Automatic, existingPresent: false, alwaysOwn: false));
    }

    [Fact]
    public void TheOverridesWinAndThePinBeatsTheMode()
    {
        Assert.Equal(GasSource.Own, SourceRule.Decide(GasSourceMode.AlwaysOwn, existingPresent: true, alwaysOwn: false));
        Assert.Equal(GasSource.Existing, SourceRule.Decide(GasSourceMode.ExistingOnly, existingPresent: true, alwaysOwn: false));
        Assert.Equal(GasSource.None, SourceRule.Decide(GasSourceMode.ExistingOnly, existingPresent: false, alwaysOwn: false));
        Assert.Equal(GasSource.Own, SourceRule.Decide(GasSourceMode.ExistingOnly, existingPresent: true, alwaysOwn: true));
    }
}

public class SignalRuleTests
{
    [Fact]
    public void CombatExtendedsConditionIsTheSignalOnlyWhileItsSmokeEffectsRun()
    {
        Assert.True(SignalRule.UseCombatExtendedSmoke(true, true));
        Assert.False(SignalRule.UseCombatExtendedSmoke(true, false));
        Assert.False(SignalRule.UseCombatExtendedSmoke(false, true));
    }

    [Fact]
    public void TheCombatExtendedPathIgnoresTheGamesSmokeAndClamps()
    {
        Assert.Equal(1f, SignalRule.BreathedSmoke(true, 1.4f, 0.9f, 0.1f));
        Assert.Equal(0.3f, SignalRule.BreathedSmoke(true, 0.3f, 0.9f, 0.1f), 4);
    }

    [Fact]
    public void TheVanillaPathIsDensityThroughTheMask()
    {
        Assert.Equal(0.18f, SignalRule.BreathedSmoke(false, 0.9f, 0.9f, 0.2f), 4);
        Assert.Equal(0f, SignalRule.BreathedSmoke(false, 0.9f, 0.9f, 0f));
    }
}

public class MaskRuleTests
{
    [Fact]
    public void AFilterMaskIsTheGamesToxicEnvironmentResistance()
    {
        Assert.Equal(0.2f, MaskRule.Filtered(0.8f, false), 4);
        Assert.Equal(1f, MaskRule.Filtered(0f, false));
        Assert.Equal(0f, MaskRule.Filtered(0f, true));
    }

    [Fact]
    public void CarbonMonoxideIgnoresMasksButNotSealedHeadgear()
    {
        Assert.Equal(1f, MaskRule.Unfiltered(false));
        Assert.Equal(0f, MaskRule.Unfiltered(true));
        // Combat Extended: its power and recon helmets set smoke sensitivity to 0 (an offset of -1).
        Assert.True(MaskRule.IsSealed(true, 0f, 0f, 0.999f));
        Assert.False(MaskRule.IsSealed(true, 1f, 0.8f, 0.999f));
        // Its gas masks carry the same -1, but a filter mask is not sealed air: their offset is left out of the figure.
        Assert.Equal(1f, MaskRule.SensitivityFromSealingGear(1f, new float[0]));
        Assert.Equal(0f, MaskRule.SensitivityFromSealingGear(1f, new[] { -1f }));
        Assert.Equal(0f, MaskRule.SensitivityFromSealingGear(1f, new[] { -1f, -0.6f }));
        Assert.False(MaskRule.IsSealed(true, MaskRule.SensitivityFromSealingGear(1f, new float[0]), 0.8f, 0.999f));
        // Without it, only total toxic environment resistance counts as sealed.
        Assert.True(MaskRule.IsSealed(false, 1f, 1f, 0.999f));
        Assert.False(MaskRule.IsSealed(false, 1f, 0.8f, 0.999f));
    }
}

public class EnclosureAndFireSmokeRuleTests
{
    [Fact]
    public void EnclosureIsOneInASealedRoomAndZeroInTheOpen()
    {
        Assert.Equal(1f, EnclosureRule.Compute(true, false, 0, 20, 0.25f));
        Assert.Equal(0f, EnclosureRule.Compute(false, false, 0, 20, 0.25f));
        Assert.Equal(0.5f, EnclosureRule.Compute(true, false, 10, 20, 0.25f));
        Assert.Equal(0.25f, EnclosureRule.Compute(true, true, 0, 20, 0.25f));
    }

    [Fact]
    public void FireSmokeScalesWithFireSizeUnderARoofOnly()
    {
        Assert.Equal(0.08f, FireSmokeRule.Amount(1f, true, 0.08f, 0.2f), 4);
        Assert.Equal(0.2f, FireSmokeRule.Amount(5f, true, 0.08f, 0.2f), 4);
        Assert.Equal(0f, FireSmokeRule.Amount(1f, false, 0.08f, 0.2f));
        Assert.Equal(255, FireSmokeRule.ToGridUnits(1f));
        Assert.Equal(20, FireSmokeRule.ToGridUnits(0.08f));
        Assert.Equal(0, FireSmokeRule.ToGridUnits(0f));
    }
}

public class ConditionDangerRuleTests
{
    static StageDanger Stage(float min, bool threatening = false, float mtb = -1f) => new(min, threatening, mtb);

    [Fact]
    public void TheLowestDangerousStageWins()
    {
        var stages = new[] { Stage(0f), Stage(0.4f, threatening: true), Stage(0.8f, mtb: 1f) };
        Assert.Equal(0.4f, ConditionDangerRule.DangerSeverity(stages, -1f));
    }

    [Fact]
    public void ALethalSeverityBelowEveryStageCountsAndNoDangerIsHuge()
    {
        Assert.Equal(0.3f, ConditionDangerRule.DangerSeverity(new[] { Stage(0f), Stage(0.5f, threatening: true) }, 0.3f));
        Assert.Equal(ConditionDangerRule.NoDanger, ConditionDangerRule.DangerSeverity(new[] { Stage(0f), Stage(0.5f) }, -1f));
    }

    [Fact]
    public void TheSoonestClockDecides()
    {
        // A four-day death clock, a severity reaching lethal in two days, bleeding out in a day: the bleed wins.
        Assert.Equal(24f, ConditionDangerRule.ExpectedHoursToDeath(4f, 0.5f, 1f, 0.25f, 24f));
        Assert.Equal(48f, ConditionDangerRule.ExpectedHoursToDeath(4f, 0.5f, 1f, 0.25f, -1f));
        Assert.Equal(96f, ConditionDangerRule.ExpectedHoursToDeath(4f, 0.5f, 1f, 0f, -1f));
    }

    [Fact]
    public void AFallingSeverityHasNoLethalClockAndNothingMeansNoClock()
    {
        Assert.Equal(ConditionDangerRule.NoClock, ConditionDangerRule.ExpectedHoursToDeath(-1f, 0.5f, 1f, -0.2f, -1f));
        Assert.Equal(0f, ConditionDangerRule.ExpectedHoursToDeath(-1f, 1.2f, 1f, 0.1f, -1f));
    }
}
