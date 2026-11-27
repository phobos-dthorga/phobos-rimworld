using System.Collections.Generic;
using Phobos.Framework.Interface;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// On the game's own caravans, those on foot (Framework 0.353.0; the Radio Framework's caravans by hand-held, the owner's cards of 27 Nov
/// 2026): its reach home in the inspect pane, and the radio's buttons and a medevac call, as a convoy has on its first vehicle. Added to the
/// game's Caravan world object by an XML patch; a vehicle convoy is a Def of its own and keeps its own. Nothing shows without a radio in
/// the caravan. Presentation only: the reach is ConvoyRadio's, the medevac Medevac's.
/// </summary>
public class WorldObjectComp_PhobosCaravanRadio : WorldObjectComp
{
    const string GuardKey = "A caravan's radio line";

    /// <summary>The player's caravan on foot, or null.</summary>
    Caravan? OnFoot => parent is Caravan caravan and not VehicleCaravan && caravan.Faction == Faction.OfPlayer ? caravan : null;

    public override string? CompInspectStringExtra()
    {
        // Guarded: the game asks for this every frame while the caravan is selected (UiGuard).
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try { return OnFoot is { } caravan ? ConvoyRadio.Of(caravan)?.Line : null; }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (OnFoot is not { } caravan || !ConvoyRadio.Available || ConvoyRadio.Of(caravan) == null) yield break;
        foreach (Gizmo gizmo in ConvoyRadio.Gizmos(caravan)) yield return gizmo;
        foreach (Gizmo gizmo in Medevac.Gizmos(caravan)) yield return gizmo;
    }
}
