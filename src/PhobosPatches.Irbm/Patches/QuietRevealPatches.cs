using HarmonyLib;
using IRBM;
using Phobos.Framework.Work;
using Phobos.MissilePatches.Strike;
using Verse;

namespace Phobos.MissilePatches.Patches;

/// <summary>
/// Whether a strategic blast is working on a map right now: one of ours queued for it (the fast sweep or D'thorga's strike, from its
/// first slice to its last), or the missile mod's own one-frame pass while the "spread the blast" setting is off. Read only when the
/// game reveals a fogged area, which is rare, so the queue is scanned then and nothing is cached or saved.
/// </summary>
public static class BlastsUnderway
{
    static int unspreadPasses;

    public static bool On(Map map)
    {
        if (unspreadPasses > 0) return true;
        foreach (FrameWork work in FrameWorkQueue.All)
        {
            if (work is IrbmSweep sweep && sweep.Map == map) return true;
            if (work is StrikeWork strike && strike.Map == map) return true;
        }
        return false;
    }

    /// <summary>Turns a reveal's letters off while a blast works on its map, if the player keeps the setting on.</summary>
    internal static void QuietDuringBlast(Map map, ref bool sendLetters)
    {
        if (sendLetters && MissilePatchesMod.Settings.quietReveals && On(map)) sendLetters = false;
    }

    internal static void EnterUnspread() => unspreadPasses++;

    internal static void LeaveUnspread()
    {
        if (unspreadPasses > 0) unspreadPasses--;
    }
}

/// <summary>
/// Marks the missile mod's own damage pass as a blast for <see cref="BlastsUnderway"/>, for the case where it runs whole in one frame
/// (the "spread the blast" setting off). First in line, so the mark is set before the spread prefix queues the sweep instead; the
/// finalizer always clears it, whichever way the pass ends. Neither changes what the pass does.
/// </summary>
[HarmonyPatch(typeof(StrategicImpactCountdown), nameof(StrategicImpactCountdown.ApplyStrategicDamage))]
public static class Patch_ApplyStrategicDamage_MarkBlast
{
    [HarmonyPriority(Priority.First)]
    public static void Prefix() => BlastsUnderway.EnterUnspread();

    public static void Finalizer() => BlastsUnderway.LeaveUnspread();
}

/// <summary>
/// A strategic blast breaks open every sealed cave, ruin and vault on the map at once, and the game sends an "Area revealed" letter
/// for each one: about eighty after one strike in the owner's game (7 Nov 2026). The owner's call: none while a blast works, since the
/// blast leaves nothing in them. The prefix only turns the game's own "send letters" argument off for that map; the fog is cleared
/// exactly as before. The game reveals through two overloads of FloodUnfogAdjacent, so both are patched, named by parameter types.
/// The mechanoid warning that shares the letter is silenced with it, as the owner asked for every letter of this kind.
/// </summary>
[HarmonyPatch(typeof(FogGrid), nameof(FogGrid.FloodUnfogAdjacent), new[] { typeof(IntVec3), typeof(bool) })]
public static class Patch_FloodUnfogAdjacentCell_Quiet
{
    public static void Prefix(Map ___map, ref bool sendLetters) => BlastsUnderway.QuietDuringBlast(___map, ref sendLetters);
}

/// <summary>The same as <see cref="Patch_FloodUnfogAdjacentCell_Quiet"/>, for the overload the game uses when a wall or rock is removed.</summary>
[HarmonyPatch(typeof(FogGrid), nameof(FogGrid.FloodUnfogAdjacent), new[] { typeof(Thing), typeof(bool) })]
public static class Patch_FloodUnfogAdjacentThing_Quiet
{
    public static void Prefix(Map ___map, ref bool sendLetters) => BlastsUnderway.QuietDuringBlast(___map, ref sendLetters);
}
