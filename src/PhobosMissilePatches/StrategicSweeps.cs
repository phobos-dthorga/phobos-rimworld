using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using IRBM;
using Phobos.MissilePatches.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.MissilePatches;

/// <summary>One strategic warhead's pass over one map: the mod's own parameters, and the things still to hit.</summary>
public sealed class StrategicSweep
{
    public StrategicSweep(Map map, DamageDef damageDef, float amount, float armorPen, Thing? launcher, string? sourceLabel,
        List<ThingCategory> scanCategories, bool penetrateThickRoof, bool lockCorePart, bool completeDismantle)
    {
        Map = map;
        DamageDef = damageDef;
        Amount = amount;
        ArmorPen = armorPen;
        Launcher = launcher;
        SourceLabel = sourceLabel;
        ScanCategories = scanCategories;
        PenetrateThickRoof = penetrateThickRoof;
        LockCorePart = lockCorePart;
        CompleteDismantle = completeDismantle;
    }

    public Map Map { get; }
    public DamageDef DamageDef { get; }
    public float Amount { get; }
    public float ArmorPen { get; }
    public Thing? Launcher { get; }
    public string? SourceLabel { get; }
    public List<ThingCategory> ScanCategories { get; }
    public bool PenetrateThickRoof { get; }
    public bool LockCorePart { get; }
    public bool CompleteDismantle { get; }

    /// <summary>The things to hit, taken when the sweep starts and ordered from the map's centre outward.</summary>
    public List<Thing>? Targets;
    public int Next;
    public int Hit;
    public int Gone;
    public int Errors;
    public string? FirstError;
    public int Frames;
    public float StartedAt = -1f;
    public bool LongMessageShown;

    public bool Started => Targets != null;
    public bool Finished => Targets != null && Next >= Targets.Count;
    public string PlaceLabel => Map.Parent?.Label ?? Map.ToString();
}

/// <summary>
/// Spreads InterRim Ballistic Missile's strategic blast over frames. The mod hits every thing on the struck map in one frame,
/// and after each destroyed building the next one's leavings ask the game for a room, which rebuilds every region and room the
/// earlier destructions dirtied; on a large modded map that frame ran for over two minutes in the owner's game and Windows
/// closed the game as hung (7 November 2026). Here each frame takes a slice within a time budget, and during a slice the
/// game's region and room updater is held so the rebuild happens once per frame instead of once per building.
///
/// The per-thing rules are the mod's own, repeated here so the outcome is the same: the same damage, the same thick-roof
/// shelter, the same core-part hit on pawns, the same dismantle paths. Only the pacing and the order change.
/// </summary>
public static class StrategicSweeps
{
    static readonly Queue<StrategicSweep> queue = new();
    static readonly Stopwatch stopwatch = new();
    static Game? game;

    /// <summary>
    /// The updater's private "working" flag: while it is set, TryRebuildDirtyRegionsAndRooms returns at once and a room
    /// lookup in a dirtied cell answers null, which every caller already handles (impassable cells have no room). Setting
    /// Enabled to false instead would log a warning on every lookup.
    /// </summary>
    static readonly AccessTools.FieldRef<RegionAndRoomUpdater, bool>? updaterWorking = FindWorkingFlag();

    public static bool CanHoldRoomRebuilds => updaterWorking != null;
    public static int Pending => queue.Count;
    public static StrategicSweep? Current => queue.Count == 0 ? null : queue.Peek();

    static AccessTools.FieldRef<RegionAndRoomUpdater, bool>? FindWorkingFlag()
    {
        try
        {
            return AccessTools.FieldRefAccess<RegionAndRoomUpdater, bool>("working");
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Queues one pass; the first slice runs in this frame's update. Nothing is hit here.</summary>
    public static void Enqueue(StrategicSweep sweep)
    {
        if (game != Verse.Current.Game)
        {
            queue.Clear();
            game = Verse.Current.Game;
        }
        queue.Enqueue(sweep);
    }

    /// <summary>Called once per frame from Root_Play.Update. Cheap while nothing is queued.</summary>
    public static void Update()
    {
        if (queue.Count == 0) return;
        if (Verse.Current.ProgramState != ProgramState.Playing || game != Verse.Current.Game)
        {
            queue.Clear();
            return;
        }
        StrategicSweep sweep = queue.Peek();
        if (sweep.Map.Index < 0 || sweep.Map.Disposed)
        {
            Log.Message(MissilePatchesMod.LogPrefix + " The map a blast was crossing is gone; the rest of that blast is dropped.");
            queue.Dequeue();
            return;
        }
        if (!sweep.Started) Start(sweep);
        RunSlice(sweep, SweepBudgetRule.ClampBudget(MissilePatchesMod.Settings.frameBudgetMs));
        if (sweep.Finished) Finish(sweep);
    }

    static void Start(StrategicSweep sweep)
    {
        sweep.StartedAt = Time.realtimeSinceStartup;
        Map map = sweep.Map;
        List<Thing> all = map.listerThings.AllThings;
        var targets = new List<Thing>(all.Count);
        if (!sweep.ScanCategories.NullOrEmpty())
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (sweep.ScanCategories.Contains(all[i].def.category)) targets.Add(all[i]);
            }
        }
        IntVec3 centre = map.Center;
        var ranks = new long[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            IntVec3 position = targets[i].Position;
            ranks[i] = SweepOrderRule.Rank(position.x, position.z, centre.x, centre.z);
        }
        Thing[] ordered = targets.ToArray();
        Array.Sort(ranks, ordered);
        sweep.Targets = new List<Thing>(ordered);
    }

    static void RunSlice(StrategicSweep sweep, float budgetMs)
    {
        List<Thing> targets = sweep.Targets!;
        if (sweep.Next >= targets.Count) return;
        sweep.Frames++;
        Map map = sweep.Map;
        RegionAndRoomUpdater updater = map.regionAndRoomUpdater;
        bool held = false;
        bool previousIgnore = IRBM_DamageContext.ignoreDamageMultipliers;
        string previousLabel = IRBM_DamageContext.currentDamageSourceLabel;
        try
        {
            // The mod's own context for its damage patches: no damage multipliers, and the missile's name in the logs.
            IRBM_DamageContext.ignoreDamageMultipliers = true;
            IRBM_DamageContext.currentDamageSourceLabel = sweep.SourceLabel;
            if (IRBM_DefOf.IRBM_MissileWeapon != null && !sweep.SourceLabel.NullOrEmpty()) IRBM_DefOf.IRBM_MissileWeapon.label = sweep.SourceLabel;
            if (MissilePatchesMod.Settings.holdRoomRebuilds && updaterWorking != null && !updaterWorking(updater))
            {
                updaterWorking(updater) = true;
                held = true;
            }
            var damage = new DamageInfo(sweep.DamageDef, sweep.Amount, sweep.ArmorPen, -1f, sweep.Launcher, null, IRBM_DefOf.IRBM_MissileWeapon);
            stopwatch.Restart();
            int done = 0;
            while (sweep.Next < targets.Count && SweepBudgetRule.MayContinue(stopwatch.Elapsed.TotalMilliseconds, budgetMs, done))
            {
                Thing thing = targets[sweep.Next++];
                done++;
                try
                {
                    HitOne(sweep, map, thing, damage);
                }
                catch (Exception error)
                {
                    // The mod swallows every per-thing exception too; here they are counted and the first is reported once.
                    sweep.Errors++;
                    sweep.FirstError ??= error.ToString();
                }
            }
        }
        finally
        {
            if (held)
            {
                updaterWorking!(updater) = false;
                updater.TryRebuildDirtyRegionsAndRooms();
            }
            IRBM_DamageContext.ignoreDamageMultipliers = previousIgnore;
            IRBM_DamageContext.currentDamageSourceLabel = previousLabel;
        }
        if (!sweep.LongMessageShown && Time.realtimeSinceStartup - sweep.StartedAt >= SweepBudgetRule.LongBlastSeconds && sweep.Next < targets.Count)
        {
            sweep.LongMessageShown = true;
            Messages.Message("PhobosMissilePatches_BlastStillRunning".Translate(sweep.PlaceLabel.Named("PLACE")), MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    /// <summary>One thing, by the mod's own rules.</summary>
    static void HitOne(StrategicSweep sweep, Map map, Thing thing, DamageInfo damage)
    {
        if (thing.Destroyed || !thing.Spawned || thing.Map != map)
        {
            sweep.Gone++;
            return;
        }
        if (!sweep.PenetrateThickRoof)
        {
            IntVec3 position = thing.Position;
            if (position.Roofed(map) && map.roofGrid.RoofAt(position) == RoofDefOf.RoofRockThick) return;
        }
        sweep.Hit++;
        if (sweep.CompleteDismantle)
        {
            if (thing is Building building)
            {
                DismantleBuilding(building, map);
                return;
            }
            if (thing is Plant plant)
            {
                HarvestAndRemove(plant, map);
                return;
            }
            if (thing.def.category == ThingCategory.Item)
            {
                // Dismantling touches only fresh corpses among items; every other item is left as it is.
                if (thing is Corpse { InnerPawn: not null } corpse && corpse.GetRotStage() == RotStage.Fresh) SalvageCorpse(corpse, map);
                return;
            }
        }
        if (sweep.LockCorePart && thing is Pawn pawn && pawn.RaceProps.body != null) damage.SetHitPart(pawn.RaceProps.body.corePart);
        thing.TakeDamage(damage);
    }

    static void DismantleBuilding(Building building, Map map)
    {
        if (!building.def.destroyable) return;
        if (building is Mineable mineable)
        {
            mineable.Notify_TookMiningDamage(mineable.MaxHitPoints, null);
            mineable.DestroyMined(null);
            return;
        }
        List<ThingDefCountClass> leavings = building.CostListAdjusted();
        if (leavings.NullOrEmpty()) leavings = building.def.killedLeavings;
        if (!leavings.NullOrEmpty())
        {
            foreach (ThingDefCountClass entry in leavings)
            {
                if (entry.thingDef != null && entry.count > 0) Drop(entry.thingDef, entry.count, building.Position, map);
            }
        }
        if (building.def.killedLeavingsRanges != null)
        {
            foreach (ThingDefCountRangeClass range in building.def.killedLeavingsRanges)
            {
                int count = Mathf.RoundToInt(range.countRange.RandomInRange);
                if (count > 0 && range.thingDef != null) Drop(range.thingDef, count, building.Position, map);
            }
        }
        building.Destroy();
    }

    static void HarvestAndRemove(Plant plant, Map map)
    {
        int yield = plant.YieldNow();
        if (yield > 0 && plant.def.plant.harvestedThingDef != null) Drop(plant.def.plant.harvestedThingDef, yield, plant.Position, map);
        plant.Destroy(DestroyMode.KillFinalize);
    }

    static void SalvageCorpse(Corpse corpse, Map map)
    {
        IntVec3 position = corpse.Position;
        Pawn inner = corpse.InnerPawn;
        corpse.Strip();
        if (inner.health?.hediffSet != null && inner.RaceProps.Humanlike)
        {
            var partsDone = new HashSet<BodyPartRecord>();
            foreach (Hediff hediff in inner.health.hediffSet.hediffs)
            {
                if (hediff.def.spawnThingOnRemoved == null) continue;
                Drop(hediff.def.spawnThingOnRemoved, 1, position, map);
                if (hediff.Part != null) partsDone.Add(hediff.Part);
            }
            foreach (BodyPartRecord part in inner.health.hediffSet.GetNotMissingParts())
            {
                if (part.def.spawnThingOnRemoved == null || partsDone.Contains(part)) continue;
                Drop(part.def.spawnThingOnRemoved, 1, position, map);
                partsDone.Add(part);
            }
        }
        foreach (Thing product in inner.ButcherProducts(null, 1f))
        {
            GenPlace.TryPlaceThing(product, position, map, ThingPlaceMode.Near);
        }
        corpse.Destroy();
    }

    static void Drop(ThingDef def, int count, IntVec3 position, Map map)
    {
        Thing thing = ThingMaker.MakeThing(def);
        thing.stackCount = count;
        GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Near);
    }

    static void Finish(StrategicSweep sweep)
    {
        queue.Dequeue();
        float seconds = Time.realtimeSinceStartup - sweep.StartedAt;
        int total = sweep.Targets?.Count ?? 0;
        string line = MissilePatchesMod.LogPrefix + " Blast over " + sweep.PlaceLabel + ": " + sweep.Hit + " of " + total + " things hit in "
            + seconds.ToString("0.0") + " s over " + sweep.Frames + " frame" + (sweep.Frames == 1 ? "" : "s")
            + (sweep.Gone > 0 ? ", " + sweep.Gone + " already gone" : "")
            + (sweep.Errors > 0 ? ", " + sweep.Errors + " error" + (sweep.Errors == 1 ? "" : "s") + " swallowed; the first: " + sweep.FirstError : "")
            + ".";
        if (sweep.Errors > 0) Log.Warning(line); else Log.Message(line);
        if (sweep.LongMessageShown)
        {
            Messages.Message("PhobosMissilePatches_BlastSpent".Translate(sweep.PlaceLabel.Named("PLACE")), MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    /// <summary>For the dev action: forgets every queued pass without hitting anything more.</summary>
    public static void Clear() => queue.Clear();
}
