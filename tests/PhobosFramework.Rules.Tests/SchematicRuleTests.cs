using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class SchematicRuleTests
{
    static readonly List<(double x, double y)> Elbow = new() { (0, 0), (30, 0), (30, 40) };

    [Fact]
    public void APipeIsAsLongAsItsRuns() => Assert.Equal(70, SchematicRule.Length(Elbow), 6);

    [Fact]
    public void APointAlongTheFirstRunIsOnIt() => Assert.Equal((15.0, 0.0), SchematicRule.PointAlong(Elbow, 15));

    [Fact]
    public void APointPastTheElbowTurnsTheCorner() => Assert.Equal((30.0, 10.0), SchematicRule.PointAlong(Elbow, 40));

    [Theory]
    [InlineData(-5)]
    [InlineData(500)]
    public void APointOffEitherEndIsHeldAtThatEnd(double distance)
    {
        (double x, double y) point = SchematicRule.PointAlong(Elbow, distance);
        Assert.Equal(distance < 0 ? Elbow[0] : Elbow[2], point);
    }

    [Fact]
    public void MarksSitOneSpacingApartFromThePhase() =>
        Assert.Equal(new[] { 4.0, 20, 36, 52, 68 }, SchematicRule.MarkDistances(70, 16, 4).ToArray());

    [Fact]
    public void APhaseOfAWholeSpacingPutsTheMarksBackWhereTheyStarted() =>
        Assert.Equal(SchematicRule.MarkDistances(70, 16, 3).ToArray(), SchematicRule.MarkDistances(70, 16, 35).ToArray());

    [Fact]
    public void ANegativePhaseStillStartsOnThePipe() => Assert.Equal(12, SchematicRule.MarkDistances(70, 16, -4).First(), 6);

    [Fact]
    public void AnEmptyPipeHasNoMarks() => Assert.Empty(SchematicRule.MarkDistances(0, 16, 0));

    [Fact]
    public void AWideDrawingFitsTheSpaceByItsWidthAndIsCentredUpAndDown()
    {
        (double scale, double x, double y) = SchematicRule.Fit(720, 360, 360, 360);
        Assert.Equal(0.5, scale, 6);
        Assert.Equal(0, x, 6);
        Assert.Equal(90, y, 6);
    }

    [Fact]
    public void NoSpaceDrawsNothing() => Assert.Equal(0, SchematicRule.Fit(720, 360, 0, 100).scale);

    [Theory]
    [InlineData(0.39, SchematicTone.Bad)]
    [InlineData(0.4, SchematicTone.Caution)]
    [InlineData(0.69, SchematicTone.Caution)]
    [InlineData(0.7, SchematicTone.Good)]
    [InlineData(1, SchematicTone.Good)]
    public void HealthReadsInTheGamesBands(double share, SchematicTone tone) => Assert.Equal(tone, SchematicRule.FromHealth(share));

    [Fact]
    public void TheWorseToneWins()
    {
        Assert.Equal(SchematicTone.Bad, SchematicRule.Worst(SchematicTone.Good, SchematicTone.Bad));
        Assert.Equal(SchematicTone.Caution, SchematicRule.Worst(SchematicTone.Caution, SchematicTone.Off));
    }

    [Theory]
    [InlineData(true, 1, false, true)]
    [InlineData(false, 1, false, false)]
    [InlineData(true, 0.09, false, false)]
    [InlineData(true, 0.1, false, true)]
    [InlineData(true, 1, true, false)]
    public void SomethingFlowsOnlyWhileTheMachineRunsAndItsDriverWorks(bool running, double work, bool stopped, bool flows) =>
        Assert.Equal(flows, SchematicRule.Flows(running, work, stopped));
}
