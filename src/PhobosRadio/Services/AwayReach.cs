using System;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// What a tie-in last worked out about a colonist's way home with no station of theirs on the map (Radio 0.34.0; the cross-band hop through
/// a vehicle's dual-band radio): the weakest hop's margin home, or null. A kept answer only: the "in touch" mood reads it and never computes,
/// as the radio's rules ask; the tie-in keeps it fresh a few times a game hour on the maps where it matters.
/// </summary>
public static class AwayReach
{
    static Func<Pawn, double?>? kept;

    public static void Register(Func<Pawn, double?> keptMargin) => kept = keptMargin;

    /// <summary>The kept margin home for this colonist, or null when none is known or there is none.</summary>
    public static double? KeptMarginDb(Pawn pawn) => kept?.Invoke(pawn);
}
