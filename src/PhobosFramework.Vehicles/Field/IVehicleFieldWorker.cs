using System.Collections.Generic;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Why a field worker cannot go on, at a row's end or before a run.</summary>
public enum FieldWorkStop
{
    /// <summary>It can go on.</summary>
    None,
    /// <summary>Its hold is full: a run takes it to a bin to pour, or ends.</summary>
    Full,
    /// <summary>It has run out of what it uses (seed, spray): the run ends.</summary>
    OutOfSupply,
    /// <summary>Something failed: the worker says what.</summary>
    Fault,
}

/// <summary>
/// The implement of a vehicle that works an area row by row (Framework 0.182.0; docs/development/farming-batch-one.md): a combine's front,
/// later a drill or a sprayer. The field run drives; the worker acts on the line of cells its strip covers each time the vehicle moves a
/// cell, and settles its work once a row. The worker is the content mod's; the run is the Framework's.
/// </summary>
public interface IVehicleFieldWorker
{
    /// <summary>The words for its order, in the player's language: "Harvest a paddock".</summary>
    string OrderLabel { get; }
    /// <summary>What the order does, for the button's description.</summary>
    string OrderDescription { get; }
    /// <summary>The order button's picture path.</summary>
    string OrderIconPath { get; }

    /// <summary>How many cells wide its strip is now (a front fitted by upgrade).</summary>
    int StripWidth { get; }
    /// <summary>How many cells beyond the body's leading edge its strip lies: one for a front, negative for an implement behind.</summary>
    int StripAhead { get; }

    /// <summary>Whether it would work this area at all (a growing zone, marked for machines), for the list of areas to choose from.</summary>
    bool Accepts(Zone zone);
    /// <summary>Why it cannot work this area now (no front, nothing ripe, a crop no front suits), or null.</summary>
    string? CannotWork(Zone zone);
    /// <summary>Whether any cell of this row still has work for it, so a row with none is passed over.</summary>
    bool HasWork(IReadOnlyList<IntVec3> rowCells);

    /// <summary>Called when a run starts on an area, to clear its tallies.</summary>
    void BeginRun(Zone zone);
    /// <summary>Works the cells of its strip that lie in the area, as the vehicle enters them. Cheap: a handful of cells.</summary>
    void WorkCells(List<IntVec3> cells);
    /// <summary>Settles a row's work (a harvest's tally into the grain tank). Returns why it cannot go on, or None.</summary>
    FieldWorkStop RowDone(out string? reason);
    /// <summary>Called when a run ends, with what it did in words, for the end message; and its notes.</summary>
    string EndRun(bool finished);
}
