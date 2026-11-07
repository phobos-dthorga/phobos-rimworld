using System.Collections.Generic;
using System.Linq;
using Phobos.Medical;
using Phobos.MedicalSaveBridge.Rules;
using Verse;

namespace Phobos.MedicalSaveBridge.Services;

/// <summary>What the player chose for one load of one save.</summary>
public enum BridgeChoice
{
    /// <summary>Nobody asked (another mod loaded the save directly, or there was nothing worth asking): act as before the choice existed.</summary>
    None,
    Convert,
    NotNow,
    Never,
}

/// <summary>What converting a save would change, worked out from its header before it loads.</summary>
public sealed class BridgeNeed
{
    public static readonly BridgeNeed Nothing = new(new List<string>(), new List<string>(), false);

    public BridgeNeed(List<string> modNames, List<string> bodyLabels, bool moreInjuriesNew)
    {
        ModNames = modNames;
        BodyLabels = bodyLabels;
        MoreInjuriesNew = moreInjuriesNew;
    }

    /// <summary>The mods new to the save that add body parts, or More Injuries.</summary>
    public List<string> ModNames { get; }
    /// <summary>The bodies whose saved part numbers would shift.</summary>
    public List<string> BodyLabels { get; }
    public bool MoreInjuriesNew { get; }

    /// <summary>
    /// Worth a question only when loading without the bridge would lose something: shifted body parts, or More Injuries' first-load
    /// wipe. Any other new mod leaves the bridge only the hediff class upgrade, which acts only where a class no longer matches, as
    /// before (agent choice, 7 November 2026: asking on every new mod would ask about almost every load).
    /// </summary>
    public bool WorthAsking => BodyLabels.Count > 0 || MoreInjuriesNew;
}

/// <summary>
/// The player's choice before a save is converted (owner's call, 7 November 2026: convert now, not now, or never for this save).
/// The load menu asks (<see cref="Patches.LoadChoicePatches"/>), the choice is kept here for that one load, and the header gate
/// (<see cref="Patches.SaveHeaderPatches"/>) takes it when the save really loads. "Never" is remembered per save file in the mod's
/// settings until the player clears it there. A load that no menu announced (another mod's quick-load) acts as before: it converts.
/// </summary>
public static class BridgeChoices
{
    const int MaxBodiesNamed = 6;

    static string? pendingFile;
    static BridgeChoice pending = BridgeChoice.None;
    static string? loadingFile;

    /// <summary>Both loader prefixes are in place, so the gate can tell the menu's header read from a real load.</summary>
    public static bool LoaderHooked { get; internal set; }

    /// <summary>A save is really loading now (between the loader's start and the header read).</summary>
    public static bool Loading => loadingFile != null;

    public static void NotifyLoadingFile(string fileName) => loadingFile = fileName;

    /// <summary>The choice for the load now reading its header, taken once.</summary>
    public static BridgeChoice TakeForThisLoad()
    {
        string? file = loadingFile;
        loadingFile = null;
        if (file == null) return BridgeChoice.None;
        if (file == pendingFile)
        {
            BridgeChoice choice = pending;
            pendingFile = null;
            pending = BridgeChoice.None;
            return choice;
        }
        return IsNever(file) ? BridgeChoice.Never : BridgeChoice.None;
    }

    public static bool IsNever(string fileName) => MedicalSaveBridgeMod.Settings.neverConvert.Contains(fileName);

    /// <summary>What converting would change, from the header the menu has just read (ScribeMetaHeaderUtility.loadedModIdsList).</summary>
    public static BridgeNeed Assess(List<string>? savedModIds)
    {
        HashSet<string> newIds = ModListRule.NewModIds(savedModIds, LoadedModManager.RunningMods.Select(mod => mod.PackageId));
        if (newIds.Count == 0) return BridgeNeed.Nothing;
        var partMods = new HashSet<string>();
        var bodies = new List<string>();
        foreach (BodyDef body in DefDatabase<BodyDef>.AllDefs)
        {
            bool changed = false;
            foreach (BodyPartRecord part in body.AllParts)
            {
                string? owner = part.def?.modContentPack?.PackageId?.ToLowerInvariant();
                if (owner == null || !newIds.Contains(owner)) continue;
                partMods.Add(owner);
                changed = true;
            }
            if (changed) bodies.Add(body.LabelCap.ToString());
        }
        bool moreInjuries = newIds.Contains(MedicalSaveBridgeMod.MoreInjuriesPackageId.ToLowerInvariant());
        if (moreInjuries) partMods.Add(MedicalSaveBridgeMod.MoreInjuriesPackageId.ToLowerInvariant());
        List<string> names = LoadedModManager.RunningMods.Where(mod => partMods.Contains(mod.PackageId.ToLowerInvariant())).Select(mod => mod.Name).ToList();
        return new BridgeNeed(names, bodies, moreInjuries);
    }

    /// <summary>Called by the load menu once its own checks pass: ask if it's worth asking, otherwise load at once.</summary>
    public static void AskThenLoad(string fileName)
    {
        BridgeNeed need = Assess(ScribeMetaHeaderUtility.loadedModIdsList);
        if (!need.WorthAsking || IsNever(fileName))
        {
            GameDataSaveLoader.LoadGame(fileName);
            return;
        }
        Find.WindowStack.Add(UI.BridgeChoiceDialog.Make(fileName, need));
    }

    /// <summary>The dialog's buttons: keep the choice for this save's next load, remember "never", then load.</summary>
    public static void ChooseAndLoad(string fileName, BridgeChoice choice)
    {
        pendingFile = fileName;
        pending = choice;
        if (choice == BridgeChoice.Never && !IsNever(fileName))
        {
            MedicalSaveBridgeMod.Settings.neverConvert.Add(fileName);
            MedicalMod.WriteAll();
        }
        Log.Message(BridgeState.LogPrefix + " " + fileName + ": the player chose " + choice + ".");
        GameDataSaveLoader.LoadGame(fileName);
    }

    /// <summary>The settings button: every save marked "never" is asked about again on its next load.</summary>
    public static void ForgetNever()
    {
        MedicalSaveBridgeMod.Settings.neverConvert.Clear();
        MedicalMod.WriteAll();
    }

    /// <summary>The bodies, named for the dialog: the first few, then how many more.</summary>
    public static string NameBodies(List<string> bodies) =>
        bodies.Count <= MaxBodiesNamed
            ? string.Join(", ", bodies)
            : string.Join(", ", bodies.Take(MaxBodiesNamed)) + " " + "PhobosSaveBridge_AndMore".Translate((bodies.Count - MaxBodiesNamed).Named("COUNT"));
}
