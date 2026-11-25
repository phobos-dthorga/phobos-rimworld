using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.AircraftRearmPatches;

/// <summary>D'thorga Patches' part for one author's aircraft (potemkinvillager's Harrier, Hind, Mongoose, Close Air Support and Prowler),
/// switched on only while one of them is active (this assembly lives in a load folder for them). It began with rearming and from 0.41.0 also
/// holds the fixed-wing jets' sortie over a home map, both fixes reached by name in the authors' code. No settings: each fixes a fault.</summary>
public class AircraftRearmPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.43.0";
    public const string LogPrefix = "[D'thorga Patches]";

    public AircraftRearmPatchesMod(ModContentPack content) : base(content)
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Aircraft rearming and sorties");
        var harmony = new Harmony(PackageId);
        RearmOneKind.Apply(harmony, report);
        // The fixed-wing framework is in play with the Harrier, Close Air Support or the Prowler; the helicopters have none.
        if (ModsConfig.IsActive("potemkinvillager.av8bharrier") || ModsConfig.IsActive("potemkinvillager.CloseAirSupport")
            || ModsConfig.IsActive("potemkinvillager.ea6bprowler"))
        {
            SortieOverHome.Apply(harmony, report);
            SortieFuel.Apply(harmony, report);
        }
        LongEventHandler.ExecuteWhenFinished(report.Log);
    }
}
