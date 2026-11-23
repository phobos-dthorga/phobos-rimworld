using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Phobos.Shared;

/// <summary>
/// Applies an assembly's patch classes one at a time, so a target that a game or mod update renames switches off only that part,
/// with one log line, instead of every patch in the mod. Compiled into each project that sets UsesHarmony (Directory.Build.targets),
/// so the code lives once in source; D'thorga Framework compiles it too, as it has used Harmony since 0.205.0. Internal, so no two
/// mods' copies can clash.
/// </summary>
internal static class HarmonyPatchApplier
{
    public static void ApplyEach(Harmony harmony, Assembly assembly, string logPrefix)
    {
        foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
        {
            if (!type.IsDefined(typeof(HarmonyPatch), inherit: false)) continue;
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception error)
            {
                Log.Error(logPrefix + " Could not apply " + type.Name + "; that part is switched off. " + error);
            }
        }
    }
}
