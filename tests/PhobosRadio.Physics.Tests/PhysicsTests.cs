using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

public class DecibelsTests
{
    [Fact]
    public void FiveWattsIsThirtySevenDbm() => Assert.Equal(36.99, Decibels.FromWatts(5), 2);

    [Fact]
    public void DoublingDistanceCostsSixDbAndFourTimesThePower()
    {
        Assert.Equal(6.02, Decibels.DoublingDistanceLossDb, 2);
        Assert.Equal(4.0, Decibels.ToMilliwatts(Decibels.DoublingDistanceLossDb), 2);
    }

    [Fact]
    public void TwoEqualNoisesSumToThreeDbMore() => Assert.Equal(-97.0, Decibels.PowerSumDbm(-100, -100), 1);

    [Fact]
    public void NothingSumsToNegativeInfinity() => Assert.True(double.IsNegativeInfinity(Decibels.PowerSumDbm(new double[0])));
}

public class PropagationTests
{
    [Fact]
    public void FreeSpaceLossAtOneKilometreAndOneHundredFortySixMegahertz()
    {
        // 20 log10(1) + 20 log10(146) + 32.45 = 75.74 dB
        Assert.Equal(75.74, Propagation.FreeSpacePathLossDb(1000, 146), 2);
    }

    [Fact]
    public void DoublingDistanceAddsSixDb()
    {
        var near = Propagation.FreeSpacePathLossDb(200, 146);
        var far = Propagation.FreeSpacePathLossDb(400, 146);
        Assert.Equal(Decibels.DoublingDistanceLossDb, far - near, 3);
    }

    [Fact]
    public void DistancesUnderOneMetreClampToOneMetre()
    {
        Assert.Equal(Propagation.FreeSpacePathLossDb(1, 146), Propagation.FreeSpacePathLossDb(0, 146), 6);
    }

    [Fact]
    public void AMastSeesOverAWallUntilTheRayDropsNearTheHandheld()
    {
        // Mast at 10 m to a hand-held at 1.5 m; wall height 2.5 m. The ray is above the wall for the
        // first 88 % of the way and below it for the last 12 %.
        Assert.False(Propagation.RayBlockedByWall(0.5, 10, 1.5, 2.5));
        Assert.False(Propagation.RayBlockedByWall(0.88, 10, 1.5, 2.5));
        Assert.True(Propagation.RayBlockedByWall(0.9, 10, 1.5, 2.5));
        Assert.True(Propagation.RayBlockedByWall(1.0, 10, 1.5, 2.5));
    }

    [Fact]
    public void TwoHandheldsAreBlockedByEveryWall()
    {
        Assert.True(Propagation.RayBlockedByWall(0.0, 1.5, 1.5, 2.5));
        Assert.True(Propagation.RayBlockedByWall(0.5, 1.5, 1.5, 2.5));
    }
}

public class NoiseFloorTests
{
    [Fact]
    public void ThermalNoiseInTwoAndAHalfKilohertzIsMinusOneHundredAndForty()
    {
        Assert.Equal(-140.0, NoiseFloor.ThermalDbm(2500, 0), 1);
    }

    [Fact]
    public void NoiseFigureAddsDirectly() => Assert.Equal(-130.0, NoiseFloor.ThermalDbm(2500, 10), 1);

    [Fact]
    public void NoLocalNoiseLeavesTheThermalFloor()
    {
        Assert.Equal(NoiseFloor.ThermalDbm(12500, 10), NoiseFloor.TotalDbm(12500, 10, double.NegativeInfinity), 6);
    }

    [Fact]
    public void StrongLocalNoiseDominates()
    {
        Assert.Equal(-90.0, NoiseFloor.TotalDbm(12500, 10, -90), 1);
    }

    [Fact]
    public void NoiseFallsSixDbPerDoubling()
    {
        Assert.Equal(-60.0, NoiseFloor.AtDistanceDbm(-60, 1), 6);
        Assert.Equal(-66.02, NoiseFloor.AtDistanceDbm(-60, 2), 2);
        Assert.Equal(-100.0, NoiseFloor.AtDistanceDbm(-60, 100), 2);
    }
}

public class CoaxTests
{
    [Fact]
    public void LossScalesWithLengthAndRootFrequency()
    {
        // RG-213-like: 8 dB per 100 m at 146 MHz. 50 m at 146 MHz = 4 dB; at 4 × the frequency, 2 × the loss.
        Assert.Equal(4.0, Coax.LossDb(50, 8, 146, 146), 6);
        Assert.Equal(8.0, Coax.LossDb(50, 8, 146, 584), 6);
    }

    [Fact]
    public void ZeroLengthIsFree() => Assert.Equal(0.0, Coax.LossDb(0, 8, 146, 146));
}

public class LinkSheetTests
{
    private static LinkInputs Handheld250mOpenGround() => new(
        TransmitPowerDbm: Decibels.FromWatts(5),
        TransmitAntennaGainDbi: 0,
        TransmitFeedLossDb: 0,
        PathLossDb: Propagation.FreeSpacePathLossDb(250, 146),
        ObstructionLossDb: 0,
        ReceiveAntennaGainDbi: 0,
        ReceiveFeedLossDb: 0,
        NoiseFloorDbm: NoiseFloor.ThermalDbm(12500, 10),
        RequiredSnrDb: 12);

    [Fact]
    public void TheSheetAddsUp()
    {
        var direction = new LinkDirection(Handheld250mOpenGround());
        // 37.0 − 63.7 = −26.7 dBm received; floor −123.0 dBm; SNR 96.3; margin 84.3.
        Assert.Equal(-26.7, direction.ReceivedDbm, 1);
        Assert.Equal(84.3, direction.MarginDb, 1);
        Assert.Equal(LinkDirection.KeyMargin, direction.Terms[^1].Key);
        Assert.Equal(direction.MarginDb, direction.Terms[^1].Value);
    }

    [Fact]
    public void TheLargestLossIsThePlainReason()
    {
        var inputs = Handheld250mOpenGround() with { ObstructionLossDb = 80 };
        Assert.Equal(LinkDirection.KeyObstruction, new LinkDirection(inputs).LargestLoss().Key);
        Assert.Equal(LinkDirection.KeyPath, new LinkDirection(Handheld250mOpenGround()).LargestLoss().Key);
    }

    [Fact]
    public void TheWorseDirectionDecides()
    {
        var forward = new LinkDirection(Handheld250mOpenGround());
        var reverse = new LinkDirection(Handheld250mOpenGround() with { ObstructionLossDb = 80 });
        var link = new LinkBudget(forward, reverse, MarginBands.Default);
        Assert.True(link.ReverseLimits);
        Assert.Equal(reverse.MarginDb, link.MarginDb, 6);
        Assert.Equal(LinkOutcome.Marginal, link.Outcome);
    }

    [Theory]
    [InlineData(-0.1, LinkOutcome.NoContact)]
    [InlineData(0.0, LinkOutcome.Marginal)]
    [InlineData(5.9, LinkOutcome.Marginal)]
    [InlineData(6.0, LinkOutcome.Workable)]
    [InlineData(14.9, LinkOutcome.Workable)]
    [InlineData(15.0, LinkOutcome.Clean)]
    public void MarginBandsFollowTheDesign(double margin, LinkOutcome expected)
    {
        Assert.Equal(expected, MarginBands.Default.Classify(margin));
    }
}
