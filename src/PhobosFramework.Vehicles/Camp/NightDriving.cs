using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Driving through the night (Framework 0.214.0; docs/development/vehicle-accessories.md, B3; the owner, 13 Nov 2026: a setting, off by
/// default). With the setting on, a vehicle caravan of the player's keeps going at night while every vehicle has a driver fit to drive. A
/// driver whose rest falls to the rules Def's line hands the wheel to a rested passenger of the same vehicle who can drive, and takes a passenger
/// seat (and the bunk, if one is fitted and free). With nobody to take over in any one vehicle, the whole caravan stops for the night as before.
/// Each caravan has its own switch, on by default once the setting is on. Checked every few seconds of game time; the night-rest answer reads
/// only what that check found.
/// </summary>
public class NightDriving : WorldComponent
{
    const int CheckTicks = 250;

    /// <summary>Caravans the player has told to stop at night as before, by ID. Saved.</summary>
    HashSet<int> stopping = new();
    /// <summary>Whether each caravan may drive on now, by ID, from the last check. Derived, never saved: until the first check, caravans rest.</summary>
    readonly Dictionary<int, bool> drivesOn = new();
    /// <summary>
    /// Caravans held for a meal halt, by ID, and the tick each drives on again (Framework 0.216.0). Derived, never saved: a game loaded during a
    /// halt simply drives on, its meal already cooked.
    /// </summary>
    readonly Dictionary<int, int> haltUntil = new();

    public NightDriving(World world) : base(world)
    {
    }

    public static NightDriving? Current => Find.World?.GetComponent<NightDriving>();

    public static bool Enabled => VehicleSystemsMod.Settings.driveThroughNight;

    public bool SwitchedOn(Caravan caravan) => !stopping.Contains(caravan.ID);

    public void Toggle(Caravan caravan)
    {
        if (!stopping.Remove(caravan.ID)) stopping.Add(caravan.ID);
        drivesOn.Remove(caravan.ID);
    }

    /// <summary>Holds a caravan that drives through the night for this long: a meal halt, begun by its field kitchen's cooks.</summary>
    public void Halt(Caravan caravan, int ticks)
    {
        if (ticks > 0) haltUntil[caravan.ID] = Find.TickManager.TicksGame + ticks;
    }

    /// <summary>Whether this caravan is held for a meal halt now. A halt ends by the clock, so none can hold a caravan for good.</summary>
    public bool Halted(Caravan caravan)
    {
        if (haltUntil.Count == 0 || !haltUntil.TryGetValue(caravan.ID, out int until)) return false;
        int now = Find.TickManager.TicksGame;
        // An end further off than a day is no halt of ours (the clock was set back): it ends too.
        if (now < until && until - now <= GenDate.TicksPerDay) return true;
        haltUntil.Remove(caravan.ID);
        return false;
    }

    /// <summary>Whether this caravan may drive on through the night, as last checked.</summary>
    public bool DrivesOn(Caravan caravan) => drivesOn.TryGetValue(caravan.ID, out bool on) && on;

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (Find.TickManager.TicksGame % CheckTicks != 0) return;
        drivesOn.Clear();
        if (!Enabled) return;
        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            if (caravan is not VehicleCaravan vehicles || vehicles.Faction != Faction.OfPlayer || !vehicles.Spawned || !SwitchedOn(vehicles)) continue;
            // Only at night, on the road: by day, or standing still, nothing changes hands.
            if (!vehicles.vehiclePather.Moving || !CaravanNightRestUtility.RestingNowAt(vehicles.Tile)) continue;
            drivesOn[vehicles.ID] = CheckDrivers(vehicles);
        }
    }

    /// <summary>Hands each tired driver's wheel to a rested passenger where it can; true when every vehicle then has a fit driver.</summary>
    bool CheckDrivers(VehicleCaravan caravan)
    {
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        bool all = true;
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading.ToList())
        {
            foreach (VehicleRoleHandler driving in vehicle.handlers.Where(h => h.role != null && (h.role.HandlingTypes & HandlingType.Movement) != 0).ToList())
            {
                foreach (Pawn driver in driving.thingOwner.OfType<Pawn>().ToList())
                {
                    if (NightDriveRule.Fit(driver.needs?.rest?.CurLevel, rules.nightDriverFitRest)) continue;
                    if (!HandOver(caravan, vehicle, driving, driver, rules.nightReliefRest)) all = false;
                }
            }
        }
        if (all && Seen.Wants("vehicles.night-drive", caravan.VehiclesListForReading.FirstOrDefault())) Seen.Note("vehicles.night-drive", caravan.VehiclesListForReading.FirstOrDefault(), caravan.Label);
        return all;
    }

    /// <summary>Swaps a tired driver with the most rested passenger of the same vehicle who can drive. False when nobody can take over.</summary>
    static bool HandOver(VehicleCaravan caravan, VehiclePawn vehicle, VehicleRoleHandler driving, Pawn driver, double reliefRest)
    {
        var seats = new List<(VehicleRoleHandler handler, Pawn pawn)>();
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
            if (handler.role == null || handler.role.handlingTypes != HandlingType.None) continue;
            foreach (Pawn pawn in handler.thingOwner) seats.Add((handler, pawn));
        }
        var riders = seats.Select(s => ((double?)s.pawn.needs?.rest?.CurLevel, driving.CanOperateRole(s.pawn))).ToList();
        int index = NightDriveRule.Relief(riders, reliefRest);
        if (index < 0) return false;
        (VehicleRoleHandler seat, Pawn relief) = seats[index];
        if (!Swap(vehicle, driving, driver, seat, relief)) return false;
        Messages.Message("PhobosNightDrive_HandOver".Translate(relief.LabelShort.Named("RELIEF"), driver.LabelShort.Named("DRIVER"), vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            caravan, MessageTypeDefOf.SilentInput, historical: false);
        if (Seen.Wants("vehicles.night-drive-hand-over", vehicle)) Seen.Note("vehicles.night-drive-hand-over", vehicle, driver.LabelShort + " to " + relief.LabelShort);
        // The tired driver may want the bunk now: the caravan works out its beds again.
        caravan.beds?.Notify_PawnRemoved();
        return true;
    }

    /// <summary>Changes the two over through Vehicle Framework's own seat calls; puts both back as they were if either seat refuses.</summary>
    static bool Swap(VehiclePawn vehicle, VehicleRoleHandler driving, Pawn driver, VehicleRoleHandler seat, Pawn relief)
    {
        if (!vehicle.TryRemovePawn(driver, driving)) return false;
        if (!vehicle.TryRemovePawn(relief, seat))
        {
            vehicle.TryAddPawn(driver, driving);
            return false;
        }
        if (vehicle.TryAddPawn(relief, driving))
        {
            if (vehicle.TryAddPawn(driver, seat) || vehicle.TryAddPawn(driver)) return true;
            vehicle.TryRemovePawn(relief, driving);
        }
        // Back as they were.
        bool restored = vehicle.TryAddPawn(driver, driving) & vehicle.TryAddPawn(relief, seat);
        if (!restored) VehicleSystemsMod.Logger.Warn("A driver change in " + vehicle.LabelShort + " could not be undone; check that " + driver.LabelShort + " and " + relief.LabelShort + " are aboard.");
        return false;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref stopping, "PhobosNightDrive_Stopping", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) stopping ??= new HashSet<int>();
    }
}

/// <summary>
/// The night-rest answer for a vehicle caravan: Vehicle Framework replaces the game's with its own (a prefix); this postfix, after it, turns
/// "rest" into "drive on" only when the setting is on and the last check found every vehicle with a fit driver. It reads only what the check
/// stored. One thing turns "drive on" into "rest": a meal halt (Framework 0.216.0), begun only by a caravan's cooks while it drives through
/// the night, and ended by the clock.
/// </summary>
[HarmonyPatch(typeof(Caravan), nameof(Caravan.NightResting), MethodType.Getter)]
public static class NightDrivingRest
{
    public static void Postfix(Caravan __instance, ref bool __result)
    {
        if (!NightDriving.Enabled || __instance is not VehicleCaravan || NightDriving.Current is not { } driving) return;
        if (driving.Halted(__instance))
        {
            __result = true;
            return;
        }
        if (__result && driving.DrivesOn(__instance)) __result = false;
    }
}

/// <summary>
/// The arrival time of a caravan that drives through the night (Framework 0.228.0; docs/development/vehicle-accessories.md, B3). Vehicle
/// Framework's estimate always adds the hours of each night at rest, so with driving through the night on, trips arrived well before the
/// time the caravan showed. When the setting is on, the caravan's own switch is on and every vehicle carries a relief driver
/// (<c>NightDriveRule.ExpectsToDriveThrough</c>), the estimate becomes the time left on the current tile plus each tile's own cost along the
/// rest of the path, by Vehicle Framework's own cost of moving, with no nights taken out. Otherwise its answer stands. A meal halt is not
/// counted: it is short and chosen by the player. Kept for 100 ticks, as Vehicle Framework keeps its own.
/// </summary>
[HarmonyPatch(typeof(VehicleCaravanPathingHelper), nameof(VehicleCaravanPathingHelper.EstimatedTicksToArrive), new[] { typeof(VehicleCaravan), typeof(bool) })]
public static class NightDrivingArrival
{
    const int KeepTicks = 100;
    static VehicleCaravan? keptFor;
    static int keptAt = -1;
    static int keptTicks;

    static readonly List<(bool, int)> Crews = new();

    public static void Postfix(VehicleCaravan caravan, ref int __result)
    {
        if (__result <= 0 || !NightDriving.Enabled || caravan.Faction != Faction.OfPlayer || NightDriving.Current is not { } driving || !driving.SwitchedOn(caravan)) return;
        int now = Find.TickManager.TicksGame;
        if (caravan == keptFor && now - keptAt < KeepTicks)
        {
            if (keptTicks > 0) __result = keptTicks;
            return;
        }
        keptFor = caravan;
        keptAt = now;
        keptTicks = Expects(caravan) ? Straight(caravan) : 0;
        if (keptTicks <= 0) return;
        if (keptTicks < __result && Seen.Wants("vehicles.night-drive-arrival", caravan.VehiclesListForReading.FirstOrDefault()))
        {
            Seen.Note("vehicles.night-drive-arrival", caravan.VehiclesListForReading.FirstOrDefault(),
                (__result / (float)GenDate.TicksPerDay).ToString("0.0") + " to " + (keptTicks / (float)GenDate.TicksPerDay).ToString("0.0") + " days");
        }
        __result = keptTicks;
    }

    /// <summary>Every vehicle with its driving seats filled and a rider beside them who can take the wheel.</summary>
    static bool Expects(VehicleCaravan caravan)
    {
        Crews.Clear();
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
        {
            bool drivers = true;
            int reliefs = 0;
            foreach (VehicleRoleHandler driving in vehicle.handlers)
            {
                if (driving.role == null || (driving.role.HandlingTypes & HandlingType.Movement) == 0) continue;
                if (driving.thingOwner.Count < driving.role.SlotsToOperate) drivers = false;
                foreach (VehicleRoleHandler seat in vehicle.handlers)
                {
                    if (seat.role == null || seat.role.handlingTypes != HandlingType.None) continue;
                    foreach (Pawn rider in seat.thingOwner)
                    {
                        if (driving.CanOperateRole(rider)) reliefs++;
                    }
                }
            }
            Crews.Add((drivers, reliefs));
        }
        bool expects = NightDriveRule.ExpectsToDriveThrough(Crews);
        Crews.Clear();
        return expects;
    }

    /// <summary>The ticks left on this tile and then each tile's cost to the end of the path, with no rest; 0 with no path to walk.</summary>
    static int Straight(VehicleCaravan caravan)
    {
        if (caravan.vehiclePather is not { Moving: true, curPath: { Found: true } path } pather) return 0;
        List<VehiclePawn> vehicles = caravan.VehiclesListForReading;
        int ticks = Mathf.CeilToInt(pather.nextTileCostLeft);
        int nowAbs = Find.TickManager.TicksAbs;
        for (int step = 1; step < path.NodesLeftCount; step++)
        {
            var move = new TicksPerMoveData { tile = path.Peek(step - 1), nextTile = path.Peek(step), ticksPerMove = caravan.TicksPerMove, ticksAbs = nowAbs + ticks };
            ticks += VehicleCaravan_PathFollower.CostToMove(vehicles, in move);
        }
        return ticks;
    }
}
