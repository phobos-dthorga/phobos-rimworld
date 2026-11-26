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
    /// <summary>Breathing at or under this is a breathing rescue: where triage calls a vital capacity immediate (the owner's card of 26
    /// Nov 2026).</summary>
    public float breathingRescueLevel = 0.3f;
    /// <summary>An item must add at least this to breathing, after any breathing it costs, to count as a help to it. Authored.</summary>
    public float minBreathingGain = 0.1f;
    /// <summary>Toxic exposure counts as under way from this toxic buildup: about two of the game's fallout ticks unprotected. Authored.</summary>
    public float toxicExposureFrom = 0.04f;
    /// <summary>An item must keep out at least this share of toxic buildup to count as protection against it. Authored.</summary>
    public float minToxicCut = 0.25f;
    /// <summary>An item must add at least this to vacuum resistance to count as protection against vacuum. Authored.</summary>
    public float minVacuumGain = 0.25f;

    // The long-term needs (Medical 0.31.0).

    /// <summary>An item must lower the mental break threshold by at least this to count as steadying someone. Authored.</summary>
    public float minBreakThresholdDrop = 0.03f;
    /// <summary>Conditions that treat addiction, by their own class or a comp's class, full names: read, never run.</summary>
    public List<string> addictionTreatments = new();
    /// <summary>Comps that regrow missing body parts, by their class's full name.</summary>
    public List<string> regrowthClasses = new();
    /// <summary>Reservoirs an artificial part draws on, and the outcome that refills them: read by name, never run.</summary>
    public List<ReservoirReader> reservoirs = new();
    /// <summary>Prevention is for someone whose chance factor of catching the disease (the game's own) is this or more. Authored.</summary>
    public float preventionMinRisk = 0.5f;
    /// <summary>Conditions that count as an outbreak besides the ones found by what they are (a disease an event of the game brings, or
    /// one that spreads between people): for a mod whose disease arrives another way.</summary>
    public List<HediffDef> outbreakDiseases = new();
    /// <summary>How to read "this item removes these conditions" from another mod's own outcome class, as data: the class by its full
    /// name and the field holding the conditions. Read only; the class's code runs when the game gives the item, as always.</summary>
    public List<RemovalReader> removalReaders = new();
    /// <summary>How to read "this item gives this condition" from another mod's own class (Medical 0.32.0): an outcome class, or a
    /// use-effect comp on an item the game applies through its use-on-swallowing outcome. Read only, as the removal readers are.</summary>
    public List<GivenConditionReader> givenConditionReaders = new();

    public DoseWeights Weights => new(consciousnessWeight, breathingWeight, bleedWeight, addictionWeight, bleedWeightWhenNotBleeding, vomitWeight);

    public static PhobosDosingRulesDef Current => Current<PhobosDosingRulesDef>();
}

/// <summary>
/// A reservoir another mod keeps on a person (The Dead Man's Switch's biogel), and the outcome of its that refills it (Medical 0.31.0): the
/// condition's class and the member holding its level, its capacity, and the refilling outcome's class with the field holding how much
/// one dose adds. A class no loaded mod has is skipped.
/// </summary>
public class ReservoirReader
{
    public string hediffClass = "";
    public string levelMember = "";
    public float capacity = 100f;
    public string refillDoerClass = "";
    public string amountField = "";
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
/// One class of another mod's that gives a condition when an item is swallowed (Medical 0.32.0): an outcome class (Vanilla Races Expanded -
/// Genie's antibiotics), or a use-effect comp's properties on an item the game applies through its use-on-swallowing outcome (Vanilla
/// Factions Expanded - Medieval 2's draughts). The game's own use-effect comp needs no reader. A class no loaded mod has is skipped.
/// </summary>
public class GivenConditionReader
{
    /// <summary>The class's full name, such as Namespace.ClassName.</summary>
    public string className = "";
    /// <summary>The field holding the condition given.</summary>
    public string conditionField = "hediffDef";
    /// <summary>The field holding the severity given; empty, or zero or less without <see cref="negativeTakesAway"/>, means the
    /// condition's own starting severity.</summary>
    public string severityField = "";
    /// <summary>A negative severity takes that much away from a condition already there, rather than meaning "the default": such an
    /// effect gives nothing and is not read.</summary>
    public bool negativeTakesAway;
    /// <summary>A field that, when set, makes the effect depend on the taker (a gene they must have): such an effect is not read,
    /// because the index is the same for everyone.</summary>
    public string skipWhenSetField = "";
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
    /// <summary>One dose eases an addiction (Medical 0.31.0).</summary>
    public bool treatsAddiction;
    /// <summary>One dose heals old wounds and chronic conditions over time (Medical 0.31.0).</summary>
    public bool healsOldWounds;
    /// <summary>One dose regrows missing body parts (Medical 0.31.0).</summary>
    public bool regrowsParts;
}
