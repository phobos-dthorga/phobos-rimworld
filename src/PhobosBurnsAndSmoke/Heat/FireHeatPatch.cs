using HarmonyLib;
using Verse;

namespace Phobos.BurnsAndSmoke.Heat;

/// <summary>
/// Adds the hot zone around fires in the open to the game's cell temperature. The game answers every "how hot is it here"
/// question (a pawn's heatstroke and burns from hot air, hot and cold thoughts, plant growth, food spoiling, the cell inspector)
/// through this one method, and for an outdoor cell it answers with the weather, fire or no fire. A postfix, so every other
/// mod's answer comes first and ours is added to it; the game has no extension point for a cell's temperature. The map
/// component holds the zones, so this is one dictionary lookup, and nothing at all when no fire burns.
/// </summary>
[HarmonyPatch(typeof(GenTemperature), nameof(GenTemperature.TryGetTemperatureForCell))]
public static class FireHeatPatch
{
    public static void Postfix(IntVec3 c, Map map, bool __result, ref float tempResult)
    {
        if (__result && MapComponent_PhobosFireHeat.TryGetRise(map, c, out float degrees)) tempResult += degrees;
    }
}
