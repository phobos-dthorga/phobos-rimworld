using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>What stands on one cell of a vehicle's walk-in cabin (docs/development/vehicle-cabins.md).</summary>
public enum CabinCell
{
    Floor,
    Seat,
    /// <summary>A litter's head: the cell north of its foot.</summary>
    LitterHead,
    /// <summary>A litter's foot, where the litter stands.</summary>
    LitterFoot,
    /// <summary>A fuel tank: blocks the cell.</summary>
    Tank,
    /// <summary>The way out.</summary>
    WayOut,
}

/// <summary>A cabin layout read from its rows: its size and every cell, x across from the left, z from the bottom row up.</summary>
public sealed class CabinPlan
{
    public int Width;
    public int Length;
    public CabinCell[,] Cells = new CabinCell[0, 0];
    public int WayOutX = -1, WayOutZ = -1;
    public readonly List<string> Problems = new();

    public bool Valid => Problems.Count == 0;
}

/// <summary>
/// Reads a walk-in cabin's layout (Framework, vehicle cabins, 22 Nov 2026). Rows are written nose first, so the first row is the top of the
/// map: <c>S</c> a seat, <c>L</c> over <c>l</c> a litter (head over foot), <c>T</c> a fuel tank, <c>E</c> the way out (exactly one),
/// <c>.</c> floor. Game-free.
/// </summary>
public static class CabinLayoutRule
{
    public static CabinPlan Parse(IReadOnlyList<string> rows)
    {
        var plan = new CabinPlan();
        if (rows == null || rows.Count == 0)
        {
            plan.Problems.Add("has no rows");
            return plan;
        }
        plan.Length = rows.Count;
        plan.Width = rows[0]?.Length ?? 0;
        if (plan.Width == 0)
        {
            plan.Problems.Add("has an empty first row");
            return plan;
        }
        plan.Cells = new CabinCell[plan.Width, plan.Length];
        for (int r = 0; r < rows.Count; r++)
        {
            string row = rows[r] ?? "";
            if (row.Length != plan.Width)
            {
                plan.Problems.Add($"row {r + 1} is {row.Length} wide, the first {plan.Width}");
                continue;
            }
            int z = plan.Length - 1 - r;
            for (int x = 0; x < row.Length; x++)
            {
                CabinCell cell;
                switch (row[x])
                {
                    case '.': cell = CabinCell.Floor; break;
                    case 'S': cell = CabinCell.Seat; break;
                    case 'L': cell = CabinCell.LitterHead; break;
                    case 'l': cell = CabinCell.LitterFoot; break;
                    case 'T': cell = CabinCell.Tank; break;
                    case 'E':
                        cell = CabinCell.WayOut;
                        if (plan.WayOutX >= 0) plan.Problems.Add("has more than one way out");
                        plan.WayOutX = x;
                        plan.WayOutZ = z;
                        break;
                    default:
                        plan.Problems.Add($"row {r + 1} has '{row[x]}', which is no cabin cell");
                        cell = CabinCell.Floor;
                        break;
                }
                plan.Cells[x, z] = cell;
            }
        }
        if (!plan.Valid) return plan;
        if (plan.WayOutX < 0) plan.Problems.Add("has no way out");
        for (int x = 0; x < plan.Width; x++)
        {
            for (int z = 0; z < plan.Length; z++)
            {
                if (plan.Cells[x, z] == CabinCell.LitterFoot && (z + 1 >= plan.Length || plan.Cells[x, z + 1] != CabinCell.LitterHead))
                    plan.Problems.Add($"has a litter's foot at column {x + 1} with no head above it");
                if (plan.Cells[x, z] == CabinCell.LitterHead && (z == 0 || plan.Cells[x, z - 1] != CabinCell.LitterFoot))
                    plan.Problems.Add($"has a litter's head at column {x + 1} with no foot below it");
            }
        }
        return plan;
    }

    /// <summary>How many of a kind of cell the plan has: seats, or litters counted by their feet.</summary>
    public static int Count(CabinPlan plan, CabinCell kind)
    {
        int count = 0;
        for (int x = 0; x < plan.Width; x++)
        {
            for (int z = 0; z < plan.Length; z++)
            {
                if (plan.Cells[x, z] == kind) count++;
            }
        }
        return count;
    }
}

/// <summary>
/// How many more people a vehicle with a walk-in cabin takes (Framework 0.211.1; owner, 22 Nov 2026: seats and cabin share one limit). Its
/// passenger seats and its cabin hold the same people, so those seated and those in the cabin are counted together against the passenger
/// seats; the cabin also holds no more than its own places (seats and litters). A vehicle with no passenger seats is limited by the cabin's
/// places alone. Game-free.
/// </summary>
public static class CabinRoomRule
{
    /// <summary>How many more may go into the cabin.</summary>
    public static int CabinRoom(int passengerSeats, int seated, int cabinPlaces, int inCabin)
    {
        int byPlaces = cabinPlaces - inCabin;
        int room = passengerSeats > 0 ? System.Math.Min(byPlaces, passengerSeats - seated - inCabin) : byPlaces;
        return room < 0 ? 0 : room;
    }

    /// <summary>Whether a passenger seat may still be taken: those seated and those in the cabin leave one free.</summary>
    public static bool SeatFree(int passengerSeats, int seated, int inCabin) => seated + inCabin < passengerSeats;
}
