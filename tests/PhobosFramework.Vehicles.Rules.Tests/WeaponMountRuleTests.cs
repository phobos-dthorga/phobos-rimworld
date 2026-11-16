using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class WeaponMountRuleTests
{
    [Fact]
    public void AFreeWeaponGetsAMount() => Assert.True(WeaponMountRule.NeedsMount(authorRequirement: false, attachedToAnother: false, fixedMount: false));

    [Fact]
    public void AnAuthorsOwnRequirementStands() => Assert.False(WeaponMountRule.NeedsMount(authorRequirement: true, attachedToAnother: false, fixedMount: false));

    [Fact]
    public void AWeaponOnAnotherFollowsItsParent() => Assert.False(WeaponMountRule.NeedsMount(authorRequirement: false, attachedToAnother: true, fixedMount: false));

    [Fact]
    public void AFixedSmokeLauncherIsLeftAlone() => Assert.False(WeaponMountRule.NeedsMount(authorRequirement: false, attachedToAnother: false, fixedMount: true));

    [Fact]
    public void TheVehiclesOwnTurretPartIsUsed()
    {
        string[] parts = { "Engine", "maInTurret", "LeftTrack" };
        Assert.Equal(1, WeaponMountRule.AuthorMount(parts, new[] { "MainTurret", "PhobosPylons" }));
    }

    [Fact]
    public void TheFirstListedMountWins()
    {
        string[] parts = { "PhobosPylons", "MainTurret" };
        Assert.Equal(1, WeaponMountRule.AuthorMount(parts, new[] { "MainTurret", "PhobosPylons" }));
    }

    [Fact]
    public void NoMountPartMeansOneIsAdded() => Assert.Equal(-1, WeaponMountRule.AuthorMount(new[] { "Engine", "TurretERAPlating" }, new[] { "MainTurret" }));
}
