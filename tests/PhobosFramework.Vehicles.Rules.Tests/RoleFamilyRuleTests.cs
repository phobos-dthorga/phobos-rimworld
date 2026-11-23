using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Numbered roles roll up into their base role (Framework 0.256.0).</summary>
public class RoleFamilyRuleTests
{
    [Theory]
    [InlineData("Engine1", "Engine")]
    [InlineData("Engine12", "Engine")]
    [InlineData("FuelTank2", "FuelTank")]
    public void ANumberedRoleHasABase(string role, string expected) => Assert.Equal(expected, RoleFamilyRule.BaseOf(role));

    [Theory]
    [InlineData("Engine")]
    [InlineData("12")]
    [InlineData("")]
    public void OthersHaveNone(string role) => Assert.Null(RoleFamilyRule.BaseOf(role));

    [Fact]
    public void MembersComeInNumberOrder()
    {
        var families = RoleFamilyRule.Families(new[] { "Engine10", "Engine2", "Engine1", "Wheels", "Radiator" });
        Assert.Single(families);
        Assert.Equal(new[] { "Engine1", "Engine2", "Engine10" }, families["Engine"]);
    }

    [Fact]
    public void OneMemberIsStillAFamily() => Assert.Equal(new[] { "Engine1" }, RoleFamilyRule.Families(new[] { "Engine1" })["Engine"]);
}
