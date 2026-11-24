using System;
using System.Collections.Generic;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>
/// Which version of each of our mods was running when (Framework 0.287.0; docs/development/test-automation.md, part 3). At each start of
/// a game, new or loaded, a line is added for every running D'thorga mod whose version differs from its last line: "tick|mod
/// name|version" (<see cref="VersionLedgerRule"/>). phobos-modkit's test-pass reads the lines from saves, so an event a vehicle saved (its
/// event log, its logbook) names the version that recorded it, and evidence older than a test row's change is never counted as fresh.
///
/// A few dozen short lines in a save, added only when a version changes; nothing is done during play. It lives in the core, which always
/// loads, so a save never names a class that can vanish (as GameComponent_PhobosPartStore).
/// </summary>
public sealed class GameComponent_PhobosVersionLedger : GameComponent
{
    /// <summary>The package ID our mods start with: those are the ones the ledger keeps.</summary>
    public const string PackagePrefix = "PhobosDthorga.";

    List<string> lines = new();

    public GameComponent_PhobosVersionLedger(Game game) { }

    /// <summary>The lines kept, oldest first.</summary>
    public IReadOnlyList<string> Lines => lines;

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        int now = Find.TickManager?.TicksGame ?? 0;
        foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
        {
            if (mod?.PackageId == null || !mod.PackageId.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase)) continue;
            string name = mod.Name ?? mod.PackageId;
            string version = mod.ModMetaData?.ModVersion ?? "";
            if (version.Length == 0 || !VersionLedgerRule.NeedsLine(lines, name, version)) continue;
            lines.Add(VersionLedgerRule.Line(now, name, version));
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref lines, "PhobosFramework_VersionLedger", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) lines ??= new List<string>();
    }
}
