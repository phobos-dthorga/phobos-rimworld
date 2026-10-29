using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Generates the bench bills while the game generates its implied Defs, so they join the Def database
/// before references are resolved, as VFE Factory's own machining bills do. The game has no extension point
/// for this step. A postfix, so it runs after the game has made the recipes the bills point to and after
/// VFE Factory's prefix has made its machining bay bills. The storage switches are added here too, so the
/// game resolves and checks them like any comp written in XML.
/// </summary>
[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
public static class DefGeneratorPatch
{
    public static void Postfix(bool hotReload)
    {
        // A failure here must never stop the game from loading: the factory then just lacks the new bills.
        try
        {
            BenchBillGenerator.GenerateAll(hotReload);
        }
        catch (Exception error)
        {
            Log.Error($"[D'thorga Factory Additions] Could not set up the factory bills; the game goes on without them. {error}");
        }
        try
        {
            BeltAccessSetup.AddSwitches();
        }
        catch (Exception error)
        {
            Log.Error($"[D'thorga Factory Additions] Could not add the \"Belts may take from here\" switch; storage works as before. {error}");
        }
    }
}
