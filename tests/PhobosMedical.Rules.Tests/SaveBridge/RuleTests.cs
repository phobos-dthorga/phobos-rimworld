using Phobos.MedicalSaveBridge.Rules;
using Xunit;

namespace Phobos.MedicalSaveBridge.Rules.Tests;

public class PartIndexRuleTests
{
    [Fact]
    public void ABodyWithNoNewPartsNeedsNoMap() =>
        Assert.Null(PartIndexRule.OldToNewIndices(new[] { false, false, false }));

    [Fact]
    public void OldPartsKeepTheirOrderAndSkipTheNewOnes()
    {
        // Today's order: core, old, NEW, NEW, old, NEW, old. The save numbered the old parts 0..3.
        bool[] isNew = { false, false, true, true, false, true, false };
        int[]? map = PartIndexRule.OldToNewIndices(isNew);
        Assert.NotNull(map);
        Assert.Equal(new[] { 0, 1, 4, 6 }, map);
    }

    [Fact]
    public void TheHumanBodyAfterMoreInjuries()
    {
        // The layout seen in the owner's colony on 3 November 2026, by matching 4,351 hediffs between saves written before and
        // after More Injuries: 64 old parts become 72. The spinal cord lands right after the spine, two arteries close each leg, and
        // the abdomen with its two intestines closes the torso, so those last five parts come after every old part.
        var isNew = new bool[72];
        isNew[5] = true;                                    // SpinalCord
        isNew[56] = isNew[57] = true;                       // the first leg's arteries
        isNew[67] = isNew[68] = true;                       // the second leg's arteries
        isNew[69] = isNew[70] = isNew[71] = true;           // Abdomen, SmallIntestine, LargeIntestine
        int[]? map = PartIndexRule.OldToNewIndices(isNew);
        Assert.NotNull(map);
        Assert.Equal(64, map!.Length);
        Assert.Equal(4, map[4]);    // before the first insertion: unchanged
        Assert.Equal(6, map[5]);    // after the spinal cord: shifted by 1
        Assert.Equal(55, map[54]);  // still shifted by 1, just before the first leg's arteries
        Assert.Equal(58, map[55]);  // after them: shifted by 3
        Assert.Equal(66, map[63]);  // the last old part: still shifted by 3
    }

    [Fact]
    public void ASavedIndexBeyondTheOldBodyIsRefused()
    {
        int[] map = { 0, 1, 4 };
        Assert.False(PartIndexRule.TryRemap(map, 3, out int unchanged));
        Assert.Equal(3, unchanged);
        Assert.False(PartIndexRule.TryRemap(map, -1, out _));
        Assert.True(PartIndexRule.TryRemap(map, 2, out int moved));
        Assert.Equal(4, moved);
    }
}

public class ModListRuleTests
{
    [Fact]
    public void AModTheSaveNeverHadIsNew()
    {
        var saved = new[] { "ludeon.rimworld", "brrainz.harmony", "MSE2.Core" };
        var running = new[] { "Ludeon.RimWorld", "brrainz.harmony", "mse2.core", "Th3Fr3d.ExtendedInjuries" };
        Assert.Equal(new[] { "th3fr3d.extendedinjuries" }, ModListRule.NewModIds(saved, running));
    }

    [Fact]
    public void ReorderingAndCaseChangeNothing()
    {
        var saved = new[] { "b.two", "A.One" };
        var running = new[] { "a.one", "B.Two" };
        Assert.Empty(ModListRule.NewModIds(saved, running));
    }

    [Fact]
    public void AMissingHeaderMakesNothingNew() =>
        Assert.Empty(ModListRule.NewModIds(null, new[] { "a.one" }));

    [Fact]
    public void AModTheSaveHadButIsNowOffIsNotNew() =>
        Assert.Empty(ModListRule.NewModIds(new[] { "a.one", "b.two" }, new[] { "a.one" }));
}
