using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

/// <summary>The detail tier (docs/development/vehicle-health-design.md): the player's things in full, others' diffuse.</summary>
public class DetailTierTests
{
    [Fact]
    public void ThePlayersThingsAreAlwaysFull()
    {
        Assert.Equal(DetailTier.Full, DetailTierRule.For(playerFaction: true, spawnedOnMap: true, fullForEveryone: false));
        Assert.Equal(DetailTier.Full, DetailTierRule.For(playerFaction: true, spawnedOnMap: false, fullForEveryone: false)); // in a caravan or in flight
    }

    [Fact]
    public void AnotherFactionsThingOnAMapIsLiteAndOffTheMapIsOff()
    {
        Assert.Equal(DetailTier.Lite, DetailTierRule.For(playerFaction: false, spawnedOnMap: true, fullForEveryone: false));
        Assert.Equal(DetailTier.Off, DetailTierRule.For(playerFaction: false, spawnedOnMap: false, fullForEveryone: false));
    }

    [Fact]
    public void TheSettingGivesEveryoneOnAMapFullDetail()
    {
        Assert.Equal(DetailTier.Full, DetailTierRule.For(playerFaction: false, spawnedOnMap: true, fullForEveryone: true));
        Assert.Equal(DetailTier.Off, DetailTierRule.For(playerFaction: false, spawnedOnMap: false, fullForEveryone: true));
    }
}
