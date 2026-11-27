using System.Collections.Concurrent;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The game's food search finds a paste dispenser only when its class is exactly the game's own: the food-source groups test
/// <c>def.thingClass == typeof(Building_NutrientPasteDispenser)</c>, so a subclass such as the Billy's paste tap
/// (<see cref="Building_PhobosPasteTap"/>) was never offered to a hungry colonist, a warden or a nurse (the owner's card of 28 Nov 2026,
/// "also let people eat from it"; row 453 had "a tap colonists ignore" to watch for). A postfix on the game's own group test counts any
/// subclass too, for the two food-source groups only. Vanilla Nutrient Paste Expanded and its Reimagined Progression add their own taps
/// to the same groups by name in postfixes of their own; ours only ever turns a false into a true, so theirs and ours stand side by side.
/// A permanent patch of ours on the game's own code, not another author's fault.
/// <para>Only a subclass the game's search can use safely counts: one with a powered comp (the search reads <c>powerComp.PowerOn</c>) and an
/// interaction cell (where the eater stands), so another mod's dispenser subclass without them never reaches the search. The answer is
/// kept per Def, as the test runs for every group each time a thing is listed on a map.</para>
/// </summary>
[HarmonyPatch(typeof(ThingListGroupHelper), nameof(ThingListGroupHelper.Includes), typeof(ThingRequestGroup), typeof(ThingDef))]
public static class PasteTapFoodSourcePatch
{
    static readonly ConcurrentDictionary<ThingDef, bool> dispenserSubclass = new();

    public static void Postfix(ThingRequestGroup group, ThingDef def, ref bool __result)
    {
        if (__result || def == null || (group != ThingRequestGroup.FoodSourceNotPlantOrTree && group != ThingRequestGroup.FoodSource)) return;
        if (dispenserSubclass.GetOrAdd(def, IsServingSubclass)) __result = true;
    }

    /// <summary>A paste dispenser's subclass the food search can use: a powered comp and an interaction cell.</summary>
    static bool IsServingSubclass(ThingDef def)
    {
        if (def.thingClass == null || def.thingClass == typeof(Building_NutrientPasteDispenser)
            || !typeof(Building_NutrientPasteDispenser).IsAssignableFrom(def.thingClass) || !def.hasInteractionCell || def.comps == null) return false;
        foreach (CompProperties props in def.comps)
        {
            if (props?.compClass != null && typeof(CompPowerTrader).IsAssignableFrom(props.compClass)) return true;
        }
        return false;
    }
}
