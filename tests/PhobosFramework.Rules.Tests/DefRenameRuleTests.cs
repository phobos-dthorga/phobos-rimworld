using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class DefRenameRuleTests
{
    static DefRenameTable Table()
    {
        var table = new DefRenameTable();
        table.AddItem("TestCaseMk82", "TestMk82Case");
        table.AddItem("TestCaseMk84", "TestMk84Case");
        return table;
    }

    [Fact]
    public void AnItemAndItsRecipeAreRenamed()
    {
        Assert.Equal("TestMk84Case", Table().CurrentName("TestCaseMk84"));
        Assert.Equal("Make_TestMk82Case", Table().CurrentName("Make_TestCaseMk82"));
    }

    [Fact]
    public void OtherNamesAreLeftAlone() => Assert.Equal("Steel", Table().CurrentName("Steel"));

    [Fact]
    public void AThingKeepsTheNumberTheGameReadsFromItsOldId()
    {
        // The old name's own digits are read as part of the number: this is the ID a load of the old save gives the thing.
        Assert.Equal("Thing_TestMk84Case8484840", Table().CurrentLoadId("Thing_TestCaseMk8484840"));
        Assert.Equal("Thing_TestMk82Case820", Table().CurrentLoadId("Thing_TestCaseMk820"));
    }

    [Fact]
    public void ANumberTooLongToHoldReadsAsZeroAsInTheGame() =>
        Assert.Equal("Thing_TestMk84Case0", Table().CurrentLoadId("Thing_TestCaseMk84848484840"));

    [Fact]
    public void ABillKeepsItsNumber() =>
        Assert.Equal("Bill_Make_TestMk84Case_2057", Table().CurrentLoadId("Bill_Make_TestCaseMk84_2057"));

    [Fact]
    public void OtherLoadIdsAreLeftAlone()
    {
        Assert.Equal("Thing_Human19320404", Table().CurrentLoadId("Thing_Human19320404"));
        Assert.Equal("Bill_Make_Component_12", Table().CurrentLoadId("Bill_Make_Component_12"));
        Assert.Equal("Thing_TestCaseMk84Lid12", Table().CurrentLoadId("Thing_TestCaseMk84Lid12"));
    }
}
