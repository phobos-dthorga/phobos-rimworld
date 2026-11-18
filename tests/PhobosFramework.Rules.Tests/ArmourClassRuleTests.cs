using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ArmourClassRuleTests
{
    // Skin, Splinter and Rifle on each scale, as ArmourClasses.xml writes them.
    static readonly (double, double)[] Millimetres = { (2, 4), (5, 20), (8, 40) };
    static readonly (double, double)[] Ratings = { (0.10, 0.08), (0.25, 0.20), (0.45, 0.35) };

    [Fact]
    public void SkinUnderCombatExtendedIsFound() => Assert.Equal(0, ArmourClassRule.Find(2, 4, Millimetres));

    [Fact]
    public void RifleWithoutItIsFound() => Assert.Equal(2, ArmourClassRule.Find(0.45, 0.35, Ratings));

    [Fact]
    public void AFloatThatIsNearlyTheFigureStillMatches() => Assert.Equal(1, ArmourClassRule.Find(0.25000001f, 0.2f, Ratings));

    [Fact]
    public void BothFiguresMustMatch() => Assert.Equal(-1, ArmourClassRule.Find(8, 4, Millimetres));

    [Fact]
    public void AnotherAuthorsFigureIsNoClass() => Assert.Equal(-1, ArmourClassRule.Find(0.4, 0.26, Ratings));

    [Fact]
    public void NoArmourIsNoClass() => Assert.Equal(-1, ArmourClassRule.Find(0, 0, Ratings));

    [Fact]
    public void ARatingIsNotReadAsMillimetres() => Assert.Equal(-1, ArmourClassRule.Find(0.10, 0.08, Millimetres));
}
