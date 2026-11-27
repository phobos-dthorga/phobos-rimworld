using System;
using Phobos.Radio.Physics;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// What a tie-in last worked out about a colonist's way home with no station of theirs on the map (Radio 0.34.0; the cross-band hop through
/// a vehicle's dual-band radio): the weakest hop's margin home, or null. Since Radio 0.44.0 also a caravan's on the world map (the owner's
/// card of 27 Nov 2026: the in-touch mood on the road, for any caravan of the player's). Kept answers only: the "in touch" mood reads them
/// and never computes, as the radio's rules ask; the tie-in keeps them fresh a few times a game hour where they matter.
/// </summary>
public static class AwayReach
{
    static Func<Pawn, double?>? kept;
    static Func<Caravan, LinkOutcome?>? keptCaravan;

    public static void Register(Func<Pawn, double?> keptMargin) => kept = keptMargin;

    /// <summary>A caravan's kept reach home, as an outcome (Radio 0.44.0).</summary>
    public static void RegisterCaravans(Func<Caravan, LinkOutcome?> keptOutcome) => keptCaravan = keptOutcome;

    /// <summary>The kept margin home for this colonist, or null when none is known or there is none.</summary>
    public static double? KeptMarginDb(Pawn pawn) => kept?.Invoke(pawn);

    /// <summary>The kept outcome of this caravan's reach home, or null when none is known or it has no radio.</summary>
    public static LinkOutcome? KeptCaravanOutcome(Caravan caravan) => keptCaravan?.Invoke(caravan);
}
