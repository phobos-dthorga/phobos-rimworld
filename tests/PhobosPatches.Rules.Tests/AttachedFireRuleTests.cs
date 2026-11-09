using Phobos.VehicleFrameworkPatches.Rules;
using Xunit;

namespace Phobos.Patches.Rules.Tests;

public class AttachedFireRuleTests
{
    [Fact]
    public void OneFireOnTheVehiclesMapIsKept() => Assert.Empty(AttachedFireRule.Surplus(new[] { true }));

    [Fact]
    public void EveryFireAfterTheFirstIsSurplus() => Assert.Equal(new[] { 1, 2 }, AttachedFireRule.Surplus(new[] { true, true, true }));

    [Fact]
    public void AFireOnAnotherMapIsSurplusEvenWhenItIsTheOnlyOne() => Assert.Equal(new[] { 0 }, AttachedFireRule.Surplus(new[] { false }));

    [Fact]
    public void TheFirstFireOnTheVehiclesMapIsKeptWhereverItSits() => Assert.Equal(new[] { 0, 2, 3 }, AttachedFireRule.Surplus(new[] { false, true, false, true }));

    [Fact]
    public void NoFiresMeansNothingToDo() => Assert.Empty(AttachedFireRule.Surplus(System.Array.Empty<bool>()));
}
