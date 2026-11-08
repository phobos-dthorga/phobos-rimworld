using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace Phobos.Framework.Gases;

/// <summary>
/// Combat Extended's smoke, reached by name only (no reference to its assembly). CE's roofed fires make black smoke and its
/// SmokeInhalation condition already accounts for masks, sleep, being downed and breathing capacity, so while CE runs with its
/// "smoke effects" setting on, that condition is the smoke signal the framework hands out (owner's rule: reuse an existing
/// effect before making one). Read 4 Nov 2026 from CE 16.7.4: Controller.settings is a public static field, Settings.SmokeEffects
/// a public property, the hediff defName "SmokeInhalation", the stat "SmokeSensitivity".
/// </summary>
public static class CombatExtendedSmoke
{
    public const string SmokeInhalationDefName = "SmokeInhalation";
    public const string SmokeSensitivityStatName = "SmokeSensitivity";

    static bool looked;
    static FieldInfo? settingsField;
    static PropertyInfo? smokeEffectsProperty;
    static int cachedTick = -1;
    static bool cachedInUse;

    public static bool Active => OptionalMods.IsActive(OptionalMods.CombatExtended);

    /// <summary>
    /// CE is active and its smoke effects are on. Re-read once per tick, so a settings change takes effect at once. Outside a
    /// game (the main menu, the load report) there is no tick manager: Find.TickManager dereferences Current.Game and throws,
    /// so the setting is read directly and not cached.
    /// </summary>
    public static bool InUse
    {
        get
        {
            if (!Active) return false;
            if (Current.Game == null) return ReadSmokeEffects();
            int tick = Find.TickManager.TicksGame;
            if (tick == cachedTick) return cachedInUse;
            cachedTick = tick;
            cachedInUse = ReadSmokeEffects();
            return cachedInUse;
        }
    }

    public static HediffDef? SmokeInhalationDef => Active ? OptionalMods.HediffDefNamed(SmokeInhalationDefName) : null;

    /// <summary>CE's own inhalation severity on the pawn, 0 when absent.</summary>
    public static float Severity(Pawn pawn)
    {
        HediffDef? def = SmokeInhalationDef;
        if (def == null) return 0f;
        Hediff? hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(def);
        return hediff?.Severity ?? 0f;
    }

    /// <summary>CE's smoke sensitivity stat (1 bare-faced, 0 or below in a sealed helmet), or 1 without CE.</summary>
    public static float Sensitivity(Pawn pawn)
    {
        if (!Active) return 1f;
        var stat = OptionalMods.StatDefNamed(SmokeSensitivityStatName);
        return stat == null ? 1f : pawn.GetStatValue(stat);
    }

    static bool ReadSmokeEffects()
    {
        if (!looked)
        {
            looked = true;
            try
            {
                Type? controller = OptionalMods.TypeNamed("CombatExtended.Controller");
                settingsField = controller?.GetField("settings", BindingFlags.Public | BindingFlags.Static);
                smokeEffectsProperty = settingsField?.FieldType.GetProperty("SmokeEffects", BindingFlags.Public | BindingFlags.Instance);
            }
            catch (Exception exception)
            {
                FrameworkMod.Logger.Warn("Could not read Combat Extended's smoke setting; assuming it is on. " + exception.Message);
            }
        }
        if (settingsField == null || smokeEffectsProperty == null) return true;
        try
        {
            object? settings = settingsField.GetValue(null);
            return settings == null || smokeEffectsProperty.GetValue(settings) is not bool on || on;
        }
        catch
        {
            return true;
        }
    }
}
