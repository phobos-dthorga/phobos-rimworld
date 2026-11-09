using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class RearmRuleTests
{
    const string Heat = "HEAT";
    const string He = "HE";

    [Fact]
    public void RoundsInTheMagazineAreKept() => Assert.Equal(Heat, RearmRule.KindToKeep(Heat, 4, true));

    [Fact]
    public void AnEmptyMagazineKeepsNothing() => Assert.Null(RearmRule.KindToKeep(Heat, 0, true));

    [Fact]
    public void AKindThePlayerSwitchedOffIsNotKept() => Assert.Null(RearmRule.KindToKeep(Heat, 4, false));

    [Fact]
    public void WhatIsLoadedDecidesWhatIsBrought() => Assert.Equal(Heat, RearmRule.KindToBring<string>(Heat, He));

    [Fact]
    public void WithAnEmptyMagazineTheFirstOnItsWayDecides() => Assert.Equal(He, RearmRule.KindToBring<string>(null, He));

    [Fact]
    public void WithNothingLoadedOrComingAnyKindWillDo() => Assert.Null(RearmRule.KindToBring<string>(null, null));

    [Fact]
    public void TheSameKindTopsUp() => Assert.True(RearmRule.MayLoad(Heat, Heat, null));

    [Fact]
    public void ADifferentKindCannotEvictTheMagazine() => Assert.False(RearmRule.MayLoad(Heat, He, null));

    [Fact]
    public void ThePlayersChoiceMaySwap() => Assert.True(RearmRule.MayLoad(Heat, He, He));

    [Fact]
    public void AnEmptyMagazineTakesAnything() => Assert.True(RearmRule.MayLoad(null, He, null));
}
