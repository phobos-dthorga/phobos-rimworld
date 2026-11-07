using System.Collections.Generic;
using IRBM;
using LudeonTK;
using Phobos.Framework.Debug;
using RimWorld;
using Verse;

namespace Phobos.MissilePatches.Debug;

/// <summary>Dev-mode actions, under the shared D'thorga category. The strike action destroys the current map's contents.</summary>
public static class MissilePatchesDebugActions
{
    [DebugAction(DebugCategory.Name, "Missile Patches: strike current map (as an extinction-class warhead; destroys everything)",
        actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void StrikeCurrentMap()
    {
        // Through the mod's own method, so the prefix and the sweep run exactly as they do under a real missile. The figures
        // are the extinction-class warhead's (Bomb, 10,000 damage, 200 armour penetration, every category, thick roof shelters).
        StrategicImpactCountdown.ApplyStrategicDamage(Find.CurrentMap, DamageDefOf.Bomb, 10000f, 200f, null, "dev strike",
            new List<ThingCategory> { ThingCategory.Pawn, ThingCategory.Building, ThingCategory.Item, ThingCategory.Plant },
            penetrateThickRoof: false, lockCorePart: true, completeDismantle: false);
    }

    [DebugAction(DebugCategory.Name, "Missile Patches: log blast queue", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
    public static void LogQueue()
    {
        StrategicSweep? current = StrategicSweeps.Current;
        string state = current == null ? "no blast queued"
            : StrategicSweeps.Pending + " queued; the current one over " + current.PlaceLabel + " at " + current.Next + " of " + (current.Targets?.Count ?? 0)
              + " after " + current.Frames + " frames";
        Log.Message(MissilePatchesMod.LogPrefix + " " + state + "; room rebuilds can be held: " + StrategicSweeps.CanHoldRoomRebuilds + ".");
    }

    [DebugAction(DebugCategory.Name, "Missile Patches: drop queued blasts", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
    public static void DropQueue()
    {
        int pending = StrategicSweeps.Pending;
        StrategicSweeps.Clear();
        Log.Message(MissilePatchesMod.LogPrefix + " Dropped " + pending + " queued blast" + (pending == 1 ? "" : "s") + ".");
    }
}
