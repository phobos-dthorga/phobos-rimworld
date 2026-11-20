using System.Collections.Generic;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// An implement a player may lower by hand (Framework 0.193.0; the owner's button, 20 Nov 2026, docs/development/farming-batch-two.md): it
/// then works and drags while the vehicle in front drives anywhere, until it is lifted or a field run starts.
/// </summary>
public interface IVehicleHandLowered
{
    /// <summary>Whether a player has lowered it by hand.</summary>
    bool HandLowered { get; }
    /// <summary>Lifts it: a field run has taken over.</summary>
    void LiftByHand();
}

/// <summary>
/// An implement behind a vehicle (docs/development/farming-batch-two.md, "The linkage"): a plough or a cultivator on the three-point
/// linkage, a drill on the drawbar. It is its own vehicle, held by the vehicle in front through our hitch, perhaps in a chain (Framework
/// 0.193.0). It is lowered, and works and drags, while the field run of the vehicle that drives the chain works a row with it, or while a
/// player has lowered it by hand; at a row's end, on a road and parked it is lifted. Whether a run lowers it is read each time; only the
/// hand-set flag is saved, by the implement.
/// </summary>
public static class Implements
{
    /// <summary>The vehicle holding this implement through its hitch, or null.</summary>
    public static VehiclePawn? HeldBy(VehiclePawn implement) => Hitches.Towing(implement)?.Vehicle;

    /// <summary>The vehicle that drives the chain this implement is held in, or null when nothing holds it.</summary>
    public static VehiclePawn? DrivenBy(VehiclePawn implement) => HeldBy(implement) == null ? null : CompPhobosHitch.HeadOf(implement);

    /// <summary>Whether the field run of the vehicle driving the chain works a row with this implement now.</summary>
    public static bool RunLowers(VehiclePawn implement) => DrivenBy(implement)?.GetComp<CompPhobosFieldRun>()?.Lowers(implement) == true;

    /// <summary>Whether this implement is lowered and working: held, and its run works a row with it or a player lowered it by hand.</summary>
    public static bool Lowered(VehiclePawn implement)
    {
        // Never off a map: carried on a journey it is lifted, whatever was set by hand (the fault-read of 21 Nov 2026, H4: it had dragged
        // its draught along the world map).
        if (!implement.Spawned || HeldBy(implement) == null) return false;
        if (RunLowers(implement)) return true;
        List<ThingComp> comps = implement.AllComps;
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is IVehicleHandLowered { HandLowered: true }) return true;
        }
        return false;
    }

    /// <summary>Lifts an implement lowered by hand: it has been unhitched.</summary>
    public static void Lift(VehiclePawn implement)
    {
        List<ThingComp> comps = implement.AllComps;
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is IVehicleHandLowered { HandLowered: true } hand) hand.LiftByHand();
        }
    }

    /// <summary>A field run has started: every implement in the chain lowered by hand is lifted, and the run lowers its own as it works.</summary>
    public static void RunStarted(VehiclePawn vehicle)
    {
        foreach (VehiclePawn behind in CompPhobosHitch.Behind(vehicle))
        {
            List<ThingComp> comps = behind.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is IVehicleHandLowered { HandLowered: true } hand) hand.LiftByHand();
            }
        }
    }
}
