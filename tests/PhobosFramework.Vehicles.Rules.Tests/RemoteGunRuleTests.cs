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
}
