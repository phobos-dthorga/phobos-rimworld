using System;
using HarmonyLib;
using Phobos.Framework.Patching;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>
/// Rain soaks open fields (Framework 0.269.0; the owner's cards of 23 Nov 2026, farming record SL10): the map's ground wetness, which
/// rain fills within the hour, reaches the soil crops grow in. Off by default (owner: such things have cost noticeable performance
/// before), and while off neither patch is in the game at all (<see cref="SwitchedPatch"/>), so it costs nothing.
/// <para>Where a mod keeps the soil's water, it is that mod's figure (owner: "Dub's is preferred when available"): its tie-in registers
/// its own patch through <see cref="KeptBy"/>, which raises its figure while it rains and leaves the drying to it. Otherwise our own
/// patch makes open ground more fertile in proportion to the map's figure, drying as it dries (owner: "otherwise our own code takes
/// over"). Never both, so a field is never given the bonus twice.</para>
/// </summary>
public static class RainSoakedFields
{
    const double DefaultSoakedFactor = 1.8;

    static readonly Harmony harmony = new(FrameworkMod.PackageId + ".RainSoakedFields");
    static readonly SwitchedPatch ownFertility = new(harmony,
        () => AccessTools.Method("RimWorld.FertilityGrid:CalculateFertilityAt"),
        postfix: AccessTools.Method(typeof(RainSoakedFields), nameof(FertilityPostfix)));

    static SwitchedPatch? keeperPatch;
    static string? keeperName;
    static float soakedFactor = (float)DefaultSoakedFactor;
    static bool ownNoted, keeperNoted;

    /// <summary>How many times as fertile soaked open ground is by our own rule (the rules Def's figure, read when the patches are set).</summary>
    public static float SoakedFactor => soakedFactor;

    /// <summary>The mod whose figure for the soil's water rain fills, or null where ours is used.</summary>
    public static string? Keeper => keeperName;

    /// <summary>Dubs Bad Hygiene, whose tie-in (PhobosFramework.Hygiene, RainIrrigation) registers at its own start-up, which may come
    /// after the load line: until it registers or stands down, ours waits rather than going in for a moment.</summary>
    public const string DubsBadHygiene = "Dubwise.DubsBadHygiene";
    const string DubsBadHygieneName = "Dubs Bad Hygiene";

    static bool keeperDeclined;

    /// <summary>A tie-in for a mod that keeps the soil's water registers the patch that fills it, once, at start-up. Ours then stands
    /// down for good, whatever the setting.</summary>
    public static void KeptBy(string modName, SwitchedPatch patch)
    {
        keeperName = modName;
        keeperPatch = patch;
        Apply();
    }

    /// <summary>The tie-in found its mod keeps no water in the soil after all (Dubs' lite mode): ours does the job.</summary>
    public static void KeeperStandsDown()
    {
        keeperDeclined = true;
        Apply();
    }

    /// <summary>A keeper is active whose tie-in has not yet registered or stood down.</summary>
    static bool KeeperAwaited => keeperPatch == null && !keeperDeclined && OptionalMods.IsActive(DubsBadHygiene);

    /// <summary>Puts in or takes out whichever patch the setting and the installed mods call for. Run at start-up and when the setting
    /// changes; nothing happens when everything is already as asked.</summary>
    public static void Apply()
    {
        try
        {
            soakedFactor = (float)(PhobosGroundWetnessRulesDef.Current?.soakedFertilityFactor ?? DefaultSoakedFactor);
        }
        catch (Exception)
        {
            soakedFactor = (float)DefaultSoakedFactor;
        }
        bool on = FrameworkMod.Settings.rainSoaksFields;
        if (keeperPatch != null)
        {
            ownFertility.Set(false);
            keeperPatch.Set(on);
        }
        else
        {
            ownFertility.Set(on && !KeeperAwaited);
        }
    }

    /// <summary>The load line's words for this part, and whether it is on.</summary>
    public static (string text, bool on, string? why) Describe()
    {
        SwitchedPatch patch = keeperPatch ?? ownFertility;
        string? through = keeperName ?? (KeeperAwaited ? DubsBadHygieneName : null);
        string how = through != null
            ? "rain soaking open fields, through " + through + "'s irrigation"
            : "rain soaking open fields, open ground up to " + soakedFactor.ToString("0.##") + " times as fertile when soaked";
        if (!FrameworkMod.Settings.rainSoaksFields) return (how, false, "setting");
        // The tie-in registers after this line is written: its patch goes in then.
        if (KeeperAwaited) return (how + ", put in as its tie-in loads", true, null);
        if (!patch.Applied) return (how, false, patch.Problem ?? "not applied");
        return (how, true, null);
    }

    /// <summary>For a keeper's patch: the least its figure should be at a cell in the open while it rains, on its 0 to 100 scale.</summary>
    public static float IrrigationFloor(Map map) => (float)GroundWetnessRule.IrrigationTarget(MapComponent_PhobosGroundWetness.OpenOnQuick(map));

    /// <summary>For a keeper's patch, after it raised a cell: the seen-in-play note for testing.</summary>
    public static void NoteSoaked(float before, float after)
    {
        if (keeperNoted || !FrameworkMod.Settings.noteSeenInPlay) return;
        keeperNoted = true;
        if (Seen.Wants("fields.rainSoaks", null, "keeper")) Seen.Note("fields.rainSoaks", null, keeperName + " irrigation " + before.ToString("0") + " -> " + after.ToString("0"), "keeper");
    }

    /// <summary>
    /// Our own way, where no mod keeps the soil's water: open ground is more fertile in proportion to the map's wetness
    /// (GroundWetnessRule.FertilityFactor), never less than the game's own. Postfix on the game's FertilityGrid.CalculateFertilityAt,
    /// which everything that asks a cell's fertility goes through (plants' growth, sowing, the fertility overlay). In only while the
    /// setting is on; a roofed cell and infertile ground are left alone.
    /// </summary>
    static void FertilityPostfix(IntVec3 loc, ref float __result, Map ___map)
    {
        if (__result <= 0f) return;
        float wet = MapComponent_PhobosGroundWetness.OpenOnQuick(___map);
        if (wet <= 0f || loc.Roofed(___map)) return;
        float before = __result;
        __result *= (float)GroundWetnessRule.FertilityFactor(wet, soakedFactor);
        // Once a session: the notes' own check takes a lock, too dear for a method this busy.
        if (ownNoted || !FrameworkMod.Settings.noteSeenInPlay) return;
        ownNoted = true;
        if (Seen.Wants("fields.rainSoaks", null, "own")) Seen.Note("fields.rainSoaks", null, "fertility " + before.ToString("0.00") + " -> " + __result.ToString("0.00") + " at wetness " + wet.ToString("0.00"), "own");
    }
}
