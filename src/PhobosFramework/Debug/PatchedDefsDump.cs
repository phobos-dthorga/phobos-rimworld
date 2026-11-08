using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using RimWorld;
using Verse;

namespace Phobos.Framework.Debug;

/// <summary>
/// Writes every Def as the game holds it once every mod's XML patches have run, for phobos-modkit's Def tool
/// (docs/development/analysis-tooling.md, "Resolved-Defs dump"; owner's go-ahead, 8 Nov 2026). The game combines all mods' XML
/// into one document, applies every patch to it in load order, then reads Defs from it and lets it go. A patch operation of
/// ours (<see cref="PatchOperationPhobosKeepPatchedXml"/>, in this mod's Patches folder) keeps a reference to that document when
/// its turn comes; mods after us in load order go on patching the same document, so by the time the game has finished loading
/// it holds every patch. <see cref="PatchedDefsDumpWriter"/> then writes it and lets it go. Inheritance (ParentName) is not
/// applied: the file shows each Def as patched, before its parent's values are merged in.
/// One-shot and off by default: the dev setting asks for one file at the next start, and turns itself off once written, so the
/// document is never held and no time is spent otherwise. The file holds other authors' Defs, so it stays in the player's own
/// RimWorld folder and is never shared or committed.
/// </summary>
public static class PatchedDefsDump
{
    public const string FolderName = "PhobosPatchedDefs";
    public const string FileName = "PatchedDefs.xml";

    static XmlDocument? kept;

    public static string OutputPath => Path.Combine(GenFilePaths.SaveDataFolderPath, FolderName, FileName);

    /// <summary>Called by the patch operation: keeps the document when a dump was asked for.</summary>
    public static void Keep(XmlDocument document)
    {
        if (FrameworkMod.Settings.writePatchedDefsOnce) kept = document;
    }

    /// <summary>Writes the kept document, if any, then forgets it and turns the request off. Logs one line either way it ends.</summary>
    public static void WriteKept()
    {
        XmlDocument? document = kept;
        kept = null;
        if (!FrameworkMod.Settings.writePatchedDefsOnce) return;
        FrameworkMod.Settings.writePatchedDefsOnce = false;
        FrameworkMod.Settings.Write();
        if (document?.DocumentElement == null)
        {
            Log.Warning(FrameworkMod.LogPrefix + " A patched-Defs file was asked for, but the patched XML was not seen (another mod may apply "
                + "patches its own way). Nothing written; the request is off again.");
            return;
        }
        var watch = Stopwatch.StartNew();
        try
        {
            string path = OutputPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + ".writing";
            var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", Encoding = new System.Text.UTF8Encoding(false) };
            int count = 0;
            using (XmlWriter writer = XmlWriter.Create(temporary, settings))
            {
                writer.WriteStartDocument();
                writer.WriteComment(" Every Def after every mod's XML patches, before inheritance. Written by D'thorga Framework for "
                    + "phobos-modkit's Def tool. It holds other authors' Defs: keep it on this computer. ");
                writer.WriteStartElement("Defs");
                writer.WriteAttributeString("gameVersion", VersionControl.CurrentVersionStringWithRev);
                writer.WriteAttributeString("written", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteAttributeString("activeMods", LoadedModManager.RunningModsListForReading.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
                foreach (XmlNode node in document.DocumentElement.ChildNodes)
                {
                    if (node.NodeType != XmlNodeType.Element) continue;
                    node.WriteTo(writer);
                    count++;
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
            Log.Message(FrameworkMod.LogPrefix + " Wrote " + count + " Defs with every patch applied to " + path + " in "
                + watch.ElapsedMilliseconds + " ms. The request is off again.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or XmlException)
        {
            Log.Warning(FrameworkMod.LogPrefix + " The patched-Defs file could not be written: " + error.Message);
        }
    }
}

/// <summary>
/// The patch operation that hands the game's combined XML document to <see cref="PatchedDefsDump"/>. It changes nothing and
/// always succeeds; while no dump is asked for it does nothing at all.
/// </summary>
public class PatchOperationPhobosKeepPatchedXml : PatchOperation
{
    protected override bool ApplyWorker(XmlDocument xml)
    {
        PatchedDefsDump.Keep(xml);
        return true;
    }
}

/// <summary>Runs once the game has loaded every Def: writes the kept document when one was asked for.</summary>
[StaticConstructorOnStartup]
public static class PatchedDefsDumpWriter
{
    static PatchedDefsDumpWriter() => PatchedDefsDump.WriteKept();
}
