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

/// <summary>A pour under way from one hold: into another, or onto the ground beside it. Saved with the hold pouring.</summary>
public class BulkPouring : IExposable
{
    /// <summary>The hold poured into, or null for the ground.</summary>
    public ThingWithComps? into;
    public bool active;
    /// <summary>How much this pour has moved, in units, for the message at its end.</summary>
    public int moved;

    public void ExposeData()
    {
        Scribe_References.Look(ref into, "into");
        Scribe_Values.Look(ref active, "active");
        Scribe_Values.Look(ref moved, "moved");
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
        state.active = true;
        state.moved = 0;
        string where = to?.HoldThing.LabelShort ?? "PhobosBulk_TheGround".Translate().Resolve();
        Messages.Message("PhobosBulk_Started".Translate(from.HoldThing.LabelShort.Named("HOLD"), where.Named("INTO")).Resolve(), from.HoldThing,
            MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Stops a pour by hand.</summary>
    public static void Stop(IBulkHold from, BulkPouring state) => End(from, state, "PhobosBulk_EndStopped".Translate().Resolve());

    /// <summary>One step of a pour under way; call from the pouring hold's rare tick.</summary>
    public static void Step(IBulkHold from, BulkPouring state)
    {
        if (!state.active) return;
        IBulkHold? to = state.into != null ? HoldOf(state.into) : null;
        if (state.into != null && (to == null || state.into.Destroyed))
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
        int step = BulkHoldRule.UnitsPerStep(from.PourKgPerSecond, SecondsPerRareTick, def.BaseMass);
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
        while (left > 0)
        {
            Thing? stack = from.Contents.MakeStack(def, left);
            if (stack == null) break;
            int count = stack.stackCount;
            if (!GenPlace.TryPlaceThing(stack, holder.Position, holder.Map, ThingPlaceMode.Near, out Thing _, null,
                    c => !holder.OccupiedRect().Contains(c)))
            {
                from.Contents.Return(stack);
                break;
            }
            moved += count;
            left -= count;
        }
        return moved;
    }

    static void End(IBulkHold from, BulkPouring state, string why)
    {
        if (!state.active) return;
        state.active = false;
        string where = state.into?.LabelShort ?? "PhobosBulk_TheGround".Translate().Resolve();
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
        List<string> spoiled = hold.Contents.Age(Find.TickManager.TicksGame, thing.AmbientTemperature, hold.StoreFactor);
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

    /// <summary>A picture, else the game's eject picture, else a plain white square: never the red X.</summary>
    static Texture2D Icon(string path) => ContentFinder<Texture2D>.Get(path, reportFailure: false)
        ?? ContentFinder<Texture2D>.Get(PourFallbackIcon, reportFailure: false) ?? BaseContent.WhiteTex;

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
        if (state.active)
        {
            string where = state.into?.LabelShort ?? "PhobosBulk_TheGround".Translate().Resolve();
            line += "\n" + "PhobosBulk_LinePouring".Translate(where.Named("INTO")).Resolve();
        }
        return line;
    }
}
