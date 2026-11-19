using System;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.CombatExtendedPatches.KeepGuns;

/// <summary>
/// Simple Sidearms' memory of a person's weapons, reached by name only (no reference to its assembly; owner rule for optional tie-ins). Read
/// 19 Nov 2026 from its public source (github.com/PeteTimesSix/SimpleSidearms, the build of 13 Aug 2025 that loads on 1.7): each person's
/// CompSidearmMemory keeps a preferred ranged weapon (DefaultRangedWeapon) that its quick-switch puts back in their hands, and a weapon the
/// player forced (ForcedWeapon). It records whatever the game's Equip job puts in empty hands as the preferred ranged weapon, a grenade
/// included, and from then on swaps the grenade back in. <see cref="KeepAsPreferred"/> gives the person's gun that place again, through Simple
/// Sidearms' own public InformOfAddedPrimary, as the Equip job would have. Anything that cannot be read counts as "not there", so an update
/// that moves these names leaves Simple Sidearms as it is and never stops a load.
/// </summary>
public static class SidearmsMemory
{
    const string MemoryType = "SimpleSidearms.rimworld.CompSidearmMemory";
    const string PairType = "SimpleSidearms.rimworld.ThingDefStuffDefPair";

    static bool looked;
    static MethodInfo? getMemory, informOfAddedPrimary;
    static PropertyInfo? defaultRanged, forcedWeapon;
    static FieldInfo? pairThing;

    /// <summary>Simple Sidearms is active and every name this needs was found.</summary>
    public static bool Available
    {
        get
        {
            Look();
            return getMemory != null && informOfAddedPrimary != null && defaultRanged != null && forcedWeapon != null && pairThing != null;
        }
    }

    /// <summary>The kind of weapon Simple Sidearms will keep putting in this person's hands (their preferred ranged weapon), or null.</summary>
    public static ThingDef? PreferredRanged(Pawn pawn) => ThingOf(pawn, defaultRanged);

    /// <summary>The kind of weapon the player told Simple Sidearms this person must hold, or null.</summary>
    public static ThingDef? Forced(Pawn pawn) => ThingOf(pawn, forcedWeapon);

    /// <summary>
    /// Makes the gun in this person's hands their preferred ranged weapon in Simple Sidearms when it has none, or a single-use one. Leaves a
    /// different gun the player preferred alone. True when it changed something.
    /// </summary>
    public static bool KeepAsPreferred(Pawn pawn, ThingWithComps gun)
    {
        if (!Available || !SingleUseWeapons.IsGun(gun.def)) return false;
        ThingDef? preferred = PreferredRanged(pawn);
        if (preferred != null && !SingleUseWeapons.IsSingleUse(preferred)) return false;
        object? memory = Memory(pawn);
        if (memory == null) return false;
        try
        {
            informOfAddedPrimary!.Invoke(memory, new object[] { gun });
            return true;
        }
        catch (Exception e)
        {
            CombatExtendedPatchesMod.Logger.WarnOnce("Could not give " + pawn.LabelShort + "'s " + gun.LabelShort + " its place in Simple Sidearms: " + e.Message);
            return false;
        }
    }

    static ThingDef? ThingOf(Pawn pawn, PropertyInfo? property)
    {
        if (!Available || property == null) return null;
        object? memory = Memory(pawn);
        if (memory == null) return null;
        try
        {
            object? pair = property.GetValue(memory);
            return pair == null ? null : pairThing!.GetValue(pair) as ThingDef;
        }
        catch (Exception)
        {
            return null;
        }
    }

    static object? Memory(Pawn pawn)
    {
        try
        {
            return getMemory!.Invoke(null, new object[] { pawn, true });
        }
        catch (Exception)
        {
            return null;
        }
    }

    static void Look()
    {
        if (looked) return;
        looked = true;
        if (!OptionalMods.IsActive("petetimessix.simplesidearms")) return;
        Type? memory = AccessTools.TypeByName(MemoryType);
        Type? pair = AccessTools.TypeByName(PairType);
        if (memory == null || pair == null) return;
        getMemory = AccessTools.Method("SimpleSidearms.rimworld.CompSidearmMemory:GetMemoryCompForPawn", new[] { typeof(Pawn), typeof(bool) });
        informOfAddedPrimary = AccessTools.Method("SimpleSidearms.rimworld.CompSidearmMemory:InformOfAddedPrimary", new[] { typeof(Thing) });
        defaultRanged = AccessTools.Property(memory, "DefaultRangedWeapon");
        forcedWeapon = AccessTools.Property(memory, "ForcedWeapon");
        pairThing = AccessTools.Field(pair, "thing");
    }
}
