using UnityEngine;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>
/// One per-pawn choice a Phobos mod offers the player, drawn by <see cref="CompPhobosPatient"/> as a gizmo with a menu. The
/// key is saved on the pawn and never changes; the labels are translation keys. The default comes from the mod's settings,
/// usually by the pawn's group, so a pawn with no saved choice follows the settings as they stand.
/// </summary>
public sealed class PatientPreference
{
    /// <summary>The saved key, prefixed with the mod's, stable for ever.</summary>
    public readonly string Key;
    /// <summary>The gizmo's label, with the current choice's label as {CHOICE}.</summary>
    public readonly string LabelKey;
    /// <summary>The gizmo's description.</summary>
    public readonly string DescKey;
    /// <summary>One label key per choice, in menu order; the saved value is the index.</summary>
    public readonly IReadOnlyList<string> OptionKeys;
    /// <summary>The choice a pawn with none saved follows.</summary>
    public readonly Func<Pawn, int> DefaultFor;
    /// <summary>Whether the gizmo shows for this pawn at all; null for every patient the doctors look after.</summary>
    public readonly Func<Pawn, bool>? Applies;
    /// <summary>A texture path for the gizmo; null or missing gives the game's industrial medicine icon.</summary>
    public readonly string? IconPath;

    public PatientPreference(string key, string labelKey, string descKey, IReadOnlyList<string> optionKeys, Func<Pawn, int> defaultFor,
        Func<Pawn, bool>? applies = null, string? iconPath = null)
    {
        IconPath = iconPath;
        Key = key;
        LabelKey = labelKey;
        DescKey = descKey;
        OptionKeys = optionKeys;
        DefaultFor = defaultFor;
        Applies = applies;
    }
}

/// <summary>
/// The registry: mods add their preferences and inspect lines at startup, the comp draws them. The icon and menu of a choice live here
/// so every place that offers it (the pawn's button, D'thorga Medical's health-tab button) draws the same thing.
/// </summary>
public static class PatientPreferences
{
    static Texture2D? defaultIcon;
    /// <summary>The game's industrial medicine icon, as on its medical care menu. The game has no texture of its own for "normal care".</summary>
    static Texture2D DefaultIcon => defaultIcon ??= ThingDefOf.MedicineIndustrial?.uiIcon ?? BaseContent.BadTex;
    static readonly Dictionary<string, Texture2D> icons = new();

    /// <summary>A preference's own icon when it names one that exists, else the default.</summary>
    public static Texture2D IconFor(PatientPreference preference)
    {
        if (string.IsNullOrEmpty(preference.IconPath)) return DefaultIcon;
        if (!icons.TryGetValue(preference.IconPath!, out Texture2D? icon))
        {
            icon = ContentFinder<Texture2D>.Get(preference.IconPath, false) ?? DefaultIcon;
            icons[preference.IconPath!] = icon;
        }
        return icon;
    }

    /// <summary>The choices as a menu; picking one saves it on the pawn's patient comp.</summary>
    public static List<FloatMenuOption> MenuFor(Pawn pawn, PatientPreference preference)
    {
        var options = new List<FloatMenuOption>();
        for (int i = 0; i < preference.OptionKeys.Count; i++)
        {
            int index = i;
            options.Add(new FloatMenuOption(preference.OptionKeys[i].Translate(), () => pawn.TryGetComp<CompPhobosPatient>()?.SetChoice(preference.Key, index)));
        }
        return options;
    }

    /// <summary>The choice's label with the pawn's current option, as on the pawn's button.</summary>
    public static string LabelFor(Pawn pawn, PatientPreference preference) =>
        preference.LabelKey.Translate(preference.OptionKeys[Choice(pawn, preference)].Translate().Named("CHOICE"));

    static readonly List<PatientPreference> all = new();
    static readonly List<Func<Pawn, string?>> inspectLines = new();

    public static IReadOnlyList<PatientPreference> All => all;
    public static IReadOnlyList<Func<Pawn, string?>> InspectLines => inspectLines;

    public static void Register(PatientPreference preference)
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Key == preference.Key)
            {
                all[i] = preference;
                return;
            }
        }
        all.Add(preference);
    }

    /// <summary>A line for the pawn's inspect pane, or null for nothing; keep it short and in the game's words.</summary>
    public static void RegisterInspectLine(Func<Pawn, string?> provider) => inspectLines.Add(provider);

    public static PatientPreference? Find(string key)
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Key == key) return all[i];
        }
        return null;
    }

    /// <summary>The pawn's choice for a preference: saved on the pawn, else the preference's default for them.</summary>
    public static int Choice(Pawn pawn, PatientPreference preference)
    {
        CompPhobosPatient? comp = pawn.TryGetComp<CompPhobosPatient>();
        int? saved = comp?.SavedChoice(preference.Key);
        int choice = saved ?? preference.DefaultFor(pawn);
        if (choice < 0) choice = 0;
        if (choice >= preference.OptionKeys.Count) choice = preference.OptionKeys.Count - 1;
        return choice;
    }
}
