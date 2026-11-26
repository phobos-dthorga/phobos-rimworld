using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Trailers in Vehicle Framework's window for forming a caravan (Framework 0.303.1; the owner's report of 26 Nov 2026, a flatbed ticked
/// beside a HEMTT it was nowhere near). The window lists every vehicle on the map as a member of its own and knows nothing of the hitch:
/// a trailer has no seats, so it was offered as ready to go, and when the caravan left it went from wherever it stood. These rules make
/// the window and the departure follow the hitch:
///
/// - an unhitched trailer cannot be chosen, and says why (<see cref="TrailerTileRefusal"/>);
/// - a hitched trailer is chosen exactly when the vehicle towing it is (<see cref="TrailersGoWithTheirTractor"/>), so its cargo room counts
///   in the window and the caravan's people load it;
/// - while the caravan forms, it is never drafted (<see cref="TrailerNotDraftedForCaravan"/>), never leads (<see cref="TrailerNeverLeads"/>),
///   and it leaves on its hitch, never as a member (<see cref="TrailersLeaveOnTheirHitch"/>), which also keeps any unhitched trailer that
///   something else sends on the map.
///
/// Other factions: Vehicle Framework's caravan window and forming are the player's; the departure rule applies to any faction's caravan
/// leaving a map through Vehicle Framework, with a message only for the player's.
/// </summary>
public static class CaravanTrailers
{
    /// <summary>How many hitches deep a line of trailers is followed when one is chosen with the vehicle ahead (Chains' own guard).</summary>
    const int ChainGuard = 16;

    /// <summary>Whether this vehicle is a trailer: one with no engine of its own, moved only on a hitch.</summary>
    public static bool IsTrailer(VehiclePawn? vehicle) => vehicle?.GetComp<CompPhobosTrailer>() != null;

    /// <summary>
    /// Sets each trailer's row in the window to its tractor's: chosen with it, or not at all. A line of trailers settles from the front in
    /// as many passes as it is long. Returns whether any row changed, so the window works its figures out again.
    /// </summary>
    public static bool Mirror(List<TransferableOneWay>? rows)
    {
        if (rows == null) return false;
        bool changed = false;
        for (int pass = 0; pass < ChainGuard; pass++)
        {
            bool any = false;
            foreach (TransferableOneWay row in rows)
            {
                if (row?.AnyThing is not VehiclePawn trailer || !IsTrailer(trailer)) continue;
                int want = 0;
                if (Hitches.Towing(trailer) is { } hitch && RowOf(rows, hitch.Vehicle) is { CountToTransfer: > 0 })
                {
                    want = System.Math.Min(1, row.GetMaximumToTransfer());
                }
                if (row.CountToTransfer == want) continue;
                row.ForceTo(want);
                any = true;
                if (want > 0 && Seen.Wants("trailers.caravan-chosen-with-tractor", trailer))
                {
                    Seen.Note("trailers.caravan-chosen-with-tractor", trailer, trailer.LabelShort + " with " + Hitches.Towing(trailer)!.Vehicle.LabelShort);
                }
            }
            if (!any) break;
            changed = true;
        }
        return changed;
    }

    static TransferableOneWay? RowOf(List<TransferableOneWay> rows, Thing thing)
    {
        foreach (TransferableOneWay row in rows)
        {
            if (row?.things != null && row.things.Contains(thing)) return row;
        }
        return null;
    }

    /// <summary>
    /// The pawns a caravan leaving the map takes as members, without its trailers. A hitched trailer goes with the vehicle towing it, in its
    /// hitch (TowTravel.TakeAlong), and must not be a member too; an unhitched one cannot move, so it stays where it stands, and the player is
    /// told. A list with nothing left but trailers is returned as it was: a caravan of nothing cannot be made.
    /// </summary>
    public static List<Pawn> WithoutTrailers(IEnumerable<Pawn> pawns, Faction? faction)
    {
        var all = new List<Pawn>(pawns);
        var kept = new List<Pawn>(all.Count);
        List<VehiclePawn>? stay = null;
        foreach (Pawn pawn in all)
        {
            if (pawn is VehiclePawn { Spawned: true } trailer && IsTrailer(trailer))
            {
                if (Hitches.Towing(trailer) is { } hitch)
                {
                    if (all.Contains(CompPhobosHitch.HeadOf(trailer)) && Seen.Wants("trailers.caravan-leaves-on-hitch", trailer))
                    {
                        Seen.Note("trailers.caravan-leaves-on-hitch", trailer, trailer.LabelShort + " behind " + hitch.Vehicle.LabelShort);
                    }
                }
                else
                {
                    (stay ??= new List<VehiclePawn>()).Add(trailer);
                }
                continue;
            }
            kept.Add(pawn);
        }
        if (kept.Count == 0) return all;
        if (stay != null && faction == Faction.OfPlayer)
        {
            foreach (VehiclePawn trailer in stay)
            {
                Messages.Message("PhobosHitch_CaravanLeftUnhitched".Translate(trailer.LabelShort.Named("TRAILER")).Resolve(), trailer,
                    MessageTypeDefOf.NeutralEvent);
                if (Seen.Wants("trailers.caravan-left-unhitched", trailer)) Seen.Note("trailers.caravan-left-unhitched", trailer, trailer.LabelShort);
            }
        }
        return kept;
    }
}

/// <summary>
/// A trailer's tile in the caravan window cannot be ticked by hand: unhitched, it says it must be hitched first; hitched, it says it goes with
/// the vehicle towing it, and its tick follows that vehicle's (<see cref="TrailersGoWithTheirTractor"/>). Vehicle Framework's own test
/// (<c>TransferableVehicleWidget.CanCaravan</c>, private, with no extension point) offered it because it needs no crew; its reason is shown
/// as a text key, so ours are keys. A postfix that only turns a yes into a no, in the forming window; splitting a caravan on the world map
/// is left alone, so a trailer that joined a caravan before this rule can still be moved between its halves.
/// </summary>
[HarmonyPatch(typeof(TransferableVehicleWidget), "CanCaravan")]
public static class TrailerTileRefusal
{
    public static void Postfix(TransferableOneWay transferable, ref bool __result, ref string disableReason)
    {
        if (!__result || CaravanFormation.formation == null) return;
        if (transferable?.AnyThing is not VehiclePawn trailer || !CaravanTrailers.IsTrailer(trailer)) return;
        __result = false;
        if (Hitches.Towing(trailer) != null)
        {
            disableReason = "PhobosHitch_CaravanGoesWithTractor";
            return;
        }
        disableReason = "PhobosHitch_CaravanNotHitched";
        if (Seen.Wants("trailers.caravan-not-hitched", trailer)) Seen.Note("trailers.caravan-not-hitched", trailer, trailer.LabelShort);
    }
}

/// <summary>
/// Each time the caravan window draws, every trailer's row is set to its tractor's (<see cref="CaravanTrailers.Mirror"/>), and the window is
/// told when one changed, so its mass, cargo room and days of food count the trailer and its load. The window has no extension point for
/// rows that depend on each other. A prefix that never skips; with no trailer on the map it reads the rows and changes nothing.
/// </summary>
[HarmonyPatch(typeof(Dialog_FormCaravan), nameof(Dialog_FormCaravan.DoWindowContents))]
public static class TrailersGoWithTheirTractor
{
    public static void Prefix(Dialog_FormCaravan __instance)
    {
        if (CaravanTrailers.Mirror(__instance.transferables)) CaravanFormation.Current?.NotifyTransferablesChanged();
    }
}

/// <summary>
/// While a caravan forms, Vehicle Framework drafts each of its vehicles every 100 ticks, and a trailer refuses (it has no engine:
/// <see cref="CompPhobosTrailer.CanDraft"/>), with a message and a sound each time. A trailer in a forming caravan is simply left undrafted,
/// which is what it is: undrafted, Vehicle Framework gives it no journey of its own, and its hitch moves it.
///
/// A prefix that skips the original, recorded as AGENTS.md asks: only for drafting a trailer whose lord is forming a caravan. Drafting by the
/// player, and every other vehicle, passes untouched, so the player still reads why a trailer cannot be drafted.
/// </summary>
[HarmonyPatch(typeof(VehicleIgnitionController), nameof(VehicleIgnitionController.Drafted), MethodType.Setter)]
public static class TrailerNotDraftedForCaravan
{
    public static bool Prefix(VehiclePawn ___vehicle, bool value) =>
        !value || ___vehicle == null || !CaravanTrailers.IsTrailer(___vehicle) || !CaravanFormingUtility.IsFormingCaravan(___vehicle);
}

/// <summary>
/// A forming caravan's lead vehicle is never a trailer. Vehicle Framework takes the vehicle with the largest footprint, and the caravan leaves
/// only once it stands at the exit: a trailer behind a smaller vehicle stops its drawbar's length short, so the caravan could wait for it.
/// A postfix on Vehicle Framework's private choice: the largest vehicle that is not a trailer, when there is one.
/// </summary>
[HarmonyPatch(typeof(LordJob_FormAndSendVehicles), "DetermineLeadVehicle")]
public static class TrailerNeverLeads
{
    public static void Postfix(LordJob_FormAndSendVehicles __instance, ref VehiclePawn ___leadVehicle)
    {
        if (___leadVehicle == null || !CaravanTrailers.IsTrailer(___leadVehicle) || __instance.vehicles == null) return;
        VehiclePawn? best = null;
        foreach (VehiclePawn vehicle in __instance.vehicles)
        {
            if (vehicle == null || CaravanTrailers.IsTrailer(vehicle)) continue;
            if (best == null || vehicle.VehicleDef.Size.Magnitude > best.VehicleDef.Size.Magnitude) best = vehicle;
        }
        if (best != null) ___leadVehicle = best;
    }
}

/// <summary>
/// A caravan leaving the map takes no trailer as a member (<see cref="CaravanTrailers.WithoutTrailers"/>): a hitched one goes in its hitch
/// as the vehicle towing it leaves, and an unhitched one stays where it stands. Without this, a trailer chosen by itself was taken from
/// wherever it stood, however far from the rest. Vehicle Framework's departure has no extension point; a prefix that only shortens the list
/// it is given, and never skips.
/// </summary>
[HarmonyPatch(typeof(CaravanHelper), nameof(CaravanHelper.ExitMapAndCreateVehicleCaravan))]
public static class TrailersLeaveOnTheirHitch
{
    public static void Prefix(ref IEnumerable<Pawn> pawns, Faction faction)
    {
        if (pawns != null) pawns = CaravanTrailers.WithoutTrailers(pawns, faction);
    }
}
