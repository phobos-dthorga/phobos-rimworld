using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Burning tyres (Framework 0.245.0; docs/development/vehicle-fire.md, FR11; the owner's card of 23 Nov 2026). Rubber holds its heat: a fire
/// in a wheeled vehicle's running gear that is put out smoulders on for a while and may light again, unless rain puts it out; and a tyre
/// burning under pressure may burst, a blast that hurts anyone standing near and leaves the tyre in shreds. Tracked vehicles have no tyres
/// here. Two settings: relighting on (risk moderate: the vehicle burns again, and the player sees it), bursting off (risk high: people
/// near). Asked by <see cref="FireFighting.Look"/> on the rare tick, so it reads nothing unless the vehicle burns or smoulders.
/// </summary>
public static class TyreFire
{
    static readonly float RareTickHours = GenTicks.TickRareInterval / (float)ElapsedRule.TicksPerHour;

    static PhobosFireFightingRulesDef Rules => PhobosFireFightingRulesDef.Rules;

    /// <summary>A wheeled vehicle with tyre parts.</summary>
    static bool HasTyres(VehiclePawn vehicle, CompPhobosVehicleSystems? systems) =>
        systems != null && !VehicleFittings.TrackedDesign(vehicle.VehicleDef) && systems.PartsOf(Rules.tyreRole).Any();

    /// <summary>The fire on the vehicle has just gone out: a fire that had reached its running gear leaves its tyres smouldering.</summary>
    internal static void WentOut(CompPhobosFireFighting comp, FireZones wereBurning)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!VehicleSystemsMod.Settings.tyresRelight || (wereBurning & FireZones.RunningGear) == 0 || vehicle is not { Spawned: true }) return;
        if (!HasTyres(vehicle, vehicle.GetComp<CompPhobosVehicleSystems>())) return;
        comp.smoulderUntil = Find.TickManager.TicksGame + (int)(Rules.smoulderHours * GenDate.TicksPerHour);
    }

    /// <summary>A rare tick with no fire on the vehicle: smouldering tyres may light again, until the time runs out or rain puts them out.</summary>
    internal static void Smoulder(CompPhobosFireFighting comp)
    {
        if (comp.smoulderUntil < 0) return;
        VehiclePawn vehicle = comp.Vehicle;
        int now = Find.TickManager.TicksGame;
        PhobosFireFightingRulesDef rules = Rules;
        if (now >= comp.smoulderUntil || vehicle is not { Spawned: true } || !VehicleSystemsMod.Settings.tyresRelight || RainedOn(vehicle, rules))
        {
            comp.smoulderUntil = -1;
            return;
        }
        if (!Rand.Chance((float)FireFightingRule.ChanceIn(rules.relightsPerHour, RareTickHours))) return;
        comp.smoulderUntil = -1;
        // The new fire starts in the running gear, as a hit there would light it.
        comp.lastHitZone = FireZones.RunningGear;
        comp.lastHitTick = now;
        vehicle.TryAttachFire(rules.relightSize, null);
        if (vehicle.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosFireFighting_TyresRelit".Translate(vehicle.LabelShort.Named("VEHICLE")), vehicle, MessageTypeDefOf.NegativeEvent);
        }
        Seen.Note("vehicles.tyre-relit", vehicle);
    }

    /// <summary>A rare tick with the running gear burning: each sound tyre may burst, by the fire's size; at most one a tick.</summary>
    internal static void Burst(CompPhobosFireFighting comp, float fireSize)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!VehicleSystemsMod.Settings.tyreBursts || (comp.burning & FireZones.RunningGear) == 0 || vehicle is not { Spawned: true }) return;
        CompPhobosVehicleSystems? systems = vehicle.GetComp<CompPhobosVehicleSystems>();
        if (!HasTyres(vehicle, systems)) return;
        PhobosFireFightingRulesDef rules = Rules;
        float chance = (float)FireFightingRule.ChanceIn(rules.burstsPerHour * fireSize / Fire.MaxFireSize, RareTickHours);
        foreach (VehicleComponent tyre in systems!.PartsOf(rules.tyreRole).Where(p => p.Health > 0f).ToList())
        {
            if (!Rand.Chance(chance)) continue;
            systems.BreakPart(tyre);
            Map map = vehicle.Map;
            IntVec3 cell = CellOf(vehicle, tyre);
            GenExplosion.DoExplosion(cell, map, rules.burstRadius, DamageDefOf.Bomb, vehicle, rules.burstDamage,
                CombatExtendedRules.BlastPenetrationForDamage(rules.burstDamage, OptionalMods.IsActive(OptionalMods.CombatExtended)),
                ignoredThings: new List<Thing> { vehicle });
            if (vehicle.Faction == Faction.OfPlayer)
            {
                Messages.Message("PhobosFireFighting_TyreBurst".Translate(vehicle.LabelShort.Named("VEHICLE")), new TargetInfo(cell, map),
                    MessageTypeDefOf.NegativeEvent);
            }
            if (Seen.Wants("vehicles.tyre-burst", vehicle)) Seen.Note("vehicles.tyre-burst", vehicle, tyre.props.key);
            return;
        }
    }

    static bool RainedOn(VehiclePawn vehicle, PhobosFireFightingRulesDef rules) =>
        vehicle.Map.weatherManager.RainRate > rules.rainPutsOutAbove && !vehicle.Position.Roofed(vehicle.Map);

    /// <summary>The map cell of one of the part's own cells, turned with the vehicle; the vehicle's own cell when the part has none.</summary>
    static IntVec3 CellOf(VehiclePawn vehicle, VehicleComponent part)
    {
        List<IntVec2>? cells = part.props.hitbox?.cells;
        if (cells == null || cells.Count == 0) return vehicle.Position;
        IntVec2 offset = cells.RandomElement().MirrorRotatedBy(vehicle.Rotation, vehicle.VehicleDef.Size);
        IntVec3 cell = new(vehicle.Position.x + offset.x, 0, vehicle.Position.z + offset.z);
        return cell.InBounds(vehicle.Map) ? cell : vehicle.Position;
    }
}
