using System.Collections.Generic;
using System.Linq;
using System.Text;
using LudeonTK;
using Phobos.Framework.Debug;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// Why nobody is loading a vehicle (Framework 0.134.0; owner, 18 Nov 2026: an M1075 HEMTT told to take 12,500 steel that no colonist
/// would load, with nothing in the log or the save to say why). Walks the same questions Vehicle Framework's own loading work asks, in
/// its order (the order itself, the vehicle listed for loading, its weight limit, then each colonist: the work switched on, the vehicle
/// allowed and reachable, a stack they may take), and says where the chain breaks. It reads only; the dev action prints it. Vehicle
/// Framework's work giver has the last word: "would take the job now" is its own answer, asked for each colonist.
/// </summary>
public static class CargoLoadingReport
{
    /// <summary>Vehicle Framework's name for the list of vehicles that wait for cargo.</summary>
    const string LoadVehicleLister = "LoadVehicle";
    /// <summary>How many kinds of cargo to list before the rest are counted.</summary>
    const int KindsListed = 8;

    public static string For(VehiclePawn vehicle)
    {
        var text = new StringBuilder();
        text.AppendLine("Loading report for " + vehicle.LabelShort + " (" + vehicle.ThingID + "):");
        List<TransferableOneWay>? cargo = vehicle.cargoToLoad;
        if (cargo.NullOrEmpty())
        {
            text.AppendLine("- No cargo order is waiting on this vehicle. Use its Load cargo button first, then run this again while nobody comes.");
            return text.ToString();
        }
        int pieces = 0;
        foreach (TransferableOneWay kind in cargo!) pieces += kind.CountToTransfer;
        text.AppendLine("- The order: " + cargo.Count + " kind(s) of cargo, " + pieces + " piece(s) still to load.");
        foreach (TransferableOneWay kind in cargo.Take(KindsListed))
        {
            int gone = kind.things.Count(t => t == null || t.Destroyed || !t.Spawned);
            int forbidden = kind.things.Count(t => t is { Destroyed: false, Spawned: true } && t.IsForbidden(Faction.OfPlayer));
            text.AppendLine("    " + (kind.ThingDef?.defName ?? "?") + ": " + kind.CountToTransfer + " wanted, from " + kind.things.Count + " stack(s) noted when the order was given; "
                + gone + " of them no longer on the map, " + forbidden + " forbidden.");
        }
        if (cargo.Count > KindsListed) text.AppendLine("    and " + (cargo.Count - KindsListed) + " more kind(s).");

        Map map = vehicle.Map;
        bool listed = map.GetComponent<VehicleReservationManager>()?.VehicleListed(vehicle, LoadVehicleLister) == true;
        text.AppendLine(listed
            ? "- Listed for loading: yes."
            : "- Listed for loading: NO. Vehicle Framework only sends haulers to vehicles on its loading list, and this one is not on it although it has an order. This is the fault: give the order again.");

        float carried = MassUtility.GearAndInventoryMass(vehicle);
        float limit = MassUtility.Capacity(vehicle);
        bool over = MassUtility.IsOverEncumbered(vehicle);
        text.AppendLine("- Weight: carrying " + carried.ToString("0.#") + " kg; the limit the game reports for it is " + limit.ToString("0.#") + " kg (its cargo capacity stat is "
            + vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity).ToString("0.#") + " kg)." + (over ? " OVER THE LIMIT: nobody loads an overloaded vehicle. This is the fault." : " Under the limit."));
        text.AppendLine("- Movement status: " + vehicle.movementStatus + (vehicle.movementStatus == VehicleMovementStatus.Offline ? " (a vehicle that cannot move is not loaded: this is the fault)." : "."));

        WorkGiverDef? giverDef = DefDatabase<WorkGiverDef>.AllDefsListForReading.FirstOrDefault(d => d.giverClass == typeof(WorkGiver_PackVehicle));
        if (giverDef == null)
        {
            text.AppendLine("- Vehicle Framework's loading work was not found among the work givers: no colonist can be given the job. This is the fault.");
            return text.ToString();
        }
        text.AppendLine("- The work: \"" + giverDef.label + "\", under " + giverDef.workType.labelShort + ".");
        var giver = (WorkGiver_Scanner)giverDef.Worker;
        int total = 0, workOff = 0, notAllowed = 0, noWay = 0, noStack = 0, wouldTake = 0;
        string? firstTaker = null;
        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned.Concat(map.mapPawns.SpawnedColonyMechs))
        {
            if (pawn.Downed || pawn.Drafted) continue;
            total++;
            if (pawn.WorkTypeIsDisabled(giverDef.workType) || pawn.workSettings is { EverWork: true } settings && !settings.WorkIsActive(giverDef.workType)) { workOff++; continue; }
            if (vehicle.IsForbidden(pawn)) { notAllowed++; continue; }
            if (!pawn.CanReach(vehicle.Position, PathEndMode.OnCell, Danger.Deadly)) { noWay++; continue; }
            Job? job = null;
            try { job = giver.JobOnThing(pawn, vehicle); }
            catch (System.Exception error) { text.AppendLine("    Asking for " + pawn.LabelShort + " threw: " + error.GetBaseException().Message); }
            if (job == null) { noStack++; continue; }
            wouldTake++;
            firstTaker ??= pawn.LabelShort;
        }
        text.AppendLine("- Colonists and mechanoids on the map, not downed or drafted: " + total + ".");
        text.AppendLine("    " + workOff + " do not have that work switched on.");
        text.AppendLine("    " + notAllowed + " may not go to the vehicle (it is forbidden, or stands outside their allowed area).");
        text.AppendLine("    " + noWay + " cannot reach the vehicle's middle cell.");
        text.AppendLine("    " + noStack + " are refused by Vehicle Framework: the vehicle is not listed or is over its limit (above), or every stack of the order is reserved, forbidden or out of their reach.");
        text.AppendLine("    " + wouldTake + " would take the job now" + (firstTaker != null ? " (" + firstTaker + " for one)" : "") + ".");
        text.AppendLine(wouldTake > 0
            ? "- Verdict: nothing blocks the loading. Those who would take it have work of a higher priority first; raise their hauling priority, or right-click the vehicle with one."
            : "- Verdict: nobody can take the job; the lines above marked as the fault, or the largest count, say why.");
        return text.ToString();
    }

    /// <summary>Select vehicles on the map: logs, for each, the questions Vehicle Framework's loading work asks (the order, the
    /// vehicle's listing and weight limit, then each colonist) and where the chain breaks. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: why is nobody loading this vehicle?", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void WhyNotLoading()
    {
        List<VehiclePawn> vehicles = Find.Selector.SelectedObjects.OfType<VehiclePawn>().Where(v => v.Spawned).ToList();
        if (vehicles.Count == 0)
        {
            Log.Message(VehicleSystemsMod.LogPrefix + " Select a vehicle on the map first.");
            return;
        }
        foreach (VehiclePawn vehicle in vehicles) Log.Message(VehicleSystemsMod.LogPrefix + " " + For(vehicle));
    }
}
