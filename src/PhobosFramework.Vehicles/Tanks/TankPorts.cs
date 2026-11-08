using System;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// One way a tank's contents meet the world. Subclass it in any mod and name it in XML with Class="Namespace.ClassName". Each returns the
/// units it moved; when it moved none, the reason is in-world text saying what is missing, or null if it has nothing to say.
/// </summary>
public abstract class TankPort
{
    /// <summary>Takes up to the given units from the world into the tank.</summary>
    public virtual float Draw(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        return 0f;
    }

    /// <summary>Gives up to the given units from the tank to the world.</summary>
    public virtual float Give(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        return 0f;
    }
}

/// <summary>Stacks of an item beside the vehicle, pumped in, and set down when pumped out. Whole items only.</summary>
public class Port_Items : TankPort
{
    public ThingDef? thing;
    public float unitsPerItem = 1f;

    public override float Draw(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        if (thing == null) return 0f;
        int wanted = (int)(most / unitsPerItem);
        if (wanted <= 0) return 0f;
        Map map = tank.Vehicle.Map;
        int taken = 0;
        foreach (IntVec3 cell in tank.ContactCells())
        {
            if (!cell.InBounds(map)) continue;
            Thing? found = map.thingGrid.ThingAt(cell, thing);
            if (found == null || found.IsForbidden(Faction.OfPlayer)) continue;
            int count = Math.Min(found.stackCount, wanted - taken);
            found.SplitOff(count).Destroy();
            taken += count;
            if (taken >= wanted) break;
        }
        if (taken == 0) reason = "PhobosVehicleSystems_TankNoStacksBeside".Translate(thing.label.Named("ITEM")).Resolve();
        return taken * unitsPerItem;
    }

    public override float Give(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        if (thing == null) return 0f;
        int count = Math.Min((int)(most / unitsPerItem), thing.stackLimit);
        if (count <= 0) return 0f;
        Thing stack = ThingMaker.MakeThing(thing);
        stack.stackCount = count;
        VehiclePawn vehicle = tank.Vehicle;
        return GenPlace.TryPlaceThing(stack, vehicle.Position, vehicle.Map, ThingPlaceMode.Near) ? count * unitsPerItem : 0f;
    }
}

/// <summary>Another vehicle within reach of the hose that burns this fuel. Pumps out only.</summary>
public class Port_VehicleFuel : TankPort
{
    public ThingDef? thing;

    public override float Give(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        VehiclePawn self = tank.Vehicle;
        foreach (Pawn pawn in self.Map.mapPawns.SpawnedPawnsInFaction(self.Faction))
        {
            if (pawn == self || pawn is not VehiclePawn other || other.Position.DistanceTo(self.Position) > tank.Props.hoseReach) continue;
            CompFueledTravel? fuel = other.CompFueledTravel;
            if (fuel == null || fuel.Props.electricPowered || fuel.Props.fuelType != thing) continue;
            float room = fuel.FuelCapacity - fuel.Fuel;
            if (room < 1f) continue;
            float given = Math.Min(most, room);
            fuel.Refuel(given);
            return given;
        }
        reason = "PhobosVehicleSystems_TankNoVehicleBeside".Translate().Resolve();
        return 0f;
    }
}

/// <summary>A hit on the shell throws out some of the load at once. The leak afterwards and the burst come from the tank's own rare tick,
/// which reads the part's health, because Combat Extended writes part health without calling reactors.</summary>
public class Reactor_PhobosTankShell : Reactor
{
    public float unitsLostPerDamage = 6f;

    public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration) =>
        vehicle.GetComp<CompPhobosTank>()?.ShellHit(dinfo.Amount * unitsLostPerDamage);
}
