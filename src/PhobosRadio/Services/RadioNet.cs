using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Radio.Physics;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>One kind's share of a station's channels (Radio 0.8.0): its shape, the radios served, the channels they hold, where its
/// block starts, and how many more of it would fit.</summary>
public readonly record struct KindLedger(ChannelKindDef Kind, KindShape Shape, int Served, int Held, int Start, int Room);

/// <summary>A station's channels as the ledger last counted them (Radio 0.7.0; by kind since 0.8.0).</summary>
public sealed class StationChannels
{
    public static readonly StationChannels None = new(0, System.Array.Empty<KindLedger>());

    public StationChannels(int channels, IReadOnlyList<KindLedger> kinds)
    {
        Channels = channels;
        Kinds = kinds;
        foreach (var kind in kinds) InUse += kind.Held;
    }

    public int Channels { get; }
    /// <summary>Every kind, in the ledger's order (<see cref="ChannelKindDef.InOrder"/>), in use or not.</summary>
    public IReadOnlyList<KindLedger> Kinds { get; }
    public int InUse { get; }
    public int Free => System.Math.Max(0, Channels - InUse);

    /// <summary>One kind's share, or an empty one when the ledger has not counted it.</summary>
    public KindLedger Of(ChannelKindDef kind)
    {
        var at = kind.LedgerIndex;
        return at >= 0 && at < Kinds.Count ? Kinds[at] : new KindLedger(kind, default, 0, 0, 0, 0);
    }
}

/// <summary>
/// The map's radio picture: every worn hand-held, and every radio built into a vehicle of the colony (Radio 0.10.0,
/// <see cref="CarriedRadios"/>), evaluated against every base station, recomputed on a short interval and never saved. Gizmos, inspect strings and the thought read this cache; nothing
/// else computes a link. Since Radio 0.7.0 it also keeps the channel ledger: which station serves each radio in touch, from each
/// station's pool of channels (<see cref="Channels"/>), counted by kind since 0.8.0 (<see cref="ChannelKindDef"/>).
/// </summary>
public sealed class RadioNet : MapComponent
{
    /// <summary>The cycle in which radios sharing a channel take their turns, in ticks (ten seconds; authored).</summary>
    public const int KeyCycleTicks = 600;

    // Weak: loading a game never calls MapRemoved, and a plain reference would keep the last game's map.
    private static readonly ConditionalWeakTable<Map, RadioNet> byMap = new();

    private readonly NoiseService noise = new();
    private readonly List<LinkEvaluation> pairs = new();
    private readonly Dictionary<Pawn, LinkEvaluation> bestByPawn = new();
    private readonly Dictionary<Thing, int> inTouchByStation = new();
    private readonly Dictionary<Thing, string> antennaLabelByStation = new();
    private readonly List<CompRadio> stations = new();
    /// <summary>Every radio that goes with a pawn: worn hand-helds, and radios built into vehicles (Radio 0.10.0), with that pawn.</summary>
    private readonly List<(CompRadio radio, Pawn holder)> mobileRadios = new();
    /// <summary>The pawns a radio is built into that the net counts now (Radio 0.10.0).</summary>
    private readonly HashSet<Pawn> carriers = new();
    /// <summary>Built-in radios whose links are worked out but which ask no channel (<see cref="CarriedRadioUse.StandingBy"/>).</summary>
    private readonly HashSet<Thing> standingBy = new();
    /// <summary>Built-in radios that ask a picture too (<see cref="CarriedRadioUse.InUseWithPicture"/>; Radio 0.15.0).</summary>
    private readonly HashSet<Thing> wantsPicture = new();
    /// <summary>Those whose picture the station serving their orders also serves (Radio 0.15.0).</summary>
    private readonly HashSet<Thing> pictureServed = new();
    /// <summary>The picture kind's name: a vehicle's camera, two channels at once (Radio 0.9.0's ChannelKinds.xml).</summary>
    public const string PictureKindName = "PhobosRadio_KindVehiclePicture";
    /// <summary>The colonist working each station, at its console (Radio 0.12.0), counted at each pass.</summary>
    private readonly Dictionary<Thing, Pawn> operatorAt = new();
    /// <summary>How many vehicles each station serves on its channels (Radio 0.12.0).</summary>
    private readonly Dictionary<Thing, int> vehiclesOn = new();
    /// <summary>The stations that serve a vehicle and have nobody working them (Radio 0.12.0): the operator's work.</summary>
    private readonly List<Thing> stationsWantingOperator = new();
    /// <summary>The base turrets on this map (Radio 0.18.0), registered as they spawn: the net never scans the buildings for them.</summary>
    private readonly List<CompRemoteTurret> turrets = new();
    /// <summary>Each turret's station on its feedline, those of them its operator watches, and each station's turrets, at each pass.</summary>
    private readonly Dictionary<Thing, Thing> turretStation = new();
    private readonly HashSet<Thing> turretWatched = new();
    private readonly Dictionary<Thing, int> turretsOn = new();
    /// <summary>Whether enemies were about at the last pass, when a station calls an operator for its turrets [A]. Only read with turrets.</summary>
    private bool threat;
    /// <summary>
    /// The radios of base turrets no coax reaches (Radio 0.20.0): fixed radios, worked out with the hand-helds and vehicles at each pass.
    /// </summary>
    private readonly List<CompRadio> fixedRadios = new();
    /// <summary>The control kind's name: a vehicle's orders, one channel for two radios (Radio 0.9.0's ChannelKinds.xml).</summary>
    public const string ControlKindName = "PhobosRadio_KindVehicleControl";
    /// <summary>The station each radio in touch is served by, kept between recomputes so a holder keeps its channel. Never saved.</summary>
    private Dictionary<int, int> held = new();
    private readonly Dictionary<Pawn, Thing> servedBy = new();
    private readonly HashSet<Pawn> busy = new();
    /// <summary>Calls made at a desk this pass (Radio 0.51.0), and those the desk's station had no channel for.</summary>
    private readonly List<(Pawn Caller, Thing Desk)> deskCalls = new();
    private readonly HashSet<Pawn> deskBusy = new();
    private readonly Dictionary<Thing, List<Thing>> servedOn = new();
    /// <summary>The net's change sounds (Radio 0.30.0): nothing saved, worked out after each recompute.</summary>
    private readonly RadioNetSounds sounds = new();
    /// <summary>Each served radio's station, its kind (its index in the ledger) and its place among that station's radios of the kind.</summary>
    private readonly Dictionary<Thing, (Thing station, int kind, int place)> placeOf = new();
    private readonly Dictionary<Thing, StationChannels> channelsOf = new();
    private int nextRecomputeTick = -1;
    private bool dirty = true;
    private bool unpoweredStation;

    public RadioNet(Map map) : base(map) { }

    /// <summary>The net for a map, kept so callers never scan the component list each time.</summary>
    public static RadioNet For(Map map)
    {
        if (byMap.TryGetValue(map, out var net)) return net;
        net = map.GetComponent<RadioNet>();
        if (net != null) byMap.AddOrUpdate(map, net);
        return net!;
    }

    /// <summary>Whether a base station stands on this map with no power, and so reaches nobody.</summary>
    public bool HasUnpoweredStation
    {
        get
        {
            EnsureFresh();
            return unpoweredStation;
        }
    }

    /// <summary>
    /// Each tick, after the net (Radio 0.14.0): a tie-in that works something out a little at a time on a map with a net (the cover map
    /// for vehicles driven from afar) hangs on here, so it needs no map component of its own in the save. Keep a handler cheap when it has
    /// nothing to do.
    /// </summary>
    public static event System.Action<RadioNet>? Ticked;

    public override void MapComponentTick()
    {
        if (dirty || Find.TickManager.TicksGame >= nextRecomputeTick) Recompute();
        Ticked?.Invoke(this);
    }

    public override void MapRemoved()
    {
        Clear();
        sounds.Reset();
        byMap.Remove(map);
    }

    /// <summary>Whether a pawn is spawned, free and of the colony: the only pawns whose radios count.</summary>
    public static bool Counts(Pawn pawn) => pawn.Spawned && pawn.IsFreeNonSlaveColonist;

    public bool TryGetBest(Pawn pawn, out LinkEvaluation? link)
    {
        EnsureFresh();
        return bestByPawn.TryGetValue(pawn, out link);
    }

    public IReadOnlyList<LinkEvaluation> PairsFor(Thing station)
    {
        EnsureFresh();
        var result = new List<LinkEvaluation>();
        foreach (var pair in pairs) if (pair.B.Thing == station) result.Add(pair);
        return result;
    }

    public int InTouchCount(Thing station)
    {
        EnsureFresh();
        return inTouchByStation.TryGetValue(station, out var count) ? count : 0;
    }

    /// <summary>The powered base stations the last pass counted (Radio 0.14.0, for the cover map). Read only; the net owns the list.</summary>
    public IReadOnlyList<CompRadio> Stations
    {
        get
        {
            EnsureFresh();
            return stations;
        }
    }

    /// <summary>The station serving a colonist's radio on one of its channels, or null.</summary>
    public Thing? ServedBy(Pawn pawn)
    {
        EnsureFresh();
        return servedBy.TryGetValue(pawn, out var station) ? station : null;
    }

    /// <summary>Whether the net counts a radio built into this pawn now (a vehicle with its radio fitted; Radio 0.10.0).</summary>
    public bool Carries(Pawn pawn)
    {
        EnsureFresh();
        return carriers.Contains(pawn);
    }

    /// <summary>A radio built into this pawn has its links worked out but asks no channel until it is in use (Radio 0.10.0).</summary>
    public bool IsStandingBy(Pawn pawn)
    {
        EnsureFresh();
        return standingBy.Contains(pawn);
    }

    /// <summary>
    /// Whether the station serving this vehicle's orders serves its picture too (Radio 0.15.0, step 6): asked for while it is driven drafted,
    /// and only where the picture's own margin holds, so the picture fails first as the vehicle goes further or behind cover.
    /// </summary>
    public bool HasPicture(Pawn vehicle)
    {
        EnsureFresh();
        return pictureServed.Contains(vehicle);
    }

    /// <summary>A vehicle picture's id in the ledger: the radio's own, negated, so it never meets a thing's.</summary>
    private static int PictureId(Thing radio) => -radio.thingIDNumber - 1;

    /// <summary>The colonist working this station now, or null (Radio 0.12.0).</summary>
    public Pawn? OperatorAt(Thing station)
    {
        EnsureFresh();
        return operatorAt.TryGetValue(station, out var pawn) ? pawn : null;
    }

    /// <summary>How many vehicles this station serves on its channels (Radio 0.12.0).</summary>
    public int VehiclesOn(Thing station)
    {
        EnsureFresh();
        return vehiclesOn.TryGetValue(station, out var count) ? count : 0;
    }

    /// <summary>Whether this station serves a vehicle and so has work for an operator (Radio 0.12.0).</summary>
    public bool HasVehiclesToDrive(Thing station) => VehiclesOn(station) > 0 || TurretsCalling(station);

    /// <summary>Whether this station has base turrets on its feedline while enemies are about (Radio 0.18.0): work for an operator too.</summary>
    public bool TurretsCalling(Thing station)
    {
        EnsureFresh();
        return threat && turretsOn.ContainsKey(station);
    }

    /// <summary>Whether this station serves a vehicle and nobody works it (Radio 0.12.0).</summary>
    public bool WantsOperator(Thing station) => HasVehiclesToDrive(station) && OperatorAt(station) == null;

    /// <summary>The stations that serve a vehicle with nobody working them (Radio 0.12.0).</summary>
    public IReadOnlyList<Thing> StationsWantingOperator
    {
        get
        {
            EnsureFresh();
            return stationsWantingOperator;
        }
    }

    /// <summary>
    /// Whether a station drives this vehicle's radio now (Radio 0.12.0; the autonomous vehicles record, step 5): it serves it on a channel,
    /// a colonist works the station, and the vehicle is among the first the operator can drive at once (the rules' vehicles per
    /// operator, the ledger's order deciding which).
    /// </summary>
    public bool Drives(Thing station, Thing radio)
    {
        var place = PlaceOf(station, radio);
        return place >= 0 && OperatorAt(station) != null && place < RadioRulesDef.Current.vehiclesPerOperator;
    }

    /// <summary>A vehicle served by a station that does not drive it: nobody works the station, or its operator has their hands full.</summary>
    public bool WaitsForOperator(Pawn vehicle) => ServedBy(vehicle) is { } station && !Drives(station, vehicle);

    /// <summary>In touch with a station, but every station that can hear them has its channels full: the net is busy for them.</summary>
    public bool IsBusy(Pawn pawn)
    {
        EnsureFresh();
        return busy.Contains(pawn);
    }

    /// <summary>A station's channels: how many, and each kind's share of them.</summary>
    /// <summary>Whether this colonist's call at a desk found no channel at the last pass (Radio 0.51.0).</summary>
    public bool DeskCallBusy(Pawn pawn) => deskBusy.Contains(pawn);

    /// <summary>
    /// Whether one more call at this desk would find a channel of the local voice kind, by the last pass's ledger (Radio 0.51.0); true before
    /// the first pass has counted the station.
    /// </summary>
    public bool DeskHasRoom(Thing desk) => !channelsOf.ContainsKey(desk) || ChannelsOf(desk).Of(ChannelKindDef.LocalVoice).Room > 0;

    public StationChannels ChannelsOf(Thing station)
    {
        EnsureFresh();
        return channelsOf.TryGetValue(station, out var channels) ? channels : StationChannels.None;
    }

    /// <summary>
    /// A radio's place among the radios of its kind a station serves, which sets its channel and its turn; -1 when it is not served
    /// there.
    /// </summary>
    public int PlaceOf(Thing station, Thing radio)
    {
        EnsureFresh();
        return placeOf.TryGetValue(radio, out var at) && at.station == station ? at.place : -1;
    }

    /// <summary>The kind's share a served radio counts in at a station, or null when it is not served there.</summary>
    public KindLedger? LedgerOf(Thing station, Thing radio)
    {
        EnsureFresh();
        if (!placeOf.TryGetValue(radio, out var at) || at.station != station) return null;
        var channels = ChannelsOf(station);
        return at.kind < channels.Kinds.Count ? channels.Kinds[at.kind] : null;
    }

    /// <summary>The channel a served radio's use starts on at a station, counting from 0, or -1.</summary>
    public int ChannelOf(Thing station, Thing radio)
    {
        var place = PlaceOf(station, radio);
        return place >= 0 && LedgerOf(station, radio) is { } ledger ? Channels.ChannelOf(place, ledger.Shape, ledger.Start) : -1;
    }

    /// <summary>The radios a station serves, kind by kind in the ledger's order, each kind in its places.</summary>
    public IReadOnlyList<Thing> ServedOn(Thing station)
    {
        EnsureFresh();
        return servedOn.TryGetValue(station, out var radios) ? radios : (IReadOnlyList<Thing>)System.Array.Empty<Thing>();
    }

    /// <summary>Whether a served radio is taking its turn on its channel now.</summary>
    public bool RadioKeyed(Thing station, Thing radio)
    {
        var place = PlaceOf(station, radio);
        if (place < 0 || LedgerOf(station, radio) is not { } ledger) return false;
        return Channels.KeyedNow(place, ledger.Shape.RadiosPerUse, Find.TickManager.TicksGame + Seed(station), KeyCycleTicks,
            ledger.Kind.Mode.dutyCycle);
    }

    /// <summary>Whether a station is answering anyone now: one of its served radios is taking its turn (the lamp, the needle).</summary>
    public bool StationKeyed(Thing station)
    {
        foreach (var radio in ServedOn(station)) if (RadioKeyed(station, radio)) return true;
        return false;
    }

    /// <summary>Each station's turns start at its own point in the cycle, so two stations are not keyed in step.</summary>
    private static int Seed(Thing station) => station.thingIDNumber * 37;

    public string AntennaLabelFor(Thing station)
    {
        EnsureFresh();
        return antennaLabelByStation.TryGetValue(station, out var label) ? label : "PhobosRadio_AntennaBuiltIn".Translate();
    }

    public void MarkDirty() => dirty = true;

    /// <summary>The noise snapshot every link on this map was worked out against; the coverage overlay reads the same one.</summary>
    public NoiseService Noise
    {
        get
        {
            EnsureFresh();
            return noise;
        }
    }

    /// <summary>Local noise at a cell from the last snapshot, in dBm, or −∞ when nothing reaches it.</summary>
    public double NoiseAt(IntVec3 cell)
    {
        EnsureFresh();
        return noise.LocalNoiseDbm(cell, RadioMod.Settings.metresPerCell);
    }

    private void EnsureFresh()
    {
        if (dirty) Recompute();
    }

    private void Clear()
    {
        pairs.Clear();
        bestByPawn.Clear();
        inTouchByStation.Clear();
        antennaLabelByStation.Clear();
        stations.Clear();
        mobileRadios.Clear();
        carriers.Clear();
        standingBy.Clear();
        wantsPicture.Clear();
        pictureServed.Clear();
        operatorAt.Clear();
        vehiclesOn.Clear();
        stationsWantingOperator.Clear();
        turretStation.Clear();
        turretWatched.Clear();
        turretsOn.Clear();
        threat = false;
        fixedRadios.Clear();
        servedBy.Clear();
        busy.Clear();
        deskCalls.Clear();
        deskBusy.Clear();
        servedOn.Clear();
        placeOf.Clear();
        channelsOf.Clear();
        unpoweredStation = false;
    }

    private void Recompute()
    {
        var rules = RadioRulesDef.Current;
        dirty = false;
        nextRecomputeTick = Find.TickManager.TicksGame + rules.recomputeIntervalTicks;
        Clear();
        noise.Snapshot(map, rules);
        CollectStations();
        // Before the check below, so a vehicle's radio still shows "no station" (Radio 0.10.0).
        CollectMobileRadios();
        CollectTurretRadios();
        if (stations.Count == 0)
        {
            sounds.Heard(this, map, servedOn);
            return;
        }

        var metresPerCell = RadioMod.Settings.metresPerCell;
        foreach (var station in stations)
        {
            var stationEnds = StationEndpoints(station, metresPerCell);
            LinkEvaluation? strongest = null;
            foreach (var (handheld, wearer) in mobileRadios)
            {
                var handheldEnd = RadioEndpoint.BuiltIn(handheld.parent, wearer.Position, handheld);
                LinkEvaluation? link = null;
                foreach (var stationEnd in stationEnds)
                {
                    var candidate = LinkBudgetService.Evaluate(map, handheldEnd, stationEnd, noise, rules, metresPerCell);
                    if (candidate != null && (link == null || candidate.MarginDb > link.MarginDb)) link = candidate;
                }
                if (link == null) continue;
                pairs.Add(link);
                if (strongest == null || link.MarginDb > strongest.MarginDb) strongest = link;
                if (!bestByPawn.TryGetValue(wearer, out var best) || link.MarginDb > best.MarginDb) bestByPawn[wearer] = link;
            }
            // Base turrets' own radios (Radio 0.20.0): fixed, so each is one more link per station, worked out as a hand-held's.
            foreach (var fixedRadio in fixedRadios)
            {
                var fixedEnd = RadioEndpoint.BuiltIn(fixedRadio.parent, fixedRadio.parent.Position, fixedRadio);
                LinkEvaluation? link = null;
                foreach (var stationEnd in stationEnds)
                {
                    var candidate = LinkBudgetService.Evaluate(map, fixedEnd, stationEnd, noise, rules, metresPerCell);
                    if (candidate != null && (link == null || candidate.MarginDb > link.MarginDb)) link = candidate;
                }
                if (link == null) continue;
                pairs.Add(link);
                if (strongest == null || link.MarginDb > strongest.MarginDb) strongest = link;
            }
            // With nobody to reach yet, a mast on the coax is what the station would use, before its built-in whip.
            antennaLabelByStation[station.parent] = strongest?.B.AntennaLabel ?? stationEnds[stationEnds.Count > 1 ? 1 : 0].AntennaLabel;
        }
        if (noise.JammerCount > 0) NoteJammedLinks();
        ServeFromChannels(rules);
        ServeTurrets(rules);
        // Radio 0.30.0: the squelch of each radio newly in touch, and a station's knob as it moves a radio, from this recompute's changes.
        sounds.Heard(this, map, servedOn);
    }

    /// <summary>
    /// Seen in play (Radio 0.45.0, row 707; EW1): a radio whose best link jamming holds back, still in touch (it burns through) or lost.
    /// Runs only while the map has a jammer, and reads each radio's best link once.
    /// </summary>
    private void NoteJammedLinks()
    {
        foreach (var (pawn, link) in bestByPawn)
        {
            if (double.IsNegativeInfinity(link.JammingAtLimitingReceiverDbm)) continue;
            var variant = link.InTouch ? "burns through" : "lost";
            if (!Seen.Wants("radio.jammed-link", pawn, variant) || !link.JammingLimits) continue;
            Seen.Note("radio.jammed-link", pawn, link.Mode.label + ", " + link.MarginDb.ToString("0.0") + " dB to spare", variant);
        }
    }

    /// <summary>A base turret spawned on this map (Radio 0.18.0): the net works it in at its next pass.</summary>
    public void AddTurret(CompRemoteTurret turret)
    {
        if (!turrets.Contains(turret)) turrets.Add(turret);
        dirty = true;
    }

    public void RemoveTurret(CompRemoteTurret turret)
    {
        turrets.Remove(turret);
        dirty = true;
    }

    /// <summary>Whether a station's operator watches this turret now (Radio 0.18.0).</summary>
    public bool TurretWatched(Thing turret)
    {
        EnsureFresh();
        return turretWatched.Contains(turret);
    }

    /// <summary>The powered station on this turret's feedline, or null (Radio 0.18.0).</summary>
    public Thing? TurretStation(Thing turret)
    {
        EnsureFresh();
        return turretStation.TryGetValue(turret, out var station) ? station : null;
    }

    /// <summary>
    /// The base turrets linked by coax (Radio 0.18.0; the owner's cards of 26 Nov 2026): each takes the powered station on its feedline, and
    /// one of that station's operator's hands after its vehicles, in their order. While enemies are about, a station with turrets and nobody
    /// at its console wants an operator [A], as one serving a vehicle does always.
    /// <para>Cost: nothing without turrets; with them, one walk of each turret's feedline connectors and the game's threat check, once a
    /// pass (about once a second), as the record states.</para>
    /// </summary>
    private void ServeTurrets(RadioRulesDef rules)
    {
        if (turrets.Count == 0) return;
        // The hands taken at each station by radios of the control kind (vehicles and turrets by radio, Radio 0.20.0), ahead of coax turrets.
        var controlKind = DefDatabase<ChannelKindDef>.GetNamedSilentFail(ControlKindName)?.LedgerIndex ?? -1;
        var controlOn = new Dictionary<Thing, int>();
        foreach (var entry in placeOf.Values)
        {
            if (entry.kind == controlKind) controlOn[entry.station] = (controlOn.TryGetValue(entry.station, out var c) ? c : 0) + 1;
        }
        var coaxOn = new Dictionary<Thing, int>();
        foreach (var turret in turrets)
        {
            if (!turret.parent.Spawned) continue;
            Thing at;
            bool hand;
            if (coaxStation.TryGetValue(turret.parent, out var station))
            {
                at = station;
                var place = (controlOn.TryGetValue(at, out var control) ? control : 0) + (coaxOn.TryGetValue(at, out var before) ? before : 0);
                coaxOn[at] = before + 1;
                hand = RemoteTurretRule.HasHand(operatorAt.ContainsKey(at), place, rules.vehiclesPerOperator);
            }
            else if (placeOf.TryGetValue(turret.parent, out var served))
            {
                // By its own radio (Radio 0.20.0): served on the control kind, its picture on the same station, and a hand in the ledger's order.
                at = served.station;
                hand = pictureServed.Contains(turret.parent) && RemoteTurretRule.HasHand(operatorAt.ContainsKey(at), served.place, rules.vehiclesPerOperator);
                if (hand && Seen.Wants("radio.turret-by-radio", turret.parent)) Seen.Note("radio.turret-by-radio", turret.parent, at.LabelShort);
            }
            else continue;
            turretStation[turret.parent] = at;
            turretsOn[at] = (turretsOn.TryGetValue(at, out var on) ? on : 0) + 1;
            if (hand) turretWatched.Add(turret.parent);
            if (threat && !operatorAt.ContainsKey(at) && !stationsWantingOperator.Contains(at)) stationsWantingOperator.Add(at);
        }
    }

    /// <summary>Each turret's powered station on its coax, found before the links (Radio 0.20.0): coax first, its own radio otherwise.</summary>
    private readonly Dictionary<Thing, Thing> coaxStation = new();

    /// <summary>
    /// The radios of base turrets no coax reaches (Radio 0.20.0): while enemies are about each asks a control channel and the picture, as a
    /// driven vehicle does [A]; otherwise it stands by, its link worked out and shown but no channel held.
    /// <para>Cost: nothing without turrets; with them, one feedline walk each and the game's threat check, once a pass.</para>
    /// </summary>
    private void CollectTurretRadios()
    {
        coaxStation.Clear();
        if (turrets.Count == 0) return;
        threat = GenHostility.AnyHostileActiveThreatToPlayer(map);
        turrets.Sort((a, b) => a.parent.thingIDNumber.CompareTo(b.parent.thingIDNumber));
        foreach (var turret in turrets)
        {
            if (!turret.parent.Spawned) continue;
            if (turret.Port is { } port && FeedlineService.StationOn(port) is { } station && stations.Contains(station))
            {
                coaxStation[turret.parent] = station.parent;
                continue;
            }
            if (turret.parent.TryGetComp<CompRadio>() is not { } radio || radio.Props.isStation) continue;
            fixedRadios.Add(radio);
            if (threat) wantsPicture.Add(turret.parent);
            else standingBy.Add(turret.parent);
        }
    }

    /// <summary>
    /// The channel ledger (Radio 0.7.0; by kind since 0.8.0): every radio in touch is served by the best station with room for one
    /// more of its kind; a radio already served keeps its station while it stays in touch with it. Kinds with a lower order are served
    /// first, then the strongest; a kind that pre-empts (0.9.0) takes channels from the kinds after it when it finds no room. A radio
    /// in touch that no station has room for is busy. Only served radios count as in touch with a station, for its draw, its lamp and
    /// the colonist's mood.
    /// <para>Cost: one pass over the links already worked out, a sort, and a few lookups per radio, about once a second; pre-emption
    /// adds one pass over a full station's radios, only for a radio that pre-empts and found no room.</para>
    /// </summary>
    private void ServeFromChannels(RadioRulesDef rules)
    {
        var kinds = ChannelKindDef.InOrder;
        if (kinds.Count == 0) return;
        var shapes = new KindShape[kinds.Count];
        for (var k = 0; k < kinds.Count; k++)
        {
            shapes[k] = new KindShape(Channels.PerUse(kinds[k].Mode.bandwidthHz, rules.channelWidthHz),
                Channels.RadiosPerUse(kinds[k].Mode.dutyCycle), kinds[k].preempts);
        }
        var channelsByStation = new Dictionary<int, int>();
        var stationById = new Dictionary<int, CompRadio>();
        foreach (var station in stations)
        {
            channelsByStation[station.parent.thingIDNumber] = (int)RadioUpgrades.Figure(station.parent, RadioUpgrades.Channels, station.Props.channels);
            stationById[station.parent.thingIDNumber] = station;
        }

        // Each radio in touch, with its kind and the stations it is in touch with, best first.
        var linksByRadio = new Dictionary<Thing, List<LinkEvaluation>>();
        foreach (var pair in pairs)
        {
            // A built-in radio standing by is shown, never served (Radio 0.10.0).
            if (!pair.InTouch || standingBy.Contains(pair.A.Thing)) continue;
            if (!linksByRadio.TryGetValue(pair.A.Thing, out var links)) linksByRadio[pair.A.Thing] = links = new List<LinkEvaluation>();
            links.Add(pair);
        }
        var ordered = new List<(Thing radio, int kind, List<LinkEvaluation> links)>();
        foreach (var entry in linksByRadio)
        {
            entry.Value.Sort((a, b) => b.MarginDb.CompareTo(a.MarginDb));
            var kind = entry.Key.TryGetComp<CompRadio>()?.Kind ?? ChannelKindDef.LocalVoice;
            ordered.Add((entry.Key, kind.LedgerIndex, entry.Value));
        }
        ordered.Sort((a, b) =>
        {
            if (a.kind != b.kind) return a.kind.CompareTo(b.kind);
            var byMargin = b.links[0].MarginDb.CompareTo(a.links[0].MarginDb);
            return byMargin != 0 ? byMargin : a.radio.thingIDNumber.CompareTo(b.radio.thingIDNumber);
        });
        var candidates = new List<ChannelCandidate>(ordered.Count);
        foreach (var (radio, kind, links) in ordered)
        {
            var best = new List<int>(links.Count);
            foreach (var link in links) best.Add(link.B.Thing.thingIDNumber);
            candidates.Add(new ChannelCandidate(radio.thingIDNumber, kind, best));
        }
        // A vehicle's picture (Radio 0.15.0, step 6): a second use of its radio, of the picture kind, offered to the stations where the
        // picture's margin on the same path holds, best first. Its id is the radio's, negated, so it never meets a thing's.
        var pictureKind = wantsPicture.Count == 0 ? null : DefDatabase<ChannelKindDef>.GetNamedSilentFail(PictureKindName);
        if (pictureKind != null)
        {
            foreach (var (radio, _, links) in ordered)
            {
                if (!wantsPicture.Contains(radio)) continue;
                var best = new List<int>(links.Count);
                foreach (var link in links)
                {
                    var pictureMargin = OtherMode.MarginDb(link.MarginDb, link.Mode.bandwidthHz, link.Mode.requiredSnrDb,
                        pictureKind.Mode.bandwidthHz, pictureKind.Mode.requiredSnrDb);
                    if (pictureMargin >= 0) best.Add(link.B.Thing.thingIDNumber);
                }
                if (best.Count > 0) candidates.Add(new ChannelCandidate(PictureId(radio), pictureKind.LedgerIndex, best));
            }
            // The ledger serves kinds in their order: keep each kind's candidates in the order they were added (strongest first).
            candidates = candidates.OrderBy(c => c.Kind).ToList();
        }

        // A call made at a desk (Radio 0.51.0; the owner's card of 27 Nov 2026: "like a hand-held"): one use of the local voice kind, offered
        // to that desk's station only. Its id is the caller's, which no radio's ever is. Served after the radios of its kind already counted.
        var voice = ChannelKindDef.LocalVoice;
        if (deskCalls.Count > 0)
        {
            foreach (var (caller, desk) in deskCalls)
                if (stationById.ContainsKey(desk.thingIDNumber)) candidates.Add(new ChannelCandidate(caller.thingIDNumber, voice.LedgerIndex, new List<int> { desk.thingIDNumber }));
            candidates = candidates.OrderBy(c => c.Kind).ToList();
        }

        var served = Channels.Allocate(channelsByStation, shapes, candidates, held);
        // Desk calls served, by station; one the station had no room for is busy, and its colonist hangs up.
        var deskCallsOn = new Dictionary<int, int>();
        foreach (var (caller, desk) in deskCalls)
        {
            if (served.TryGetValue(caller.thingIDNumber, out var at) && at == desk.thingIDNumber) deskCallsOn[at] = (deskCallsOn.TryGetValue(at, out var c) ? c : 0) + 1;
            else deskBusy.Add(caller);
        }
        held = served;
        // Pictures served, by station; one counts only where the same station serves the vehicle's orders.
        var picturesOn = new Dictionary<int, int>();
        if (pictureKind != null)
        {
            foreach (var radio in wantsPicture)
            {
                if (!served.TryGetValue(PictureId(radio), out var pictureStation)) continue;
                picturesOn[pictureStation] = (picturesOn.TryGetValue(pictureStation, out var p) ? p : 0) + 1;
                if (served.TryGetValue(radio.thingIDNumber, out var orderStation) && orderStation == pictureStation) pictureServed.Add(radio);
            }
        }
        var kindOf = new Dictionary<Thing, int>();
        foreach (var (radio, kind, links) in ordered)
        {
            var wearer = CompRadio.HolderOf(radio);
            if (!served.TryGetValue(radio.thingIDNumber, out var stationId) || !stationById.TryGetValue(stationId, out var station))
            {
                if (wearer != null)
                {
                    busy.Add(wearer);
                    // Seen in play (Radio 0.17.0, row 474): a radio in reach with no channel left for it is net busy.
                    Seen.Note("radio.net-busy", wearer, null, "busy");
                }
                continue;
            }
            if (wearer != null) servedBy[wearer] = station.parent;
            // Seen in play (Radio 0.17.0, row 475): a station other than the radio's best took it, the best one's channels being full.
            if (wearer != null && links[0].B.Thing != station.parent) Seen.Note("radio.channel-station", station.parent, wearer.LabelShort, "overflow");
            // A radio built into a vehicle is the vehicle itself (Radio 0.12.0): count it for the operator's work.
            if (radio is Pawn) vehiclesOn[station.parent] = (vehiclesOn.TryGetValue(station.parent, out var vehicles) ? vehicles : 0) + 1;
            if (!servedOn.TryGetValue(station.parent, out var list)) servedOn[station.parent] = list = new List<Thing>();
            list.Add(radio);
            kindOf[radio] = kind;
            inTouchByStation[station.parent] = (inTouchByStation.TryGetValue(station.parent, out var n) ? n : 0) + 1;
        }

        // Each station's radios kind by kind, each kind's block of channels after the one before it.
        var counts = new int[kinds.Count];
        foreach (var station in stations)
        {
            System.Array.Clear(counts, 0, counts.Length);
            if (servedOn.TryGetValue(station.parent, out var list))
            {
                list.Sort((a, b) => kindOf[a] != kindOf[b] ? kindOf[a].CompareTo(kindOf[b]) : a.thingIDNumber.CompareTo(b.thingIDNumber));
                foreach (var radio in list) placeOf[radio] = (station.parent, kindOf[radio], counts[kindOf[radio]]++);
            }
            // Its desk calls hold channels of the local voice kind (Radio 0.51.0).
            if (deskCallsOn.TryGetValue(station.parent.thingIDNumber, out var calls)) counts[voice.LedgerIndex] += calls;
            // Its pictures hold channels of their own kind too (Radio 0.15.0).
            if (pictureKind != null && picturesOn.TryGetValue(station.parent.thingIDNumber, out var pictures)) counts[pictureKind.LedgerIndex] += pictures;
            var total = channelsByStation[station.parent.thingIDNumber];
            var ledgers = new KindLedger[kinds.Count];
            var start = 0;
            for (var k = 0; k < kinds.Count; k++)
            {
                var heldHere = Channels.ChannelsHeld(counts[k], shapes[k]);
                ledgers[k] = new KindLedger(kinds[k], shapes[k], counts[k], heldHere, start, Channels.Room(k, counts, shapes, total));
                start += heldHere;
            }
            channelsOf[station.parent] = new StationChannels(total, ledgers);
            if (vehiclesOn.ContainsKey(station.parent) && !operatorAt.ContainsKey(station.parent)) stationsWantingOperator.Add(station.parent);
        }
    }

    private void CollectStations()
    {
        foreach (var building in map.listerBuildings.allBuildingsColonist)
        {
            var radio = building.TryGetComp<CompRadio>();
            if (radio == null || !radio.Props.isStation) continue;
            // Flicked off, broken down, unwired or caught by a solar flare: it reaches nobody.
            if (building.TryGetComp<CompPowerTrader>() is { PowerOn: false })
            {
                unpoweredStation = true;
                continue;
            }
            stations.Add(radio);
        }
    }

    /// <summary>
    /// Worn hand-helds, then radios built into the colony's pawns that are not people (a vehicle's, through
    /// <see cref="CarriedRadios"/>; Radio 0.10.0). Cost: the colony's free colonists' apparel, as before, and one comp lookup for each
    /// other spawned pawn of the colony (its vehicles, animals and mechanoids), about once a second.
    /// </summary>
    private void CollectMobileRadios()
    {
        foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
        {
            // A colonist working a station's console is its operator (Radio 0.12.0).
            if (pawn.jobs?.curDriver is Work.JobDriver_RemoteOperation { AtConsole: true, Station: { } station }) operatorAt[station] = pawn;
            // A call made at a desk (Radio 0.51.0) asks that station for a channel, as a hand-held does.
            if (pawn.jobs?.curDriver is IRadioDeskCall { CallingFrom: { } desk }) deskCalls.Add((pawn, desk));
            if (!Counts(pawn) || pawn.apparel == null) continue;
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var radio = apparel.TryGetComp<CompRadio>();
                if (radio != null && !radio.Props.isStation) mobileRadios.Add((radio, pawn));
            }
        }
        foreach (var pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (pawn.RaceProps.Humanlike) continue;
            var radio = pawn.GetComp<CompRadio>();
            if (radio == null || radio.Props.isStation) continue;
            var use = CarriedRadios.UseOf(pawn, radio);
            if (use == CarriedRadioUse.Off) continue;
            mobileRadios.Add((radio, pawn));
            carriers.Add(pawn);
            if (use == CarriedRadioUse.StandingBy) standingBy.Add(pawn);
            if (use == CarriedRadioUse.InUseWithPicture) wantsPicture.Add(pawn);
        }
    }

    /// <summary>
    /// Every antenna a station can use: its built-in whip, plus each antenna building on its coax that a
    /// cable path reaches. The link picks the best of them, so the best antenna for the band emerges.
    /// </summary>
    public static List<RadioEndpoint> StationEndpoints(CompRadio station, float metresPerCell)
    {
        var ends = new List<RadioEndpoint> { RadioEndpoint.BuiltIn(station.parent, station.parent.Position, station) };
        var port = station.parent.TryGetComp<CompFeedlinePort>();
        if (port?.PipeNet == null) return ends;
        foreach (var antenna in FeedlineService.AntennasOn(port))
        {
            if (!antenna.parent.Spawned) continue;
            var path = FeedlineService.Path(port.PipeNet, station.parent, antenna.parent, metresPerCell);
            if (path == null) continue;
            var end = RadioEndpoint.ViaAntenna(station, antenna, path);
            if (end.Bands.Count > 0) ends.Add(end);
        }
        return ends;
    }
}
