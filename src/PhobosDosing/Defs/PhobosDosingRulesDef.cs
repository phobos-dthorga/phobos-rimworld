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

    /// <summary>Pain from which a resting patient gets a painkiller. Authored: inside the game's "severe" label, which runs 0.4 to 0.8.</summary>
    public float painThreshold = 0.6f;
    /// <summary>Pain from which a sedative may be given when nothing lighter can: where the game's "extreme" label starts.</summary>
    public float sedationPainThreshold = 0.8f;

    /// <summary>Pain from which a pawn who is up and about looks for a painkiller: the game's "serious pain" mood band starts at 0.15 and "intense" at 0.4. Owner's choice.</summary>
    public float selfPainThreshold = 0.4f;
    /// <summary>How much lower that is with chemical interest, and with chemical fascination. Authored.</summary>
    public float selfInterestOffset = 0.1f;
    public float selfFascinationOffset = 0.2f;
    /// <summary>
    /// Consciousness and breathing must stay at or above this after a dose a pawn takes on their own feet. The game drops a pawn below 0.3
    /// consciousness (PawnCapacitiesHandler.CanBeAwake), so this sits above it with a margin. Authored.
    /// </summary>
    public float selfCapacityFloor = 0.4f;

    /// <summary>Doctors hold a painkiller back while a patient bleeds this fast or faster: 0.3 is 30% a day on the health tab. Authored.</summary>
    public float bleedHoldRate = 0.3f;

    /// <summary>A drug must add at least this to immunity gain speed to count as one for fighting disease. Authored.</summary>
    public float minImmunityGain = 0.1f;
    /// <summary>A disease counts as winning when immunity would arrive this many days or less before the lethal mark. Authored.</summary>
    public float immunityRaceMarginDays = 1f;

    /// <summary>An item must take away at least this share of the pain to be indexed: relief as a side effect does not make a painkiller.</summary>
    public float minPainRelief = 0.3f;

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
    /// <summary>The share of the bleed weight that counts while the patient is not bleeding.</summary>
    public float bleedWeightWhenNotBleeding = 0.25f;
    public float addictionWeight = 0.5f;

    public DoseWeights Weights => new(consciousnessWeight, breathingWeight, bleedWeight, addictionWeight, bleedWeightWhenNotBleeding);

    public static PhobosDosingRulesDef Current => Current<PhobosDosingRulesDef>();
}

/// <summary>Lets any mod keep one of its ingestibles out of automatic dosing, in XML.</summary>
public class PhobosDosingExtension : DefModExtension
{
    public bool neverAutoDose;
}
