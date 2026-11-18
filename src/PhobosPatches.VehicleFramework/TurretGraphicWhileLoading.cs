using System.Threading;
using HarmonyLib;
using Vehicles;
using Verse;

namespace Phobos.VehicleFrameworkPatches;

/// <summary>
/// A turret's picture is never resolved off the main thread (Patches 0.24.0; temporary, docs/development/temporary-patches.md row 19). While
/// a save loads, Vehicle Framework lines up each angle-limited turret that had a target when it was saved (VehicleTurret.PostPostLoadInit,
/// AlignToTargetRestricted), on the loading thread. VTOL Vehicle's postfix on VehicleTurret.TurretLocation then reads the turret's picture
/// to place it (VtolVehicleTurret.VtolDrawPosition), the game refuses to load a picture off the main thread, and Vehicle Framework keeps the
/// failed result: the turret is drawn with no picture and its draw throws every frame (the owner's Mongoose, 19 Nov 2026: 3,770 errors
/// in 75 seconds, filling the log's limit). A prefix that skips the original off the main thread only, so nothing is cached and the
/// picture is resolved on the first draw, on the main thread, as for any other turret; VTOL Vehicle already copes with no picture at that
/// moment. On the main thread it does nothing. Stands down by itself: once the fault is fixed upstream, nothing asks off the main thread.
/// </summary>
[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.ResolveGraphics), typeof(VehiclePawn), typeof(bool))]
public static class TurretGraphicWhileLoadingPatch
{
    /// <summary>Turrets whose picture was put off since the last report: counted on the loading thread, reported on the main one.</summary>
    static int deferred;

    public static bool Prefix()
    {
        if (UnityData.IsInMainThread) return true;
        if (Interlocked.Increment(ref deferred) == 1) LongEventHandler.ExecuteWhenFinished(Report);
        return false;
    }

    static void Report()
    {
        int count = Interlocked.Exchange(ref deferred, 0);
        if (count <= 0) return;
        if (VehicleFrameworkPatchesMod.Logger.DebugEnabled)
            VehicleFrameworkPatchesMod.Logger.Debug(count + " turret picture(s) were asked for while the game was loading; they are drawn once it has loaded");
        if (Seen.Wants("patches.turret-picture-after-loading")) Seen.Note("patches.turret-picture-after-loading", null, count + " turret picture(s)");
    }
}
