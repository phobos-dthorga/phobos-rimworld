using Phobos.Medical.Rules;
using Xunit;

namespace Phobos.Medical.Rules.Tests;

public class DecompressionRuleTests
{
    [Fact]
    public void ANeedleOnlyHelpsPressureWorthRelieving()
    {
        Assert.Equal(DeviceRefusal.None, DeviceRule.Decompression(true));
        Assert.Equal(DeviceRefusal.NotNeeded, DeviceRule.Decompression(false));
    }

    [Fact]
    public void AFluidsKitHelpsShockWithPoorerFluidsThanItsOwn()
    {
        Assert.Equal(DeviceRefusal.NotNeeded, DeviceRule.Fluids(true, 0.7f, 0.9f));
        Assert.Equal(DeviceRefusal.None, DeviceRule.Fluids(true, 0f, 0.9f));
        Assert.Equal(DeviceRefusal.None, DeviceRule.Fluids(true, 0.5f, 0.9f));
        Assert.Equal(DeviceRefusal.NotNeeded, DeviceRule.Fluids(true, 0.9f, 0.9f));
        Assert.Equal(DeviceRefusal.NotNeeded, DeviceRule.Fluids(false, 0f, 0.9f));
    }

    [Fact]
    public void ReliefNeverClearsTheConditionSoTheOperationStillFollows()
    {
        Assert.Equal(0.3f, DeviceRule.AfterRelief(0.8f, 0.5f, 0.01f), 4);
        Assert.Equal(0.01f, DeviceRule.AfterRelief(0.3f, 0.5f, 0.01f), 4);
    }
}
