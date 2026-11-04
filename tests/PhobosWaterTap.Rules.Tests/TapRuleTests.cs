using Phobos.WaterTap.Rules;
using Xunit;

namespace Phobos.WaterTap.Rules.Tests;

public class TapRuleTests
{
    /// <summary>A tap with room, a water tower and plenty of clean water. Each test changes one thing.</summary>
    private static readonly TapReading Ready = new(
        Held: 3,
        Target: 10,
        Blocked: false,
        HeldIsDifferentWater: false,
        HasWaterTower: true,
        Contaminated: false,
        WaterStored: 500f,
        WaterPerBottle: 0.5f);

    [Fact]
    public void A_tap_with_room_and_water_fills() => Assert.Equal(TapState.Filling, TapRule.Decide(Ready));

    [Fact]
    public void Target_zero_is_off_and_reports_no_other_problem() =>
        Assert.Equal(TapState.SwitchedOff, TapRule.Decide(Ready with { Target = 0, HasWaterTower = false, Blocked = true }));

    [Fact]
    public void A_full_tap_reports_no_water_problem_because_it_needs_none() =>
        Assert.Equal(TapState.Full, TapRule.Decide(Ready with { Held = 10, HasWaterTower = false, WaterStored = 0f }));

    [Fact]
    public void A_target_lowered_below_what_is_held_counts_as_full() =>
        Assert.Equal(TapState.Full, TapRule.Decide(Ready with { Held = 8, Target = 5 }));

    [Fact]
    public void Another_item_on_the_cell_blocks() =>
        Assert.Equal(TapState.Blocked, TapRule.Decide(Ready with { Blocked = true }));

    [Fact]
    public void No_tower_is_told_apart_from_an_empty_tower()
    {
        Assert.Equal(TapState.NoWaterTower, TapRule.Decide(Ready with { HasWaterTower = false, WaterStored = 0f }));
        Assert.Equal(TapState.Dry, TapRule.Decide(Ready with { WaterStored = 0.4f }));
    }

    [Fact]
    public void Contaminated_water_is_never_bottled() =>
        Assert.Equal(TapState.Contaminated, TapRule.Decide(Ready with { Contaminated = true }));

    [Fact]
    public void Different_water_waits_for_the_stack_to_go() =>
        Assert.Equal(TapState.DifferentWater, TapRule.Decide(Ready with { HeldIsDifferentWater = true }));

    [Fact]
    public void One_bottle_a_fill() => Assert.Equal(1, TapRule.BottlesToMake(Ready, 1));

    [Fact]
    public void Never_makes_more_than_the_target_allows() =>
        Assert.Equal(2, TapRule.BottlesToMake(Ready with { Held = 8 }, 5));

    [Fact]
    public void Never_makes_more_than_the_water_can_fill() =>
        Assert.Equal(3, TapRule.BottlesToMake(Ready with { WaterStored = 1.7f }, 5));

    [Fact]
    public void Makes_nothing_unless_filling()
    {
        Assert.Equal(0, TapRule.BottlesToMake(Ready with { Contaminated = true }, 5));
        Assert.Equal(0, TapRule.BottlesToMake(Ready with { Held = 10 }, 5));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(7, 7)]
    [InlineData(10, 10)]
    [InlineData(11, 10)]
    public void Target_stays_within_one_stack(int asked, int expected) =>
        Assert.Equal(expected, TapRule.ClampTarget(asked, stackLimit: 10));
}
