using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class CombatExtendedRuleTests
{
    [Fact]
    public void WithoutCombatExtendedTheVehicleFrameworkWeightStands() =>
        Assert.Equal(0.3, CombatExtendedRules.HitWeight(0.3, double.NaN, ceActive: false));

    [Fact]
    public void UnderCombatExtendedARarelyHitPartGetsALargeWeight()
    {
        // CE sorts by a random draw times the weight, smallest first: a part meant to be hit rarely needs a large weight.
        Assert.Equal(1 / 0.3, CombatExtendedRules.HitWeight(0.3, double.NaN, ceActive: true), 6);
        Assert.Equal(5, CombatExtendedRules.HitWeight(0.2, 5, ceActive: true));
        Assert.Equal(0.2, CombatExtendedRules.HitWeight(0.2, 5, ceActive: false));
    }

    [Fact]
    public void BlastPenetrationFollowsTheScaleInUse()
    {
        Assert.Equal(0.9f, CombatExtendedRules.BlastPenetrationForDamage(60, ceActive: false), 3);
        Assert.Equal(60f * 62f / 156f, CombatExtendedRules.BlastPenetrationForDamage(60, ceActive: true), 3);
        Assert.True(CombatExtendedRules.BlastPenetrationForDamage(60, ceActive: true) > 20f, "about 24 MPa, enough to matter to armour");
    }
}
