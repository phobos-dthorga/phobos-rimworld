using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The Billy's paste tap is a nutrient paste dispenser filled from a tanker, never from a hopper, so the game's "Need food hopper" alert has
/// nothing to ask of it (fault-read 25 Nov 2026, B3): the alert looks only for a hopper beside each dispenser, so the tap stood in it for as
/// long as it was set out. Vanilla Nutrient Paste Expanded clears its own piped dispensers from the same list, and the tap has no pipe. A
/// postfix on the alert's private list, taking our taps out; a permanent patch of ours on the game's own alert, not another author's fault.
/// </summary>
[HarmonyPatch(typeof(Alert_PasteDispenserNeedsHopper), "BadDispensers", MethodType.Getter)]
public static class PasteTapAlertPatch
{
    public static void Postfix(List<Thing> __result) => __result?.RemoveAll(thing => thing is Building_PhobosPasteTap);
}
