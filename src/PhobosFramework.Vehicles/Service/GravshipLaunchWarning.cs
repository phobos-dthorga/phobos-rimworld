using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The gravship's launch confirmation names vehicles that stand partly off the ship's floor (Framework 0.188.0;
/// docs/development/vehicle-emergent-play.md, EM18). The game carries any vehicle whose centre cell is on the floor, and already lists the
/// ones whose centre is off it among what will be left behind; it says nothing of a vehicle that comes along with part of its body
/// hanging over the edge. A postfix that only adds a paragraph to the game's own confirmation window: it changes nothing about the launch.
/// </summary>
[HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.PreLaunchConfirmation))]
public static class GravshipOverhangWarning
{
    public static void Postfix(Building_GravEngine engine)
    {
        if (!ModsConfig.OdysseyActive || engine?.Map == null) return;
        List<VehiclePawn> hanging = Overhanging(engine).ToList();
        if (hanging.Count == 0) return;
        Dialog_MessageBox? box = Find.WindowStack.Windows.OfType<Dialog_MessageBox>().LastOrDefault();
        if (box == null) return;
        box.text += "\n\n" + "PhobosVehicleSystems_GravshipOverhang".Translate().Colorize(ColorLibrary.RedReadable) + "\n"
            + hanging.Select(v => v.LabelShortCap).ToLineList("  - ");
        Seen.Note("vehicles.gravship-overhang", hanging[0]);
    }

    /// <summary>The player's vehicles that will ride along (centre on the floor) with a cell of their body off it.</summary>
    public static IEnumerable<VehiclePawn> Overhanging(Building_GravEngine engine)
    {
        foreach (Pawn pawn in engine.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (pawn is not VehiclePawn vehicle || !engine.ValidSubstructureAt(vehicle.Position)) continue;
            foreach (IntVec3 cell in vehicle.OccupiedRect())
            {
                if (engine.ValidSubstructureAt(cell)) continue;
                yield return vehicle;
                break;
            }
        }
    }
}
