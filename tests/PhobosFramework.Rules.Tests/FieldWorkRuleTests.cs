using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class FieldWorkRuleTests
{
    [Fact]
    public void APloughsDraughtGrowsWithItsWidth()
    {
        // A mouldboard plough in medium soil, 7.3 kN a metre, three cells wide at 1.5 m a cell: about 32.9 kN.
        Assert.Equal(32850, FieldWorkRule.DraughtNewtons(true, 7.3, 3, 1.5), 6);
        Assert.Equal(2 * FieldWorkRule.DraughtNewtons(true, 7.3, 3, 1.5), FieldWorkRule.DraughtNewtons(true, 7.3, 6, 1.5), 6);
    }

    [Fact]
    public void ALiftedImplementDragsNothing() => Assert.Equal(0, FieldWorkRule.DraughtNewtons(lowered: false, 7.3, 3, 1.5));

    [Fact]
    public void NonsenseFiguresDragNothing()
    {
        Assert.Equal(0, FieldWorkRule.DraughtNewtons(true, -1, 3, 1.5));
        Assert.Equal(0, FieldWorkRule.DraughtNewtons(true, 7.3, 0, 1.5));
        Assert.Equal(0, FieldWorkRule.DraughtNewtons(true, 7.3, 3, 0));
    }

    [Fact]
    public void ARiceCellTakesAFewGramsOfSeed() => Assert.Equal(0.0054, FieldWorkRule.SeedKilogramsPerCell(6, 0.03, 0.03), 9);

    [Fact]
    public void ACropThatYieldsNothingTakesNoSeed() => Assert.Equal(0, FieldWorkRule.SeedKilogramsPerCell(0, 0.03, 0.03));

    [Fact]
    public void SeedIsDrawnAWholeUnitAtATimeAsTheOwedWeightPassesOne()
    {
        // 0.0054 kg a cell, 0.03 kg a unit: the first unit is drawn on the sixth cell, with 0.0024 kg still owed.
        double owed = 0;
        int drawn = 0;
        for (int cell = 1; cell <= 5; cell++)
        {
            (int units, double left) = FieldWorkRule.DrawSeed(owed, 0.0054, 0.03);
            drawn += units;
            owed = left;
        }
        Assert.Equal(0, drawn);
        (int sixth, double after) = FieldWorkRule.DrawSeed(owed, 0.0054, 0.03);
        Assert.Equal(1, sixth);
        Assert.Equal(0.0024, after, 9);
    }

    [Fact]
    public void ACellNeedingSeveralUnitsDrawsThemAtOnce()
    {
        (int units, double owed) = FieldWorkRule.DrawSeed(0, 0.1, 0.03);
        Assert.Equal(3, units);
        Assert.Equal(0.01, owed, 9);
    }

    [Fact]
    public void CleaningTakesItsShareOff()
    {
        Assert.Equal(90, FieldWorkRule.SeedFromGrain(100, 0.1));
        Assert.Equal(8, FieldWorkRule.SeedFromGrain(9, 0.1));
        Assert.Equal(0, FieldWorkRule.SeedFromGrain(0, 0.1));
        Assert.Equal(100, FieldWorkRule.SeedFromGrain(100, -1));
        Assert.Equal(0, FieldWorkRule.SeedFromGrain(100, 2));
    }
}
