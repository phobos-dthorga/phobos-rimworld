using System;
using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// Carries saves across the renaming of the two bomb cases (Patches 0.14.2). Their old names ended in a digit, which the game forbids on
/// an item: it reads an item's ID from the digits at the end of "name + number", so the name's own digits joined the number and it
/// grew at every load until it overflowed to 0. Saves also name the cases and their recipes in bills, storage filters and factory
/// hoppers, and refer to the things and bills by load ID.
/// </summary>
internal static class BombCaseRename
{
    internal static readonly DefRenameTable Table = Build();

    static DefRenameTable Build()
    {
        var table = new DefRenameTable();
        table.AddItem("PhobosBombCaseMk82", "PhobosBombMk82Case");
        table.AddItem("PhobosBombCaseMk84", "PhobosBombMk84Case");
        return table;
    }
}

/// <summary>
/// The Defs' old names. A postfix, because the game's own converters are skipped when a save's mod list matches the running one, and
/// this method still returns then; five other mods in the owner's list convert their old names the same way.
/// </summary>
[HarmonyPatch(typeof(BackCompatibility), nameof(BackCompatibility.BackCompatibleDefName))]
public static class BombCaseOldDefNames
{
    public static void Postfix(Type defType, ref string __result)
    {
        if (defType == typeof(ThingDef) || defType == typeof(RecipeDef)) __result = BombCaseRename.Table.CurrentName(__result);
    }
}

/// <summary>
/// References to the renamed things and bills, which name them by load ID (a pawn's current job, a haul order). The game has no
/// extension point for these; a prefix that only rewrites the ID, never skipping the original. It runs while a save loads, not in play.
/// </summary>
[HarmonyPatch(typeof(LoadIDsWantedBank), nameof(LoadIDsWantedBank.RegisterLoadIDReadFromXml),
    new[] { typeof(string), typeof(Type), typeof(string), typeof(IExposable) })]
public static class BombCaseOldLoadId
{
    public static void Prefix(ref string targetLoadID)
    {
        if (targetLoadID != null) targetLoadID = BombCaseRename.Table.CurrentLoadId(targetLoadID);
    }
}

/// <summary>The same for lists of references (what a hauler carries). Rewrites the list in place; never skips the original.</summary>
[HarmonyPatch(typeof(LoadIDsWantedBank), nameof(LoadIDsWantedBank.RegisterLoadIDListReadFromXml),
    new[] { typeof(List<string>), typeof(string), typeof(IExposable) })]
public static class BombCaseOldLoadIdList
{
    public static void Prefix(List<string> targetLoadIDList)
    {
        if (targetLoadIDList == null) return;
        for (int i = 0; i < targetLoadIDList.Count; i++)
        {
            if (targetLoadIDList[i] != null) targetLoadIDList[i] = BombCaseRename.Table.CurrentLoadId(targetLoadIDList[i]);
        }
    }
}
