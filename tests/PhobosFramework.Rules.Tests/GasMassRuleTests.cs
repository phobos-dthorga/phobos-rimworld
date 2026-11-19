using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class GasMassRuleTests
{
    const double CarbonMonoxide = 28.01;

    [Fact]
    public void OnePpmOfCarbonMonoxideIsAbout1Point15MilligramsACubicMetre() =>
        Assert.Equal(1.1456, GasMassRule.MilligramsPerCubicMetrePerPpm(CarbonMonoxide), 3);

    [Fact]
    public void AGramInOneCellIsItsPpmOverFull()
    {
        // 1 g in 3 m3 is 333.3 mg/m3, about 291 ppm; against a full of 1600 ppm that is about 0.18 of a cell.
        Assert.Equal(0.1819, GasMassRule.CellsFromGrams(1, CarbonMonoxide, 1600), 3);
    }

    [Fact]
    public void GramsScaleInStraightLine() =>
        Assert.Equal(10 * GasMassRule.CellsFromGrams(1, CarbonMonoxide, 1600), GasMassRule.CellsFromGrams(10, CarbonMonoxide, 1600), 9);

    [Theory]
    [InlineData(0, 28, 1600)]
    [InlineData(-5, 28, 1600)]
    [InlineData(5, 0, 1600)]
    [InlineData(5, 28, 0)]
    public void NothingComesOfNothing(double grams, double molar, double full) => Assert.Equal(0, GasMassRule.CellsFromGrams(grams, molar, full));
}
