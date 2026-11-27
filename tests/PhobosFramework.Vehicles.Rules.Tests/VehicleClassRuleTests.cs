using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The vehicle classes (Framework 0.369.0; the owner's cards of 28 Nov 2026).</summary>
public class VehicleClassRuleTests
{
    [Fact]
    public void ArmedAsBuiltIsMilitaryWhateverItsUpgrades()
    {
        Assert.Equal(VehicleClass.Military, VehicleClassRule.Classify(EmpHardening.ByClass, armedAsBuilt: true, armedByUpgrade: false));
        Assert.Equal(VehicleClass.Military, VehicleClassRule.Classify(EmpHardening.ByClass, armedAsBuilt: true, armedByUpgrade: true));
    }

    [Fact]
    public void AGunOnlyByUpgradeIsItsOwnClassAndNotMilitary()
    {
        // The L-ATV, the MaxxPro, the Humvee and our service trucks with gun kits.
        Assert.Equal(VehicleClass.ArmedByUpgrade, VehicleClassRule.Classify(EmpHardening.ByClass, armedAsBuilt: false, armedByUpgrade: true));
        Assert.Equal(VehicleClass.Civilian, VehicleClassRule.Classify(EmpHardening.ByClass, armedAsBuilt: false, armedByUpgrade: false));
    }

    [Fact]
    public void ALoadoutsOwnRatingComesFirst()
    {
        Assert.Equal(VehicleClass.Military, VehicleClassRule.Classify(EmpHardening.Military, armedAsBuilt: false, armedByUpgrade: false));
        Assert.Equal(VehicleClass.Mechanical, VehicleClassRule.Classify(EmpHardening.Mechanical, armedAsBuilt: true, armedByUpgrade: true));
        Assert.Equal(VehicleClass.Civilian, VehicleClassRule.Classify(EmpHardening.Civilian, armedAsBuilt: false, armedByUpgrade: true));
    }
}
