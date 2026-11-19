using System;
using System.Collections.Generic;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Logging;

/// <summary>
/// Says when our code was on an error's path (Framework 0.170.0; docs/development/logging-framework-design.md, "Saying when our code was
/// on an error's path"). The owner asked (19 Nov 2026) that an error which passed through our code say so, as the log-reading mod they
/// remembered (Better Stacktraces) does for patches, so that a player's report reaches us and not only the other author whose code also
/// shows. Fed from the engine's log stream by <see cref="GameLogKeeper"/>, on whichever thread logged the error: errors only, never the
/// informational lines, which on 1.7 carry a trace too. ErrorAttributionRule reads the trace; a note is queued once per kind of error
/// and written on the main thread at the next frame of play (an error while a save loads is noted once the game runs), under our own
/// category, so it never feeds itself. At most ErrorAttributionRule.MaxNotesPerSession a session. It says where our code was, never
/// that it caused the error. Behind the setting "Say when D'thorga code was part of an error", on by default. Nothing is saved.
/// </summary>
public static class ErrorNotes
{
    readonly struct Pending
    {
        public readonly Attribution Found;
        public readonly string Quote;

        public Pending(Attribution found, string quote)
        {
            Found = found;
            Quote = quote;
        }
    }

    static readonly object gate = new();
    static readonly HashSet<string> seen = new();
    static readonly List<Pending> queue = new();
    static int written;
    static bool toldLimit;

    /// <summary>Notes written this session.</summary>
    public static int Written => written;

    /// <summary>Kinds of error seen with our code on their path this session, noted or not.</summary>
    public static int KindsSeen
    {
        get
        {
            lock (gate) return seen.Count;
        }
    }

    /// <summary>Reads one error from the log stream. Any thread; cheap when the trace shows nothing of ours.</summary>
    public static void Consider(string? category, string message, string? stackTrace, string signature)
    {
        if (!FrameworkMod.Settings.noteOurPartInErrors) return;
        if (category != null && category.StartsWith("D'thorga", System.StringComparison.Ordinal)) return;
        Attribution found = ErrorAttributionRule.Find(message, stackTrace);
        if (found.Kind == AttributionKind.None) return;
        lock (gate)
        {
            if (!seen.Add(signature)) return;
            if (written + queue.Count >= ErrorAttributionRule.MaxNotesPerSession) return;
            queue.Add(new Pending(found, ErrorAttributionRule.Quote(message)));
        }
    }

    /// <summary>Writes the queued notes. Main thread, once a frame of play, from the framework's game component.</summary>
    public static void Flush()
    {
        List<Pending>? due = null;
        lock (gate)
        {
            if (queue.Count > 0)
            {
                due = new List<Pending>(queue);
                queue.Clear();
            }
        }
        if (due != null)
            foreach (Pending pending in due) Write(pending);
        if (!toldLimit && written >= ErrorAttributionRule.MaxNotesPerSession)
        {
            toldLimit = true;
            HostLog.Write(GameLogKeeper.Category, HostLevel.Info, "That was the last of this session's " + ErrorAttributionRule.MaxNotesPerSession
                + " notes saying when D'thorga code was on an error's path; later errors are not checked for it.");
        }
    }

    static void Write(Pending pending)
    {
        string mod = ModOf(pending.Found.Where);
        string how = pending.Found.Kind == AttributionKind.OurFrame
            ? "passed through D'thorga code: " + pending.Found.Where + " (" + mod + ")"
            : "passed through a method " + mod + " patches (" + pending.Found.Where + ")";
        HostLog.Write(GameLogKeeper.Category, HostLevel.Warning, "The error \"" + pending.Quote + "\" " + how + ". D'thorga may have caused it, "
            + "or may only have been on the way. If you report it, please tell D'thorga too.");
        written++;
        string kind = pending.Found.Kind.ToString();
        if (Seen.Wants("framework.error-attributed", variant: kind)) Seen.Note("framework.error-attributed", what: pending.Found.Where, variant: kind);
    }

    /// <summary>The name of the mod whose assembly holds the type a note names; "a D'thorga mod" when it cannot be found.</summary>
    static string ModOf(string where)
    {
        try
        {
            string typeName = ErrorAttributionRule.TypeOf(where);
            Type? type = null;
            for (string name = typeName; type == null && name.Length > 0; name = ErrorAttributionRule.TypeOf(name) == name ? "" : ErrorAttributionRule.TypeOf(name))
                type = GenTypes.GetTypeInAnyAssembly(name);
            if (type == null) return "a D'thorga mod";
            foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
                if (pack.assemblies.loadedAssemblies.Contains(type.Assembly)) return pack.Name;
        }
        catch (Exception)
        {
            // A name that cannot be looked up is no reason to lose the note.
        }
        return "a D'thorga mod";
    }

    /// <summary>What has been noted this session, for the log-keeper's status dev action.</summary>
    public static string Status() => "Errors with D'thorga code on their path: " + KindsSeen + " kinds seen, " + written + " noted (at most "
        + ErrorAttributionRule.MaxNotesPerSession + "). Setting: " + (FrameworkMod.Settings.noteOurPartInErrors ? "on" : "off") + ".";
}
