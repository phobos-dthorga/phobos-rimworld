using System.Collections.Generic;
using DMS;
using Fortified;
using HarmonyLib;
using Phobos.Radio.Services;
using Phobos.Shared;
using RimWorld;
using Verse;

namespace Phobos.Radio.DeadMansSwitch;

/// <summary>
/// The tie-in's start-up (Radio 0.46.0): applies its one patch. Loaded only while The Dead Man's Switch is active (its load folder).
/// </summary>
[StaticConstructorOnStartup]
public static class DeadMansSwitchTieIn
{
    /// <summary>The Harmony ID is the radio's packageId; this is its only patch so far.</summary>
    public const string HarmonyId = "PhobosDthorga.RadioFramework";
    public const string LogPrefix = "[D'thorga Radio Framework]";

    static DeadMansSwitchTieIn() => HarmonyPatchApplier.ApplyEach(new Harmony(HarmonyId), typeof(DeadMansSwitchTieIn).Assembly, LogPrefix);
}

/// <summary>
/// Jam the call (Radio 0.46.0; docs/development/electronic-warfare.md, EW18; the owner's word of 27 Nov 2026, "Start slice 2"). The Dead
/// Man's Switch's reinforcement uplink and fire support uplink send a call off the map when their base's alarm trips. While radio jamming
/// drowns the reply at the uplink (<see cref="JammedCalls"/>), the call does not go out: a postfix on Fortified Features Framework's
/// CompAlertEffector.CanFire, the one check every alarm response passes before it acts, narrowed to those two. The building's own
/// one-shot is not spent, so the call goes out at the next alarm once the jamming stops, as its author's text says cutting its power does.
/// <para>Cost: one lookup in the radio's noise snapshot each time one of the two answers an alarm; nothing else, and nothing per tick.
/// Nobody else patches CanFire (query-patches.py who "CompAlertEffector.*", 27 Nov 2026).</para>
/// </summary>
[HarmonyPatch(typeof(CompAlertEffector), "CanFire")]
public static class UplinkJammedPatch
{
    /// <summary>How long, in ticks, before the player is told again of the same uplink being jammed (about an in-game hour).</summary>
    const int TellAgainTicks = 2500;

    static readonly Dictionary<int, int> toldAt = new();

    static void Postfix(CompAlertEffector __instance, ref bool __result)
    {
        if (!__result || __instance is not (CompAlertEffector_CallBattleGroup or CompAlertEffector_MortarStrike)) return;
        var building = __instance.parent;
        if (building is not { Spawned: true } || !JammedCalls.Jammed(building.Map, building.Position, out _)) return;
        __result = false;
        Tell(building, __instance is CompAlertEffector_MortarStrike ? "fire support" : "reinforcements");
    }

    /// <summary>Tells the player, once an hour per uplink and only where they could see it, that its call was jammed.</summary>
    static void Tell(ThingWithComps building, string variant)
    {
        if (Seen.Wants("radio.uplink-jammed", building, variant)) Seen.Note("radio.uplink-jammed", building, building.def.label, variant);
        if (building.Position.Fogged(building.Map)) return;
        var now = Find.TickManager.TicksGame;
        if (toldAt.TryGetValue(building.thingIDNumber, out var last) && now - last < TellAgainTicks && now >= last) return;
        toldAt[building.thingIDNumber] = now;
        var ours = building.Faction == Faction.OfPlayer;
        Messages.Message(
            (ours ? "PhobosRadio_UplinkJammedOurs" : "PhobosRadio_UplinkJammed").Translate(building.LabelShort.Named("UPLINK")).Resolve(),
            building, ours ? MessageTypeDefOf.NegativeEvent : MessageTypeDefOf.PositiveEvent, historical: false);
    }
}
