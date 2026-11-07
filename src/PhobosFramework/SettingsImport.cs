using System;
using System.IO;
using System.Reflection;
using Verse;

namespace Phobos.Framework;

/// <summary>
/// Carries a player's settings across a move. The game names a settings file after the mod's folder and its Mod class
/// (Config/Mod_Folder_Class.xml), so a mod that changes either, as the D'thorga mods did when several were merged into one on
/// 7 November 2026, starts with defaults unless it reads the old file once. Read the old files only when the mod has no file of
/// its own yet; after the first save of the settings the new file wins and the old ones are never read again. The usual shape:
/// <code>
/// bool fresh = !SettingsImport.HasOwnFile(this);
/// Settings = GetSettings&lt;MySettings&gt;();
/// if (fresh &amp;&amp; SettingsImport.ImportInto(Settings, "OldFolder", "OldModClass")) LongEventHandler.ExecuteWhenFinished(WriteSettings);
/// </code>
/// </summary>
public static class SettingsImport
{
    /// <summary>Whether <paramref name="mod"/> already has a settings file under its own folder and class.</summary>
    public static bool HasOwnFile(Mod mod) => File.Exists(PathFor(mod.Content.FolderName, mod.GetType().Name));

    /// <summary>
    /// The settings an older folder and Mod class left behind, read through the game's own reader, or null when there is no such
    /// file or it can't be read. The type must be the class the old file names (its Class attribute), or a base of it.
    /// </summary>
    public static T? ReadOld<T>(string oldFolderName, string oldModClassName) where T : ModSettings, new()
    {
        if (!File.Exists(PathFor(oldFolderName, oldModClassName))) return null;
        try
        {
            return LoadedModManager.ReadModSettings<T>(oldFolderName, oldModClassName);
        }
        catch (Exception error)
        {
            Log.Warning(FrameworkMod.LogPrefix + " Could not read the old settings of " + oldFolderName + "; that part starts with defaults. " + error.Message);
            return null;
        }
    }

    /// <summary>
    /// Copies the old file's values into <paramref name="target"/>, the object the game already handed the mod (GetSettings), so
    /// the game keeps saving that one. False when there is no old file. Every field below ModSettings is copied, shallowly.
    /// </summary>
    public static bool ImportInto<T>(T target, string oldFolderName, string oldModClassName) where T : ModSettings, new()
    {
        T? old = ReadOld<T>(oldFolderName, oldModClassName);
        if (old == null) return false;
        for (Type? type = typeof(T); type != null && type != typeof(ModSettings); type = type.BaseType)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                field.SetValue(target, field.GetValue(old));
            }
        }
        return true;
    }

    /// <summary>The game's own naming (LoadedModManager.GetSettingsFilename, private in 1.6 and 1.7, read 7 Nov 2026).</summary>
    static string PathFor(string folderName, string modClassName) =>
        Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename("Mod_" + folderName + "_" + modClassName + ".xml"));
}
