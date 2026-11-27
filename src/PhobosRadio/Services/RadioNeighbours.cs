using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Services;

// Neighbours ask for yours (Radio 0.32.0; docs/development/radio-friends-school-medevac.md, the owner's card of 27 Nov 2026: "Goodwill, now
// and then"). Now and then a faction that is not hostile, with a settlement the colony's stations reach, asks by radio for the colony's
// best doctor or mechanic for an hour. Yes earns goodwill when the hour is done; no costs nothing.

/// <summary>
/// Neighbours' calls: when one is asked, who is asked for, and the hour at the radio.
/// <para>Cost: one chance roll a game hour; a reach search and a look over a colony's colonists only when the roll asks.</para>
/// </summary>
public static class RadioNeighbours
{
    private static Dictionary<Pawn, Faction> callFaction = new();
    private static Dictionary<Pawn, SkillDef> callSkill = new();
    private static List<Pawn>? factionKeys, skillKeys;
    private static List<Faction>? factionValues;
    private static List<SkillDef>? skillValues;

    /// <summary>Whether the player allows neighbours' calls (a setting, on by default).</summary>
    public static bool On => RadioMod.Settings.neighbourCalls;

    public static void Clear()
    {
        callFaction = new Dictionary<Pawn, Faction>();
        callSkill = new Dictionary<Pawn, SkillDef>();
    }

    /// <summary>Saved: the calls accepted and not yet done, so a load mid-call still earns the goodwill.</summary>
    public static void ExposeData()
    {
        Scribe_Collections.Look(ref callFaction, "PhobosRadioNeighbourFaction", LookMode.Reference, LookMode.Reference, ref factionKeys, ref factionValues);
        Scribe_Collections.Look(ref callSkill, "PhobosRadioNeighbourSkill", LookMode.Reference, LookMode.Def, ref skillKeys, ref skillValues);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            callFaction ??= new Dictionary<Pawn, Faction>();
            callSkill ??= new Dictionary<Pawn, SkillDef>();
            callFaction.RemoveAll(pair => pair.Key == null || pair.Value == null);
            callSkill.RemoveAll(pair => pair.Key == null || pair.Value == null || !callFaction.ContainsKey(pair.Key));
        }
    }

    /// <summary>The call a colonist has accepted and is on, or null.</summary>
    public static (Faction faction, SkillDef skill)? CallOf(Pawn colonist) =>
        callFaction.TryGetValue(colonist, out var faction) && callSkill.TryGetValue(colonist, out var skill) ? (faction, skill) : null;

    /// <summary>Once a game hour: by chance (the rules' days on average), a neighbour asks.</summary>
    public static void Hourly()
    {
        var rules = RadioRulesDef.Current;
        if (!On || rules.neighbourMtbDays <= 0f || !Rand.MTBEventOccurs(rules.neighbourMtbDays, GenDate.TicksPerDay, GenDate.TicksPerHour)) return;
        TryAsk(forced: false);
    }

    /// <summary>
    /// A neighbour asks now, if one can: a colony with a station that reaches a settlement of a faction that would talk, and a colonist there
    /// good enough at Medicine or Construction, free and able to take a call. False when nobody could be asked.
    /// </summary>
    public static bool TryAsk(bool forced)
    {
        var rules = RadioRulesDef.Current;
        foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome && RadioNet.For(m).Stations.Count > 0).InRandomOrder())
        {
            if (FactionDoctors.ReachToTalk(map, map.Tile, () => FactionDoctors.StationEnds(map)) is not { } link) continue;
            Pawn? best = null;
            SkillDef? bestSkill = null;
            int bestLevel = rules.neighbourMinSkill - 1;
            foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if (!Free(colonist)) continue;
                foreach (SkillDef skill in new[] { SkillDefOf.Medicine, SkillDefOf.Construction })
                {
                    if (colonist.skills?.GetSkill(skill) is not { TotallyDisabled: false } record || record.Level <= bestLevel) continue;
                    best = colonist;
                    bestSkill = skill;
                    bestLevel = record.Level;
                }
            }
            if (best == null || bestSkill == null) continue;
            var letter = (ChoiceLetter_PhobosNeighbourCall)LetterMaker.MakeLetter(RadioNeighbourDefOf.PhobosRadio_NeighbourCall);
            letter.Set(link.Faction, link.Settlement.LabelCap, best, bestSkill, Find.TickManager.TicksGame + rules.neighbourExpiryTicks);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
        if (forced) Messages.Message("PhobosRadioNeighbour_NoneToAsk".Translate().Resolve(), MessageTypeDefOf.RejectInput, historical: false);
        return false;
    }

    /// <summary>Whether a colonist can take a neighbour's call now: as an adviser can, on a hand-held in touch or with a desk to walk to.</summary>
    public static bool Free(Pawn colonist)
    {
        if (colonist.Downed || !colonist.Awake() || colonist.InMentalState || colonist.Drafted || colonist.InBed()) return false;
        if (!colonist.health.capacities.CapableOf(PawnCapacityDefOf.Talking) || !colonist.health.capacities.CapableOf(PawnCapacityDefOf.Hearing)) return false;
        if (RadioAdvice.CallBy(colonist) != null || RadioAdvice.CallOf(colonist) != null || callFaction.ContainsKey(colonist)) return false;
        return RadioAdvice.InTouch(colonist) || RadioAdvice.NearestDesk(colonist, RadioNet.For(colonist.Map)) != null;
    }

    /// <summary>The player said yes: the colonist goes to the radio for the rules' hour. From the letter.</summary>
    public static void Accept(Faction faction, Pawn colonist, SkillDef skill)
    {
        if (!colonist.Spawned || !Free(colonist) || !FactionDoctors.Talks(faction))
        {
            Messages.Message("PhobosRadioNeighbour_CannotNow".Translate(colonist.LabelShort.Named("COLONIST")).Resolve(), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Thing? desk = RadioAdvice.InTouch(colonist) ? null : RadioAdvice.NearestDesk(colonist, RadioNet.For(colonist.Map));
        callFaction[colonist] = faction;
        callSkill[colonist] = skill;
        Job job = JobMaker.MakeJob(RadioNeighbourDefOf.PhobosRadio_NeighbourCallJob, desk == null ? LocalTargetInfo.Invalid : desk);
        job.count = RadioRulesDef.Current.neighbourCallTicks;
        colonist.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
    }

    /// <summary>A tick of the call: the colonist gains a little of the skill and of Social (the rules' rates).</summary>
    public static void Talking(Pawn colonist, int delta)
    {
        if (CallOf(colonist) is not { } call) return;
        var rules = RadioRulesDef.Current;
        colonist.skills?.Learn(call.skill, rules.neighbourSkillXpPerTick * delta);
        colonist.skills?.Learn(SkillDefOf.Social, rules.adviceTeachSocialPerTick * delta);
    }

    /// <summary>The call has ended: done in full, the faction's goodwill rises; cut short, nothing.</summary>
    public static void Ended(Pawn colonist, bool done)
    {
        if (CallOf(colonist) is not { } call) return;
        callFaction.Remove(colonist);
        callSkill.Remove(colonist);
        if (!done || !FactionDoctors.Talks(call.faction)) return;
        int goodwill = RadioRulesDef.Current.neighbourGoodwill;
        call.faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwill, canSendMessage: true, canSendHostilityLetter: false,
            reason: RadioNeighbourDefOf.PhobosRadio_HelpedByRadio);
        string variant = call.skill == SkillDefOf.Medicine ? "medicine" : "repairs";
        if (Seen.Wants("radio.neighbour-call", colonist, variant)) Seen.Note("radio.neighbour-call", colonist, call.faction.Name, variant);
    }
}

/// <summary>A neighbour's request for a colonist on the radio (Radio 0.32.0): yes or no, gone after the rules' hours.</summary>
public class ChoiceLetter_PhobosNeighbourCall : ChoiceLetter
{
    private Faction? faction;
    private Pawn? colonist;
    private SkillDef? skill;
    private int expiresAt;

    public void Set(Faction asking, string settlement, Pawn asked, SkillDef forSkill, int expires)
    {
        faction = asking;
        colonist = asked;
        skill = forSkill;
        expiresAt = expires;
        relatedFaction = asking;
        lookTargets = new LookTargets(asked);
        radioMode = true;
        string role = (forSkill == SkillDefOf.Medicine ? "PhobosRadioAdvice_RoleDoctor" : "PhobosRadioAdvice_RoleMechanic").Translate().Resolve();
        Label = "PhobosRadioNeighbour_Label".Translate(asking.Name.Named("FACTION"));
        Text = "PhobosRadioNeighbour_Text".Translate(asking.Name.Named("FACTION"), settlement.Named("SETTLEMENT"), asked.LabelShort.Named("COLONIST"),
            role.Named("ROLE"), forSkill.label.Named("SKILL"), RadioRulesDef.Current.neighbourGoodwill.Named("GOODWILL")).Resolve();
    }

    private bool Expired => Find.TickManager.TicksGame >= expiresAt;

    public override bool CanShowInLetterStack => base.CanShowInLetterStack && !Expired;

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (ArchivedOnly || Expired || faction == null || colonist == null || skill == null)
            {
                yield return Option_Close;
                yield break;
            }
            var yes = new DiaOption("PhobosRadioNeighbour_Yes".Translate(colonist.LabelShort.Named("COLONIST")))
            {
                action = () =>
                {
                    RadioNeighbours.Accept(faction, colonist, skill);
                    Find.LetterStack.RemoveLetter(this);
                },
                resolveTree = true,
            };
            if (!colonist.Spawned || !RadioNeighbours.Free(colonist)) yes.Disable("PhobosRadioNeighbour_Busy".Translate(colonist.LabelShort.Named("COLONIST")));
            yield return yes;
            yield return new DiaOption("PhobosRadioNeighbour_No".Translate())
            {
                action = () => Find.LetterStack.RemoveLetter(this),
                resolveTree = true,
            };
            yield return Option_Postpone;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref faction, "PhobosRadioNeighbourFaction");
        Scribe_References.Look(ref colonist, "PhobosRadioNeighbourColonist");
        Scribe_Defs.Look(ref skill, "PhobosRadioNeighbourSkill");
        Scribe_Values.Look(ref expiresAt, "PhobosRadioNeighbourExpires");
    }
}

[DefOf]
public static class RadioNeighbourDefOf
{
    public static LetterDef PhobosRadio_NeighbourCall = null!;
    public static JobDef PhobosRadio_NeighbourCallJob = null!;
    public static HistoryEventDef PhobosRadio_HelpedByRadio = null!;

    static RadioNeighbourDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioNeighbourDefOf));
}
