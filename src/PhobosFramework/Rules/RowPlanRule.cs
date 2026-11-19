using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>A cell of an area a machine works over, as the map counts it: X east, Z north.</summary>
public readonly record struct FieldCell(int X, int Z);

/// <summary>
/// One row of a plan: a strip <see cref="CrossMin"/> to <see cref="CrossMax"/> wide across the rows, worked from <see cref="From"/> to
/// <see cref="To"/> along them. <see cref="AlongX"/> says the rows run east and west (along is X, across is Z); otherwise north and south.
/// </summary>
public readonly record struct FieldRow(bool AlongX, int CrossMin, int CrossMax, int From, int To)
{
    /// <summary>Plus one when the row is worked toward higher X or Z, minus one toward lower. A row of one cell counts as plus one.</summary>
    public int Step => To < From ? -1 : 1;

    /// <summary>How many cells long the row is.</summary>
    public int Length => Math.Abs(To - From) + 1;

    /// <summary>The cell at this place along the row and this place across it.</summary>
    public FieldCell Cell(int along, int cross) => AlongX ? new FieldCell(along, cross) : new FieldCell(cross, along);
}

/// <summary>
/// Plans the rows a machine drives to cover an area with a strip of a given width (Framework 0.182.0; docs/development/farming-batch-one.md,
/// "Harvest this paddock"): back and forth, each row beside the last, starting from the corner nearest the machine. Game-free, so a
/// combine, a seed drill, a sprayer and any later machine that covers an area (a mower, a road paver) share it. It plans from the area's
/// cells alone: what stands in a cell, and whether the machine can drive a row, are the run's to find out.
/// </summary>
public static class RowPlanRule
{
    /// <summary>Whether rows should run east and west: along the longer side of the area, so there are fewer turns. A square runs east and west.</summary>
    public static bool LongerAlongX(IReadOnlyCollection<FieldCell> cells)
    {
        if (cells.Count == 0) return true;
        Bounds(cells, out int minX, out int maxX, out int minZ, out int maxZ);
        return maxX - minX >= maxZ - minZ;
    }

    /// <summary>
    /// The rows, in the order to drive them. The area is cut into strips <paramref name="stripWidth"/> wide across the rows; the last strip
    /// is moved back to end at the area's edge, overlapping the one before, so the machine never hangs outside the area where the area is
    /// at least a strip wide. A place along a strip where none of its cells belong to the area splits the row there. Strips are taken from
    /// the side nearer <paramref name="from"/>, the first row from the end nearer it, and each strip is driven the opposite way to the last.
    /// </summary>
    public static List<FieldRow> Plan(IReadOnlyCollection<FieldCell> cells, int stripWidth, bool alongX, FieldCell from)
    {
        var rows = new List<FieldRow>();
        if (cells.Count == 0 || stripWidth <= 0) return rows;
        var area = cells as HashSet<FieldCell> ?? new HashSet<FieldCell>(cells);
        Bounds(area, out int minX, out int maxX, out int minZ, out int maxZ);
        int alongMin = alongX ? minX : minZ;
        int alongMax = alongX ? maxX : maxZ;
        int crossMin = alongX ? minZ : minX;
        int crossMax = alongX ? maxZ : maxX;
        int fromAlong = alongX ? from.X : from.Z;
        int fromCross = alongX ? from.Z : from.X;

        var strips = new List<int>();
        for (int start = crossMin; start <= crossMax; start += stripWidth)
        {
            // The last strip ends at the edge, overlapping the one before, where the area is at least a strip wide.
            int low = start + stripWidth - 1 > crossMax ? Math.Max(crossMin, crossMax - stripWidth + 1) : start;
            if (strips.Count == 0 || strips[strips.Count - 1] != low) strips.Add(low);
        }
        // From the side nearer the machine.
        if (Math.Abs(fromCross - crossMax) < Math.Abs(fromCross - crossMin)) strips.Reverse();
        bool ascending = Math.Abs(fromAlong - alongMin) <= Math.Abs(fromAlong - alongMax);

        var runs = new List<(int Start, int End)>();
        foreach (int low in strips)
        {
            int high = low + stripWidth - 1;
            runs.Clear();
            int runStart = 0;
            bool inRun = false;
            for (int along = alongMin; along <= alongMax + 1; along++)
            {
                bool any = along <= alongMax && AnyAcross(area, alongX, along, low, Math.Min(high, crossMax));
                if (any && !inRun)
                {
                    runStart = along;
                    inRun = true;
                }
                else if (!any && inRun)
                {
                    runs.Add((runStart, along - 1));
                    inRun = false;
                }
            }
            if (runs.Count == 0) continue;
            if (ascending)
            {
                foreach ((int start, int end) in runs) rows.Add(new FieldRow(alongX, low, high, start, end));
            }
            else
            {
                for (int i = runs.Count - 1; i >= 0; i--) rows.Add(new FieldRow(alongX, low, high, runs[i].End, runs[i].Start));
            }
            ascending = !ascending;
        }
        return rows;
    }

    static bool AnyAcross(HashSet<FieldCell> area, bool alongX, int along, int low, int high)
    {
        for (int cross = low; cross <= high; cross++)
        {
            if (area.Contains(alongX ? new FieldCell(along, cross) : new FieldCell(cross, along))) return true;
        }
        return false;
    }

    static void Bounds(IEnumerable<FieldCell> cells, out int minX, out int maxX, out int minZ, out int maxZ)
    {
        minX = minZ = int.MaxValue;
        maxX = maxZ = int.MinValue;
        foreach (FieldCell cell in cells)
        {
            if (cell.X < minX) minX = cell.X;
            if (cell.X > maxX) maxX = cell.X;
            if (cell.Z < minZ) minZ = cell.Z;
            if (cell.Z > maxZ) maxZ = cell.Z;
        }
    }
}

/// <summary>One line of cells across a machine's path: <see cref="CrossMin"/> to <see cref="CrossMax"/> at one place <see cref="Along"/>.</summary>
public readonly record struct StripLine(bool AlongX, int CrossMin, int CrossMax, int Along)
{
    /// <summary>How many cells wide the line is.</summary>
    public int Width => CrossMax - CrossMin + 1;

    /// <summary>The cell at this place across the line.</summary>
    public FieldCell Cell(int cross) => AlongX ? new FieldCell(Along, cross) : new FieldCell(cross, Along);
}

/// <summary>
/// Where a machine's working strip lies against its body (Framework 0.182.0; docs/development/farming-batch-one.md, "The strip hook"): a
/// line of cells across its path, as wide as its implement, a set number of cells ahead of its body's leading edge (a header) or behind
/// its trailing edge (a drill, with a negative figure). A strip wider than the body overhangs it evenly; an odd cell left over goes to the
/// machine's right-hand side, so it turns with the machine. Game-free; the facing is a step east, west, north or south.
/// </summary>
public static class StripRule
{
    /// <summary>
    /// The strip's line for a body standing on these cells and facing this way (<paramref name="dirX"/> and <paramref name="dirZ"/>: one of
    /// them plus or minus one, the other nought). <paramref name="ahead"/> counts cells beyond the body's leading edge: one is the line just
    /// in front, nought the body's own front line, minus the body's length the line just behind it.
    /// </summary>
    public static StripLine Front(int minX, int minZ, int maxX, int maxZ, int dirX, int dirZ, int width, int ahead)
    {
        bool alongX = dirX != 0;
        int step = alongX ? Math.Sign(dirX) : Math.Sign(dirZ);
        if (step == 0) step = 1;
        int along = alongX ? (step > 0 ? maxX + ahead : minX - ahead) : (step > 0 ? maxZ + ahead : minZ - ahead);
        int bodyLow = alongX ? minZ : minX;
        int bodyHigh = alongX ? maxZ : maxX;
        int low = bodyLow - LowSideExtra(bodyHigh - bodyLow + 1, width, alongX, step);
        return new StripLine(alongX, low, low + width - 1, along);
    }

    /// <summary>
    /// The lowest cell across that the body must cover for its strip to lie on <paramref name="stripCrossMin"/> onward: the other way round
    /// from <see cref="Front"/>, for placing a machine on a planned row.
    /// </summary>
    public static int BodyCrossMin(int stripCrossMin, int bodyWidth, int width, bool alongX, int step) =>
        stripCrossMin + LowSideExtra(bodyWidth, width, alongX, step);

    /// <summary>
    /// How many cells of the strip lie beyond the body on its low side (lower X or Z). The overhang is split evenly; an odd cell goes to the
    /// machine's right. Facing north the right is higher X, south lower X, east lower Z, west higher Z. A strip narrower than the body is
    /// set in by the same rule, as a negative figure.
    /// </summary>
    static int LowSideExtra(int bodyWidth, int width, bool alongX, int step)
    {
        int extra = width - bodyWidth;
        int small = FloorHalf(extra);
        int large = extra - small;
        bool rightIsHigh = alongX ? step < 0 : step > 0;
        return rightIsHigh ? small : large;
    }

    static int FloorHalf(int value) => value >= 0 ? value / 2 : -((-value + 1) / 2);
}
