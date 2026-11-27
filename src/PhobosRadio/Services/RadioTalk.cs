using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Mood;
using Phobos.Radio.Physics;
using Phobos.Radio.Work;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Phobos.Radio.Services;

// Life on the air, slice 1 (Radio 0.63.0; docs/development/radio-life-on-the-air.md, the owner's cards of 28 Nov 2026: "the air is alive"
// first, and "lone workers chat often"). Colonists talk on the radio by themselves: a chat between two apart on the map (RN7), a call in the
// evening to someone close who is away in a caravan (RN8), and company through the night for someone in bed or working alone (RN9). One
// talk pass does it all, never a tick per colonist.

/// <summary>
/// The talk pass (Radio 0.63.0): every <see cref="RadioRulesDef.talkLookTicks"/>, at most <see cref="RadioRulesDef.talkPeoplePerLook"/>
/// colonists of each map with a station, in turn, may start a chat or find company; in the evening hours, each colonist away in a caravan may
/// be called by someone close at home, once a day.
/// <para>Cost (the record's budget): with the setting off, nothing. On, every 250 ticks: for each map with a station, up to eight colonists,
/// each read from the net's last pass; one who starts a chat or looks for company walks the map's free colonists once (opinions and a
/// distance). In the evening, each colonist in a caravan walks the home map's colonists once, and its reach home is searched once a day.
/// Nothing is recomputed: every link is the net's, every reach the tie-in's. Not yet measured with perf.py.</para>
/// </summary>
public static class RadioTalk
{
    private static readonly Dictionary<int, int> cursorByMap = new();
    /// <summary>When each colonist last had company on the radio at night. Not saved: after a load someone may have company twice one night.</summary>
    private static readonly Dictionary<Pawn, int> lastCompany = new();
    /// <summary>The day each colonist away was last called (or tried), so once a day. Not saved, for the same reason.</summary>
    private static readonly Dictionary<Pawn, int> calledDay = new();
    private static readonly List<(Pawn Pawn, float Weight)> choices = new();

    public static void Clear()
    {
        cursorByMap.Clear();
        lastCompany.Clear();
        calledDay.Clear();
        choices.Clear();
    }

    /// <summary>One pass over every map with a station, from the game component at the rules' interval.</summary>
    public static void Look()
    {
        if (!RadioAir.Live) return;
        var rules = RadioRulesDef.Current;
        foreach (Map map in Find.Maps)
        {
            var net = RadioNet.For(map);
            if (net == null || net.Stations.Count == 0) continue;
            LookAt(map, net, rules);
            if (TalkRule.InHours(GenLocalDate.HourOfDay(map), rules.talkEveningFrom, rules.talkEveningTo)) EveningCalls(map, net, rules);
        }
    }

    /// <summary>
    /// Whether a colonist could talk on a hand-held now: awake and well enough to talk and hear, not drafted, in a gathering or already talking,
    /// and served on a channel of a station in touch.
    /// </summary>
    public static bool Ready(Pawn pawn, RadioNet net)
    {
        if (!Free(pawn)) return false;
        return net.ServedBy(pawn) != null && net.TryGetBest(pawn, out var link) && link != null && link.Outcome != LinkOutcome.NoContact;
    }

    private static bool Free(Pawn pawn) =>
        pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.RaceProps.Humanlike && pawn.Awake() && !pawn.InMentalState && !pawn.Drafted
        && pawn.GetLord() == null && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking)
        && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) && !RadioAir.OnAir(pawn);

    /// <summary>Nobody else of the colony within the rules' cells: someone working alone.</summary>
    private static bool Lone(Pawn pawn, Map map, int cells)
    {
        foreach (Pawn other in map.mapPawns.FreeColonistsSpawned)
            if (other != pawn && other.Position.InHorDistOf(pawn.Position, cells)) return false;
        return true;
    }

    private static void LookAt(Map map, RadioNet net, RadioRulesDef rules)
    {
        var people = map.mapPawns.FreeColonistsSpawned;
        int n = people.Count;
        if (n == 0) return;
        int start = cursorByMap.TryGetValue(map.uniqueID, out var at) ? at % n : 0;
        int count = System.Math.Min(n, rules.talkPeoplePerLook);
        cursorByMap[map.uniqueID] = (start + count) % n;
        int now = Find.TickManager.TicksGame;
        bool night = TalkRule.InHours(GenLocalDate.HourOfDay(map), rules.talkNightFrom, rules.talkNightTo);
        for (int i = 0; i < count; i++)
        {
            Pawn pawn = people[(start + i) % n];
            if (!Ready(pawn, net)) continue;
            if (night && WantsCompany(pawn, map, rules, now))
            {
                Company(pawn, map, net, rules, now);
                continue;
            }
            Chat(pawn, map, net, rules);
        }
    }

    // ---- RN7: a chat across the map ----

    /// <summary>
    /// A chat (RN7): a colonist working alone calls someone every few hours on average, one with others near every half a day (the one named
    /// roll, <see cref="TalkRule.ChancePerLook"/>). Whom: a colonist on the same map out of earshot, served too, chosen by what each thinks of
    /// the other. On a faint link it may be misheard (the second named roll, as for a call to a faction): a slight instead of a chat.
    /// </summary>
    private static void Chat(Pawn pawn, Map map, RadioNet net, RadioRulesDef rules, bool rollSkipped = false)
    {
        bool lone = Lone(pawn, map, rules.talkLoneCells);
        double chance = TalkRule.ChancePerLook(rules.talkLookTicks, lone ? rules.talkChatGapLoneHours : rules.talkChatGapHours, GenDate.TicksPerHour);
        if (!rollSkipped && !Rand.Chance((float)chance)) return;
        choices.Clear();
        foreach (Pawn other in map.mapPawns.FreeColonistsSpawned)
        {
            if (other == pawn || other.Position.InHorDistOf(pawn.Position, rules.talkMinCells) || !Ready(other, net)) continue;
            double weight = TalkRule.ChatWeight(pawn.relations?.OpinionOf(other) ?? 0, other.relations?.OpinionOf(pawn) ?? 0, rules.talkFloorOpinion);
            if (weight > 0) choices.Add((other, (float)weight));
        }
        if (!choices.TryRandomElementByWeight(c => c.Weight, out var chosen)) return;
        Pawn answer = chosen.Pawn;
        bool faint = Faint(pawn, net) || Faint(answer, net);
        bool misheard = faint && Rand.Chance(rules.radioMishearChance);
        InteractionDef interaction = misheard ? Slight : InteractionDefOf.Chitchat;
        TalkApart.Interact(pawn, answer, interaction);
        pawn.needs?.joy?.GainJoy(rules.talkChatJoy, JoyKindDefOf.Social);
        answer.needs?.joy?.GainJoy(rules.talkChatJoy, JoyKindDefOf.Social);
        int now = Find.TickManager.TicksGame;
        RadioAir.Begin(new Over(pawn, answer, null, OverKind.Chat, now, now + rules.talkChatTicks, faint, Carriers(net, pawn, answer)));
        string variant = misheard ? "misheard" : lone ? "lone" : "company";
        if (Seen.Wants("radio.air-chat", pawn, variant)) Seen.Note("radio.air-chat", pawn, pawn.LabelShort + " to " + answer.LabelShort, variant);
    }

    /// <summary>The game's own slight, for a chat misheard; the insult's memories if another mod has removed it.</summary>
    private static InteractionDef Slight => DefDatabase<InteractionDef>.GetNamedSilentFail("Slight") ?? InteractionDefOf.Insult;

    private static bool Faint(Pawn pawn, RadioNet net) => net.TryGetBest(pawn, out var link) && link?.Outcome == LinkOutcome.Marginal;

    private static IReadOnlyList<Thing> Carriers(RadioNet net, Pawn first, Pawn? second)
    {
        var stations = new List<Thing>(2);
        if (net.ServedBy(first) is { } a) stations.Add(a);
        if (second != null && second.Spawned && second.Map == first.Map && net.ServedBy(second) is { } b && !stations.Contains(b)) stations.Add(b);
        return stations;
    }

    // ---- RN9: company through the night ----

    /// <summary>
    /// At night, once a night: a colonist in bed who should rest (sick, hurt, recovering), or one awake at work with nobody near.
    /// </summary>
    private static bool WantsCompany(Pawn pawn, Map map, RadioRulesDef rules, int now)
    {
        if (!TalkRule.Since(lastCompany.TryGetValue(pawn, out var last) ? last : -1, now, rules.talkCompanyGapTicks)) return false;
        if (pawn.InBed()) return HealthAIUtility.ShouldSeekMedicalRest(pawn);
        return pawn.CurJob?.workGiverDef != null && Lone(pawn, map, rules.talkLoneCells);
    }

    /// <summary>
    /// Company through the night (RN9): the colonist awake on the map who likes them best and is not in bed keeps them company on the radio;
    /// with nobody, their radio friend, if the friend's people are in reach. No roll: someone who needs company finds it when anyone is there.
    /// </summary>
    private static void Company(Pawn pawn, Map map, RadioNet net, RadioRulesDef rules, int now)
    {
        Pawn? best = null;
        int bestScore = int.MinValue;
        foreach (Pawn other in map.mapPawns.FreeColonistsSpawned)
        {
            if (other == pawn || other.InBed() || !Ready(other, net)) continue;
            int of = pawn.relations?.OpinionOf(other) ?? 0, from = other.relations?.OpinionOf(pawn) ?? 0;
            if (of < 0 || from < 0 || of + from <= bestScore) continue;
            best = other;
            bestScore = of + from;
        }
        string? friendName = null;
        if (best == null)
        {
            if (RadioFriends.Of(pawn)?.Friend is not { Dead: false, Destroyed: false, Faction: { } faction } friend || !FactionDoctors.Talks(faction)
                || FactionDoctors.ReachToTalk(map, map.Tile, () => FactionDoctors.StationEnds(map), faction) == null) return;
            friendName = friend.LabelShort;
            // The friendship grows by the night's company, as by a chat.
            RadioFriends.Chatting(pawn, rules.talkCompanyTicks);
        }
        lastCompany[pawn] = now;
        bool inBed = pawn.InBed();
        pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtMaker.MakeThought(RadioTalkDefOf.PhobosRadio_CompanyOnAir, best != null ? 0 : 1));
        if (best != null) best.needs?.mood?.thoughts?.memories?.TryGainMemory(RadioTalkDefOf.PhobosRadio_KeptCompany, pawn);
        bool faint = Faint(pawn, net) || best != null && Faint(best, net);
        RadioAir.Begin(new Over(pawn, best, friendName, OverKind.Company, now, now + rules.talkCompanyTicks, faint, Carriers(net, pawn, best)));
        string other2 = best?.LabelShort ?? friendName!;
        RadioLog.Add(RadioLogKind.Talk, (inBed ? "PhobosRadioTalk_CompanyInBed" : "PhobosRadioTalk_CompanyAtWork")
            .Translate(pawn.LabelShort.Named("COLONIST"), other2.Named("OTHER")).Resolve());
        string variant = (inBed ? "bed" : "work") + (best != null ? "-colonist" : "-friend");
        if (Seen.Wants("radio.air-company", pawn, variant)) Seen.Note("radio.air-company", pawn, pawn.LabelShort + " with " + other2, variant);
    }

    // ---- RN8: the evening call ----

    /// <summary>
    /// In the evening hours, for each colonist away in one of the player's caravans, once a day: the one at home closest to them (a partner,
    /// then family, then a friend) calls when free to, from their hand-held or a station's desk. The caravan's reach home is searched once, when
    /// someone is ready to call; a caravan with a hand-held but no reach leaves the caller unable to get through.
    /// </summary>
    private static void EveningCalls(Map map, RadioNet net, RadioRulesDef rules)
    {
        int day = GenLocalDate.Year(map) * GenDate.DaysPerYear + GenLocalDate.DayOfYear(map);
        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            if (caravan.Faction != Faction.OfPlayer) continue;
            foreach (Pawn away in caravan.PawnsListForReading)
            {
                if (!away.IsColonist || away.IsSlave || away.Dead || away.Downed || !away.RaceProps.Humanlike) continue;
                if (calledDay.TryGetValue(away, out var last) && last == day) continue;
                if (!away.health.capacities.CapableOf(PawnCapacityDefOf.Talking)) continue;
                if (Closest(away, map, rules, out var tie) is not { } caller) continue;
                if (!CanCall(caller, net)) continue;
                calledDay[away] = day;
                RemoteHome? home = null;
                foreach (RemoteHome reached in RemoteReach.Of(away))
                {
                    if (reached.Home != map || reached.Morse) continue;
                    home = reached;
                    break;
                }
                if (home is not { } found)
                {
                    if (CaravanRadios.BestHandheld(caravan) != null) CouldNotGetThrough(caller, away);
                    continue;
                }
                StartCall(caller, away, tie, rules.MarginBands.Classify(found.MarginDb) == LinkOutcome.Marginal, net, rules);
            }
        }
    }

    /// <summary>The colonist at home closest to one away: a partner first, then family by blood, then the best-liked friend; null with none.</summary>
    private static Pawn? Closest(Pawn away, Map map, RadioRulesDef rules, out TalkRule.Tie tie)
    {
        Pawn? best = null;
        tie = TalkRule.Tie.None;
        int bestOpinion = int.MinValue;
        foreach (Pawn home in map.mapPawns.FreeColonistsSpawned)
        {
            if (away.relations == null || home.relations == null || home.relations.OpinionOf(away) < rules.talkFloorOpinion) continue;
            int opinion = away.relations.OpinionOf(home);
            var found = TalkRule.TieOf(LovePartnerRelationUtility.LovePartnerRelationExists(away, home), away.relations.FamilyByBlood.Contains(home),
                opinion, rules.talkFriendOpinion);
            if (found == TalkRule.Tie.None || found > tie || found == tie && opinion <= bestOpinion) continue;
            best = home;
            tie = found;
            bestOpinion = opinion;
        }
        return best;
    }

    /// <summary>Whether a colonist at home can make a call now: free, not in bed, doing nothing that may not be put aside, with a hand-held in
    /// touch or a station's desk to walk to.</summary>
    private static bool CanCall(Pawn caller, RadioNet net) =>
        Free(caller) && !caller.InBed() && RadioAdvice.Interruptible(caller) && (Ready(caller, net) || RadioAdvice.NearestDesk(caller, net) != null);

    private static void StartCall(Pawn caller, Pawn away, TalkRule.Tie tie, bool faint, RadioNet net, RadioRulesDef rules)
    {
        Thing? desk = Ready(caller, net) ? null : RadioAdvice.NearestDesk(caller, net);
        Job job = JobMaker.MakeJob(RadioTalkDefOf.PhobosRadio_EveningCall, desk == null ? LocalTargetInfo.Invalid : desk);
        job.count = rules.talkEveningCallTicks;
        JobDriver_PhobosEveningCall.Pending = (away, (int)tie, faint);
        caller.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
        JobDriver_PhobosEveningCall.Pending = null;
    }

    /// <summary>The call begins, once the caller is at the radio: an over on the station that carries it.</summary>
    public static void CallBegan(Pawn caller, Pawn away, bool faint, Thing? desk)
    {
        int now = Find.TickManager.TicksGame;
        var stations = new List<Thing>(1);
        if (desk != null) stations.Add(desk);
        else if (caller.Spawned && RadioNet.For(caller.Map).ServedBy(caller) is { } station) stations.Add(station);
        RadioAir.Begin(new Over(caller, away, null, OverKind.EveningCall, now, now + RadioRulesDef.Current.talkEveningCallTicks, faint, stations));
    }

    /// <summary>
    /// The call ends: talked for at least half its time, both are glad of it, by how close they are; hung up almost at once (no channel at the
    /// desk), the caller could not get through.
    /// </summary>
    public static void CallEnded(Pawn caller, Pawn? away, TalkRule.Tie tie, int talked, int length, bool faint)
    {
        RadioAir.End(caller);
        if (away == null) return;
        if (talked < RadioRulesDef.Current.talkTurnTicks)
        {
            CouldNotGetThrough(caller, away);
            return;
        }
        if (talked * 2 < length) return;
        int stage = System.Math.Min((int)tie, 2);
        caller.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtMaker.MakeThought(RadioTalkDefOf.PhobosRadio_TalkedApart, stage), away);
        away.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtMaker.MakeThought(RadioTalkDefOf.PhobosRadio_TalkedApart, stage), caller);
        string where = away.GetCaravan()?.LabelCap ?? "?";
        RadioLog.Add(RadioLogKind.Talk, "PhobosRadioTalk_EveningCall".Translate(caller.LabelShort.Named("CALLER"), away.LabelShort.Named("AWAY"),
            where.Named("CARAVAN")).Resolve());
        string variant = tie.ToString().ToLowerInvariant() + (faint ? "-faint" : "");
        if (Seen.Wants("radio.air-evening-call", caller, variant))
            Seen.Note("radio.air-evening-call", caller, caller.LabelShort + " to " + away.LabelShort + ", " + talked + " ticks", variant);
    }

    private static void CouldNotGetThrough(Pawn caller, Pawn away)
    {
        caller.needs?.mood?.thoughts?.memories?.TryGainMemory(RadioTalkDefOf.PhobosRadio_CouldNotGetThrough);
        RadioLog.Add(RadioLogKind.Talk, "PhobosRadioTalk_CouldNotGetThrough".Translate(caller.LabelShort.Named("CALLER"), away.LabelShort.Named("AWAY")).Resolve());
        if (Seen.Wants("radio.air-no-answer", caller)) Seen.Note("radio.air-no-answer", caller, caller.LabelShort + " to " + away.LabelShort);
    }

    /// <summary>Dev action: a chat now for this colonist, the roll skipped (Radio 0.63.0). False with nobody to chat with.</summary>
    public static bool ChatNow(Pawn pawn)
    {
        if (!pawn.Spawned || RadioNet.For(pawn.Map) is not { } net || !Ready(pawn, net)) return false;
        Chat(pawn, pawn.Map, net, RadioRulesDef.Current, rollSkipped: true);
        return RadioAir.Of(pawn) != null;
    }

    /// <summary>Dev action: the evening calls now, whatever the hour, and again today for those already called (Radio 0.63.0).</summary>
    public static void EveningNow(Map map)
    {
        if (RadioNet.For(map) is not { } net) return;
        calledDay.Clear();
        EveningCalls(map, net, RadioRulesDef.Current);
    }
}

[DefOf]
public static class RadioTalkDefOf
{
    public static ThoughtDef PhobosRadio_TalkedApart = null!;
    public static ThoughtDef PhobosRadio_CouldNotGetThrough = null!;
    public static ThoughtDef PhobosRadio_CompanyOnAir = null!;
    public static ThoughtDef PhobosRadio_KeptCompany = null!;
    public static JobDef PhobosRadio_EveningCall = null!;

    static RadioTalkDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioTalkDefOf));
}
