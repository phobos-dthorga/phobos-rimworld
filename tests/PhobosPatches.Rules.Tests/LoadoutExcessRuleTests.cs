using System.Collections.Generic;
using Phobos.CombatExtendedPatches.Rules;
using Xunit;

namespace Phobos.Patches.Rules.Tests;

/// <summary>Combat Extended's excess rule for one kind (Patches 0.29.1): the sidearm loop of 21 Nov 2026 and its neighbours.</summary>
public class LoadoutExcessRuleTests
{
    static List<KeyValuePair<string, int>> Carried(params (string Kind, int Count)[] items)
    {
        var list = new List<KeyValuePair<string, int>>();
        foreach ((string kind, int count) in items) list.Add(new KeyValuePair<string, int>(kind, count));
        return list;
    }

    // Carlotta's case: an M250 in hand, a second fetched into her pack; her ad-hoc loadout counts the gun in hand as one, and drops nothing
    // it does not list.
    static readonly LoadoutSlotView[] Soldier =
    {
        new("Weapon_GrenadeFrag", null, 3),
        new("CE_Weapon_SatchelCharge", null, 1),
        new("CE_Gun_MTwoFifty", null, 1),
    };

    [Fact]
    public void ASecondCopyOfTheGunInHandIsDropped() =>
        Assert.True(LoadoutExcessRule.WouldDrop(Carried(("CE_Gun_MTwoFifty", 2)), Soldier, "CE_Gun_MTwoFifty", null, dropUndefined: false));

    [Fact]
    public void TheOnlyCopyIsKept() =>
        Assert.False(LoadoutExcessRule.WouldDrop(Carried(("CE_Gun_MTwoFifty", 1)), Soldier, "CE_Gun_MTwoFifty", null, dropUndefined: false));

    [Fact]
    public void ASidearmTheLoadoutDoesNotNameIsKept() =>
        Assert.False(LoadoutExcessRule.WouldDrop(Carried(("CE_Gun_MTwoFifty", 1), ("MeleeWeapon_PlasmaSword", 1)), Soldier, "MeleeWeapon_PlasmaSword", null,
            dropUndefined: false));

    [Fact]
    public void ALoadoutThatDropsWhatItDoesNotNameDropsIt() =>
        Assert.True(LoadoutExcessRule.WouldDrop(Carried(("MeleeWeapon_PlasmaSword", 1)), Soldier, "MeleeWeapon_PlasmaSword", null, dropUndefined: true));

    [Fact]
    public void AHoldRecordAllowingTwoKeepsTheSecond() =>
        Assert.False(LoadoutExcessRule.WouldDrop(Carried(("CE_Gun_MTwoFifty", 2)), Soldier, "CE_Gun_MTwoFifty", 1, dropUndefined: false));

    [Fact]
    public void AHoldRecordAllowingNoneOfTheExtraDropsIt() =>
        Assert.True(LoadoutExcessRule.WouldDrop(Carried(("CE_Gun_MTwoFifty", 3)), Soldier, "CE_Gun_MTwoFifty", 1, dropUndefined: false));

    [Fact]
    public void AGenericSlotCoveringTheKindKeepsWhatItWants()
    {
        var slots = new[] { new LoadoutSlotView(null, new[] { "MealSimple", "MealFine" }, 5) };
        Assert.False(LoadoutExcessRule.WouldDrop(Carried(("MealSimple", 3), ("MealFine", 2)), slots, "MealFine", null, dropUndefined: true));
        Assert.True(LoadoutExcessRule.WouldDrop(Carried(("MealSimple", 3), ("MealFine", 3)), slots, "MealFine", null, dropUndefined: true));
    }

    [Fact]
    public void AKindNotCarriedIsNeverDropped() =>
        Assert.False(LoadoutExcessRule.WouldDrop(Carried(("Weapon_GrenadeFrag", 3)), Soldier, "CE_Gun_MTwoFifty", null, dropUndefined: true));
}
