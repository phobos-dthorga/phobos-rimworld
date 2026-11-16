using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Crews leave a burning vehicle or a dead hull (Framework 0.115.0; owner, 16 Nov 2026: AI crews leave by themselves, the player's crews get
/// an alert; docs/development/vehicle-knocked-out-systems.md, C2). Vehicle Framework only empties a vehicle when it is destroyed or has stood
/// idle five hours, so a crew sat in a burning or knocked-out hull until it was destroyed around them, and an enemy's vehicle could only be
/// taken by destroying it. Read on the rare tick from Vehicle Framework's own answers (crew aboard, speed, weapons) and our fire share; costs
/// a few reads per vehicle, on every faction's. Never touches the player's crews: <see cref="Alert_PhobosCrewInDanger"/> tells the player.
/// </summary>
public static class CrewBailOut
{
    /// <summary>The danger this vehicle's crew is in now.</summary>
    public static CrewDanger Danger(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (vehicle == null || !vehicle.Spawned || vehicle.statHandler == null) return CrewDanger.None;
        bool crew = vehicle.AllPawnsAboard.Count > 0;
        if (!crew) return CrewDanger.None;
        bool designMoves = vehicle.VehicleDef.GetStatValueAbstract(VehicleStatDefOf.MoveSpeed) > Mobility.StoppedAtOrBelow;
        return CrewRule.Danger(crew, comp.FireShare > 0f, designMoves, !Mobility.Stopped(vehicle), AnyWeaponWorks(vehicle));
    }

    /// <summary>Whether any weapon that fires (not a fixed smoke launcher) is still on its mount and free to work.</summary>
    public static bool AnyWeaponWorks(VehiclePawn vehicle)
    {
        var turrets = vehicle.CompVehicleTurrets?.Turrets;
        if (turrets == null) return false;
        foreach (VehicleTurret turret in turrets)
        {
            if (turret.def == null || turret.def.turretType == TurretType.Static) continue;
            if (!turret.ComponentDisabled && !turret.TurretRestricted) return true;
        }
        return false;
    }

    /// <summary>The rare tick for another faction's vehicle: its crew gets out when in danger, and fights or flees on foot as its orders say.</summary>
    public static void RareTick(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (vehicle == null || vehicle.Faction == Faction.OfPlayer || !PhobosRulesDef.Current<PhobosKnockOutRulesDef>().crewsBailOut) return;
        CrewDanger danger = Danger(comp);
        if (danger == CrewDanger.None) return;
        if (VehicleSystemsMod.Logger.DebugEnabled)
        {
            VehicleSystemsMod.Logger.Debug(vehicle.LabelShort + " (" + (vehicle.Faction?.Name ?? "no faction") + "): crew bails out, " + danger);
        }
        Seen.Note("vehicles.crew-bailed-out", vehicle, danger.ToString());
        vehicle.DisembarkAll();
    }
}
