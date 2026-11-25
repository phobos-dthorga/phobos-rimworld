using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Upgrades;

/// <summary>
/// The service behind building upgrades (D'thorga Framework 0.210.0): ordering, cancelling and taking off, what a colonist brings and
/// works, and the words for each. The window and the jobs call this; the comp holds the state.
/// </summary>
[StaticConstructorOnStartup]
public static class BuildingUpgrades
{
    /// <summary>The upgrades button's picture, drawn in code (assets/phobos-vehicles/draw_service_yard_icons.py).</summary>
    public static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("Phobos/Framework/Upgrades/BuildingUpgrades", reportFailure: false)
        ?? BaseContent.BadTex;

    /// <summary>The player's buildings on a map with work under way.</summary>
    public static IEnumerable<CompPhobosBuildingUpgrades> WithOrders(Map map, Faction faction)
    {
        IReadOnlyList<CompPhobosBuildingUpgrades> all = MapRegistry<CompPhobosBuildingUpgrades>.On(map);
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Ordered != null && all[i].parent.Faction == faction) yield return all[i];
        }
    }

    // ---- What may be done ----

    /// <summary>Why this upgrade cannot be ordered now, in the player's words, or null.</summary>
    public static string? Refusal(CompPhobosBuildingUpgrades comp, PhobosBuildingUpgradeDef def)
    {
        UpgradeRefusal refusal = BuildingUpgradeRule.CanOrder(comp.Has(def), def.prerequisite == null || comp.Has(def.prerequisite), def.Researched,
            comp.Ordered != null);
        return refusal switch
        {
            UpgradeRefusal.Fitted => "PhobosUpgrade_RefuseFitted".Translate(def.label.Named("UPGRADE")).Resolve(),
            UpgradeRefusal.Busy => "PhobosUpgrade_RefuseBusy".Translate(comp.Ordered!.label.Named("UPGRADE")).Resolve(),
            UpgradeRefusal.NeedsFirst => "PhobosUpgrade_RefuseNeedsFirst".Translate(def.prerequisite!.label.Named("FIRST")).Resolve(),
            UpgradeRefusal.Research => "PhobosUpgrade_RefuseResearch".Translate(def.researchPrerequisite!.label.Named("RESEARCH")).Resolve(),
            _ => null,
        };
    }

    /// <summary>Why this fitted upgrade cannot be taken off now, or null.</summary>
    public static string? RemoveRefusal(CompPhobosBuildingUpgrades comp, PhobosBuildingUpgradeDef def)
    {
        PhobosBuildingUpgradeDef? dependant = comp.Fitted.FirstOrDefault(other => other.prerequisite == def);
        UpgradeRemoveRefusal refusal = BuildingUpgradeRule.CanRemove(comp.Has(def), dependant != null, comp.Ordered != null);
        return refusal switch
        {
            UpgradeRemoveRefusal.NotFitted => "PhobosUpgrade_RefuseNotFitted".Translate(def.label.Named("UPGRADE")).Resolve(),
            UpgradeRemoveRefusal.Busy => "PhobosUpgrade_RefuseBusy".Translate(comp.Ordered!.label.Named("UPGRADE")).Resolve(),
            UpgradeRemoveRefusal.Needed => "PhobosUpgrade_RefuseNeeded".Translate(dependant!.label.Named("OTHER")).Resolve(),
            _ => null,
        };
    }

    // ---- The player's orders ----

    public static void Order(CompPhobosBuildingUpgrades comp, PhobosBuildingUpgradeDef def)
    {
        if (Refusal(comp, def) is { } refused)
        {
            Messages.Message(refused, comp.parent, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        comp.SetOrder(def, remove: false);
        Messages.Message("PhobosUpgrade_Ordered".Translate(def.label.Named("UPGRADE"), comp.parent.LabelShort.Named("BUILDING"), def.CostText.Named("COST")).Resolve(),
            comp.parent, MessageTypeDefOf.SilentInput, historical: false);
    }

    public static void OrderRemoval(CompPhobosBuildingUpgrades comp, PhobosBuildingUpgradeDef def)
    {
        if (RemoveRefusal(comp, def) is { } refused)
        {
            Messages.Message(refused, comp.parent, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        comp.SetOrder(def, remove: true);
        Messages.Message("PhobosUpgrade_RemovalOrdered".Translate(def.label.Named("UPGRADE"), comp.parent.LabelShort.Named("BUILDING")).Resolve(),
            comp.parent, MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Cancels the work under way; what was brought for it is put down beside the building.</summary>
    public static void Cancel(CompPhobosBuildingUpgrades comp)
    {
        if (comp.Ordered is not { } def) return;
        if (comp.parent.Spawned) PutDown(comp.ClearOrder(), comp.parent.Position, comp.parent.Map);
        else comp.ClearOrder();
        Messages.Message("PhobosUpgrade_Cancelled".Translate(def.label.Named("UPGRADE")).Resolve(), comp.parent, MessageTypeDefOf.SilentInput, historical: false);
    }

    // ---- The colonists' work ----

    /// <summary>Puts the carried material into the order, no more than is still wanted, and drops the rest. Returns how many went in.</summary>
    public static int Deliver(Pawn pawn, CompPhobosBuildingUpgrades comp)
    {
        Thing? carried = pawn.carryTracker.CarriedThing;
        if (carried == null) return 0;
        int wanted = comp.Missing().Where(line => line.what == carried.def).Sum(line => line.count);
        int moved = Mathf.Min(wanted, carried.stackCount);
        if (moved > 0)
        {
            comp.AddDelivered(carried.def, moved);
            Thing used = carried.SplitOff(moved);
            if (!used.Destroyed) used.Destroy();
            // The case is the upgrade's name (Framework 0.301.0), so each building's test row stands on its own upgrades.
            string? upgrade = comp.Ordered?.defName;
            if (Seen.Wants("framework.building-upgrade.delivered", comp.parent, upgrade))
                Seen.Note("framework.building-upgrade.delivered", comp.parent, carried.def.label + " x" + moved, upgrade);
        }
        if (pawn.carryTracker.CarriedThing != null) pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
        return moved;
    }

    /// <summary>A colonist's work on the order this tick; returns true when it is done.</summary>
    public static bool Work(Pawn pawn, CompPhobosBuildingUpgrades comp, float amount)
    {
        if (!comp.ReadyForWork) return false;
        comp.AddWork(amount);
        if (comp.WorkDone < comp.TotalWork) return false;
        Finish(comp);
        return true;
    }

    static void Finish(CompPhobosBuildingUpgrades comp)
    {
        PhobosBuildingUpgradeDef def = comp.Ordered!;
        bool removing = comp.Removing;
        comp.ClearOrder();
        comp.SetFitted(def, !removing);
        if (removing)
        {
            var refund = new Dictionary<ThingDef, int>();
            foreach ((ThingDef what, int count) in def.Cost)
            {
                int back = BuildingUpgradeRule.Refund(count, def.refundShare);
                if (back > 0) refund[what] = back;
            }
            if (comp.parent.Spawned) PutDown(refund, comp.parent.Position, comp.parent.Map);
            Messages.Message("PhobosUpgrade_Removed".Translate(def.label.Named("UPGRADE"), comp.parent.LabelShort.Named("BUILDING")).Resolve(),
                comp.parent, MessageTypeDefOf.PositiveEvent, historical: false);
            Seen.Note("framework.building-upgrade.removed", comp.parent, def.label, def.defName);
            return;
        }
        Messages.Message("PhobosUpgrade_Fitted".Translate(def.label.Named("UPGRADE"), comp.parent.LabelShort.Named("BUILDING")).Resolve(),
            comp.parent, MessageTypeDefOf.PositiveEvent, historical: false);
        Seen.Note("framework.building-upgrade.fitted", comp.parent, def.label, variant: def.defName);
    }

    /// <summary>Puts materials down beside a place, as stacks.</summary>
    public static void PutDown(Dictionary<ThingDef, int> materials, IntVec3 at, Map map)
    {
        foreach (KeyValuePair<ThingDef, int> line in materials)
        {
            int left = line.Value;
            while (left > 0)
            {
                Thing thing = ThingMaker.MakeThing(line.Key, GenStuff.DefaultStuffFor(line.Key));
                thing.stackCount = Mathf.Min(left, line.Key.stackLimit);
                left -= thing.stackCount;
                GenPlace.TryPlaceThing(thing, at, map, ThingPlaceMode.Near);
            }
        }
    }

    // ---- Words ----

    /// <summary>The order's line for the inspect pane and the window: what it waits for, or how far along it is.</summary>
    public static string OrderLine(CompPhobosBuildingUpgrades comp)
    {
        PhobosBuildingUpgradeDef def = comp.Ordered!;
        if (comp.Removing)
        {
            return "PhobosUpgrade_LineRemoving".Translate(def.label.Named("UPGRADE"), comp.Progress.ToStringPercent().Named("DONE")).Resolve();
        }
        List<(ThingDef what, int count)> missing = comp.Missing();
        if (missing.Count > 0)
        {
            return "PhobosUpgrade_LineWaiting".Translate(def.label.Named("UPGRADE"),
                missing.Select(line => line.what.label + " x" + line.count).ToCommaList(useAnd: true).Named("MATERIALS")).Resolve();
        }
        return "PhobosUpgrade_LineFitting".Translate(def.label.Named("UPGRADE"), comp.Progress.ToStringPercent().Named("DONE")).Resolve();
    }
}
