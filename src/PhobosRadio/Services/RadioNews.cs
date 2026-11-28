using HarmonyLib;
using Phobos.Framework.Incidents;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// News from neighbours, a raid warning (Radio 0.53.0; docs/development/radio-comms-console.md, B4; the owner's card of 27 Nov 2026, "Raid
/// warning"): when the storyteller sends raiders against a colony with an ally whose settlement its stations reach, the ally calls with a
/// warning, and the raid arrives the rules' few hours later, through the game's own incident queue. A raid a quest or the dev tools start,
/// or one already queued, is never held. Behind the setting "Allies warn of raids by radio", on by default (a benefit, no risk).
/// <para>Simplification [A]: any ally in reach warns, wherever the raiders come from; their path is not worked out.</para>
/// <para>Cost: one look at the incident each time the storyteller fires one; for a raid on a colony, one reach search per ally faction,
/// kept a game hour.</para>
/// </summary>
public static class RadioNews
{
    /// <summary>Holds a fresh storyteller raid back and warns of it, when an ally in reach can. True when it was held (the caller skips it).</summary>
    public static bool TryHold(FiringIncident fi)
    {
        if (!RadioMod.Settings.alliesWarnOfRaids || fi?.def == null || fi.parms == null) return false;
        // One hold per raid (the owner's card of 28 Nov 2026): a raid another hold is letting go (Rimatomics fires its own as fresh) is
        // never held again.
        if (RaidHolds.Releasing) return false;
        if (fi.def.Worker is not IncidentWorker_RaidEnemy || fi.parms.forced || fi.parms.quest != null) return false;
        if (fi.parms.target is not Map map || !map.IsPlayerHome) return false;
        if (!fi.def.Worker.CanFireNow(fi.parms)) return false;
        if (WarningAlly(map) is not { } ally) return false;
        // Radio 0.73.0 (RN4b; docs/development/radio-life-on-the-air.md): an ally's warning is heard only if someone listens; one nobody hears is missed (logged), and the raid comes as the game sends it.
        if (RadioWatch.Hear(map, ally.Faction.Name, "PhobosRadioWatch_WhatRaid".Translate().Resolve(), "raid warning") == null) return false;
        int delay = Mathf.RoundToInt(RadioRulesDef.Current.radioRaidWarningHours * GenDate.TicksPerHour);
        // Queued, it fires through the storyteller again, then as any raid does; RetryDurationTicks as the game queues its own.
        int fireTick = Find.TickManager.TicksGame + delay;
        Find.Storyteller.incidentQueue.Add(fi.def, fireTick, fi.parms, GenDate.TicksPerDay);
        // When it fires, D'thorga Framework lets it through Rimatomics' and Fortified's holds (Framework 0.383.0).
        RaidHolds.MarkHeld(fi, fireTick);
        string text = "PhobosRadioNews_RaidWarning".Translate(ally.Faction.Name.Named("FACTION"), ally.Settlement.LabelCap.Named("SETTLEMENT"),
            map.Parent.LabelCap.Named("COLONY"), delay.ToStringTicksToPeriod().Named("TIME")).Resolve();
        Find.LetterStack.ReceiveLetter("PhobosRadioNews_RaidWarningLabel".Translate(), text, LetterDefOf.ThreatSmall, new LookTargets(map.Parent));
        RadioLog.Add(RadioLogKind.Diplomacy, text);
        if (Seen.Wants("radio.raid-warning", null)) Seen.Note("radio.raid-warning", null, ally.Faction.Name + ", " + delay.ToStringTicksToPeriod());
        return true;
    }

    /// <summary>An ally whose settlement this colony's stations reach, or null.</summary>
    static FactionDoctorLink? WarningAlly(Map map)
    {
        foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
        {
            if (faction.IsPlayer || faction.PlayerRelationKind != FactionRelationKind.Ally) continue;
            if (RadioDiplomacy.Reach(map, faction) is { } link) return link;
        }
        return null;
    }
}

/// <summary>
/// The raid warning's hook (Radio 0.53.0): a prefix on <see cref="Storyteller.TryFire"/>, where the storyteller fires an incident. It is the
/// radio's first Harmony patch. It skips the original when it holds a raid back, which queues the same raid a few hours later instead, so
/// every other mod's patches on the raid still run when it fires. Reason for skipping, recorded in docs/development/radio-comms-console.md:
/// the game has no other point at which a raid can be held without firing it. Only Vanilla Expanded Framework also patches this method (a
/// prefix that may skip it); when it has skipped it, this does nothing. A queued firing, and every incident that is not a fresh
/// storyteller raid on a colony, pass straight through.
/// </summary>
[HarmonyPatch(typeof(Storyteller), nameof(Storyteller.TryFire))]
internal static class Patch_Storyteller_TryFire_RaidWarning
{
    static bool Prefix(FiringIncident fi, bool queued, ref bool __result, bool __runOriginal)
    {
        if (!__runOriginal || queued) return true;
        // A radar's warning first (Radio 0.74.0, RD7), then an ally's: one hold per raid, whichever gives it.
        if (!RadarWarning.TryHold(fi) && !RadioNews.TryHold(fi)) return true;
        __result = true;
        return false;
    }
}
