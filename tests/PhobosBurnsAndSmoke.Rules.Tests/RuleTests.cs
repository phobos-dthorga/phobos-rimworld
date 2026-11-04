using Phobos.BurnsAndSmoke.Rules;
using Xunit;

namespace Phobos.BurnsAndSmoke.Rules.Tests;

public class BurnLoadRuleTests
{
    const float Fire = 1f / 12f;

    [Fact]
    public void NoBurnsGivesNothingBurnedAndEverythingTended()
    {
        BurnLoad load = BurnLoadRule.Compute(new BurnedPart[0]);
        Assert.Equal(0f, load.Tbsa);
        Assert.Equal(1f, load.TendedFraction);
    }

    [Fact]
    public void AreaComesFromDamageByKindNotFromPartHealth()
    {
        // Fire: 12 points cover a whole part, more never covers more than the part.
        Assert.Equal(0.25f, BurnLoadRule.AreaFraction(3f, Fire, 1f), 4);
        Assert.Equal(1f, BurnLoadRule.AreaFraction(40f, Fire, 1f), 4);
        // An incendiary round: deep but small, 40 points for a whole part and never past 30% of it.
        Assert.Equal(0.25f, BurnLoadRule.AreaFraction(10f, 0.025f, 0.3f), 4);
        Assert.Equal(0.3f, BurnLoadRule.AreaFraction(40f, 0.025f, 0.3f), 4);
        Assert.Equal(0f, BurnLoadRule.AreaFraction(0f, Fire, 1f));
    }

    [Fact]
    public void TenSecondsOnFireIsASeriousBurn()
    {
        // Four fire hits of 2.5: two on the torso (share 0.405), one on a leg (0.126), one on an arm (0.08).
        BurnLoad load = BurnLoadRule.Compute(new[]
        {
            new BurnedPart(BurnLoadRule.AreaFraction(5f, Fire, 1f), 0.405f, false),
            new BurnedPart(BurnLoadRule.AreaFraction(2.5f, Fire, 1f), 0.126f, false),
            new BurnedPart(BurnLoadRule.AreaFraction(2.5f, Fire, 1f), 0.08f, false),
        });
        Assert.Equal(0.405f * 5f / 12f + 0.126f * 2.5f / 12f + 0.08f * 2.5f / 12f, load.Tbsa, 4);
        Assert.True(load.Tbsa >= 0.15f);
    }

    [Fact]
    public void TheTendedShareIsByAreaNotByCount()
    {
        // A tended whole torso and an untended whole hand (share 0.008): nearly all of the burned area is tended.
        BurnLoad load = BurnLoadRule.Compute(new[]
        {
            new BurnedPart(1f, 0.405f, true),
            new BurnedPart(1f, 0.008f, false),
        });
        Assert.Equal(0.405f / 0.413f, load.TendedFraction, 3);
    }
}

public class SkinShareRuleTests
{
    [Fact]
    public void OwnShareIsTheSubtreeLessItsOutsideChildren()
    {
        // The human torso: coverage 1 with children, less neck 0.075, two shoulders at 0.12 and two legs at 0.14 (the design
        // record's 0.405). Inside organs are not subtracted.
        Assert.Equal(0.405f, SkinShareRule.OwnShare(1f, 0.075f + 0.24f + 0.28f), 4);
        // The head: 0.06 with children, less two eyes, two ears, nose and jaw; the brain stays with the skin over it.
        Assert.Equal(0.0282f, SkinShareRule.OwnShare(0.06f, 0.0084f + 0.0084f + 0.006f + 0.009f), 4);
        Assert.Equal(0f, SkinShareRule.OwnShare(0.1f, 0.2f));
    }
}

public class BurnedOffTests
{
    const int HealTicks = 15 * 60000;

    [Fact]
    public void AFreshStumpCountsWholeThenFadesWithAge()
    {
        Assert.Equal(1f, BurnLoadRule.BurnedOffWeight(true, 100000, HealTicks));
        Assert.Equal(0.5f, BurnLoadRule.BurnedOffWeight(false, HealTicks / 2, HealTicks), 4);
        Assert.Equal(0f, BurnLoadRule.BurnedOffWeight(false, HealTicks + 1, HealTicks));
        Assert.Equal(0f, BurnLoadRule.BurnedOffWeight(false, 0, 0));
    }

    [Fact]
    public void ALostArmAndALostLegAloneCrossTheShockThreshold()
    {
        // Subtree shares from the human body: an arm 0.0924, a leg 0.14.
        BurnLoad load = BurnLoadRule.Compute(new[] { BurnedPart.Lost(0.0924f, 1f, false), BurnedPart.Lost(0.14f, 1f, true) });
        Assert.Equal(0.2324f, load.Tbsa, 4);
        Assert.True(load.Tbsa >= 0.15f);
        Assert.Equal(0.14f / 0.2324f, load.TendedFraction, 3);
    }
}

public class BurnShockRuleTests
{
    [Fact]
    public void PastTheThresholdTheTargetHasAFloor()
    {
        Assert.Equal(0.1f, BurnShockRule.WithThresholdFloor(0f, 0.15f, 0.15f, 0.1f));
        Assert.Equal(0f, BurnShockRule.WithThresholdFloor(0f, 0.14f, 0.15f, 0.1f));
        Assert.Equal(0.5f, BurnShockRule.WithThresholdFloor(0.5f, 0.4f, 0.15f, 0.1f));
    }

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
    public void TreatmentLowersTheTargetByMedicineQualityToAFloor()
    {
        // Burns tended and in bed, fluids by the game's tend quality: none 0.3, herbal 0.6, industrial 1, glitterworld 1.3.
        Assert.Equal(1f, BurnShockRule.TreatmentFactor(0f, false, 0f, 0.2f, 0.15f, 0.35f, 1.3f, 0.1f));
        Assert.Equal(0.545f, BurnShockRule.TreatmentFactor(1f, true, 0.3f, 0.2f, 0.15f, 0.35f, 1.3f, 0.1f), 4);
        Assert.Equal(0.44f, BurnShockRule.TreatmentFactor(1f, true, 0.6f, 0.2f, 0.15f, 0.35f, 1.3f, 0.1f), 4);
        Assert.Equal(0.30f, BurnShockRule.TreatmentFactor(1f, true, 1f, 0.2f, 0.15f, 0.35f, 1.3f, 0.1f), 4);
        Assert.Equal(0.195f, BurnShockRule.TreatmentFactor(1f, true, 1.3f, 0.2f, 0.15f, 0.35f, 1.3f, 0.1f), 4);
        // Quality past the cap earns nothing more; everything together never passes the floor.
        Assert.Equal(0.195f, BurnShockRule.TreatmentFactor(1f, true, 1.6f, 0.2f, 0.15f, 0.35f, 1.3f, 0.1f), 4);
        Assert.Equal(0.1f, BurnShockRule.TreatmentFactor(1f, true, 1.3f, 0.5f, 0.3f, 0.35f, 1.3f, 0.1f), 4);
    }

    [Fact]
    public void TheWorstScoresStayOutOfTheDeathClocksWithIndustrialCare()
    {
        // Score 110 or more: target 1. Full care with industrial medicine lands at the "shock" stage (0.30), under the
        // "severe" clock at 0.55; glitterworld at "compensated"; no medicine just under "severe".
        float untreated = BurnShockRule.Target(120f, 36f, 110f);
        Assert.Equal(1f, untreated);
        Assert.Equal(0.30f, untreated * BurnShockRule.TreatmentFactor(1f, true, 1f, 0.2f, 0.15f, 0.35f, 1.3f, 0.1f), 4);
        Assert.Equal(0.195f, untreated * BurnShockRule.TreatmentFactor(1f, true, 1.3f, 0.2f, 0.15f, 0.35f, 1.3f, 0.1f), 4);
        Assert.True(untreated * BurnShockRule.TreatmentFactor(1f, true, 0.3f, 0.2f, 0.15f, 0.35f, 1.3f, 0.1f) < 0.55f);
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
