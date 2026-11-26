using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Services;

// Another faction's doctor by radio (Radio 0.26.0; docs/development/radio-advice-and-relays.md, the owner's cards of 27 Nov 2026: "By real
// radio reach" and "Allies free, others goodwill"). When no colonist of the player's can help, a doctor at another faction's settlement in
// radio reach talks the carer through it: allies free, a new call at most once a day; a neutral faction for a little goodwill, never taking
// it below the rules' floor; a hostile one never. Each faction has one doctor, a person of theirs kept in the world, so the same voice
// answers each time. What the call lifts is advice by radio's own (RadioAdvice); this file finds the doctor and keeps the faction's terms.

/// <summary>A settlement of another faction a carer reaches by radio, and the link's margin.</summary>
public sealed record FactionDoctorLink(Faction Faction, Settlement Settlement, double MarginDb);

/// <summary>
/// The doctors of other factions, their terms and who reaches them.
/// <para>Cost: nothing each tick. A search runs only when a carer finds no colonist to help them, for a map or a convoy at most once every
/// <see cref="RadioRulesDef.factionReachKeepTicks"/> (2500); it looks only at settlements within
/// <see cref="RadioRulesDef.factionSearchTiles"/> (100) tiles of factions that would help now, allies first, and is one relay chain search a
/// station end until one reaches. A doctor is made once a faction, at the next look, never during a stat read.</para>
/// </summary>
public static class FactionDoctors
{
    private static Dictionary<Faction, Pawn> doctors = new();
    private static Dictionary<Faction, int> lastOpened = new();
    private static Dictionary<Faction, int> lastUse = new();
    private static readonly Dictionary<object, (int tick, FactionDoctorLink? link)> reach = new();
    private static readonly HashSet<Faction> wanted = new();
    private static Thing? radioUnit;
    // Scribe's working lists for the dictionaries above: kept between its passes over a load.
    private static List<Faction>? doctorKeys, openedKeys, useKeys;
    private static List<Pawn>? doctorValues;
    private static List<int>? openedValues, useValues;

    /// <summary>Whether the player allows other factions' doctors (a setting, on by default).</summary>
    public static bool On => RadioMod.Settings.factionAdvice;

    public static void Clear()
    {
        doctors = new Dictionary<Faction, Pawn>();
        lastOpened = new Dictionary<Faction, int>();
        lastUse = new Dictionary<Faction, int>();
        reach.Clear();
        wanted.Clear();
    }

    /// <summary>Saved with the game: each faction's doctor and when its last call was opened and last used.</summary>
    public static void ExposeData()
    {
        Scribe_Collections.Look(ref doctors, "PhobosRadioFactionDoctors", LookMode.Reference, LookMode.Reference, ref doctorKeys, ref doctorValues);
        Scribe_Collections.Look(ref lastOpened, "PhobosRadioFactionCallOpened", LookMode.Reference, LookMode.Value, ref openedKeys, ref openedValues);
        Scribe_Collections.Look(ref lastUse, "PhobosRadioFactionCallUsed", LookMode.Reference, LookMode.Value, ref useKeys, ref useValues);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            doctors ??= new Dictionary<Faction, Pawn>();
            lastOpened ??= new Dictionary<Faction, int>();
            lastUse ??= new Dictionary<Faction, int>();
            doctors.RemoveAll(pair => pair.Key == null || pair.Value == null);
            lastOpened.RemoveAll(pair => pair.Key == null);
            lastUse.RemoveAll(pair => pair.Key == null);
        }
    }

    // ---- Terms ----

    public static FactionStanding StandingOf(Faction faction) =>
        faction.HostileTo(Faction.OfPlayer) ? FactionStanding.Hostile
        : faction.PlayerRelationKind == FactionRelationKind.Ally ? FactionStanding.Ally
        : FactionStanding.Neutral;

    /// <summary>What the faction asks for its doctor now (see <see cref="FactionDoctorRule.Terms"/>).</summary>
    public static FactionTerms TermsOf(Faction faction)
    {
        var rules = RadioRulesDef.Current;
        return FactionDoctorRule.Terms(StandingOf(faction), faction.PlayerGoodwill, rules.factionNeutralGoodwillCost, rules.factionGoodwillFloor,
            lastOpened.TryGetValue(faction, out var opened) ? opened : -1, lastUse.TryGetValue(faction, out var used) ? used : -1,
            Find.TickManager.TicksGame, rules.factionAllyEveryTicks, rules.factionCallWindowTicks);
    }

    /// <summary>The Medicine of the faction's doctor: 0 for a faction with no radio (before industry).</summary>
    public static int LevelOf(Faction faction)
    {
        var rules = RadioRulesDef.Current;
        return FactionDoctorRule.DoctorLevel((int)faction.def.techLevel, rules.factionDoctorIndustrial, rules.factionDoctorSpacer, rules.factionDoctorUltra);
    }

    /// <summary>
    /// The call has been used: an open one goes on; a new one is opened on the faction's terms (an ally's day starts, a neutral faction's
    /// goodwill falls) and the player is told. False when the faction no longer helps.
    /// </summary>
    public static bool Charge(Faction faction, Pawn doctor, Pawn carer)
    {
        int now = Find.TickManager.TicksGame;
        FactionTerms terms = TermsOf(faction);
        if (terms == FactionTerms.Refused) return false;
        lastUse[faction] = now;
        if (terms == FactionTerms.Continuing) return true;
        lastOpened[faction] = now;
        int cost = RadioRulesDef.Current.factionNeutralGoodwillCost;
        if (terms == FactionTerms.Goodwill)
            faction.TryAffectGoodwillWith(Faction.OfPlayer, -cost, canSendMessage: true, canSendHostilityLetter: false, reason: RadioAdviceDefOf.PhobosRadio_DoctorByRadio);
        string text = (terms == FactionTerms.Free ? "PhobosRadioAdvice_FactionCallAlly" : "PhobosRadioAdvice_FactionCallNeutral").Translate(
            doctor.LabelShort.Named("DOCTOR"), faction.Name.Named("FACTION"), carer.LabelShort.Named("CARER"), cost.Named("GOODWILL")).Resolve();
        Messages.Message(text, new LookTargets(carer), MessageTypeDefOf.NeutralEvent, historical: false);
        string variant = terms == FactionTerms.Free ? "ally" : "neutral";
        if (Seen.Wants("radio.faction-doctor", carer, variant)) Seen.Note("radio.faction-doctor", carer, faction.Name + ", " + doctor.LabelShort, variant);
        return true;
    }

    /// <summary>A live call is still in use: it stays open (Radio 0.26.0, the call window).</summary>
    public static void Using(Faction faction) => lastUse[faction] = Find.TickManager.TicksGame;

    // ---- Doctors ----

    /// <summary>The faction's doctor, or null when none is made yet: one is made at the next look (never during a stat read).</summary>
    public static Pawn? DoctorOf(Faction faction)
    {
        if (doctors.TryGetValue(faction, out var doctor) && !doctor.Dead && !doctor.Destroyed && doctor.Faction == faction) return doctor;
        wanted.Add(faction);
        return null;
    }

    /// <summary>Makes the doctors asked for since the last look. From the game's tick only.</summary>
    public static void MakeWanted()
    {
        if (wanted.Count == 0) return;
        foreach (Faction faction in wanted.ToList())
        {
            if (!faction.defeated && Make(faction) is { } doctor) doctors[faction] = doctor;
        }
        wanted.Clear();
    }

    /// <summary>A person of the faction's own kind with the doctor's Medicine, kept in the world. Up to three tries for one able to tend.</summary>
    private static Pawn? Make(Faction faction)
    {
        PawnKindDef? kind = faction.def.basicMemberKind;
        int level = LevelOf(faction);
        if (kind == null || level <= 0) return null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true));
            if (pawn.skills?.GetSkill(SkillDefOf.Medicine) is not { TotallyDisabled: false } medicine)
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                continue;
            }
            medicine.Level = level;
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            return pawn;
        }
        return null;
    }

    // ---- Reach ----

    /// <summary>The powered stations' ends on a map, as the start of a search: the tallest aerial first.</summary>
    public static List<RelayNode> StationEnds(Map map)
    {
        var ends = new List<RelayNode>();
        foreach (CompRadio station in RadioNet.For(map).Stations)
        {
            if (station.parent.TryGetComp<CompPowerTrader>() is { PowerOn: false }) continue;
            foreach (RadioEndpoint end in RadioNet.StationEndpoints(station, RadioMod.Settings.metresPerCell))
                ends.Add(new RelayNode(end, map.Tile, map.Parent.LabelCap, map, SiteMetres: RelayNode.SiteOf(map.Tile)));
        }
        ends.Sort((a, b) => b.End.AntennaHeightMetres.CompareTo(a.End.AntennaHeightMetres));
        return ends;
    }

    /// <summary>
    /// The settlement of a faction that would help now that this place reaches by radio, allies first, or null. <paramref name="origin"/> is
    /// the map or convoy searched for, kept a while; <paramref name="froms"/> its radio's ends, tried in turn until one reaches.
    /// </summary>
    public static FactionDoctorLink? Reach(object origin, PlanetTile tile, System.Func<IReadOnlyList<RelayNode>> froms)
    {
        if (!On) return null;
        int now = Find.TickManager.TicksGame;
        var rules = RadioRulesDef.Current;
        if (reach.TryGetValue(origin, out var held) && now - held.tick < rules.factionReachKeepTicks)
            return held.link != null && TermsOf(held.link.Faction) != FactionTerms.Refused ? held.link : null;
        if (reach.Count > 64) reach.Clear();
        IReadOnlyList<RelayNode> ends = froms();
        FactionDoctorLink? found = ends.Count == 0 ? null : Search(origin, tile, ends, rules);
        reach[origin] = (now, found);
        return found;
    }

    private static FactionDoctorLink? Search(object origin, PlanetTile tile, IReadOnlyList<RelayNode> froms, RadioRulesDef rules)
    {
        var allies = new List<RelayNode>();
        var others = new List<RelayNode>();
        foreach (Settlement settlement in Find.WorldObjects.Settlements)
        {
            if (settlement.Faction is not { } faction || faction.IsPlayer || faction.Hidden || faction.defeated || LevelOf(faction) <= 0) continue;
            if (Find.WorldGrid.ApproxDistanceInTiles(tile, settlement.Tile) > rules.factionSearchTiles) continue;
            FactionTerms terms = TermsOf(faction);
            if (terms == FactionTerms.Refused) continue;
            if (EndOf(settlement) is not { } end) continue;
            (StandingOf(faction) == FactionStanding.Ally ? allies : others).Add(end);
        }
        if (allies.Count == 0 && others.Count == 0) return null;
        List<RelayNode> relays = RelayNodes.All().ToList();
        object holder = (origin, "PhobosRadioFactionDoctor");
        foreach (List<RelayNode> targets in new[] { allies, others })
        {
            if (targets.Count == 0) continue;
            foreach (RelayNode from in froms)
            {
                if (RelayNodes.BestChain(from, targets, relays, leaseHolder: holder) is not { } chain) continue;
                RelayNode reached = chain.nodes[chain.chain.Path[chain.chain.Path.Count - 1]];
                if (reached.Owner is Settlement settlement && settlement.Faction is { } faction)
                    return new FactionDoctorLink(faction, settlement, chain.chain.WorstMarginDb);
            }
        }
        return null;
    }

    /// <summary>A settlement's radio as the end of a search: the rules' settlement radio on its tile, its site lifted by the tile's hills.</summary>
    private static RelayNode? EndOf(Settlement settlement)
    {
        radioUnit ??= ThingMaker.MakeThing(RadioAdviceDefOf.PhobosRadio_SettlementRadio);
        if (radioUnit.TryGetComp<CompRadio>() is not { } radio) return null;
        return new RelayNode(RadioEndpoint.BuiltIn(radioUnit, IntVec3.Zero, radio), settlement.Tile, settlement.LabelCap, settlement,
            SiteMetres: RelayNode.SiteOf(settlement.Tile));
    }
}
