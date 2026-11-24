using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class RemoteGunRuleTests
{
    [Fact]
    public void WithoutTheKitAGunFiresAsItsAuthorBuiltIt() => Assert.True(RemoteGunRule.MayFire(kitFitted: false, picture: false, fireAlone: false));

    [Fact]
    public void WithTheKitItWaitsForAPicture()
    {
        Assert.False(RemoteGunRule.MayFire(kitFitted: true, picture: false, fireAlone: false));
        Assert.True(RemoteGunRule.MayFire(kitFitted: true, picture: true, fireAlone: false));
    }

    [Fact]
    public void TheSettingLetsItFireAlone() => Assert.True(RemoteGunRule.MayFire(kitFitted: true, picture: false, fireAlone: true));

    [Fact]
    public void OurStationFollowsTheRuleWithoutTheKit()
    {
        // Framework 0.285.0 (the owner: "Picture link, or the setting", one rule everywhere): no kit, no radio, no picture.
        Assert.False(RemoteGunRule.MayFire(kitFitted: false, ours: true, picture: false, fireAlone: false));
        Assert.True(RemoteGunRule.MayFire(kitFitted: false, ours: true, picture: false, fireAlone: true));
        Assert.True(RemoteGunRule.MayFire(kitFitted: true, ours: true, picture: true, fireAlone: false));
        // Another author's gun on a vehicle without the kit is untouched.
        Assert.True(RemoteGunRule.MayFire(kitFitted: false, ours: false, picture: false, fireAlone: false));
    }
}
