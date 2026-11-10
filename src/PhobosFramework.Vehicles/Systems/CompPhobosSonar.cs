using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Phobos.VehicleSystems;

[DefOf]
public static class SonarDefOf
{
    public static VehicleStatDef Phobos_SonarRange = null!;

    static SonarDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SonarDefOf));
}

/// <summary>An active sonar set a vehicle carries in the water. Every figure is authored for play (docs/development/vehicle-seabed-crawler.md, "The sonar set as built").</summary>
public class CompProperties_PhobosSonar : VehicleCompProperties
{
    /// <summary>How long the set needs between pings.</summary>
    public float cooldownSeconds = 30f;
    /// <summary>The share of its range at which a ping finds a boat resting on the bottom, among the seabed's own echoes.</summary>
    public float settledShare = 0.5f;
    /// <summary>How many times its own range the ping itself is heard at, by anyone in the water.</summary>
    public float heardFactor = 2f;
    /// <summary>How long a ping gives the boat away: it is followed for this long, as if spotted, while it is under.</summary>
    public float givenAwaySeconds = 60f;
    /// <summary>How long what a ping found stays drawn on the map while the boat is selected.</summary>
    public float showSeconds = 10f;
    /// <summary>The chance per look, about every four seconds, that a crew other than the player's pings while hunting.</summary>
    public float crewPingChance = 0.15f;
    /// <summary>Plays at the boat with every ping, whoever sends it: a ping is no secret. Null for silence.</summary>
    public SoundDef? pingSound;
    /// <summary>The ping button's picture.</summary>
    public string pingIcon = "Phobos/VehicleSystems/SonarPing";

    public CompProperties_PhobosSonar() => compClass = typeof(CompPhobosSonar);
}

/// <summary>
/// An active sonar set. A ping finds every hostile thing in the water within reach, moving or still, and every boat that is under, and spots them; it also gives the pinging boat away for a
/// while, and anyone in the water hears it from twice as far, with or without a set of their own. Works the same for any faction's boat, and a crew other than the player's pings by itself
/// while it hunts. Nothing is saved: after a load the set is ready again and the boat is no longer given away.
/// </summary>
public class CompPhobosSonar : VehicleComp
{
    const float TicksPerSecond = 60f;
    const int Never = -1;

    readonly List<IntVec3> found = new();
    int lastPingTick = Never;
    int givenAwayUntilTick;
    int shownUntilTick;
    float shownRange;

    public CompProperties_PhobosSonar Props => (CompProperties_PhobosSonar)props;

    float FittedRange => Vehicle.GetStatValue(SonarDefOf.Phobos_SonarRange);
    bool Fitted => FittedRange > 0f;
    int Now => Find.TickManager.TicksGame;
    int CooldownTicks => (int)(Props.cooldownSeconds * TicksPerSecond);
    bool Ready => SonarRule.Ready(Now, lastPingTick, CooldownTicks);
    bool InWater => Vehicle.Spawned && (Vehicle.Position.GetTerrain(Vehicle.Map)?.IsWater ?? false);

    /// <summary>Someone aboard awake and up to work the set; mechanoid crews count, since a screen needs no ears.</summary>
    bool AnyoneToWorkIt => Vehicle.AllPawnsAboard.Any(p => !p.Dead && !p.Downed && p.Awake());

    float Range => SonarRule.Range(FittedRange, AnyoneToWorkIt);

    /// <summary>Why it cannot ping now, or null.</summary>
    public string? PingRefusal()
    {
        if (!InWater) return "PhobosSonar_RefuseOutOfWater".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (!AnyoneToWorkIt) return "PhobosSonar_RefuseNobody".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (!Ready) return "PhobosSonar_RefuseCooling".Translate(SonarRule.SecondsUntilReady(Now, lastPingTick, CooldownTicks, TicksPerSecond).Named("SECONDS")).Resolve();
        return null;
    }

    /// <summary>One ping: what it finds, the boat it gives away and everyone who hears it. The one place a ping is decided, for the player's button and another crew's choice alike.</summary>
    public void Ping()
    {
        string? refused = PingRefusal();
        if (refused != null)
        {
            if (Vehicle.Faction == Faction.OfPlayer) Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        float range = Range;
        lastPingTick = Now;
        givenAwayUntilTick = Now + (int)(Props.givenAwaySeconds * TicksPerSecond);
        shownUntilTick = Now + (int)(Props.showSeconds * TicksPerSecond);
        shownRange = range;
        Props.pingSound?.PlayOneShot(new TargetInfo(Vehicle.Position, Vehicle.Map));
        GiveAway();
        Search(range, out float nearest, out IntVec3 nearestCell, out int boats);
        SonarPings.Heard(Vehicle, SonarRule.HeardWithin(range, Props.heardFactor));
        if (Vehicle.Faction != Faction.OfPlayer) return;
        string text = found.Count == 0
            ? "PhobosSonar_FoundNothing".Translate(Vehicle.LabelShort.Named("VEHICLE"), range.ToString("0").Named("RANGE")).Resolve()
            : "PhobosSonar_Found".Translate(Vehicle.LabelShort.Named("VEHICLE"), found.Count.Named("COUNT"), nearest.ToString("0").Named("DISTANCE"), CompassText.From(Vehicle.Position, nearestCell).Named("BEARING")).Resolve();
        if (boats > 0) text += " " + "PhobosSonar_FoundBoats".Translate(boats.Named("COUNT")).Resolve();
        Messages.Message(text, Vehicle, found.Count == 0 ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.ThreatSmall, historical: false);
    }

    /// <summary>Everything hostile the ping finds, spotting the boats among them.</summary>
    void Search(float range, out float nearest, out IntVec3 nearestCell, out int boats)
    {
        found.Clear();
        nearest = -1f;
        nearestCell = Vehicle.Position;
        boats = 0;
        Map map = Vehicle.Map;
        IntVec3 at = Vehicle.Position;
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn == Vehicle || pawn.Dead || !pawn.HostileTo(Vehicle)) continue;
            float distance = pawn.Position.DistanceTo(at);
            if (distance >= range) continue;
            CompPhobosSubmersible? boat = pawn is VehiclePawn vehicle && Submersibles.IsHidden(vehicle) ? Submersibles.CompOf(vehicle) : null;
            bool inWater = pawn.Position.GetTerrain(map)?.IsWater ?? false;
            ContactKind kind = boat != null ? ContactKind.HiddenBoat : inWater ? ContactKind.InWater : ContactKind.OnLand;
            if (!SonarRule.Finds(kind, distance, range, boat?.Settled ?? false, Props.settledShare)) continue;
            found.Add(pawn.Position);
            if (boat != null)
            {
                boats++;
                boat.Spot();
            }
            if (nearest < 0f || distance < nearest)
            {
                nearest = distance;
                nearestCell = pawn.Position;
            }
        }
    }

    /// <summary>A boat that is under is followed while its ping gives it away. On the surface it is in plain sight anyway.</summary>
    void GiveAway()
    {
        if (Now < givenAwayUntilTick) Submersibles.CompOf(Vehicle)?.Spot();
    }

    /// <summary>A crew other than the player's pings now and then while something hostile is within reach and the boat is under.</summary>
    void CrewChoice()
    {
        if (Vehicle.Faction == null || Vehicle.Faction == Faction.OfPlayer || !Fitted || !InWater) return;
        bool under = Submersibles.CompOf(Vehicle)?.Hidden ?? false;
        if (!under || !Ready) return;
        float range = Range;
        if (range <= 0f) return;
        bool hostileWithin = Vehicle.Map.attackTargetsCache.GetPotentialTargetsFor(Vehicle).Any(t => t.Thing.Position.DistanceTo(Vehicle.Position) < range);
        if (SonarRule.CrewPingsNow(under, Ready, hostileWithin, Rand.Value, Props.crewPingChance)) Ping();
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!Vehicle.Spawned) return;
        GiveAway();
        CrewChoice();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (!Fitted || !Vehicle.Spawned || Vehicle.Faction != Faction.OfPlayer) yield break;
        var command = new Command_Action
        {
            defaultLabel = "PhobosSonar_GizmoPing".Translate().Resolve(),
            defaultDesc = "PhobosSonar_GizmoPingDesc".Translate(FittedRange.ToString("0").Named("RANGE"), SonarRule.HeardWithin(FittedRange, Props.heardFactor).ToString("0").Named("HEARD"),
                Props.givenAwaySeconds.ToString("0").Named("SECONDS"), Props.cooldownSeconds.ToString("0").Named("COOLDOWN")).Resolve(),
            icon = SystemIcons.For(Props.pingIcon),
            action = Ping,
        };
        string? refused = PingRefusal();
        if (refused != null) command.Disable(refused);
        yield return command;
    }

    /// <summary>While selected and for a few seconds after a ping, the ring it reached and a line to everything it found. A line into the fog gives a bearing and lifts no fog.</summary>
    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (!Vehicle.Spawned || Vehicle.Faction != Faction.OfPlayer || Now >= shownUntilTick || shownRange <= 0f) return;
        GenDraw.DrawRadiusRing(Vehicle.Position, Mathf.Min(shownRange, GenRadial.MaxRadialPatternRadius));
        // Through Thing: Vehicle Framework's 1.6 build declares its own DrawPos against a Unity module the 1.7 build does not reference.
        Vector3 from = ((Thing)Vehicle).DrawPos;
        foreach (IntVec3 cell in found) GenDraw.DrawLineBetween(from, cell.ToVector3Shifted(), SimpleColor.Yellow);
    }

    public override string? CompInspectStringExtra()
    {
        if (!Fitted || !Vehicle.Spawned) return null;
        return Ready
            ? "PhobosSonar_InspectReady".Translate(FittedRange.ToString("0").Named("RANGE")).Resolve()
            : "PhobosSonar_InspectCooling".Translate(SonarRule.SecondsUntilReady(Now, lastPingTick, CooldownTicks, TicksPerSecond).Named("SECONDS")).Resolve();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        found.Clear();
        shownUntilTick = 0;
        givenAwayUntilTick = 0;
        base.PostDeSpawn(map, mode);
    }
}

/// <summary>Who hears a ping. Anyone in the water with ears hears one, with or without a set of their own; the player is told the bearing it came from, never more.</summary>
public static class SonarPings
{
    public static void Heard(VehiclePawn pinger, float heardWithin)
    {
        if (!pinger.Spawned || heardWithin <= 0f || pinger.Faction == Faction.OfPlayer) return;
        Map map = pinger.Map;
        VehiclePawn? nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (pawn is not VehiclePawn listener || !CanHear(listener, map)) continue;
            float distance = listener.Position.DistanceTo(pinger.Position);
            if (distance >= heardWithin || distance >= nearestDistance) continue;
            nearest = listener;
            nearestDistance = distance;
        }
        if (nearest == null) return;
        string text = "PhobosSonar_PingHeard".Translate(nearest.LabelShort.Named("VEHICLE"), CompassText.From(nearest.Position, pinger.Position).Named("BEARING")).Resolve();
        Messages.Message(text, nearest, MessageTypeDefOf.ThreatSmall, historical: false);
    }

    /// <summary>A vehicle in the water with someone aboard awake who can hear: a hull carries the sound to everyone inside it.</summary>
    static bool CanHear(VehiclePawn listener, Map map) =>
        (listener.Position.GetTerrain(map)?.IsWater ?? false)
        && listener.AllPawnsAboard.Any(p => !p.Dead && !p.Downed && p.Awake() && p.health?.capacities != null && p.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) > 0f);
}
