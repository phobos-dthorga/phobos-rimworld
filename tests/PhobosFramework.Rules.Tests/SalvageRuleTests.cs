using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class SalvageRuleTests
{
    [Fact]
    public void AHullGivesAQuarterOfItsWorthAsParts() => Assert.Equal(10, SalvageRule.HullParts(2400));

    [Fact]
    public void ACheapHullStillGivesOne() => Assert.Equal(1, SalvageRule.HullParts(50));

    [Fact]
    public void AWorthlessHullGivesNothing() => Assert.Equal(0, SalvageRule.HullParts(0));

    [Fact]
    public void PartsPayRoundedUp() => Assert.Equal(3, SalvageRule.PartsToPay(121));

    [Fact]
    public void ExactWorthPaysExactly() => Assert.Equal(2, SalvageRule.PartsToPay(120));

    [Fact]
    public void NothingToPayTakesNothing() => Assert.Equal(0, SalvageRule.PartsToPay(0));
}
