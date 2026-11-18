using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Gives Combat Extended a vehicle's real width to shoot at (Framework 0.154.0; docs/development/vehicle-armour.md, "A wider target").
/// Combat Extended tests a shot against one box at a vehicle's centre, one cell wide whatever the vehicle's size, so most shots at a
/// three-cell truck that should strike it pass by. Combat Extended asks a public list of callbacks for a pawn's box before working one out,
/// and its own vehicle compatibility answers every vehicle with a width of one. Ours goes first in that list and answers only a Def that
/// states a width (<see cref="PhobosArmourExtension.hitWidthCells"/>), with Combat Extended's own height (the Def's fill); every other pawn
/// falls through to Combat Extended. No Harmony: the list and its register are Combat Extended's published compatibility API, reached by
/// name so no assembly of theirs is referenced. One dictionary look-up a call, with no allocation after a Def's first.
/// </summary>
[StaticConstructorOnStartup]
public static class CombatExtendedHitWidth
{
    const string CallbacksField = "CombatExtended.Compatibility.Patches:CollisionBodyFactorCallbacks";
    const string RegisterMethod = "CombatExtended.Compatibility.Patches:RegisterCollisionBodyFactorCallback";

    static readonly Tuple<bool, Vector2> NoAnswer = new(false, default);
    static readonly Dictionary<ThingDef, Tuple<bool, Vector2>> answers = new();
    static readonly Func<Pawn, Tuple<bool, Vector2>> Callback = Answer;
    static readonly HashSet<ThingDef> noted = new();

    /// <summary>Whether the callback is in Combat Extended's list, for the load line and a dev check.</summary>
    public static bool Registered { get; private set; }

    static CombatExtendedHitWidth()
    {
        if (!OptionalMods.IsActive(OptionalMods.CombatExtended)) return;
        try
        {
            MethodInfo? register = AccessTools.Method(RegisterMethod);
            FieldInfo? field = AccessTools.Field(CallbacksField);
            if (register == null || field?.GetValue(null) is not List<Func<Pawn, Tuple<bool, Vector2>>> callbacks)
            {
                VehicleSystemsMod.Logger.Warn("Combat Extended's list of hit boxes was not found, so it keeps shooting at one cell of our vehicles. Nothing else is affected.");
                return;
            }
            // Registering sets Combat Extended's own flag that the list is in use; then ours moves to the front, ahead of its own answer for every vehicle.
            register.Invoke(null, new object[] { Callback });
            callbacks.Remove(Callback);
            callbacks.Insert(0, Callback);
            Registered = true;
            VehicleSystemsMod.Logger.Debug("Combat Extended shoots at our armoured vehicles' full width.");
        }
        catch (Exception problem)
        {
            VehicleSystemsMod.Logger.Warn("Combat Extended's list of hit boxes could not be joined (" + problem.Message + "), so it keeps shooting at one cell of our vehicles.");
        }
    }

    static Tuple<bool, Vector2> Answer(Pawn pawn)
    {
        ThingDef? def = pawn?.def;
        if (def == null) return NoAnswer;
        if (!answers.TryGetValue(def, out Tuple<bool, Vector2>? answer))
        {
            float width = def.GetModExtension<PhobosArmourExtension>()?.hitWidthCells ?? 0f;
            answer = width > 0f ? new Tuple<bool, Vector2>(true, new Vector2(width, def.fillPercent)) : NoAnswer;
            answers[def] = answer;
        }
        // The vehicle's Def is the case, so each vehicle's test row reads its own note.
        if (answer.Item1 && noted.Add(def) && Seen.Wants("vehicles.armour-hit-width", pawn, def.defName))
        {
            Seen.Note("vehicles.armour-hit-width", pawn, variant: def.defName,
                values: new List<KeyValuePair<string, double>> { new("width", answer.Item2.x) });
        }
        return answer;
    }
}
