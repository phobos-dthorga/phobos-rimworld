using System.Collections.Generic;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class BuildingUpgradeRuleTests
{
    [Fact]
    public void AnUpgradeWithEverythingInPlaceCanBeOrdered() =>
        Assert.Equal(UpgradeRefusal.None, BuildingUpgradeRule.CanOrder(fitted: false, prerequisiteFitted: true, researched: true, anotherOrder: false));

    [Fact]
    public void AFittedUpgradeSaysSoBeforeAnythingElse() =>
        Assert.Equal(UpgradeRefusal.Fitted, BuildingUpgradeRule.CanOrder(fitted: true, prerequisiteFitted: false, researched: false, anotherOrder: true));

    [Fact]
    public void OnePieceOfWorkAtATime() =>
        Assert.Equal(UpgradeRefusal.Busy, BuildingUpgradeRule.CanOrder(fitted: false, prerequisiteFitted: true, researched: true, anotherOrder: true));

    [Fact]
    public void TheUpgradeItBuildsOnComesBeforeResearch() =>
        Assert.Equal(UpgradeRefusal.NeedsFirst, BuildingUpgradeRule.CanOrder(fitted: false, prerequisiteFitted: false, researched: false, anotherOrder: false));

    [Fact]
    public void UnresearchedIsRefused() =>
        Assert.Equal(UpgradeRefusal.Research, BuildingUpgradeRule.CanOrder(fitted: false, prerequisiteFitted: true, researched: false, anotherOrder: false));

    [Fact]
    public void AnUpgradeAnotherBuildsOnStaysOn() =>
        Assert.Equal(UpgradeRemoveRefusal.Needed, BuildingUpgradeRule.CanRemove(fitted: true, anotherFittedNeedsIt: true, anotherOrder: false));

    [Fact]
    public void OnlyAFittedUpgradeComesOff() =>
        Assert.Equal(UpgradeRemoveRefusal.NotFitted, BuildingUpgradeRule.CanRemove(fitted: false, anotherFittedNeedsIt: false, anotherOrder: false));

    [Fact]
    public void AFreeFittedUpgradeComesOff() =>
        Assert.Equal(UpgradeRemoveRefusal.None, BuildingUpgradeRule.CanRemove(fitted: true, anotherFittedNeedsIt: false, anotherOrder: false));

    [Fact]
    public void MissingLeavesOutLinesAlreadyMet()
    {
        var delivered = new Dictionary<string, int> { ["steel"] = 40, ["components"] = 1 };
        List<(string what, int count)> missing = BuildingUpgradeRule.Missing(new[] { ("steel", 40), ("components", 3), ("cable", 2) },
            what => delivered.TryGetValue(what, out int have) ? have : 0);
        Assert.Equal(new[] { ("components", 2), ("cable", 2) }, missing);
    }

    [Theory]
    [InlineData(40, 0.5f, 20)]
    [InlineData(3, 0.5f, 1)]
    [InlineData(1, 0.5f, 0)]
    [InlineData(10, 0f, 0)]
    [InlineData(10, 2f, 10)]
    public void ARefundIsAShareRoundedDown(int count, float share, int expected) =>
        Assert.Equal(expected, BuildingUpgradeRule.Refund(count, share));

    [Fact]
    public void AFigureSetByNoUpgradeIsTheBuildingsOwn() =>
        Assert.Equal(30f, BuildingUpgradeRule.Figure(new float[0], 30f));

    [Fact]
    public void TwoUpgradesSettingAFigureGiveTheLargerNeverTheSum() =>
        Assert.Equal(60f, BuildingUpgradeRule.Figure(new[] { 45f, 60f }, 30f));

    [Fact]
    public void ProgressIsClamped()
    {
        Assert.Equal(0.5f, BuildingUpgradeRule.Progress(500f, 1000f));
        Assert.Equal(1f, BuildingUpgradeRule.Progress(1500f, 1000f));
        Assert.Equal(1f, BuildingUpgradeRule.Progress(5f, 0f));
    }

    [Fact]
    public void TakingOffIsAShareOfTheWork() => Assert.Equal(500f, BuildingUpgradeRule.RemoveWork(1000f, 0.5f));
}
