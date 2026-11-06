using System.Collections.Generic;
using Phobos.Framework.Defs;
using Phobos.Triage.Rules;
using Verse;

namespace Phobos.Triage.Defs;

/// <summary>
/// The starting values the design says to tune in play. One Def; the first loaded wins. Every figure is authored unless its
/// comment names a source. Player preferences (the defaults by group, the risk the doctors may take, messages) are mod settings.
/// </summary>
public class PhobosTriageRulesDef : PhobosRulesDef
{
    /// <summary>How often each map's patients are re-read and bills queued or withdrawn.</summary>
    public int recomputeIntervalTicks = 250;
    /// <summary>A vital capacity at or under this, pushed down by a lasting bad condition, is lethal: the game drops a pawn below 0.3 consciousness.</summary>
    public float lethalCapacityLevel = 0.3f;
    /// <summary>A capacity capped at or under this cripples (authored).</summary>
    public float cripplingCapacityLevel = 0.5f;
    /// <summary>A part efficiency or capacity offset at or under this cripples (authored).</summary>
    public float cripplingOffset = -0.5f;
    /// <summary>Dying within this many hours is immediate (authored).</summary>
    public float immediateHours = 6f;
    /// <summary>Dying within this many hours is urgent; past it still urgent, with a lower score (authored).</summary>
    public float urgentHours = 24f;
    /// <summary>A dying patient with a vital capacity at or under this is immediate whatever the clock (authored).</summary>
    public float immediateVitalLevel = 0.3f;
    /// <summary>Never queue past this many bills on one patient: the game's own list holds 15.</summary>
    public int maxBillsPerPatient = 15;
    /// <summary>A bill for a lethal condition goes to the top of the patient's list.</summary>
    public bool lethalBillsToTop = true;
    /// <summary>An ingredient that is not medicine and is worth at least this is a rare tool, kept for the player's own bills unless allowed (authored).</summary>
    public float rareToolMarketValue = 300f;
    /// <summary>Drug addictions and gene-driven chemical dependencies are left out of the pass entirely (owner's rule, 6 Nov 2026).</summary>
    public bool excludeChemicalDependencies = true;

    public ClassThresholds Thresholds => new(lethalCapacityLevel, cripplingCapacityLevel, cripplingOffset);
    public TriageBands Bands => new(immediateHours, urgentHours, immediateVitalLevel);

    public static PhobosTriageRulesDef Current => Current<PhobosTriageRulesDef>();
}

/// <summary>
/// On a RecipeDef: the conditions it treats beyond its removesHediff (for a recipe whose worker does the removing in its own
/// way), or a refusal to ever be queued by the doctors on their own.
/// </summary>
public class PhobosTriageExtension : DefModExtension
{
    public List<HediffDef> treatsHediffs = new();
    public bool neverAutoQueue;
}
