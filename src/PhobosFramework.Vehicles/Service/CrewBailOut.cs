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
/// a few reads per vehicle, on every faction's. The player's crews leave only a burning vehicle, by <see cref="PlayerRareTick"/> (a setting); for a dead
/// hull <see cref="Alert_PhobosCrewInDanger"/> tells the player.
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
        return CrewRule.Danger(crew, comp.FireShare > 0f, designMoves, !Mobility.StoppedByDamage(vehicle, comp.Props), AnyWeaponWorks(vehicle));
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
        if (Seen.Wants("vehicles.crew-bailed-out", vehicle)) Seen.Note("vehicles.crew-bailed-out", vehicle, danger.ToString());
        vehicle.DisembarkAll();
    }

    /// <summary>
    /// The rare tick for the player's vehicle (Framework 0.245.0; docs/development/vehicle-fire.md, FR8; the owner's card of 23 Nov 2026: "crews
    /// bail out"): a crew gets out of a burning vehicle by itself, as other factions' crews do, once it has stopped, so a driver taking it
    /// clear of danger is never pulled out at the wheel, and never in the air. Only fire, never a dead hull: staying in one is the player's
    /// call. Read after the suppression systems have had their go this tick, from the fires themselves, so a fire just put out never empties
    /// the vehicle. A setting, on (risk low, the agent's grade: it saves people). Costs one list read while there is a crew aboard.
    /// </summary>
    public static void PlayerRareTick(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (vehicle == null || vehicle.Faction != Faction.OfPlayer || !vehicle.Spawned || !VehicleSystemsMod.Settings.playerCrewsBailOut
            || !PhobosRulesDef.Current<PhobosKnockOutRulesDef>().crewsBailOut) return;
        int aboard = vehicle.AllPawnsAboard.Count;
        if (aboard == 0 || Phobos.Framework.Environment.AttachedFires.Biggest(vehicle) <= 0f) return;
        if (vehicle.vehiclePather?.Moving == true || When_Airborne.Airborne(comp, live: true)) return;
        // A vehicle the player has drafted keeps its crew, to drive it out of the fire (the owner's card of 25 Nov 2026, fault-read D11): the
        // order wins, with its consequence, until the fire reaches the crew compartment.
        if (vehicle.Drafted && !(FireFighting.CompOf(vehicle) is { } fire && (fire.burning & FireZones.CrewCompartment) != 0)) return;
        vehicle.DisembarkAll();
        Messages.Message("PhobosVehicleSystems_CrewBailedOutOwn".Translate(vehicle.LabelShort.Named("VEHICLE"), aboard.Named("COUNT")), vehicle,
            MessageTypeDefOf.NegativeEvent);
        if (Seen.Wants("vehicles.own-crew-bailed-out", vehicle)) Seen.Note("vehicles.own-crew-bailed-out", vehicle, aboard.ToString());
    }
}
