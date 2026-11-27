using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Verse;

namespace Phobos.Framework.Setup;

/// <summary>
/// One setting the setup wizard can show and set (Framework 0.362.0; docs/development/first-load-setup-wizard.md). Its value travels as
/// text, the key a page's presets name in XML: "true" or "false" for a tick, the option's own name for a choice. The mod that owns it
/// registers it once, with the label its settings window already shows, so both say the same thing; the settings window stays the master.
/// </summary>
public sealed class SetupSetting
{
    public readonly string Id;
    /// <summary>The Mod whose settings file holds it, written when the wizard finishes.</summary>
    public readonly Mod Owner;
    public readonly string Default;
    public readonly bool Restart;
    public readonly bool IsTick;
    public readonly IReadOnlyList<string> Options;
    readonly Func<string> label;
    readonly Func<string?>? tip;
    readonly Func<string> get;
    readonly Action<string> set;
    readonly Func<string, string> optionLabel;

    internal SetupSetting(string id, Mod owner, Func<string> label, Func<string?>? tip, Func<string> get, Action<string> set, string byDefault,
        IReadOnlyList<string> options, Func<string, string> optionLabel, bool restart, bool isTick)
    {
        Id = id;
        Owner = owner;
        this.label = label;
        this.tip = tip;
        this.get = get;
        this.set = set;
        Default = byDefault;
        Options = options;
        this.optionLabel = optionLabel;
        Restart = restart;
        IsTick = isTick;
    }

    public string Label => label();
    public string? Tip => tip?.Invoke();
    public string Current => get();
    public string OptionLabel(string key) => optionLabel(key);

    /// <summary>Sets it to one of its own options; anything else (a preset naming a value this setting does not have) is passed over.</summary>
    public bool Set(string key)
    {
        string? option = Options.FirstOrDefault(o => string.Equals(o, key, StringComparison.OrdinalIgnoreCase));
        if (option == null) return false;
        set(option);
        return true;
    }
}

/// <summary>
/// The settings the setup wizard knows, by ID (Framework 0.362.0). A mod registers its own from its Mod constructor; a page Def names them.
/// The wizard never knows a mod's settings class.
/// </summary>
public static class SetupSettings
{
    static readonly Dictionary<string, SetupSetting> all = new(StringComparer.Ordinal);

    public static SetupSetting? Get(string id) => all.TryGetValue(id, out SetupSetting? setting) ? setting : null;

    /// <summary>A tick: on or off.</summary>
    public static void Tick(string id, Mod owner, Func<string> label, Func<string?>? tip, Func<bool> get, Action<bool> set, bool byDefault,
        bool restart = false)
    {
        all[id] = new SetupSetting(id, owner, label, tip, () => Key(get()), key => set(key == "true"), Key(byDefault), new[] { "true", "false" },
            key => (key == "true" ? "PhobosFramework_SetupOn" : "PhobosFramework_SetupOff").Translate(), restart, isTick: true);
    }

    /// <summary>A choice among options; each option's key is its own name (an enum's name, a number in invariant figures).</summary>
    public static void Choice<T>(string id, Mod owner, Func<string> label, Func<string?>? tip, Func<T> get, Action<T> set, T byDefault,
        IEnumerable<T> options, Func<T, string> optionLabel, bool restart = false) where T : notnull
    {
        List<T> list = options.ToList();
        var keys = list.Select(o => Key(o)).ToList();
        all[id] = new SetupSetting(id, owner, label, tip, () => Key(get()), key =>
            {
                int at = keys.IndexOf(key);
                if (at >= 0) set(list[at]);
            }, Key(byDefault), keys, key =>
            {
                int at = keys.IndexOf(key);
                return at >= 0 ? optionLabel(list[at]) : key;
            }, restart, isTick: false);
    }

    static string Key(object value) => value switch
    {
        bool b => b ? "true" : "false",
        float f => f.ToString("0.###", CultureInfo.InvariantCulture),
        double d => d.ToString("0.###", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };
}
