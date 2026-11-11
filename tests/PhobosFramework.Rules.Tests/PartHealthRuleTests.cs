using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class PartHealthRuleTests
{
    [Fact]
    public void OurSavedValueWins() => Assert.Equal(7, PartHealthRule.Restored(7, true, 30, 10), 6);

    [Fact]
    public void ASavedValueAboveTheMaximumIsCapped() => Assert.Equal(10, PartHealthRule.Restored(30, true, 30, 10), 6);

    [Fact]
    public void APartTheSaveNeverHeldWasFittedNew() => Assert.Equal(20, PartHealthRule.Restored(null, true, 30, 20), 6);

    [Fact]
    public void WithNoSavedHealthsTheCurrentHealthIsKept() => Assert.Equal(4, PartHealthRule.Restored(null, false, 4, 10), 6);

    [Fact]
    public void WithNoSavedHealthsTheCurrentHealthIsStillCapped() => Assert.Equal(10, PartHealthRule.Restored(null, false, 30, 10), 6);

    [Fact]
    public void HealthIsNeverBelowNothing() => Assert.Equal(0, PartHealthRule.Restored(-5, true, 3, 10), 6);
}
