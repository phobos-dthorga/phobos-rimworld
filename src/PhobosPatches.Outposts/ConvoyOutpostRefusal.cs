using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.OutpostPatches;

/// <summary>
/// Refuses the outposts dialog's founding button for a caravan that holds a vehicle, unless the player has asked for the original
/// behaviour (docs/development/vehicle-garage-design.md). What it stops, read from the code and not reproduced in game: founding adds the
/// outpost to the world and then offers it every pawn in the caravan; for a vehicle that reflects on a method Vehicle Framework's vehicles
/// do not have, and throws, which leaves the outpost empty to be abandoned; and a caravan with no human left in its own list, which crew
/// aboard a vehicle are not in, is destroyed.
///
/// A postfix on the dialog's constructor, the one place that knows which caravan is founding: the checks underneath are given only a
/// tile and pawns, so patching them could refuse a caravan on foot for a convoy sharing its tile. It only adds a reason where the dialog
/// found none, so the existing reasons stay as they were. The owner chose to patch this (8 Nov 2026) as possible loss of colony data.
/// </summary>
[HarmonyPatch(typeof(Dialog_CreateCamp), MethodType.Constructor, typeof(Caravan))]
public static class ConvoyOutpostRefusal
{
    public static void Postfix(Caravan creator, Dictionary<WorldObjectDef, Pair<string, string>> ___validity)
    {
        if (!OutpostPatchesMod.Settings.refuseConvoyFounding || !creator.PawnsListForReading.Any(p => p is VehiclePawn)) return;
        string reason = "PhobosOutpostPatches_RefusedWithVehicles".Translate();
        foreach (WorldObjectDef outpost in ___validity.Keys.ToList())
        {
            Pair<string, string> entry = ___validity[outpost];
            if (entry.First.NullOrEmpty()) ___validity[outpost] = new Pair<string, string>(reason, entry.Second);
        }
    }
}
