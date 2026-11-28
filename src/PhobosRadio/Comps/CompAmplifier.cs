using System.Collections.Generic;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Phobos.Radio;

/// <summary>
/// A linear amplifier in a station's cable run (Radio 0.5.0; the owner's card of 24 Nov 2026: "Separate, on the coax", then "100 W and
/// 400 W"): it counts for an antenna only when the cable path from the station to that antenna passes through it
/// (<see cref="FeedlineService"/>). Its figures are data, so other modders can add amplifiers in XML.
/// </summary>
public class CompProperties_Amplifier : CompProperties
{
    /// <summary>How much it raises what reaches it, in dB. Authored.</summary>
    public float gainDb = 10f;
    /// <summary>The most it puts out, in watts: 100 W is the Australian Standard licence's limit, 400 W the Advanced licence's.</summary>
    public float ratedOutputWatts = 100f;
    /// <summary>What it costs a signal passing through it while it is switched out of the line (receiving, or unpowered), in dB. Authored.</summary>
    public float lossDb = 0.5f;

    public CompProperties_Amplifier() => compClass = typeof(CompAmplifier);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (var error in base.ConfigErrors(parentDef)) yield return error;
        if (ratedOutputWatts <= 0) yield return $"{parentDef.defName}: ratedOutputWatts must be positive";
        if (lossDb < 0) yield return $"{parentDef.defName}: lossDb cannot be negative";
    }
}

public class CompAmplifier : ThingComp
{
    private const int RareTicks = 250;
    private bool sending;

    /// <summary>Its cooling fan while it sends (Radio 0.16.0, S148); nothing saved, worked out again after a load.</summary>
    private readonly Phobos.Framework.Sounds.SustainedSound fan = new();

    /// <summary>Whether its state has been worked out since it spawned or was loaded: the first look sets it quietly, so a load never clacks
    /// every working amplifier's relay at once.</summary>
    private bool settled;

    /// <summary>Its station is in use and it is powered: it amplifies and draws (the needle, Radio 0.6.0).</summary>
    public bool Sending => sending;

    public CompProperties_Amplifier Props => (CompProperties_Amplifier)props;

    /// <summary>Switched on and fed: only then does it amplify. Unpowered, a signal passes through it, less its loss.</summary>
    // Radio 0.72.0: through a solar flare too, as the radio's own flare keeps its kit working.
    public bool Powered => Services.RadioFlare.Works(parent);

    /// <summary>The station on its coax, if one is.</summary>
    public CompRadio? StationNow => parent.TryGetComp<CompFeedlinePort>() is { } port ? FeedlineService.StationOn(port) : null;

    /// <summary>What it puts out for its station, in watts, or 0 with no station.</summary>
    public float OutputWatts(CompRadio? station) =>
        station == null ? 0f : (float)Amplifier.OutputWatts(station.Props.transmitPowerWatts, Props.gainDb, Props.ratedOutputWatts);

    /// <summary>
    /// While its station is in touch with someone, it draws what sending takes and warms its room, as a station does: its output over
    /// the mode's efficiency, times the mode's duty cycle. An authored simplification: every amplifier on a station's coax counts as
    /// sending, whichever antenna is in use.
    /// </summary>
    public override void CompTickRare()
    {
        if (!parent.Spawned) return;
        var power = parent.TryGetComp<CompPowerTrader>();
        var station = StationNow;
        var inUse = Powered && station != null && station.parent.Spawned && RadioNet.For(parent.Map).InTouchCount(station.parent) > 0;
        if (power != null)
        {
            var standby = ((CompProperties_Power)power.props).PowerConsumption;
            var sendingDraw = inUse ? OutputWatts(station) / station!.Mode.transmitEfficiency * station.Mode.dutyCycle : 0f;
            if (inUse != sending || inUse) power.PowerOutput = -(standby + sendingDraw);
        }
        // The relay clacks in as it starts sending and out as it stops (S147); the fan runs while it sends (S148). Only on a change.
        if (settled && inUse != sending) RadioSoundDefOf.PhobosRadio_AmplifierRelay.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
        settled = true;
        sending = inUse;
        fan.Set(RadioSoundDefOf.PhobosRadio_AmplifierFan, sending, parent);
        if (inUse)
        {
            var output = OutputWatts(station);
            var waste = (output / station!.Mode.transmitEfficiency - output) * station.Mode.dutyCycle;
            GenTemperature.PushHeat(parent, waste * RadioRulesDef.Current.heatPerWattSecond * RareTicks / 60f);
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        fan.End();
        settled = false;
        base.PostDeSpawn(map, mode);
    }

    /// <summary>Switched on or off, or an upgrade changed: the net and the coverage picture work it in at once.</summary>
    public override void ReceiveCompSignal(string signal)
    {
        if (!parent.Spawned) return;
        if (signal == CompPowerTrader.PowerTurnedOnSignal || signal == CompPowerTrader.PowerTurnedOffSignal)
        {
            RadioNet.For(parent.Map).MarkDirty();
            RadioCoverage.For(parent.Map).NotifyChanged();
        }
    }

    public override string CompInspectStringExtra()
    {
        if (!parent.Spawned) return string.Empty;
        var station = StationNow;
        if (station == null) return "PhobosRadio_AmplifierNoStation".Translate(Props.ratedOutputWatts.ToString("0").Named("RATED"));
        if (!Powered) return "PhobosRadio_AmplifierUnpowered".Translate();
        return "PhobosRadio_AmplifierOutput".Translate(OutputWatts(station).ToString("0").Named("OUTPUT"),
            station.parent.LabelShort.Named("STATION"), Props.ratedOutputWatts.ToString("0").Named("RATED"));
    }
}
