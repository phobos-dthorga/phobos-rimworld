using System;
using System.Collections.Generic;
using System.Linq;
using IRBM;
using Phobos.Framework.Work;
using Phobos.MissilePatches.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.MissilePatches;

/// <summary>
/// One strategic warhead's pass over one map, by the missile mod's own rules but fast (D'thorga Patches 0.8.0, 7 November 2026).
/// What the warhead's damage would destroy is destroyed without the damage path (<see cref="HitRule"/>): no leavings, filth, effects
/// or Combat Extended fragments to pile up while the game is paused. Constructed roofs go first in one pass, so falling walls find
/// no roof to check; cells the shelter setting protects are never swept. Runs a slice per frame through the Framework's queue.
/// </summary>
public sealed class IrbmSweep : FrameWork
{
    readonly HashSet<ThingCategory> categories;
    List<Thing>? targets;
    int next;
    int passesDone;

    ShelterMode shelter;
    bool rubble;
    bool fragmentsLive;
    CorpseMode corpses;

    bool heldRooms;
    CombatExtendedFragments.Override fragmentChange;
    bool previousIgnore;
    string? previousLabel;
    bool longMessageShown;

    public IrbmSweep(Map map, DamageDef damageDef, float amount, float armorPen, Thing? launcher, string? sourceLabel,
        List<ThingCategory>? scanCategories, bool penetrateThickRoof, bool lockCorePart, bool completeDismantle)
    {
        Map = map;
        DamageDef = damageDef;
        Amount = amount;
        ArmorPen = armorPen;
        Launcher = launcher;
        SourceLabel = sourceLabel;
        categories = new HashSet<ThingCategory>(scanCategories ?? new List<ThingCategory>());
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
    public bool PenetrateThickRoof { get; }
    public bool LockCorePart { get; }
    public bool CompleteDismantle { get; }

    /// <summary>Identical strikes queued behind this one before it started (repeat-hit warheads) run as further passes.</summary>
    public int Passes { get; private set; } = 1;

    public int Total { get; private set; }
    public int Vaporised { get; private set; }
    public int Wrecked { get; private set; }
    public int Damaged { get; private set; }
    public int PawnsHit { get; private set; }
    public int Sheltered { get; private set; }
    public int Gone { get; private set; }
    public int RoofsStripped { get; private set; }
    public int Errors { get; private set; }
    public string? FirstError { get; private set; }
    public int Next => next;
    public int Count => targets?.Count ?? 0;

    public string PlaceLabel => Map.Parent?.Label ?? Map.ToString();
    public override string Label => "Strategic blast over " + PlaceLabel;
    public override bool StillValid => Map.Index >= 0 && !Map.Disposed;
    public override float BudgetMs => MissilePatchesMod.BudgetMs(SecondsSinceStart);
    public override int MinimumStepsPerSlice => IrbmBudgetRule.MinimumThingsPerSlice;
    public override bool Finished => targets != null && next >= targets.Count && passesDone + 1 >= Passes;

    /// <summary>Whether this warhead's damage tears down buildings, so their roofs go with them.</summary>
    bool DestroysBuildings => categories.Contains(ThingCategory.Building) && DamageDef.harmsHealth;

    public override bool TryAbsorb(FrameWork later)
    {
        if (later is not IrbmSweep other || other.Map != Map || other.DamageDef != DamageDef || other.Amount != Amount
            || other.ArmorPen != ArmorPen || other.PenetrateThickRoof != PenetrateThickRoof || other.LockCorePart != LockCorePart
            || other.CompleteDismantle != CompleteDismantle || !other.categories.SetEquals(categories)) return false;
        Passes++;
        return true;
    }

    protected override void Start()
    {
        MissilePatchesSettings settings = MissilePatchesMod.Settings;
        shelter = ShelterRule.Effective(settings.shelter, PenetrateThickRoof);
        rubble = settings.rubble;
        corpses = settings.corpses;
        fragmentsLive = CombatExtendedFragments.Live(settings.fragments);
        if (DestroysBuildings) StripConstructedRoofs();
        Snapshot();
    }

    /// <summary>Constructed roofs never shelter, and the blast takes them first: one pass, before any wall falls.</summary>
    void StripConstructedRoofs()
    {
        RoofGrid roofs = Map.roofGrid;
        foreach (IntVec3 cell in Map.AllCells)
        {
            RoofDef? roof = roofs.RoofAt(cell);
            if (roof == null || roof.isNatural) continue;
            roofs.SetRoof(cell, null);
            RoofsStripped++;
        }
    }

    void Snapshot()
    {
        List<Thing> all = Map.listerThings.AllThings;
        var chosen = new List<Thing>(all.Count);
        for (int i = 0; i < all.Count; i++)
        {
            Thing thing = all[i];
            if (!categories.Contains(thing.def.category) || !thing.Spawned) continue;
            RoofDef? roof = Map.roofGrid.RoofAt(thing.Position);
            if (ShelterRule.Sheltered(shelter, roof != null, roof?.isNatural ?? false, roof?.isThickRoof ?? false))
            {
                Sheltered++;
                continue;
            }
            chosen.Add(thing);
        }
        IntVec3 centre = Map.Center;
        var ranks = new long[chosen.Count];
        for (int i = 0; i < chosen.Count; i++)
        {
            IntVec3 position = chosen[i].Position;
            ranks[i] = SweepOrderRule.Rank(position.x, position.z, centre.x, centre.z);
        }
        Thing[] ordered = chosen.ToArray();
        Array.Sort(ranks, ordered);
        targets = new List<Thing>(ordered);
        next = 0;
        Total += ordered.Length;
    }

    protected override void BeginSlice()
    {
        // The missile mod's own context for its damage patches: no damage multipliers, and the missile's name in the logs.
        previousIgnore = IRBM_DamageContext.ignoreDamageMultipliers;
        previousLabel = IRBM_DamageContext.currentDamageSourceLabel;
        IRBM_DamageContext.ignoreDamageMultipliers = true;
        IRBM_DamageContext.currentDamageSourceLabel = SourceLabel;
        if (IRBM_DefOf.IRBM_MissileWeapon != null && !SourceLabel.NullOrEmpty()) IRBM_DefOf.IRBM_MissileWeapon.label = SourceLabel;
        heldRooms = MissilePatchesMod.Settings.holdRoomRebuilds && RoomRebuildHold.TryHold(Map);
        fragmentChange = CombatExtendedFragments.Apply(MissilePatchesMod.Settings.fragments);
    }

    protected override void EndSlice()
    {
        CombatExtendedFragments.Restore(fragmentChange);
        fragmentChange = default;
        if (heldRooms) RoomRebuildHold.Release(Map);
        heldRooms = false;
        IRBM_DamageContext.ignoreDamageMultipliers = previousIgnore;
        IRBM_DamageContext.currentDamageSourceLabel = previousLabel;
        if (!longMessageShown && !Finished && SecondsSinceStart >= IrbmBudgetRule.LongBlastSeconds && MissilePatchesMod.Settings.messages)
        {
            longMessageShown = true;
            Messages.Message("PhobosMissilePatches_BlastStillRunning".Translate(PlaceLabel.Named("PLACE")), MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    protected override void Step()
    {
        if (targets == null) return;
        if (next >= targets.Count)
        {
            passesDone++;
            Snapshot();
            return;
        }
        Thing thing = targets[next++];
        try
        {
            HitOne(thing);
        }
        catch (Exception error)
        {
            // The missile mod swallows every per-thing exception; here they are counted and the first is reported once.
            Errors++;
            FirstError ??= error.ToString();
        }
    }

    void HitOne(Thing thing)
    {
        if (thing.Destroyed || !thing.Spawned || thing.Map != Map)
        {
            Gone++;
            return;
        }
        if (CompleteDismantle && IrbmDismantle.TryDismantle(thing, Map)) return;
        var damage = new DamageInfo(DamageDef, Amount, ArmorPen, -1f, Launcher, null, IRBM_DefOf.IRBM_MissileWeapon);
        if (thing is Pawn pawn)
        {
            if (LockCorePart && pawn.RaceProps.body != null) damage.SetHitPart(pawn.RaceProps.body.corePart);
            pawn.TakeDamage(damage);
            PawnsHit++;
            if (pawn.Dead && corpses == CorpseMode.Vaporised && pawn.Corpse is { Spawned: true } corpse) corpse.Destroy();
            return;
        }
        ThingHit hit = HitRule.Choose(false, thing.def.category == ThingCategory.Building, DamageDef.harmsHealth, thing.def.useHitPoints,
            thing.def.destroyable, thing.HitPoints, Amount, rubble, fragmentsLive);
        switch (hit)
        {
            case ThingHit.Vanish:
                EmptyHeldThings(thing);
                thing.Destroy(DestroyMode.Vanish);
                Vaporised++;
                break;
            case ThingHit.KillFinalize:
                thing.Destroy(DestroyMode.KillFinalize);
                Wrecked++;
                break;
            default:
                thing.TakeDamage(damage);
                Damaged++;
                break;
        }
    }

    /// <summary>
    /// A container's contents vanish with it, as in Dubs Rimatomics' nuke; a living pawn inside (a sleeper in a casket) is killed
    /// first, so it dies rather than being sent off the map alive.
    /// </summary>
    static void EmptyHeldThings(Thing thing)
    {
        if (thing is not IThingHolder holder) return;
        ThingOwner? owner = holder.GetDirectlyHeldThings();
        if (owner == null || owner.Count == 0) return;
        foreach (Thing held in owner.ToList())
        {
            if (held is Pawn { Dead: false } pawn)
            {
                try
                {
                    pawn.Kill(null);
                }
                catch (Exception error)
                {
                    Log.Warning(MissilePatchesMod.LogPrefix + " A pawn in " + thing.LabelShort + " could not be killed: " + error.Message);
                }
            }
        }
        owner.ClearAndDestroyContents();
    }

    protected override void Finish()
    {
        string line = MissilePatchesMod.LogPrefix + " Blast over " + PlaceLabel + ": " + Total + " things in " + SecondsSinceStart.ToString("0.0")
            + " s over " + Frames + " frame" + (Frames == 1 ? "" : "s") + " (" + WorkMs.ToString("0") + " ms of work)"
            + ": vaporised " + Vaporised + ", wrecked " + Wrecked + ", damaged " + Damaged + ", pawns hit " + PawnsHit
            + ", sheltered " + Sheltered + " (" + shelter + "), already gone " + Gone + ", roofs stripped " + RoofsStripped
            + (Passes > 1 ? ", passes " + Passes : "")
            + (Errors > 0 ? ", " + Errors + " error" + (Errors == 1 ? "" : "s") + " swallowed; the first: " + FirstError : "")
            + ".";
        if (Errors > 0) Log.Warning(line); else Log.Message(line);
        if (longMessageShown)
        {
            Messages.Message("PhobosMissilePatches_BlastSpent".Translate(PlaceLabel.Named("PLACE")), MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    protected override void Dropped() =>
        Log.Message(MissilePatchesMod.LogPrefix + " The blast over " + PlaceLabel + " was dropped at " + next + " of " + Count + ".");
}

/// <summary>
/// The missile mod's dismantling warhead (it refunds buildings, harvests plants and salvages fresh corpses instead of destroying
/// them), repeated from its StrategicImpactCountdown.ApplyStrategicDamage so the outcome is the same.
/// </summary>
internal static class IrbmDismantle
{
    /// <summary>True if the thing was handled; false to fall through to the ordinary hit (pawns, and items that are not fresh corpses).</summary>
    public static bool TryDismantle(Thing thing, Map map)
    {
        switch (thing)
        {
            case Building building:
                DismantleBuilding(building, map);
                return true;
            case Plant plant:
                int yield = plant.YieldNow();
                if (yield > 0 && plant.def.plant.harvestedThingDef != null) Drop(plant.def.plant.harvestedThingDef, yield, plant.Position, map);
                plant.Destroy(DestroyMode.KillFinalize);
                return true;
        }
        if (thing.def.category != ThingCategory.Item) return false;
        if (thing is Corpse { InnerPawn: not null } corpse && corpse.GetRotStage() == RotStage.Fresh) SalvageCorpse(corpse, map);
        return true;
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
}
