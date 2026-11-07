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
    public void ReliefNeverClearsTheConditionSoTheOperationStillFollows()
    {
        Assert.Equal(0.3f, DeviceRule.AfterRelief(0.8f, 0.5f, 0.01f), 4);
        Assert.Equal(0.01f, DeviceRule.AfterRelief(0.3f, 0.5f, 0.01f), 4);
    }
}
