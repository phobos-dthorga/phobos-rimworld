using System.Collections.Generic;
using Phobos.MissilePatches.Rules;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.MissilePatches.Strike;

/// <summary>
/// Remembers each town a strategic strike hit, and how many of its people were standing to defend it just before the strike, so an
/// ally's assault that follows can face the garrison the town had, not the handful the missile left (see <see cref="AssaultSizeRule"/>).
/// Saved, because the count cannot be worked out again afterwards and the owner saves between a strike and the assault. Stable keys.
/// The game makes one for every world; removing the mod leaves a harmless error line about it on load.
/// </summary>
public class WorldComponent_PhobosStrikeMemory : WorldComponent
{
    /// <summary>A strike counts for an assault for 15 days (authored: long enough for a battle to be opened and fought).</summary>
    public const int WindowTicks = 15 * GenDate.TicksPerDay;

    List<Settlement?> towns = new();
    List<int> defenders = new();
    List<int> ticks = new();

    public WorldComponent_PhobosStrikeMemory(World world) : base(world) { }

    /// <summary>True when no strike is remembered: the cheap first check before any RimPacts state is read.</summary>
    public bool Empty => towns.Count == 0;

    public static WorldComponent_PhobosStrikeMemory? Current => Find.World?.GetComponent<WorldComponent_PhobosStrikeMemory>();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref towns, "PhobosStrike_Towns", LookMode.Reference);
        Scribe_Collections.Look(ref defenders, "PhobosStrike_Defenders", LookMode.Value);
        Scribe_Collections.Look(ref ticks, "PhobosStrike_Ticks", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            towns ??= new List<Settlement?>();
            defenders ??= new List<int>();
            ticks ??= new List<int>();
            int count = System.Math.Min(towns.Count, System.Math.Min(defenders.Count, ticks.Count));
            for (int i = count - 1; i >= 0; i--)
            {
                if (towns[i] == null) RemoveAt(i);
            }
        }
    }

    /// <summary>Counts a struck map's defenders before the strike does any harm. A town only; a later strike on the same town keeps the
    /// first count while it is recent, so a second missile does not shrink the squad again.</summary>
    public static void RecordBeforeStrike(Map map)
    {
        if (map.Parent is not Settlement town || town.Faction == null || town.Faction == Faction.OfPlayer) return;
        WorldComponent_PhobosStrikeMemory? memory = Current;
        if (memory == null) return;
        int now = Find.TickManager.TicksGame;
        int index = memory.towns.IndexOf(town);
        if (index >= 0 && memory.defenders[index] >= 0 && AssaultSizeRule.Recent(memory.ticks[index], now, WindowTicks)) return;
        int count = CountDefenders(map, town.Faction);
        if (index >= 0) memory.RemoveAt(index);
        memory.towns.Add(town);
        memory.defenders.Add(count);
        memory.ticks.Add(now);
        Log.Message(MissilePatchesMod.LogPrefix + " " + town.Label + " had " + count + " defenders standing before the strike; an ally's assault on it will face that many.");
    }

    /// <summary>The garrison counted before a recent strike on this town: null if none, negative for an entry from an older draft of
    /// this mod that marked a strike without a count.</summary>
    public int? DefendersBefore(Settlement town)
    {
        int index = towns.IndexOf(town);
        if (index < 0 || !AssaultSizeRule.Recent(ticks[index], Find.TickManager.TicksGame, WindowTicks)) return null;
        return defenders[index];
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

    void RemoveAt(int i)
    {
        towns.RemoveAt(i);
        defenders.RemoveAt(i);
        ticks.RemoveAt(i);
    }
}
