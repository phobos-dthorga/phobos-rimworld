using Phobos.Framework.Debug;
using Phobos.Framework.Interface;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// On every vehicle caravan (added to Vehicle Framework's VehicleCaravan world object by an XML patch): says in the caravan's inspect
/// text why a vehicle of it will not move, and offers the caravan jump-start when one needs it (docs/development/vehicle-world-map-design.md).
/// Presentation only: the rules are the comp's and JumpStarts'.
/// </summary>
public class WorldObjectComp_PhobosVehicleCaravan : WorldObjectComp
{
    VehicleCaravan? Caravan => parent as VehicleCaravan;

    static IEnumerable<CompPhobosVehicleSystems> Comps(VehicleCaravan caravan) =>
        caravan.VehiclesListForReading.Select(v => v.GetComp<CompPhobosVehicleSystems>()).Where(c => c != null && c.Usable)!;

    public override string? CompInspectStringExtra()
    {
        // Guarded: the game asks for this every frame while the thing is selected (UiGuard).
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try { return InspectLine(); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A vehicle caravan's line";

    string? InspectLine()
    {
        VehicleCaravan? caravan = Caravan;
        if (caravan == null) return null;
        var lines = new List<string>();
        // How long the fuel aboard lasts on the move (Framework 0.85.0); its reach is drawn on the globe while the caravan is selected.
        if (caravan.Faction == Faction.OfPlayer && VehicleRange.BudgetTicks(caravan, out _) is float budget)
        {
            lines.Add("PhobosVehicleSystems_CaravanRange".Translate(((int)budget).ToStringTicksToPeriod().Named("TIME")).Resolve());
        }
        foreach (CompPhobosVehicleSystems comp in Comps(caravan))
        {
            if (comp.MoveRefusal != null)
            {
                lines.Add("PhobosVehicleSystems_CaravanRefusal".Translate(comp.Vehicle.LabelShort.Named("VEHICLE"), comp.MoveRefusal.Named("REASON")).Resolve());
            }
        }
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    /// <summary>A repair done at a halt, with supplies from the vehicle's cargo. All at once: an authored simplification, as the caravan
    /// jump-start is. Presentation only: the repair is the comp's.</summary>
    static Gizmo? RepairGizmo(VehicleCaravan caravan, CompPhobosVehicleSystems comp)
    {
        if (comp.Repairs.Pending.Count == 0) return null;
        RepairWork? ready = comp.Repairs.Ready;
        RepairWork shown = ready ?? comp.Repairs.Pending[0];
        var command = new Command_Action
        {
            defaultLabel = "PhobosVehicleSystems_CaravanRepair".Translate(comp.Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            defaultDesc = shown.Label.CapitalizeFirst(),
            icon = SystemIcons.For("Phobos/VehicleSystems/CaravanRepair"),
        };
        // The best mechanic aboard: some work needs a minimum skill.
        Pawn? mechanic = caravan.PawnsListForReading.Where(p => p is not VehiclePawn && p.IsColonist && !p.Downed && !p.WorkTagIsDisabled(WorkTags.Constructing))
            .OrderByDescending(p => p.skills?.GetSkill(SkillDefOf.Construction)?.Level ?? 0).FirstOrDefault();
        if (caravan.vehiclePather != null && caravan.vehiclePather.MovingNow) command.Disable("PhobosVehicleSystems_CaravanRepairMoving".Translate().Resolve());
        else if (mechanic == null) command.Disable("PhobosVehicleSystems_CaravanRepairNoMechanic".Translate().Resolve());
        else if (comp.Repairs.ReadyFor(mechanic) == null) command.Disable(ready == null ? comp.Repairs.NeedsLine(shown)
            : "PhobosVehicleSystems_FitNeedsSkill".Translate(ready.MinSkill.Named("SKILL")).CapitalizeFirst().Resolve());
        command.action = () =>
        {
            if (mechanic != null) comp.Repairs.CompleteReady(mechanic);
        };
        return command;
    }

    // The reach's edge on the globe is drawn by WorldComponent_PhobosCaravanReach: Vehicle Framework's caravan never runs its comps'
    // selection overlays, so a PostDrawExtraSelectionOverlays here was never called.

    public override IEnumerable<Gizmo> GetGizmos()
    {
        VehicleCaravan? caravan = Caravan;
        if (caravan == null || caravan.Faction != Faction.OfPlayer) yield break;
        foreach (CompPhobosVehicleSystems comp in Comps(caravan).ToList())
        {
            Gizmo? gizmo = JumpStarts.CaravanGizmo(caravan, comp);
            if (gizmo != null) yield return gizmo;
            Gizmo? repair = RepairGizmo(caravan, comp);
            if (repair != null) yield return repair;
        }
    }
}

/// <summary>
/// Keeps an aircraft's systems going while it flies on the world map (Framework 0.119.1; the owner's report of 17 Nov 2026: "in aircraft,
/// things like the cab heater are not working"). Vehicle Framework's flight holds the aircraft without ticking it (its
/// ShouldTickContents is false, and the game ticks a world object's contents only when asked), so nothing of ours ran from take-off to
/// landing: the cab stayed at its take-off temperature, the generator charged nothing, a lost generator's battery clock never ran, and
/// the whole flight was caught up afterwards as hours parked. This comp, added to Vehicle Framework's AerialVehicle by patch, gives the
/// player's aircraft the same look a parked vehicle gets, at the same interval. Other factions' aircraft in flight stay as they were:
/// nothing of theirs evolves off a map. No Harmony; saves nothing.
/// </summary>
public class WorldObjectComp_PhobosFlightSystems : WorldObjectComp
{
    public override void CompTick()
    {
        if ((Find.TickManager.TicksGame + parent.ID) % GenTicks.TickRareInterval != 0) return;
        if (parent is not AerialVehicleInFlight { Flying: true } flight || flight.Vehicle is not { } vehicle || vehicle.Faction != Faction.OfPlayer) return;
        CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
        if (comp is not { Usable: true }) return;
        using (PhobosTiming.Measure("vehicle rare tick / in flight")) comp.UpdateNow();
        Seen.Note("aircraft.systems-run-in-flight", vehicle);
    }
}
