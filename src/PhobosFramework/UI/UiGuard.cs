using System;
using System.Collections.Generic;
using Phobos.Framework.Rules;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Interface;

/// <summary>
/// The screen guard (Framework 0.94.0; owner's go-ahead, 11 Nov 2026). The game calls a tab, a window or an inspect text on every frame,
/// and logs whatever it throws every time: about sixty errors a second, which uses up the game's 10,000 log messages in under three
/// minutes and silences the log for the rest of the session. A guarded screen that throws is logged once in full, rests for a few seconds
/// showing one line in its place, and then tries again. A screen is guarded like this, written inline so nothing is allocated a frame:
/// <code>
/// if (UiGuard.Resting(Key)) { UiGuard.Fallback(rect); return; }
/// try { DrawInner(rect); } catch (Exception e) { UiGuard.Report(Key, e); }
/// </code>
/// Resting also matters because a throw can leave a scroll view or a group open: not drawing again for a while keeps that to one frame.
/// </summary>
public static class UiGuard
{
    /// <summary>How long a screen rests after a fault before it is tried again, in seconds of real time (authored).</summary>
    public const double RestSeconds = 5;
    /// <summary>A screen that keeps failing is mentioned again every this many faults (authored: about every five minutes).</summary>
    public const int RemindEvery = 60;

    static readonly Dictionary<string, FaultLatch> latches = new();
    static readonly Phobos.Logging.PhobosLog Logger = Logging.Logs.For("D'thorga Framework", "Screens");

    /// <summary>Whether this screen has failed lately and should show its fallback line.</summary>
    public static bool Resting(string key) => latches.Count > 0 && latches.TryGetValue(key, out FaultLatch? latch) && latch.Resting(RealTime.LastRealTime);

    /// <summary>Notes that a screen threw: logs it (in full the first time, a reminder now and then), rests it, and puts the text settings
    /// a half-drawn screen may have left changed back as the game expects them.</summary>
    public static void Report(string key, Exception exception)
    {
        if (!latches.TryGetValue(key, out FaultLatch? latch)) latches[key] = latch = new FaultLatch();
        switch (latch.Note(RealTime.LastRealTime, RestSeconds, RemindEvery))
        {
            case FaultSay.Full:
                Logger.Error(key + " could not be drawn. It rests for " + RestSeconds + " seconds at a time and is tried again; further failures are counted, not logged.", exception);
                break;
            case FaultSay.Reminder:
                Logger.Warn(key + " has now failed " + latch.Count + " times. The first failure is logged in full above.");
                break;
        }
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.WordWrap = true;
        GUI.color = Color.white;
    }

    /// <summary>The line a resting screen shows in its place.</summary>
    public static void Fallback(Rect rect)
    {
        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft)) Widgets.Label(rect.ContractedBy(4f), FallbackText);
    }

    /// <summary>The same line, for an inspect text or a tooltip.</summary>
    public static string FallbackText => "PhobosFramework_ScreenFault".Translate().Resolve();

    /// <summary>How many screens have failed this session, and how often in all, for a dev action or the self-test.</summary>
    public static (int screens, int faults) Tally()
    {
        int faults = 0;
        foreach (FaultLatch latch in latches.Values) faults += latch.Count;
        return (latches.Count, faults);
    }
}
