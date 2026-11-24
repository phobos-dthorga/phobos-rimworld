using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Marks an aircraft on our own flight that raiders may come in (Framework 0.157.0), put on its VehicleDef in XML: the least tech level a
/// faction needs to field it, and how often it is picked among others.
/// </summary>
public class PhobosRaiderAircraftExtension : DefModExtension
{
    public TechLevel minTechLevel = TechLevel.Industrial;
    public float weight = 1f;
}

/// <summary>
/// A raid that arrives by aircraft (D'thorga Framework 0.157.0; docs/development/vehicle-flight-over-the-map.md, slice 6's raider half; the
/// owner's answers of 19 Nov 2026): one of the game's own ways for raiders to arrive, as drop pods are, so raid points are unchanged. Each
/// aircraft of ours fit for the faction comes in over the edge with the raiders aboard, puts them down by one of three ways chosen by chance,
/// and leaves the way it came. Whoever does not fit walks in from that edge. Off with the player's setting.
/// </summary>
public class PawnsArrivalModeWorker_PhobosAirAssault : PawnsArrivalModeWorker
{
    /// <summary>The most aircraft one raid brings (authored, as VTOL Raids' own limit of three).</summary>
    const int MostAircraft = 3;

    public override bool CanUseWith(IncidentParms parms) =>
        VehicleSystemsMod.Settings.raidersArriveByAir && !VehicleSystemsMod.Settings.showOnly && base.CanUseWith(parms)
        && parms.faction is { def.humanlikeFaction: true } faction && RaiderFlight.AircraftFor(faction) != null;

    public override bool TryResolveRaidSpawnCenter(IncidentParms parms) =>
        new PawnsArrivalModeWorker_EdgeWalkIn { def = def }.TryResolveRaidSpawnCenter(parms);

    public override void Arrive(List<Pawn> pawns, IncidentParms parms)
    {
        Map map = (Map)parms.target;
        var left = pawns.ToList();
        for (int i = 0; i < MostAircraft && left.Count > 0; i++)
        {
            if (!RaiderFlight.Launch(parms, map, left, i)) break;
        }
        // Whoever did not fit walks in from the edge the aircraft came over, as the game's own walk-in puts them.
        foreach (Pawn pawn in left)
        {
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 8), map, parms.spawnRotation);
        }
    }
}

/// <summary>The raiders' aircraft: which to send, how to crew and fill it, and where it puts its people down.</summary>
public static class RaiderFlight
{
    /// <summary>Degrees between the bearings of a raid's aircraft, so they come in side by side (authored).</summary>
    const float SpreadDegrees = 12f;

    /// <summary>Candidate spots looked at for a landing (authored: enough to find open ground on a crowded map, once a raid).</summary>
    const int Candidates = 400;

    /// <summary>A raiders' aircraft for this faction, by weight, or null where none of ours suits it.</summary>
    public static VehicleDef? AircraftFor(Faction faction)
    {
        var fit = DefDatabase<VehicleDef>.AllDefsListForReading
            .Where(d => d.GetModExtension<PhobosRaiderAircraftExtension>() is { } ext && faction.def.techLevel >= ext.minTechLevel
                && d.comps.Any(c => c is CompProperties_PhobosFlight))
            .ToList();
        return fit.Count == 0 ? null : fit.RandomElementByWeight(d => d.GetModExtension<PhobosRaiderAircraftExtension>()!.weight);
    }

    /// <summary>
    /// Makes one aircraft for the raid, crews it with two of the faction's people, seats as many of <paramref name="left"/> as fit (taking
    /// them out of it), and brings it in over the edge, flying. False when it could not, with nobody taken.
    /// </summary>
    public static bool Launch(IncidentParms parms, Map map, List<Pawn> left, int index)
    {
        Faction faction = parms.faction;
        if (AircraftFor(faction) is not { } def) return false;
        VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(def, faction);
        if (vehicle?.GetComp<CompPhobosFlight>() is not { } flight)
        {
            vehicle?.Destroy();
            return false;
        }
        if (vehicle.CompFueledTravel is { } fuel) fuel.Refuel(fuel.FuelCapacity);
        // The crew to fly it: the faction's own people, not part of the raid, who leave with it.
        PawnKindDef crewKind = faction.def.basicMemberKind ?? PawnKindDefOf.Villager;
        foreach (VehicleRoleHandler handler in vehicle.handlers.Where(h => (h.role.HandlingTypes & HandlingType.Movement) != 0))
        {
            while (handler.AreSlotsAvailable)
            {
                Pawn crew = PawnGenerator.GeneratePawn(new PawnGenerationRequest(crewKind, faction, PawnGenerationContext.NonPlayer, map.Tile,
                    forceGenerateNewPawn: true, mustBeCapableOfViolence: false));
                if (!vehicle.TryAddPawn(crew, handler)) break;
            }
        }
        var seated = new List<Pawn>();
        foreach (Pawn pawn in left)
        {
            // Fault-read R2 and R4 (Framework 0.163.0): people only, awake; animals would go to the hold and never come out.
            if (pawn.RaceProps.Humanlike && !pawn.Downed && vehicle.TryAddPawn(pawn)) seated.Add(pawn);
        }
        if (seated.Count == 0 || !vehicle.HasEnoughOperators)
        {
            Unseat(vehicle, seated);
            vehicle.Destroy();
            return false;
        }
        Vector3 middle = map.Center.ToVector3Shifted();
        double bearing = FlightPathRule.HeadingTo(parms.spawnCenter.x + 0.5f - middle.x, parms.spawnCenter.z + 0.5f - middle.z, 0)
            + (index == 0 ? 0 : (index % 2 == 1 ? 1 : -1) * SpreadDegrees * ((index + 1) / 2));
        if (!FlightArrival.EnterAt(vehicle, map, bearing))
        {
            Unseat(vehicle, seated);
            if (!vehicle.Destroyed) vehicle.Destroy();
            return false;
        }
        left.RemoveAll(seated.Contains);
        // Framework 0.288.0 (docs/development/airdrop.md; the owner's card of 25 Nov 2026): an aircraft that can drop brings some raids in
        // by parachute, with a packed parachute for each of its raiders.
        bool chutes = vehicle.def.GetModExtension<PhobosAirdropExtension>() is { onlyWithStockSetting: false } drops
            && Rand.Chance((float)RaidLandingRule.ParachuteShare);
        if (chutes)
        {
            Thing packed = ThingMaker.MakeThing(AirdropDefOf.Phobos_PersonnelParachute);
            packed.stackCount = seated.Count + vehicle.def.GetModExtension<PhobosAirdropExtension>()!.raiderSpareChutes;
            vehicle.inventory.innerContainer.TryAdd(packed);
        }
        flight.BeginRaid(chutes ? RaidLanding.Parachute : RaidLandingRule.Choose(Rand.Value));
        return true;
    }

    static void Unseat(VehiclePawn vehicle, List<Pawn> seated)
    {
        foreach (Pawn pawn in vehicle.AllPawnsAboard.ToList())
        {
            vehicle.RemovePawn(pawn);
            if (!seated.Contains(pawn) && !pawn.Destroyed) pawn.Destroy();
        }
    }

    /// <summary>Open ground for ropes (fault-read H3): at least eight standable, unroofed cells within four of the hover point (authored), so
    /// its people come down outside, not on a roof or into a room.</summary>
    static bool OpenAround(Map map, IntVec3 c)
    {
        int open = 0;
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(c, 4f, useCenter: true))
        {
            if (cell.InBounds(map) && cell.Standable(map) && !cell.Roofed(map) && ++open >= 8) return true;
        }
        return false;
    }

    /// <summary>The middle of the colony: its buildings' mean position, else its people's, else the map's middle.</summary>
    public static IntVec3 ColonyMiddle(Map map)
    {
        List<Building> buildings = map.listerBuildings.allBuildingsColonist;
        IEnumerable<IntVec3> cells = buildings.Count > 0 ? buildings.Select(b => b.Position) : map.mapPawns.FreeColonistsSpawned.Select(p => p.Position);
        int n = 0;
        float sx = 0f, sz = 0f;
        foreach (IntVec3 c in cells)
        {
            sx += c.x;
            sz += c.z;
            n++;
        }
        return n == 0 ? map.Center : new IntVec3(Mathf.RoundToInt(sx / n), 0, Mathf.RoundToInt(sz / n));
    }

    /// <summary>The colony's turrets and how far each reaches, cells (a turret whose weapon cannot be read is given 25, authored).</summary>
    static List<(IntVec3 At, float Reach)> Turrets(Map map)
    {
        var found = new List<(IntVec3, float)>();
        foreach (Building building in map.listerBuildings.allBuildingsColonist)
        {
            if (building is not Building_Turret turret) continue;
            float reach = 25f;
            try
            {
                if (turret.AttackVerb?.verbProps is { } props && props.range > 0f) reach = props.range;
            }
            catch (Exception)
            {
                // A turret of another mod that cannot say: the default stands.
            }
            found.Add((turret.Position, reach));
        }
        return found;
    }

    /// <summary>
    /// The best spot for this way of putting people down (RaidLandingRule): a landing spot the aircraft's footprint takes, facing the colony,
    /// or, for the ropes, a point over open ground. Raiders know the ground, so unseen ground is no bar. False when none was found.
    /// </summary>
    public static bool FindZone(VehiclePawn vehicle, RaidLanding mode, out IntVec3 cell, out Rot4 rot, List<IntVec3>? taken = null)
    {
        float spacing = SpotSpacing(vehicle);
        cell = IntVec3.Invalid;
        rot = Rot4.North;
        Map map = vehicle.Map;
        IntVec3 middle = ColonyMiddle(map);
        List<(IntVec3 At, float Reach)> turrets = Turrets(map);
        double best = double.PositiveInfinity;
        for (int i = 0; i < Candidates; i++)
        {
            Vector2 offset = Rand.InsideUnitCircle * (float)RaidLandingRule.FarthestCells;
            var c = new IntVec3(middle.x + Mathf.RoundToInt(offset.x), 0, middle.z + Mathf.RoundToInt(offset.y));
            if (!c.InBounds(map)) continue;
            // Framework 0.174.0: never on top of a spot another aircraft of the raid holds.
            if (taken != null && taken.Any(s => s.InHorDistOf(c, spacing))) continue;
            bool covered = turrets.Any(t => t.At.InHorDistOf(c, t.Reach + (float)RaidLandingRule.TurretMarginCells));
            double score = RaidLandingRule.Score(mode, (c - middle).LengthHorizontal, covered);
            if (score >= best) continue;
            Rot4 facing = Rot4.FromAngleFlat((middle - c).AngleFlat);
            bool fits = mode is RaidLanding.RopeClose or RaidLanding.Parachute
                ? OpenAround(map, c)
                : FlightRules.SpotRefusal(vehicle, c, facing, forced: false, knowsGround: true) == null;
            if (!fits) continue;
            best = score;
            cell = c;
            rot = facing;
        }
        return cell.IsValid;
    }

    /// <summary>
    /// The spots the other raiders' aircraft on this map hold (Framework 0.174.0; the audit of 19 Nov 2026, finding 6: every aircraft of a
    /// raid made for the same best spot and fell back on retries). Read once as an aircraft plans its approach: a raid's few aircraft.
    /// </summary>
    public static List<IntVec3> SpotsTaken(VehiclePawn vehicle)
    {
        var spots = new List<IntVec3>();
        Map? map = vehicle.Map;
        if (map == null) return spots;
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn == vehicle || pawn is not VehiclePawn other || other.GetComp<CompPhobosFlight>() is not { IsRaider: true } flight) continue;
            IntVec3 spot = flight.RaidSpot;
            if (spot.IsValid) spots.Add(spot);
        }
        return spots;
    }

    /// <summary>How far apart two raiders' spots stay, cells: two of this aircraft's longest sides and a margin (authored), so footprints and
    /// their rotor wash never overlap.</summary>
    static float SpotSpacing(VehiclePawn vehicle)
    {
        IntVec2 size = vehicle.VehicleDef.Size;
        return 2f * Mathf.Max(size.x, size.z) + SpotMarginCells;
    }

    const float SpotMarginCells = 4f;
}

/// <summary>Where a raiders' aircraft is in its raid.</summary>
public enum RaiderStage
{
    Inbound,
    Unloading,
    Leaving,
}

/// <summary>A raiders' aircraft's plan (Framework 0.157.0), saved on its flight comp.</summary>
public class RaiderPlan : IExposable
{
    public RaidLanding mode;
    public RaiderStage stage;
    public int tries;
    public float exitX, exitZ;

    public void ExposeData()
    {
        Scribe_Values.Look(ref mode, "mode");
        Scribe_Values.Look(ref stage, "stage");
        Scribe_Values.Look(ref tries, "tries");
        Scribe_Values.Look(ref exitX, "exitX");
        Scribe_Values.Look(ref exitZ, "exitZ");
    }
}
