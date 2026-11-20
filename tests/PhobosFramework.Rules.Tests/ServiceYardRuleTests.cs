using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ServiceYardRuleTests
{
    [Fact]
    public void AllowanceIsTheSumOfTheEquipment() =>
        Assert.Equal(160, ServiceYardRule.Allowance(new[] { (80, true), (40, false), (40, false) }));

    [Fact]
    public void AYardWithoutADynoBayIsAllowedNothing() =>
        Assert.Equal(0, ServiceYardRule.Allowance(new[] { (40, false), (40, false) }));

    [Fact]
    public void TwoDynoBaysPayForTwiceTheRoom() =>
        Assert.Equal(160, ServiceYardRule.Allowance(new[] { (80, true), (80, true) }));

    [Theory]
    [InlineData(100, 80, 20)]
    [InlineData(80, 80, 0)]
    [InlineData(50, 80, 0)]
    public void OverflowIsWhatLiesPastTheAllowance(int cells, int allowance, int expected) =>
        Assert.Equal(expected, ServiceYardRule.Overflow(cells, allowance));

    [Theory]
    [InlineData(50, 80, 30)]
    [InlineData(90, 80, 0)]
    public void RoomIsWhatIsLeftOfTheAllowance(int cells, int allowance, int expected) =>
        Assert.Equal(expected, ServiceYardRule.Room(cells, allowance));

    [Fact]
    public void AYardTooBigOrWithoutItsDynoGivesNothing()
    {
        Assert.True(ServiceYardRule.Works(80, 80, true));
        Assert.False(ServiceYardRule.Works(81, 80, true));
        Assert.False(ServiceYardRule.Works(40, 80, false));
        Assert.False(ServiceYardRule.Works(0, 80, true));
    }

    [Fact]
    public void TheBetterOfYardAndPadIsTaken()
    {
        Assert.Equal(0.8f, ServiceYardRule.WorkFactor(true, 0.8f, false, 0.7f));
        Assert.Equal(0.7f, ServiceYardRule.WorkFactor(true, 0.8f, true, 0.7f));
        Assert.Equal(0.7f, ServiceYardRule.WorkFactor(false, 0.8f, true, 0.7f));
        Assert.Equal(1f, ServiceYardRule.WorkFactor(false, 0.8f, false, 0.7f));
    }

    [Fact]
    public void ANonsenseFactorNeverSpeedsWorkUp() => Assert.Equal(1f, ServiceYardRule.WorkFactor(true, 0f, false, 0.7f));

    sealed record V(int Id, int Asked, int MovedUp);

    [Fact]
    public void TheLongestWaitingGoesFirst()
    {
        var line = ServiceYardRule.Line(new[] { new V(1, 300, -1), new V(2, 100, -1), new V(3, 200, -1) }, v => v.Asked, v => v.MovedUp, v => v.Id);
        Assert.Equal(new[] { 2, 3, 1 }, line.Select(v => v.Id));
    }

    [Fact]
    public void ThePlayersMoveUpGoesBeforeEveryoneAndTheLatestFirst()
    {
        var line = ServiceYardRule.Line(new[] { new V(1, 100, -1), new V(2, 300, 500), new V(3, 200, 700) }, v => v.Asked, v => v.MovedUp, v => v.Id);
        Assert.Equal(new[] { 3, 2, 1 }, line.Select(v => v.Id));
    }

    [Fact]
    public void ATieKeepsItsOrderByID()
    {
        var line = ServiceYardRule.Line(new[] { new V(9, 100, -1), new V(4, 100, -1) }, v => v.Asked, v => v.MovedUp, v => v.Id);
        Assert.Equal(new[] { 4, 9 }, line.Select(v => v.Id));
    }

    [Fact]
    public void TurnsAddUpOverTheBays() => Assert.Equal(3, ServiceYardRule.Turns(new[] { 1, 2 }));

    sealed record T(int Id, float Share);

    [Fact]
    public void ThePumpFillsTheEmptiestFirstAndSkipsTheFull()
    {
        T? next = ServiceYardRule.NextToFill(new[] { new T(1, 0.5f), new T(2, 0.2f), new T(3, 1f) }, t => t.Share, t => t.Id);
        Assert.Equal(2, next!.Id);
        Assert.Null(ServiceYardRule.NextToFill(new[] { new T(3, 1f) }, t => t.Share, t => t.Id));
    }

    sealed record S(int Id, int Since);

    [Fact]
    public void RoomIsMadeByTheOneIdleLongest()
    {
        S? out_ = ServiceYardRule.ToMakeRoom(new[] { new S(1, 500), new S(2, 100), new S(3, 300) }, s => s.Since, s => s.Id);
        Assert.Equal(2, out_!.Id);
        Assert.Null(ServiceYardRule.ToMakeRoom(new List<S>(), s => s.Since, s => s.Id));
    }

    [Fact]
    public void EquipmentTouchingTwoYardsBelongsToTheOlder() => Assert.Equal(3, ServiceYardRule.Owner(new[] { 7, 3, 5 }));
}
