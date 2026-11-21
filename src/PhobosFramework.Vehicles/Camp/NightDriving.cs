using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
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
/// "rest" into "drive on" only when the setting is on and the last check found every vehicle with a fit driver. It never turns "drive on" into
/// "rest", and reads only what the check stored.
/// </summary>
[HarmonyPatch(typeof(Caravan), nameof(Caravan.NightResting), MethodType.Getter)]
public static class NightDrivingRest
{
    public static void Postfix(Caravan __instance, ref bool __result)
    {
        if (!__result || !NightDriving.Enabled || __instance is not VehicleCaravan) return;
        if (NightDriving.Current?.DrivesOn(__instance) == true) __result = false;
    }
}
