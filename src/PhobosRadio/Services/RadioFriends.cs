using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Services;

// A radio friend (Radio 0.29.0; docs/development/radio-friends-school-medevac.md, the owner's card of 27 Nov 2026: "Mood, and a little
// goodwill"). A colonist on recreation chats by radio with a person at a friendly settlement in reach; the friendship grows with the hours,
// each chat leaves a mood lift by how close they are, an hour's chat raises the friend's faction's goodwill a little, and a friendship that
// ends (the friend dies, their faction turns hostile) is mourned. The friend is a person of that faction kept in the world.

/// <summary>A colonist's radio friend and how many hours they have talked.</summary>
public sealed class RadioFriendship : IExposable
{
    public Pawn Friend = null!;
    public float Hours;

    public void ExposeData()
    {
        Scribe_References.Look(ref Friend, "PhobosRadioFriend", saveDestroyedThings: true);
        Scribe_Values.Look(ref Hours, "PhobosRadioFriendHours");
    }
}

/// <summary>
/// Radio friends: who each colonist's friend is, the friendship's hours, and what a chat leaves behind.
/// <para>Cost: nothing each tick. The game's recreation asks <see cref="JoyGiver_PhobosRadioChat"/> only when a colonist picks recreation;
/// it reads the radio net's last pass and a reach search kept an hour for the map. Friendships are looked over once a game hour, a few
/// at most.</para>
/// </summary>
public static class RadioFriends
{
    private static Dictionary<Pawn, RadioFriendship> friendships = new();
    private static Dictionary<Faction, int> goodwillQuadrum = new();
    private static Dictionary<Faction, int> goodwillCount = new();
    private static List<Pawn>? friendshipKeys;
    private static List<RadioFriendship>? friendshipValues;
    private static List<Faction>? quadrumKeys, countKeys;
    private static List<int>? quadrumValues, countValues;

    public static void Clear()
    {
        friendships = new Dictionary<Pawn, RadioFriendship>();
        goodwillQuadrum = new Dictionary<Faction, int>();
        goodwillCount = new Dictionary<Faction, int>();
    }

    public static void ExposeData()
    {
        Scribe_Collections.Look(ref friendships, "PhobosRadioFriendships", LookMode.Reference, LookMode.Deep, ref friendshipKeys, ref friendshipValues);
        Scribe_Collections.Look(ref goodwillQuadrum, "PhobosRadioFriendGoodwillQuadrum", LookMode.Reference, LookMode.Value, ref quadrumKeys, ref quadrumValues);
        Scribe_Collections.Look(ref goodwillCount, "PhobosRadioFriendGoodwillCount", LookMode.Reference, LookMode.Value, ref countKeys, ref countValues);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            friendships ??= new Dictionary<Pawn, RadioFriendship>();
            goodwillQuadrum ??= new Dictionary<Faction, int>();
            goodwillCount ??= new Dictionary<Faction, int>();
            friendships.RemoveAll(pair => pair.Key == null || pair.Value?.Friend == null);
            goodwillQuadrum.RemoveAll(pair => pair.Key == null);
            goodwillCount.RemoveAll(pair => pair.Key == null);
        }
    }

    /// <summary>A colonist's friendship, or null.</summary>
    public static RadioFriendship? Of(Pawn colonist) => friendships.TryGetValue(colonist, out var held) ? held : null;

    /// <summary>The stage of a friendship by its hours: 0 a new friend, 1 a friend, 2 a close friend (the rules' hours).</summary>
    public static int Stage(float hours)
    {
        var rules = RadioRulesDef.Current;
        return hours >= rules.friendCloseHours ? 2 : hours >= rules.friendHours ? 1 : 0;
    }

    /// <summary>
    /// The friend this colonist on <paramref name="map"/> can chat with now, made if they have none: their friend, when the friend's faction
    /// still talks and the map's stations reach one of its settlements; else a new friend at a settlement in reach. Null with nobody in reach.
    /// From the game's tick only (a friend may be made).
    /// </summary>
    public static Pawn? FriendInReach(Pawn colonist, Map map)
    {
        if (Of(colonist) is { } held && held.Friend is { Dead: false, Destroyed: false } friend && friend.Faction is { } faction)
        {
            if (!FactionDoctors.Talks(faction) || FactionDoctors.ReachToTalk(map, map.Tile, () => FactionDoctors.StationEnds(map), faction) is not { } reached)
                return null;
            // Radio 0.38.0: a chat with a settlement earns its QSL card, the first time.
            RadioQsl.Worked(reached.Settlement, colonist, reached.MarginDb, map.Tile, QslContact.Friend);
            return friend;
        }
        if (FactionDoctors.ReachToTalk(map, map.Tile, () => FactionDoctors.StationEnds(map)) is not { } link) return null;
        if (FactionDoctors.MakePerson(link.Faction, null) is not { } made) return null;
        friendships[colonist] = new RadioFriendship { Friend = made };
        string madeText = "PhobosRadioFriend_Made".Translate(colonist.LabelShort.Named("COLONIST"), made.LabelShort.Named("FRIEND"),
            link.Faction.Name.Named("FACTION"), link.Settlement.LabelCap.Named("SETTLEMENT")).Resolve();
        Messages.Message(madeText, colonist, MessageTypeDefOf.PositiveEvent, historical: false);
        RadioLog.Add(RadioLogKind.Friend, madeText);
        RadioQsl.Worked(link.Settlement, colonist, link.MarginDb, map.Tile, QslContact.Friend);
        if (Seen.Wants("radio.friend-made", colonist)) Seen.Note("radio.friend-made", colonist, made.LabelShort + ", " + link.Faction.Name);
        return made;
    }

    /// <summary>A chat tick: the friendship grows by the time spent.</summary>
    public static void Chatting(Pawn colonist, int ticks)
    {
        if (Of(colonist) is { } held) held.Hours += ticks / (float)GenDate.TicksPerHour;
    }

    /// <summary>
    /// A chat has ended after <paramref name="ticks"/>: the colonist's mood lifts by the friendship's stage, and a chat of an hour or more
    /// raises the friend's faction's goodwill a little, at most the rules' times a quadrum for each faction.
    /// </summary>
    public static void Chatted(Pawn colonist, int ticks)
    {
        if (ticks <= 0 || Of(colonist) is not { } held || held.Friend?.Faction is not { } faction) return;
        var rules = RadioRulesDef.Current;
        int stage = Stage(held.Hours);
        colonist.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtMaker.MakeThought(RadioFriendDefOf.PhobosRadio_ChattedWithRadioFriend, stage));
        if (Seen.Wants("radio.friend-chat", colonist, stage.ToString()))
            Seen.Note("radio.friend-chat", colonist, held.Friend.LabelShort + ", " + held.Hours.ToString("0.0") + " h", stage.ToString());
        if (ticks < rules.friendGoodwillChatTicks || rules.friendGoodwillPerQuadrum <= 0 || !FactionDoctors.Talks(faction)) return;
        int quadrum = (int)(Find.TickManager.TicksAbs / GenDate.TicksPerQuadrum);
        if (!goodwillQuadrum.TryGetValue(faction, out var lastQuadrum) || lastQuadrum != quadrum)
        {
            goodwillQuadrum[faction] = quadrum;
            goodwillCount[faction] = 0;
        }
        if (goodwillCount.TryGetValue(faction, out var count) && count >= rules.friendGoodwillPerQuadrum) return;
        goodwillCount[faction] = count + 1;
        faction.TryAffectGoodwillWith(Faction.OfPlayer, rules.friendGoodwill, canSendMessage: true, canSendHostilityLetter: false,
            reason: RadioFriendDefOf.PhobosRadio_RadioFriendship);
        if (Seen.Wants("radio.friend-goodwill", colonist)) Seen.Note("radio.friend-goodwill", colonist, faction.Name);
    }

    /// <summary>Once a game hour: a friendship whose friend has died or whose faction no longer talks ends, and the colonist mourns it.</summary>
    public static void LookOver()
    {
        if (friendships.Count == 0) return;
        foreach (var pair in friendships.ToList())
        {
            Pawn colonist = pair.Key;
            Pawn friend = pair.Value.Friend;
            if (colonist.Dead || colonist.Destroyed)
            {
                friendships.Remove(colonist);
                continue;
            }
            bool lost = friend == null || friend.Dead || friend.Destroyed || friend.Faction == null || !FactionDoctors.Talks(friend.Faction);
            if (!lost) continue;
            friendships.Remove(colonist);
            colonist.needs?.mood?.thoughts?.memories?.TryGainMemory(RadioFriendDefOf.PhobosRadio_LostRadioFriend);
            string name = friend?.LabelShort ?? "?";
            string lostText = "PhobosRadioFriend_Lost".Translate(colonist.LabelShort.Named("COLONIST"), name.Named("FRIEND")).Resolve();
            Messages.Message(lostText, colonist.Spawned ? new LookTargets(colonist) : LookTargets.Invalid, MessageTypeDefOf.NegativeEvent, historical: false);
            RadioLog.Add(RadioLogKind.Friend, lostText);
            if (Seen.Wants("radio.friend-lost", colonist)) Seen.Note("radio.friend-lost", colonist, name);
        }
    }
}

/// <summary>
/// Recreation: chatting with a radio friend (Radio 0.29.0). On a hand-held in touch with a station, where the colonist stands or in bed, or
/// at a powered station's desk they walk to. Needs a friend in reach.
/// </summary>
public class JoyGiver_PhobosRadioChat : JoyGiver
{
    public override Job? TryGiveJob(Pawn pawn) => Give(pawn, inBed: false);

    public override Job? TryGiveJobWhileInBed(Pawn pawn) => Give(pawn, inBed: true);

    private Job? Give(Pawn pawn, bool inBed)
    {
        if (!pawn.Spawned || !pawn.IsColonist || pawn.Map is not { } map || RadioNet.For(map).Stations.Count == 0) return null;
        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing)) return null;
        bool handHeld = RadioAdvice.InTouch(pawn);
        Thing? desk = null;
        if (!handHeld)
        {
            if (inBed) return null;
            if (RadioAdvice.NearestDesk(pawn, RadioNet.For(map)) is not { } found) return null;
            desk = found;
        }
        if (RadioFriends.FriendInReach(pawn, map) == null) return null;
        Job job = JobMaker.MakeJob(def.jobDef, desk == null ? LocalTargetInfo.Invalid : desk, LocalTargetInfo.Invalid,
            inBed && pawn.CurrentBed() is { } bed ? bed : LocalTargetInfo.Invalid);
        return job;
    }
}

[DefOf]
public static class RadioFriendDefOf
{
    public static ThoughtDef PhobosRadio_ChattedWithRadioFriend = null!;
    public static ThoughtDef PhobosRadio_LostRadioFriend = null!;
    public static HistoryEventDef PhobosRadio_RadioFriendship = null!;

    static RadioFriendDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioFriendDefOf));
}
