using HarmonyLib;
using Phobos.Medical.Comps;
using Phobos.Medical.Services;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.Medical.Patches;

/// <summary>
/// An unpowered liquid oxygen store takes in no more oxygen (the owner, 7 Nov 2026). Vanilla Expanded Framework's pipe network asks
/// each storage how much it can accept before filling it, and that getter is not virtual, so a building can't answer for itself: a
/// postfix sets the answer to nothing while <see cref="CompPhobosBoilOff"/> says the store is refusing. It only ever lowers the
/// answer, so the store still gives out what it holds. Runs on every pipe-network pass, so the check is one set lookup.
/// </summary>
[HarmonyPatch(typeof(CompResourceStorage), nameof(CompResourceStorage.AmountCanAccept), MethodType.Getter)]
public static class StoreAcceptPatch
{
    public static void Postfix(CompResourceStorage __instance, ref float __result)
    {
        if (__result > 0f && CompPhobosBoilOff.RefusesFilling(__instance)) __result = 0f;
    }
}

/// <summary>
/// No operation on a patient lying in a pressure chamber (the owner, 7 Nov 2026): the surgeon can't reach them through it. The game
/// has no extension point for "this bed allows no surgery", and a medical bill's readiness is its virtual ShouldDoNow, so a postfix
/// turns it off for that patient only. The bill stays and runs once they are moved; the chamber's inspect line says so.
/// </summary>
[HarmonyPatch(typeof(Bill_Medical), nameof(Bill_Medical.ShouldDoNow))]
public static class NoSurgeryInChamberPatch
{
    public static void Postfix(Bill_Medical __instance, ref bool __result)
    {
        if (__result && ChamberService.BlocksSurgery(__instance)) __result = false;
    }
}

/// <summary>
/// The one-time letter recommending Vanilla Gravship Expanded (the owner, 7 Nov 2026). A postfix on the game's last start-up step
/// shows it once a colony is ready, new or loaded, with nothing saved in the game: a game component would leave a class name in the
/// save. <see cref="RecommendationService"/> decides whether it is due.
/// </summary>
[HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
public static class RecommendationLetterPatch
{
    public static void Postfix() => RecommendationService.ShowOnceIfDue();
}
