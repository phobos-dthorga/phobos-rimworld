using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class ReachRuleTests
{
    [Theory]
    [InlineData(12, 3, 5)]     // the Hind across: a twelve-cell rotor over a three-cell body, 4.5 a side, rounded up
    [InlineData(12, 9, 2)]     // the Hind along its length
    [InlineData(8.4, 5, 2)]    // the Harrier across: 1.7 a side
    [InlineData(8.4, 7, 1)]    // and along: 0.7
    [InlineData(5, 5, 0)]      // a truck drawn to its footprint
    [InlineData(3.5, 5, 0)]    // drawn smaller than its footprint
    [InlineData(7, 5, 1)]      // exactly one cell a side
    public void TheMarginCoversThePictureOnEachSide(double drawn, int footprint, int margin) =>
        Assert.Equal(margin, ReachRule.Margin(drawn, footprint));
}
