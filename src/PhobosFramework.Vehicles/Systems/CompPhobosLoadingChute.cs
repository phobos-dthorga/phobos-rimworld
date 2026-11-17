using System.Collections.Generic;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A loading chute (Framework 0.126.0; docs/development/universal-hoppers.md, UH13): put on a one-cell storage building.</summary>
public class CompProperties_PhobosLoadingChute : CompProperties
{
    /// <summary>How many stacks pass down the chute each rare tick, about four seconds. Authored: quick enough that a belt never waits on it.</summary>
    public int stacksPerLook = 4;

    public CompProperties_PhobosLoadingChute() => compClass = typeof(CompPhobosLoadingChute);
}

/// <summary>
/// A storage building whose goods drop into the cargo of a vehicle of its own side parked across its spout, the cell it faces. Belts and
/// haulers fill it like any storage; with no vehicle there it simply holds what it has, and its inspect line says what it waits for.
/// <see cref="LoadingChute"/> decides and moves; this only ticks, draws the spout and shows the line. Nothing is saved: the line is worked out
/// again at the next look.
/// </summary>
public class CompPhobosLoadingChute : ThingComp
{
    string? waitingFor;

    public CompProperties_PhobosLoadingChute Props => (CompProperties_PhobosLoadingChute)props;

    /// <summary>The cell the chute empties into: the one it faces.</summary>
    public IntVec3 Spout => parent.Position + parent.Rotation.FacingCell;

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (parent.Spawned) waitingFor = LoadingChute.Pass(this);
    }

    public override string? CompInspectStringExtra() => waitingFor;

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (parent.Spawned) GenDraw.DrawFieldEdges(new List<IntVec3> { Spout });
    }
}
