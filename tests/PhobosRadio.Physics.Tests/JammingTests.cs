using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

/// <summary>Local noise by bandwidth (EW2) and jamming as noise (EW1): docs/development/electronic-warfare.md, "A worked example".</summary>
public class JammingTests
{
    [Fact]
    public void NoiseQuotedInAVoiceChannelScalesToTheMode()
    {
        Assert.Equal(0.0, NoiseFloor.BandwidthScaleDb(12500), 6);
        Assert.Equal(-13.98, NoiseFloor.BandwidthScaleDb(500), 2);
        Assert.Equal(6.02, NoiseFloor.BandwidthScaleDb(50000), 2);
    }

    [Fact]
    public void MorseHearsLessOfANearbyGeneratorThanVoice()
    {
        // A source at -90 dBm in 12.5 kHz: voice hears all of it, Morse's 500 Hz about 14 dB less.
        Assert.Equal(-90.0, NoiseFloor.TotalDbm(12500, 10, -90), 1);
        Assert.Equal(-104.0, NoiseFloor.TotalDbm(500, 10, -90), 1);
    }

    [Fact]
    public void AVoiceChannelFloorIsUnchangedByTheScaling()
    {
        Assert.Equal(Decibels.PowerSumDbm(NoiseFloor.ThermalDbm(12500, 8), -110), NoiseFloor.TotalDbm(12500, 8, -110), 6);
    }

    [Fact]
    public void TheWorkedExampleJammerArrivesAtMinusFortyFiveDbm()
    {
        // 100 W (50 dBm) over the 4 MHz of the 2 m band, heard in 12.5 kHz, 500 m away at 146 MHz: 50 - 25.05 - 69.72.
        Assert.Equal(-44.77, Jamming.AtAntennaDbm(50, 4e6, 12500, 500, 146), 2);
    }

    [Fact]
    public void ACloseHandheldBurnsThroughButAFarOneDoesNot()
    {
        var jamming = Jamming.AtAntennaDbm(50, 4e6, 12500, 500, 146);
        var near = Decibels.FromWatts(5) - Propagation.FreeSpacePathLossDb(50, 146);
        var far = Decibels.FromWatts(5) - Propagation.FreeSpacePathLossDb(1000, 146);
        Assert.True(near - jamming > 12, "voice needs 12 dB over the floor");
        Assert.True(far - jamming < 12);
    }

    [Fact]
    public void MorseHearsFourteenDbLessJammingThanVoice()
    {
        var voice = Jamming.AtAntennaDbm(50, 4e6, 12500, 500, 146);
        var morse = Jamming.AtAntennaDbm(50, 4e6, 500, 500, 146);
        Assert.Equal(13.98, voice - morse, 2);
    }

    [Fact]
    public void AReceiverWiderThanTheJammedSpanHearsAllOfItAndNoMore()
    {
        Assert.Equal(Jamming.AtAntennaDbm(40, 10000, 10000, 100, 146), Jamming.AtAntennaDbm(40, 10000, 50000, 100, 146), 6);
    }

    [Fact]
    public void JammingEntersThroughTheAntennaAndTheCable()
    {
        Assert.Equal(-50.0 + 6 - 2, Jamming.AtInputDbm(-50, 6, 2), 6);
        Assert.True(double.IsNegativeInfinity(Jamming.AtInputDbm(double.NegativeInfinity, 6, 2)));
    }

    [Fact]
    public void NoJammingLeavesTheFloorAndStrongJammingRaisesIt()
    {
        Assert.Equal(-125.0, NoiseFloor.WithJammingDbm(-125, double.NegativeInfinity), 6);
        Assert.Equal(-44.0, NoiseFloor.WithJammingDbm(-125, -44), 1);
    }
}
