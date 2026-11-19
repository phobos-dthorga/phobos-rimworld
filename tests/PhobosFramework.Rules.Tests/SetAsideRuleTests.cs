using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class SetAsideRuleTests
{
    [Fact]
    public void ATabNotSetAsideIsAlwaysInView() =>
        Assert.False(SetAsideRule.Hidden(false, new[] { 0f, 0f }));

    [Fact]
    public void ASetAsideTabIsOutOfViewForAColonyThatNeverStartedIt() =>
        Assert.True(SetAsideRule.Hidden(true, new[] { 0f, 0f, 0f }));

    [Fact]
    public void AnyResearchPutIntoItKeepsItInView() =>
        Assert.False(SetAsideRule.Hidden(true, new[] { 0f, 12.5f, 0f }));

    [Fact]
    public void OutsideAGameItStaysInView() =>
        Assert.False(SetAsideRule.Hidden(true, null));

    [Fact]
    public void ATabWithNoProjectsIsOutOfView() =>
        Assert.True(SetAsideRule.Hidden(true, System.Array.Empty<float>()));
}
