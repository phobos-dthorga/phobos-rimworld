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

    /// <summary>The colour of the reach's edge on the globe (authored: the dashboard's amber, so it reads as "fuel").</summary>
    static Material? rangeMaterial;
    static Material RangeMaterial => rangeMaterial ??= MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(0.95f, 0.70f, 0.25f, 0.9f), 3590);

    /// <summary>While the player's vehicle caravan is selected, the edge of where the fuel aboard takes it, drawn on the globe as the game
    /// draws a caravan's path (Framework 0.85.0; VehicleRange works it out).</summary>
    public override void PostDrawExtraSelectionOverlays()
    {
        VehicleCaravan? caravan = Caravan;
        if (caravan == null || caravan.Faction != Faction.OfPlayer) return;
        VehicleRange.Reach? reach = VehicleRange.For(caravan);
        if (reach == null) return;
        foreach ((Vector3 a, Vector3 b) in reach.Edge) GenDraw.DrawWorldLineBetween(a, b, RangeMaterial, 0.6f);
    }

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
