using System.Collections.Generic;
using Phobos.Framework.Persistence;
using Phobos.MissilePatches.Rules;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.MissilePatches.Strike;

/// <summary>
/// Remembers each town a strategic strike hit, and how many of its people were standing to defend it just before the strike, so an
/// ally's assault that follows can face the garrison the town had, not the handful the missile left (see <see cref="AssaultSizeRule"/>).
/// Saved, because the count cannot be worked out again afterwards and the owner saves between a strike and the assault. Kept in
/// D'thorga Framework's part store (from Patches 0.16.8), so removing IRBM leaves no error in the save: a town is kept by its world
/// object ID, three lists in step. Stable names.
/// </summary>
public static class StrikeMemory
{
    /// <summary>A strike counts for an assault for 15 days (authored: long enough for a battle to be opened and fought).</summary>
    public const int WindowTicks = 15 * GenDate.TicksPerDay;

    internal const string RecordKey = "PhobosPatches.Irbm.StrikeMemory";
    const string TownsName = "towns", DefendersName = "defenders", TicksName = "ticks";

    /// <summary>The three lists, made in step; null before a game exists.</summary>
    static (List<int> towns, List<int> defenders, List<int> ticks)? Lists()
    {
        PartRecord? record = GameComponent_PhobosPartStore.Record(RecordKey);
        if (record == null) return null;
        List<int> towns = record.Ints(TownsName), defenders = record.Ints(DefendersName), ticks = record.Ints(TicksName);
        int count = System.Math.Min(towns.Count, System.Math.Min(defenders.Count, ticks.Count));
        if (towns.Count > count) towns.RemoveRange(count, towns.Count - count);
        if (defenders.Count > count) defenders.RemoveRange(count, defenders.Count - count);
        if (ticks.Count > count) ticks.RemoveRange(count, ticks.Count - count);
        return (towns, defenders, ticks);
    }

    /// <summary>True when no strike is remembered: the cheap first check before any RimPacts state is read.</summary>
    public static bool Empty => Lists() is not { } lists || lists.towns.Count == 0;

    /// <summary>Counts a struck map's defenders before the strike does any harm. A town only; a later strike on the same town keeps the
    /// first count while it is recent, so a second missile does not shrink the squad again.</summary>
    public static void RecordBeforeStrike(Map map)
    {
        if (map.Parent is not Settlement town || town.Faction == null || town.Faction == Faction.OfPlayer) return;
        if (Lists() is not { } lists) return;
        int now = Find.TickManager.TicksGame;
        int index = lists.towns.IndexOf(town.ID);
        if (index >= 0 && lists.defenders[index] >= 0 && AssaultSizeRule.Recent(lists.ticks[index], now, WindowTicks)) return;
        int count = CountDefenders(map, town.Faction);
        if (index >= 0) RemoveAt(lists, index);
        Add(lists, town.ID, count, now);
        MissilePatchesMod.Logger.Info(town.Label + " had " + count + " defenders standing before the strike; an ally's assault on it will face that many.");
    }

    /// <summary>The garrison counted before a recent strike on this town: null if none, negative for an entry from an older draft of
    /// this mod that marked a strike without a count.</summary>
    public static int? DefendersBefore(Settlement town)
    {
        if (Lists() is not { } lists) return null;
        int index = lists.towns.IndexOf(town.ID);
        if (index < 0 || !AssaultSizeRule.Recent(lists.ticks[index], Find.TickManager.TicksGame, WindowTicks)) return null;
        return lists.defenders[index];
    }

    /// <summary>Takes over what a save from before 0.16.8 kept in the old world component. Only fills an empty record.</summary>
    internal static void TakeOver(List<Settlement?> towns, List<int> defenders, List<int> ticks)
    {
        if (Lists() is not { } lists || lists.towns.Count > 0) return;
        int count = System.Math.Min(towns.Count, System.Math.Min(defenders.Count, ticks.Count));
        for (int i = 0; i < count; i++)
        {
            if (towns[i] is { } town) Add(lists, town.ID, defenders[i], ticks[i]);
        }
    }

    static void Add((List<int> towns, List<int> defenders, List<int> ticks) lists, int town, int defenders, int tick)
    {
        lists.towns.Add(town);
        lists.defenders.Add(defenders);
        lists.ticks.Add(tick);
    }

    static void RemoveAt((List<int> towns, List<int> defenders, List<int> ticks) lists, int i)
    {
        lists.towns.RemoveAt(i);
        lists.defenders.RemoveAt(i);
        lists.ticks.RemoveAt(i);
    }

    static int CountDefenders(Map map, Faction owner)
    {
        int count = 0;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (pawn.Dead || pawn.Downed || pawn.Faction == null || pawn.Faction == Faction.OfPlayer) continue;
            if (!pawn.RaceProps.Humanlike && !pawn.RaceProps.IsMechanoid) continue;
            if (pawn.Faction != owner && pawn.Faction.HostileTo(owner)) continue;
            count++;
        }
        return count;
    }
}

/// <summary>
/// Where the strike memory lived before Patches 0.16.8. Kept so a save that still holds it loads cleanly: it reads the old lists, hands
/// them to <see cref="StrikeMemory"/> and retires once loading ends, so the next save no longer names this class and removing IRBM
/// later costs no error. Never saves anything itself.
/// </summary>
public class WorldComponent_PhobosStrikeMemory : WorldComponent
{
    List<Settlement?>? towns;
    List<int>? defenders;
    List<int>? ticks;

    public WorldComponent_PhobosStrikeMemory(World world) : base(world) { }

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.Saving) return;
        Scribe_Collections.Look(ref towns, "PhobosStrike_Towns", LookMode.Reference);
        Scribe_Collections.Look(ref defenders, "PhobosStrike_Defenders", LookMode.Value);
        Scribe_Collections.Look(ref ticks, "PhobosStrike_Ticks", LookMode.Value);
    }

    public override void FinalizeInit(bool fromLoad)
    {
        base.FinalizeInit(fromLoad);
        if (towns != null && defenders != null && ticks != null) StrikeMemory.TakeOver(towns, defenders, ticks);
        towns = null;
        defenders = null;
        ticks = null;
        RetiredComponents.Retire(this);
    }
}
