using Phobos.Factory.Rules;
using Xunit;

namespace Phobos.Factory.Rules.Tests;

public class OutputRuleTests
{
    [Fact]
    public void AnIdenticalGunLeavesNoRoomForTheNext() =>
        Assert.False(OutputRule.BatchFits(stackCount: 1, batch: 1, stackLimit: 1));

    [Fact]
    public void AFullStackTakesNoBatch()
    {
        // VFE's refinery: 25 chemfuel per batch, limit 150 = the stack limit, so Pipe System never waits.
        Assert.False(OutputRule.BatchFits(stackCount: 150, batch: 25, stackLimit: 150));
        Assert.False(OutputRule.BatchFits(stackCount: 126, batch: 25, stackLimit: 150));
        Assert.True(OutputRule.BatchFits(stackCount: 125, batch: 25, stackLimit: 150));
    }

    [Theory]
    [InlineData(301, 75, true)] // A grizzly's meat: one more than a four-stack deep hopper can hold.
    [InlineData(140, 75, true)] // A human's meat.
    [InlineData(76, 75, true)]
    [InlineData(75, 75, false)]
    [InlineData(23, 75, false)]
    public void ABatchAboveOneStackIsOversized(int batch, int stackLimit, bool oversized) =>
        Assert.Equal(oversized, OutputRule.IsOversized(batch, stackLimit));

    [Theory]
    [InlineData(4, true)] // The deep factory hopper.
    [InlineData(1, false)] // VFE Factory's own hopper, and bare floor.
    public void OnlyACellHoldingSeveralStacksTakesANewStack(int maxItemsInCell, bool fills) =>
        Assert.Equal(fills, OutputRule.FillsWholeCell(maxItemsInCell));

    [Fact]
    public void AGeneratedBillWaitsAtANumberTheItemCanReach()
    {
        Assert.Equal(1f, OutputRule.WaitLimit(75f, stackLimit: 1));
        Assert.Equal(25f, OutputRule.WaitLimit(75f, stackLimit: 25));
        Assert.Equal(75f, OutputRule.WaitLimit(75f, stackLimit: 500));
    }

    [Fact]
    public void AWaitLimitIsNeverZero() => Assert.Equal(1f, OutputRule.WaitLimit(0f, stackLimit: 75));

    [Theory]
    [InlineData(1f, 10, 1, 9f)] // A hand-held radio: the hopper fills to a stack of ten.
    [InlineData(1f, 1, 1, 1f)] // A gun never stacks: VFE's own limit stands.
    [InlineData(1f, 2, 1, 1f)] // Two to a stack: VFE's limit already fills it.
    [InlineData(30f, 25, 1, 30f)] // A limit already higher is never lowered.
    [InlineData(1f, 5, 5, 1f)] // A batch as big as a stack: nothing to raise.
    public void AMachiningBillFillsAStackOfWhatStacks(float vfeLimit, int stackLimit, int batch, float limit) =>
        Assert.Equal(limit, OutputRule.MachiningLimit(vfeLimit, stackLimit, batch));

    [Fact]
    public void ARaisedMachiningLimitNeverOverfillsTheStack()
    {
        // Pipe System merges while the stack holds no more than the limit, so the fullest stack is the limit plus one batch.
        float limit = OutputRule.MachiningLimit(1f, stackLimit: 10, batch: 1);
        Assert.True(OutputRule.BatchFits((int)limit, batch: 1, stackLimit: 10));
        Assert.False(OutputRule.BatchFits((int)limit + 1, batch: 1, stackLimit: 10));
    }
}
