using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class MechLinkRuleTests
{
    [Fact]
    public void CostGoesBySize()
    {
        Assert.Equal(1, MechLinkRule.Cost(false, 2700, 4000));   // a light vehicle, a HUMVEE's mass
        Assert.Equal(2, MechLinkRule.Cost(false, 6400, 4000));   // a medium one, an L-ATV's
        Assert.Equal(3, MechLinkRule.Cost(true, 2000, 4000));    // heavy or tracked, whatever it weighs
        Assert.Equal(2, MechLinkRule.Cost(false, double.NaN, 4000));
    }

    [Fact]
    public void MechanoidsKeepPriorityAndTheNewestVehicleGoesFirst()
    {
        // Twelve bandwidth, mechanoids use eight: a 2 and a 1 fit, the next 2 does not, nor anything after it.
        Assert.Equal(2, MechLinkRule.Kept(12, 8, new[] { 2, 1, 2, 1 }));
        // Mechanoids using it all leave no vehicle in control.
        Assert.Equal(0, MechLinkRule.Kept(10, 10, new[] { 1 }));
        Assert.Equal(3, MechLinkRule.Kept(10, 0, new[] { 3, 3, 3 }));
    }
}
