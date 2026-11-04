using Phobos.BurnsAndSmoke.Rules;
using Xunit;

namespace Phobos.BurnsAndSmoke.Rules.Tests;

public class BurnLoadRuleTests
{
    [Fact]
    public void NoBurnsGivesNothingBurnedAndEverythingTended()
    {
        BurnLoad load = BurnLoadRule.Compute(new BurnedPart[0]);
        Assert.Equal(0f, load.Tbsa);
        Assert.Equal(1f, load.TendedFraction);
    }

    [Fact]
    public void ABurnPastThePartsHealthCountsAsTheWholePart()
    {
        // Two burns of 20 on a 30-health torso (skin share 0.405, from the human body's coverage) burn the whole torso.
        BurnLoad load = BurnLoadRule.Compute(new[] { new BurnedPart(40f, 30f, 0.405f, false) });
        Assert.Equal(0.405f, load.Tbsa, 4);
        Assert.Equal(0f, load.TendedFraction);
    }

    [Fact]
    public void BurnedAreaIsWeightedByEachPartsShareOfTheSkin()
    {
        // Half the torso and a whole hand (share 0.008).
        BurnLoad load = BurnLoadRule.Compute(new[]
        {
            new BurnedPart(15f, 30f, 0.405f, false),
            new BurnedPart(10f, 10f, 0.008f, false),
        });
        Assert.Equal(0.2025f + 0.008f, load.Tbsa, 4);
    }

    [Fact]
    public void TheTendedShareIsByAreaNotByCount()
    {
        // A tended torso and an untended hand: nearly all of the burned area is tended.
        BurnLoad load = BurnLoadRule.Compute(new[]
        {
            new BurnedPart(30f, 30f, 0.405f, true),
            new BurnedPart(10f, 10f, 0.008f, false),
        });
        Assert.Equal(0.405f / 0.413f, load.TendedFraction, 3);
    }
}

public class BurnShockRuleTests
{
    [Fact]
    public void BelowTheNoDeathScoreThereIsNoTarget()
    {
        // Age 20, 10% burned: score 30, below 36.
        float baux = BurnShockRule.Baux(20f, 0.10f, false, 17f);
        Assert.Equal(30f, baux);
        Assert.Equal(0f, BurnShockRule.Target(baux, 36f, 110f));
    }

    [Fact]
    public void AnInhalationInjuryAddsSeventeenPoints()
    {
        // The plan's worked case: age 40, 25% burned.
        Assert.Equal(65f, BurnShockRule.Baux(40f, 0.25f, false, 17f));
        Assert.Equal(82f, BurnShockRule.Baux(40f, 0.25f, true, 17f));
        Assert.Equal((65f - 36f) / 74f, BurnShockRule.Target(65f, 36f, 110f), 4);
        Assert.Equal(1f, BurnShockRule.Target(130f, 36f, 110f));
    }

    [Fact]
    public void TreatmentLowersTheTargetToAFloor()
    {
        Assert.Equal(1f, BurnShockRule.TreatmentFactor(0f, false, 0.6f, 0.3f, 0.1f));
        Assert.Equal(0.4f, BurnShockRule.TreatmentFactor(1f, false, 0.6f, 0.3f, 0.1f), 4);
        Assert.Equal(0.1f, BurnShockRule.TreatmentFactor(1f, true, 0.6f, 0.3f, 0.1f), 4);
    }

    [Fact]
    public void SeverityStepsTowardTheTargetWithoutOvershooting()
    {
        // Rising at 1 per day: half a day brings 0.1 to 0.6, not past a target of 0.5.
        Assert.Equal(0.5f, BurnShockRule.Step(0.1f, 0.5f, 0.5f, 1f, 0.35f), 4);
        Assert.Equal(0.3f, BurnShockRule.Step(0.1f, 0.5f, 0.2f, 1f, 0.35f), 4);
        // Falling at 0.35 per day toward a treated target.
        Assert.Equal(0.6f - 0.35f, BurnShockRule.Step(0.6f, 0.06f, 1f, 1f, 0.35f), 4);
        Assert.Equal(0.06f, BurnShockRule.Step(0.2f, 0.06f, 1f, 1f, 0.35f), 4);
    }
}

public class CarbonMonoxideRuleTests
{
    [Fact]
    public void HalvesInOneHalfLife()
    {
        Assert.Equal(0.2f, CarbonMonoxideRule.Remaining(0.4f, 5.3f, 5.3f), 4);
        Assert.Equal(0.4f, CarbonMonoxideRule.Remaining(0.4f, 0f, 5.3f));
    }

    [Fact]
    public void UptakeScalesWithWhatIsBreathedAndStopsAtTheCap()
    {
        Assert.Equal(0.03f, CarbonMonoxideRule.Uptake(1f, 0.1f, 0.3f, 0f, 0.8f), 4);
        Assert.Equal(0.015f, CarbonMonoxideRule.Uptake(0.5f, 0.1f, 0.3f, 0f, 0.8f), 4);
        Assert.Equal(0f, CarbonMonoxideRule.Uptake(0f, 0.1f, 0.3f, 0f, 0.8f));
        Assert.Equal(0.05f, CarbonMonoxideRule.Uptake(1f, 1f, 0.3f, 0.75f, 0.8f), 4);
    }

    [Fact]
    public void WithoutTheGasItIsInferredFromSmokeInAnEnclosedSpace()
    {
        Assert.Equal(0.5f, CarbonMonoxideRule.InferredBreathed(0.5f, 1f));
        Assert.Equal(0.125f, CarbonMonoxideRule.InferredBreathed(0.5f, 0.25f), 4);
        Assert.Equal(0f, CarbonMonoxideRule.InferredBreathed(0.5f, 0f));
    }
}

public class AirwayRuleTests
{
    [Fact]
    public void SwellingPeaksAtThePeakHourHoldsThenResolves()
    {
        Assert.Equal(0.5f, AirwayRule.Shape(4f, 8f, 24f, 48f), 4);
        Assert.Equal(1f, AirwayRule.Shape(8f, 8f, 24f, 48f));
        Assert.Equal(1f, AirwayRule.Shape(20f, 8f, 24f, 48f));
        Assert.Equal(0.5f, AirwayRule.Shape(36f, 8f, 24f, 48f), 4);
        Assert.Equal(0f, AirwayRule.Shape(48f, 8f, 24f, 48f));
    }

    [Fact]
    public void TheLightestExposureReachesOnlyHalfTheStageTable()
    {
        Assert.Equal(0.5f, AirwayRule.Peak(0f));
        Assert.Equal(1f, AirwayRule.Peak(1f));
        Assert.Equal(0.75f, AirwayRule.Severity(8f, AirwayRule.Peak(0.5f), 8f, 24f, 48f), 4);
    }

    [Fact]
    public void NoExposureMeansNoOnset()
    {
        Assert.Equal(0f, AirwayRule.OnsetChance(false, 0f, 0.15f, 10f));
        Assert.Equal(0.15f, AirwayRule.OnsetChance(true, 0f, 0.15f, 10f));
        Assert.Equal(0.075f, AirwayRule.OnsetChance(false, 10f, 0.15f, 10f), 4);
        Assert.Equal(0.15f, AirwayRule.OnsetChance(false, 40f, 0.15f, 10f), 4);
    }
}
