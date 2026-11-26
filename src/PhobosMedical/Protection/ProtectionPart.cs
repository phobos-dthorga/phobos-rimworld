using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Protection;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Medical.Protection;

/// <summary>
/// D'thorga Medical's protective-gear part (docs/development/radiation-control-line.md), built on D'thorga Framework's protective-gear
/// lines: a radiation control point and a radiation controlled area, for Rimatomics (owner, 10 Nov 2026), whose Defs load only with
/// it; and a toxic controlled area (Medical 0.30.0; the owner's card of 26 Nov 2026), always. The hooks below cost a field read on any
/// map without a line.
/// </summary>
internal static class ProtectionPart
{
    public const string RadiationZone = "PhobosZone_RadiationControlled";
    public const string ToxicZone = "PhobosZone_ToxicControlled";

    /// <summary>From the mod's constructor: a dose taken at any line goes through the dosing part's safety and the player's reserve.</summary>
    internal static void Start() => ProtectionLine.DoseAllowed = Phobos.Dosing.Services.DoseService.LineDoseAllowed;

    internal static LoadReport Report(LoadReport report)
    {
        int kinds = DefDatabase<ProtectionZoneDef>.AllDefsListForReading.Count;
        return kinds == 0
            ? report.Off("protective-gear lines", "no kind of line loaded")
            : report.On("protective-gear lines: " + kinds + " kind" + (kinds == 1 ? "" : "s") + " (" + string.Join(", ", DefDatabase<ProtectionZoneDef>.AllDefsListForReading.ConvertAll(d => d.defName)) + ")");
    }
}

/// <summary>Paints the radiation controlled area. Listed in the build menu's zone tab by class, so it is a class of its own.</summary>
public sealed class Designator_PhobosRadiationAreaExpand : Designator_PhobosProtectionArea
{
    public Designator_PhobosRadiationAreaExpand() : base(true, "Phobos/Medical/Protection/RadiationAreaExpand")
    {
    }

    protected override string ZoneDefName => ProtectionPart.RadiationZone;
}

/// <summary>Clears the radiation controlled area.</summary>
public sealed class Designator_PhobosRadiationAreaClear : Designator_PhobosProtectionArea
{
    public Designator_PhobosRadiationAreaClear() : base(false, "Phobos/Medical/Protection/RadiationAreaClear")
    {
    }

    protected override string ZoneDefName => ProtectionPart.RadiationZone;
}

/// <summary>Paints the toxic controlled area (Medical 0.30.0). Its own pictures are to come; until then, the game's home-area ones.</summary>
public sealed class Designator_PhobosToxicAreaExpand : Designator_PhobosProtectionArea
{
    public Designator_PhobosToxicAreaExpand() : base(true, "Phobos/Medical/Protection/ToxicAreaExpand")
    {
    }

    protected override string ZoneDefName => ProtectionPart.ToxicZone;
}

/// <summary>Clears the toxic controlled area.</summary>
public sealed class Designator_PhobosToxicAreaClear : Designator_PhobosProtectionArea
{
    public Designator_PhobosToxicAreaClear() : base(false, "Phobos/Medical/Protection/ToxicAreaClear")
    {
    }

    protected override string ZoneDefName => ProtectionPart.ToxicZone;
}

/// <summary>
/// Stops a colonist at a protective-gear line to suit up, and has them take the gear off just after stepping out. The game has no
/// extension point for "this cell needs something before entering", and every route into a room passes through this one method, so it
/// is the only place that catches them all; Field Tents and others patch it the same way. The prefix skips the original only on the
/// step it refuses, when the pawn has just been given the jobs to fetch gear (or turned back): taking the step would put them over the
/// line unprotected. The postfix acts only on a tick the pawn really stepped.
/// </summary>
[HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
public static class ProtectionLineStepPatch
{
    public static bool Prefix(Pawn ___pawn, IntVec3 ___nextCell) => ProtectionLine.BeforeStep(___pawn, ___nextCell);

    public static void Postfix(Pawn ___pawn, IntVec3 ___lastCell, int ___lastEnteredCellTick)
    {
        if (___lastEnteredCellTick == Find.TickManager.TicksGame) ProtectionLine.AfterStep(___pawn, ___lastCell);
    }
}

/// <summary>
/// A colonist turned back at a line for want of gear treats the zone as outside their allowed area for an hour, so the game's own work
/// search offers them other work instead of sending them back to the line. Only ever narrows the answer.
/// </summary>
[HarmonyPatch(typeof(ForbidUtility), nameof(ForbidUtility.InAllowedArea))]
public static class ProtectionLineAllowedAreaPatch
{
    public static void Postfix(IntVec3 c, Pawn forPawn, ref bool __result)
    {
        if (__result && !ProtectionLine.AllowsCell(forPawn, c)) __result = false;
    }
}
