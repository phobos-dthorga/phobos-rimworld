using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.CombatExtendedPatches.KeepGuns;

/// <summary>
/// Notes the gun a colonist lets fall when downed (Patches 0.26.0). The game remembers it too (Pawn_MindState.droppedWeapon, set by
/// DropAllEquipment when asked to remember the primary, read 19 Nov 2026 from the 1.7 decompile) and sends the person back for it, but it
/// forgets it the moment anything else goes into their hands (Pawn_EquipmentTracker.AddEquipment and the Equip job both clear it), and with
/// Combat Extended a grenade from the pack usually gets there first. Our own note survives that. A postfix that only reads; it never skips
/// or changes the game's drop.
/// </summary>
[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.DropAllEquipment), new[] { typeof(IntVec3), typeof(bool), typeof(bool) })]
public static class DroppedWhenDownedPatch
{
    public static void Postfix(Pawn_EquipmentTracker __instance, bool rememberPrimary)
    {
        if (!rememberPrimary || !CombatExtendedPatchesMod.Settings.keepGuns) return;
        Pawn? pawn = __instance.pawn;
        if (pawn == null || !KeepGunsRule.IsOurSoldier(pawn)) return;
        Thing? dropped = pawn.mindState?.droppedWeapon;
        if (dropped == null || !SingleUseWeapons.IsGun(dropped.def)) return;
        if (OwnGuns.For(pawn, make: true) is not { } own) return;
        own.DroppedId = dropped.thingIDNumber;
        own.GunDef = dropped.def.defName;
        own.StuffDef = dropped.Stuff?.defName;
        OwnGuns.Save();
        if (CombatExtendedPatchesMod.Logger.DebugEnabled)
            CombatExtendedPatchesMod.Logger.Debug(pawn.LabelShort + " was downed and let fall " + dropped.LabelShort + "; noted, to fetch it again.");
    }
}
