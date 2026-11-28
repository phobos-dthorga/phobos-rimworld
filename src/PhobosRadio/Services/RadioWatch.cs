using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>How a call coming in was heard (Radio 0.73.0, RN4).</summary>
public enum RadioWatchHow
{
    /// <summary>A colonist at a station's desk: on watch, working the console, or on a call there.</summary>
    Desk,
    /// <summary>An awake colonist whose hand-held is in touch with the colony's stations.</summary>
    Handheld,
    /// <summary>Nobody was listening, and the selective call woke the on-call colonist.</summary>
    SelectiveCall,
}

/// <summary>Who is listening to the radio on one map, as last worked out (Radio 0.73.0, RN4). Never saved.</summary>
public sealed class RadioWatchState
{
    /// <summary>Awake colonists at a working station's desk.</summary>
    public readonly List<Pawn> AtDesks = new();
    /// <summary>Awake colonists, not drafted, whose hand-held is in touch.</summary>
    public readonly List<Pawn> OnHandhelds = new();
    /// <summary>Colonists on the radio-watch job, walking to the desk or there.</summary>
    public readonly List<Pawn> Watchers = new();
    /// <summary>The colonist the selective call would wake: asleep, able to hear, with Radio watch ticked at its highest priority.</summary>
    public Pawn? OnCall;
    /// <summary>A working station with the selective-call upgrade, or null.</summary>
    public Thing? SelectiveCallStation;
    /// <summary>Whether the map has a working station at all.</summary>
    public bool HasStation;
    public int Tick = int.MinValue;

    public bool AnyListening => AtDesks.Count > 0 || OnHandhelds.Count > 0;

    /// <summary>The one who takes a call: someone at a desk first, then someone on a hand-held.</summary>
    public Pawn? FirstListener => AtDesks.Count > 0 ? AtDesks[0] : OnHandhelds.Count > 0 ? OnHandhelds[0] : null;

    /// <summary>Whether anyone but this colonist is listening.</summary>
    public bool ListeningBesides(Pawn pawn) => AtDesks.Any(p => p != pawn) || OnHandhelds.Any(p => p != pawn);
}

/// <summary>
/// Radio watch (Radio 0.73.0; docs/development/radio-life-on-the-air.md, RN4 and RN4b, the owner's answer of 28 Nov 2026: every call needs a
/// listener, with no setting, as repeaters, relays and the selective call are the remedy). A station hears a call only while someone
/// listens; one nobody hears is missed and logged.
/// <para>Cost: one look over a map's free colonists, reading what the net already holds (no link sums), kept 250 ticks and worked out only
/// when read: a station's inspect text while selected, the radio-watch work's look, or a call coming in (which always looks afresh).</para>
/// </summary>
public static class RadioWatch
{
    /// <summary>How long a look stays good, in ticks (the record's figure).</summary>
    private const int FreshTicks = 250;

    // Authored figures (Radio 0.73.0), for the owner to revise. Named here rather than in the rules Def [A], as another session was
    // reworking that file in the same tree; they move there when it is next touched.
    /// <summary>Hours after a missed neighbour's call before they try once more.</summary>
    public const float RetryHours = 3f;
    /// <summary>One stint of radio watch at the desk, in ticks (an hour), before the colonist looks again at what else needs them.</summary>
    public const int StintTicks = 2500;
    /// <summary>The hour of the map's day the standing order to ask a trader in is looked at.</summary>
    public const int TraderHour = 10;

    private static readonly Dictionary<Map, RadioWatchState> states = new();
    private static WorkTypeDef? watchWork;

    /// <summary>The Radio watch work type.</summary>
    public static WorkTypeDef WatchWork => watchWork ??= DefDatabase<WorkTypeDef>.GetNamed("PhobosRadio_RadioWatch");

    public static void Clear() => states.Clear();

    /// <summary>Something changed who listens (a watch began or ended, someone was woken): the next read looks again.</summary>
    public static void MarkDirty(Map? map)
    {
        if (map != null && states.TryGetValue(map, out var state)) state.Tick = int.MinValue;
    }

    /// <summary>Who is listening on this map: the last look while it is fresh, or a new one (always new with <paramref name="fresh"/>).</summary>
    public static RadioWatchState Of(Map map, bool fresh = false)
    {
        if (!states.TryGetValue(map, out var state))
        {
            if (states.Count > Find.Maps.Count + 2) states.RemoveAll(pair => !Find.Maps.Contains(pair.Key));
            states[map] = state = new RadioWatchState();
        }
        int now = Find.TickManager.TicksGame;
        if (fresh || now - state.Tick >= FreshTicks || now < state.Tick) Look(map, state, now);
        return state;
    }

    private static void Look(Map map, RadioWatchState state, int now)
    {
        state.AtDesks.Clear();
        state.OnHandhelds.Clear();
        state.Watchers.Clear();
        state.OnCall = null;
        state.SelectiveCallStation = null;
        state.HasStation = false;
        state.Tick = now;
        var net = RadioNet.For(map);
        foreach (CompRadio station in net.Stations)
        {
            Thing desk = station.parent;
            if (!desk.Spawned || desk.Map != map || !RadioFlare.Works(desk)) continue;
            state.HasStation = true;
            if (state.SelectiveCallStation == null && RadioUpgrades.Figure(desk, RadioUpgrades.SelectiveCall, 0f) >= 1f) state.SelectiveCallStation = desk;
        }
        if (!state.HasStation) return;
        int onCallPriority = int.MaxValue;
        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn.jobs?.curDriver is Work.JobDriver_PhobosRadioWatch) state.Watchers.Add(pawn);
            if (pawn.Downed || !CanHear(pawn)) continue;
            if (!pawn.Awake())
            {
                if (pawn.workSettings == null || pawn.WorkTypeIsDisabled(WatchWork) || !pawn.workSettings.WorkIsActive(WatchWork)) continue;
                int priority = pawn.workSettings.GetPriority(WatchWork);
                if (priority <= 0) priority = int.MaxValue - 1;
                if (priority < onCallPriority)
                {
                    onCallPriority = priority;
                    state.OnCall = pawn;
                }
                continue;
            }
            if (pawn.InMentalState) continue;
            if (DeskOf(pawn) is { } desk && RadioFlare.Works(desk))
            {
                state.AtDesks.Add(pawn);
                continue;
            }
            // A drafted colonist's hand-held is on the guard net (slice 1), not the home net that calls come in on.
            if (!pawn.Drafted && RadioAdvice.InTouch(pawn)) state.OnHandhelds.Add(pawn);
        }
    }

    private static bool CanHear(Pawn pawn) => pawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Hearing) ?? false;

    /// <summary>The station desk this colonist is at now, or null: on radio watch, working the console for vehicles, or on a call there.</summary>
    public static Thing? DeskOf(Pawn pawn) => pawn.jobs?.curDriver switch
    {
        Work.JobDriver_PhobosRadioWatch { AtDesk: true } watch => watch.Station,
        Work.JobDriver_RemoteOperation { AtConsole: true } console => console.Station,
        IRadioDeskCall call => call.CallingFrom,
        _ => null,
    };

    /// <summary>Whether this colonist should keep watch at a desk now: a working station, nobody else listening or on watch, and not
    /// already listening on their own hand-held.</summary>
    public static bool WantsWatcher(Map map, Pawn pawn)
    {
        RadioWatchState state = Of(map);
        if (!state.HasStation || state.ListeningBesides(pawn) || state.OnHandhelds.Contains(pawn)) return false;
        return state.Watchers.All(watcher => watcher == pawn);
    }

    /// <summary>
    /// A call coming in to this map, from <paramref name="caller"/> about <paramref name="what"/>: the colonist who hears it, or null when
    /// nobody does (the call is missed, and logged so). With nobody listening, a station's selective call wakes the on-call colonist.
    /// <paramref name="kind"/> names the call for the seen-in-play note.
    /// </summary>
    public static Pawn? Hear(Map map, string caller, string what, string kind)
    {
        RadioWatchState state = Of(map, fresh: true);
        Pawn? listener = state.FirstListener;
        RadioWatchHow how = state.AtDesks.Count > 0 ? RadioWatchHow.Desk : RadioWatchHow.Handheld;
        if (listener == null && state.SelectiveCallStation != null && state.OnCall is { } onCall)
        {
            RestUtility.WakeUp(onCall);
            // The game's own memory for broken sleep.
            onCall.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.SleepDisturbed);
            listener = onCall;
            how = RadioWatchHow.SelectiveCall;
            MarkDirty(map);
        }
        if (listener == null)
        {
            RadioLog.Add(RadioLogKind.Watch, "PhobosRadioLog_CallMissed".Translate(caller.Named("CALLER"), what.Named("WHAT")).Resolve());
            if (Seen.Wants("radio.call-missed", null, kind)) Seen.Note("radio.call-missed", null, caller, kind);
            return null;
        }
        NamedArgument who = listener.LabelShort.Named("LISTENER"), from = caller.Named("CALLER"), about = what.Named("WHAT");
        RadioLog.Add(RadioLogKind.Watch, (how switch
        {
            RadioWatchHow.Desk => "PhobosRadioLog_CallHeardDesk".Translate(who, from, about),
            RadioWatchHow.Handheld => "PhobosRadioLog_CallHeardHandheld".Translate(who, from, about),
            _ => "PhobosRadioLog_CallHeardSelectiveCall".Translate(who, from, about),
        }).Resolve());
        string variant = how switch
        {
            RadioWatchHow.Desk => "desk",
            RadioWatchHow.Handheld => "handheld",
            _ => "selective call",
        };
        if (Seen.Wants("radio.watch-heard", listener, variant)) Seen.Note("radio.watch-heard", listener, caller + ", " + kind, variant);
        return listener;
    }

    /// <summary>The station's line on who is listening, for its inspect text.</summary>
    public static string InspectLine(Map map)
    {
        RadioWatchState state = Of(map);
        if (state.AtDesks.Count > 0)
            return "PhobosRadioWatch_InspectDesk".Translate(state.AtDesks[0].LabelShort.Named("NAME"),
                (state.AtDesks.Count + state.OnHandhelds.Count).Named("COUNT")).Resolve();
        if (state.OnHandhelds.Count > 0)
            return "PhobosRadioWatch_InspectHandhelds".Translate(state.OnHandhelds[0].LabelShort.Named("NAME"),
                state.OnHandhelds.Count.Named("COUNT")).Resolve();
        if (state.SelectiveCallStation != null && state.OnCall != null)
            return "PhobosRadioWatch_InspectOnCall".Translate(state.OnCall.LabelShort.Named("NAME")).Resolve();
        return (state.SelectiveCallStation != null ? "PhobosRadioWatch_InspectNoOnCall".Translate() : "PhobosRadioWatch_InspectNobody".Translate()).Resolve();
    }
}

/// <summary>
/// Standing orders (Radio 0.73.0; docs/development/radio-life-on-the-air.md, RN5): what the colonist listening at the radio may do unasked,
/// ticked on a station (saved there). A box ticked on any working station of the map counts. Each order rides the event it answers.
/// <para>Cost: answering a neighbour rides the neighbour's call; the trader order looks once a game day per home map, at the map's hour
/// from the rules, reading the silver count the game keeps and the allies in reach (the reach search, already kept an hour).</para>
/// </summary>
public static class RadioStandingOrders
{
    /// <summary>The silver the trader order starts at, its step in the window, and its bounds (authored).</summary>
    public const int DefaultTraderSilver = 2000;
    public const int TraderSilverStep = 100;
    public const int MinTraderSilver = 100;
    public const int MaxTraderSilver = 1000000;

    // ---- Changing an order (from the station's window) ----

    public static void SetAnswerNeighbours(CompRadio station, bool on) => station.orderAnswerNeighbours = on;
    public static void SetAskTrader(CompRadio station, bool on) => station.orderAskTrader = on;
    public static void SetTraderSilver(CompRadio station, int silver) =>
        station.orderTraderSilver = UnityEngine.Mathf.Clamp(silver, MinTraderSilver, MaxTraderSilver);

    /// <summary>A working station of the map with this order ticked, or null.</summary>
    private static CompRadio? Ordering(Map map, System.Func<CompRadio, bool> ticked)
    {
        foreach (CompRadio station in RadioNet.For(map).Stations)
        {
            if (station.parent.Spawned && station.parent.Map == map && RadioFlare.Works(station.parent) && ticked(station)) return station;
        }
        return null;
    }

    /// <summary>Whether the map's stations have the order to answer neighbours who ask for help.</summary>
    public static bool AnswersNeighbours(Map map) => Ordering(map, station => station.orderAnswerNeighbours) != null;

    /// <summary>A neighbour's request answered by order: a message in place of the letter, and the Log.</summary>
    public static void AnsweredNeighbour(Pawn listener, Pawn sent, Faction faction)
    {
        string text = "PhobosRadioOrders_NeighbourAnswered".Translate(listener.LabelShort.Named("LISTENER"), sent.LabelShort.Named("COLONIST"),
            faction.Name.Named("FACTION")).Resolve();
        Messages.Message(text, new LookTargets(sent), MessageTypeDefOf.NeutralEvent, historical: false);
        RadioLog.Add(RadioLogKind.Watch, text);
        if (Seen.Wants("radio.standing-order", listener, "neighbours")) Seen.Note("radio.standing-order", listener, faction.Name, "neighbours");
    }

    /// <summary>Once a game hour: at each home map's hour from the rules, the trader order.</summary>
    public static void Hourly()
    {
        int hour = RadioWatch.TraderHour;
        foreach (Map map in Find.Maps)
        {
            if (map.IsPlayerHome && GenLocalDate.HourOfDay(map) == hour) TryAskTrader(map);
        }
    }

    /// <summary>
    /// The order to ask an allied trader in, now: why it did not, in the player's words, or null when a trader was asked for. The one
    /// listening asks the first ally in reach that can send one, with a caravan kind of theirs at random, as the call menu asks.
    /// </summary>
    public static string? TryAskTrader(Map map)
    {
        if (Ordering(map, station => station.orderAskTrader) is not { } station) return "PhobosRadioOrders_TraderNoOrder".Translate().Resolve();
        int silver = map.resourceCounter.Silver;
        if (silver < station.orderTraderSilver)
            return "PhobosRadioOrders_TraderNotEnough".Translate(silver.Named("SILVER"), station.orderTraderSilver.Named("THRESHOLD")).Resolve();
        if (GameComponent_PhobosRadioDiplomacy.Current?.HasOrderFor(map) == true) return "PhobosRadioOrders_TraderOnWay".Translate().Resolve();
        RadioWatchState state = RadioWatch.Of(map, fresh: true);
        Pawn? caller = state.AtDesks.Concat(state.OnHandhelds).FirstOrDefault(p => p.health.capacities.CapableOf(PawnCapacityDefOf.Talking));
        if (caller == null) return "PhobosRadioOrders_TraderNobody".Translate().Resolve();
        foreach (FactionDoctorLink link in RadioDiplomacy.InReach(map))
        {
            Faction faction = link.Faction;
            if (RadioDiplomacy.TraderRefusal(map, faction) != null) continue;
            List<TraderKindDef> kinds = faction.def.caravanTraderKinds.Where(kind => kind.requestable).ToList();
            if (kinds.Count == 0) continue;
            string reply = RadioDiplomacy.RequestTrader(caller, map, faction, kinds.RandomElement(), link);
            string text = "PhobosRadioOrders_TraderAsked".Translate(caller.LabelShort.Named("LISTENER"), faction.Name.Named("FACTION"),
                silver.Named("SILVER"), reply.Named("REPLY")).Resolve();
            Messages.Message(text, new LookTargets(caller), MessageTypeDefOf.NeutralEvent, historical: false);
            RadioLog.Add(RadioLogKind.Watch, text);
            if (Seen.Wants("radio.standing-order", caller, "trader")) Seen.Note("radio.standing-order", caller, faction.Name, "trader");
            return null;
        }
        return "PhobosRadioOrders_TraderNoAlly".Translate().Resolve();
    }
}
