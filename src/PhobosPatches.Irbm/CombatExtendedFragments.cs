using System.Reflection;
using Phobos.Framework;
using Phobos.MissilePatches.Rules;

namespace Phobos.MissilePatches;

/// <summary>
/// Combat Extended's wall fragments during a strike, by name: no reference to Combat Extended's assembly. CE's prefix on
/// DamageWorker.Apply spawns up to a hundred medium fragments for every building that takes heavy damage, and drains them at once
/// (CombatExtended.HarmonyCE.Harmony_DamageWorker_Apply, read 7 Nov 2026). The missile mod pauses the game for a strike, so the
/// fragments never fly away; they pile up in the map's lists of things, and each one slows every later removal. That is most of why
/// the first spread-out blast still crawled in the owner's game. CE's setting (Controller.settings, fields fragmentsFromWalls and
/// fragmentsFromWallsIntensity) is changed in memory for the length of one slice and put back after; it is never written to CE's
/// settings file.
/// </summary>
internal static class CombatExtendedFragments
{
    const string ControllerType = "CombatExtended.Controller";
    const string EnabledField = "fragmentsFromWalls";
    const string IntensityField = "fragmentsFromWallsIntensity";

    /// <summary>The "Reduced" choice: a tenth of CE's own intensity (agent default).</summary>
    public const float ReducedFactor = 0.1f;

    static bool looked;
    static FieldInfo? settingsField;
    static FieldInfo? enabledField;
    static FieldInfo? intensityField;

    /// <summary>A strike's change to CE's setting, undone by <see cref="Restore"/>.</summary>
    public readonly struct Override
    {
        public Override(object settings, bool enabled, float intensity)
        {
            Settings = settings;
            Enabled = enabled;
            Intensity = intensity;
        }

        public object? Settings { get; }
        public bool Enabled { get; }
        public float Intensity { get; }
    }

    /// <summary>Combat Extended is active and its fragments setting was found by name.</summary>
    public static bool Present
    {
        get
        {
            Look();
            return enabledField != null && SettingsObject != null;
        }
    }

    /// <summary>CE's own setting: whether it makes wall fragments at all.</summary>
    public static bool OnInCombatExtended => Present && enabledField!.GetValue(SettingsObject) is true;

    /// <summary>Whether a strike's building damage makes fragments under the player's choice.</summary>
    public static bool Live(FragmentMode mode) => mode != FragmentMode.None && OnInCombatExtended;

    static object? SettingsObject => settingsField?.GetValue(null);

    public static Override Apply(FragmentMode mode)
    {
        if (mode == FragmentMode.AsCombatExtended || !Present) return default;
        object settings = SettingsObject!;
        bool enabled = enabledField!.GetValue(settings) is true;
        float intensity = intensityField?.GetValue(settings) is float value ? value : 1f;
        if (mode == FragmentMode.None) enabledField.SetValue(settings, false);
        else if (intensityField != null) intensityField.SetValue(settings, intensity * ReducedFactor);
        return new Override(settings, enabled, intensity);
    }

    public static void Restore(Override change)
    {
        if (change.Settings == null) return;
        enabledField!.SetValue(change.Settings, change.Enabled);
        intensityField?.SetValue(change.Settings, change.Intensity);
    }

    static void Look()
    {
        if (looked) return;
        looked = true;
        if (!OptionalMods.IsActive(OptionalMods.CombatExtended)) return;
        settingsField = OptionalMods.TypeNamed(ControllerType)?.GetField("settings", BindingFlags.Public | BindingFlags.Static);
        System.Type? type = settingsField?.FieldType;
        FieldInfo? enabled = type?.GetField(EnabledField, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        FieldInfo? intensity = type?.GetField(IntensityField, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        enabledField = enabled?.FieldType == typeof(bool) ? enabled : null;
        intensityField = intensity?.FieldType == typeof(float) ? intensity : null;
    }
}
