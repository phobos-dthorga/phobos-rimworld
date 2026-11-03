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
        // More Injuries appends Abdomen (with two intestines) after the torso's last child, two arteries under each leg, and the
        // spinal cord under the spine. A part saved just after any insertion point moves by the number of parts inserted before it.
        var isNew = new bool[20];
        isNew[5] = isNew[6] = isNew[7] = true;   // Abdomen, SmallIntestine, LargeIntestine
        isNew[12] = isNew[13] = true;           // left femoral and popliteal arteries
        isNew[18] = true;                       // SpinalCord
        int[]? map = PartIndexRule.OldToNewIndices(isNew);
        Assert.NotNull(map);
        Assert.Equal(14, map!.Length);
        Assert.Equal(4, map[4]);    // before the first insertion: unchanged
        Assert.Equal(8, map[5]);    // the old part 5 now sits at 8: shifted by 3
        Assert.Equal(11, map[8]);   // still shifted by 3, just before the arteries
        Assert.Equal(14, map[9]);   // after the arteries too: shifted by 5
        Assert.Equal(19, map[13]);  // the last old part, after the spinal cord: shifted by 6
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
