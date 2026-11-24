using System;
using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>One electronic jamming field on a map: where, how wide, and whose (a field never jams its own faction).</summary>
public readonly struct JamField
{
    public readonly Vector3 centre;
    public readonly float radius;
    public readonly Faction? faction;
    public readonly Thing? source;

    public JamField(Vector3 centre, float radius, Faction? faction, Thing? source)
    {
        this.centre = centre;
        this.radius = radius;
        this.faction = faction;
        this.source = source;
    }
}

/// <summary>
/// Jamming fields jam our systems (Framework 0.225.0; owner, 22 Nov 2026: "fields jam our systems"; docs/development/ea6b-prowler.md, step 2).
/// Any mod's jammer registers where its fields are (the EA-6B Prowler's, through D'thorga Patches; our own Busbar's ground jammer since 0.227.0,
/// built in); inside a field of a faction
/// hostile to it, a vehicle's radar and warning receiver (the rules Def's list) read as not working, so everything that relies on them (aim
/// by radar in rain and at night, the dodge's warning, the lock-on warning) follows. Any faction's vehicles, both ways: the player's Prowler
/// jams raiders' aircraft as theirs jams the player's. Fields are read again at most every second of game time per map and never saved.
/// </summary>
public static class JamFields
{
    const int RefreshTicks = 60;

    static readonly List<Func<Map, IEnumerable<JamField>>> sources = new() { GroundJammers };
    static readonly Dictionary<Map, (int tick, List<JamField> fields)> byMap = new();
    /// <summary>The player's vehicles told they are jammed, so each is told once while it stays inside.</summary>
    static readonly HashSet<VehiclePawn> told = new();
    static Game? game;

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>Our own: each Busbar generator set whose jammer mast is working (Framework 0.227.0; docs/development/ea6b-prowler.md, step 4).</summary>
    static IEnumerable<JamField> GroundJammers(Map map)
    {
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            if (pawns[i] is not VehiclePawn vehicle || vehicle.GetComp<CompPhobosGeneratorSet>() is not { JammerLive: true } set) continue;
            if (Seen.Wants("vehicles.ground-jammer", vehicle)) Seen.Note("vehicles.ground-jammer", vehicle, set.JammerRadius.ToString("0") + " cells");
            yield return new JamField(vehicle.DrawPos, set.JammerRadius, vehicle.Faction, vehicle);
        }
    }

    /// <summary>Registers a mod's jammer: called with a map, it lists the fields there now. Called once, at start-up.</summary>
    public static void Register(Func<Map, IEnumerable<JamField>> source)
    {
        if (source != null && !sources.Contains(source)) sources.Add(source);
    }

    /// <summary>Whether any jammer could have a field: always, since our own ground jammers are built in.</summary>
    public static bool Any => sources.Count > 0;

    static void Check()
    {
        if (game == Current.Game) return;
        byMap.Clear();
        told.Clear();
        game = Current.Game;
    }

    /// <summary>The fields on this map now, read again at most once a second.</summary>
    public static IReadOnlyList<JamField> On(Map map)
    {
        Check();
        int now = Find.TickManager.TicksGame;
        if (byMap.TryGetValue(map, out (int tick, List<JamField> fields) cached) && now - cached.tick < RefreshTicks && now >= cached.tick) return cached.fields;
        var fields = new List<JamField>();
        foreach (Func<Map, IEnumerable<JamField>> source in sources)
        {
            try
            {
                fields.AddRange(source(map));
            }
            catch (Exception e)
            {
                VehicleSystemsMod.Logger.Warn("A jammer's fields could not be read and are left out until the next start: " + e.Message);
                sources.Remove(source);
                break;
            }
        }
        // Maps that have closed are let go (fault-read 25 Nov 2026, D5): keyed by map, the cache kept every temporary map it was asked
        // about, and all it held, until the next load; and so are vehicles told while on one.
        if (byMap.Count > 0) PruneClosed();
        byMap[map] = (now, fields);
        return fields;
    }

    static void PruneClosed()
    {
        List<Map>? gone = null;
        foreach (Map key in byMap.Keys)
        {
            if (key.Disposed || !Find.Maps.Contains(key)) (gone ??= new List<Map>()).Add(key);
        }
        if (gone == null) return;
        foreach (Map key in gone) byMap.Remove(key);
        told.RemoveWhere(vehicle => vehicle.Destroyed || vehicle.Discarded);
    }

    /// <summary>A field of a faction hostile to this thing's that covers where it is now, or null.</summary>
    public static JamField? Covering(Thing thing)
    {
        if (!Any || !thing.Spawned) return null;
        Faction? own = thing.Faction;
        Vector3 at = thing.DrawPos;
        foreach (JamField field in On(thing.Map))
        {
            if (field.source == thing || field.faction == own) continue;
            if (field.faction != null && own != null && !field.faction.HostileTo(own)) continue;
            if (JamFieldRule.Inside(at.x - field.centre.x, at.z - field.centre.z, field.radius)) return field;
        }
        return null;
    }

    /// <summary>Whether this capability of the vehicle is jammed now. The player is told once while their vehicle stays inside a field.</summary>
    public static bool Jams(VehiclePawn vehicle, string quantity)
    {
        if (!Any || !JamFieldRule.Blinds(Rules.jammedQuantities, quantity)) return false;
        if (Covering(vehicle) is not { } field)
        {
            told.Remove(vehicle);
            return false;
        }
        if (vehicle.Faction == Faction.OfPlayer && told.Add(vehicle))
        {
            Messages.Message("PhobosJam_Jammed".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), vehicle, MessageTypeDefOf.NegativeEvent, historical: false);
        }
        if (Seen.Wants("vehicles.jammed", vehicle, quantity)) Seen.Note("vehicles.jammed", vehicle, field.source?.LabelShort ?? "a field", quantity);
        return true;
    }
}
