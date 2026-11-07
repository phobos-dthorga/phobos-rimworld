using Phobos.MissilePatches.Rules;
using Xunit;

namespace Phobos.MissilePatches.Rules.Tests;

public class IrbmBudgetRuleTests
{
    [Fact]
    public void ThePlayersBudgetStaysWithinItsBounds()
    {
        Assert.Equal(IrbmBudgetRule.MinFrameBudgetMs, IrbmBudgetRule.FrameBudget(0f, false, 100f));
        Assert.Equal(IrbmBudgetRule.MaxFrameBudgetMs, IrbmBudgetRule.FrameBudget(1000f, false, 100f));
        Assert.Equal(12f, IrbmBudgetRule.FrameBudget(12f, false, 0f));
    }

    [Fact]
    public void WhileTheScreenIsWhiteTheBlastTakesMore()
    {
        Assert.Equal(IrbmBudgetRule.WhiteScreenBudgetMs, IrbmBudgetRule.FrameBudget(12f, true, 1f));
        Assert.Equal(12f, IrbmBudgetRule.FrameBudget(12f, true, IrbmBudgetRule.WhiteScreenSeconds + 0.1f));
        Assert.Equal(12f, IrbmBudgetRule.FrameBudget(12f, false, 1f));
        // A player who allows more than the white-screen budget keeps their own.
        Assert.Equal(50f, IrbmBudgetRule.FrameBudget(50f, true, 1f));
    }
}

public class ShelterRuleTests
{
    [Fact]
    public void AnyRockRoofSheltersUnderThinAndThickButNotUnderBuiltRoof()
    {
        Assert.True(ShelterRule.Sheltered(ShelterMode.AnyRockRoof, roofed: true, naturalRoof: true, thickRoof: false));
        Assert.True(ShelterRule.Sheltered(ShelterMode.AnyRockRoof, roofed: true, naturalRoof: true, thickRoof: true));
        Assert.False(ShelterRule.Sheltered(ShelterMode.AnyRockRoof, roofed: true, naturalRoof: false, thickRoof: false));
        Assert.False(ShelterRule.Sheltered(ShelterMode.AnyRockRoof, roofed: false, naturalRoof: false, thickRoof: false));
    }

    [Fact]
    public void ThickRockOnlyIsTheMissileModsOwnRule()
    {
        Assert.False(ShelterRule.Sheltered(ShelterMode.ThickRockOnly, true, true, false));
        Assert.True(ShelterRule.Sheltered(ShelterMode.ThickRockOnly, true, true, true));
        Assert.False(ShelterRule.Sheltered(ShelterMode.Nothing, true, true, true));
    }

    [Fact]
    public void AWarheadThatPenetratesThickRoofIgnoresEveryShelter()
    {
        Assert.Equal(ShelterMode.Nothing, ShelterRule.Effective(ShelterMode.AnyRockRoof, true));
        Assert.Equal(ShelterMode.AnyRockRoof, ShelterRule.Effective(ShelterMode.AnyRockRoof, false));
    }
}

public class HitRuleTests
{
    static ThingHit Hit(bool pawn = false, bool building = true, bool harms = true, bool hp = true, bool destroyable = true,
        int hitPoints = 300, float amount = 10000f, bool rubble = false, bool fragments = false) =>
        HitRule.Choose(pawn, building, harms, hp, destroyable, hitPoints, amount, rubble, fragments);

    [Fact]
    public void WhatTheDamageWouldDestroyVanishes()
    {
        Assert.Equal(ThingHit.Vanish, Hit());
        Assert.Equal(ThingHit.Vanish, Hit(building: false));
    }

    [Fact]
    public void PawnsAndWhatCannotBeDestroyedTakeTheDamage()
    {
        Assert.Equal(ThingHit.Damage, Hit(pawn: true));
        Assert.Equal(ThingHit.Damage, Hit(harms: false));
        Assert.Equal(ThingHit.Damage, Hit(hp: false));
        Assert.Equal(ThingHit.Damage, Hit(destroyable: false));
        Assert.Equal(ThingHit.Damage, Hit(hitPoints: 3000, amount: 2000f));
    }

    [Fact]
    public void RubbleAndLiveFragmentsChangeOnlyBuildings()
    {
        Assert.Equal(ThingHit.KillFinalize, Hit(rubble: true));
        Assert.Equal(ThingHit.Vanish, Hit(building: false, rubble: true));
        Assert.Equal(ThingHit.Damage, Hit(fragments: true));
        Assert.Equal(ThingHit.Damage, Hit(rubble: true, fragments: true));
        Assert.Equal(ThingHit.Vanish, Hit(building: false, fragments: true));
    }
}

public class SweepOrderRuleTests
{
    [Fact]
    public void TheCentreGoesFirstAndTheCornersLast()
    {
        long centre = SweepOrderRule.Rank(125, 125, 125, 125);
        long near = SweepOrderRule.Rank(128, 121, 125, 125);
        long corner = SweepOrderRule.Rank(0, 249, 125, 125);
        Assert.Equal(0, centre);
        Assert.True(centre < near);
        Assert.True(near < corner);
    }
}
