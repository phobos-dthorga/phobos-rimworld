using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Hooking a trailer up again from a depot, on the world map (Framework 0.324.0, EM41, "drop and hook"; docs/development/vehicle-road-work.md):
/// the trailer goes straight into this vehicle's holder, as a trailer towed off the map rides, with no visit to the depot's map. The
/// rules are <see cref="Depots.HookUp"/>'s; this part keeps the hitch's own state, as coupling on a map does.
/// </summary>
public sealed partial class CompPhobosHitch
{
    /// <summary>Why this vehicle cannot hook up this trailer off the map, or null: the hitch's own refusals that hold off a map, trailers only.</summary>
    public string? HookUpRefusal(VehiclePawn candidate)
    {
        if (Refusal() is { } own) return own;
        if (TrailerProps(candidate) == null) return "PhobosDepot_HookNotTrailer".Translate(candidate.LabelShort.Named("TRAILER")).Resolve();
        if (candidate.GetComp<CompPhobosTrailer>()?.DrawbarBroken == true) return "PhobosHitch_DrawbarBroken".Translate(candidate.LabelShort.Named("TRAILER")).Resolve();
        if (Couplings.Refusal(Vehicle, candidate) is { } coupling) return coupling;
        return null;
    }

    /// <summary>Takes a trailer that is off every map into the holder behind this vehicle; false if the holder would not take it.</summary>
    internal bool TakeHeld(VehiclePawn candidate)
    {
        if (trailer != null || candidate.Spawned || Geometry(candidate) == null) return false;
        candidate.holdingOwner?.Remove(candidate);
        if (!Held.TryAdd(candidate, canMergeWithExistingStacks: false)) return false;
        trailer = candidate;
        Hitches.Note(this, candidate);
        weightTick = -1;
        RefreshWeight();
        Systems?.Powertrain?.MarkMassDirty();
        ChainChanged();
        return true;
    }

    /// <summary>On the world map, at a depot holding trailers: one button, a menu of the trailers, each refused with its reason.</summary>
    public override IEnumerable<Gizmo> CompCaravanGizmos()
    {
        foreach (Gizmo gizmo in base.CompCaravanGizmos()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || Vehicle.GetVehicleCaravan() is not { } convoy || Depots.At(convoy.Tile) is not { } depot) yield break;
        List<VehiclePawn> trailers = depot.Vehicles.Where(v => TrailerProps(v) != null).ToList();
        if (trailers.Count == 0) yield break;
        var command = new Command_Action
        {
            defaultLabel = "PhobosDepot_HookGizmo".Translate().Resolve(),
            defaultDesc = "PhobosDepot_HookGizmoDesc".Translate(Vehicle.LabelShort.Named("TRACTOR")).Resolve(),
            icon = SystemIcons.For("Phobos/VehicleSystems/RoadWork/HookFromDepot"),
            action = () => Find.WindowStack.Add(new FloatMenu(trailers.Select(t =>
            {
                string? why = Depots.HookUpRefusal(convoy, Vehicle, t);
                return why == null
                    ? new FloatMenuOption(t.LabelCap, () => Depots.HookUp(convoy, Vehicle, t))
                    : new FloatMenuOption(t.LabelCap + ": " + why, null);
            }).ToList())),
        };
        if (Depots.HookUpRefusal(convoy, Vehicle, null) is { } refused) command.Disable(refused);
        yield return command;
    }
}
