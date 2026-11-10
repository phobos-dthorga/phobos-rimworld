using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class ListeningRuleTests
{
    const float LandShare = 0.5f, StillBoat = 0.5f;

    [Fact]
    public void AtRestWithGoodEarsTheWholeRange() => Assert.Equal(40f, ListeningRule.Range(40f, 1f, false, 0.5f), 3);

    [Fact]
    public void OnTheMoveItsOwnNoiseHalvesIt() => Assert.Equal(20f, ListeningRule.Range(40f, 1f, true, 0.5f), 3);

    [Fact]
    public void PoorHearingShortensIt() => Assert.Equal(24f, ListeningRule.Range(40f, 0.6f, false, 0.5f), 3);

    [Fact]
    public void HearingAboveFullGivesNoMore() => Assert.Equal(40f, ListeningRule.Range(40f, 1.3f, false, 0.5f), 3);

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(40f, 0f)]
    [InlineData(-5f, 1f)]
    public void NoArrayOrNoListenerHearsNothing(float fitted, float hearing) => Assert.Equal(0f, ListeningRule.Range(fitted, hearing, false, 0.5f));

    [Theory]
    [InlineData(39f, true, true)]
    [InlineData(40f, true, false)]
    [InlineData(10f, false, false)]
    public void InTheWater(float distance, bool moving, bool heard) => Assert.Equal(heard, ListeningRule.Hears(ContactKind.InWater, distance, 40f, moving, LandShare, 1f, StillBoat));

    [Theory]
    [InlineData(19f, true, true)]
    [InlineData(21f, true, false)]
    [InlineData(5f, false, false)]
    public void OnLandOnlyAShare(float distance, bool moving, bool heard) => Assert.Equal(heard, ListeningRule.Hears(ContactKind.OnLand, distance, 40f, moving, LandShare, 1f, StillBoat));

    [Theory]
    [InlineData(35f, 1f, true, true)]
    [InlineData(35f, 1f, false, false)]
    [InlineData(19f, 1f, false, true)]
    [InlineData(9f, 0.25f, true, true)]
    [InlineData(9f, 0.25f, false, false)]
    public void AHiddenBoatByItsNoise(float distance, float noise, bool moving, bool heard) => Assert.Equal(heard, ListeningRule.Hears(ContactKind.HiddenBoat, distance, 40f, moving, LandShare, noise, StillBoat));

    [Fact]
    public void NothingIsHeardWithNoRange() => Assert.False(ListeningRule.Hears(ContactKind.InWater, 1f, 0f, true, LandShare, 1f, StillBoat));

    [Theory]
    [InlineData(0, 10, Bearing.North)]
    [InlineData(7, 7, Bearing.NorthEast)]
    [InlineData(10, 0, Bearing.East)]
    [InlineData(7, -7, Bearing.SouthEast)]
    [InlineData(0, -10, Bearing.South)]
    [InlineData(-7, -7, Bearing.SouthWest)]
    [InlineData(-10, 0, Bearing.West)]
    [InlineData(-7, 7, Bearing.NorthWest)]
    [InlineData(-10, 2, Bearing.West)]
    [InlineData(0, 0, Bearing.North)]
    public void Bearings(int dx, int dz, Bearing expected) => Assert.Equal(expected, ListeningRule.BearingOf(dx, dz));

    [Theory]
    [InlineData(10f, 25f, true)]
    [InlineData(25f, 25f, false)]
    [InlineData(10f, 0f, false)]
    [InlineData(-1f, 25f, false)]
    public void WithinSpotting(float nearest, float spotRange, bool expected) => Assert.Equal(expected, ListeningRule.WithinSpotting(nearest, spotRange));
}
