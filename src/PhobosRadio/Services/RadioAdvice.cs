using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Skills;
using Phobos.Radio.Physics;
using Phobos.Radio.Work;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Phobos.Radio.Services;

// Advice by radio (Radio 0.23.0; docs/development/radio-advice-and-relays.md, the owner's cards of 26 Nov 2026). A colonist doing a job a
// RadioAdviceDef names is talked through it by a more skilled colonist in radio touch: half the skill gap on a workable or clean link, a
// quarter on a marginal one (AdviceRule). What the lift does to the work is D'thorga Framework's skill lift (Phobos.Framework.Skills); this
// file finds the adviser, keeps the calls and books the adviser's time. Nothing here is saved: a call is worked out again after a load.
// Since Radio 0.26.0 (the owner's cards of 27 Nov 2026) the carer learns the skill half again as fast while a call is live (the Elmer), the
// adviser gains a little Social, and when no colonist can help, another faction's doctor may (FactionDoctors, whose terms are saved).

/// <summary>Where the adviser takes the call: in bed on a hand-held, standing where they are with one, or at a radio station's desk.</summary>
public enum AdvicePlace
{
    InBed,
    InPlace,
    AtDesk,
    /// <summary>At another faction's settlement (Radio 0.26.0): their doctor, never on the player's maps.</summary>
    Afar,
}

/// <summary>A home a person off the map reaches by radio: its map, the link's margin, and what the person reaches it through.</summary>
public readonly record struct RemoteHome(double MarginDb, Map Home, bool Morse = false);

/// <summary>
/// Something that can say which colonies a person off the map reaches by radio: the Vehicle Framework tie-in, for convoys. Since Radio
/// 0.24.1 a lazy list, the colony reached first before the others, so a caller that stops at the first that helps asks no more.
/// </summary>
public interface IRemoteReachSource
{
    IEnumerable<RemoteHome> Homes(Pawn pawn);

    /// <summary>Whether a map with no station of the player's still has a way home for those on it (Radio 0.25.0: a cross-band vehicle).</summary>
    bool Serves(Map map) => false;

    /// <summary>
    /// This person's own end between world tiles, for a call that searches its own chain (Radio 0.26.0: another faction's doctor), and the
    /// margin of any hop before it (a hand-held to a cross-band vehicle); null with none.
    /// </summary>
    (RelayNode End, double HopMarginDb)? WorldEnd(Pawn pawn) => null;
}

/// <summary>Where a person off the map reaches home by radio, when a tie-in can say (Radio 0.23.0): a convoy's reach home.</summary>
public static class RemoteReach
{
    private static IRemoteReachSource? source;

    public static void Register(IRemoteReachSource reachSource) => source = reachSource;

    /// <summary>The colonies this person reaches, the first reached first; asked one by one as the caller goes on.</summary>
    public static IEnumerable<RemoteHome> Of(Pawn pawn) => source?.Homes(pawn) ?? Enumerable.Empty<RemoteHome>();

    /// <summary>Whether people on this map may reach home through a tie-in, with no station of their own (Radio 0.25.0).</summary>
    public static bool Serves(Map map) => source?.Serves(map) == true;

    /// <summary>This person's end between world tiles, when a tie-in can say (Radio 0.26.0).</summary>
    public static (RelayNode End, double HopMarginDb)? WorldEndOf(Pawn pawn) => source?.WorldEnd(pawn);
}

/// <summary>One adviser found for one carer, and what the call would lift them to.</summary>
public sealed class AdviceOffer
{
    public AdviceOffer(Pawn carer, Pawn adviser, RadioAdviceDef def, int level, LinkOutcome outcome, AdvicePlace place, Thing? station, string? aid,
        Faction? faction = null, Settlement? settlement = null, double marginDb = 0.0, bool picture = false)
    {
        Picture = picture;
        Faction = faction;
        Settlement = settlement;
        MarginDb = marginDb;
        Carer = carer;
        Adviser = adviser;
        Def = def;
        Level = level;
        Outcome = outcome;
        Place = place;
        Station = station;
        Aid = aid != null;
        Reason = faction != null
            ? (aid != null ? "PhobosRadioAdvice_ReasonFactionAid" : "PhobosRadioAdvice_ReasonFaction").Translate(adviser.LabelShort.Named("ADVISER"),
                faction.Name.Named("FACTION"), def.skill.label.Named("SKILL"), SkillLifts.OwnLevel(adviser, def.skill).Named("LEVEL"),
                (aid ?? "").Named("AID")).Resolve()
            : (aid != null ? "PhobosRadioAdvice_ReasonAid" : "PhobosRadioAdvice_Reason").Translate(adviser.LabelShort.Named("ADVISER"),
                def.skill.label.Named("SKILL"), SkillLifts.OwnLevel(adviser, def.skill).Named("LEVEL"), (aid ?? "").Named("AID"))
                .Resolve();
    }

    /// <summary>Whether the slow-scan camera kit's picture helps this call (Radio 0.39.0).</summary>
    public bool Picture { get; }
    /// <summary>Another faction whose doctor this is (Radio 0.26.0), or null for a colonist.</summary>
    public Faction? Faction { get; }
    /// <summary>The settlement that faction's doctor answers from, and the link's margin (Radio 0.38.0: its QSL card).</summary>
    public Settlement? Settlement { get; }
    public double MarginDb { get; }
    /// <summary>The whole link to that settlement (Radio 0.62.0): its band, mode and relays, for the QSL card.</summary>
    public FactionDoctorLink? Link { get; init; }

    public Pawn Carer { get; }
    public Pawn Adviser { get; }
    public RadioAdviceDef Def { get; }
    public int Level { get; }
    public LinkOutcome Outcome { get; }
    public AdvicePlace Place { get; }
    public Thing? Station { get; }
    public bool Aid { get; }
    /// <summary>One line naming who helps, for stat explanations and buttons.</summary>
    public string Reason { get; }
}

/// <summary>A call in progress for a job on a map: live once the adviser is on the radio.</summary>
public sealed class AdviceCall
{
    public AdviceCall(AdviceOffer offer) => Offer = offer;

    public AdviceOffer Offer { get; }
    public bool Live { get; set; }
}

/// <summary>
/// Advice by radio: who can talk whom through what, the calls in progress, and D'thorga Framework's skill-lift source.
/// <para>Cost: nothing each tick. Every <see cref="RadioRulesDef.adviceLookTicks"/> (60) the colonists on maps with a station are looked at: a
/// set lookup on each one's job, and a search of the colony for an adviser only for one doing a job a Def names, at most once every
/// <see cref="RadioRulesDef.adviceRetryTicks"/> (250) each. A search reads the radio net's last pass; it never works a link out. A stat read
/// of a lifted stat costs two dictionary lookups, and for a person off the map an offer kept for 250 ticks.</para>
/// </summary>
public static class RadioAdvice
{
    private static readonly List<RadioAdviceDef> defs = new();
    private static readonly Dictionary<JobDef, List<RadioAdviceDef>> byJob = new();
    private static readonly Dictionary<string, List<RadioAdviceDef>> byAct = new();
    private static readonly Dictionary<Pawn, AdviceCall> calls = new();
    private static readonly Dictionary<Pawn, AdviceCall> byAdviser = new();
    private static readonly Dictionary<Pawn, int> nextTry = new();
    private static readonly Dictionary<(Pawn, RadioAdviceDef), (int tick, AdviceOffer? offer)> offers = new();
    private static readonly Dictionary<(Pawn, RadioAdviceDef), int> lastSaid = new();
    private static AdviceOffer? tradeShown;
    private static bool ready;

    /// <summary>A day between two messages about the same carer and act on the road (agent default), so a convoy's tends do not flood.</summary>
    private const int SayAgainTicks = GenDate.TicksPerDay;

    public static readonly ISkillLiftSource Source = new AdviceLiftSource();

    /// <summary>Indexes the Defs, registers with D'thorga Framework and lifts the Defs' stats. Once, at start-up.</summary>
    public static void Init()
    {
        if (ready) return;
        ready = true;
        foreach (RadioAdviceDef def in DefDatabase<RadioAdviceDef>.AllDefsListForReading)
        {
            defs.Add(def);
            foreach (JobDef job in def.jobs)
            {
                if (!byJob.TryGetValue(job, out var list)) byJob[job] = list = new List<RadioAdviceDef>();
                list.Add(def);
            }
            if (!byAct.TryGetValue(def.act, out var acts)) byAct[def.act] = acts = new List<RadioAdviceDef>();
            acts.Add(def);
            foreach (StatDef stat in def.liftedStats) SkillLifts.LiftStat(stat, def.skill, def.act, slows: false);
            foreach (StatDef stat in def.slowedStats) SkillLifts.LiftStat(stat, def.skill, def.act, slows: true);
        }
        if (defs.Count > 0) SkillLifts.Register(Source);
    }

    public static IReadOnlyList<RadioAdviceDef> Defs => defs;

    /// <summary>Whether a kind of advice is on: always, unless it has its own switch and the player has turned it off.</summary>
    public static bool Enabled(RadioAdviceDef def) => !def.ownSetting || !RadioMod.Settings.adviceOff.Contains(def.defName);

    /// <summary>The call a carer is on, or null.</summary>
    public static AdviceCall? CallOf(Pawn carer) => calls.TryGetValue(carer, out var call) ? call : null;

    /// <summary>The call an adviser is giving, or null.</summary>
    public static AdviceCall? CallBy(Pawn adviser) => byAdviser.TryGetValue(adviser, out var call) ? call : null;

    public static void Clear()
    {
        calls.Clear();
        byAdviser.Clear();
        nextTry.Clear();
        offers.Clear();
        lastSaid.Clear();
        tradeShown = null;
        SkillLifts.ClearTeaching();
        FactionDoctors.Clear();
    }

    // ---- Finding an adviser ----

    /// <summary>
    /// The best adviser for a carer now, or null: free, more skilled, with the work switched on, and in radio touch with the carer: both
    /// served by the same map's stations (the adviser's hand-held, or the adviser at a station's desk), or, for a carer off the map, the
    /// convoy's reach home and the adviser's radio there. The worse of the two links decides the share. Changes nothing.
    /// </summary>
    public static AdviceOffer? Seek(Pawn carer, RadioAdviceDef def)
    {
        if (!Enabled(def) || carer.skills == null) return null;
        var rules = RadioRulesDef.Current;
        if (carer.Spawned)
        {
            var net = RadioNet.For(carer.Map);
            if (net.ServedBy(carer) != null && net.TryGetBest(carer, out var link) && link != null)
            {
                Map map = carer.Map;
                LinkOutcome own = link.Budget.Outcome;
                return SeekAt(carer, def, map, own) ?? SeekFaction(carer, def, map, map.Tile, () => FactionDoctors.StationEnds(map), own);
            }
            // Radio 0.25.0: no station of theirs serves them (a camp, a site): through a cross-band vehicle on the map, as from a convoy.
        }
        // Off the map: the colony the convoy reaches first, then (Radio 0.24.1) each other colony it reaches, through a repeater station
        // among the relays, until one has someone who can help. The others are asked only when the first could not help.
        foreach (RemoteHome remote in RemoteReach.Of(carer))
        {
            // Radio 0.40.0: by Morse, advice is held to a marginal link's share (the owner's card: a quarter).
            LinkOutcome outcome = rules.MarginBands.Classify(remote.MarginDb);
            if (remote.Morse) outcome = AdviceRule.Worse(outcome, LinkOutcome.Marginal);
            if (SeekAt(carer, def, remote.Home, outcome) is { } offer) return offer;
        }
        // Radio 0.26.0: nobody at any colony in reach can help: another faction's doctor, from the convoy's radio or the cross-band vehicle.
        if (def.fromFactions && FactionDoctors.On && RemoteReach.WorldEndOf(carer) is { } end)
        {
            LinkOutcome hop = double.IsPositiveInfinity(end.HopMarginDb) ? LinkOutcome.Clean : rules.MarginBands.Classify(end.HopMarginDb);
            return SeekFaction(carer, def, end.End.Owner, end.End.Tile, () => new[] { end.End }, hop);
        }
        return null;
    }

    /// <summary>
    /// Another faction's doctor for a carer no colonist can help (Radio 0.26.0), or null: a settlement in reach of <paramref name="origin"/>'s
    /// radio whose faction would help now, a doctor made for it, free of another call, who knows more than the carer.
    /// </summary>
    private static AdviceOffer? SeekFaction(Pawn carer, RadioAdviceDef def, object origin, PlanetTile tile, System.Func<IReadOnlyList<RelayNode>> froms,
        LinkOutcome carerOutcome)
    {
        if (!def.fromFactions || !FactionDoctors.On || carerOutcome == LinkOutcome.NoContact) return null;
        if (FactionDoctors.Reach(origin, tile, froms) is not { } link || FactionDoctors.HelperOf(link.Faction, def.skill) is not { } doctor) return null;
        if (byAdviser.ContainsKey(doctor)) return null;
        var rules = RadioRulesDef.Current;
        int own = SkillLifts.OwnLevel(carer, def.skill);
        var outcome = AdviceRule.Worse(carerOutcome, rules.MarginBands.Classify(link.MarginDb));
        double aidShare = AidShare(carer, def, outcome, out string? aid, out bool picture);
        int level = AdviceRule.EffectiveLevel(own, SkillLifts.OwnLevel(doctor, def.skill),
            AdviceRule.Share(outcome, rules.adviceShareSolid, rules.adviceShareMarginal, aidShare));
        return level <= own ? null : new AdviceOffer(carer, doctor, def, level, outcome, AdvicePlace.Afar, null, aid, link.Faction, link.Settlement, link.MarginDb,
            picture) { Link = link };
    }

    /// <summary>The best adviser at one colony for a carer whose own link there has this outcome, or null.</summary>
    private static AdviceOffer? SeekAt(Pawn carer, RadioAdviceDef def, Map home, LinkOutcome carerOutcome)
    {
        var rules = RadioRulesDef.Current;
        if (carerOutcome == LinkOutcome.NoContact) return null;
        int own = SkillLifts.OwnLevel(carer, def.skill);
        // At most what every aid at hand could add on a clean link, for the first look over the colony (Radio 0.39.0: the picture too).
        double mostShare = AidShare(carer, def, LinkOutcome.Clean, out _, out _);
        var homeNet = RadioNet.For(home);
        AdviceOffer? best = null;
        float bestDistance = float.MaxValue;
        foreach (Pawn adviser in home.mapPawns.FreeColonistsSpawned)
        {
            if (adviser == carer || !Eligible(adviser, def)) continue;
            int theirs = SkillLifts.OwnLevel(adviser, def.skill);
            if (AdviceRule.EffectiveLevel(own, theirs, AdviceRule.Share(LinkOutcome.Clean, rules.adviceShareSolid, rules.adviceShareMarginal, mostShare)) <= own) continue;
            AdvicePlace place;
            Thing? station = null;
            LinkOutcome theirOutcome;
            float distance = 0f;
            if (homeNet.ServedBy(adviser) != null && homeNet.TryGetBest(adviser, out var theirLink) && theirLink != null)
            {
                theirOutcome = theirLink.Budget.Outcome;
                place = adviser.InBed() ? AdvicePlace.InBed : AdvicePlace.InPlace;
            }
            else if (!def.trade && !adviser.InBed() && NearestDesk(adviser, homeNet) is { } desk)
            {
                // At the desk, the station itself carries the call: the adviser's end is as good as the station's.
                station = desk;
                theirOutcome = LinkOutcome.Clean;
                place = AdvicePlace.AtDesk;
                distance = adviser.Position.DistanceTo(desk.Position);
            }
            else continue;
            var outcome = AdviceRule.Worse(carerOutcome, theirOutcome);
            double aidShare = AidShare(carer, def, outcome, out string? aid, out bool picture);
            int level = AdviceRule.EffectiveLevel(own, theirs, AdviceRule.Share(outcome, rules.adviceShareSolid, rules.adviceShareMarginal, aidShare));
            if (level <= own) continue;
            if (best != null && (level < best.Level || level == best.Level && distance >= bestDistance)) continue;
            best = new AdviceOffer(carer, adviser, def, level, outcome, place, station, aid, picture: picture);
            bestDistance = distance;
        }
        return best;
    }

    /// <summary>An offer for a carer and act, kept a short while (an act with no job asks many times in a moment).</summary>
    private static AdviceOffer? Offer(Pawn carer, RadioAdviceDef def)
    {
        int now = Find.TickManager.TicksGame;
        if (offers.TryGetValue((carer, def), out var held) && now - held.tick < RadioRulesDef.Current.adviceOfferKeepTicks) return held.offer;
        if (offers.Count > 256) offers.Clear();
        AdviceOffer? offer = Seek(carer, def);
        offers[(carer, def)] = (now, offer);
        return offer;
    }

    /// <summary>Whether a colonist can take a call now: awake, able to talk and hear, not drafted or in a lord's duty, with the work switched
    /// on, on no other call, and doing nothing more urgent (in bed counts as free: the call needs no job there).</summary>
    public static bool Eligible(Pawn adviser, RadioAdviceDef def)
    {
        if (adviser.Downed || !adviser.Awake() || adviser.InMentalState || adviser.Drafted || adviser.GetLord() != null) return false;
        if (byAdviser.ContainsKey(adviser) || calls.ContainsKey(adviser)) return false;
        if (!adviser.health.capacities.CapableOf(PawnCapacityDefOf.Talking) || !adviser.health.capacities.CapableOf(PawnCapacityDefOf.Hearing)) return false;
        if (def.workType != null)
        {
            if (adviser.WorkTypeIsDisabled(def.workType) || adviser.workSettings == null || !adviser.workSettings.WorkIsActive(def.workType)) return false;
        }
        else if (adviser.WorkTagIsDisabled(WorkTags.Social)) return false;
        return adviser.InBed() || Interruptible(adviser);
    }

    /// <summary>Whether a colonist's job may give way to a call: nothing ordered by the player, no emergency, no advice or advisable work of
    /// their own, and a job the game lets be interrupted.</summary>
    public static bool Interruptible(Pawn pawn)
    {
        Job? job = pawn.CurJob;
        if (job == null) return true;
        if (job.playerForced || job.def == RadioAdviceDefOf.PhobosRadio_AdviseByRadio || job.def == RadioAdviceDefOf.PhobosRadio_AdviseByRadioBooked) return false;
        if (byJob.ContainsKey(job.def) || job.workGiverDef is { emergency: true }) return false;
        if (job.def == JobDefOf.Ingest || job.def == JobDefOf.LayDown) return false;
        return job.def.casualInterruptible;
    }

    /// <summary>
    /// The nearest powered station's desk the adviser can walk to, or null. Since Radio 0.51.0 only one with a seat free for them and a
    /// channel free for one more call.
    /// </summary>
    public static Thing? NearestDesk(Pawn adviser, RadioNet net)
    {
        Thing? nearest = null;
        float best = float.MaxValue;
        foreach (CompRadio station in net.Stations)
        {
            Thing desk = station.parent;
            if (!desk.Spawned || desk.Map != adviser.Map || desk.TryGetComp<CompPowerTrader>() is { PowerOn: false }) continue;
            float distance = adviser.Position.DistanceToSquared(desk.Position);
            if (distance >= best || desk.IsForbidden(adviser) || !adviser.CanReach(desk, PathEndMode.Touch, Danger.Some)) continue;
            if (!RadioSeats.CanSit(adviser, desk) || !net.DeskHasRoom(desk)) continue;
            best = distance;
            nearest = desk;
        }
        return nearest;
    }

    /// <summary>
    /// What the aids at hand add to the share on this link, and what they are called: the numbered medical chest whatever the link, and
    /// (Radio 0.39.0; the owner's card of 27 Nov 2026, "A sixth, on a clean link") the slow-scan camera kit only on a clean link, as noise
    /// ruins a picture long before it ruins speech.
    /// </summary>
    public static double AidShare(Pawn carer, RadioAdviceDef def, LinkOutcome outcome, out string? label, out bool picture)
    {
        double share = 0.0;
        var labels = new List<string>();
        picture = false;
        if (def.aid != null && HasAid(carer, def.aid))
        {
            share += def.aidShare;
            labels.Add(def.aid.label);
        }
        if (def.pictureAid != null && outcome == LinkOutcome.Clean && HasAid(carer, def.pictureAid))
        {
            share += def.pictureShare;
            labels.Add(def.pictureAid.label);
            picture = true;
        }
        label = labels.Count > 0 ? labels.ToCommaList(useAnd: true) : null;
        return share;
    }

    /// <summary>Whether an aid (the numbered medical chest, the camera kit) is at hand: carried, in the convoy, or near the carer or in their room.</summary>
    public static bool HasAid(Pawn carer, ThingDef aid)
    {
        if (carer.inventory?.innerContainer is { } carried)
        {
            foreach (Thing thing in carried) if (thing.def == aid) return true;
        }
        if (carer.GetCaravan() is { } convoy)
        {
            foreach (Thing thing in CaravanInventoryUtility.AllInventoryItems(convoy)) if (thing.def == aid) return true;
            return false;
        }
        if (!carer.Spawned) return false;
        Room? room = carer.GetRoom();
        foreach (Thing thing in carer.Map.listerThings.ThingsOfDef(aid))
        {
            if (thing.Position.InHorDistOf(carer.Position, 8f)) return true;
            if (room != null && !room.PsychologicallyOutdoors && thing.GetRoom() == room) return true;
        }
        return false;
    }

    /// <summary>Whether a job is one this Def advises on.</summary>
    public static bool Matches(Job? job, RadioAdviceDef def) =>
        job != null && def.jobs.Contains(job.def) && (!def.medicalBillsOnly || job.bill is Bill_Medical);

    // ---- Calls for jobs on a map ----

    /// <summary>Looks at the colony: ends calls no longer wanted, and opens calls for colonists newly at work a Def names.</summary>
    public static void Look()
    {
        if (!ready || defs.Count == 0) return;
        int now = Find.TickManager.TicksGame;
        FactionDoctors.MakeWanted();
        var teach = RadioRulesDef.Current;
        foreach (AdviceCall call in calls.Values.ToList())
        {
            if (Ends(call) is { } dropped)
            {
                End(call, dropped);
                continue;
            }
            if (!call.Live) continue;
            Pawn adviser = call.Offer.Adviser;
            // Radio 0.26.0: the adviser gains a little Social for the call (the owner's card); another faction's call stays open while used.
            if (call.Offer.Faction is { } faction) FactionDoctors.Using(faction);
            else if (adviser.Spawned && adviser.skills != null && teach.adviceTeachSocialPerTick > 0f)
            {
                adviser.skills.Learn(SkillDefOf.Social, teach.adviceTeachSocialPerTick * teach.adviceLookTicks);
                if (Seen.Wants("radio.elmer-teacher", adviser)) Seen.Note("radio.elmer-teacher", adviser, call.Offer.Carer.LabelShort);
            }
        }
        if (byJob.Count == 0) return;
        var rules = RadioRulesDef.Current;
        foreach (Map map in Find.Maps)
        {
            // Radio 0.25.0: a map with no station is looked at too when a cross-band vehicle there gives its people a way home.
            if (RadioNet.For(map).Stations.Count == 0 && !RemoteReach.Serves(map)) continue;
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                // After a load: an adviser still on a call nothing keeps steps away from the radio; the call is opened again below.
                if (pawn.jobs?.curDriver is JobDriver_AdviseByRadio { Booked: false } driver && !byAdviser.ContainsKey(pawn))
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    continue;
                }
                Job? job = pawn.CurJob;
                if (job == null || !byJob.TryGetValue(job.def, out var forJob) || calls.ContainsKey(pawn)) continue;
                if (nextTry.TryGetValue(pawn, out var at) && at > now) continue;
                nextTry[pawn] = now + rules.adviceRetryTicks;
                foreach (RadioAdviceDef def in forJob)
                {
                    if (!Matches(job, def) || Seek(pawn, def) is not { } offer) continue;
                    Open(offer);
                    break;
                }
            }
        }
        if (nextTry.Count > 512) nextTry.Clear();
    }

    /// <summary>Opens a call: an adviser in bed is on it at once; one on their feet is given the call as a job, and is on it once at the radio.</summary>
    public static void Open(AdviceOffer offer)
    {
        var call = new AdviceCall(offer);
        calls[offer.Carer] = call;
        byAdviser[offer.Adviser] = call;
        if (offer.Place is AdvicePlace.InBed or AdvicePlace.Afar)
        {
            GoLive(call);
            return;
        }
        Job job = JobMaker.MakeJob(RadioAdviceDefOf.PhobosRadio_AdviseByRadio, offer.Carer, offer.Station == null ? LocalTargetInfo.Invalid : offer.Station);
        offer.Adviser.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
    }

    /// <summary>The adviser has reached the radio: the call is live. From the call's job.</summary>
    public static void Arrived(Pawn adviser, Pawn carer)
    {
        if (calls.TryGetValue(carer, out var call) && call.Offer.Adviser == adviser) GoLive(call);
    }

    /// <summary>The adviser's call job has ended, for whatever reason: the call ends with it.</summary>
    public static void Stopped(Pawn adviser)
    {
        if (byAdviser.TryGetValue(adviser, out var call)) End(call, dropped: Matches(call.Offer.Carer.CurJob, call.Offer.Def));
    }

    private static void GoLive(AdviceCall call)
    {
        if (call.Live) return;
        AdviceOffer offer = call.Offer;
        // Radio 0.26.0: another faction's doctor comes on their terms (an ally's day, a neutral faction's goodwill), or not at all.
        if (offer.Faction is { } faction && !FactionDoctors.Charge(faction, offer.Adviser, offer.Carer, offer.Def.skill))
        {
            End(call, dropped: false);
            return;
        }
        call.Live = true;
        RadioLog.Add(RadioLogKind.Advice, Say(offer, "PhobosRadioAdvice_CallOpened", MessageTypeDefOf.SilentInput));
        Worked(offer);
        Thank(offer);
        // Radio 0.26.0, the Elmer: the carer learns the skill half again as fast while talked through (the owner's card).
        SkillLifts.Teach(offer.Carer, offer.Def.skill, RadioRulesDef.Current.adviceLearnFactor);
        if (Seen.Wants("radio.elmer", offer.Carer, offer.Def.act)) Seen.Note("radio.elmer", offer.Carer, offer.Def.skill.label, offer.Def.act);
        string variant = offer.Def.act + (offer.Faction != null ? "-faction" : "");
        if (offer.Picture && Seen.Wants("radio.advice-picture", offer.Carer, offer.Def.act)) Seen.Note("radio.advice-picture", offer.Carer, offer.Def.skill.label, offer.Def.act);
        if (Seen.Wants("radio.advice", offer.Adviser, variant))
            Seen.Note("radio.advice", offer.Adviser, offer.Carer.LabelShort + " at " + offer.Def.skill.label + " " + offer.Level + ", " + offer.Place, variant);
    }

    /// <summary>Why a call should end now (true: dropped while the carer still works; false: the work is done), or null to go on.</summary>
    private static bool? Ends(AdviceCall call)
    {
        AdviceOffer offer = call.Offer;
        Pawn carer = offer.Carer, adviser = offer.Adviser;
        if (carer.Destroyed || carer.Dead || !carer.Spawned || !Matches(carer.CurJob, offer.Def) || !Enabled(offer.Def)) return false;
        if (offer.Place == AdvicePlace.Afar)
        {
            // Another faction's doctor: the call holds while the faction will still help and the carer is in touch.
            if (!FactionDoctors.On || offer.Faction == null || FactionDoctors.StandingOf(offer.Faction) == FactionStanding.Hostile) return true;
            return InTouch(carer) ? null : true;
        }
        if (adviser.Destroyed || adviser.Dead || !adviser.Spawned || adviser.Downed || !adviser.Awake() || adviser.InMentalState || adviser.Drafted) return true;
        if (offer.Place == AdvicePlace.InBed ? !adviser.InBed() : adviser.CurJob?.def != RadioAdviceDefOf.PhobosRadio_AdviseByRadio) return true;
        if (!InTouch(carer)) return true;
        if (offer.Place != AdvicePlace.AtDesk && !InTouch(adviser)) return true;
        return null;
    }

    public static bool InTouch(Pawn pawn)
    {
        var net = RadioNet.For(pawn.Map);
        if (net.ServedBy(pawn) != null) return net.TryGetBest(pawn, out var link) && link != null && link.Budget.Outcome != LinkOutcome.NoContact;
        // Radio 0.25.0: through a cross-band vehicle, kept a short while by the tie-in, so a live call's look costs no new search.
        return RemoteReach.Of(pawn).Any(home => RadioRulesDef.Current.MarginBands.Classify(home.MarginDb) != LinkOutcome.NoContact);
    }

    /// <summary>Ends a call. <paramref name="dropped"/>: the carer still works and goes on alone, and is told so.</summary>
    public static void End(AdviceCall call, bool dropped)
    {
        AdviceOffer offer = call.Offer;
        if (!calls.TryGetValue(offer.Carer, out var held) || held != call) return;
        calls.Remove(offer.Carer);
        byAdviser.Remove(offer.Adviser);
        SkillLifts.StopTeaching(offer.Carer);
        nextTry[offer.Carer] = Find.TickManager.TicksGame + RadioRulesDef.Current.adviceRetryTicks;
        if (offer.Adviser.CurJob is { } job && job.def == RadioAdviceDefOf.PhobosRadio_AdviseByRadio && job.targetA.Thing == offer.Carer)
            offer.Adviser.jobs.EndCurrentJob(JobCondition.Succeeded);
        if (dropped && call.Live)
        {
            Say(offer, "PhobosRadioAdvice_CallDropped", MessageTypeDefOf.NeutralEvent);
            if (Seen.Wants("radio.advice-dropped", offer.Carer)) Seen.Note("radio.advice-dropped", offer.Carer, offer.Def.act);
        }
    }

    // ---- Acts with no job: on the road, and the trade talks ----

    /// <summary>The adviser helped with an act that has no job: they stay on the radio a while (booked), and the carer is told.</summary>
    public static void Book(AdviceOffer offer)
    {
        Pawn adviser = offer.Adviser;
        // Radio 0.26.0: another faction's doctor is paid for on their terms; a colonist adviser gains a little Social for the time booked.
        if (offer.Faction is { } faction) FactionDoctors.Charge(faction, adviser, offer.Carer, offer.Def.skill);
        else if (adviser.Spawned && adviser.skills != null)
        {
            var rules = RadioRulesDef.Current;
            adviser.skills.Learn(SkillDefOf.Social, rules.adviceTeachSocialPerTick * offer.Def.bookedTicks);
            if (Seen.Wants("radio.elmer-teacher", adviser)) Seen.Note("radio.elmer-teacher", adviser, offer.Carer.LabelShort + ", booked");
        }
        if (adviser.Spawned && offer.Place != AdvicePlace.InBed && !byAdviser.ContainsKey(adviser) && Interruptible(adviser) && !adviser.Drafted)
        {
            Job job = JobMaker.MakeJob(RadioAdviceDefOf.PhobosRadio_AdviseByRadioBooked, offer.Carer, offer.Station == null ? LocalTargetInfo.Invalid : offer.Station);
            // The booked time rides on the job's count: this job hauls nothing.
            job.count = offer.Def.bookedTicks;
            adviser.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
        }
        int now = Find.TickManager.TicksGame;
        if (!lastSaid.TryGetValue((offer.Carer, offer.Def), out var said) || now - said >= SayAgainTicks)
        {
            lastSaid[(offer.Carer, offer.Def)] = now;
            RadioLog.Add(RadioLogKind.Advice, Say(offer, "PhobosRadioAdvice_Helped", MessageTypeDefOf.PositiveEvent));
            Thank(offer);
            Worked(offer);
        }
        if (lastSaid.Count > 256) lastSaid.Clear();
        string variant = offer.Def.act + (offer.Faction != null ? "-faction" : "") + (offer.Carer.Spawned ? "" : "-road");
        if (Seen.Wants("radio.advice", offer.Adviser, variant))
            Seen.Note("radio.advice", offer.Adviser, offer.Carer.LabelShort + " at " + offer.Def.skill.label + " " + offer.Level + (offer.Aid ? ", with the aid" : ""), variant);
    }

    /// <summary>Each frame: when trade talks a lift was shown in close, the adviser's time is booked.</summary>
    public static void WatchTrade()
    {
        if (tradeShown == null || TradeSession.Active) return;
        AdviceOffer offer = tradeShown;
        tradeShown = null;
        SkillLifts.StopTeaching(offer.Carer);
        Book(offer);
    }

    /// <summary>Radio 0.38.0: a call with another faction's settlement earns its QSL card, the first time.</summary>
    private static void Worked(AdviceOffer offer)
    {
        if (offer.Settlement == null || offer.Faction == null) return;
        PlanetTile from = offer.Carer.Spawned ? offer.Carer.Map.Tile : offer.Carer.GetCaravan()?.Tile ?? PlanetTile.Invalid;
        RadioQsl.Worked(offer.Link ?? new FactionDoctorLink(offer.Faction, offer.Settlement, offer.MarginDb), offer.Carer, from, QslContact.Advice);
    }

    private static string Say(AdviceOffer offer, string key, MessageTypeDef type)
    {
        string text = key.Translate(offer.Adviser.LabelShort.Named("ADVISER"), offer.Carer.LabelShort.Named("CARER"), offer.Def.actLabel.Named("ACT"),
            offer.Def.skill.label.Named("SKILL"), offer.Level.Named("LEVEL"), SkillLifts.OwnLevel(offer.Carer, offer.Def.skill).Named("OWN")).Resolve();
        Messages.Message(text, new LookTargets(offer.Adviser.Spawned ? offer.Adviser : offer.Carer), type, historical: false);
        return text;
    }

    /// <summary>The carer thinks the better of the adviser for it.</summary>
    private static void Thank(AdviceOffer offer) =>
        offer.Carer.needs?.mood?.thoughts?.memories?.TryGainMemory(RadioAdviceDefOf.PhobosRadio_TalkedThrough, offer.Adviser);

    // ---- D'thorga Framework's skill lift ----

    private sealed class AdviceLiftSource : ISkillLiftSource
    {
        public SkillLift? Lift(Pawn pawn, SkillDef skill, string act)
        {
            if (!byAct.TryGetValue(act, out var forAct)) return null;
            if (calls.TryGetValue(pawn, out var call) && call.Live && call.Offer.Def.skill == skill && call.Offer.Def.act == act)
                return LiftOf(call.Offer, live: true);
            foreach (RadioAdviceDef def in forAct)
            {
                if (def.skill != skill || !Enabled(def)) continue;
                if (def.trade)
                {
                    if (!TradeSession.Active || TradeSession.playerNegotiator != pawn || Offer(pawn, def) is not { } talks) continue;
                    tradeShown = talks;
                    // Radio 0.26.0, the Elmer: what the talks teach the negotiator, they learn half again as fast.
                    SkillLifts.Teach(pawn, skill, RadioRulesDef.Current.adviceLearnFactor);
                    return LiftOf(talks, live: false);
                }
                // On the road an act has no job, so help on offer is what counts; on a map only a gate (a repair's minimum skill) asks ahead.
                if ((!pawn.Spawned || def.gate) && Offer(pawn, def) is { } offer) return LiftOf(offer, live: false);
            }
            return null;
        }

        public void Used(Pawn pawn, SkillDef skill, string act, SkillLift lift)
        {
            if (lift.Token is AdviceOffer offer && !lift.Live) Book(offer);
        }

        private static SkillLift LiftOf(AdviceOffer offer, bool live) =>
            new(offer.Level, RadioRulesDef.Current.adviceWorkSpeedFactor, offer.Reason, Source, live, offer);
    }
}

/// <summary>
/// Looks at the colony for calls to open and end, at the rules' interval, and watches the trade talks each frame. Saves only other factions'
/// doctors and their calls' times (Radio 0.26.0) and radio friendships (Radio 0.29.0); a save without them loads with none.
/// </summary>
public class GameComponent_RadioAdvice : GameComponent
{
    /// <summary>Where in its interval the talk pass falls, apart from the advice look and the hour (authored).</summary>
    private const int TalkOffset = 37;

    public GameComponent_RadioAdvice(Game game)
    {
        RadioAdvice.Clear();
        RadioFriends.Clear();
        RadioSchool.Clear();
        RadioNeighbours.Clear();
        RadioLog.Clear();
        RadioQsl.Clear();
        RadioTalk.Clear();
        RadioAir.Clear();
    }

    public override void ExposeData()
    {
        FactionDoctors.ExposeData();
        RadioFriends.ExposeData();
        RadioNeighbours.ExposeData();
        RadioLog.ExposeData();
        RadioQsl.ExposeData();
    }

    public override void GameComponentTick()
    {
        int interval = RadioRulesDef.Current.adviceLookTicks;
        if (interval > 0 && Find.TickManager.TicksGame % interval == 0) RadioAdvice.Look();
        // Radio 0.63.0: colonists talk on the air by themselves, a look at the rules' interval, offset from the others.
        int talk = RadioRulesDef.Current.talkLookTicks;
        if (talk > 0 && Find.TickManager.TicksGame % talk == TalkOffset % talk) RadioTalk.Look();
        // Radio 0.29.0: friendships that have ended are mourned, looked over once a game hour.
        if (Find.TickManager.TicksGame % GenDate.TicksPerHour == 0)
        {
            RadioFriends.LookOver();
            // Radio 0.36.0: by chance, a neighbour asks for a colonist on the radio.
            RadioNeighbours.Hourly();
            // Radio 0.66.0: a friendly group on a home map brings the QSL cards awaited by the bureau.
            RadioQsl.Bureau();
        }
    }

    public override void GameComponentUpdate() => RadioAdvice.WatchTrade();
}

/// <summary>Sets advice by radio up once the Defs are loaded.</summary>
[StaticConstructorOnStartup]
public static class RadioAdviceStartup
{
    static RadioAdviceStartup() => RadioAdvice.Init();
}
