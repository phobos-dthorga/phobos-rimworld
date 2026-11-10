using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.Mood;

/// <summary>
/// Writes the colony mood file at every save (docs/development/mood-snapshot.md; owner's go-ahead, 10 Nov 2026). A save holds only
/// remembered thoughts; this file adds what the game works out live (rooms, pain, beliefs), so phobos-modkit's mood report can
/// name every cause. It runs while the game writes its save and writes a file of its own beside it; it never touches the save.
/// </summary>
public sealed class ColonyMoodFileComponent : GameComponent
{
    public ColonyMoodFileComponent(Game game)
    {
    }

    public override void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving && FrameworkMod.Settings.writeColonyMoodFile) ColonyMoodFile.Write();
    }
}

/// <summary>Builds and writes the file: every colonist's, slave's and prisoner's thought groups as the game adds them up.</summary>
public static class ColonyMoodFile
{
    public const string FolderName = "PhobosSnapshots";
    public const string FileName = "ColonyMood.xml";
    /// <summary>The file's shape; phobos-modkit reads it and refuses one it does not know.</summary>
    public const int Format = 1;

    static bool failureLogged;
    static readonly List<Thought> groups = new();
    static readonly List<Thought> members = new();

    public static string OutputPath => Path.Combine(GenFilePaths.SaveDataFolderPath, FolderName, FileName);

    /// <summary>colonist, slave or prisoner for the colony's own pawns with a mood; null for everyone else.</summary>
    public static string? GroupOf(Pawn pawn)
    {
        if (pawn.needs?.mood == null) return null;
        if (pawn.IsPrisonerOfColony) return "prisoner";
        if (pawn.IsSlaveOfColony) return "slave";
        if (pawn.IsColonist) return "colonist";
        return null;
    }

    /// <summary>Writes the file. A failure is logged once a session and never reaches the save being written.</summary>
    public static void Write()
    {
        try
        {
            WriteFile();
        }
        catch (Exception problem)
        {
            if (failureLogged) return;
            failureLogged = true;
            FrameworkMod.Logger.Warn("The colony mood file could not be written (" + problem.Message + "). The save itself is "
                + "unaffected. This is said once a session.");
        }
    }

    static string Number(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    static void WriteFile()
    {
        string path = OutputPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".writing";
        var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", Encoding = new System.Text.UTF8Encoding(false) };
        using (XmlWriter writer = XmlWriter.Create(temporary, settings))
        {
            writer.WriteStartDocument();
            writer.WriteComment(" Every colonist's, slave's and prisoner's thoughts as the game added them up at the last save. Written "
                + "by D'thorga Framework for phobos-modkit's mood report; it matches a save by its tick count and world seed. ");
            writer.WriteStartElement("ColonyMood");
            writer.WriteAttributeString("format", Format.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("gameVersion", VersionControl.CurrentVersionStringWithRev);
            writer.WriteAttributeString("ticksGame", Find.TickManager.TicksGame.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("seed", Find.World?.info?.seedString ?? "");
            writer.WriteAttributeString("written", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("difficultyMoodOffset", Number(Find.Storyteller?.difficulty?.colonistMoodOffset ?? 0f));
            var seen = new HashSet<Pawn>();
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    if (seen.Add(pawn) && GroupOf(pawn) is string group) WritePawn(writer, pawn, group);
                }
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        if (File.Exists(path)) File.Delete(path);
        File.Move(temporary, path);
    }

    static void WritePawn(XmlWriter writer, Pawn pawn, string group)
    {
        Need_Mood mood = pawn.needs!.mood!;
        MentalBreaker? breaker = pawn.mindState?.mentalBreaker;
        writer.WriteStartElement("pawn");
        writer.WriteAttributeString("id", pawn.ThingID);
        writer.WriteAttributeString("name", pawn.LabelShortCap);
        writer.WriteAttributeString("group", group);
        writer.WriteAttributeString("mood", Number(mood.CurLevel));
        writer.WriteAttributeString("instantMood", Number(mood.CurInstantLevel));
        if (breaker != null)
        {
            writer.WriteAttributeString("minor", Number(breaker.BreakThresholdMinor));
            writer.WriteAttributeString("major", Number(breaker.BreakThresholdMajor));
            writer.WriteAttributeString("extreme", Number(breaker.BreakThresholdExtreme));
        }
        groups.Clear();
        mood.thoughts.GetDistinctMoodThoughtGroups(groups);
        foreach (Thought thought in groups)
        {
            float offset = mood.thoughts.MoodOffsetOfGroup(thought);
            if (offset == 0f) continue;
            members.Clear();
            mood.thoughts.GetMoodThoughts(thought, members);
            writer.WriteStartElement("thought");
            writer.WriteAttributeString("def", thought.def.defName);
            writer.WriteAttributeString("label", thought.LabelCap);
            writer.WriteAttributeString("kind", thought is Thought_Memory ? "memory" : "situational");
            writer.WriteAttributeString("count", members.Count.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("mood", Number(offset));
            if (thought.sourcePrecept?.def != null) writer.WriteAttributeString("precept", thought.sourcePrecept.def.defName);
            writer.WriteEndElement();
        }
        groups.Clear();
        members.Clear();
        writer.WriteEndElement();
    }
}
