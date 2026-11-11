using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Shared;
using Phobos.VehicleFrameworkPatches.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleFrameworkPatches;

/// <summary>
/// D'thorga Patches' Vehicle Framework part (from 0.16.6), switched on only while Vehicle Framework is active: fires on a vehicle that could never go out are
/// put out. The owner's Harrier came home from a sortie with 37 fires attached, all left behind on the sortie's map, and read as burning for days, which stopped
/// it refuelling (docs/development/vehicle-framework-fires.md). Remove this part when Vehicle Framework keeps one fire per vehicle and puts it out when the
/// vehicle leaves a map; the record says how to tell. Built at the owner's request (10 Nov 2026). No settings, no save data.
/// </summary>
public class VehicleFrameworkPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.16.9";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Vehicle Framework");
    const string Feature = "fires a vehicle cannot put out are put out: a second fire on it, or one left behind when it leaves a map";

    public VehicleFrameworkPatchesMod(ModContentPack content) : base(content)
    {
        HarmonyPatchApplier.ApplyEach(new Harmony(PackageId), typeof(VehicleFrameworkPatchesMod).Assembly, LogPrefix);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Vehicle Framework");
        report.On(Feature);
        report.Log();
    }
}

/// <summary>The service: reads a vehicle's attached fires and puts out the ones that cannot be real. The patches below only call it.</summary>
public static class VehicleFires
{
    /// <summary>Puts out every fire on the vehicle: it is leaving its map, and the game puts a thing's fire out when the thing enters a container.</summary>
    public static int PutOutAll(VehiclePawn vehicle) => PutOut(vehicle, all: true);

    /// <summary>Puts out fires on another map than the vehicle, and every fire after the first on its own map.</summary>
    public static int PutOutSurplus(VehiclePawn vehicle) => PutOut(vehicle, all: false);

    static int PutOut(VehiclePawn vehicle, bool all)
    {
        CompAttachBase? comp = vehicle.GetComp<CompAttachBase>();
        List<AttachableThing>? attachments = comp?.attachments;
        if (attachments == null || attachments.Count == 0) return 0;
        var fires = new List<Fire>();
        foreach (AttachableThing attachment in attachments)
        {
            if (attachment is Fire fire) fires.Add(fire);
        }
        if (fires.Count == 0) return 0;
        List<int> gone;
        if (all)
        {
            gone = new List<int>();
            for (int i = 0; i < fires.Count; i++) gone.Add(i);
        }
        else
        {
            var onOwnMap = new List<bool>(fires.Count);
            foreach (Fire fire in fires) onOwnMap.Add(fire.Spawned && vehicle.Spawned && fire.Map == vehicle.Map);
            gone = AttachedFireRule.Surplus(onOwnMap);
        }
        foreach (int i in gone)
        {
            Fire fire = fires[i];
            // Destroy takes the fire off the list itself; one already destroyed but still listed is only taken off.
            if (!fire.Destroyed) fire.Destroy();
            else attachments.Remove(fire);
        }
        return gone.Count;
    }
}

/// <summary>A vehicle leaving its map takes no fire with it, as the game's own things do when they enter a container. A prefix, while the vehicle and its fires are still on the map; it never skips the original.</summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.DeSpawn))]
public static class VehicleLeavesFiresBehindPatch
{
    public static void Prefix(VehiclePawn __instance)
    {
        if (__instance.Spawned) VehicleFires.PutOutAll(__instance);
    }
}

/// <summary>Now and then, a vehicle's fires are checked: a second one, or one on another map, is put out and the player told. Postfix on the rare tick.</summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.TickRare))]
public static class VehicleSurplusFiresPatch
{
    public static void Postfix(VehiclePawn __instance)
    {
        if (!__instance.Spawned) return;
        int count = VehicleFires.PutOutSurplus(__instance);
        if (count == 0) return;
        VehicleFrameworkPatchesMod.Logger.Info("Put out " + count + " fire(s) on " + __instance.LabelShort + " that could never go out.");
        if (__instance.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosVehicleFrameworkPatches_FiresPutOut".Translate(count.Named("COUNT"), __instance.LabelShort.Named("VEHICLE")).Resolve(),
                __instance, MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }
}
