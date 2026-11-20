using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Phobos.CombatExtendedPatches.Rules;
using Phobos.Framework;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.CombatExtendedPatches.KeepGuns;

/// <summary>
/// Combat Extended's loadouts, reached by name only (no reference to its assembly; owner rule for optional tie-ins), read 21 Nov 2026 from
/// its public source: a person's loadout, its slots, what they carry by kind, and their hold records, enough to ask Combat Extended's own
/// question, "would this be dropped as excess?", for one more of a kind (<see cref="LoadoutExcessRule"/>). Anything that cannot be read
/// answers "cannot tell", so an update that moves these names leaves both mods as they are.
/// </summary>
public static class LoadoutBridge
{
    static bool looked;
    static MethodInfo? getLoadout, getSlotsFor, getStorage, getHoldRecords;
    static FieldInfo? defaultLoadout, dropUndefined, integerValue, recordCount;
    static PropertyInfo? slotThingDef, slotGenericDef, slotCount, genericLambda, recordThingDef;

    /// <summary>Combat Extended is active and every name this needs was found.</summary>
    public static bool Available
    {
        get
        {
            Look();
            return getLoadout != null && getSlotsFor != null && getStorage != null && getHoldRecords != null && defaultLoadout != null
                && dropUndefined != null && integerValue != null && recordCount != null && slotThingDef != null && slotGenericDef != null
                && slotCount != null && genericLambda != null && recordThingDef != null;
        }
    }

    /// <summary>Whether this person's loadout would drop one more of this kind from their pack: true, false, or null when it cannot tell.</summary>
    public static bool? WouldDropOneMore(Pawn pawn, ThingDef kind)
    {
        if (!Available) return null;
        try
        {
            object? loadout = getLoadout!.Invoke(null, new object[] { pawn });
            if (loadout == null || (bool)defaultLoadout!.GetValue(loadout)!) return false;
            var carried = new List<KeyValuePair<string, int>>();
            var byName = new Dictionary<string, ThingDef>();
            if (getStorage!.Invoke(null, new object[] { pawn }) is IDictionary storage)
            {
                foreach (DictionaryEntry entry in storage)
                {
                    if (entry.Key is not ThingDef def || entry.Value == null) continue;
                    carried.Add(new KeyValuePair<string, int>(def.defName, (int)integerValue!.GetValue(entry.Value)!));
                    byName[def.defName] = def;
                }
            }
            carried.Add(new KeyValuePair<string, int>(kind.defName, 1));
            byName[kind.defName] = kind;
            var slots = new List<LoadoutSlotView>();
            if (getSlotsFor!.Invoke(loadout, new object[] { pawn }) is IEnumerable slotList)
            {
                foreach (object slot in slotList)
                {
                    int count = (int)slotCount!.GetValue(slot)!;
                    if (slotThingDef!.GetValue(slot) is ThingDef named)
                    {
                        slots.Add(new LoadoutSlotView(named.defName, null, count));
                    }
                    else if (slotGenericDef!.GetValue(slot) is { } generic && genericLambda!.GetValue(generic) is Predicate<ThingDef> covers)
                    {
                        slots.Add(new LoadoutSlotView(null, byName.Where(p => covers(p.Value)).Select(p => p.Key).ToList(), count));
                    }
                }
            }
            int? held = null;
            if (getHoldRecords!.Invoke(null, new object[] { pawn }) is IEnumerable records)
            {
                foreach (object record in records)
                {
                    if (record == null || recordThingDef!.GetValue(record) as ThingDef != kind) continue;
                    held = (int)recordCount!.GetValue(record)!;
                    break;
                }
            }
            return LoadoutExcessRule.WouldDrop(carried, slots, kind.defName, held, (bool)dropUndefined!.GetValue(loadout)!);
        }
        catch (Exception e)
        {
            CombatExtendedPatchesMod.Logger.WarnOnce("Could not read " + pawn.LabelShort + "'s loadout for " + kind.defName + ": " + (e.InnerException ?? e).Message);
            return null;
        }
    }

    static void Look()
    {
        if (looked) return;
        looked = true;
        if (!OptionalMods.IsActive("ceteam.combatextended")) return;
        Type? loadoutType = AccessTools.TypeByName("CombatExtended.Loadout");
        Type? slotType = AccessTools.TypeByName("CombatExtended.LoadoutSlot");
        Type? genericType = AccessTools.TypeByName("CombatExtended.LoadoutGenericDef");
        Type? recordType = AccessTools.TypeByName("CombatExtended.HoldRecord");
        Type? integerType = AccessTools.TypeByName("CombatExtended.Integer");
        if (loadoutType == null || slotType == null || genericType == null || recordType == null || integerType == null) return;
        getLoadout = AccessTools.Method("CombatExtended.Utility_Loadouts:GetLoadout", new[] { typeof(Pawn) });
        getStorage = AccessTools.Method("CombatExtended.Utility_HoldTracker:GetStorageByThingDef", new[] { typeof(Pawn) });
        getHoldRecords = AccessTools.Method("CombatExtended.LoadoutManager:GetHoldRecords", new[] { typeof(Pawn) });
        getSlotsFor = AccessTools.Method("CombatExtended.Loadout:GetSlotsFor", new[] { typeof(Pawn) });
        defaultLoadout = AccessTools.Field(loadoutType, "defaultLoadout");
        dropUndefined = AccessTools.Field(loadoutType, "dropUndefined");
        integerValue = AccessTools.Field(integerType, "value");
        recordCount = AccessTools.Field(recordType, "count");
        slotThingDef = AccessTools.Property(slotType, "thingDef");
        slotGenericDef = AccessTools.Property(slotType, "genericDef");
        slotCount = AccessTools.Property(slotType, "count");
        genericLambda = AccessTools.Property(genericType, "lambda");
        recordThingDef = AccessTools.Property(recordType, "thingDef");
    }
}

/// <summary>
/// Sidearms that loop (Patches 0.29.1; the owner's choice of 21 Nov 2026, "the loadout wins"; docs/development/soldiers-keep-their-guns.md):
/// Simple Sidearms fetches each weapon a person remembers, and Combat Extended's loadout drops any copy beyond what it allows, so a second
/// copy of a weapon the loadout counts once (most often the gun in hand, which an ad-hoc loadout counts as one) was fetched and dropped
/// again and again. A person no longer fetches a copy their loadout would only drop, and the player is told once which person and weapon,
/// to settle it in the sidearms or the loadout. Any other sidearm is fetched as before.
/// </summary>
public static class SidearmLoopGuard
{
    static readonly HashSet<long> told = new();
    static Game? toldIn;

    /// <summary>Whether Simple Sidearms should not fetch this weapon for this person now; tells the player the first time.</summary>
    public static bool Refuses(Pawn pawn, Thing weapon)
    {
        if (!CombatExtendedPatchesMod.Settings.sidearmLoopGuard || pawn.Faction != Faction.OfPlayer) return false;
        if (LoadoutBridge.WouldDropOneMore(pawn, weapon.def) != true) return false;
        Tell(pawn, weapon.def);
        if (Seen.Wants("patches.sidearms.loop-guard", pawn)) Seen.Note("patches.sidearms.loop-guard", pawn, weapon.def.label);
        return true;
    }

    static void Tell(Pawn pawn, ThingDef weapon)
    {
        if (toldIn != Current.Game)
        {
            told.Clear();
            toldIn = Current.Game;
        }
        if (!told.Add(((long)pawn.thingIDNumber << 20) ^ weapon.shortHash)) return;
        Messages.Message("PhobosCombatExtendedPatches_SidearmNotFetched".Translate(pawn.LabelShort.Named("PAWN"), weapon.label.Named("WEAPON")).Resolve(),
            pawn, MessageTypeDefOf.CautionInput, historical: false);
    }
}

/// <summary>
/// Sidearms that loop, the patch: Simple Sidearms' choice of a weapon to fetch back. Reason for the patch: it has no extension point, and the
/// fault is in no one place but where the two mods meet. A postfix; Simple Sidearms' method always runs, and a fetch is only ever taken away,
/// never added. Applied only with Simple Sidearms active; its setting is read at each fetch, so turning it off takes effect at once.
/// </summary>
[HarmonyPatch]
public static class Patch_SimpleSidearms_RetrieveWeapon
{
    public static bool Prepare() => OptionalMods.IsActive("petetimessix.simplesidearms") && TargetMethod() != null && LoadoutBridge.Available;

    public static MethodBase? TargetMethod() =>
        AccessTools.Method("SimpleSidearms.rimworld.JobGiver_RetrieveWeapon:TryGiveJobStatic", new[] { typeof(Pawn), typeof(bool) });

    public static void Postfix(Pawn pawn, ref Job? __result)
    {
        if (__result?.targetA.Thing is { } weapon && SidearmLoopGuard.Refuses(pawn, weapon)) __result = null;
    }
}
