using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class CruiseRuleTests
{
    const double NoOxygenLine = double.NaN;

    [Fact]
    public void AtItsDesignHeightAnAircraftFliesAsItsAuthorMadeIt()
    {
        foreach (CruiseLift lift in new[] { CruiseLift.Rotor, CruiseLift.Jet, CruiseLift.Propeller })
        {
            (double fuel, double speed) = CruiseRule.Factors(lift, 4600, 4600);
            Assert.Equal(1, fuel, 9);
            Assert.Equal(1, speed, 9);
        }
        CruiseLeg leg = CruiseRule.Leg(CruiseLift.Jet, 7000, 11000, 1, 1, NoOxygenLine, CruiseRule.Floor(500, double.NaN));
        Assert.Equal(CruiseLimit.Design, leg.Limit);
        Assert.Equal(7000, leg.HeightMetres, 6);
        Assert.Equal(1, leg.FuelFactor, 9);
    }

    [Fact]
    public void AJetFlyingLowWithoutOxygenMatchesTheFiguresBuiltBeforeIt()
    {
        // The record's worked table: a jet built for 7,000 m flown at 3,810 m burns 1.19 times and flies 0.84 times; avionics A14's fixed
        // figures were 1.2 and 0.85, so nothing installed changes.
        CruiseLeg leg = CruiseRule.Leg(CruiseLift.Jet, 7000, 11000, 1, 1, CruiseRule.DefaultOxygenLineMetres, CruiseRule.Floor(0, double.NaN));
        Assert.Equal(CruiseLimit.Oxygen, leg.Limit);
        Assert.Equal(3810, leg.HeightMetres, 6);
        Assert.Equal(1.19, leg.FuelFactor, 2);
        Assert.Equal(0.84, leg.SpeedFactor, 2);
    }

    [Fact]
    public void TheWorkedTableHolds()
    {
        // A tiltrotor built for 4,600 m: troops without oxygen bring it to 3,050 m (1.04 and 0.96); at 1,000 m, 1.10 and 0.91.
        (double fuel, double speed) = CruiseRule.Factors(CruiseLift.Propeller, 4600, 3050);
        Assert.Equal(1.04, fuel, 2);
        Assert.Equal(0.96, speed, 2);
        (fuel, speed) = CruiseRule.Factors(CruiseLift.Propeller, 4600, 1000);
        Assert.Equal(1.10, fuel, 2);
        Assert.Equal(0.91, speed, 2);
        // A helicopter built for 1,000 m crossing a range at 2,500 m, and the owner's highest ground at 4,300 m: speed unchanged.
        (fuel, speed) = CruiseRule.Factors(CruiseLift.Rotor, 1000, 2500);
        Assert.Equal(1.08, fuel, 2);
        Assert.Equal(1, speed, 9);
        (fuel, _) = CruiseRule.Factors(CruiseLift.Rotor, 1000, 4300);
        Assert.Equal(1.18, fuel, 2);
    }

    [Fact]
    public void TheCeilingFitsThePublishedOneAndFallsWithAnEngineOut()
    {
        // The Osprey: 7,620 m published; the rule gives it back at full weight, and about 4,000 m on one engine (the record's check).
        Assert.Equal(7620, CruiseRule.CeilingNow(7620, 1, 1), 0);
        Assert.InRange(CruiseRule.CeilingNow(7620, 1, 0.5), 3950, 4050);
        Assert.InRange(CruiseRule.CeilingNow(7620, 0.8, 0.5), 5700, 5900);
        // Lighter climbs higher; no engines, no climb.
        Assert.True(CruiseRule.CeilingNow(7620, 0.7, 1) > 7620);
        Assert.Equal(0, CruiseRule.CeilingNow(7620, 1, 0));
        // Nothing is ever flown above the troposphere's top.
        Assert.Equal(CruiseRule.HighestMetres, CruiseRule.CeilingNow(15000, 0.5, 1));
    }

    [Fact]
    public void TheWeightToComeDownToClimbsExactlyToTheHeight()
    {
        double share = CruiseRule.WeightShareFor(5000, 7620, 0.5);
        Assert.InRange(share, 0.5, 1);
        Assert.Equal(5000, CruiseRule.CeilingNow(7620, share, 0.5), 0);
    }

    [Fact]
    public void GroundAboveTheCeilingRefusesTheLeg()
    {
        // One engine, full weight: about 4,000 m. The owner's highest tile, 3,968 m, mountainous, wants 4,568 m.
        double floor = CruiseRule.Floor(3500, 3968);
        Assert.Equal(4568, floor, 6);
        CruiseLeg leg = CruiseRule.Leg(CruiseLift.Propeller, 4600, 7620, 1, 0.5, NoOxygenLine, floor);
        Assert.False(leg.Flyable);
        Assert.Equal(CruiseLimit.Refused, leg.Limit);
        Assert.InRange(leg.ShortMetres, 500, 650);
        // Light enough, it goes over, raised by the ground.
        CruiseLeg light = CruiseRule.Leg(CruiseLift.Propeller, 4600, 7620, 0.7, 0.5, NoOxygenLine, floor);
        Assert.True(light.Flyable);
    }

    [Fact]
    public void TheGroundRaisesTheLegAboveTheOxygenLine()
    {
        // A helicopter whose people have no oxygen still climbs over a range: it is never flown below the ground and its clearance.
        double floor = CruiseRule.Floor(double.NaN, 3700);
        CruiseLeg leg = CruiseRule.Leg(CruiseLift.Rotor, 1000, 4500, 0.8, 1, 3050, floor);
        Assert.Equal(CruiseLimit.Ground, leg.Limit);
        Assert.Equal(4300, leg.HeightMetres, 6);
        Assert.True(leg.FuelFactor > 1);
    }

    [Fact]
    public void ClearanceIsDoubledOverMountains()
    {
        Assert.Equal(300, CruiseRule.Floor(0, double.NaN), 9);
        Assert.Equal(1300, CruiseRule.Floor(1000, double.NaN), 9);
        Assert.Equal(1600, CruiseRule.Floor(1000, 1000), 9);
        // Lower mountains than the ordinary high ground do not raise it.
        Assert.Equal(2300, CruiseRule.Floor(2000, 1000), 9);
        // Below the sea reads as the sea.
        Assert.Equal(300, CruiseRule.Floor(-50, double.NaN), 9);
    }

    [Fact]
    public void FuelOverTheSameGroundIsFuelOverSpeed()
    {
        CruiseLeg leg = CruiseRule.Leg(CruiseLift.Jet, 7000, 11000, 1, 1, 3810, 300);
        Assert.Equal(leg.FuelFactor / leg.SpeedFactor, leg.FuelPerDistance, 9);
        Assert.Equal(1, CruiseLeg.AsAuthored(1000).FuelPerDistance, 9);
    }
}
