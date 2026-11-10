using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ProtectionRuleTests
{
    [Theory]
    [InlineData(0.8f, 0.75f, true)]
    [InlineData(0.75f, 0.75f, true)]
    [InlineData(0.74999f, 0.75f, true)]
    [InlineData(0.4f, 0.75f, false)]
    public void ProtectedMeansAtOrAboveTheRequirement(float value, float required, bool expected) =>
        Assert.Equal(expected, ProtectionRule.IsProtected(value, required));

    [Theory]
    [InlineData(false, true, false, false, CrossingAction.SuitUp)]
    [InlineData(false, true, true, false, CrossingAction.Continue)]
    [InlineData(true, true, false, false, CrossingAction.Continue)]
    [InlineData(true, false, false, true, CrossingAction.StepOff)]
    [InlineData(true, false, true, false, CrossingAction.Continue)]
    [InlineData(false, false, false, true, CrossingAction.Continue)]
    public void OnlyCrossingTheLineCounts(bool from, bool to, bool protectedNow, bool wearsLineGear, CrossingAction expected) =>
        Assert.Equal(expected, ProtectionRule.Decide(from, to, protectedNow, wearsLineGear));

    [Fact]
    public void ASuitAndAMaskTogetherReachTheRequirement()
    {
        // A suit and a mask at 0.4 each, a nearer second suit that can't be worn with the first.
        var chosen = ProtectionRule.Choose(0f, 0.75f, new[] { 0.4f, 0.4f, 0.4f }, new[] { 10f, 20f, 5f },
            (a, b) => !((a == 0 && b == 2) || (a == 2 && b == 0)));
        Assert.Equal(new[] { 2, 1 }, chosen);
    }

    [Fact]
    public void TheFewestItemsAreFetched()
    {
        var chosen = ProtectionRule.Choose(0f, 0.75f, new[] { 0.2f, 0.8f, 0.4f }, new[] { 1f, 50f, 1f }, (_, _) => true);
        Assert.Equal(new[] { 1 }, chosen);
    }

    [Fact]
    public void AlreadyProtectedFetchesNothing() =>
        Assert.Empty(ProtectionRule.Choose(0.8f, 0.75f, new[] { 0.4f }, new[] { 1f }, (_, _) => true));

    [Fact]
    public void NotEnoughGearAnywhereFetchesNothing() =>
        Assert.Empty(ProtectionRule.Choose(0f, 0.75f, new[] { 0.4f, 0.2f }, new[] { 1f, 1f }, (_, _) => true));

    [Fact]
    public void GearThatWouldLowerProtectionIsNeverFetched() =>
        Assert.Empty(ProtectionRule.Choose(0.5f, 0.75f, new[] { -0.1f, 0f }, new[] { 1f, 1f }, (_, _) => true));
}
