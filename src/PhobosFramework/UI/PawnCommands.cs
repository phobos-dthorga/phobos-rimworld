using System;
using System.Collections.Generic;
using Verse;

namespace Phobos.Framework.Interface;

/// <summary>
/// Buttons another mod adds to a humanlike pawn's command bar (Framework 0.381.0), drawn by the comp the framework already puts on
/// every humanlike race (<see cref="Health.CompPhobosPatient"/>), so a mod needs neither a comp of its own on every person nor a
/// Harmony patch on the pawn's buttons. A provider is asked only while the pawn is selected; it answers nothing for a pawn it has no
/// button for, and should decide that cheaply. The first user is the radio's hand-held carried in an inventory rather than worn.
/// </summary>
public static class PawnCommands
{
    static readonly List<Func<Pawn, IEnumerable<Gizmo>>> providers = new();

    public static IReadOnlyList<Func<Pawn, IEnumerable<Gizmo>>> Providers => providers;

    /// <summary>Registers a provider once, from a static constructor on start-up.</summary>
    public static void Register(Func<Pawn, IEnumerable<Gizmo>> provider)
    {
        if (!providers.Contains(provider)) providers.Add(provider);
    }
}
