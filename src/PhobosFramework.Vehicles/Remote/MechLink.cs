using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The mechanitor level as a source for the remote control comp (Framework 0.282.0; docs/development/autonomous-vehicles.md, step 7b; the
/// owner's choices of 18 and 25 Nov 2026). A vehicle with the mech control link fitting, its power on, linked to a mechanitor, is driven
/// while that mechanitor is up, on its map, within the game's own command range (Pawn_MechanitorTracker.CanCommandTo, so other mods' range
/// changes carry over) and the mechanitor's bandwidth still covers it after their mechanoids. The vehicle never becomes a mechanoid and
/// never goes feral. Biotech only.
/// </summary>
public sealed class MechControl : IRemoteControlSource
{
    public const string SourceKey = "mechanitor";
    /// <summary>What the mech control link fitting grants (MechLink.xml); its fitting's feed says what powers it.</summary>
    public const string KitGrant = "MechControl";
    /// <summary>The load's name, kept so other authors' C# still compiles (the owner's card of 28 Nov 2026, "Unread feed"): the fitting's feed now says what powers it.</summary>
    [System.Obsolete("Use Feeds.Works(vehicle, \"MechControl\")")]
    public const string KitLoad = "mechLink";

    /// <summary>The setting "Mechanitors can drive vehicles", on by default, and Biotech active. Live.</summary>
    public static bool On => VehicleSystemsMod.Settings.mechControl && ModsConfig.BiotechActive;

    public string Key => SourceKey;
    public bool StationToggle => false;
    public bool Enabled => On;
    public string TextPrefix => "PhobosMechLink_";
    public Texture2D? Icon => null;

    public bool Fitted(VehiclePawn vehicle) => KitWorks(vehicle);

    public static bool KitWorks(VehiclePawn vehicle) => Feeds.Works(vehicle, KitGrant);

    /// <summary>The link is on the vehicle, working or not (Framework 0.284.0): a failed link is a lost link, told, not a let-go.</summary>
    public bool Carried(VehiclePawn vehicle) =>
        vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.Fittings.Carries(KitGrant);

    public RemoteLink LinkOf(VehiclePawn vehicle)
    {
        if (vehicle.GetComp<CompPhobosMechLink>() is not { } link) return default;
        // Its own reason, said in the lost-link letter (Framework 0.314.0).
        if (link.Why() is { } why) return new RemoteLink(false, IntVec3.Invalid) { Why = why };
        return new RemoteLink(true, link.Mechanitor!.Position);
    }

    public void WantChanged(VehiclePawn vehicle)
    {
    }
}

/// <summary>
/// The links of this game, so a mechanitor's bandwidth can count their vehicles at once (Framework 0.282.0). Derived from the vehicles' own
/// saved links at load; never saved itself.
/// </summary>
public sealed class GameComponent_PhobosMechLinks : GameComponent
{
    /// <summary>How often links whose vehicle was left behind are let go: as a vehicle's rare tick.</summary>
    const int LeftBehindTicks = 250;

    readonly List<CompPhobosMechLink> links = new();

    public GameComponent_PhobosMechLinks(Game game)
    {
    }

    /// <summary>
    /// Lets go of a link whose vehicle has been left behind (Framework 0.284.0): its map closed with it on it, so it is held by nothing and
    /// no button reaches it, yet it took its mechanitor's bandwidth. A vehicle in a caravan, a hold or the air is held by those, and keeps
    /// its link, as an overseen mechanoid does. A few field reads every 250 ticks while any vehicle is linked.
    /// </summary>
    public override void GameComponentTick()
    {
        if (links.Count == 0 || Find.TickManager.TicksGame % LeftBehindTicks != 0) return;
        for (var i = links.Count - 1; i >= 0 && i < links.Count; i--)
        {
            var link = links[i];
            if (!LeftBehind(link.Vehicle)) continue;
            link.Unlink(quietly: true);
            links.Remove(link);
        }
    }

    static bool LeftBehind(VehiclePawn? vehicle) =>
        vehicle == null || vehicle.Destroyed || vehicle.Discarded || !vehicle.Spawned && vehicle.ParentHolder == null && vehicle.IsWorldPawn();

    public static GameComponent_PhobosMechLinks? Current => Verse.Current.Game?.GetComponent<GameComponent_PhobosMechLinks>();

    public void Add(CompPhobosMechLink link)
    {
        if (!links.Contains(link)) links.Add(link);
    }

    public void Remove(CompPhobosMechLink link) => links.Remove(link);

    /// <summary>A mechanitor's linked vehicles, oldest link first.</summary>
    public List<CompPhobosMechLink> Of(Pawn mechanitor)
    {
        links.RemoveAll(l => l.Vehicle == null || l.Vehicle.Destroyed || l.Mechanitor == null);
        return links.Where(l => l.Mechanitor == mechanitor).OrderBy(l => l.LinkedTick).ToList();
    }

    /// <summary>What a mechanitor's linked vehicles take from their bandwidth, all of them, in control or not, as overseen mechanoids do.</summary>
    public int CostOf(Pawn mechanitor)
    {
        var sum = 0;
        foreach (var link in links) if (link.Mechanitor == mechanitor && link.Vehicle is { Destroyed: false }) sum += link.Cost;
        return sum;
    }
}

/// <summary>
/// Lets a mechanitor drive a vehicle through the mech control link fitting. It takes no figures (they are in PhobosRemoteControlRulesDef);
/// D'thorga Framework gives it to every design with the control radio bay, so XML seldom names it.
/// </summary>
public class CompProperties_PhobosMechLink : CompProperties
{
    public CompProperties_PhobosMechLink() => compClass = typeof(CompPhobosMechLink);
}

/// <summary>
/// A vehicle's link to a mechanitor (Framework 0.282.0; docs/development/autonomous-vehicles.md, step 7b). The player links it to one of the
/// colony's mechanitors with a button; it then asks the remote control comp to drive it through the mechanitor source, so the lost-link
/// choices, delay and letters are step 3's. It counts against that mechanitor's bandwidth by its size (MechLinkRule), all of it, as an
/// overseen mechanoid does; when the bandwidth runs short the newest vehicles lose control first. A mechanitor's death, or the loss of
/// their mechlink, frees the vehicle, which stops. It looks on the vehicle's rare tick; nothing per tick.
/// </summary>
public sealed class CompPhobosMechLink : VehicleComp
{
    Pawn? mechanitor;
    int linkedTick = -1;
    int cost = -1;

    public override bool TickByRequest => true;

    public Pawn? Mechanitor => mechanitor;
    public int LinkedTick => linkedTick;

    static PhobosRemoteControlRulesDef Rules => PhobosRemoteControlRulesDef.Current;

    /// <summary>Its bandwidth, by its size, worked out once.</summary>
    public int Cost
    {
        get
        {
            if (cost >= 0) return cost;
            var systems = Vehicle.GetComp<CompPhobosVehicleSystems>();
            var kind = systems?.Props.loadout?.kind ?? "";
            var heavy = kind.Contains("Heavy") || kind.Contains("Tracked");
            var kerb = systems?.Powertrain?.KerbKg ?? double.NaN;
            return cost = MechLinkRule.Cost(heavy, kerb, Rules.mechLightMaxKg);
        }
    }

    CompPhobosRemoteControl? Remote => Vehicle.GetComp<CompPhobosRemoteControl>();

    /// <summary>Why the mechanitor cannot drive it now, or null when it can.</summary>
    public string? Why()
    {
        if (mechanitor == null) return "PhobosMechLink_NotLinked".Translate();
        if (!MechControl.On || !MechControl.KitWorks(Vehicle)) return "PhobosMechLink_NoKit".Translate(Vehicle.LabelShort.Named("VEHICLE"));
        if (mechanitor.Downed) return "PhobosMechLink_Downed".Translate(mechanitor.LabelShort.Named("MECHANITOR"));
        if (!Vehicle.Spawned || !mechanitor.Spawned || mechanitor.Map != Vehicle.Map || mechanitor.mechanitor == null
            || !mechanitor.mechanitor.CanCommandTo(Vehicle))
        {
            return "PhobosMechLink_OutOfReach".Translate(mechanitor.LabelShort.Named("MECHANITOR"));
        }
        if (!Kept()) return "PhobosMechLink_NoBandwidth".Translate(mechanitor.LabelShort.Named("MECHANITOR"));
        return null;
    }

    /// <summary>Whether the mechanitor's bandwidth, after their mechanoids, still covers this vehicle among their older links.</summary>
    bool Kept()
    {
        var tracker = mechanitor?.mechanitor;
        if (tracker == null || GameComponent_PhobosMechLinks.Current is not { } all) return false;
        var mine = all.Of(mechanitor!);
        var kept = MechLinkRule.Kept(tracker.TotalBandwidth, tracker.UsedBandwidthFromSubjects + tracker.UsedBandwidthFromGestation,
            mine.Select(l => l.Cost).ToList());
        var at = mine.IndexOf(this);
        var keep = at >= 0 && at < kept;
        // Seen in play (Framework 0.301.0, row 500): bandwidth short, the newest linked vehicle loses control first.
        if (!keep && at >= 0 && Seen.Wants("vehicles.mech-link-short", Vehicle))
        {
            Seen.Note("vehicles.mech-link-short", Vehicle, mechanitor!.LabelShort, null, new[] {
                new KeyValuePair<string, double>("place", at), new KeyValuePair<string, double>("kept", kept),
                new KeyValuePair<string, double>("links", mine.Count) });
        }
        return keep;
    }

    /// <summary>Links the vehicle to a mechanitor of the colony. Null when done, or why not.</summary>
    public string? LinkTo(Pawn pawn)
    {
        if (!MechControl.On || !MechControl.KitWorks(Vehicle)) return "PhobosMechLink_NoKit".Translate(Vehicle.LabelShort.Named("VEHICLE"));
        if (pawn.mechanitor == null || pawn.Faction != Faction.OfPlayer) return "PhobosMechLink_NotMechanitor".Translate(pawn.LabelShort.Named("PAWN"));
        var tracker = pawn.mechanitor;
        // The game's used figure counts no vehicle outside its bandwidth meter (Framework 0.284.0), so the links are added here.
        var free = tracker.TotalBandwidth - tracker.UsedBandwidth - (GameComponent_PhobosMechLinks.Current?.CostOf(pawn) ?? 0);
        if (mechanitor != pawn && Cost > free)
        {
            return "PhobosMechLink_TooLittle".Translate(pawn.LabelShort.Named("MECHANITOR"), Cost.Named("COST"), Mathf.Max(0, free).Named("FREE"));
        }
        Unlink(quietly: true);
        mechanitor = pawn;
        linkedTick = Find.TickManager.TicksGame;
        GameComponent_PhobosMechLinks.Current?.Add(this);
        Remote?.SetWanted(true, MechControl.SourceKey);
        if (Seen.Wants("vehicles.mech-link", Vehicle)) Seen.Note("vehicles.mech-link", Vehicle, "to " + pawn.LabelShort, null);
        return null;
    }

    /// <summary>Lets the mechanitor go; the vehicle stops where it is.</summary>
    public void Unlink(bool quietly = false, string? why = null)
    {
        if (mechanitor == null) return;
        var was = mechanitor;
        GameComponent_PhobosMechLinks.Current?.Remove(this);
        mechanitor = null;
        linkedTick = -1;
        if (Remote is { } remote && remote.WantedBy(MechControl.SourceKey)) remote.SetWanted(false, null);
        if (!quietly && Vehicle.Spawned)
        {
            Messages.Message(why ?? "PhobosMechLink_Unlinked".Translate(Vehicle.LabelShort.Named("VEHICLE"), was.LabelShort.Named("MECHANITOR")),
                Vehicle, MessageTypeDefOf.NeutralEvent, false);
        }
    }

    /// <summary>A mechanitor who has died, or lost their mechlink, frees the vehicle; a downed one keeps it and commands nothing.</summary>
    public override void CompTickRare()
    {
        if (mechanitor == null) return;
        if (mechanitor.Dead || mechanitor.Destroyed || mechanitor.mechanitor == null)
        {
            Unlink(why: "PhobosMechLink_Freed".Translate(Vehicle.LabelShort.Named("VEHICLE"), mechanitor.LabelShort.Named("MECHANITOR")));
            return;
        }
        // Another source took it (the station's button or a route): it lets go of the mechanitor.
        if (Remote is { } remote && !remote.WantedBy(MechControl.SourceKey)) Unlink(quietly: true);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        var vehicle = Vehicle;
        if (vehicle.Faction != Faction.OfPlayer || !vehicle.Spawned || !MechControl.On) yield break;
        if (mechanitor != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "PhobosMechLink_GizmoUnlink".Translate(mechanitor.LabelShort.Named("MECHANITOR")),
                defaultDesc = "PhobosMechLink_GizmoUnlinkDesc".Translate(),
                icon = MechLinkIcons.Unlink,
                action = () => Unlink(),
            };
            yield break;
        }
        if (!MechControl.KitWorks(vehicle)) yield break;
        yield return new Command_Action
        {
            defaultLabel = "PhobosMechLink_GizmoLink".Translate(),
            defaultDesc = "PhobosMechLink_GizmoLinkDesc".Translate(Cost.Named("COST")),
            icon = MechLinkIcons.Link,
            action = () =>
            {
                var parameters = new TargetingParameters
                {
                    canTargetPawns = true,
                    canTargetBuildings = false,
                    validator = t => t.Thing is Pawn p && p.mechanitor != null && p.Faction == Faction.OfPlayer && p.Map == vehicle.Map,
                };
                Find.Targeter.BeginTargeting(parameters, target =>
                {
                    if (target.Thing is Pawn chosen && LinkTo(chosen) is { } why) Messages.Message(why, vehicle, MessageTypeDefOf.RejectInput, false);
                });
            },
        };
    }

    public override string CompInspectStringExtra()
    {
        if (mechanitor == null) return string.Empty;
        var state = Remote?.State ?? RemoteState.Off;
        var lost = state switch
        {
            RemoteState.Waiting => "PhobosMechLink_InspectWaiting",
            RemoteState.Holding => "PhobosMechLink_InspectHolding",
            RemoteState.ComingBack => "PhobosMechLink_InspectComingBack",
            RemoteState.CarryingOn => "PhobosMechLink_InspectCarryingOn",
            _ => null,
        };
        var head = "PhobosMechLink_InspectLinked".Translate(mechanitor.LabelShort.Named("MECHANITOR"), Cost.Named("COST")).Resolve();
        if (lost != null) return head + "\n" + lost.Translate();
        return Why() is { } why && mechanitor != null ? head + "\n" + why : head;
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        // Destroyed: the link goes with it. Off the map it keeps its link, takes no orders, and still counts, as an overseen mechanoid does.
        if (mode != DestroyMode.Vanish) Unlink(quietly: true);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref mechanitor, "PhobosMechLink_Mechanitor");
        Scribe_Values.Look(ref linkedTick, "PhobosMechLink_LinkedTick", -1);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && mechanitor != null) GameComponent_PhobosMechLinks.Current?.Add(this);
    }
}

/// <summary>The mech link's button pictures, ours and bought (Framework 0.340.4 and 0.340.5); each falls back to a visible icon of ours.</summary>
[StaticConstructorOnStartup]
public static class MechLinkIcons
{
    // Their own pictures, bought, the owner's picks of 27 Nov 2026: Link (Framework 0.340.4) and Unlink drawn to match it (0.340.5).
    public static readonly Texture2D Link = SystemIcons.For("Phobos/VehicleSystems/Remote/MechLink");
    public static readonly Texture2D Unlink = SystemIcons.For("Phobos/VehicleSystems/Remote/MechUnlink");
}

/// <summary>Registers the mechanitor level as a source of the remote control comp at start-up (Framework 0.282.0).</summary>
[StaticConstructorOnStartup]
public static class MechLinkStartup
{
    static MechLinkStartup() => RemoteControl.Register(new MechControl());
}

/// <summary>
/// A mechanitor's used bandwidth, as their bandwidth meter shows it, counts their linked vehicles too (Framework 0.282.0; the owner's choice
/// of 18 Nov 2026, "add to the used figure"). Since 0.284.0 only while the meter draws (<see cref="MechLinkBandwidthGizmoPatch"/>): the
/// same getter decides whether a mechanoid may be controlled, resurrected or kept (the game's CanControlMech, HasBandwidthToResurrect,
/// the band node's warning, other mods' mech capsules), and a vehicle counted there would push mechanoids out, against the owner's
/// "mechanoids first". Postfix: it only adds; the sum is a short list read. No other mod patches this getter (query-patches.py, 25 Nov 2026).
/// </summary>
[HarmonyPatch(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.UsedBandwidth), MethodType.Getter)]
public static class MechLinkBandwidthPatch
{
    public static void Postfix(Pawn ___pawn, ref int __result)
    {
        if (!MechLinkBandwidthGizmoPatch.Drawing || ___pawn == null || GameComponent_PhobosMechLinks.Current is not { } links) return;
        __result += links.CostOf(___pawn);
    }
}

/// <summary>
/// Marks the game's bandwidth meter while it draws (Framework 0.284.0), so its used figure counts the linked vehicles and nothing else's
/// does. The meter builds its tooltip in the same call. Main thread only; the finalizer clears the mark even if the meter throws. No other
/// mod patches the meter (query-patches.py, 25 Nov 2026).
/// </summary>
[HarmonyPatch(typeof(MechanitorBandwidthGizmo), nameof(MechanitorBandwidthGizmo.GizmoOnGUI))]
public static class MechLinkBandwidthGizmoPatch
{
    public static bool Drawing { get; private set; }

    public static void Prefix() => Drawing = true;

    public static void Finalizer() => Drawing = false;
}
