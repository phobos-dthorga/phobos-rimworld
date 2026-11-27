using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class WorkTypeSeedRuleTests
{
    [Fact]
    public void AColonistTakesItsHaulingPriority() =>
        Assert.Equal(2, WorkTypeSeedRule.StartingPriority(new[] { (true, 2), (true, 6) }));

    [Fact]
    public void ADroneThatCannotHaulTakesItsConstructionPriority() =>
        Assert.Equal(3, WorkTypeSeedRule.StartingPriority(new[] { (false, 0), (true, 3) }));

    [Fact]
    public void HaulingSwitchedOffByThePlayerStaysOffRatherThanFallingThrough() =>
        Assert.Equal(0, WorkTypeSeedRule.StartingPriority(new[] { (true, 0), (true, 3) }));

    [Fact]
    public void APawnAbleToDoNoneOfTheSourcesStaysOff() =>
        Assert.Equal(0, WorkTypeSeedRule.StartingPriority(new[] { (false, 1), (false, 2) }));

    [Fact]
    public void NoSourcesLeaveItOff() =>
        Assert.Equal(0, WorkTypeSeedRule.StartingPriority(System.Array.Empty<(bool, int)>()));

    [Fact]
    public void ANegativeSavedPriorityCountsAsOff() =>
        Assert.Equal(0, WorkTypeSeedRule.StartingPriority(new[] { (true, -1) }));
}
