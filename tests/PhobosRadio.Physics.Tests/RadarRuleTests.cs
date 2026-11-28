using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

/// <summary>
/// The two-way radar sum against the handbook's own example and the worked example in docs/development/radar-and-air-defence.md.
/// </summary>
public class RadarRuleTests
{
    /// <summary>
    /// NAWCWD TP 8347, section 4-4: 70 dBm out, 40 dB of antenna each way (45 dB less 5 dB of line loss), 5 GHz, a 9 m² target at 31 km
    /// gives −77.52 dBm. Our constants give −77.53.
    /// </summary>
    [Fact]
    public void TheHandbookExampleEchoIsMinusSeventySevenPointFiveDbm()
    {
        Assert.Equal(-77.52, RadarRule.EchoDbm(70, 40, 5000, 9, 31000), 1);
    }

    [Fact]
    public void DoublingTheRangeCostsTwelveDb()
    {
        var near = RadarRule.EchoDbm(80, 33, 3000, 5, 20000);
        var far = RadarRule.EchoDbm(80, 33, 3000, 5, 40000);
        Assert.Equal(12.04, near - far, 2);
    }

    /// <summary>The worked example: S band, 100 kW (80 dBm), 33 dBi, 1 MHz, noise figure 5 dB, so a floor of −109 dBm.</summary>
    [Theory]
    [InlineData(5.0, true, 50.0)]     // a fighter, fluctuating
    [InlineData(5.0, false, 79.0)]    // the same, were it steady
    [InlineData(100.0, true, 106.0)]  // a cargo aircraft
    [InlineData(0.01, true, 10.0)]    // a mortar bomb
    public void TheWorkedExampleRadarSeesEachTargetAtItsRange(double squareMetres, bool fluctuating, double km)
    {
        var floor = NoiseFloor.ThermalDbm(1e6, 5);
        var metres = RadarRule.DetectionRangeMetres(80, 33, 3000, squareMetres, floor, RadarRule.NeededSnrDb(fluctuating));
        // The record's figures came from exact constants (c and 4π); the handbook's rounded ones differ by under 1 %.
        Assert.InRange(metres / 1000.0, km - 1.0, km + 1.0);
    }

    [Fact]
    public void AtTheDetectionRangeTheEchoStandsTheNeededMarginAboveTheFloor()
    {
        var floor = NoiseFloor.ThermalDbm(1e6, 5);
        var metres = RadarRule.DetectionRangeMetres(80, 33, 3000, 5, floor, RadarRule.FluctuatingTargetSnrDb);
        Assert.Equal(floor + RadarRule.FluctuatingTargetSnrDb, RadarRule.EchoDbm(80, 33, 3000, 5, metres), 6);
    }

    [Fact]
    public void TheHorizonCutsALowFlyerButNotAHighOne()
    {
        // 10 m mast: 63.5 km to a flyer at 150 m, 239 km to one at 3,000 m.
        Assert.Equal(63.5, RadarRule.SeenRangeMetres(1e6, 10, 150) / 1000.0, 1);
        Assert.Equal(239.0, RadarRule.SeenRangeMetres(1e6, 10, 3000) / 1000.0, 0);
        Assert.Equal(50000.0, RadarRule.SeenRangeMetres(50000, 10, 3000));
    }
}
