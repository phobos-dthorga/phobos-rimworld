using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Phobos.Framework;

/// <summary>
/// Combat Extended's ammunition, reached by name only (no reference to its assembly; owner rule for optional tie-ins). Read 16 Nov 2026
/// from our decompile of CE: Controller.settings is a public static field and Settings.EnableAmmoSystem a public property (the "enable
/// ammo system" option); an AmmoDef cooks off or detonates when hit only if its public cookOffProjectile or detonateProjectile is set
/// (AmmoThing.CanCookOffOrDetonate). Anything that cannot be read counts as off, so a CE update that moves these never sets off a load
/// (owner, 13 Nov 2026: cook-off only where CE's own ammunition settings allow it).
/// </summary>
public static class CombatExtendedAmmo
{
    const string ControllerType = "CombatExtended.Controller";
    const string AmmoDefType = "CombatExtended.AmmoDef";

    static bool looked;
    static FieldInfo? settingsField;
    static PropertyInfo? ammoSystemProperty;
    static FieldInfo? cookOffField;
    static FieldInfo? detonateField;
    static readonly Dictionary<ThingDef, bool> cooksOff = new();
    static int cachedTick = -1;
    static bool cachedOn;

    public static bool Active => OptionalMods.IsActive(OptionalMods.CombatExtended);

    /// <summary>CE is active and its ammunition system is on. Re-read once per tick in a game, so a settings change takes effect at once.</summary>
    public static bool SystemOn
    {
        get
        {
            if (!Active) return false;
            if (Current.Game == null) return ReadSystemOn();
            int tick = Find.TickManager.TicksGame;
            if (tick == cachedTick) return cachedOn;
            cachedTick = tick;
            cachedOn = ReadSystemOn();
            return cachedOn;
        }
    }

    /// <summary>Whether a thing of this Def is CE ammunition that cooks off or detonates in a fire.</summary>
    public static bool CooksOff(ThingDef def)
    {
        if (!Active) return false;
        if (cooksOff.TryGetValue(def, out bool known)) return known;
        Look();
        bool result = false;
        try
        {
            if (cookOffField != null && cookOffField.DeclaringType!.IsInstanceOfType(def))
            {
                result = cookOffField.GetValue(def) != null || detonateField?.GetValue(def) != null;
            }
        }
        catch
        {
            result = false;
        }
        cooksOff[def] = result;
        return result;
    }

    static void Look()
    {
        if (looked) return;
        looked = true;
        try
        {
            Type? controller = OptionalMods.TypeNamed(ControllerType);
            settingsField = controller?.GetField("settings", BindingFlags.Public | BindingFlags.Static);
            ammoSystemProperty = settingsField?.FieldType.GetProperty("EnableAmmoSystem", BindingFlags.Public | BindingFlags.Instance);
            Type? ammoDef = OptionalMods.TypeNamed(AmmoDefType);
            cookOffField = ammoDef?.GetField("cookOffProjectile", BindingFlags.Public | BindingFlags.Instance);
            detonateField = ammoDef?.GetField("detonateProjectile", BindingFlags.Public | BindingFlags.Instance);
        }
        catch (Exception exception)
        {
            FrameworkMod.Logger.Warn("Could not read Combat Extended's ammunition; ammunition will not cook off. " + exception.Message);
        }
    }

    static bool ReadSystemOn()
    {
        Look();
        if (settingsField == null || ammoSystemProperty == null || cookOffField == null) return false;
        try
        {
            object? settings = settingsField.GetValue(null);
            return settings != null && ammoSystemProperty.GetValue(settings) is true;
        }
        catch
        {
            return false;
        }
    }
}
