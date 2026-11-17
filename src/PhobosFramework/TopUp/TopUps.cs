using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.TopUp;

/// <summary>
/// The service behind topping up (Framework 0.134.0; docs/development/top-up-design.md): which building Defs carry a comp a
/// <see cref="PhobosTopUpDef"/> names, what each such building wants now, and the hauling job that brings it. Found once after Defs
/// load; nothing is saved and nothing ticks. The work giver only calls it.
/// </summary>
public static class TopUps
{
    /// <summary>A building Def that carries a named comp, with the Def that names it.</summary>
    readonly struct Kind
    {
        public readonly ThingDef Building;
        public readonly PhobosTopUpDef TopUp;
        public readonly Type CompType;

        public Kind(ThingDef building, PhobosTopUpDef topUp, Type compType)
        {
            Building = building;
            TopUp = topUp;
            CompType = compType;
        }
    }

    static List<Kind>? kinds;
    static readonly HashSet<PhobosTopUpDef> reported = new();

    static List<Kind> Kinds
    {
        get
        {
            if (kinds != null) return kinds;
            kinds = new List<Kind>();
            foreach (PhobosTopUpDef topUp in DefDatabase<PhobosTopUpDef>.AllDefsListForReading)
            {
                Type? type = topUp.CompType;
                if (type == null)
                {
                    ReportProblem(topUp);
                    continue;
                }
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    // Buildings only (agent default): a store on something that walks about is another job.
                    if (def.category != ThingCategory.Building || def.comps == null) continue;
                    if (def.comps.Any(c => c.compClass != null && type.IsAssignableFrom(c.compClass))) kinds.Add(new Kind(def, topUp, type));
                }
            }
            return kinds;
        }
    }

    /// <summary>How many kinds of building are topped up, for the load line.</summary>
    public static int KindCount => Kinds.Count;

    /// <summary>The Defs that stand down, with why, for the load line.</summary>
    public static IEnumerable<PhobosTopUpDef> Problems => DefDatabase<PhobosTopUpDef>.AllDefsListForReading.Where(d => d.problem != null);

    /// <summary>Says once, in the log, that a Def's names no longer fit the code they name.</summary>
    public static void ReportProblem(PhobosTopUpDef topUp)
    {
        if (topUp.problem == null || !reported.Add(topUp)) return;
        FrameworkMod.Logger.Warn("Topping up stands down for " + topUp.defName + ": " + topUp.problem
            + ". The mod it names may have changed; its machines must be filled by hand until the Def is put right.");
    }

    /// <summary>Whether topping up runs at all: the player's setting, and at least one kind of building to top up.</summary>
    public static bool Active => FrameworkMod.Settings.topUpMachines && Kinds.Count > 0;

    /// <summary>The player's buildings on a map that want more of their material now, enough to be worth a walk.</summary>
    public static IEnumerable<Thing> Wanting(Map map, Faction faction)
    {
        foreach (Kind kind in Kinds)
        {
            List<Thing> things = map.listerThings.ThingsOfDef(kind.Building);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing.Faction != faction) continue;
                if (WantOf(thing, kind) is { } want && TopUpRule.Worth(want.Amount, want.LeastLoad, forced: false)) yield return thing;
            }
        }
    }

    /// <summary>What a building wants now, or null: it is not a kind we top up, or it wants nothing.</summary>
    public static TopUpWant? WantOf(Thing thing)
    {
        foreach (Kind kind in Kinds)
        {
            if (kind.Building == thing.def) return WantOf(thing, kind);
        }
        return null;
    }

    static TopUpWant? WantOf(Thing thing, Kind kind)
    {
        if (thing is not ThingWithComps withComps) return null;
        List<ThingComp> comps = withComps.AllComps;
        for (int i = 0; i < comps.Count; i++)
        {
            if (kind.CompType.IsInstanceOfType(comps[i])) return kind.TopUp.Read(comps[i]);
        }
        return null;
    }

    /// <summary>How much of a building's material other haulers are already carrying to it.</summary>
    public static int AlreadyComing(Thing building, Pawn except)
    {
        int coming = 0;
        List<Pawn> pawns = building.Map.mapPawns.SpawnedPawnsInFaction(except.Faction);
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (pawn == except) continue;
            Job? job = pawn.CurJob;
            if (job == null || job.def != JobDefOf.HaulToContainer || job.targetB.Thing != building) continue;
            coming += Math.Max(0, job.count);
        }
        return coming;
    }

    /// <summary>
    /// The job that tops a building up, or null with the reason set for the right-click menu. The same job the machine's own "fill"
    /// order gives (the game's haul-to-container, with the further stacks queued), so it fills exactly as it does by hand.
    /// </summary>
    public static Job? JobFor(Pawn pawn, Thing building, bool forced)
    {
        if (WantOf(building) is not { } want) return null;
        if (building.Faction != pawn.Faction || building.IsForbidden(pawn) || building.IsBurning()) return null;
        if (!TopUpRule.Worth(want.Amount, want.LeastLoad, forced)) return null;
        if (!pawn.CanReach(building, PathEndMode.Touch, Danger.Deadly)) return null;
        int toBring = TopUpRule.ToBring(want.Amount, AlreadyComing(building, pawn));
        if (!TopUpRule.Worth(toBring, want.LeastLoad, forced)) return null;
        List<Thing> found = HaulAIUtility.FindFixedIngredientCount(pawn, want.Material, toBring);
        if (found.NullOrEmpty())
        {
            JobFailReason.Is("PhobosFramework_TopUpNoneToFetch".Translate(want.Material.label.Named("MATERIAL")));
            return null;
        }
        Job job = HaulAIUtility.HaulToContainerJob(pawn, found[0], building);
        job.count = Math.Min(job.count, toBring);
        job.targetQueueB = found.Skip(1).Select(thing => new LocalTargetInfo(thing)).ToList();
        Seen.Note("framework.top-up", building, toBring + " " + want.Material.label);
        if (FrameworkMod.Logger.DebugEnabled)
        {
            FrameworkMod.Logger.Debug(pawn.LabelShort + " tops up " + building.LabelShort + " (" + building.ThingID + ") with " + toBring + " "
                + want.Material.defName + "; it wants " + want.Amount + ", least load " + want.LeastLoad);
        }
        return job;
    }
}

/// <summary>"Seen in play" notes for this part (Debug/SeenInPlay; AGENTS.md: a feature with a test row notes itself).</summary>
static class Seen
{
    public static void Note(string key, Thing? on = null, string? what = null)
    {
        if (!Debug.SeenInPlay.On) return;
        string? detail = on == null ? what : what == null ? on.LabelShort : on.LabelShort + ": " + what;
        Debug.SeenInPlay.Note("D'thorga Framework", FrameworkMod.ModVersion, key, detail);
    }
}

/// <summary>
/// Hauling work: tops up the buildings a <see cref="PhobosTopUpDef"/> names. Skipped at once on a map with nothing to top up, so it
/// costs a colony without such buildings one check per look for work.
/// </summary>
public class WorkGiver_PhobosTopUp : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !TopUps.Active || !TopUps.Wanting(pawn.Map, pawn.Faction).Any();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => TopUps.Wanting(pawn.Map, pawn.Faction);

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false) => TopUps.Active ? TopUps.JobFor(pawn, thing, forced) : null;
}
