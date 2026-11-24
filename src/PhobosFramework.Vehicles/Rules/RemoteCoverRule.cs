using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Keeping to radio cover (Framework 0.279.0; docs/development/autonomous-vehicles.md, step 8), game-free. A source of remote control
/// works out, block by block, whether a vehicle standing there would keep its link; a vehicle it drives then pays more to cross a cell
/// of a block it would not. The cost is authored and only makes a way dearer, never closes it.
/// </summary>
public static class RemoteCoverRule
{
    /// <summary>A block where a vehicle would keep its link.</summary>
    public const byte Covered = 1;
    /// <summary>A block where it would not.</summary>
    public const byte Uncovered = 0;
    /// <summary>A block with no cell a vehicle could stand on, so nothing was worked out; it costs nothing (no vehicle crosses it).</summary>
    public const byte NoSample = 2;

    /// <summary>How many blocks of <paramref name="blockCells"/> cover a row of <paramref name="cells"/> cells, the last one part-filled.</summary>
    public static int Blocks(int cells, int blockCells) => (cells + Math.Max(1, blockCells) - 1) / Math.Max(1, blockCells);

    /// <summary>
    /// The extra cost of each cell of a map (index = x + z * width): <paramref name="uncoveredCost"/> on every cell of a block marked
    /// <see cref="Uncovered"/>, nought elsewhere. Blocks run by row, <see cref="Blocks"/> of them across.
    /// </summary>
    public static short[] Costs(byte[] blocks, int blockCells, int width, int height, int uncoveredCost)
    {
        int block = Math.Max(1, blockCells);
        int across = Blocks(width, block);
        if (blocks.Length != across * Blocks(height, block)) throw new ArgumentException("The blocks do not cover the map.", nameof(blocks));
        var costs = new short[width * height];
        short cost = (short)Math.Clamp(uncoveredCost, 0, short.MaxValue);
        if (cost == 0) return costs;
        for (int z = 0; z < height; z++)
        {
            int row = z / block * across;
            for (int x = 0; x < width; x++)
            {
                if (blocks[row + x / block] == Uncovered) costs[x + z * width] = cost;
            }
        }
        return costs;
    }

    /// <summary>How many blocks are marked <see cref="Uncovered"/>: what a seen-in-play note reports.</summary>
    public static int UncoveredCount(byte[] blocks)
    {
        int count = 0;
        foreach (byte b in blocks) if (b == Uncovered) count++;
        return count;
    }
}
