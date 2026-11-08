using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Brutal.Logging;
using Verse;

namespace Phobos.Framework.Logging;

/// <summary>Levels for <see cref="HostLog"/>.</summary>
public enum HostLevel
{
    Info,
    Warning,
    Error,
}

/// <summary>
/// Writes a line under a log category of our own (shown in Player.log as "[D'thorga Framework]") through the 1.7 engine's logging
/// library, Brutal.Core.Logging, which the game's own Verse.Log writes through too. Read from the engine (decompiled 8 Nov 2026,
/// docs/development/logging-framework-design.md): such a line never passes the gates the game's 10,000-message limit closes, so it
/// still reaches Player.log after the limit; before it, it shows in the in-game log window like any other. It still counts toward
/// the limit.
/// Every touch of the engine's library is in this class, behind a method that is not inlined: if a later 1.7 build moves the type,
/// the first call fails with a catchable load error, the class says so once, and every later line goes through Verse.Log with the
/// category as a prefix.
/// </summary>
public static class HostLog
{
    static readonly Dictionary<string, object> categories = new();
    static bool unavailable;

    /// <summary>Why the engine's category could not be used, or null while it can.</summary>
    public static string? Unavailable { get; private set; }

    public static void Write(string category, HostLevel level, string text)
    {
        if (!unavailable)
        {
            try
            {
                WriteToCategory(category, level, text);
                return;
            }
            catch (Exception error) when (error is TypeLoadException or MissingMethodException or MissingFieldException or FileNotFoundException)
            {
                unavailable = true;
                Unavailable = error.GetType().Name + ": " + error.Message;
                Log.Warning("[" + category + "] The engine's log categories are not where they were (" + Unavailable
                    + "); lines go through the game's own log from now on.");
            }
        }
        string line = "[" + category + "] " + text;
        switch (level)
        {
            case HostLevel.Error: Log.Error(line); break;
            case HostLevel.Warning: Log.Warning(line); break;
            default: Log.Message(line); break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void WriteToCategory(string name, HostLevel level, string text)
    {
        LogCategory category;
        lock (categories)
        {
            if (categories.TryGetValue(name, out object? found)) category = (LogCategory)found;
            else categories[name] = category = LogCategory.Make(name);
        }
        switch (level)
        {
            case HostLevel.Error: category.Error(text); break;
            case HostLevel.Warning: category.Warning(text); break;
            default: category.Info(text); break;
        }
    }
}
