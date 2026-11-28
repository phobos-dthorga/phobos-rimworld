using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework.Incidents;
using Phobos.Radio.UI;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// Early warning (Radio 0.74.0; docs/development/radar-and-air-defence.md, row RD7; the owner's cards of 28 Nov 2026). A raid does not exist
/// in RimWorld until it lands, so a radar's warning holds the storyteller's raid back and announces it, as the radio's allied warning does
/// (<see cref="RadioNews"/>), for drop and air raids only (card 2), once (card 3, <see cref="RaidHolds"/>).
/// <para>How long: how far the colony's radars see the raid along its way in (the sum, the horizon, the high ground; RD1 to RD3) ÷ the
/// rules' approach speed at play speed (the owner's card, "by the sum, at play speed"), between the rules' least and most.</para>
/// <para>To know drop from walk-in, the raid's faction, strategy and arrival mode are chosen now, by the game's own methods, into its
/// parms; the game keeps values already set (<c>TryGenerateRaidInfo</c>, read 28 Nov 2026), so the raid that lands is the one announced. It
/// comes from the bearing of its faction's nearest settlement [A]: the game does not say where raiders come from.</para>
/// <para>Cost: one look each time the storyteller fires a raid on a colony with a radar; then the held raids (a few) once a rare tick.</para>
/// </summary>
public static class RadarWarning
{
    private static readonly MethodInfo? resolveFaction = AccessTools.Method(typeof(IncidentWorker_Raid), "TryResolveRaidFaction");
    private static readonly List<PlanetTile> scratch = new();

    /// <summary>Holds a fresh storyteller drop or air raid a radar sees coming, and warns of it. True when it was held (the caller skips it).</summary>
    public static bool TryHold(FiringIncident fi)
    {
        if (!RadioMod.Settings.radarHoldsRaids || RaidHolds.Releasing || fi?.def == null || fi.parms == null) return false;
        if (fi.def.Worker is not IncidentWorker_RaidEnemy worker || fi.parms.forced || fi.parms.quest != null) return false;
        if (fi.parms.target is not Map map || !map.IsPlayerHome) return false;
        var picture = MapComponent_PhobosRadar.For(map);
        if (picture.Watching().Count == 0) return false;
        if (!worker.CanFireNow(fi.parms) || !Resolve(worker, fi.parms)) return false;
        PawnsArrivalModeDef mode = fi.parms.raidArrivalMode;
        if (mode.walkIn || mode == PawnsArrivalModeDefOf.EmergeFromWater) return false;

        var rules = RadioRulesDef.Current;
        bool air = mode.defName == AirAssaultMode;
        int count = Mathf.Max(1, Mathf.RoundToInt(fi.parms.points / Mathf.Max(1f, rules.radarPointsPerRaider)));
        double squareMetres = count * rules.radarDropPodSquareMetres;
        double height = air ? rules.radarAirRaidMetres : rules.radarDropCarrierMetres;
        float bearing = BearingOf(map, fi.parms.faction);
        double metres = WarningMetres(picture, map, bearing, squareMetres, height, rules, out CompRadar? best);
        if (best == null) return false;
        float hours = Mathf.Min(rules.radarMaximumWarningHours, (float)(metres / 1000.0 / rules.radarApproachKmPerGameHour));
        if (hours < rules.radarMinimumWarningHours) return false;

        int now = Find.TickManager.TicksGame;
        int delay = Mathf.RoundToInt(hours * GenDate.TicksPerHour);
        int fireTick = now + delay;
        // Queued, it fires through the storyteller again, then as any raid does; RaidHolds lets it through the other holds.
        Find.Storyteller.incidentQueue.Add(fi.def, fireTick, fi.parms, GenDate.TicksPerDay);
        RaidHolds.MarkHeld(fi, fireTick);
        var raid = new PhobosInboundRaid
        {
            map = map,
            faction = fi.parms.faction,
            heldTick = now,
            fireTick = fireTick,
            bearing = bearing,
            metres = metres,
            countLow = Mathf.Max(1, Mathf.FloorToInt(count * (1f - rules.radarCountBlur))),
            countHigh = Mathf.CeilToInt(count * (1f + rules.radarCountBlur)),
            air = air,
            remote = best.parent.Map != map,
        };
        GameComponent_PhobosRadarWarnings.Current?.Add(raid);
        raid.marker = WorldObject_PhobosRadarContact.Make(raid);
        string text = "PhobosRadar_WarningText".Translate(map.Parent.LabelCap.Named("COLONY"), raid.CountText.Named("COUNT"),
            CompassOf(bearing).Named("BEARING"), delay.ToStringTicksToPeriod().Named("TIME"), best.parent.LabelShort.Named("RADAR"),
            (air ? "PhobosRadar_KindAir" : "PhobosRadar_KindDrop").Translate().Named("KIND")).Resolve();
        Find.LetterStack.ReceiveLetter("PhobosRadar_WarningLabel".Translate(), text, LetterDefOf.ThreatSmall,
            new LookTargets(raid.marker != null ? new GlobalTargetInfo(raid.marker) : new GlobalTargetInfo(map.Parent)));
        RadioLog.Add(RadioLogKind.Diplomacy, text);
        if (Seen.Wants("radio.radar-hold", best.parent))
            Seen.Note("radio.radar-hold", best.parent, raid.CountText + ", " + CompassOf(bearing) + ", " + delay.ToStringTicksToPeriod(),
                air ? "air" : "drop");
        return true;
    }

    /// <summary>The air assault's arrival mode (D'thorga Framework's vehicles): raiders' aircraft, flying in low.</summary>
    public const string AirAssaultMode = "PhobosAirAssault";

    /// <summary>The raid's faction, strategy and arrival mode, chosen by the game's own methods in its own order. False if any fails.</summary>
    private static bool Resolve(IncidentWorker_RaidEnemy worker, IncidentParms parms)
    {
        if (parms.faction == null)
        {
            if (resolveFaction == null || resolveFaction.Invoke(worker, new object[] { parms }) is not true || parms.faction == null) return false;
        }
        worker.ResolveRaidStrategy(parms, parms.pawnGroupKind ?? PawnGroupKindDefOf.Combat);
        if (parms.raidStrategy == null) return false;
        if (parms.raidArrivalMode == null && !worker.TryResolveRaidArriveMode(parms)) return false;
        return parms.raidArrivalMode != null;
    }

    /// <summary>The bearing the raid comes from: its faction's nearest settlement, else one of the compass's, the same for the same raid.</summary>
    public static float BearingOf(Map map, Faction? faction)
    {
        PlanetTile home = map.Tile;
        Settlement? nearest = null;
        float best = float.MaxValue;
        if (faction != null)
        {
            foreach (Settlement settlement in Find.WorldObjects.Settlements)
            {
                if (settlement.Faction != faction) continue;
                float d = Find.WorldGrid.ApproxDistanceInTiles(home, settlement.Tile);
                if (d < best)
                {
                    best = d;
                    nearest = settlement;
                }
            }
        }
        if (nearest != null && nearest.Tile != home) return Find.WorldGrid.GetHeadingFromTo(home, nearest.Tile);
        return Rand.RangeSeeded(0f, 360f, Gen.HashCombineInt(home.tileId, Find.TickManager.TicksGame / GenDate.TicksPerHour));
    }

    /// <summary>
    /// How far out the colony's radars see a raid coming in along a bearing, in metres; the radar that sees it farthest. A radar of the colony
    /// sees along the bearing as far as its sum and horizon allow, short of the first high ground standing over the line. A radar of another
    /// colony, by data link, sees the part of the way in that passes within its reach (flat ground taken, an approximation [A]).
    /// </summary>
    public static double WarningMetres(MapComponent_PhobosRadar picture, Map map, float bearing, double squareMetres, double heightMetres,
        RadioRulesDef rules, out CompRadar? best)
    {
        best = null;
        double farthest = 0.0;
        PlanetTile home = map.Tile;
        double tileMetres = rules.radarMetresPerWorldTile;
        foreach (CompRadar radar in picture.Watching())
        {
            double reach = radar.SeesMetres(squareMetres, heightMetres);
            double along;
            if (radar.parent.Map == map)
            {
                along = reach;
                int steps = Mathf.FloorToInt((float)(reach / tileMetres));
                PlanetTile at = home;
                for (int i = 1; i <= steps; i++)
                {
                    at = StepAlong(home, at, bearing);
                    if (!at.Valid) break;
                    if (MapComponent_PhobosRadar.Shadowed(home, at, radar.HeightMetres, heightMetres, i * tileMetres, rules))
                    {
                        along = Math.Min(along, (i - 0.5) * tileMetres);
                        break;
                    }
                }
            }
            else
            {
                PlanetTile from = radar.parent.Map.Tile;
                double apart = Find.WorldGrid.ApproxDistanceInTiles(home, from) * tileMetres;
                double towards = apart * Math.Cos((Find.WorldGrid.GetHeadingFromTo(home, from) - bearing) * Mathf.Deg2Rad);
                double spare = towards * towards - apart * apart + reach * reach;
                along = spare < 0 ? 0 : towards + Math.Sqrt(spare);
            }
            if (along > farthest)
            {
                farthest = along;
                best = radar;
            }
        }
        return farthest;
    }

    /// <summary>One tile further out from home along a bearing: the neighbour that keeps closest to it.</summary>
    public static PlanetTile StepAlong(PlanetTile home, PlanetTile here, float bearing)
    {
        WorldGrid grid = Find.WorldGrid;
        grid.GetTileNeighbors(here, scratch);
        float hereDistance = here == home ? 0f : grid.ApproxDistanceInTiles(home, here);
        PlanetTile pick = PlanetTile.Invalid;
        float bestOff = float.MaxValue;
        foreach (PlanetTile next in scratch)
        {
            if (grid.ApproxDistanceInTiles(home, next) <= hereDistance) continue;
            float off = Mathf.Abs(Mathf.DeltaAngle(grid.GetHeadingFromTo(home, next), bearing));
            if (off < bestOff)
            {
                bestOff = off;
                pick = next;
            }
        }
        return pick;
    }

    /// <summary>A bearing as a compass point: "north-east".</summary>
    public static string CompassOf(float bearing)
    {
        int point = Mathf.RoundToInt(((bearing % 360f) + 360f) % 360f / 45f) % 8;
        return ("PhobosRadar_Compass" + point).Translate();
    }
}

/// <summary>A raid a radar is holding back, until it lands (Radio 0.74.0). Saved.</summary>
public sealed class PhobosInboundRaid : IExposable
{
    public Map? map;
    public Faction? faction;
    public int heldTick;
    public int fireTick;
    public float bearing;
    public double metres;
    public int countLow;
    public int countHigh;
    public bool air;
    public bool remote;
    public bool identified;
    public WorldObject_PhobosRadarContact? marker;

    public string CountText => countLow == countHigh ? countLow.ToString() : "PhobosRadar_CountRange".Translate(countLow.Named("LOW"), countHigh.Named("HIGH")).Resolve();

    /// <summary>How far through its warning it is now, 0 to 1.</summary>
    public float Progress => fireTick <= heldTick ? 1f : Mathf.Clamp01((Find.TickManager.TicksGame - heldTick) / (float)(fireTick - heldTick));

    /// <summary>How far out it is now, in metres: closing at a steady rate from where it was first seen.</summary>
    public double MetresOut => metres * (1f - Progress);

    public RadarIdentity Identity => identified ? MapComponent_PhobosRadar.Standing(faction) : RadarIdentity.Unknown;

    public void ExposeData()
    {
        Scribe_References.Look(ref map, "map");
        Scribe_References.Look(ref faction, "faction");
        Scribe_Values.Look(ref heldTick, "heldTick");
        Scribe_Values.Look(ref fireTick, "fireTick");
        Scribe_Values.Look(ref bearing, "bearing");
        Scribe_Values.Look(ref metres, "metres");
        Scribe_Values.Look(ref countLow, "countLow");
        Scribe_Values.Look(ref countHigh, "countHigh");
        Scribe_Values.Look(ref air, "air");
        Scribe_Values.Look(ref remote, "remote");
        Scribe_Values.Look(ref identified, "identified");
        Scribe_References.Look(ref marker, "marker");
    }
}

/// <summary>
/// The raids radars are holding back (Radio 0.74.0, rows RD7 to RD9): identified partway through the warning, with a letter (the owner's
/// card of 28 Nov 2026, "unknown, then identified"), sooner with a colonist on watch or the plot extractor; gone once they land. Saved.
/// <para>Cost: one look at a few entries once every 250 ticks.</para>
/// </summary>
public class GameComponent_PhobosRadarWarnings : GameComponent
{
    private const int LookTicks = 250;
    /// <summary>How long a held raid stays after it was due, in case the game's queue tries it again (its retries last a day).</summary>
    private const int StayAfterTicks = GenDate.TicksPerHour;

    private List<PhobosInboundRaid> inbound = new();

    public GameComponent_PhobosRadarWarnings(Game game) { }

    public static GameComponent_PhobosRadarWarnings? Current => Verse.Current.Game?.GetComponent<GameComponent_PhobosRadarWarnings>();

    public IReadOnlyList<PhobosInboundRaid> Inbound => inbound;

    public void Add(PhobosInboundRaid raid) => inbound.Add(raid);

    public bool AnyUnknownFor(Map map) => inbound.Any(r => r.map == map && !r.identified && Find.TickManager.TicksGame < r.fireTick);

    /// <summary>The held raids on a colony's way, as contacts on its scope.</summary>
    public void AddContacts(Map map, List<RadarContact> into)
    {
        int now = Find.TickManager.TicksGame;
        foreach (PhobosInboundRaid raid in inbound)
        {
            if (raid.map != map || now >= raid.fireTick) continue;
            into.Add(new RadarContact
            {
                Key = raid,
                Kind = RadarContactKind.Raid,
                Identity = raid.Identity,
                Label = raid.identified && raid.faction != null ? raid.faction.Name : "PhobosRadar_UnknownContacts".Translate().Resolve(),
                Faction = raid.identified ? raid.faction : null,
                BearingDegrees = raid.bearing,
                RangeMetres = raid.MetresOut,
                HeightMetres = raid.air ? RadioRulesDef.Current.radarAirRaidMetres : RadioRulesDef.Current.radarDropCarrierMetres,
                Remote = raid.remote,
                Count = raid.CountText,
            });
        }
    }

    public override void GameComponentTick()
    {
        int now = Find.TickManager.TicksGame;
        if (now % LookTicks != 0) return;
        LookForInbound();
        if (inbound.Count == 0) return;
        for (int i = inbound.Count - 1; i >= 0; i--)
        {
            PhobosInboundRaid raid = inbound[i];
            if (raid.map == null || now >= raid.fireTick + StayAfterTicks)
            {
                if (raid.marker is { Destroyed: false }) raid.marker.Destroy();
                inbound.RemoveAt(i);
                continue;
            }
            if (raid.identified || now >= raid.fireTick) continue;
            int? skill = MapComponent_PhobosRadar.For(raid.map).WatchSkill();
            if (now < raid.heldTick + MapComponent_PhobosRadar.IdentifyShare(skill) * (raid.fireTick - raid.heldTick)) continue;
            raid.identified = true;
            Identified(raid, skill);
        }
    }

    /// <summary>
    /// A missile or shell on its way to a colony warns even with no scope open: while one is in flight towards the player, each colony with a
    /// radar watching looks, once every 250 ticks, and its first sight of it sends the letter. Nothing in flight, nothing asked.
    /// </summary>
    private static void LookForInbound()
    {
        bool asked = false;
        foreach (Map map in Find.Maps)
        {
            if (!map.IsPlayerHome || map.GetComponent<MapComponent_PhobosRadar>() is not { } picture || picture.Watching().Count == 0) continue;
            if (!asked)
            {
                if (!RadarTargets.AnyInbound()) return;
                asked = true;
            }
            picture.Contacts();
        }
    }

    private static void Identified(PhobosInboundRaid raid, int? skill)
    {
        string who = raid.faction?.Name ?? "PhobosRadar_UnknownFaction".Translate().Resolve();
        string time = (raid.fireTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();
        string text = "PhobosRadar_IdentifiedText".Translate(raid.map!.Parent.LabelCap.Named("COLONY"), who.Named("FACTION"), raid.CountText.Named("COUNT"),
            RadarWarning.CompassOf(raid.bearing).Named("BEARING"), time.Named("TIME")).Resolve();
        LetterDef letter = MapComponent_PhobosRadar.Standing(raid.faction) == RadarIdentity.Hostile ? LetterDefOf.ThreatBig : LetterDefOf.NeutralEvent;
        Find.LetterStack.ReceiveLetter("PhobosRadar_IdentifiedLabel".Translate(who.Named("FACTION")), text, letter,
            new LookTargets(raid.marker is { Destroyed: false } ? new GlobalTargetInfo(raid.marker) : new GlobalTargetInfo(raid.map.Parent)));
        RadioLog.Add(RadioLogKind.Diplomacy, text);
        if (Seen.Wants("radio.radar-identified", null, skill == null ? "unmanned" : "watched"))
            Seen.Note("radio.radar-identified", null, who + ", " + time + " out", skill == null ? "unmanned" : "watched");
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref inbound, "PhobosRadar_Inbound", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            inbound ??= new List<PhobosInboundRaid>();
            inbound.RemoveAll(r => r == null || r.map == null);
        }
    }
}

/// <summary>
/// A held raid on the world map (Radio 0.74.0, row RD8): a blip that closes on the colony along its bearing as the warning runs out, then
/// goes. Drawn as a diamond in its identity's colour; the expanded icon is the same. Saved.
/// </summary>
public class WorldObject_PhobosRadarContact : WorldObject
{
    private PhobosInboundRaid? held;
    private PlanetTile home = PlanetTile.Invalid;
    private PlanetTile start = PlanetTile.Invalid;

    public static WorldObject_PhobosRadarContact? Make(PhobosInboundRaid raid)
    {
        if (raid.map == null || RadarContactDefOf.PhobosRadio_RadarContact == null) return null;
        PlanetTile home = raid.map.Tile;
        PlanetTile start = home;
        int steps = Mathf.Max(1, Mathf.RoundToInt((float)(raid.metres / RadioRulesDef.Current.radarMetresPerWorldTile)));
        for (int i = 0; i < steps; i++)
        {
            PlanetTile next = RadarWarning.StepAlong(home, start, raid.bearing);
            if (!next.Valid) break;
            start = next;
        }
        var contact = (WorldObject_PhobosRadarContact)WorldObjectMaker.MakeWorldObject(RadarContactDefOf.PhobosRadio_RadarContact!);
        contact.held = raid;
        contact.home = home;
        contact.start = start;
        contact.Tile = start;
        Find.WorldObjects.Add(contact);
        return contact;
    }

    /// <summary>Its raid, kept by the warnings' component (which saves it); found again after a load.</summary>
    private PhobosInboundRaid? raid => held ??= GameComponent_PhobosRadarWarnings.Current?.Inbound.FirstOrDefault(r => r.marker == this);

    private RadarIdentity Identity => raid?.Identity ?? RadarIdentity.Unknown;

    public override Vector3 DrawPos
    {
        get
        {
            if (raid == null || !home.Valid || !start.Valid) return base.DrawPos;
            Vector3 from = Find.WorldGrid.GetTileCenter(start);
            Vector3 to = Find.WorldGrid.GetTileCenter(home);
            return Vector3.Slerp(from, to, raid.Progress);
        }
    }

    public override Material Material => RadarSymbols.BlipMaterial(Identity);

    public override Texture2D ExpandingIcon => RadarSymbols.Blip(Identity);

    public override Color ExpandingIconColor => Color.white;

    public override string Label => raid == null ? base.Label
        : raid.identified && raid.faction != null ? "PhobosRadar_ContactLabelKnown".Translate(raid.faction.Name.Named("FACTION")).Resolve()
        : "PhobosRadar_UnknownContacts".Translate().Resolve();

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (raid?.map == null) return text;
        string line = "PhobosRadar_ContactInspect".Translate(raid.CountText.Named("COUNT"), raid.map.Parent.LabelCap.Named("COLONY"),
            Mathf.Max(0, raid.fireTick - Find.TickManager.TicksGame).ToStringTicksToPeriod().Named("TIME")).Resolve();
        return text.NullOrEmpty() ? line : text + "\n" + line;
    }

    protected override void Tick()
    {
        base.Tick();
        if (raid == null || raid.map == null || Find.TickManager.TicksGame >= raid.fireTick) Destroy();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref home, "PhobosRadar_Home");
        Scribe_Values.Look(ref start, "PhobosRadar_Start");
    }
}

/// <summary>The world contact's Def, looked up when first wanted (the radar's Defs load from their own folder only once its pictures are in).</summary>
public static class RadarContactDefOf
{
    private static WorldObjectDef? contact;

    public static WorldObjectDef? PhobosRadio_RadarContact => contact ??= DefDatabase<WorldObjectDef>.GetNamedSilentFail("PhobosRadio_RadarContact");
}
