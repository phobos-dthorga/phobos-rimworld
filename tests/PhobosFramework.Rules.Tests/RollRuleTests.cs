using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class RollRuleTests
{
    [Fact]
    public void TheRollingShareComesFromTwoPublishedWeights()
    {
        // The record: the Osprey's 52,600 and 57,000 lb give 0.886; the Harrier's 9,415 and 14,100 kg give 0.668.
        Assert.Equal(0.886, RollRule.RollingShareRotor(52600, 57000), 3);
        Assert.Equal(0.668, RollRule.RollingShareThrust(9415, 14100), 3);
        Assert.True(double.IsNaN(RollRule.RollingShareRotor(0, 57000)));
    }

    [Fact]
    public void TheMarginDecidesHowItLeavesTheGround()
    {
        Assert.Equal(RollVerdict.Straight, RollRule.Judge(1.05, 0.886));
        Assert.Equal(RollVerdict.Straight, RollRule.Judge(1.0, 0.886));
        Assert.Equal(RollVerdict.Rolls, RollRule.Judge(0.97, 0.886));
        Assert.Equal(RollVerdict.Rolls, RollRule.Judge(0.886, 0.886));
        Assert.Equal(RollVerdict.Refused, RollRule.Judge(0.85, 0.886));
        // No rolling share: below 1 it cannot leave at all; a margin that cannot be judged lifts straight up.
        Assert.Equal(RollVerdict.Refused, RollRule.Judge(0.97, double.NaN));
        Assert.Equal(RollVerdict.Straight, RollRule.Judge(double.NaN, 0.886));
    }

    [Fact]
    public void TheStripFollowsTheRecordsWorkedTable()
    {
        Assert.Equal(0, RollRule.StripCells(1.05, 0.886, 5, 20));
        Assert.Equal(9, RollRule.StripCells(0.97, 0.886, 5, 20));
        Assert.Equal(13, RollRule.StripCells(0.94, 0.886, 5, 20));
        Assert.Equal(17, RollRule.StripCells(0.91, 0.886, 5, 20));
        Assert.Equal(20, RollRule.StripCells(0.886, 0.886, 5, 20));
        // Just under a hover, the shortest roll.
        Assert.Equal(5, RollRule.StripCells(0.9999, 0.886, 5, 20));
    }

    [Fact]
    public void ARefusedAircraftIsToldHowMuchLighterToRoll()
    {
        // At 0.85 against 0.886, a rotor aircraft keeps (0.85 / 0.886)^(2/3) of its weight.
        double keep = RollRule.WeightShareToRoll(0.85, 0.886, rotor: true);
        Assert.InRange(keep, 0.97, 0.975);
        // Lighter by that share, its margin rises by its inverse to the 1.5: just the rolling share.
        Assert.Equal(0.886, 0.85 * System.Math.Pow(1 / keep, 1.5), 3);
        Assert.Equal(0.85 / 0.886, RollRule.WeightShareToRoll(0.85, 0.886, rotor: false), 9);
    }
}
