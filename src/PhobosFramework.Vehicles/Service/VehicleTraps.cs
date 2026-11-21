using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Traps and vehicles (Framework 0.213.0; docs/development/vehicle-traps.md): whether a trap may go off under a vehicle, by
/// <see cref="TrapRule"/>. An aircraft of ours in the air passes over every trap (Framework 0.171.1). A vehicle of the player's passes over
/// every trap unless it is on a drive the player ordered (owner, 21 Nov 2026), when the game decides as for a colonist who knows of the
/// trap. A drive is the player's while the vehicle is still heading for a cell the player sent it to: a right-click, a drag to set its
/// facing, or a shift-click queued after them, all of which end in Vehicle Framework's PawnGotoAction. An errand of ours, a tow, a push or a
/// caravan setting off is never one; neither is a drive the player ordered before the game was loaded, since what is remembered here is
/// never saved (derived state), so such a drive passes over traps until the player's next order.
/// </summary>
public static class VehicleTraps
{
    /// <summary>The most cells remembered for one vehicle: a long shift-click queue keeps its latest legs.</summary>
    const int MaxRememberedCells = 32;

    /// <summary>Where the player has sent each vehicle; weak, so a vehicle gone from the game takes its entry with it.</summary>
    static readonly ConditionalWeakTable<VehiclePawn, List<IntVec3>> sentTo = new();

    /// <summary>What a trap does when this vehicle comes onto its cell.</summary>
    public static TrapRule.Outcome For(VehiclePawn vehicle)
    {
        bool inAir = FlightRegistry.Any && FlightRegistry.InAir(vehicle);
        bool players = vehicle.Faction is { IsPlayer: true };
        bool settingOn = VehicleSystemsMod.Settings.errandsPassTraps;
        // A towed trailer goes where its tractor is driven.
        VehiclePawn driven = Hitches.Towing(vehicle)?.Vehicle ?? vehicle;
        bool onPlayersOrder = !inAir && players && settingOn && OnPlayersOrder(driven);
        return TrapRule.For(inAir, players, settingOn, onPlayersOrder);
    }

    /// <summary>The player sent the vehicle to this cell; <paramref name="queued"/> adds it after the drive already under way.</summary>
    public static void PlayerSent(VehiclePawn vehicle, IntVec3 cell, bool queued)
    {
        List<IntVec3> cells = sentTo.GetValue(vehicle, _ => new List<IntVec3>());
        if (!queued) cells.Clear();
        if (cells.Count >= MaxRememberedCells) cells.RemoveAt(0);
        cells.Add(cell);
    }

    /// <summary>An errand of ours has taken the vehicle over: whatever the player had sent it to is no longer its drive.</summary>
    public static void ErrandTookOver(VehiclePawn vehicle) => sentTo.Remove(vehicle);

    /// <summary>
    /// Whether the vehicle is driving to a cell the player sent it to, or has just arrived there. One pushed or towed off the spot the player
    /// last sent it to keeps that spot as its destination without driving to it, and is not on the player's order.
    /// </summary>
    public static bool OnPlayersOrder(VehiclePawn vehicle)
    {
        if (!sentTo.TryGetValue(vehicle, out List<IntVec3>? cells) || cells.Count == 0) return false;
        if (vehicle.vehiclePather is not { } pather) return false;
        LocalTargetInfo heading = pather.Destination;
        if (!heading.IsValid || !cells.Contains(heading.Cell)) return false;
        return pather.Moving || vehicle.Position == heading.Cell;
    }
}

/// <summary>
/// Remembers where the player sends a vehicle (Framework 0.213.0, <see cref="VehicleTraps"/>): every move order the player gives a vehicle,
/// one or a group, ends in Vehicle Framework's PawnGotoAction. A prefix that never skips the original, so it reads the vehicle's job before
/// the order changes it: with a Goto under way and the queue key held, Vehicle Framework queues the new cell after it
/// (VehiclePathFollower.TryOrderMoveTo), and so is it remembered here. Another prefix that skips the original (ours for an aircraft in the
/// air, the Harrier's taxiing) leaves a cell remembered that the vehicle is not heading for, which changes nothing.
/// </summary>
[HarmonyPatch]
public static class Patch_PawnGotoAction_PlayerSent
{
    static MethodBase? TargetMethod() => AccessTools.Method("Vehicles.FloatMenuOptionProvider_OrderVehicle:PawnGotoAction");

    public static void Prefix(VehiclePawn vehicle, IntVec3 gotoLoc)
    {
        bool queued = vehicle.CurJobDef == JobDefOf.Goto && KeyBindingDefOf.QueueOrder.IsDownEvent;
        VehicleTraps.PlayerSent(vehicle, gotoLoc, queued);
    }
}

/// <summary>
/// A trap does not go off under an aircraft of ours in the air (Framework 0.171.1; the owner's Cherokee, 19 Nov 2026, brought down by traps it
/// flew over on arrival, every time), nor under a vehicle of the player's on a drive the player did not order (Framework 0.213.0; owner, 21 Nov
/// 2026: errands kept setting off the colony's traps). A prefix that skips the original in those two cases only, as VTOL Vehicle's and the
/// Harrier's frameworks do for their aircraft: the original only rolls the chance and springs the trap, and skipping it covers every kind of
/// trap, other mods' included, whatever chance each gives. It runs only when a pawn first comes onto a trap's cell, so it costs nothing per
/// tick. An aircraft once down is a vehicle on the ground, and a trap beneath it may spring as under any other.
/// </summary>
[HarmonyPatch(typeof(Building_Trap), "CheckSpring")]
public static class Patch_CheckSpring_Vehicles
{
    public static bool Prefix(Building_Trap __instance, Pawn p)
    {
        if (p is not VehiclePawn vehicle) return true;
        switch (VehicleTraps.For(vehicle))
        {
            case TrapRule.Outcome.Overflown:
                Seen.Note("aircraft.trap-overflown", vehicle, __instance.def.defName);
                return false;
            case TrapRule.Outcome.PassedOver:
                Seen.Note("vehicles.trap-passed-over", vehicle, __instance.def.defName);
                return false;
            default:
                return true;
        }
    }
}
