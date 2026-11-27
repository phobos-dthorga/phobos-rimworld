using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Phobos.Framework.Setup;

/// <summary>
/// One page of the setup wizard (Framework 0.362.0; docs/development/first-load-setup-wizard.md): a mod's introduction card and the
/// settings worth choosing before a colony. <c>label</c> is its title and <c>description</c> its introduction; the picture is the
/// mod's own preview. Any mod adds its pages in XML, so a new setting is a row here, never a change to the wizard.
/// </summary>
public class PhobosSetupPageDef : Def
{
    /// <summary>Where the page sits among the others; pages of equal order keep the mods' load order.</summary>
    public float order = 100f;
    /// <summary>Raise it when the page's choices change, so the wizard shows it again to players who saw an older one.</summary>
    public int version = 1;
    /// <summary>The settings, by the IDs their mods registered (<see cref="SetupSettings"/>), in the order shown.</summary>
    public List<string> settings = new();
    /// <summary>Named sets of values, shown as buttons; the settings they set are shown one by one only on request.</summary>
    public List<SetupPreset> presets = new();
    /// <summary>What this mod changes in other authors' mods, in plain words; shown in amber. Empty for none.</summary>
    [MustTranslate] public string? otherMods;
    /// <summary>Where the rest of its settings are, for the line under the rows.</summary>
    [MustTranslate] public string? moreSettings;
    /// <summary>The player guide's chapter for this mod, opened in the browser.</summary>
    public string? guideUrl;

    /// <summary>The presets as the game-free rule reads them.</summary>
    public List<IReadOnlyList<(string Setting, string Value)>> PresetValues() =>
        presets.Select(p => (IReadOnlyList<(string, string)>)p.values.Select(v => (v.setting, v.value)).ToList()).ToList();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        foreach (SetupPreset preset in presets)
        {
            if (preset.label.NullOrEmpty()) yield return "a preset has no label";
            foreach (SetupValue value in preset.values)
                if (!settings.Contains(value.setting)) yield return "preset " + preset.label + " sets " + value.setting + ", which is not in settings";
        }
    }
}

/// <summary>A named set of values: "As the game plays", the recommended one, a harsher one.</summary>
public class SetupPreset
{
    [MustTranslate] public string label = "";
    [MustTranslate] public string description = "";
    public bool recommended;
    public List<SetupValue> values = new();
}

/// <summary>One setting's value in a preset: the setting's ID and its value's key ("true", "false", an option's name).</summary>
public class SetupValue
{
    public string setting = "";
    public string value = "";
}
