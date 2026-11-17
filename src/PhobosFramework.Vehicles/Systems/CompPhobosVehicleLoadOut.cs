using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Gives a storage building the "Vehicles may load from here" switch. Added in code to every storage building type, see <see cref="VehicleLoadOut"/>.</summary>
public class CompProperties_PhobosVehicleLoadOut : CompProperties
{
    public CompProperties_PhobosVehicleLoadOut() => compClass = typeof(CompPhobosVehicleLoadOut);
}

/// <summary>
/// Whether a truck's loading grab may lift goods out of this storage (Framework 0.125.0; docs/development/universal-hoppers.md, UH12). Off
/// everywhere to start, so a grab never empties a shelf the player did not choose (owner, 17 Nov 2026). The switch is the only saved state;
/// <see cref="VehicleLoadOut"/> decides what it means and changes it.
/// </summary>
public class CompPhobosVehicleLoadOut : ThingComp
{
    /// <summary>Saved key; never rename once saves can hold it.</summary>
    public const string SaveKey = "PhobosVehiclesMayLoad";

    bool vehiclesMayLoad;

    public bool VehiclesMayLoad
    {
        get => vehiclesMayLoad;
        internal set => vehiclesMayLoad = value;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (parent.Faction != Faction.OfPlayer) yield break;
        yield return new Command_Toggle
        {
            defaultLabel = "PhobosVehicleSystems_MayLoad".Translate(),
            defaultDesc = "PhobosVehicleSystems_MayLoadDesc".Translate(),
            icon = SystemIcons.For(VehicleLoadOut.IconPath),
            isActive = () => vehiclesMayLoad,
            toggleAction = () => VehicleLoadOut.Set(this, !vehiclesMayLoad),
        };
    }

    public override string? CompInspectStringExtra() => vehiclesMayLoad ? "PhobosVehicleSystems_MayLoadInspect".Translate().Resolve() : null;

    public override void PostExposeData() => Scribe_Values.Look(ref vehiclesMayLoad, SaveKey, false);
}
