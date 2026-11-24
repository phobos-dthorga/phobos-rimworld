using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>One of another faction's people sitting in their own parked vehicle at their base. Saved with the map.</summary>
public sealed class SeatedDefender : IExposable
{
    public Pawn pawn = null!;
    public VehiclePawn vehicle = null!;
    public int since;
    /// <summary>The tick they climb out, once something has alerted them; -1 until then.</summary>
    public int outAt = -1;

    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, "PhobosDefender_Pawn");
        Scribe_References.Look(ref vehicle, "PhobosDefender_Vehicle");
        Scribe_Values.Look(ref since, "PhobosDefender_Since");
        Scribe_Values.Look(ref outAt, "PhobosDefender_OutAt", -1);
    }
}

/// <summary>
/// Defenders in their trucks (Framework 0.292.0; docs/development/vehicle-parked-use.md, other factions; the owner's pick of 25 Nov 2026). At a
/// hostile faction's base the player attacks, a few of its people are sitting in their own parked vehicles when the map is made, as the
/// player's own colonists sit in theirs. They keep their orders aboard (neither the game's despawn nor Vehicle Framework's boarding touches a
/// lord) and climb out once one of the player's people comes near, the vehicle is hit, or the rules' hours pass: a moment lost, and a truck
/// worth sneaking up on. Only with "People use parked vehicles" on. Costs one flag a tick on every map, and a short list on a base's.
/// </summary>
public class MapComponent_PhobosBaseDefenders : MapComponent
{
    bool looked;
    List<SeatedDefender> seated = new();

    const int CheckTicks = 60;
    /// <summary>Seat people only on a map this young (a save loaded in the middle of an attack never gets new sitters).</summary>
    const int NewMapTicks = 2500;

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    public MapComponent_PhobosBaseDefenders(Map map) : base(map)
    {
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref looked, "PhobosDefenders_Looked");
        Scribe_Collections.Look(ref seated, "PhobosDefenders_Seated", LookMode.Deep);
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        seated ??= new List<SeatedDefender>();
        // Who is aboard is known only after Vehicle Framework's own load step; the look drops anyone no longer aboard.
        seated.RemoveAll(d => d.pawn == null || d.vehicle == null);
    }

    public override void MapComponentTick()
    {
        if (!looked)
        {
            // The first tick after the map is made: everyone is on it by now (never from a spawn setup; the sling's landing fault).
            looked = true;
            if (Find.TickManager.TicksGame - map.generationTick <= NewMapTicks) Seat();
            return;
        }
        if (seated.Count == 0 || Find.TickManager.TicksGame % CheckTicks != 0) return;
        Look();
    }

    /// <summary>The base belongs to a faction hostile to the player: a few of its people sit in its parked vehicles' passenger seats.</summary>
    void Seat()
    {
        if (!VehicleVisits.Enabled || map.Parent is not Settlement { Faction: { } faction } || faction == Faction.OfPlayer || !faction.HostileTo(Faction.OfPlayer)) return;
        List<Pawn> theirs = map.mapPawns.SpawnedPawnsInFaction(faction).ToList();
        List<VehiclePawn> vehicles = theirs.OfType<VehiclePawn>().InRandomOrder().ToList();
        if (vehicles.Count == 0) return;
        List<Pawn> people = theirs.Where(p => p is not VehiclePawn && p.RaceProps.Humanlike && !p.Downed && !p.InMentalState && !p.IsPrisoner).InRandomOrder().ToList();
        int now = Find.TickManager.TicksGame;
        foreach (VehiclePawn vehicle in vehicles)
        {
            if (seated.Count >= Rules.baseDefendersMax || people.Count == 0) break;
            if (!Rand.Chance(Rules.baseDefenderChance)) continue;
            Pawn candidate = people[people.Count - 1];
            VehicleRoleHandler? seat = vehicle.handlers?.FirstOrDefault(h => h.role != null && h.role.HandlingTypes == HandlingType.None
                && h.AreSlotsAvailableAndReservable && h.CanOperateRole(candidate));
            if (seat == null) continue;
            Pawn pawn = people[people.Count - 1];
            people.RemoveAt(people.Count - 1);
            if (!vehicle.TryAddPawn(pawn, seat)) continue;
            seated.Add(new SeatedDefender { pawn = pawn, vehicle = vehicle, since = now });
            if (Seen.Wants("vehicles.base-defenders", vehicle)) Seen.Note("vehicles.base-defenders", vehicle, pawn.LabelShort + " (" + faction.Name + ")");
        }
    }

    /// <summary>Once a second: who has been alerted, and who climbs out now.</summary>
    void Look()
    {
        int now = Find.TickManager.TicksGame;
        float noticeSquared = Rules.baseDefenderNoticeCells * Rules.baseDefenderNoticeCells;
        List<Pawn> players = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
        foreach (SeatedDefender defender in seated.ToList())
        {
            VehiclePawn vehicle = defender.vehicle;
            if (defender.pawn == null || defender.pawn.Dead || vehicle == null || !vehicle.AllPawnsAboard.Contains(defender.pawn))
            {
                seated.Remove(defender);
                continue;
            }
            if (!vehicle.Spawned) continue;
            if (defender.outAt < 0)
            {
                string? why = null;
                if (players.Any(p => p.Position.DistanceToSquared(vehicle.Position) <= noticeSquared)) why = "near";
                else if (now - defender.since >= Rules.baseDefenderMaxHours * GenDate.TicksPerHour) why = "time";
                if (why != null) Alert(defender, why);
            }
            if (defender.outAt >= 0 && now >= defender.outAt) Out(defender);
        }
    }

    /// <summary>Something has alerted them: they climb out after a moment's scramble (authored, the rules' ticks at most).</summary>
    void Alert(SeatedDefender defender, string why)
    {
        defender.outAt = Find.TickManager.TicksGame + Rand.RangeInclusive(Rules.baseDefenderScrambleTicks / 2, Rules.baseDefenderScrambleTicks);
        if (Seen.Wants("vehicles.base-defenders-out", defender.vehicle, why)) Seen.Note("vehicles.base-defenders-out", defender.vehicle, defender.pawn.LabelShort, why);
    }

    void Out(SeatedDefender defender)
    {
        seated.Remove(defender);
        if (defender.vehicle.Spawned && defender.vehicle.AllPawnsAboard.Contains(defender.pawn)) defender.vehicle.DisembarkPawn(defender.pawn);
    }

    /// <summary>The vehicle was hit (Vehicle Framework's damage event): its sitters are alerted at once.</summary>
    public static void Hit(VehiclePawn vehicle)
    {
        if (vehicle.Faction == Faction.OfPlayer || vehicle.Map?.GetComponent<MapComponent_PhobosBaseDefenders>() is not { } component) return;
        foreach (SeatedDefender defender in component.seated)
        {
            if (defender.vehicle == vehicle && defender.outAt < 0) component.Alert(defender, "hit");
        }
    }
}
