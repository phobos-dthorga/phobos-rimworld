using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class PartGroupRuleTests
{
    static readonly string[] Words = { "left", "right", "front", "rear", "upper", "lower" };

    static List<PartGroupRule.Group> Group(params string[] labels) => PartGroupRule.Groups(labels, Words);

    [Fact]
    public void PartsThatDifferOnlyByPositionFoldTogether()
    {
        List<PartGroupRule.Group> groups = Group("Front left landing wheel", "Front right landing wheel", "Rear left landing wheel", "Rear right landing wheel");
        PartGroupRule.Group only = Assert.Single(groups);
        Assert.Equal("landing wheel", only.Label);
        Assert.Equal(new[] { 0, 1, 2, 3 }, only.Members);
    }

    [Fact]
    public void AGroupKeepsThePositionWordItsMembersShare()
    {
        List<PartGroupRule.Group> groups = Group("Left rear panel", "Right rear panel", "Left frontal panel", "Right frontal panel");
        Assert.Equal(new[] { "rear panel", "frontal panel" }, groups.Select(g => g.Label));
    }

    [Fact]
    public void NumberedPartsFoldTogether()
    {
        PartGroupRule.Group only = Assert.Single(Group("Engine 1", "Engine 2"));
        Assert.Equal("Engine", only.Label);
    }

    [Fact]
    public void APartAloneKeepsItsOwnName()
    {
        List<PartGroupRule.Group> groups = Group("Front rotor mast", "Battery");
        Assert.Equal(new[] { "Front rotor mast", "Battery" }, groups.Select(g => g.Label));
    }

    [Fact]
    public void OrderFollowsFirstAppearance()
    {
        List<PartGroupRule.Group> groups = Group("Left turbo shaft", "Battery", "Right turbo shaft");
        Assert.Equal(new[] { "turbo shaft", "Battery" }, groups.Select(g => g.Label));
        Assert.Equal(new[] { 0, 2 }, groups[0].Members);
    }

    [Fact]
    public void ANameOfOnlyPositionWordsStandsAlone()
    {
        List<PartGroupRule.Group> groups = Group("Front", "Rear");
        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public void CaseDoesNotMatter()
    {
        Assert.Single(Group("LEFT Fuel Intake", "right fuel intake"));
    }

    [Fact]
    public void WithoutPositionWordsNothingFolds()
    {
        Assert.Equal(2, PartGroupRule.Groups(new[] { "Left wing", "Right wing" }, new string[0]).Count);
    }
}
