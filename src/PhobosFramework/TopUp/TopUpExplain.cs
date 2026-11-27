using System.Collections.Generic;
using System.Text;
using LudeonTK;
using Phobos.Framework.Debug;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.TopUp;

/// <summary>
/// "Why isn't this topped up?" (Framework 0.243.1; the owner's report of 23 Nov 2026: an aircraft's chaff and flares were never refilled).
/// A dev action: for the selected thing, every step the game and our work giver take before a hauler (or a mechanitor) brings it material,
/// in their order, with the first that says no. Reads only; changes nothing. The hauler asked is the nearest free colonist with the work on.
/// </summary>
public static class TopUpExplain
{
    /// <summary>Select one building, mechanoid or vehicle: logs, in order, every step the game and our work giver take before a hauler
    /// (or a mechanitor) brings it material, ending at the first that says no. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Framework: why isn't this topped up?", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void ExplainSelected()
    {
        if (Find.Selector.SingleSelectedThing is not { } target)
        {
            Log.Message(FrameworkMod.LogPrefix + " Select the one building, mechanoid or vehicle that should be topped up, then run this again.");
            return;
        }
        Log.Message(FrameworkMod.LogPrefix + " Why isn't " + target.LabelShort + " (" + target.ThingID + ") topped up?\n" + Explain(target));
    }

    /// <summary>The steps, one a line, ending at the first refusal.</summary>
    public static string Explain(Thing target)
    {
        var text = new StringBuilder();
        void Line(bool ok, string what) => text.Append(ok ? "  yes: " : "  NO:  ").Append(what).Append('\n');

        bool active = FrameworkMod.Settings.topUpMachines;
        Line(active, "topping up is on in D'thorga Framework's settings");
        if (!active) return text.ToString();
        TopUpWant? want = TopUps.WantOf(target);
        Line(want != null, want == null ? "it is not a kind we top up (no PhobosTopUpDef names a comp it has), or its comp reads nothing"
            : "it is a kind we top up, wanting " + want.Value.Amount + " " + want.Value.Material.label + ", least load " + want.Value.LeastLoad);
        if (want == null) return text.ToString();
        bool worth = Rules.TopUpRule.Worth(want.Value.Amount, want.Value.LeastLoad, forced: false);
        Line(worth, "it wants enough to be worth a walk (at least the least load)");
        Line(TopUps.StoreOf(target) != null, "its comp holds a store to put material in");
        Map? map = target.MapHeld;
        Line(map != null && target.Spawned, "it is spawned on a map");
        if (map == null || !target.Spawned) return text.ToString();
        bool listed = false;
        foreach (Thing thing in TopUps.Wanting(map, Faction.OfPlayer, mechanoids: target is Pawn pawnTarget && pawnTarget.RaceProps.IsMechanoid))
        {
            if (thing == target) { listed = true; break; }
        }
        Line(listed, "the work giver's list of things wanting a top-up includes it");
        Line(target.Faction == Faction.OfPlayer, "it is the player's (" + (target.Faction?.Name ?? "no faction") + ")");
        Line(!target.IsBurning(), "it is not burning");

        Pawn? hauler = null;
        float best = float.MaxValue;
        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn.Downed || pawn.workSettings == null || !pawn.workSettings.WorkIsActive(WorkTypeDefOf.Hauling)) continue;
            float distance = pawn.Position.DistanceToSquared(target.Position);
            if (distance < best) { best = distance; hauler = pawn; }
        }
        Line(hauler != null, hauler == null ? "a free colonist with Hauling work on" : "nearest free colonist with Hauling work on: " + hauler.LabelShort);
        if (hauler == null) return text.ToString();
        bool forbidden = target.IsForbidden(hauler);
        string area = hauler.playerSettings?.EffectiveAreaRestrictionInPawnCurrentMap is { } allowed && !allowed[target.Position]
            ? " (its cell is outside " + hauler.LabelShort + "'s allowed area)" : "";
        Line(!forbidden, "it is not forbidden to " + hauler.LabelShort + area);
        Line(hauler.CanReach(target, PathEndMode.Touch, Danger.Deadly), hauler.LabelShort + " can reach it (touching it)");
        Line(hauler.Map.reachability.CanReach(hauler.Position, target, PathEndMode.Touch, TraverseParms.For(hauler, Danger.Some)),
            hauler.LabelShort + " can reach it as the work search does (touching it, through some danger only)");
        List<Thing> found = HaulAIUtility.FindFixedIngredientCount(hauler, want.Value.Material, 1);
        Line(!found.NullOrEmpty(), found.NullOrEmpty() ? "any " + want.Value.Material.label + " " + hauler.LabelShort + " may fetch (not forbidden, reachable, unreserved)"
            : hauler.LabelShort + " may fetch " + want.Value.Material.label + " at " + found[0].Position);
        Job? job = TopUps.JobFor(hauler, target, forced: false);
        Line(job != null, job != null ? "the work giver offers " + hauler.LabelShort + " the job: bring " + job.count : "the work giver offers no job (a step above, or one not listed here)");
        return text.ToString();
    }
}
