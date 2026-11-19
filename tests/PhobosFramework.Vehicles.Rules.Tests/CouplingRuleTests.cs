using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Couplings (Framework 0.182.0): enforced, with adapters, and everything built before joins anything.</summary>
public class CouplingRuleTests
{
    static readonly string[] Tractor = { "PhobosCoupling_Drawbar", "PhobosCoupling_PickUpHitch" };

    [Fact]
    public void AHitchWithTheTrailersCouplingJoinsDirectly() =>
        Assert.Equal(CouplingMatch.Direct, CouplingRule.Match(Tractor, "PhobosCoupling_Drawbar", null));

    [Fact]
    public void AMismatchIsRefused()
    {
        CouplingMatch match = CouplingRule.Match(Tractor, "PhobosCoupling_Ball", null);
        Assert.Equal(CouplingMatch.None, match);
        Assert.False(CouplingRule.Joins(match));
    }

    [Fact]
    public void AnAdapterFixesAMismatch()
    {
        CouplingMatch match = CouplingRule.Match(Tractor, "PhobosCoupling_Pintle", new[] { "PhobosCoupling_Pintle" });
        Assert.Equal(CouplingMatch.ByAdapter, match);
        Assert.True(CouplingRule.Joins(match));
    }

    [Fact]
    public void AnAdapterForSomethingElseDoesNot() =>
        Assert.Equal(CouplingMatch.None, CouplingRule.Match(Tractor, "PhobosCoupling_Ball", new[] { "PhobosCoupling_Pintle" }));

    [Fact]
    public void ADirectMatchIsPreferredToAnAdapter() =>
        Assert.Equal(CouplingMatch.Direct, CouplingRule.Match(Tractor, "PhobosCoupling_Drawbar", new[] { "PhobosCoupling_Drawbar" }));

    [Fact]
    public void ATrailerNamingNoCouplingJoinsAnyHitch()
    {
        Assert.Equal(CouplingMatch.Universal, CouplingRule.Match(Tractor, null, null));
        Assert.Equal(CouplingMatch.Universal, CouplingRule.Match(Tractor, "", null));
    }

    [Fact]
    public void AHitchNamingNoCouplingTakesAnyTrailer()
    {
        Assert.Equal(CouplingMatch.Universal, CouplingRule.Match(null, "PhobosCoupling_Ball", null));
        Assert.Equal(CouplingMatch.Universal, CouplingRule.Match(new string[0], "PhobosCoupling_Ball", null));
    }
}
