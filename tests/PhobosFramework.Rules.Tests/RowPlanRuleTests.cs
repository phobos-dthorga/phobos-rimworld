using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class RowPlanRuleTests
{
    static HashSet<FieldCell> Rect(int minX, int minZ, int maxX, int maxZ)
    {
        var cells = new HashSet<FieldCell>();
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++) cells.Add(new FieldCell(x, z));
        }
        return cells;
    }

    /// <summary>Every cell the rows' strips cover, inside the area.</summary>
    static HashSet<FieldCell> Covered(IEnumerable<FieldRow> rows, HashSet<FieldCell> area)
    {
        var covered = new HashSet<FieldCell>();
        foreach (FieldRow row in rows)
        {
            for (int along = row.From; along != row.To + row.Step; along += row.Step)
            {
                for (int cross = row.CrossMin; cross <= row.CrossMax; cross++)
                {
                    FieldCell cell = row.Cell(along, cross);
                    if (area.Contains(cell)) covered.Add(cell);
                }
            }
        }
        return covered;
    }

    [Fact]
    public void NothingToPlanGivesNoRows()
    {
        Assert.Empty(RowPlanRule.Plan(new HashSet<FieldCell>(), 5, true, new FieldCell(0, 0)));
        Assert.Empty(RowPlanRule.Plan(Rect(0, 0, 3, 3), 0, true, new FieldCell(0, 0)));
    }

    [Fact]
    public void RowsRunAlongTheLongerSide()
    {
        Assert.True(RowPlanRule.LongerAlongX(Rect(0, 0, 19, 9)));
        Assert.False(RowPlanRule.LongerAlongX(Rect(0, 0, 9, 19)));
        Assert.True(RowPlanRule.LongerAlongX(Rect(0, 0, 9, 9)));
    }

    [Fact]
    public void ARectangleIsCutIntoStripsDrivenBackAndForth()
    {
        List<FieldRow> rows = RowPlanRule.Plan(Rect(0, 0, 19, 9), 5, alongX: true, new FieldCell(-3, -3));
        Assert.Equal(2, rows.Count);
        Assert.Equal(new FieldRow(true, 0, 4, 0, 19), rows[0]);
        Assert.Equal(new FieldRow(true, 5, 9, 19, 0), rows[1]);
        Assert.Equal(1, rows[0].Step);
        Assert.Equal(-1, rows[1].Step);
        Assert.Equal(20, rows[0].Length);
    }

    [Fact]
    public void EveryCellIsCoveredWhateverTheWidth()
    {
        HashSet<FieldCell> area = Rect(3, 7, 25, 19);
        foreach (int width in new[] { 1, 2, 3, 5, 6, 7, 8, 13, 40 })
        {
            foreach (bool alongX in new[] { true, false })
            {
                List<FieldRow> rows = RowPlanRule.Plan(area, width, alongX, new FieldCell(0, 0));
                Assert.True(area.SetEquals(Covered(rows, area)), $"width {width}, alongX {alongX}");
            }
        }
    }

    [Fact]
    public void TheLastStripIsMovedBackToTheEdge()
    {
        // Twelve across with a strip of five: 0 to 4, 5 to 9, then 7 to 11, never 10 to 14.
        List<FieldRow> rows = RowPlanRule.Plan(Rect(0, 0, 29, 11), 5, true, new FieldCell(0, 0));
        Assert.Equal(new[] { 0, 5, 7 }, rows.Select(r => r.CrossMin));
        Assert.All(rows, r => Assert.True(r.CrossMax <= 11));
    }

    [Fact]
    public void AnAreaNarrowerThanTheStripIsOneRow()
    {
        List<FieldRow> rows = RowPlanRule.Plan(Rect(0, 0, 9, 2), 5, true, new FieldCell(0, 0));
        FieldRow row = Assert.Single(rows);
        Assert.Equal(0, row.CrossMin);
        Assert.Equal(4, row.CrossMax);
    }

    [Fact]
    public void ItStartsAtTheCornerNearestTheMachine()
    {
        HashSet<FieldCell> area = Rect(0, 0, 19, 9);
        FieldRow farCorner = RowPlanRule.Plan(area, 5, true, new FieldCell(25, 12))[0];
        Assert.Equal(5, farCorner.CrossMin);
        Assert.Equal(19, farCorner.From);
        Assert.Equal(0, farCorner.To);
        FieldRow nearCorner = RowPlanRule.Plan(area, 5, true, new FieldCell(-2, -2))[0];
        Assert.Equal(0, nearCorner.CrossMin);
        Assert.Equal(0, nearCorner.From);
    }

    [Fact]
    public void AGapRightAcrossAStripSplitsTheRow()
    {
        HashSet<FieldCell> area = Rect(0, 0, 19, 4);
        for (int z = 0; z <= 4; z++)
        {
            area.Remove(new FieldCell(9, z));
            area.Remove(new FieldCell(10, z));
        }
        List<FieldRow> rows = RowPlanRule.Plan(area, 5, true, new FieldCell(0, 0));
        Assert.Equal(2, rows.Count);
        Assert.Equal(new FieldRow(true, 0, 4, 0, 8), rows[0]);
        Assert.Equal(new FieldRow(true, 0, 4, 11, 19), rows[1]);
    }

    [Fact]
    public void SplitRowsAreDrivenInTravelOrderOnTheWayBackToo()
    {
        HashSet<FieldCell> area = Rect(0, 0, 19, 4);
        for (int z = 0; z <= 4; z++) area.Remove(new FieldCell(10, z));
        List<FieldRow> rows = RowPlanRule.Plan(area, 5, true, new FieldCell(30, 0));
        Assert.Equal(new FieldRow(true, 0, 4, 19, 11), rows[0]);
        Assert.Equal(new FieldRow(true, 0, 4, 9, 0), rows[1]);
    }

    [Fact]
    public void AHoleInsideAStripDoesNotSplitIt()
    {
        HashSet<FieldCell> area = Rect(0, 0, 19, 4);
        area.Remove(new FieldCell(10, 2));
        Assert.Single(RowPlanRule.Plan(area, 5, true, new FieldCell(0, 0)));
    }

    [Fact]
    public void AnLShapedAreaIsCoveredWithShorterRowsWhereItIsNarrow()
    {
        HashSet<FieldCell> area = Rect(0, 0, 19, 4);
        area.UnionWith(Rect(0, 5, 7, 9));
        List<FieldRow> rows = RowPlanRule.Plan(area, 5, true, new FieldCell(0, 0));
        Assert.Equal(2, rows.Count);
        Assert.Equal(20, rows[0].Length);
        Assert.Equal(8, rows[1].Length);
        Assert.True(area.SetEquals(Covered(rows, area)));
    }

    [Fact]
    public void RowsMayRunNorthAndSouth()
    {
        List<FieldRow> rows = RowPlanRule.Plan(Rect(0, 0, 9, 19), 5, alongX: false, new FieldCell(0, 0));
        Assert.Equal(new FieldRow(false, 0, 4, 0, 19), rows[0]);
        Assert.Equal(new FieldCell(2, 7), rows[0].Cell(7, 2));
    }

    // ---- The strip against the body ----

    [Fact]
    public void AHeaderLiesJustAheadOfTheBodyFacingNorth()
    {
        // A body four wide (X 10 to 13) and nine long (Z 20 to 28), a six-cell front one cell ahead.
        StripLine line = StripRule.Front(10, 20, 13, 28, 0, 1, width: 6, ahead: 1);
        Assert.Equal(new StripLine(false, 9, 14, 29), line);
        Assert.Equal(6, line.Width);
        Assert.Equal(new FieldCell(9, 29), line.Cell(9));
    }

    [Fact]
    public void FacingSouthItLiesBelowTheBody() =>
        Assert.Equal(new StripLine(false, 9, 14, 19), StripRule.Front(10, 20, 13, 28, 0, -1, 6, 1));

    [Fact]
    public void FacingEastAndWestItRunsNorthToSouth()
    {
        // The same body turned: nine long in X (20 to 28), four wide in Z (10 to 13).
        Assert.Equal(new StripLine(true, 9, 14, 29), StripRule.Front(20, 10, 28, 13, 1, 0, 6, 1));
        Assert.Equal(new StripLine(true, 9, 14, 19), StripRule.Front(20, 10, 28, 13, -1, 0, 6, 1));
    }

    [Fact]
    public void AnOddCellOfOverhangGoesToTheMachinesRight()
    {
        // Five wide on a body of four: one cell over. North: right is east (higher X). South: right is west.
        Assert.Equal(new StripLine(false, 10, 14, 29), StripRule.Front(10, 20, 13, 28, 0, 1, 5, 1));
        Assert.Equal(new StripLine(false, 9, 13, 19), StripRule.Front(10, 20, 13, 28, 0, -1, 5, 1));
        // East: right is south (lower Z). West: right is north.
        Assert.Equal(new StripLine(true, 9, 13, 29), StripRule.Front(20, 10, 28, 13, 1, 0, 5, 1));
        Assert.Equal(new StripLine(true, 10, 14, 19), StripRule.Front(20, 10, 28, 13, -1, 0, 5, 1));
    }

    [Fact]
    public void AStripNarrowerThanTheBodyIsSetIn()
    {
        StripLine line = StripRule.Front(10, 20, 13, 28, 0, 1, width: 2, ahead: 1);
        Assert.Equal(new StripLine(false, 11, 12, 29), line);
    }

    [Fact]
    public void NoughtAheadIsTheBodysOwnFrontLineAndMinusItsLengthTheLineBehind()
    {
        Assert.Equal(28, StripRule.Front(10, 20, 13, 28, 0, 1, 4, 0).Along);
        Assert.Equal(19, StripRule.Front(10, 20, 13, 28, 0, 1, 4, -9).Along);
    }

    [Fact]
    public void PlacingABodyOnAStripIsTheOtherWayRound()
    {
        foreach (int width in new[] { 2, 3, 4, 5, 6, 7, 8 })
        {
            foreach ((int dx, int dz) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
            {
                bool alongX = dx != 0;
                int step = alongX ? dx : dz;
                StripLine line = alongX ? StripRule.Front(20, 10, 28, 13, dx, dz, width, 1) : StripRule.Front(10, 20, 13, 28, dx, dz, width, 1);
                Assert.Equal(10, StripRule.BodyCrossMin(line.CrossMin, bodyWidth: 4, width, alongX, step));
            }
        }
    }
}
