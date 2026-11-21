using System.Linq;
using Phobos.Radiation.Rules;
using Xunit;

namespace Phobos.Medical.Rules.Tests.Radiation;

public class ShieldingRuleTests
{
    static readonly string[] Body = { "Torso", "Neck", "Shoulders", "Arms", "Legs" };

    [Fact]
    public void NothingWornPassesEverything() => Assert.Equal(1, ShieldingRule.Pass(Enumerable.Empty<WornLayer>()), 6);

    [Fact]
    public void TheMarrowSharesAddToOne() => Assert.Equal(1, ShieldingRule.MarrowShare.Values.Sum(), 6);

    [Fact]
    public void SixHundredAndFiftyKilogramsASquareMetreCutToATenth() => Assert.Equal(0.1, ShieldingRule.Transmission(650), 6);

    [Fact]
    public void AParkaBarelyShields()
    {
        double pass = ShieldingRule.Pass(new[] { new WornLayer(3, new[] { "Torso", "Neck", "Shoulders", "Arms" }) });
        Assert.InRange(pass, 0.98, 1.0);
    }

    [Fact]
    public void CataphractArmourCutsAboutASixth()
    {
        double pass = ShieldingRule.Pass(new[] { new WornLayer(80, Body) });
        Assert.InRange(1 - pass, 0.12, 0.2);
    }

    [Fact]
    public void LayersAddUp()
    {
        double one = ShieldingRule.Pass(new[] { new WornLayer(30, Body) });
        double two = ShieldingRule.Pass(new[] { new WornLayer(30, Body), new WornLayer(30, Body) });
        Assert.True(two < one);
    }

    [Fact]
    public void TheTorsoCountsMoreThanTheLegs()
    {
        double vest = ShieldingRule.Pass(new[] { new WornLayer(11, new[] { "Torso" }) });
        double leggings = ShieldingRule.Pass(new[] { new WornLayer(11, new[] { "Legs" }) });
        Assert.True(vest < leggings);
    }

    [Fact]
    public void AGarmentOverUnknownGroupsShieldsNothing() => Assert.Equal(1, ShieldingRule.Pass(new[] { new WornLayer(10, new[] { "Tail" }) }), 6);
}
