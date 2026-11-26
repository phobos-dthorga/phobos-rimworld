using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ClassFamilyRuleTests
{
    static ClassFamily Jets() => new(new[] { "SharedFlight", "TestJetA.Flight", "TestJetB.Flight", "TestJetC.Flight." });

    [Fact]
    public void AnObjectSavedUnderAnotherCopyMovesToItsOwnersCopy() =>
        Assert.Equal("TestJetA.Flight.VtolVehiclePawn", Jets().Rehome("TestJetC.Flight.VtolVehiclePawn", "TestJetA.Flight.VtolVehiclePawn"));

    [Fact]
    public void AnObjectSavedUnderTheOldSharedNamespaceMovesToItsOwnersCopy() =>
        Assert.Equal("TestJetB.Flight.VtolVehicleTurret", Jets().Rehome("SharedFlight.VtolVehicleTurret", "TestJetB.Flight.VtolVehiclePawn"));

    [Fact]
    public void AnObjectAlreadyInItsOwnersCopyStays() =>
        Assert.Null(Jets().Rehome("TestJetA.Flight.VtolVehicleTurret", "TestJetA.Flight.VtolVehiclePawn"));

    [Fact]
    public void AnOwnerOutsideTheFamilyLeavesTheObjectAlone() =>
        Assert.Null(Jets().Rehome("TestJetC.Flight.VtolVehiclePawn", "Vehicles.VehiclePawn"));

    [Fact]
    public void AClassOutsideTheFamilyIsLeftAlone()
    {
        Assert.Null(Jets().Rehome("Vehicles.VehicleTurret", "TestJetA.Flight.VtolVehiclePawn"));
        Assert.False(Jets().Holds("TestJetA.Patch_GetGizmos"));
    }

    [Fact]
    public void ANamespaceNestedBelowAMemberIsNotTheMember()
    {
        Assert.False(Jets().Holds("TestJetA.Flight.Inner.Thing"));
        Assert.Null(Jets().Rehome("TestJetA.Flight.Inner.Thing", "TestJetB.Flight.VtolVehiclePawn"));
    }

    [Fact]
    public void ANamespaceAloneIsNotAClass()
    {
        Assert.False(Jets().Holds("TestJetA.Flight."));
        Assert.False(Jets().Holds(null));
        Assert.False(Jets().Holds(""));
    }

    [Fact]
    public void NamespacesAreKeptOnceEachWithTheirClosingDot()
    {
        var family = new ClassFamily(new[] { "TestJetA.Flight", "TestJetA.Flight.", "" });
        Assert.Equal(new[] { "TestJetA.Flight." }, family.Prefixes);
    }
}
