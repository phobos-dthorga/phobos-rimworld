using System.Collections.Generic;
using Phobos.Framework.Interface;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A trailer with no tank or rig whose whole set-up is what it sets out (Billy's kitchen): words for its button and its refusals.</summary>
public class CompProperties_PhobosTrailerSetUp : VehicleCompProperties
{
    public CompProperties_PhobosTrailerSetUp() => compClass = typeof(CompPhobosTrailerSetUp);
}

/// <summary>
/// The plain set-up of a working trailer (Framework 0.121.0; docs/development/vehicle-trailers.md, B0.1 and B0.4): a colonist sets it up by hand,
/// and its parts (what it sets out) do the rest. A trailer with a tank sets up through its tank instead.
/// </summary>
public sealed class CompPhobosTrailerSetUp : CompPhobosSetUp
{
    const string Icons = "Phobos/VehicleSystems/Tank/";

    protected override string DeployedSaveKey => "PhobosTrailerSetUp_Deployed";

    protected override string SetUpRefusal => "PhobosTrailer_PlainSetUpRefusal".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();

    protected override GraphicDataOverlay? SetUpLook => null;

    protected override bool NeedsEngineNow => false;

    /// <summary>Why it cannot be set up now, or null.</summary>
    public string? CannotSetUp()
    {
        if (!Vehicle.Spawned) return "PhobosVehicleSystems_TankNotOnMap".Translate().Resolve();
        if (CrewAndEngineRefusal(engineNeeded: false) is { } towed) return towed;
        return null;
    }

    protected override string? HandSetUpRefusal() => CannotSetUp();

    protected override void FinishByHand(bool on, Pawn worker) => deployed = on;

    void Order(bool on)
    {
        if (on && CannotSetUp() is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        OrderByHand(on);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer) yield break;
        if (HandOrder != HandSetUpOrder.None)
        {
            yield return CancelHandOrderGizmo();
            yield break;
        }
        bool on = deployed;
        var command = new Command_Action
        {
            defaultLabel = on ? "PhobosVehicleSystems_TankGizmoPackUp".Translate().Resolve() : "PhobosVehicleSystems_TankGizmoSetUp".Translate().Resolve(),
            defaultDesc = on ? "PhobosTrailer_PlainGizmoPackUpDesc".Translate().Resolve() : "PhobosTrailer_PlainGizmoSetUpDesc".Translate().Resolve(),
            icon = SystemIcons.For(Icons + (on ? "PackUp" : "SetUp")),
            action = () => Order(!on),
        };
        string? refused = on ? PartsRefusal(false) : CannotSetUp() ?? PartsRefusal(true);
        if (refused != null) command.Disable(refused);
        yield return command;
    }

    public override string? CompInspectStringExtra()
    {
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            return HandOrderLine;
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A trailer's set-up line";
}
