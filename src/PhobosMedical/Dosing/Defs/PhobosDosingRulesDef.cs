using System.Collections.Generic;
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

    /// <summary>Pain from which a resting patient gets a painkiller: where the game's "severe" label starts. Owner's choice (7 Nov 2026; was 0.6).</summary>
    public float painThreshold = 0.4f;
    /// <summary>Pain from which a sedative may be given when nothing lighter can: where the game's "extreme" label starts.</summary>
    public float sedationPainThreshold = 0.8f;
    /// <summary>Pain from which an awake patient fusses while being tended: where the game's "extreme" label starts.</summary>
    public float fussingPainThreshold = 0.8f;
    /// <summary>The tend quality factor at that threshold (authored).</summary>
    public float fussingFactorAtThreshold = 0.9f;
    /// <summary>The tend quality factor at full pain (authored).</summary>
    public float fussingFactorAtFullPain = 0.75f;

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
    /// <summary>A disease counts as winning when immunity would arrive this many days or less before the lethal mark. Owner's choice (7 Nov 2026; was 1).</summary>
    public float immunityRaceMarginDays = 2f;

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

    // Care needs (Medical 0.28.0; docs/development/medical-care-needs.md). Every figure authored unless its comment names a source.

    /// <summary>What one vomit a day counts against a cure or a preventive: four a day cancel the whole benefit. Authored.</summary>
    public float vomitWeight = 0.25f;
    /// <summary>An item worth this much silver or more is rare, and kept for saving a life: the mark triage uses for a rare tool (the
    /// owner's card of 26 Nov 2026). Zero or less switches the rule off.</summary>
    public float rareItemMarketValue = 300f;
    /// <summary>Prevention is for someone whose chance factor of catching the disease (the game's own) is this or more. Authored.</summary>
    public float preventionMinRisk = 0.5f;
    /// <summary>Conditions that count as an outbreak besides the ones found by what they are (a disease an event of the game brings, or
    /// one that spreads between people): for a mod whose disease arrives another way.</summary>
    public List<HediffDef> outbreakDiseases = new();
    /// <summary>How to read "this item removes these conditions" from another mod's own outcome class, as data: the class by its full
    /// name and the field holding the conditions. Read only; the class's code runs when the game gives the item, as always.</summary>
    public List<RemovalReader> removalReaders = new();

    public DoseWeights Weights => new(consciousnessWeight, breathingWeight, bleedWeight, addictionWeight, bleedWeightWhenNotBleeding, vomitWeight);

    public static PhobosDosingRulesDef Current => Current<PhobosDosingRulesDef>();
}

/// <summary>One outcome class of another mod's that removes conditions, and where it keeps them: a list of conditions, or a single one.</summary>
public class RemovalReader
{
    /// <summary>The outcome class's full name, such as Namespace.ClassName. A class no loaded mod has is skipped.</summary>
    public string doerClass = "";
    /// <summary>The field on it that holds a condition or a list of conditions.</summary>
    public string field = "";
}

/// <summary>
/// Lets any mod say, in XML, how one of its ingestibles is treated by automatic dosing: kept out of it altogether, or with an effect
/// declared by hand where the item's own Defs cannot show it (an effect its mod applies in code).
/// </summary>
public class PhobosDosingExtension : DefModExtension
{
    public bool neverAutoDose;
    /// <summary>Conditions one dose removes outright (Medical 0.28.0), besides any the index reads from the item itself.</summary>
    public List<HediffDef> cures = new();
    /// <summary>Conditions one dose makes the taker immune to (Medical 0.28.0), besides any the index reads from the item itself.</summary>
    public List<HediffDef> prevents = new();
}
