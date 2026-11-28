using System;
using System.Collections.Generic;
using Phobos.Framework.Logging;
using Verse;

namespace Phobos.Framework.Interface;

/// <summary>
/// Buttons another mod adds to a pawn's command bar (Framework 0.381.0), drawn by the comp the framework already puts on every pawn
/// race (<see cref="Health.CompPhobosPatient"/>; every race from 0.381.1, humanlike ones before), so a mod needs neither a comp of its
/// own on every pawn nor a Harmony patch on the pawn's buttons. A provider is asked only while the pawn is selected; it answers nothing
/// for a pawn it has no button for, and should decide that cheaply, since animals, mechanoids and vehicles ask too. The first user is
/// the radio's hand-held carried in an inventory rather than worn.
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

    /// <summary>
    /// Every provider's buttons for the pawn, or null for none (Framework 0.381.1). A provider that fails is reported once
    /// (<see cref="ProviderFaults"/>) and skipped for this pawn, keeping any buttons it gave before failing; the others still draw, so
    /// one faulty mod never empties a person's command bar.
    /// </summary>
    public static List<Gizmo>? GizmosFor(Pawn pawn)
    {
        List<Gizmo>? gizmos = null;
        for (int i = 0; i < providers.Count; i++)
        {
            Func<Pawn, IEnumerable<Gizmo>> provider = providers[i];
            try
            {
                foreach (Gizmo gizmo in provider(pawn)) (gizmos ??= new List<Gizmo>()).Add(gizmo);
            }
            catch (Exception exception)
            {
                ProviderFaults.Report(provider, "a button", pawn, exception);
            }
        }
        return gizmos;
    }
}
