using System.Text.RegularExpressions;
using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

/// <summary>QSL cards (Radio 0.58.0): the Maidenhead locator, callsigns and reports.</summary>
public class QslRuleTests
{
    [Theory]
    [InlineData(48.14666, 11.60833, "JN58td")] // Munich, the locator's own worked example
    [InlineData(0.0, 0.0, "JJ00aa")]
    [InlineData(-90.0, -180.0, "AA00aa")]
    [InlineData(90.0, 180.0, "RR99xx")] // the far corner stays on the grid
    public void TheLocatorFollowsThePublishedAlgorithm(double lat, double lon, string locator) =>
        Assert.Equal(locator, QslRule.Locator(lat, lon));

    [Fact]
    public void ACallsignIsAPrefixAnAreaDigitAndTwoOrThreeLetters()
    {
        for (int seed = 0; seed < 200; seed++)
            Assert.Matches(new Regex("^RY[0-9][A-Z]{2,3}$"), QslRule.Callsign("RY", 30, seed));
    }

    [Theory]
    [InlineData(-89.0, '0')]
    [InlineData(0.0, '5')]
    [InlineData(89.9, '9')]
    public void TheAreaDigitRunsSouthToNorth(double latitude, char digit) =>
        Assert.Equal(digit, QslRule.Callsign("AB", latitude, 7)[2]);

    [Fact]
    public void TheSameStationAlwaysHasTheSameCallsign() =>
        Assert.Equal(QslRule.Callsign("RY", 30, 1234), QslRule.Callsign("RY", 30, 1234));

    [Fact]
    public void APrefixTakesTheNamesFirstLetterWithoutItsMarks() =>
        Assert.StartsWith("R", QslRule.Prefix("Ryūmon Protectorate", 42));

    [Fact]
    public void AnUnlettedNameStillGetsALetter() =>
        Assert.Matches(new Regex("^[A-Z]{2}$"), QslRule.Prefix("§§§", 42));

    [Fact]
    public void SteppingPastATakenPrefixChangesItsSecondLetter() =>
        Assert.NotEqual(QslRule.Prefix("Toxo", 5), QslRule.Prefix("Toxo", 5, taken: 1));

    [Theory]
    [InlineData(5, 3, false, "53")]
    [InlineData(5, 9, true, "599")]
    public void TheReportIsRsForVoiceAndRstForMorse(int r, int s, bool morse, string report) =>
        Assert.Equal(report, QslRule.Report(r, s, morse));

    [Theory]
    [InlineData("vk3vkk", "VK3VKK")]
    [InlineData(" vk3vkk/p ", "VK3VKK/P")]
    [InlineData("A-B c!1", "ABC1")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("ABCDEFGHIJKLMN", "ABCDEFGHIJ")]
    public void AnOwnCallsignKeepsLettersDigitsAndAStroke(string? typed, string printed) =>
        Assert.Equal(printed, QslRule.CleanCallsign(typed));

    [Fact]
    public void APickStaysInRangeAndRepeats()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            int pick = QslRule.Pick(seed, 3);
            Assert.InRange(pick, 0, 2);
            Assert.Equal(pick, QslRule.Pick(seed, 3));
        }
    }
}
