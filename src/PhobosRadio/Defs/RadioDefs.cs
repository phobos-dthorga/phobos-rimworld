using System.Collections.Generic;
using Verse;

namespace Phobos.Radio;

/// <summary>A frequency band. Other mods add bands in XML; the propagation model is chosen per band.</summary>
public class BandDef : Def
{
    public float lowMHz;
    public float highMHz;
    /// <summary>The frequency every loss is computed at. Defaults to the middle of the band.</summary>
    public float centreMHz;
    /// <summary>Scales the loss through walls and built roofs: below 1 copes better indoors (UHF), above 1 worse. Rock and natural roofs are not scaled.</summary>
    public float obstructionFactor = 1f;

    public override void PostLoad()
    {
        base.PostLoad();
        if (centreMHz <= 0) centreMHz = (lowMHz + highMHz) / 2f;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (lowMHz <= 0 || highMHz <= lowMHz) yield return $"{defName}: lowMHz and highMHz must be positive and in order";
        if (obstructionFactor <= 0) yield return $"{defName}: obstructionFactor must be positive";
    }
}

/// <summary>A mode: what a signal carries, how much bandwidth it needs and how weak it may be.</summary>
public class ModeDef : Def
{
    public float bandwidthHz;
    /// <summary>Signal-to-noise ratio the mode needs in its own bandwidth, in dB.</summary>
    public float requiredSnrDb;
    /// <summary>Fraction of the time a transmitter is keyed while a link is in use.</summary>
    public float dutyCycle = 0.2f;
    /// <summary>Transmitter efficiency for this mode's amplifier class; the rest becomes heat.</summary>
    public float transmitEfficiency = 0.6f;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (bandwidthHz <= 0) yield return $"{defName}: bandwidthHz must be positive";
        if (dutyCycle <= 0 || dutyCycle > 1) yield return $"{defName}: dutyCycle must be in (0, 1]";
        if (transmitEfficiency <= 0 || transmitEfficiency > 1) yield return $"{defName}: transmitEfficiency must be in (0, 1]";
    }
}

/// <summary>
/// A kind of channel use in the ledger (Radio 0.8.0; the owner, 24 Nov 2026: "Start on Phase 2's ledger kinds", after D'thorga
/// Mechanised Specialist Expansion's BVR channels, which count artillery, local, world and strike apart). A radio names its kind; the
/// kind's mode sets how many channels one use takes and how many radios share it by turns. Kinds never share a block of channels, so a
/// station's tooltip can count them apart. Other mods add kinds in XML, never a second system of frequencies (the radio's rules).
/// </summary>
public class ChannelKindDef : Def
{
    /// <summary>The defName of the kind a radio has when it names none: the colony's local voice net.</summary>
    public const string LocalVoiceName = "PhobosRadio_KindLocalVoice";

    /// <summary>What a use of this kind carries: its bandwidth and duty cycle set the channels and the turns.</summary>
    public ModeDef? mode;
    /// <summary>Kinds with a lower order are served first when channels are short, and listed first (authored).</summary>
    public int order = 100;
    /// <summary>
    /// Whether a use of this kind, finding every station it reaches full, takes channels from the kinds served after it that do not
    /// pre-empt (Radio 0.9.0), as a trunked system's emergency call does: the remote control of vehicles (authored).
    /// </summary>
    public bool preempts;

    private static List<ChannelKindDef>? inOrder;
    private static ChannelKindDef? localVoice;

    public ModeDef Mode => mode!;

    /// <summary>Every kind, lowest order first, then by defName; its index is the kind's place in the ledger.</summary>
    public static List<ChannelKindDef> InOrder
    {
        get
        {
            if (inOrder != null) return inOrder;
            inOrder = new List<ChannelKindDef>(DefDatabase<ChannelKindDef>.AllDefsListForReading);
            inOrder.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.defName, b.defName));
            for (var i = 0; i < inOrder.Count; i++) inOrder[i].ledgerIndex = i;
            return inOrder;
        }
    }

    /// <summary>The local voice net's kind, or the first kind there is.</summary>
    public static ChannelKindDef LocalVoice => localVoice ??= DefDatabase<ChannelKindDef>.GetNamedSilentFail(LocalVoiceName) ?? InOrder[0];

    [Unsaved] private int ledgerIndex = -1;

    /// <summary>This kind's place in <see cref="InOrder"/>.</summary>
    public int LedgerIndex
    {
        get
        {
            _ = InOrder;
            return ledgerIndex;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (mode == null) yield return $"{defName}: a channel kind needs a mode";
    }
}

/// <summary>
/// A radio fitted to something that does not wear one (Radio 0.10.0; the autonomous vehicles record, step 2): a vehicle's control
/// radio, written once and named by every vehicle that carries it. Its <see cref="radio"/> is the comp's figures, as on any radio:
/// bands, mode, power, antenna height and the kind of channel use it asks for. A tie-in puts it on the things that name it.
/// </summary>
public class RadioFittingDef : Def
{
    public CompProperties_Radio? radio;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (radio == null)
        {
            yield return $"{defName}: a radio fitting needs a radio";
            yield break;
        }
        if (radio.bands.Count == 0) yield return $"{defName}: its radio needs at least one band";
        if (radio.mode == null) yield return $"{defName}: its radio needs a mode";
        if (radio.isStation) yield return $"{defName}: a fitted radio is never a station";
    }
}

/// <summary>
/// The starting values the design says to tune in play. One Def; the first one loaded wins.
/// Player preferences (display mode, metres per cell, the thought) are mod settings, not rules.
/// </summary>
public class RadioRulesDef : Def
{
    // Margin bands, in dB, above which each outcome starts.
    public float marginalDb = 0f;
    public float workableDb = 6f;
    public float cleanDb = 15f;

    // What a roof over an antenna costs, in dB, by roof kind.
    public float roofConstructedLossDb = 6f;
    public float roofThinNaturalLossDb = 20f;
    public float roofThickLossDb = 60f;

    // Obstructions on the line between two antennas.
    public float rockLossDb = 30f;
    public float defaultStuffLossDb = 10f;
    /// <summary>A wall only blocks where the straight line between the antennas is below this height.</summary>
    public float wallHeightMetres = 2.5f;

    /// <summary>
    /// The width of one channel, in Hz: 25 kHz, as on a trunked radio system (the owner, 24 Nov 2026). A mode takes as many as its
    /// bandwidth needs, rounded up (Radio 0.7.0).
    /// </summary>
    public float channelWidthHz = 25000f;

    // Noise sources without a radius of their own reach this far.
    public int noiseRadiusDefaultCells = 12;
    /// <summary>
    /// Where a source's noise starts to fade, as a fraction of its radius; it reaches nothing at the radius (the owner's choice of a
    /// taper, 23 Nov 2026). 1 keeps the old hard edge. Authored.
    /// </summary>
    public float noiseFadeStartFraction = 0.67f;

    // The coverage overlay (Radio 0.2.0). Performance is the mod's first concern: these bound what it costs.
    /// <summary>Signal to spare, in dB, at which the overlay reaches its brightest colour. Authored from the mock-up's open ground.</summary>
    public float coverageFullScaleDb = 130f;
    /// <summary>The most real time the overlay spends working out cells in one frame, in milliseconds. Authored; measure and tune.</summary>
    public float coverageBudgetMsPerFrame = 1.5f;
    /// <summary>The first, quick pass works out one cell per square block this many cells wide and paints the block. Authored.</summary>
    public int coverageCoarseBlockCells = 4;
    /// <summary>How opaque the overlay is over the map, from 0 to 1. Authored.</summary>
    public float coverageOpacity = 0.45f;

    /// <summary>How often links are recomputed. A pawn walks about four cells in 60 ticks.</summary>
    public int recomputeIntervalTicks = 60;

    /// <summary>
    /// Heat pushed into the room per watt of waste per second. The vanilla heater turns 175 W into
    /// 21 heat per second, so 0.12 (read from Core's Buildings_Temperature.xml, 19 Oct 2026).
    /// </summary>
    public float heatPerWattSecond = 0.12f;

    public Physics.MarginBands MarginBands => new(marginalDb, workableDb, cleanDb);

    private static RadioRulesDef? current;

    public static RadioRulesDef Current
    {
        get
        {
            if (current == null)
            {
                var all = DefDatabase<RadioRulesDef>.AllDefsListForReading;
                current = all.Count > 0 ? all[0] : new RadioRulesDef { defName = "PhobosRadio_RulesFallback" };
                if (all.Count == 0) Log.Warning("[PhobosRadio] No RadioRulesDef loaded; using built-in defaults.");
            }
            return current;
        }
    }
}
