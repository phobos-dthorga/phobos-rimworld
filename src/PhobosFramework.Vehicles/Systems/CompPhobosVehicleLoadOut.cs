using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Gives a storage building its two vehicle switches, "Vehicles may load from here" and "Vehicles may unload here". Added in code to every
/// storage building type, see <see cref="VehicleLoadOut"/>.
/// </summary>
public class CompProperties_PhobosVehicleLoadOut : CompProperties
{
    /// <summary>
    /// Whether "Vehicles may unload here" starts on (Framework 0.175.0): set at start-up for storage built for vehicles to tip into, the
    /// kind that says which side they back up to (the bulk bunker). Off for every other storage.
    /// </summary>
    public bool unloadStartsOn;

    public CompProperties_PhobosVehicleLoadOut() => compClass = typeof(CompPhobosVehicleLoadOut);
}

/// <summary>
/// Whether a truck's loading grab may lift goods out of this storage (Framework 0.125.0; docs/development/universal-hoppers.md, UH12), and
/// whether a truck unloading by itself may tip into it (Framework 0.175.0; docs/development/vehicle-gathering-runs.md, "Marked bays and
/// Deliver"). Loading is off everywhere to start, so a grab never empties a shelf the player did not choose (owner, 17 Nov 2026);
/// unloading starts on only for storage built for it (owner, 19 Nov 2026). The switches are the only saved state;
/// <see cref="VehicleLoadOut"/> and <see cref="VehicleUnloadBays"/> decide what they mean and change them.
/// </summary>
public class CompPhobosVehicleLoadOut : ThingComp
{
    /// <summary>Saved key; never rename once saves can hold it.</summary>
    public const string SaveKey = "PhobosVehiclesMayLoad";

    /// <summary>Saved key of the unloading switch; never rename once saves can hold it.</summary>
    public const string UnloadSaveKey = "PhobosVehiclesMayUnload";

    bool vehiclesMayLoad;
    bool vehiclesMayUnload;

    public CompProperties_PhobosVehicleLoadOut Props => (CompProperties_PhobosVehicleLoadOut)props;

    public bool VehiclesMayLoad
    {
        get => vehiclesMayLoad;
        internal set => vehiclesMayLoad = value;
    }

    public bool VehiclesMayUnload
    {
        get => vehiclesMayUnload;
        internal set => vehiclesMayUnload = value;
    }

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        vehiclesMayUnload = Props.unloadStartsOn;
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
        yield return new Command_Toggle
        {
            defaultLabel = "PhobosVehicleSystems_MayUnload".Translate(),
            defaultDesc = "PhobosVehicleSystems_MayUnloadDesc".Translate(),
            icon = SystemIcons.For(VehicleUnloadBays.IconPath),
            isActive = () => vehiclesMayUnload,
            toggleAction = () => VehicleUnloadBays.Set(this, !vehiclesMayUnload),
        };
    }

    public override string? CompInspectStringExtra()
    {
        if (vehiclesMayLoad && vehiclesMayUnload) return "PhobosVehicleSystems_MayLoadAndUnloadInspect".Translate().Resolve();
        if (vehiclesMayLoad) return "PhobosVehicleSystems_MayLoadInspect".Translate().Resolve();
        if (vehiclesMayUnload) return "PhobosVehicleSystems_MayUnloadInspect".Translate().Resolve();
        return null;
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref vehiclesMayLoad, SaveKey, false);
        // Saved only when it differs from where it starts, so bunkers built before 0.175.0 load with unloading allowed.
        Scribe_Values.Look(ref vehiclesMayUnload, UnloadSaveKey, Props.unloadStartsOn);
    }
}
