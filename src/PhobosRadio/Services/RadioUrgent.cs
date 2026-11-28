using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Work;
using Phobos.Radio.Physics;
using Phobos.Radio.Work;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Phobos.Radio.Services;

/// <summary>What trouble a colonist calling for help is in (Radio 0.76.0, RN16): worse troubles are higher, and may call again.</summary>
public enum HelpTrouble
{
    None = 0,
    /// <summary>Hurt within the rules' few ticks: under attack.</summary>
    Attacked = 1,
    /// <summary>Bleeding at or over the rules' rate.</summary>
    Bleeding = 2,
    /// <summary>Downed, and not in a bed.</summary>
    Downed = 3,
}

/// <summary>
/// Acting on what is heard (Radio 0.76.0; docs/development/radio-life-on-the-air.md, slice 3). A colonist alone and in trouble calls for help on
/// their hand-held (RN16), heard by whoever is listening, who sends the nearest colonist who can doctor; a colonist who sees a threat arrive
/// calls it in (RN17), and those in touch who do not fight take cover. Both are urgent overs, served before chatter (RN6).
/// <para>Cost (the record's, stated before building): every 250 ticks, for each map with a working station, one walk over the free colonists
/// reading flags the game keeps (downed, its cached bleed rate, when last hurt); a call's searches (who is near, the nearest helper, a bed)
/// only for a colonist in trouble. Sightings only while the game's list of things hostile to the colony holds a group not yet reported: the
/// colonists in touch against at most 30 of its members, a distance before any line of sight. Each colonist taking cover searches at most the
/// rules' regions once. Nothing is saved: after a load, a group still on the map may be reported once more.</para>
/// </summary>
public static class RadioUrgent
{
    /// <summary>A call for help on the air, waiting for a channel or a listener.</summary>
    private sealed class HelpCall
    {
        public HelpCall(HelpTrouble trouble) => Trouble = trouble;
        public HelpTrouble Trouble { get; }
        public bool Waiting { get; set; }
    }

    private static readonly Dictionary<Pawn, HelpCall> calling = new();
    /// <summary>For a colonist who has called: when they may call again, and the trouble they called about (a worse one calls at once).</summary>
    private static readonly Dictionary<Pawn, (int At, HelpTrouble Trouble)> nextCall = new();
    /// <summary>The threat groups each map has had reported, by key; a key is dropped once its group is gone.</summary>
    private static readonly Dictionary<int, HashSet<string>> reported = new();
    private static readonly Dictionary<string, List<Pawn>> groups = new();
    private static readonly List<Pawn> reporters = new();

    public static void Clear()
    {
        calling.Clear();
        nextCall.Clear();
        reported.Clear();
        groups.Clear();
        reporters.Clear();
    }

    /// <summary>The urgent look, from the game component at the rules' interval.</summary>
    public static void Look()
    {
        var rules = RadioRulesDef.Current;
        int now = Find.TickManager.TicksGame;
        foreach (Map map in Find.Maps)
        {
            var net = RadioNet.For(map);
            if (net == null || net.Stations.Count == 0) continue;
            LookForHelp(map, net, rules, now);
            LookForThreats(map, net, rules, now);
        }
        if (nextCall.Count > 0) nextCall.RemoveAll(pair => pair.Key.Dead || pair.Key.Destroyed || pair.Value.At < now - rules.helpRetryTicks);
    }

    // ---- RN16: a call for help ----

    private static void LookForHelp(Map map, RadioNet net, RadioRulesDef rules, int now)
    {
        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            HelpTrouble trouble = TroubleOf(pawn, rules, now);
            if (calling.TryGetValue(pawn, out var call))
            {
                if (trouble == HelpTrouble.None)
                {
                    calling.Remove(pawn);
                    RadioAir.End(pawn);
                    continue;
                }
                Advance(pawn, call, map, net, rules, now);
                continue;
            }
            if (trouble == HelpTrouble.None) continue;
            if (nextCall.TryGetValue(pawn, out var last) && now < last.At && trouble <= last.Trouble) continue;
            if (!CanCall(pawn, net) || !Alone(pawn, map, rules.talkLoneCells)) continue;
            StartCall(pawn, trouble, map, net, rules, now);
        }
    }

    /// <summary>The trouble a colonist is in now: downed outside a bed, bleeding badly, or hurt a moment ago; none otherwise.</summary>
    public static HelpTrouble TroubleOf(Pawn pawn, RadioRulesDef rules, int now)
    {
        if (pawn.Dead || pawn.InBed()) return HelpTrouble.None;
        if (pawn.Downed) return HelpTrouble.Downed;
        if (pawn.health.hediffSet.BleedRateTotal >= rules.helpBleedRate) return HelpTrouble.Bleeding;
        if (!pawn.Drafted && pawn.mindState != null && now - pawn.mindState.lastHarmTick <= rules.helpHurtTicks) return HelpTrouble.Attacked;
        return HelpTrouble.None;
    }

    /// <summary>Awake, able to talk, with a hand-held in touch with the colony's stations (served, waiting for a channel, or through a vehicle).</summary>
    private static bool CanCall(Pawn pawn, RadioNet net) =>
        pawn.Awake() && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) && (net.IsBusy(pawn) || RadioAdvice.InTouch(pawn));

    /// <summary>No other colonist able to help within the rules' cells.</summary>
    private static bool Alone(Pawn pawn, Map map, int cells)
    {
        foreach (Pawn other in map.mapPawns.FreeColonistsSpawned)
            if (other != pawn && !other.Downed && other.Awake() && other.Position.InHorDistOf(pawn.Position, cells)) return false;
        return true;
    }

    private static void StartCall(Pawn pawn, HelpTrouble trouble, Map map, RadioNet net, RadioRulesDef rules, int now)
    {
        var call = new HelpCall(trouble);
        calling[pawn] = call;
        var stations = new List<Thing>(1);
        if (net.ServedBy(pawn) is { } station) stations.Add(station);
        bool faint = net.TryGetBest(pawn, out var link) && link?.Outcome == LinkOutcome.Marginal;
        RadioAir.Begin(new Over(pawn, null, null, OverKind.Help, now, now + rules.urgentOverTicks, faint, stations));
        Advance(pawn, call, map, net, rules, now);
    }

    /// <summary>
    /// A call on the air: heard once the hand-held has a channel (the urgent kind takes one from chatter at the net's next pass); with every
    /// channel held by kinds it cannot take from, it waits, and the Log says so; given up when its over ends unserved.
    /// </summary>
    private static void Advance(Pawn pawn, HelpCall call, Map map, RadioNet net, RadioRulesDef rules, int now)
    {
        string name = RadioOperators.Name(pawn);
        if (!RadioAir.Urgent(pawn))
        {
            calling.Remove(pawn);
            nextCall[pawn] = (now + rules.helpRetryTicks, call.Trouble);
            RadioLog.Add(RadioLogKind.Urgent, "PhobosRadioUrgent_NoChannelGaveUp".Translate(name.Named("CALLER")).Resolve());
            return;
        }
        bool served = net.ServedBy(pawn) != null || (!net.IsBusy(pawn) && RadioAdvice.InTouch(pawn));
        if (!served)
        {
            if (call.Waiting) return;
            call.Waiting = true;
            RadioLog.Add(RadioLogKind.Urgent, "PhobosRadioUrgent_Waiting".Translate(name.Named("CALLER")).Resolve());
            if (Seen.Wants("radio.urgent-waiting", pawn)) Seen.Note("radio.urgent-waiting", pawn);
            return;
        }
        calling.Remove(pawn);
        nextCall[pawn] = (now + rules.helpRetryTicks, call.Trouble);
        string what = TroubleText(call.Trouble);
        Pawn? listener = RadioWatch.Hear(map, name, what, "help", notThis: pawn);
        if (listener == null) return;
        string heard = "PhobosRadioUrgent_HelpHeard".Translate(name.Named("CALLER"), what.Named("WHAT"), listener.LabelShort.Named("LISTENER")).Resolve();
        Messages.Message(heard, new LookTargets(pawn), MessageTypeDefOf.NegativeHealthEvent, historical: false);
        RadioLog.Add(RadioLogKind.Urgent, heard);
        string variant = call.Trouble.ToString().ToLowerInvariant();
        if (Seen.Wants("radio.help-call", pawn, variant)) Seen.Note("radio.help-call", pawn, "heard by " + listener.LabelShort, variant);
        if (call.Trouble >= HelpTrouble.Bleeding && RadioMod.Settings.helpCallsSendHelp && RadioStandingOrders.SendsHelp(map)) SendHelp(pawn, map);
    }

    private static string TroubleText(HelpTrouble trouble) => trouble switch
    {
        HelpTrouble.Downed => "PhobosRadioUrgent_TroubleDowned".Translate(),
        HelpTrouble.Bleeding => "PhobosRadioUrgent_TroubleBleeding".Translate(),
        _ => "PhobosRadioUrgent_TroubleAttacked".Translate(),
    };

    /// <summary>
    /// The standing order "send help" (RN16): the nearest colonist who can doctor, not drafted and free to be ordered, carries a downed caller to
    /// a bed (the game's own rescue) or tends a bleeding one with the best medicine to hand (the game's own tending). Nobody is sent to someone
    /// already being helped.
    /// </summary>
    private static void SendHelp(Pawn caller, Map map)
    {
        if (caller.InBed() || map.reservationManager.IsReservedByAnyoneOf(caller, Faction.OfPlayer)) return;
        Pawn? rescuer = SendNearest.Find(caller, p => p != caller && !p.InBed() && !p.WorkTypeIsDisabled(WorkTypeDefOf.Doctor)
            && p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && FreeToSend(p));
        string name = RadioOperators.Name(caller);
        if (rescuer == null)
        {
            RadioLog.Add(RadioLogKind.Urgent, "PhobosRadioUrgent_NobodyToSend".Translate(name.Named("CALLER")).Resolve());
            return;
        }
        Job? job = null;
        string how = "tend";
        if (caller.Downed && RestUtility.FindBedFor(caller, rescuer, checkSocialProperness: false, ignoreOtherReservations: false, caller.GuestStatus) is { } bed
            && rescuer.CanReserve(bed))
        {
            job = JobMaker.MakeJob(JobDefOf.Rescue, caller, bed);
            job.count = 1;
            how = "rescue";
        }
        else if (caller.health.HasHediffsNeedingTend())
        {
            Thing? medicine = HealthAIUtility.FindBestMedicine(rescuer, caller);
            job = medicine == null ? JobMaker.MakeJob(JobDefOf.TendPatient, caller)
                : medicine.SpawnedParentOrMe != medicine ? JobMaker.MakeJob(JobDefOf.TendPatient, caller, medicine, medicine.SpawnedParentOrMe)
                : JobMaker.MakeJob(JobDefOf.TendPatient, caller, medicine);
        }
        if (job == null || !SendNearest.Order(rescuer, job))
        {
            RadioLog.Add(RadioLogKind.Urgent, "PhobosRadioUrgent_NobodyToSend".Translate(name.Named("CALLER")).Resolve());
            return;
        }
        string text = (how == "rescue" ? "PhobosRadioUrgent_SentRescue" : "PhobosRadioUrgent_SentTend")
            .Translate(rescuer.LabelShort.Named("RESCUER"), name.Named("CALLER")).Resolve();
        Messages.Message(text, new LookTargets(rescuer, caller), MessageTypeDefOf.NeutralEvent, historical: false);
        RadioLog.Add(RadioLogKind.Urgent, text);
        if (Seen.Wants("radio.help-sent", rescuer, how)) Seen.Note("radio.help-sent", rescuer, caller.LabelShort, how);
    }

    /// <summary>Not doing something the player ordered or an emergency (a fire, another rescue): those are not put aside.</summary>
    private static bool FreeToSend(Pawn pawn) =>
        pawn.CurJob is not { } job || !job.playerForced && job.workGiverDef is not { emergency: true } && job.def != JobDefOf.Rescue
            && job.def != JobDefOf.TendPatient;

    // ---- RN17: sighting reports ----

    private static void LookForThreats(Map map, RadioNet net, RadioRulesDef rules, int now)
    {
        if (!reported.TryGetValue(map.uniqueID, out var done)) reported[map.uniqueID] = done = new HashSet<string>();
        var hostile = map.attackTargetsCache.TargetsHostileToColony;
        if (hostile.Count == 0)
        {
            done.Clear();
            return;
        }
        groups.Clear();
        foreach (IAttackTarget target in hostile)
        {
            if (target.Thing is not Pawn pawn || !pawn.Spawned || pawn.Downed || !GenHostility.IsActiveThreatToPlayer(target)) continue;
            string key = GroupKey(pawn);
            if (!groups.TryGetValue(key, out var members)) groups[key] = members = new List<Pawn>();
            members.Add(pawn);
        }
        done.RemoveWhere(key => !groups.ContainsKey(key));
        bool looked = false;
        foreach (var group in groups)
        {
            if (done.Contains(group.Key)) continue;
            if (!looked)
            {
                Reporters(map, net);
                looked = true;
            }
            if (reporters.Count == 0) return;
            if (FirstToSee(group.Value, map, rules) is not { } reporter) continue;
            done.Add(group.Key);
            Report(reporter, group.Value, map, net, rules, now);
        }
    }

    /// <summary>One group per raid (the game's own grouping of raiders), one for every animal gone manhunting, else one for each threat.</summary>
    private static string GroupKey(Pawn pawn)
    {
        if (pawn.GetLord() is { } lord) return "lord " + lord.loadID;
        if (pawn.MentalStateDef == MentalStateDefOf.Manhunter || pawn.MentalStateDef == MentalStateDefOf.ManhunterPermanent) return "manhunters";
        return "pawn " + pawn.thingIDNumber;
    }

    /// <summary>The colonists who could report a sighting now: awake, able to talk, in touch on a hand-held.</summary>
    private static void Reporters(Map map, RadioNet net)
    {
        reporters.Clear();
        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn.Downed || !pawn.Awake() || pawn.InMentalState || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking)
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight)) continue;
            if (net.IsBusy(pawn) || RadioAdvice.InTouch(pawn)) reporters.Add(pawn);
        }
    }

    /// <summary>The first colonist who has a member of the group in sight within the rules' cells: a distance first, a line of sight only then.</summary>
    private static Pawn? FirstToSee(List<Pawn> members, Map map, RadioRulesDef rules)
    {
        int range = rules.sightCells * rules.sightCells;
        int looked = 0;
        foreach (Pawn member in members)
        {
            if (++looked > rules.sightMembersLooked) break;
            foreach (Pawn reporter in reporters)
            {
                if ((reporter.Position - member.Position).LengthHorizontalSquared > range) continue;
                if (GenSight.LineOfSight(reporter.Position, member.Position, map, skipFirstCell: true)) return reporter;
            }
        }
        return null;
    }

    private static void Report(Pawn reporter, List<Pawn> members, Map map, RadioNet net, RadioRulesDef rules, int now)
    {
        Pawn first = members[0];
        int count = members.Count;
        bool animals = first.RaceProps.Animal;
        string what = animals
            ? "PhobosRadioUrgent_SightAnimals".Translate(count.Named("COUNT"), GenLabel.BestKindLabel(first, plural: count > 1, pluralCount: count).Named("KIND")).Resolve()
            : first.Faction != null
                ? "PhobosRadioUrgent_SightRaiders".Translate(count.Named("COUNT"), first.Faction.Name.Named("FACTION")).Resolve()
                : "PhobosRadioUrgent_SightOthers".Translate(count.Named("COUNT"), GenLabel.BestKindLabel(first, plural: count > 1, pluralCount: count).Named("KIND")).Resolve();
        float x = 0f, z = 0f;
        foreach (Pawn member in members)
        {
            x += member.Position.x;
            z += member.Position.z;
        }
        IntVec3 middle = map.Center;
        string side = RadarWarning.CompassOf((float)SightingRule.Bearing(x / count - middle.x, z / count - middle.z));
        var stations = new List<Thing>(1);
        if (net.ServedBy(reporter) is { } station) stations.Add(station);
        bool faint = net.TryGetBest(reporter, out var link) && link?.Outcome == LinkOutcome.Marginal;
        RadioAir.Begin(new Over(reporter, null, null, OverKind.Sighting, now, now + rules.urgentOverTicks, faint, stations));
        string text = "PhobosRadioUrgent_Sighting".Translate(RadioOperators.Name(reporter).Named("REPORTER"), what.Named("WHAT"), side.Named("SIDE")).Resolve();
        Messages.Message(text, new LookTargets(reporter, first), MessageTypeDefOf.ThreatSmall, historical: false);
        RadioLog.Add(RadioLogKind.Urgent, text);
        string variant = animals ? "animals" : first.Faction != null ? "raiders" : "others";
        if (Seen.Wants("radio.sighting", reporter, variant))
            Seen.Note("radio.sighting", reporter, what, variant, new[] { new KeyValuePair<string, double>("count", count) });
        if (RadioMod.Settings.sightingsTakeCover && RadioStandingOrders.PassesSightings(map)) TakeCover(map, net, rules);
    }

    /// <summary>
    /// The standing order "pass sightings to everyone" (RN17): each colonist in touch who does not fight, outdoors, not drafted and free to be
    /// interrupted, goes to the nearest room indoors and stays a while (the take-cover job).
    /// </summary>
    private static void TakeCover(Map map, RadioNet net, RadioRulesDef rules)
    {
        int sent = 0;
        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn.Drafted || pawn.Downed || !pawn.Awake() || pawn.InMentalState || !NonFighter(pawn)) continue;
            if (pawn.CurJobDef == RadioUrgentDefOf.PhobosRadio_TakeCover || !RadioAdvice.Interruptible(pawn)) continue;
            if (!(net.IsBusy(pawn) || RadioAdvice.InTouch(pawn))) continue;
            if (pawn.GetRoom() is { PsychologicallyOutdoors: false }) continue;
            if (Shelter(pawn, map, rules.coverRegions) is not { } cell) continue;
            Job job = JobMaker.MakeJob(RadioUrgentDefOf.PhobosRadio_TakeCover, cell);
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false);
            sent++;
        }
        if (sent == 0) return;
        RadioLog.Add(RadioLogKind.Urgent, "PhobosRadioUrgent_TookCover".Translate(sent.Named("COUNT")).Resolve());
        if (Seen.Wants("radio.take-cover", null)) Seen.Note("radio.take-cover", null, sent + " colonists", null, new[] { new KeyValuePair<string, double>("count", sent) });
    }

    /// <summary>Who does not fight [A]: one who cannot do violence, or carries no weapon.</summary>
    public static bool NonFighter(Pawn pawn) => pawn.WorkTagIsDisabled(WorkTags.Violent) || pawn.equipment?.Primary == null;

    /// <summary>The nearest room indoors the colonist can reach, searched over at most <paramref name="maxRegions"/> regions: a cell in it, or null.</summary>
    private static IntVec3? Shelter(Pawn pawn, Map map, int maxRegions)
    {
        Region? start = pawn.GetRegion();
        if (start == null) return null;
        IntVec3? found = null;
        TraverseParms parms = TraverseParms.For(pawn);
        RegionTraverser.BreadthFirstTraverse(start, (from, to) => to.Allows(parms, isDestination: false), region =>
        {
            if (region.IsDoorway || region.Room is not { PsychologicallyOutdoors: false, IsPrisonCell: false } room || room.Fogged) return false;
            if (!region.TryFindRandomCellInRegion(c => c.Standable(map) && !c.IsForbidden(pawn), out IntVec3 cell)) return false;
            found = cell;
            return true;
        }, maxRegions);
        return found is { } at && pawn.CanReach(at, PathEndMode.OnCell, Danger.Deadly) ? at : null;
    }

    // ---- Dev actions ----

    /// <summary>
    /// Dev action: this colonist calls for help now, alone or not, about the trouble they are in (down them or cut them first to see help
    /// sent); in no trouble, as under attack, which sends nobody. Why not, or null.
    /// </summary>
    public static string? CallNow(Pawn pawn)
    {
        if (!pawn.Spawned || RadioNet.For(pawn.Map) is not { } net) return "not on a map";
        if (!CanCall(pawn, net)) return "no hand-held in touch, asleep or unable to talk";
        calling.Remove(pawn);
        nextCall.Remove(pawn);
        HelpTrouble trouble = TroubleOf(pawn, RadioRulesDef.Current, Find.TickManager.TicksGame);
        StartCall(pawn, trouble == HelpTrouble.None ? HelpTrouble.Attacked : trouble, pawn.Map, net, RadioRulesDef.Current, Find.TickManager.TicksGame);
        return null;
    }

    /// <summary>Dev action: forget the sightings already reported on this map, and look again now.</summary>
    public static void SightingsAgain(Map map)
    {
        reported.Remove(map.uniqueID);
        if (RadioNet.For(map) is { } net) LookForThreats(map, net, RadioRulesDef.Current, Find.TickManager.TicksGame);
    }
}

[DefOf]
public static class RadioUrgentDefOf
{
    public static JobDef PhobosRadio_TakeCover = null!;

    static RadioUrgentDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioUrgentDefOf));
}
