using System;
using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Persistence;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A pipe or conduit one of our underlays laid gives nothing back when the player takes it apart (Framework 0.108.1). Since 0.108.0 a vehicle lays its pieces
/// again at every stop with its engine off, so a piece that refunded materials when deconstructed would make them from nothing, stop after stop: Dubs Bad
/// Hygiene's pipe gives back half its steel (the game's rule, GenLeaving.GetBuildingResourcesLeaveCalculator: a building's cost times its
/// resourcesFractionWhenDeconstructed, 0.5 unless a Def says otherwise). The game's conduits already give back nothing (resourcesFractionWhenDeconstructed 0).
/// <para>A prefix that only turns the destroy mode for these pieces from Deconstruct to Vanish, which leaves nothing; it never skips the original, and every
/// other thing, the player's own pipes included, is untouched. It runs first, so other mods' patches on the same method see the mode it leaves.</para>
/// </summary>
[HarmonyPatch(typeof(GenLeaving), nameof(GenLeaving.DoLeavingsFor),
    new[] { typeof(Thing), typeof(Map), typeof(DestroyMode), typeof(CellRect), typeof(Predicate<IntVec3>), typeof(List<Thing>) })]
public static class UnderlayPiecesRefundNothing
{
    [HarmonyPriority(Priority.First)]
    public static void Prefix(Thing diedThing, ref DestroyMode mode)
    {
        if (mode == DestroyMode.Deconstruct && PipeUnderlay.LaidByUs(diedThing)) mode = DestroyMode.Vanish;
    }
}
