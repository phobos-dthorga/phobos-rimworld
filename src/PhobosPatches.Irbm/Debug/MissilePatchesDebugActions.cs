using System.Collections.Generic;
using IRBM;
using LudeonTK;
using Phobos.Framework.Debug;
using Phobos.Framework.Work;
using Phobos.MissilePatches.Strike;
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
        // Through the mod's own method, so the prefix and the sweep run exactly as they do under a real missile. The figures are the
        // extinction-class warhead's (Bomb, 10,000 damage, 200 armour penetration, every category, thick roof shelters).
        StrategicImpactCountdown.ApplyStrategicDamage(Find.CurrentMap, DamageDefOf.Bomb, 10000f, 200f, null, "dev strike",
            new List<ThingCategory> { ThingCategory.Pawn, ThingCategory.Building, ThingCategory.Item, ThingCategory.Plant },
            penetrateThickRoof: false, lockCorePart: true, completeDismantle: false);
    }

    [DebugAction(DebugCategory.Name, "Missile Patches: strike current map with D'thorga's blast model (destroys the centre)",
        actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void StrikeWithBlastModel()
    {
        // The extinction-class warhead's own payload and zones, through the launcher a real missile uses (no missile: a third-party letter).
        MissileWarheadDef? warhead = DefDatabase<MissileWarheadDef>.GetNamedSilentFail("IRBM_Warhead_Strategic_Standard");
        var props = warhead?.payloadComps?.Find(comp => comp is WorldObjectCompProperties_StrategicPayload) as WorldObjectCompProperties_StrategicPayload;
        PhobosStrikeExtension? zones = warhead?.GetModExtension<PhobosStrikeExtension>();
        if (props == null || zones == null)
        {
            Log.Warning(MissilePatchesMod.LogPrefix + " The extinction-class warhead or its strike zones were not found; nothing was struck.");
            return;
        }
        StrikeLauncher.Launch(null, props, zones, new List<Map> { Find.CurrentMap }, null);
    }

    [DebugAction(DebugCategory.Name, "Missile Patches: log blast queue", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
    public static void LogQueue()
    {
        var text = new System.Text.StringBuilder(MissilePatchesMod.LogPrefix + " " + FrameWorkQueue.Pending + " job(s) queued.");
        foreach (FrameWork work in FrameWorkQueue.All)
        {
            text.Append("\n  ").Append(work.Label).Append(work.Started ? ", started " + work.SecondsSinceStart.ToString("0.0") + " s ago, "
                + work.Frames + " frames, " + work.WorkMs.ToString("0") + " ms of work" : ", waiting");
            if (work is IrbmSweep sweep) text.Append(", at ").Append(sweep.Next).Append(" of ").Append(sweep.Count).Append(", passes ").Append(sweep.Passes);
            if (work is StrikeWork strike) text.Append(", at ").Append(strike.Next).Append(" of ").Append(strike.Count).Append(strike.Full ? ", full strike" : ", heat and fallout");
        }
        text.Append("\n  Room rebuilds can be held: ").Append(RoomRebuildHold.Available).Append(". Combat Extended's fragments: ")
            .Append(CombatExtendedFragments.Present ? (CombatExtendedFragments.OnInCombatExtended ? "on in its settings" : "off in its settings") : "not found");
        Log.Message(text.ToString());
    }

    [DebugAction(DebugCategory.Name, "Missile Patches: drop queued blasts", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
    public static void DropQueue()
    {
        int pending = FrameWorkQueue.Pending;
        FrameWorkQueue.Clear();
        Log.Message(MissilePatchesMod.LogPrefix + " Dropped " + pending + " queued job" + (pending == 1 ? "" : "s") + ".");
    }

    [DebugAction(DebugCategory.Name, "Missile Patches: dismiss every \"Area revealed\" letter", actionType = DebugActionType.Action,
        allowedGameStates = AllowedGameStates.Playing)]
    public static void DismissAreaRevealedLetters()
    {
        // For the letters a strike sent before 0.10.0. The game gives these letters no def or class of their own, so they are found by
        // the label the game itself gave them, translated the same way; a dev clean-up only, never a gameplay rule.
        string label = "LetterLabelAreaRevealed".Translate();
        List<Letter> letters = Find.LetterStack.LettersListForReading;
        int dismissed = 0;
        for (int i = letters.Count - 1; i >= 0; i--)
        {
            if (letters[i].Label.RawText != label) continue;
            Find.LetterStack.RemoveLetter(letters[i]);
            dismissed++;
        }
        string text = MissilePatchesMod.LogPrefix + " Dismissed " + dismissed + " \"" + label + "\" letter" + (dismissed == 1 ? "" : "s") + ".";
        Messages.Message(text, MessageTypeDefOf.NeutralEvent, historical: false);
        Log.Message(text);
    }

    [DebugAction(DebugCategory.Name, "Missile Patches: count Combat Extended fragments on map", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void CountFragments()
    {
        int fragments = 0;
        foreach (Thing thing in Find.CurrentMap.listerThings.AllThings)
        {
            if (thing.def.defName.StartsWith("Fragment_")) fragments++;
        }
        Log.Message(MissilePatchesMod.LogPrefix + " " + fragments + " Combat Extended fragment" + (fragments == 1 ? "" : "s") + " on "
            + (Find.CurrentMap.Parent?.Label ?? "this map") + ", among " + Find.CurrentMap.listerThings.AllThings.Count + " things.");
    }
}
