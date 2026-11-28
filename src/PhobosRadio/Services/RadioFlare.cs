using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The radio's own solar flare (Radio 0.72.0; docs/development/radio-hf.md, the owner's card 4 of 28 Nov 2026: "the radio's own flare: HF
/// out on the sunlit side, stations otherwise working", in place of the game's power cut for stations).
/// <para>The game's flare cuts every powered thing; the radio's kit keeps working through it (<see cref="Works"/>), as a real flare does
/// not cut mains power. HF's skywave is blacked out where its path's midpoint is in daylight, the Dellinger effect (<see cref="BlackedOut"/>);
/// the ground wave, VHF and the hand-helds carry on. A letter warns when one begins.</para>
/// <para>Cost: one look at the game's conditions when a station's power or an HF path is weighed; the letter's check once a game hour.</para>
/// </summary>
public static class RadioFlare
{
    private static bool? wasOn;

    public static void Clear() => wasOn = null;

    /// <summary>Whether a solar flare is on, on the world or any map.</summary>
    static GameConditionDef? flareDef;
    static bool looked;

    public static bool On()
    {
        if (!looked)
        {
            looked = true;
            flareDef = DefDatabase<GameConditionDef>.GetNamedSilentFail("SolarFlare");
        }
        if (flareDef is not { } flare) return false;
        if (Find.World?.gameConditionManager?.ConditionIsActive(flare) == true) return true;
        return Find.Maps != null && Find.Maps.Any(map => map.gameConditionManager.ConditionIsActive(flare));
    }

    /// <summary>
    /// Whether a radio, station or amplifier works now: it has power, or it is off only because the game's flare cut the electricity (not
    /// switched off, not broken down, and on a power net).
    /// </summary>
    public static bool Works(Thing thing)
    {
        if (thing.TryGetComp<CompPowerTrader>() is not { } power || power.PowerOn) return true;
        if (!thing.Spawned || power.PowerNet == null || !thing.Map.gameConditionManager.ElectricityDisabled(thing.Map)) return false;
        if (thing.TryGetComp<CompFlickable>() is { SwitchIsOn: false }) return false;
        if (thing.TryGetComp<CompBreakdownable>() is { BrokenDown: true }) return false;
        if (Seen.Wants("radio.flare-station-works", thing)) Seen.Note("radio.flare-station-works", thing, thing.LabelShort);
        return true;
    }

    /// <summary>Whether a sky path with this midpoint (a direction from the planet's centre) is blacked out: a flare is on and it is daylight there.</summary>
    public static bool BlackedOut(Vector3 midpoint)
    {
        if (!On()) return false;
        bool sunlit = Vector3.Dot(midpoint.normalized, GenCelestial.CurSunPositionInWorldSpace().normalized) > 0f;
        if (sunlit && Seen.Wants("radio.flare-blackout", null)) Seen.Note("radio.flare-blackout");
        return sunlit;
    }

    /// <summary>Once a game hour: a letter when a flare begins, while the colony has an HF station.</summary>
    public static void Hourly()
    {
        bool on = On();
        bool begins = on && wasOn == false;
        wasOn = on;
        if (!begins) return;
        bool hf = Find.Maps.Any(map => map.IsPlayerHome && RadioNet.For(map).Stations.Any(s => s.Bands.Any(b => b.path == BandPath.Hf)));
        if (!hf) return;
        Find.LetterStack.ReceiveLetter("PhobosRadioFlare_Label".Translate(), "PhobosRadioFlare_Text".Translate(), LetterDefOf.NegativeEvent);
        if (Seen.Wants("radio.flare-letter", null)) Seen.Note("radio.flare-letter");
    }
}
