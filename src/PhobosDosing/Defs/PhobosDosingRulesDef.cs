using Phobos.Dosing.Rules;
using Phobos.Framework.Defs;
using Verse;

namespace Phobos.Dosing.Defs;

/// <summary>
/// The starting values the design says to tune in play. One Def; the first loaded wins. Every figure is authored unless its
/// comment names a source. Player preferences (the toggles, the care level, the display mode) are mod settings, not rules.
/// </summary>
public class PhobosDosingRulesDef : PhobosRulesDef
{
    /// <summary>How often the list of patients who need a dose is rebuilt per map.</summary>
    public int recomputeIntervalTicks = 250;

    /// <summary>Pain from which a resting patient gets a painkiller: the game's "severe" pain band.</summary>
    public float painThreshold = 0.6f;
    /// <summary>Pain from which a sedative may be given when nothing lighter can: the game's pain-shock band.</summary>
    public float sedationPainThreshold = 0.8f;

    /// <summary>No new dose while an earlier one's condition is still at this share of a dose or more.</summary>
    public float redoseBelowFraction = 0.25f;
    /// <summary>No dose while the game's drug overdose is at this severity or more.</summary>
    public float overdoseLimit = 0.3f;
    /// <summary>Consciousness and breathing must stay at or above this after a dose's offsets; the game kills at zero.</summary>
    public float capacityFloor = 0.15f;

    /// <summary>A consciousness ceiling at or below this makes a dose a sedative; an offset is a cost, never a sedation.</summary>
    public float sedativeConsciousnessCeiling = 0.1f;

    // How much each side effect counts against relief when choosing between items.
    public float consciousnessWeight = 0.5f;
    public float breathingWeight = 0.5f;
    public float bleedWeight = 1f;
    public float addictionWeight = 0.5f;

    public DoseWeights Weights => new(consciousnessWeight, breathingWeight, bleedWeight, addictionWeight);

    public static PhobosDosingRulesDef Current => Current<PhobosDosingRulesDef>();
}

/// <summary>Lets any mod keep one of its ingestibles out of automatic dosing, in XML.</summary>
public class PhobosDosingExtension : DefModExtension
{
    public bool neverAutoDose;
}
