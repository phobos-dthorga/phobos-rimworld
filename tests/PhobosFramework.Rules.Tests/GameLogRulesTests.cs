using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class GameLogRulesTests
{
    // Written for these tests in the shape of the game's error lines; not copied from a log.
    const string StormA = "System.NullReferenceException: Object reference not set to an instance of an object.\n[Ref 12B4AFA6]\n"
        + "   at Example.Component.<Drop>b__1(Pawn pawn) in Example.dll:token 0x600007d+0x0\n   at System.Linq.Enumerable.Sum()";
    const string StormB = "System.NullReferenceException: Object reference not set to an instance of an object.\n[Ref 9C0FFEE1]\n"
        + "   at Example.Component.<Drop>b__1(Pawn pawn) in Example.dll:token 0x600007d+0x4\n   at System.Linq.Enumerable.Sum()";
    const string Elsewhere = "System.NullReferenceException: Object reference not set to an instance of an object.\n"
        + "   at Other.Thing.Tick() in Other.dll:token 0x6000001+0x10";

    [Fact]
    public void RepeatsOfOneFaultShareASignature()
    {
        Assert.Equal(LogSignature.Of(StormA), LogSignature.Of(StormB));
        Assert.Equal("System.NullReferenceException: Object reference not set to an instance of an object. @ Example.Component.<Drop>b__#(Pawn pawn)",
            LogSignature.Of(StormA));
    }

    [Fact]
    public void TheSameExceptionFromElsewhereIsADifferentSignature()
    {
        Assert.NotEqual(LogSignature.Of(StormA), LogSignature.Of(Elsewhere));
    }

    [Fact]
    public void NumbersAndHexAreBlankedAndLongLinesCut()
    {
        Assert.Equal("Map # at (#, #, #) item #", LogSignature.Of("Map 12 at (135, 0, 208) item 0x1F3A"));
        Assert.Equal(LogSignature.HeadLimit, LogSignature.Of(new string('a', 500)).Length);
        Assert.Equal("(empty)", LogSignature.Of(null));
    }

    [Fact]
    public void TheTallyNamesTheLoudestAndStaysBounded()
    {
        var tally = new StormTally(capacity: 2);
        for (int i = 0; i < 5; i++) tally.Add("storm");
        tally.Add("once");
        tally.Add("third, past the capacity");
        var top = tally.Top(1);
        Assert.Equal("storm", top[0].Key);
        Assert.Equal(5, top[0].Value);
        Assert.Equal(7, tally.Total);
        Assert.Equal(1, tally.Others);
        tally.Clear();
        Assert.Equal(0, tally.Total);
        Assert.Empty(tally.Top(3));
    }

    [Fact]
    public void RevivalsStopAtTheBound()
    {
        Assert.True(GameLogRule.MayRevive(GameLogRule.MaxRevivals - 1));
        Assert.False(GameLogRule.MayRevive(GameLogRule.MaxRevivals));
    }
}
