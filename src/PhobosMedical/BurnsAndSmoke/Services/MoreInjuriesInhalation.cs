using System.Reflection;
using Phobos.Framework;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// More Injuries' own fire inhalation (it burns both lungs whenever a burning pawn takes burn damage), read from its settings so the
/// player's choice there decides (owner, 7 Nov 2026: respect it, rather than removing its worker as earlier versions did). The
/// setting is an internal field behind a generated property (More Injuries 1.6.0.2), so it is found by name through the framework.
/// </summary>
public static class MoreInjuriesInhalation
{
    const string ModType = "MoreInjuries.MoreInjuriesMod";
    const string SettingsMember = "Settings";
    const string SettingName = "enableFireInhalation";
    /// <summary>More Injuries' own default, used if its setting cannot be found.</summary>
    const bool MoreInjuriesDefault = true;

    static bool looked;
    static FieldInfo? settingField;

    /// <summary>True when More Injuries is active and its fire inhalation is switched on.</summary>
    public static bool On
    {
        get
        {
            if (!OptionalMods.IsActive(OptionalMods.MoreInjuries)) return false;
            var (type, settings) = OptionalMods.StaticMember(ModType, SettingsMember);
            if (!looked)
            {
                looked = true;
                settingField = OptionalMods.SettingField(type, SettingName, typeof(bool));
                if (settingField == null) Log.Warning(BurnsAndSmokeMod.LogPrefix + " could not read More Injuries' fire inhalation setting; assuming its default (on).");
            }
            if (settingField == null || settings == null) return MoreInjuriesDefault;
            return settingField.GetValue(settings) is true;
        }
    }
}
