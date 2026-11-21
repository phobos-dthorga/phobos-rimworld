using Phobos.Radiation.Rules;
using Xunit;

namespace Phobos.Medical.Rules.Tests.Radiation;

public class ContaminationRuleTests
{
    [Fact]
    public void DustKeepsItsStrengthForADay() => Assert.Equal(1, ContaminationRule.DustLeft(0.5), 9);

    [Fact]
    public void DustFadesToATenthAfterAWeek() => Assert.InRange(ContaminationRule.DustLeft(7), 0.08, 0.12);

    [Fact]
    public void HalfAnInsideDoseIsDeliveredInAHalfLife() => Assert.Equal(0.5, ContaminationRule.Delivered(110, 110), 9);

    [Fact]
    public void NothingIsDeliveredInNoTime() => Assert.Equal(0, ContaminationRule.Delivered(0, 110), 9);

    [Fact]
    public void AShorterHalfLifeDeliversItSooner() => Assert.True(ContaminationRule.Delivered(10, 30) > ContaminationRule.Delivered(10, 110));

    [Fact]
    public void ASuitLeavesNoSkinBare() => Assert.Equal(0, ContaminationRule.Bare(1), 9);

    [Fact]
    public void ChildrensThyroidsAreFarMoreAtRisk() =>
        Assert.True(ContaminationRule.ThyroidCancerChance(1, 8) > 4 * ContaminationRule.ThyroidCancerChance(1, 30));

    [Fact]
    public void ThyroidCancerTakesYears() => Assert.InRange(ContaminationRule.ThyroidCancerYears(0.5), 4, 10);
}
