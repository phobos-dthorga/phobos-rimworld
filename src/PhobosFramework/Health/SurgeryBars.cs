using System;
using System.Collections.Generic;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>
/// Things that stop the colony's surgeons operating on a patient for now (a patient sealed in a pressure chamber), registered by the mod
/// that owns each one, so a mod that queues operations can hold them and say why without referencing that mod. Each entry returns null
/// when it doesn't apply, or the reason, already translated and in the game's words: what holds it and what to do, never a mod's name.
/// Read through <see cref="Patients.SurgeryBarred"/>. Nothing is saved; mods register at start-up.
/// </summary>
public static class SurgeryBars
{
    static readonly List<Func<Pawn, string?>> bars = new();

    public static void Register(Func<Pawn, string?> bar)
    {
        if (!bars.Contains(bar)) bars.Add(bar);
    }

    /// <summary>The first reason any registered bar gives for this patient, or null if none applies.</summary>
    public static string? ReasonFor(Pawn patient)
    {
        for (int i = 0; i < bars.Count; i++)
        {
            string? reason = bars[i](patient);
            if (!string.IsNullOrEmpty(reason)) return reason;
        }
        return null;
    }
}
