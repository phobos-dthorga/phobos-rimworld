using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class GameLogRulesTests
{
    [Fact]
    public void RevivalsStopAtTheBound()
    {
        Assert.True(GameLogRule.MayRevive(GameLogRule.MaxRevivals - 1));
        Assert.False(GameLogRule.MayRevive(GameLogRule.MaxRevivals));
    }
}
