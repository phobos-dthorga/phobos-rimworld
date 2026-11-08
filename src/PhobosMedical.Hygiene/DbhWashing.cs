using DubsBadHygiene;
using HarmonyLib;
using Phobos.BurnsAndSmoke.Comps;
using Phobos.BurnsAndSmoke.Services;
using Phobos.Shared;
using RimWorld;
using Verse;

namespace Phobos.Medical.Hygiene;

/// <summary>
/// Dubs Bad Hygiene's washing decontaminates (owner, 8 Nov 2026: the shower is enough by default, with extra mechanics for Dubs Bad
/// Hygiene players; docs/development/caustic-gas.md). A colonist taking a shower or a bath, sitting in a hot tub, swimming, or washing
/// at a water cell or a wash bucket is rinsed as at a safety shower: acid and caustic residue come off the skin, and with strip and
/// rinse on, contaminated clothes come off first. The sauna does not wash (Dubs' own sauna job, excluded by its name). Registered once
/// at startup; the burns pass asks.
/// </summary>
[StaticConstructorOnStartup]
public static class DbhWashing
{
    public const string HarmonyId = "PhobosDthorga.Medical.Hygiene";
    const string SaunaJob = "DBHUseSauna";
    static readonly string[] OtherWashingJobs = { "washAtCell", "useWashBucket" };

    static DbhWashing()
    {
        CausticService.WashingProbes.Add(IsWashing);
        HarmonyPatchApplier.ApplyEach(new Harmony(HarmonyId), typeof(DbhWashing).Assembly, "[D'thorga Medical: Dubs Bad Hygiene]");
    }

    static bool IsWashing(Pawn pawn)
    {
        JobDef? job = pawn.CurJobDef;
        if (job == null) return false;
        if (job is WashingJobDef) return job.defName != SaunaJob;
        for (int i = 0; i < OtherWashingJobs.Length; i++)
        {
            if (job.defName == OtherWashingJobs[i]) return true;
        }
        return false;
    }
}

/// <summary>
/// A wash in Dubs Bad Hygiene's washing machine clears caustic residue from clothes, as it clears the taint of a dead wearer: in
/// UnloadWashing, which takes them out. A prefix that never skips the original (the clothes are still in the drum to read); the
/// machine has no extension point for what a wash does.
/// </summary>
[HarmonyPatch(typeof(Building_WashingMachine), nameof(Building_WashingMachine.UnloadWashing))]
public static class WashingMachineCleansPatch
{
    public static void Prefix(Building_WashingMachine __instance)
    {
        ThingOwner? drum = __instance.innerContainer;
        if (drum == null) return;
        for (int i = 0; i < drum.Count; i++)
        {
            if (drum[i] is Apparel apparel) apparel.TryGetComp<CompPhobosContamination>()?.Clean();
        }
    }
}
