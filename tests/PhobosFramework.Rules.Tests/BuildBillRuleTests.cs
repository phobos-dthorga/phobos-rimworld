using System.Collections.Generic;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class BuildBillRuleTests
{
    static BillOffer Engine() => new()
    {
        OwnItem = "OurEngine",
        Substitutes = new[]
        {
            new BillSubstitute("LightEngine", 1, MaxSize: 4000),
            new BillSubstitute("MediumEngine", 1, MinSize: 4000, MaxSize: 15000),
            new BillSubstitute("HeavyEngine", 1, MinSize: 15000),
        },
    };

    [Theory]
    [InlineData(3000, "LightEngine")]
    [InlineData(4000, "MediumEngine")]
    [InlineData(9000, "MediumEngine")]
    [InlineData(15000, "HeavyEngine")]
    public void ASubstituteIsChosenByTheSizeOfTheThing(double size, string expected)
    {
        List<BillLine> lines = BuildBillRule.Fill(Engine(), 1, size, othersAllowed: true, out BillSource source);
        Assert.Equal(new[] { new BillLine(expected, 1) }, lines);
        Assert.Equal(BillSource.Substitute, source);
    }

    [Fact]
    public void OursFillsTheRoleWhenOthersAreNotAllowed()
    {
        List<BillLine> lines = BuildBillRule.Fill(Engine(), 1, 9000, othersAllowed: false, out BillSource source);
        Assert.Equal(new[] { new BillLine("OurEngine", 1) }, lines);
        Assert.Equal(BillSource.Ours, source);
    }

    [Fact]
    public void OursFillsTheRoleWhenNoSubstituteSuits()
    {
        var offer = new BillOffer { OwnItem = "OurEngine", Substitutes = new[] { new BillSubstitute("CarEngine", 1, MaxSize: 6000) } };
        Assert.Equal(new[] { new BillLine("OurEngine", 1) }, BuildBillRule.Fill(offer, 1, 9000, true, out BillSource source));
        Assert.Equal(BillSource.Ours, source);
    }

    [Fact]
    public void AnUnknownSizeSuitsOnlyAnOpenWindow()
    {
        Assert.Equal("OurEngine", BuildBillRule.Fill(Engine(), 1, double.NaN, true, out _)[0].Thing);
        var open = new BillOffer { OwnItem = "Ours", Substitutes = new[] { new BillSubstitute("Theirs", 1) } };
        Assert.Equal("Theirs", BuildBillRule.Fill(open, 1, double.NaN, true, out _)[0].Thing);
    }

    [Fact]
    public void TheFirstSubstituteThatSuitsWins()
    {
        var offer = new BillOffer { OwnItem = "Ours", Substitutes = new[] { new BillSubstitute("First", 1), new BillSubstitute("Second", 1) } };
        Assert.Equal("First", BuildBillRule.Fill(offer, 1, 100, true, out _)[0].Thing);
    }

    [Fact]
    public void CountsMultiply()
    {
        var wheels = new BillOffer { OwnMaterials = new[] { new BillLine("Steel", 10), new BillLine("Component", 1) }, Substitutes = new[] { new BillSubstitute("Tyre", 2) } };
        Assert.Equal(new[] { new BillLine("Tyre", 12) }, BuildBillRule.Fill(wheels, 6, 9000, true, out _));
        Assert.Equal(new[] { new BillLine("Steel", 60), new BillLine("Component", 6) }, BuildBillRule.Fill(wheels, 6, 9000, false, out _));
    }

    [Fact]
    public void AKitOfOursCoversTheWholeBuild()
    {
        var suspension = new BillOffer { OwnItem = "DrivetrainKit", OwnCoversWholeBuild = true, Substitutes = new[] { new BillSubstitute("Suspension", 1) } };
        Assert.Equal(new[] { new BillLine("DrivetrainKit", 1) }, BuildBillRule.Fill(suspension, 3, 9000, false, out _));
        Assert.Equal(new[] { new BillLine("Suspension", 3) }, BuildBillRule.Fill(suspension, 3, 9000, true, out _));
    }

    [Fact]
    public void NothingIsAskedForNoneOfARole() => Assert.Empty(BuildBillRule.Fill(Engine(), 0, 9000, true, out _));

    [Fact]
    public void AnOfferWithNothingGivesNothing() => Assert.Empty(BuildBillRule.Fill(new BillOffer(), 2, 9000, true, out _));

    [Fact]
    public void TheShellKeepsItsOrderAndItsFirstLine()
    {
        var shell = new[] { new BillLine("Steel", 280), new BillLine("Component", 8) };
        var parts = new[] { new BillLine("HeavyEngine", 1), new BillLine("Component", 6), new BillLine("Steel", 60), new BillLine("Tyre", 6) };
        Assert.Equal(new[] { new BillLine("Steel", 340), new BillLine("Component", 14), new BillLine("HeavyEngine", 1), new BillLine("Tyre", 6) },
            BuildBillRule.Merge(shell, parts));
    }

    [Fact]
    public void TheSamePartFromTwoRolesIsOneLine()
    {
        var parts = new[] { new BillLine("ElectricalKit", 1), new BillLine("ElectricalKit", 1) };
        Assert.Equal(new[] { new BillLine("Steel", 100), new BillLine("ElectricalKit", 2) }, BuildBillRule.Merge(new[] { new BillLine("Steel", 100) }, parts));
    }

    [Fact]
    public void EmptyLinesAreLeftOut() =>
        Assert.Equal(new[] { new BillLine("Steel", 100) }, BuildBillRule.Merge(new[] { new BillLine("Steel", 100), new BillLine("Gold", 0) }, new[] { new BillLine("Tyre", 0) }));
}
