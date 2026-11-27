using System.Collections.Generic;
using System.Text;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using Phobos.Radio.UI;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Phobos.Radio;

/// <summary>A radio: a hand-held (worn apparel) or a base station (a powered building).</summary>
public class CompProperties_Radio : CompProperties
{
    public List<BandDef> bands = new();
    public ModeDef? mode;
    public float transmitPowerWatts = 5f;
    public float noiseFigureDb = 8f;
    /// <summary>The built-in antenna: a hand-held's rubber duck, or a station's whip when no coax is connected.</summary>
    public float antennaGainDbi;
    public float antennaHeightMetres = 1.5f;
    public bool isStation;
    /// <summary>
    /// A software radio's station, whose Waterfall tab always shows (the radio terminal); any other station shows it only while the
    /// setting "Show the waterfall on the radio desk" is on (Radio 0.6.0; the owner, 24 Nov 2026: "as a setting, disabled by default").
    /// </summary>
    public bool waterfallBuiltIn;
    /// <summary>
    /// How many channels a station has, each as wide as the rules' channel width (25 kHz), shared by radios taking turns (Radio
    /// 0.7.0; the channel ledger). A hand-held needs none of its own.
    /// </summary>
    public int channels = 1;
    /// <summary>
    /// What this radio's use of a station's channels counts as in the ledger (Radio 0.8.0): the local voice net when it names
    /// none. A station serves every kind; the kind's mode sets the channels one use takes and the radios that share it.
    /// </summary>
    public ChannelKindDef? channelKind;
    /// <summary>
    /// Whether this radio may pass a call on between world tiles by itself (Radio 0.22.0). Read only through <c>RelayNodes.CanRelay</c>.
    /// False by default (the owner's card of 26 Nov 2026: a relay between tiles needs a repeater, a duplexer): a vehicle relays with the
    /// repeater kit fitted, which its source declares; a relay mast's radio is one.
    /// </summary>
    public bool relays;
    /// <summary>
    /// The radio's own gentle squelch as it engages (Radio 0.30.0; the owner, 27 Nov 2026: "a gentle squelch sound for each the vehicles,
    /// pawns, and base-stations whenever they engage the radios"): a hand-held or a vehicle's radio as it comes into touch with a station
    /// (RadioNetSounds), a station as it comes into use (its rare tick). Each kind of radio names its own, as each is its own speaker;
    /// none plays nothing.
    /// </summary>
    public SoundDef? engageSound;

    public CompProperties_Radio() => compClass = typeof(CompRadio);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (var error in base.ConfigErrors(parentDef)) yield return error;
        if (bands.Count == 0) yield return $"{parentDef.defName}: CompProperties_Radio needs at least one band";
        if (mode == null) yield return $"{parentDef.defName}: CompProperties_Radio needs a mode";
        if (transmitPowerWatts <= 0) yield return $"{parentDef.defName}: transmitPowerWatts must be positive";
    }
}

public class CompRadio : ThingComp
{
    private const int RareTicks = 250;
    private bool transmitting;

    /// <summary>The set's squelch and static while the station is in use (Radio 0.16.0, S102); nothing saved, worked out again after a load.</summary>
    private readonly Phobos.Framework.Sounds.SustainedSound squelch = new();

    /// <summary>A station in use: in touch with someone, so it sends for the mode's duty cycle (the lamp and needle, Radio 0.6.0).</summary>
    public bool Transmitting => transmitting;

    public CompProperties_Radio Props => (CompProperties_Radio)props;
    public ModeDef Mode => Props.mode!;

    private readonly List<BandDef> bands = new();

    /// <summary>
    /// The bands this set works through an antenna on its coax: its own, and any its fitted upgrades add (Radio 0.49.0: the aviation
    /// transceiver). Its built-in antenna works its own Def's bands only. Read when the net works its links out.
    /// </summary>
    public List<BandDef> Bands
    {
        get
        {
            RadioUpgrades.BandsOf(parent, Props.bands, bands);
            if (bands.Count > Props.bands.Count && Seen.Wants("radio.aviation-bands", parent, "station"))
                Seen.Note("radio.aviation-bands", parent, bands.Count + " bands", "station");
            return bands;
        }
    }

    /// <summary>The kind this radio's use counts as in the channel ledger (Radio 0.8.0).</summary>
    public ChannelKindDef Kind => Props.channelKind ?? ChannelKindDef.LocalVoice;

    /// <summary>The pawn wearing this hand-held, or null for a station or an unworn radio.</summary>
    public Pawn? Wearer => (parent as Apparel)?.Wearer;

    /// <summary>
    /// Who the radio goes with: the wearer of a hand-held, or the pawn a radio is built into (a vehicle; Radio 0.10.0); null for a
    /// station or a radio lying about.
    /// </summary>
    public Pawn? Holder => Props.isStation ? null : Wearer ?? parent as Pawn;

    /// <summary>The pawn a radio thing goes with, as the net's links name it: a hand-held's wearer, or the vehicle a radio is built into.</summary>
    public static Pawn? HolderOf(Thing radio) => (radio as Apparel)?.Wearer ?? radio as Pawn;

    /// <summary>The extra draw while the station is in use: transmitter input power times the duty cycle.</summary>
    public float TransmitDrawWatts => Props.transmitPowerWatts / Mode.transmitEfficiency * Mode.dutyCycle;

    public float WasteHeatWatts => (Props.transmitPowerWatts / Mode.transmitEfficiency - Props.transmitPowerWatts) * Mode.dutyCycle;

    public override void CompTickRare()
    {
        if (!Props.isStation || !parent.Spawned) return;
        var net = RadioNet.For(parent.Map);
        var powerComp = parent.TryGetComp<CompPowerTrader>();
        var inUse = powerComp != null && powerComp.PowerOn && net.InTouchCount(parent) > 0;
        if (inUse != transmitting)
        {
            transmitting = inUse;
            // Its squelch once as it comes into use (Radio 0.30.0), never on going quiet; S102's loop then carries on while it is in use.
            if (inUse && Props.engageSound != null)
            {
                Props.engageSound.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
                Seen.Note("radio.engage-squelch", parent, null, "station");
            }
            if (powerComp != null)
            {
                var basePower = ((CompProperties_Power)powerComp.props).PowerConsumption;
                powerComp.PowerOutput = -(basePower + (inUse ? TransmitDrawWatts : 0f));
            }
        }
        // Started or ended only when the state changes; Set returns at once while it already matches.
        squelch.Set(RadioSoundDefOf.PhobosRadio_StationSquelch, transmitting, parent);
        if (transmitting)
        {
            GenTemperature.PushHeat(parent, WasteHeatWatts * RadioRulesDef.Current.heatPerWattSecond * RareTicks / 60f);
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        squelch.End();
        base.PostDeSpawn(map, mode);
    }

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
    {
        var wearer = Wearer;
        if (wearer == null || !RadioNet.Counts(wearer)) yield break;    // the same test the net uses, or the gizmo would say there is no station
        var net = RadioNet.For(wearer.Map);
        net.TryGetBest(wearer, out var link);
        yield return SignalGizmo.ForHandheld(this, link);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!parent.Spawned || parent.Faction != Faction.OfPlayer) yield break;
        var net = RadioNet.For(parent.Map);
        if (Props.isStation)
        {
            yield return SignalGizmo.ForStation(this, net);
            yield break;
        }
        // A radio built into a vehicle (Radio 0.10.0): its bars, while the net counts it (a tie-in may say it is not fitted).
        if (parent is Pawn carrier && net.Carries(carrier))
        {
            net.TryGetBest(carrier, out var link);
            yield return SignalGizmo.ForHandheld(this, link);
        }
    }

    /// <summary>An upgrade fitted to a station or taken off: the net and the picture work it in at once (Radio 0.4.0).</summary>
    public override void ReceiveCompSignal(string signal)
    {
        if (Props.isStation) RadioUpgrades.Notice(parent, signal);
    }

    /// <summary>While a station is selected, the map shows its coverage (Radio 0.2.0); the picture's own service does the work.</summary>
    public override void PostDrawExtraSelectionOverlays()
    {
        if (!Props.isStation || !parent.Spawned || parent.Faction != Faction.OfPlayer) return;
        RadioCoverage.For(parent.Map).Show(parent);
        UI.RadioCoaxView.Draw(parent);
    }

    public override string CompInspectStringExtra()
    {
        if (!Props.isStation || !parent.Spawned) return string.Empty;
        var net = RadioNet.For(parent.Map);
        var sb = new StringBuilder();
        var powerComp = parent.TryGetComp<CompPowerTrader>();
        if (powerComp != null && !powerComp.PowerOn)
        {
            sb.Append("PhobosRadio_StationWaitingPower".Translate());
        }
        else
        {
            var pairs = net.PairsFor(parent);
            sb.Append(pairs.Count == 0
                ? "PhobosRadio_StationNoRadios".Translate()
                : "PhobosRadio_StationInTouch".Translate(net.InTouchCount(parent).Named("INTOUCH"), pairs.Count.Named("TOTAL")));
        }
        sb.AppendLine();
        sb.Append("PhobosRadio_StationAntenna".Translate(net.AntennaLabelFor(parent).ToString().Named("ANTENNA")));
        sb.AppendLine();
        sb.Append(LinkText.ChannelsLine(net.ChannelsOf(parent)));
        // Vehicles it serves, and who works it to drive them (Radio 0.12.0).
        var vehicles = net.VehiclesOn(parent);
        if (vehicles > 0)
        {
            sb.AppendLine();
            sb.Append(net.OperatorAt(parent) is { } operatorPawn
                ? "PhobosRadio_StationOperator".Translate(vehicles.Named("COUNT"), operatorPawn.LabelShort.Named("OPERATOR"),
                    RadioRulesDef.Current.vehiclesPerOperator.Named("MAX"))
                : "PhobosRadio_StationNoOperator".Translate(vehicles.Named("COUNT")));
        }
        var coverage = RadioCoverage.For(parent.Map).InspectLine(parent);
        if (coverage != null)
        {
            sb.AppendLine();
            sb.Append(coverage);
        }
        return sb.ToString();
    }

    public override string TransformLabel(string label) => label;
}
