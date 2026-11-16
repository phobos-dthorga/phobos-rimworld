using System.Collections.Generic;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Which way a set-out piece faces, in the trailer's own frame: towards the trailer, away from it, or along it.</summary>
public enum SetOutFacing
{
    Inward,
    Outward,
    Front,
    Back,
}

/// <summary>
/// A building a trailer sets out beside itself when a colonist sets it up (Framework 0.121.0; docs/development/vehicle-trailers.md, B0.4).
/// Cells are in the trailer's own frame: <c>x</c> counts out from the side the pieces go (0 is the column touching the trailer), <c>z</c>
/// runs along the trailer towards its front, in the trailer's own cells (a 3 x 8 trailer's run -3 to 4). The game's own footprint rule
/// places it; <see cref="from"/> and <see cref="to"/> are the cells it must cover.
/// </summary>
public class SetOutPiece
{
    public ThingDef? def;
    /// <summary>What it is made of, for a building made from stuff; the game's default stuff when empty.</summary>
    public ThingDef? stuff;
    public IntVec2 from;
    public IntVec2 to;
    public SetOutFacing facing = SetOutFacing.Inward;
    /// <summary>Set out only while this upgrade is fitted; empty for always.</summary>
    public string? node;
    /// <summary>A storage piece takes only this category at first, as the player may change (the kitchen's shelf takes meals).</summary>
    public ThingCategoryDef? storeOnly;
}

/// <summary>A canvas room: walls on the edge of its rectangle, one door, and a canvas roof over the inside. Cells as for a piece.</summary>
public class SetOutRoom
{
    public IntVec2 from;
    public IntVec2 to;
    public IntVec2 door;
    /// <summary>Set out only while this upgrade is fitted; empty for always.</summary>
    public string? node;
}

/// <summary>What a trailer sets out, named in its own Def so other modders can make working trailers in XML.</summary>
public class CompProperties_PhobosSetOut : VehicleCompProperties
{
    public List<SetOutPiece> pieces = new();
    public List<SetOutRoom> rooms = new();
    public ThingDef? wallDef;
    public ThingDef? doorDef;
    public RoofDef? roofDef;
    /// <summary>The power point set out with the pieces (the rig's own), for pieces that take power; none when empty.</summary>
    public ThingDef? powerPointDef;
    public IntVec2 powerPointCell = new(1, 5);
    /// <summary>What the pieces draw with nothing working, in watts (lights, the cold box): authored.</summary>
    public float idleWatts;
    /// <summary>The words for what it sets out, as "the kitchen tent": in refusals and messages.</summary>
    [MustTranslate] public string setOutLabel = "";

    public CompProperties_PhobosSetOut() => compClass = typeof(CompPhobosSetOut);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (pieces.Count == 0 && rooms.Count == 0) yield return "sets out nothing: give it pieces or rooms";
        if (rooms.Count > 0 && (wallDef == null || doorDef == null || roofDef == null)) yield return "a room needs wallDef, doorDef and roofDef";
        foreach (SetOutPiece piece in pieces)
        {
            if (piece.def == null) yield return "a piece has no def";
            else if (piece.def.category != ThingCategory.Building) yield return piece.def.defName + " is not a building";
        }
        if (setOutLabel.NullOrEmpty()) yield return "setOutLabel is empty";
    }
}

/// <summary>A set-out piece's part in what the trailer draws from its power point.</summary>
public interface ISetOutLoad
{
    /// <summary>What it draws now, in watts, beyond the trailer's idle draw.</summary>
    float WattsNow { get; }
}

/// <summary>On a set-out piece's Def: what power does for it (Framework 0.121.0). Read by the work-speed stat part and by the pieces.</summary>
public class PhobosSetOutPowerExtension : DefModExtension
{
    /// <summary>The work speed multiplied while the trailer is plugged in (Billy's range: 1.25, authored).</summary>
    public float speedWhenPowered = 1f;
    /// <summary>What it draws while working, in watts (authored).</summary>
    public float wattsWorking;
}

/// <summary>A part of a trailer that has its say when it sets up or packs up (the set-out pieces, the Dubs Bad Hygiene stand-ins).</summary>
public interface ISetUpPart
{
    /// <summary>Why it cannot set up here now, or null.</summary>
    string? SetUpPartRefusal();

    /// <summary>Why it cannot pack up now, or null.</summary>
    string? PackUpPartRefusal();

    /// <summary>The trailer has been set up (true) or packed up (false).</summary>
    void SetUpChanged(bool on);
}
