using DubsBadHygiene;
using Phobos.Framework.Persistence;
using RimWorld;
using Verse;

namespace Phobos.Framework.Hygiene;

/// <summary>Dubs Bad Hygiene's side of a <see cref="PipeUnderlay"/>: what counts as plumbing on a cell, and the pipe to lay.</summary>
public static class DubsPipes
{
    const string HiddenPipe = "sewagePipeHidden";

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
        ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(HiddenPipe);
        return def == null ? null : ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
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
