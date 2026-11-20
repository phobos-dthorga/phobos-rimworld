using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Bulk;

/// <summary>
/// Anything that holds loose goods in bulk and can pour them (Framework 0.182.0; docs/development/farming-batch-one.md, "The bulk grain
/// hold"): a field bin or silo (CompPhobosBulkStore), a grain tank or chaser bin on a vehicle (D'thorga Framework's vehicle part). Registered
/// on its map while spawned, so a pour finds the holds in reach without scanning the map.
/// </summary>
public interface IBulkHold
{
    /// <summary>The thing the hold is part of.</summary>
    ThingWithComps HoldThing { get; }
    BulkContents Contents { get; }
    float CapacityKg { get; }
    /// <summary>Whether it takes more than one product at once (a bin, a silo) or keeps to one (a combine's tank).</summary>
    bool MixesProducts { get; }
    /// <summary>How fast its auger or spout pours, kilograms a second.</summary>
    float PourKgPerSecond { get; }
    /// <summary>How far its auger reaches beyond its footprint, in cells.</summary>
    int ReachCells { get; }
    /// <summary>The share of the normal rate its contents spoil at: below one in an aired silo.</summary>
    float StoreFactor { get; }
    /// <summary>Why it cannot pour now (a vehicle moving, an auger broken), or null.</summary>
    string? CannotPour();
    /// <summary>Why it cannot take a pour now, or null.</summary>
    string? CannotReceive();
    /// <summary>Called after its contents change, so a vehicle's engine feels the weight.</summary>
    void ContentsChanged();
}

/// <summary>
/// Marks a kind of goods as loose bulk an auger takes in from the ground although no crop yields it as its harvest: seed grain. On the
/// goods' own Def, in XML (Framework 0.202.0).
/// </summary>
public class PhobosBulkGoodsExtension : DefModExtension
{
}

/// <summary>A pour under way from one hold: into another, or onto the ground beside it. Saved with the hold pouring.</summary>
public class BulkPouring : IExposable
{
    /// <summary>The hold poured into, or null for the ground.</summary>
    public ThingWithComps? into;
    public bool active;
    /// <summary>How much this pour has moved, in units, for the message at its end.</summary>
    public int moved;
    /// <summary>Framework 0.201.0: the product its auger takes in from the ground, while loading instead of pouring; null otherwise.</summary>
    public ThingDef? loading;
    /// <summary>Whether this pour goes onto the ground (Framework 0.202.0). Said outright, so a pour into a hold that is gone by the next
    /// load, or a load whose product's mod was removed, ends where it stood and never turns into a pour onto the ground.</summary>
    public bool toGround;

    public void ExposeData()
    {
        Scribe_References.Look(ref into, "into");
        Scribe_Values.Look(ref active, "active");
        Scribe_Values.Look(ref moved, "moved");
        Scribe_Defs.Look(ref loading, "loading");
        Scribe_Values.Look(ref toGround, "toGround");
    }
}

/// <summary>
/// Pouring between holds and out of them (Framework 0.182.0), one step a rare tick while a pour is under way: no more than is held, than
/// fits where it goes, or than the auger moves in a step (BulkHoldRule.Pour). Both must be standing and within the auger's reach. Every
/// pour says what it did when it ends, and every refusal says why.
/// </summary>
public static class BulkPour
{
    /// <summary>Game seconds in a rare tick, at the game's 60 ticks a second.</summary>
    const float SecondsPerRareTick = GenTicks.TickRareInterval / 60f;

    public static void Register(IBulkHold hold, Map? map) => MapRegistry<IBulkHold>.Add(map, hold);

    public static void Unregister(IBulkHold hold, Map? map) => MapRegistry<IBulkHold>.Remove(map, hold);

    public static IBulkHold? HoldOf(Thing? thing) => thing is ThingWithComps t ? t.AllComps.OfType<IBulkHold>().FirstOrDefault() : null;

    /// <summary>Whether two holds stand within the pouring one's reach.</summary>
    public static bool InReach(IBulkHold from, IBulkHold to)
    {
        if (!from.HoldThing.Spawned || !to.HoldThing.Spawned || from.HoldThing.Map != to.HoldThing.Map) return false;
        CellRect a = from.HoldThing.OccupiedRect(), b = to.HoldThing.OccupiedRect();
        return BulkHoldRule.WithinReach(a.minX, a.minZ, a.maxX, a.maxZ, b.minX, b.minZ, b.maxX, b.maxZ, from.ReachCells);
    }

    /// <summary>The other holds on the map within reach of this one, nearest first.</summary>
    public static List<IBulkHold> InReachOf(IBulkHold from)
    {
        var found = new List<IBulkHold>();
        foreach (IBulkHold other in MapRegistry<IBulkHold>.On(from.HoldThing.MapHeld))
        {
            if (other != from && InReach(from, other)) found.Add(other);
        }
        IntVec3 at = from.HoldThing.Position;
        found.Sort((x, y) => x.HoldThing.Position.DistanceToSquared(at).CompareTo(y.HoldThing.Position.DistanceToSquared(at)));
        return found;
    }

    /// <summary>Why this pour cannot go ahead now, or null.</summary>
    public static string? Refusal(IBulkHold from, IBulkHold? to)
    {
        NamedArgument name = from.HoldThing.LabelShort.Named("HOLD");
        if (from.Contents.Empty) return "PhobosBulk_Empty".Translate(name).Resolve();
        if (from.CannotPour() is { } cannot) return cannot;
        if (to == null) return null;
        NamedArgument other = to.HoldThing.LabelShort.Named("OTHER");
        if (!InReach(from, to)) return "PhobosBulk_OutOfReach".Translate(name, other, from.ReachCells.Named("CELLS")).Resolve();
        if (to.CannotReceive() is { } refused) return refused;
        if (!from.Contents.Entries.Any(e => e.def != null && to.Contents.Takes(e.def, to.MixesProducts) && to.Contents.RoomFor(e.def, to.CapacityKg) > 0))
        {
            return "PhobosBulk_NoRoom".Translate(other, name).Resolve();
        }
        return null;
    }

    /// <summary>Starts a pour, saying why not if it cannot.</summary>
    public static void Start(IBulkHold from, BulkPouring state, IBulkHold? to)
    {
        if (Refusal(from, to) is { } why)
        {
            Messages.Message(why, from.HoldThing, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        state.into = to?.HoldThing;
        state.toGround = to == null;
        state.loading = null;
        state.active = true;
        state.moved = 0;
        string where = to?.HoldThing.LabelShort ?? "PhobosBulk_TheGround".Translate().Resolve();
        Messages.Message("PhobosBulk_Started".Translate(from.HoldThing.LabelShort.Named("HOLD"), where.Named("INTO")).Resolve(), from.HoldThing,
            MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Stops a pour by hand.</summary>
    public static void Stop(IBulkHold from, BulkPouring state) => End(from, state, "PhobosBulk_EndStopped".Translate().Resolve());

    /// <summary>Ends a pour because the hold has left the map, and says so.</summary>
    public static void LeftMap(IBulkHold from, BulkPouring state) => End(from, state, "PhobosBulk_EndLeftMap".Translate().Resolve());

    /// <summary>One step of a pour under way; call from the pouring hold's rare tick.</summary>
    public static void Step(IBulkHold from, BulkPouring state)
    {
        if (!state.active) return;
        if (state.loading != null)
        {
            LoadStep(from, state);
            return;
        }
        IBulkHold? to = state.into != null ? HoldOf(state.into) : null;
        if (state.into == null ? !state.toGround : (to == null || state.into.Destroyed))
        {
            End(from, state, "PhobosBulk_EndGone".Translate().Resolve());
            return;
        }
        if (Refusal(from, to) is { } why)
        {
            End(from, state, from.Contents.Empty ? "PhobosBulk_EndEmptied".Translate().Resolve() : why);
            return;
        }
        BulkEntry? entry = to == null
            ? from.Contents.Entries.FirstOrDefault()
            : from.Contents.Entries.FirstOrDefault(e => e.def != null && to.Contents.Takes(e.def, to.MixesProducts) && to.Contents.RoomFor(e.def, to.CapacityKg) > 0);
        if (entry?.def == null)
        {
            End(from, state, "PhobosBulk_EndNoRoom".Translate().Resolve());
            return;
        }
        ThingDef def = entry.def;
        int step = BulkHoldRule.UnitsPerStep(from.PourKgPerSecond, SecondsPerRareTick, BulkContents.MassOf(def));
        int moved = to == null ? PourOnGround(from, def, step) : PourInto(from, to, def, step);
        if (moved <= 0)
        {
            End(from, state, to == null ? "PhobosBulk_EndGroundFull".Translate().Resolve() : "PhobosBulk_EndNoRoom".Translate().Resolve());
            return;
        }
        state.moved += moved;
        from.ContentsChanged();
        to?.ContentsChanged();
    }

    static int PourInto(IBulkHold from, IBulkHold to, ThingDef def, int step)
    {
        // The receiving hold is aged up to now first, so what it held is aged for its own time and the fresh load joins at now (the
        // fault-read of 20 Nov 2026: a bin last read weeks ago aged the fresh grain for all those weeks).
        Age(to);
        int room = to.Contents.RoomFor(def, to.CapacityKg);
        int count = BulkHoldRule.Pour(from.Contents.CountOf(def), room, step);
        int taken = from.Contents.Take(def, count, out float used);
        int added = to.Contents.Add(def, taken, used, to.CapacityKg, to.MixesProducts);
        if (added < taken) from.Contents.Add(def, taken - added, used, float.MaxValue, true);
        return added;
    }

    /// <summary>
    /// The most stacks a step lays on the ground (the fault-read of 20 Nov 2026: a fast auger laid about 185 stacks a rare tick). An
    /// auger pouring onto the ground is slowed to this many stacks a step, as a heap builds. Authored [A].
    /// </summary>
    const int MostStacksAStep = 6;

    /// <summary>Pours stacks onto free ground beside the hold, as many as a step moves, up to a few stacks a step.</summary>
    static int PourOnGround(IBulkHold from, ThingDef def, int step)
    {
        Thing holder = from.HoldThing;
        int left = System.Math.Min(System.Math.Min(step, MostStacksAStep * System.Math.Max(1, def.stackLimit)), from.Contents.CountOf(def));
        int moved = 0;
        IntVec3 spout = SpoutCell(holder);
        while (left > 0)
        {
            Thing? stack = from.Contents.MakeStack(def, left);
            if (stack == null) break;
            int count = stack.stackCount;
            if (!GenPlace.TryPlaceThing(stack, spout, holder.Map, ThingPlaceMode.Near, out Thing _, null,
                    c => !holder.OccupiedRect().Contains(c)))
            {
                // The game may have merged part of the stack into a heap before giving up (the fault-read of 21 Nov 2026, B8): that part
                // was poured, and only what is still in hand goes back.
                int inHand = stack.Destroyed ? 0 : stack.stackCount;
                moved += count - inHand;
                if (inHand > 0) from.Contents.Return(stack);
                break;
            }
            moved += count;
            left -= count;
        }
        return moved;
    }

    /// <summary>
    /// Where a hold's spout lets go: the hold's own cell where one can stand there (a vehicle, a bin one can walk past), else the nearest
    /// cell beside it one can stand on. A silo fills its cells whole, and the game's search for free ground from such a cell reaches
    /// through walls (the fault-read of 21 Nov 2026, B7): from a cell beside it, heaps stay on the silo's own side of a wall.
    /// </summary>
    static IntVec3 SpoutCell(Thing holder)
    {
        Map map = holder.Map;
        if (holder.Position.Standable(map)) return holder.Position;
        IntVec3 best = holder.Position;
        int bestDistance = int.MaxValue;
        foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(holder))
        {
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.Fogged(map)) continue;
            int distance = cell.DistanceToSquared(holder.Position);
            if (distance < bestDistance)
            {
                best = cell;
                bestDistance = distance;
            }
        }
        return best;
    }

    /// <summary>The most heaps a hold that is destroyed spills (the fault-read of 20 Nov 2026: a full silo is about two million units,
    /// which no map can lay down at once). What does not fit in them is lost, and said so. Authored [A].</summary>
    public const int MostSpilledStacks = 40;

    /// <summary>
    /// A hold is gone: what it held spills onto the ground around where it stood, as many heaps as will go, and the rest is named as
    /// lost. One place for buildings and vehicles (the fault-read of 21 Nov 2026, B2: a combine, a chaser bin or a drill destroyed with
    /// grain aboard had spilled nothing and said nothing).
    /// </summary>
    public static void Spill(IBulkHold hold, IntVec3 at, Map map)
    {
        BulkContents contents = hold.Contents;
        if (contents.Empty) return;
        int stacks = 0;
        foreach (BulkEntry entry in new List<BulkEntry>(contents.Entries))
        {
            if (entry.def is not { } def) continue;
            while (contents.CountOf(def) > 0 && stacks < MostSpilledStacks)
            {
                Thing? stack = contents.MakeStack(def, contents.CountOf(def));
                if (stack == null) break;
                if (!GenPlace.TryPlaceThing(stack, at, map, ThingPlaceMode.Near))
                {
                    // What is still in hand goes back into the tally, so the loss is named in full.
                    if (!stack.Destroyed && stack.stackCount > 0) contents.Return(stack);
                    stacks = MostSpilledStacks;
                    break;
                }
                stacks++;
            }
        }
        if (contents.Empty) return;
        Messages.Message("PhobosBulk_SpillLost".Translate(hold.HoldThing.LabelShort.Named("HOLD"), contents.Summary.Named("WHAT")).Resolve(),
            new TargetInfo(at, map), MessageTypeDefOf.NegativeEvent);
    }

    // ---- Loading from the ground (Framework 0.201.0) ----

    static HashSet<ThingDef>? looseGoods;

    /// <summary>
    /// The kinds of goods an auger takes in: what a crop yields as its harvest (grain, pulses, hay; never a tree's), and goods marked as
    /// loose bulk in their own Def (seed grain). The owner's choice of 21 Nov 2026, after the fault-read found every raw resource counted:
    /// a silo beside a stockpile offered to take in steel and silver. Worked out once.
    /// </summary>
    static HashSet<ThingDef> LooseGoods
    {
        get
        {
            if (looseGoods != null) return looseGoods;
            looseGoods = new HashSet<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.plant is { IsTree: false, harvestedThingDef: { } harvest }) looseGoods.Add(harvest);
                if (def.HasModExtension<PhobosBulkGoodsExtension>()) looseGoods.Add(def);
            }
            return looseGoods;
        }
    }

    /// <summary>Counts a kind of goods as loose bulk, for a mod that knows of one no Def says (another mod's second harvest product).</summary>
    public static void CountAsLoose(ThingDef def) => LooseGoods.Add(def);

    /// <summary>
    /// Whether a thing is loose goods an auger can take in from a heap: a stack of what a crop yields, or of seed, not forbidden. Anything
    /// else (steel, a weapon, a body, a building) never goes into a hold.
    /// </summary>
    public static bool Loose(Thing thing) =>
        thing.def.category == ThingCategory.Item && thing.def.stackLimit > 1 && LooseGoods.Contains(thing.def) && !thing.IsForbidden(Faction.OfPlayer);

    /// <summary>The loose goods within the auger's reach of this hold, by product, nearest stacks first.</summary>
    public static Dictionary<ThingDef, List<Thing>> GroundInReach(IBulkHold hold)
    {
        var found = new Dictionary<ThingDef, List<Thing>>();
        Thing holder = hold.HoldThing;
        if (!holder.Spawned) return found;
        Map map = holder.Map;
        // The same reach as between two holds: up to that many cells may lie between (it had been one cell less here). Never a cell in
        // fog, nor one in another room than the spout's: an auger does not reach through a wall (the fault-read of 21 Nov 2026, B7, B14).
        CellRect around = holder.OccupiedRect().ExpandedBy(hold.ReachCells + 1).ClipInsideMap(map);
        CellRect own = holder.OccupiedRect();
        Room? room = SpoutCell(holder).GetRoom(map);
        foreach (IntVec3 cell in around)
        {
            if (own.Contains(cell) || cell.Fogged(map)) continue;
            if (room != null && cell.GetRoom(map) is { } theirs && theirs != room) continue;
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (!Loose(things[i])) continue;
                if (!found.TryGetValue(things[i].def, out List<Thing>? list)) found[things[i].def] = list = new List<Thing>();
                list.Add(things[i]);
            }
        }
        return found;
    }

    /// <summary>Why this hold cannot take in this product from the ground now, or null.</summary>
    public static string? LoadRefusal(IBulkHold hold, ThingDef def)
    {
        NamedArgument name = hold.HoldThing.LabelShort.Named("HOLD");
        if (hold.CannotReceive() is { } refused) return refused;
        if (!hold.Contents.Takes(def, hold.MixesProducts) || hold.Contents.RoomFor(def, hold.CapacityKg) <= 0)
        {
            return "PhobosBulk_LoadNoRoom".Translate(name, def.label.Named("WHAT")).Resolve();
        }
        return null;
    }

    /// <summary>Starts taking in a product from the ground, saying why not if it cannot.</summary>
    public static void StartLoading(IBulkHold hold, BulkPouring state, ThingDef def)
    {
        if (LoadRefusal(hold, def) is { } why)
        {
            Messages.Message(why, hold.HoldThing, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        state.into = null;
        state.toGround = false;
        state.loading = def;
        state.active = true;
        state.moved = 0;
        Messages.Message("PhobosBulk_LoadStarted".Translate(hold.HoldThing.LabelShort.Named("HOLD"), def.label.Named("WHAT")).Resolve(),
            hold.HoldThing, MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>One step of loading: as much as the auger moves in a step, from the nearest stacks in reach, as fresh as they are.</summary>
    static void LoadStep(IBulkHold hold, BulkPouring state)
    {
        ThingDef def = state.loading!;
        if (LoadRefusal(hold, def) is { } why)
        {
            End(hold, state, why);
            return;
        }
        if (!GroundInReach(hold).TryGetValue(def, out List<Thing>? stacks) || stacks.Count == 0)
        {
            End(hold, state, "PhobosBulk_EndNothingLeft".Translate().Resolve());
            return;
        }
        Age(hold);
        int left = BulkHoldRule.UnitsPerStep(hold.PourKgPerSecond, SecondsPerRareTick, BulkContents.MassOf(def));
        int moved = 0;
        foreach (Thing stack in stacks)
        {
            if (left <= 0) break;
            int count = System.Math.Min(System.Math.Min(left, stack.stackCount), hold.Contents.RoomFor(def, hold.CapacityKg));
            if (count <= 0) break;
            int added = hold.Contents.Add(def, count, UsedShare(stack), hold.CapacityKg, hold.MixesProducts);
            if (added <= 0) break;
            stack.SplitOff(added).Destroy();
            moved += added;
            left -= added;
        }
        if (moved <= 0)
        {
            End(hold, state, "PhobosBulk_EndNoRoom".Translate().Resolve());
            return;
        }
        state.moved += moved;
        hold.ContentsChanged();
        if (Seen.Wants("bulk.load-from-ground", hold.HoldThing)) Seen.Note("bulk.load-from-ground", hold.HoldThing, def.defName);
    }

    /// <summary>How much of its life a stack has used: its rot progress against when it starts to rot; nought for what never rots.</summary>
    static float UsedShare(Thing stack)
    {
        CompRottable? rot = stack.TryGetComp<CompRottable>();
        if (rot == null || rot.PropsRot.TicksToRotStart <= 0) return 0f;
        return Mathf.Clamp01(rot.RotProgress / rot.PropsRot.TicksToRotStart);
    }

    static void End(IBulkHold from, BulkPouring state, string why)
    {
        if (!state.active) return;
        if (state.loading != null)
        {
            Messages.Message("PhobosBulk_LoadDone".Translate(from.HoldThing.LabelShort.Named("HOLD"), state.moved.Named("COUNT"),
                state.loading.label.Named("WHAT")).Resolve() + " " + why, from.HoldThing, state.moved > 0 ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput);
            state.active = false;
            state.loading = null;
            state.moved = 0;
            return;
        }
        state.active = false;
        string where = state.into?.LabelShort ?? (state.toGround ? "PhobosBulk_TheGround" : "PhobosBulk_WhatItPouredInto").Translate().Resolve();
        Messages.Message("PhobosBulk_Done".Translate(from.HoldThing.LabelShort.Named("HOLD"), state.moved.Named("COUNT"), where.Named("INTO")).Resolve()
            + " " + why, from.HoldThing, state.moved > 0 ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput);
        state.into = null;
        state.moved = 0;
    }

    // ---- Freshness ----

    /// <summary>Ages a hold's contents to now at its temperature, and says if anything spoiled.</summary>
    public static void Age(IBulkHold hold)
    {
        Thing thing = hold.HoldThing;
        if (!thing.SpawnedOrAnyParentSpawned) return;
        IReadOnlyList<string> spoiled = hold.Contents.Age(Find.TickManager.TicksGame, thing.AmbientTemperature, hold.StoreFactor);
        if (spoiled.Count == 0) return;
        hold.ContentsChanged();
        Messages.Message("PhobosBulk_Spoiled".Translate(thing.LabelShort.Named("HOLD"), spoiled.ToCommaList().Named("WHAT")).Resolve(), thing,
            MessageTypeDefOf.NegativeEvent);
    }

    // ---- The buttons ----

    /// <summary>Our pour picture (the owner's pick of 20 Nov 2026), in the Framework's Common textures.</summary>
    const string PourIcon = "Phobos/Framework/Bulk/Pour";
    /// <summary>The game's own eject picture (the cryptosleep casket's), should ours ever be missing; confirmed in the game's code.</summary>
    const string PourFallbackIcon = "UI/Commands/PodEject";
    /// <summary>The game's own cancel picture; confirmed in the game's code.</summary>
    const string StopIcon = "UI/Designators/Cancel";

    static readonly Dictionary<string, Texture2D> icons = new();

    /// <summary>A picture, else the game's eject picture, else a plain white square: never the red X. Looked up once each: the buttons are
    /// drawn every frame while a hold is selected, and a look-up walks every running mod (the fault-read of 21 Nov 2026, B12).</summary>
    static Texture2D Icon(string path)
    {
        if (!icons.TryGetValue(path, out Texture2D? icon))
        {
            icons[path] = icon = ContentFinder<Texture2D>.Get(path, reportFailure: false)
                ?? ContentFinder<Texture2D>.Get(PourFallbackIcon, reportFailure: false) ?? BaseContent.WhiteTex;
        }
        return icon;
    }

    // Whether anything loose lies in reach of the hold last asked about, kept for a second: the Load button asks every frame.
    static IBulkHold? groundAskedFor;
    static int groundAskedTick = -1;
    static bool groundAny;

    static bool AnyGroundInReach(IBulkHold hold)
    {
        int now = Find.TickManager.TicksGame;
        if (hold != groundAskedFor || now < groundAskedTick || now - groundAskedTick >= 60)
        {
            groundAskedFor = hold;
            groundAskedTick = now;
            groundAny = GroundInReach(hold).Count > 0;
        }
        return groundAny;
    }

    /// <summary>The pour buttons: pour into a hold in reach, pour onto the ground, or stop the pour under way.</summary>
    public static IEnumerable<Gizmo> Gizmos(IBulkHold hold, BulkPouring state)
    {
        NamedArgument name = hold.HoldThing.LabelShort.Named("HOLD");
        if (state.active)
        {
            yield return new Command_Action
            {
                defaultLabel = "PhobosBulk_GizmoStop".Translate().Resolve(),
                defaultDesc = "PhobosBulk_GizmoStopDesc".Translate(name).Resolve(),
                icon = Icon(StopIcon),
                action = () => Stop(hold, state),
            };
            yield break;
        }
        var pour = new Command_Action
        {
            defaultLabel = "PhobosBulk_GizmoPour".Translate().Resolve(),
            defaultDesc = "PhobosBulk_GizmoPourDesc".Translate(name, hold.ReachCells.Named("CELLS"), hold.Contents.Summary.Named("WHAT")).Resolve(),
            icon = Icon(PourIcon),
            action = () => Find.WindowStack.Add(new FloatMenu(PourOptions(hold, state))),
        };
        if (hold.Contents.Empty) pour.Disable("PhobosBulk_Empty".Translate(name).Resolve());
        else if (hold.CannotPour() is { } cannot) pour.Disable(cannot);
        yield return pour;
        // Framework 0.201.0: the auger takes in loose goods lying within its reach (grain kept back in stacks, bought seed).
        var load = new Command_Action
        {
            defaultLabel = "PhobosBulk_GizmoLoad".Translate().Resolve(),
            defaultDesc = "PhobosBulk_GizmoLoadDesc".Translate(name, hold.ReachCells.Named("CELLS")).Resolve(),
            icon = Icon(LoadIcon),
            action = () => Find.WindowStack.Add(new FloatMenu(LoadOptions(hold, state))),
        };
        if (hold.CannotReceive() is { } refused) load.Disable(refused);
        else if (!AnyGroundInReach(hold)) load.Disable("PhobosBulk_NothingInReach".Translate(name, hold.ReachCells.Named("CELLS")).Resolve());
        yield return load;
    }

    /// <summary>The game's own load picture (the transport pod's), confirmed in the game's code.</summary>
    const string LoadIcon = "UI/Commands/LoadTransporter";

    static List<FloatMenuOption> LoadOptions(IBulkHold hold, BulkPouring state)
    {
        var options = new List<FloatMenuOption>();
        foreach (KeyValuePair<ThingDef, List<Thing>> product in GroundInReach(hold).OrderBy(p => p.Key.label))
        {
            int count = product.Value.Sum(t => t.stackCount);
            string? refused = LoadRefusal(hold, product.Key);
            options.Add(new FloatMenuOption(refused == null
                    ? "PhobosBulk_LoadOption".Translate(product.Key.label.Named("WHAT"), count.Named("COUNT")).Resolve()
                    : "PhobosBulk_LoadOptionRefused".Translate(product.Key.label.Named("WHAT"), refused.Named("WHY")).Resolve(),
                refused == null ? () => StartLoading(hold, state, product.Key) : null));
        }
        return options;
    }

    static List<FloatMenuOption> PourOptions(IBulkHold hold, BulkPouring state)
    {
        var options = new List<FloatMenuOption>();
        foreach (IBulkHold other in InReachOf(hold))
        {
            string? refused = Refusal(hold, other);
            var option = new FloatMenuOption(refused == null
                    ? "PhobosBulk_PourInto".Translate(other.HoldThing.LabelShort.Named("OTHER")).Resolve()
                    : "PhobosBulk_PourIntoRefused".Translate(other.HoldThing.LabelShort.Named("OTHER"), refused.Named("WHY")).Resolve(),
                refused == null ? () => Start(hold, state, other) : null);
            options.Add(option);
        }
        options.Add(new FloatMenuOption("PhobosBulk_PourOnGround".Translate().Resolve(), () => Start(hold, state, null)));
        return options;
    }

    /// <summary>The hold's line for its inspect text: how full, what it holds, and a pour under way.</summary>
    public static string InspectLine(IBulkHold hold, BulkPouring state)
    {
        float kg = hold.Contents.Kilograms;
        string line = hold.Contents.Empty
            ? "PhobosBulk_LineEmpty".Translate(hold.CapacityKg.ToString("N0").Named("CAPACITY")).Resolve()
            : "PhobosBulk_Line".Translate(kg.ToString("N0").Named("MASS"), hold.CapacityKg.ToString("N0").Named("CAPACITY"),
                hold.Contents.Summary.Named("WHAT")).Resolve();
        if (state.active && state.loading != null)
        {
            line += "\n" + "PhobosBulk_LineLoading".Translate(state.loading.label.Named("WHAT")).Resolve();
        }
        else if (state.active)
        {
            string where = state.into?.LabelShort ?? (state.toGround ? "PhobosBulk_TheGround" : "PhobosBulk_WhatItPouredInto").Translate().Resolve();
            line += "\n" + "PhobosBulk_LinePouring".Translate(where.Named("INTO")).Resolve();
        }
        return line;
    }
}
