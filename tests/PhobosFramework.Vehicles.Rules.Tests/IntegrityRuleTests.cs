using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class IntegrityRuleTests
{
    [Fact]
    public void WithNoMountsItIsVehicleFrameworksOwnSum()
    {
        var rule = new IntegrityRule();
        rule.Add(50, 100, counts: true);
        rule.Add(25, 50, counts: true);
        Assert.Equal(0.5, rule.Fraction(), 3);
    }

    [Fact]
    public void AnIntactMountDoesNotKeepAWreckAlive()
    {
        // Every working part gone, three whole mounts left (the Highwayman's cradle, bracket and winch mount: 80 points).
        var rule = new IntegrityRule();
        rule.Add(0, 1845, counts: true);
        rule.Add(80, 80, counts: false);
        Assert.True(rule.Fraction() < 0.01);
    }

    [Fact]
    public void ABrokenMountDoesNotBringAVehicleCloserToDestruction()
    {
        var rule = new IntegrityRule();
        rule.Add(900, 1000, counts: true);
        rule.Add(0, 80, counts: false);
        Assert.Equal(0.9, rule.Fraction(), 3);
    }

    [Fact]
    public void AWorkingPartOfOursStillCounts()
    {
        // A battery we added is a counted part like any other.
        var rule = new IntegrityRule();
        rule.Add(0, 100, counts: true);
        rule.Add(20, 20, counts: true);
        Assert.Equal(0.167, rule.Fraction(), 3);
    }

    [Fact]
    public void AVehicleOfNothingButMountsFallsBackToAllItsParts()
    {
        var rule = new IntegrityRule();
        rule.Add(10, 40, counts: false);
        Assert.Equal(0.25, rule.Fraction(), 3);
    }

    [Fact]
    public void NoPartsAtAllReadsAsWhole() => Assert.Equal(1, new IntegrityRule().Fraction(), 3);
}
