using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The ham shack (Radio 0.68.0; docs/development/radio-qsl-in-play.md, part 2, the owner's card of 28 Nov 2026: a room role with a work
/// bonus): a room with a radio station and a QSL wall. Radio work done there (a lesson, a neighbour's call, work under advice) goes the
/// rules' share faster, and radio operators enjoy a good one.
/// <para>Cost: the room role is scored when the game scores rooms; the best shack's impressiveness for a map is kept a game hour, for the
/// mood; the bonus is one look at a room when a radio job runs.</para>
/// </summary>
public static class HamShack
{
    static readonly Dictionary<Map, (int hour, float best)> kept = new();

    public static bool In(Thing? thing) =>
        thing is { Spawned: true } && thing.GetRoom() is { } room && room.Role == RadioShackDefOf.PhobosRadio_HamShack;

    /// <summary>How much faster radio work goes at this thing's place: the rules' factor in a ham shack, else 1.</summary>
    public static float Factor(Thing? thing)
    {
        if (!In(thing)) return 1f;
        if (Seen.Wants("radio.ham-shack", thing)) Seen.Note("radio.ham-shack", thing, thing!.LabelShort);
        return RadioRulesDef.Current.hamShackWorkFactor;
    }

    /// <summary>The most impressive ham shack on a map, or 0, kept a game hour.</summary>
    public static float BestImpressiveness(Map? map)
    {
        if (map == null) return 0f;
        int hour = Find.TickManager.TicksGame / GenDate.TicksPerHour;
        if (kept.TryGetValue(map, out var held) && held.hour == hour) return held.best;
        if (kept.Count > 8) kept.Clear();
        float best = 0f;
        foreach (CompRadio station in RadioNet.For(map).Stations)
            if (station.parent.GetRoom() is { } room && room.Role == RadioShackDefOf.PhobosRadio_HamShack)
                best = System.Math.Max(best, room.GetStat(RoomStatDefOf.Impressiveness));
        kept[map] = (hour, best);
        return best;
    }
}

/// <summary>A room with a radio station and a QSL wall is a ham shack (a bedroom with a bed in it stays a bedroom).</summary>
public class RoomRoleWorker_HamShack : RoomRoleWorker
{
    public override float GetScore(Room room)
    {
        bool station = false, wall = false;
        foreach (Thing thing in room.ContainedAndAdjacentThings)
        {
            if (thing is Building_QslWall) wall = true;
            else if (thing.TryGetComp<CompRadio>() is { Props.isStation: true }) station = true;
            if (station && wall) return 30000f;
        }
        return 0f;
    }
}

/// <summary>Radio operators enjoy a decent ham shack in the colony (Radio 0.68.0; authored mood).</summary>
public class ThoughtWorker_PhobosHamShack : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!p.IsColonist || !RadioQsl.WorksTheRadio(p)) return ThoughtState.Inactive;
        return HamShack.BestImpressiveness(p.MapHeld) >= RadioRulesDef.Current.hamShackMoodImpressiveness ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
    }
}

[DefOf]
public static class RadioShackDefOf
{
    public static RoomRoleDef PhobosRadio_HamShack = null!;

    static RadioShackDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioShackDefOf));
}
