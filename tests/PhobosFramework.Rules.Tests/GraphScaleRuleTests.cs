using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class GraphScaleRuleTests
{
    [Fact]
    public void AFewHoursAgainstAHundredThousandScaleToTheFloorSpan() => Assert.Equal(24, GraphScaleRule.Top(5, 0, 100000, 24), 6);

    [Fact]
    public void MoreHoursGetAQuarterOfHeadroom() => Assert.Equal(250, GraphScaleRule.Top(200, 0, 100000, 24), 6);

    [Fact]
    public void DataThatFillsItsRangeKeepsTheListedTop() => Assert.Equal(4, GraphScaleRule.Top(3, 0, 4, 24), 6);

    [Fact]
    public void TheScaledTopNeverPassesTheListedTop() => Assert.Equal(2, GraphScaleRule.Top(0.1, 0, 2, 24), 6);

    [Fact]
    public void ARangeNotStartingAtZeroIsMeasuredFromItsFloor() => Assert.Equal(124, GraphScaleRule.Top(105, 100, 100000, 24), 6);

    [Fact]
    public void NoDataKeepsTheListedTop() => Assert.Equal(100, GraphScaleRule.Top(double.NaN, 0, 100, 24), 6);
}
