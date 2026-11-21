using System;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>
/// Where a radiation dose goes (Framework 0.229.0; docs/development/radiation-model.md; owner, 22 Nov 2026). D'thorga Medical's radiation
/// model registers itself here; a source of ours (D'thorga Patches' nuclear strike) gives its dose in grays through <see cref="Give"/>,
/// and falls back to its own route (Dubs Rimatomics' radiation) when nothing is registered or the model is switched off. One receiver;
/// nothing here needs D'thorga Medical or Rimatomics.
/// </summary>
public static class RadiationDoses
{
    static Action<Pawn, float, string?>? receiver;
    static Func<bool>? isOn;

    /// <summary>Registers the model that takes doses, and how to ask whether it is on. The last registration wins.</summary>
    public static void Register(Action<Pawn, float, string?> takesDose, Func<bool> on)
    {
        receiver = takesDose;
        isOn = on;
    }

    /// <summary>Whether a model is registered and on, so a source should give its dose here rather than by its own route.</summary>
    public static bool Active => receiver != null && (isOn?.Invoke() ?? false);

    /// <summary>Gives a whole-body external dose in grays, with a note of where it came from; false when no model takes it.</summary>
    public static bool Give(Pawn pawn, float grays, string? source = null)
    {
        if (!Active || pawn == null || grays <= 0f) return false;
        receiver!(pawn, grays, source);
        return true;
    }
}
