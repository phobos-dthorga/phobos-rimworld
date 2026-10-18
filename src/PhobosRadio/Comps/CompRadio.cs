using System.Collections.Generic;
using System.Text;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using Phobos.Radio.UI;
using RimWorld;
using Verse;

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

    public CompProperties_Radio Props => (CompProperties_Radio)props;
    public ModeDef Mode => Props.mode!;

    /// <summary>The pawn wearing this hand-held, or null for a station or an unworn radio.</summary>
    public Pawn? Wearer => (parent as Apparel)?.Wearer;

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
            if (powerComp != null)
            {
                var basePower = ((CompProperties_Power)powerComp.props).PowerConsumption;
                powerComp.PowerOutput = -(basePower + (inUse ? TransmitDrawWatts : 0f));
            }
        }
        if (transmitting)
        {
            GenTemperature.PushHeat(parent, WasteHeatWatts * RadioRulesDef.Current.heatPerWattSecond * RareTicks / 60f);
        }
    }

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
    {
        var wearer = Wearer;
        if (wearer == null || !wearer.Spawned || wearer.Faction != Faction.OfPlayer) yield break;
        var net = RadioNet.For(wearer.Map);
        net.TryGetBest(wearer, out var link);
        yield return SignalGizmo.ForHandheld(this, link);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!Props.isStation || !parent.Spawned || parent.Faction != Faction.OfPlayer) yield break;
        yield return SignalGizmo.ForStation(this, RadioNet.For(parent.Map));
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
                : "PhobosRadio_StationInTouch".Translate(net.InTouchCount(parent), pairs.Count));
        }
        sb.AppendLine();
        sb.Append("PhobosRadio_StationAntenna".Translate(net.AntennaLabelFor(parent)));
        return sb.ToString();
    }

    public override string TransformLabel(string label) => label;
}
