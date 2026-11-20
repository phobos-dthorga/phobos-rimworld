using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Phobos.Farming;

/// <summary>
/// "Agricultural Frame: hay only from straw crops" (owner, 20 Nov 2026: the frame's hay from cabbages is unrealistic; a setting, on by
/// default; docs/development/farming-batch-two.md, "The Agricultural Frame's hay"). The Dead Man's Switch - Legion's Agricultural Frame
/// drops extra hay with every plant it harvests or cuts, through its comp's properties (DMS_Legion.CompProperties_AgriculturalFrameHay,
/// read from a decompile of its 1.7 build, 20 Nov 2026: no public source). Those properties carry the author's own list of the plants it
/// may drop hay for, empty meaning every plant. With the setting on, the list is filled with the plants whose crop class leaves straw or
/// forage (PhobosCropClassDef.leavesStraw), so cabbages, fruit, root crops, cotton, flowers and herbs give none. The author's amounts are
/// kept. No Harmony patch and no reference to its assembly: its fields are read by their full name, only when the mod is loaded. The
/// list is read at every harvest, so a change of the setting takes effect at once; off, the author's values are put back.
/// </summary>
public static class AgriculturalFrameHay
{
    const string PropsTypeName = "DMS_Legion.CompProperties_AgriculturalFrameHay";

    /// <summary>The author's values on one frame's properties, kept so the setting can be turned off again.</summary>
    sealed class Original
    {
        public List<ThingDef>? allowed;
        public int min, max;
    }

    static readonly Dictionary<CompProperties, Original> originals = new();
    static bool looked;
    static System.Type? propsType;
    static FieldInfo? allowedPlantDefs, extraCountMin, extraCountMax;

    static bool Ready()
    {
        if (looked) return propsType != null;
        looked = true;
        propsType = AccessTools.TypeByName(PropsTypeName);
        if (propsType == null) return false;
        allowedPlantDefs = AccessTools.Field(propsType, "allowedPlantDefs");
        extraCountMin = AccessTools.Field(propsType, "extraCountMin");
        extraCountMax = AccessTools.Field(propsType, "extraCountMax");
        if (allowedPlantDefs == null || extraCountMin == null || extraCountMax == null)
        {
            FarmingMod.Logger.Warn("The Agricultural Frame's hay has changed shape; its hay is left as its author made it.");
            propsType = null;
        }
        return propsType != null;
    }

    /// <summary>Whether a plant's harvest leaves straw or forage, by its crop class. Trees never (the frame gives them none anyway).</summary>
    public static bool LeavesStraw(ThingDef plant) => plant.plant != null && !plant.plant.IsTree && Crops.ClassOf(plant)?.leavesStraw == true;

    /// <summary>Sets every frame's hay to the setting: straw crops only, or as its author made it. Call once Defs are loaded, and when
    /// the setting changes. Cheap: a pass over the Defs once.</summary>
    public static void Apply()
    {
        if (!Ready()) return;
        bool limit = FarmingMod.Settings.frameHayFromStrawCrops;
        List<ThingDef>? straw = null;
        int frames = 0;
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (def.comps == null) continue;
            foreach (CompProperties props in def.comps)
            {
                if (!propsType!.IsInstanceOfType(props)) continue;
                if (!originals.TryGetValue(props, out Original? original))
                {
                    original = new Original
                    {
                        allowed = (allowedPlantDefs!.GetValue(props) as List<ThingDef>)?.ToList(),
                        min = (int)(extraCountMin!.GetValue(props) ?? 0),
                        max = (int)(extraCountMax!.GetValue(props) ?? 0),
                    };
                    originals[props] = original;
                }
                if (!limit)
                {
                    allowedPlantDefs!.SetValue(props, original.allowed?.ToList());
                    extraCountMin!.SetValue(props, original.min);
                    extraCountMax!.SetValue(props, original.max);
                    continue;
                }
                straw ??= DefDatabase<ThingDef>.AllDefsListForReading.Where(LeavesStraw).ToList();
                // An author's (or another patch's) own narrower list is kept narrow: only its straw crops stay.
                List<ThingDef> allowed = original.allowed is { Count: > 0 } own ? own.Where(LeavesStraw).ToList() : straw.ToList();
                allowedPlantDefs!.SetValue(props, allowed);
                // An empty list means every plant to the frame, so with no straw crop at all it drops none rather than hay from anything.
                extraCountMin!.SetValue(props, allowed.Count > 0 ? original.min : 0);
                extraCountMax!.SetValue(props, allowed.Count > 0 ? original.max : 0);
                frames++;
                if (Seen.Wants("farming.frame-hay.limited"))
                {
                    Seen.Note("farming.frame-hay.limited", null, def.label,
                        values: new List<KeyValuePair<string, double>> { new("plants", allowed.Count) });
                }
            }
        }
        if (limit && frames > 0 && FarmingMod.Logger.DebugEnabled)
        {
            FarmingMod.Logger.Debug($"Agricultural Frame hay limited to {straw?.Count ?? 0} straw crops on {frames} frame Def(s).");
        }
    }
}
