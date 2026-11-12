using DubsBadHygiene;
using Phobos.Framework.Persistence;
using RimWorld;
using Verse;

namespace Phobos.Framework.Hygiene;

/// <summary>Dubs Bad Hygiene's side of a <see cref="PipeUnderlay"/>: what counts as plumbing on a cell, and the pipe to lay.</summary>
public static class DubsPipes
{
    /// <summary>Dubs' hidden plumbing, the pipe laid beneath things: it joins the net, shows in the plumbing overlay and draws nothing on the ground (owner,
    /// 13 Nov 2026, seeing grids of pipe under parked aircraft on 0.108.0: "You need to use the Hidden versions of them"). Framework 0.107.0 and 0.108.0 laid
    /// the visible pipe, "sewagePipeStuff"; the pieces of ours they laid are swapped for this one at the next look (PipeUnderlay.Renew).</summary>
    const string Pipe = "sewagePipeHidden";

    /// <summary>Whether a thing is the hidden plumbing our versions before 0.107.0 laid, for the once-only adoption of pieces 0.107.0's loading fault orphaned.
    /// It is the pipe we lay again now, so what is adopted is simply kept.</summary>
    public static bool IsOldHidden(Thing thing) => thing.def.defName == Pipe;

    static ThingDef? def;
    static bool looked;

    /// <summary>The pipe laid beneath things, or null without Dubs Bad Hygiene's plumbing.</summary>
    public static ThingDef? Def
    {
        get
        {
            if (!looked)
            {
                def = DefDatabase<ThingDef>.GetNamedSilentFail(Pipe);
                looked = true;
            }
            return def;
        }
    }

    /// <summary>Dubs' water and sewage share one plumbing net, whose pipes are all in Sewage mode.</summary>
    public static bool IsPiped(IntVec3 cell, Map map)
    {
        var things = cell.GetThingList(map);
        for (int i = 0; i < things.Count; i++)
        {
            if (things[i].TryGetComp<CompPipe>() is { mode: PipeType.Sewage }) return true;
        }
        return false;
    }

    public static Thing? Make()
    {
        ThingDef? pipe = Def;
        return pipe == null ? null : ThingMaker.MakeThing(pipe, pipe.MadeFromStuff ? GenStuff.DefaultStuffFor(pipe) : null);
    }
}

public class CompProperties_PhobosPipeUnderlay : CompProperties
{
    public CompProperties_PhobosPipeUnderlay() => compClass = typeof(CompPhobosPipeUnderlay);
}

/// <summary>
/// A building's own plumbing beneath it. On load it lays them once every thing is spawned, so it sees the pipes already there and an older save converts by itself.
/// It stays out of the way when a building is lifted whole, such as on a gravship, which carries the pipes too.
/// </summary>
public class CompPhobosPipeUnderlay : ThingComp
{
    PipeUnderlay underlay = new();

    void Lay()
    {
        if (PlumbingFeed.LiteMode || !parent.Spawned) return;
        Map map = parent.Map;
        // Once, for a building saved before Framework 0.108.0: the hidden pipe its earlier versions laid, orphaned by 0.107.0's loading fault, is taken back.
        underlay.AdoptOnce(parent.OccupiedRect(), map, DubsPipes.IsOldHidden);
        underlay.Renew(DubsPipes.Def, DubsPipes.Make);
        underlay.Fill(parent.OccupiedRect(), map, cell => DubsPipes.IsPiped(cell, map), DubsPipes.Make);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (respawningAfterLoad) LongEventHandler.ExecuteWhenFinished(Lay);
        else Lay();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (mode != DestroyMode.WillReplace) underlay.Clear();
        base.PostDeSpawn(map, mode);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref underlay, "PhobosPipeUnderlay");
        underlay ??= new PipeUnderlay();
    }
}
