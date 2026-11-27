using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class PartLayoutRuleTests
{
    [Fact]
    public void AnOlderSaveDropsTheRemovedPlacesLastFirst()
    {
        // A design of ten parts had the bays at 4 and 7 taken out by layout 1; a save from layout 0 holds all ten.
        var removed = new[] { (4, 1), (7, 1) };
        Assert.Equal(new List<int> { 7, 4 }, PartLayoutRule.DropAt(10, 0, removed));
    }

    [Fact]
    public void ASaveFromTheCurrentLayoutDropsNothing()
    {
        Assert.Empty(PartLayoutRule.DropAt(8, 1, new[] { (4, 1), (7, 1) }));
    }

    [Fact]
    public void APlacePastTheEndOfTheSaveIsLeftAlone()
    {
        // Saved before the bays were ever added: it holds only the design's own five parts.
        Assert.Equal(new List<int> { 4 }, PartLayoutRule.DropAt(5, 0, new[] { (4, 1), (7, 1) }));
        Assert.Empty(PartLayoutRule.DropAt(3, 0, new[] { (4, 1), (7, 1) }));
    }

    [Fact]
    public void PlacesAreFoundInTheListsOrder()
    {
        var keys = new[] { "Engine", "PhobosStereoSlot", "PhobosCabLightMount", "PhobosRoofMount", "PhobosSideRail" };
        Assert.Equal(new List<int> { 1, 3, 4 }, PartLayoutRule.PlacesOf(keys, new HashSet<string> { "PhobosStereoSlot", "PhobosRoofMount", "PhobosSideRail" }));
    }
}
