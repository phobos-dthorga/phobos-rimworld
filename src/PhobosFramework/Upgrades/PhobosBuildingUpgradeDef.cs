using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Phobos.Framework.Upgrades;

/// <summary>One figure an upgrade sets on its building, by name: the building's comp asks for it (<see cref="CompPhobosBuildingUpgrades.Figure"/>).</summary>
public class PhobosUpgradeFigure
{
    public string name = "";
    public float value;
}

/// <summary>
/// An upgrade fitted to a placed building (D'thorga Framework 0.210.0; docs/development/vehicle-service-yard.md, "Building upgrades"; owner,
/// 20 Nov 2026: a charging point "as an upgrade, if possible? ... perhaps even a Framework of some kind"). Data, not code: which buildings it
/// fits, what it costs and takes to fit, what it needs first, and what it changes, either a named ability a comp asks for ("charges
/// vehicles") or a figure by name ("pumpPerHour"). A building takes upgrades by carrying <see cref="CompProperties_PhobosBuildingUpgrades"/>;
/// other modders add upgrades in XML. A mechanic brings the materials and does the work, as for a vehicle fitting.
/// </summary>
public class PhobosBuildingUpgradeDef : Def
{
    /// <summary>The buildings it can be fitted to. Each must carry <see cref="CompProperties_PhobosBuildingUpgrades"/>.</summary>
    public List<ThingDef> buildings = new();
    /// <summary>What fitting it costs, brought to the building before the work starts.</summary>
    public List<ThingDefCountClass> costList = new();
    /// <summary>The work to fit it, in the game's work units (as a building's WorkToBuild).</summary>
    public float work = 1000f;
    /// <summary>The Construction skill a colonist needs to fit or take it off.</summary>
    public int constructionSkill;
    public ResearchProjectDef? researchPrerequisite;
    /// <summary>An upgrade that must be fitted first: this one builds on it.</summary>
    public PhobosBuildingUpgradeDef? prerequisite;
    /// <summary>Abilities it gives the building, by name, for the building's own comps to ask for.</summary>
    public List<string> grants = new();
    /// <summary>Figures it sets on the building, by name. Where two fitted upgrades set one, the larger is used.</summary>
    public List<PhobosUpgradeFigure> figures = new();
    /// <summary>The share of its materials taking it off gives back (authored, as a vehicle fitting's removal).</summary>
    public float refundShare = 0.5f;
    /// <summary>The share of its work taking it off takes.</summary>
    public float removeWorkShare = 0.5f;
    /// <summary>A picture drawn over the building while it is fitted; none where the inspect text is enough.</summary>
    public GraphicData? overlay;
    /// <summary>The overlay's height above the building (authored: just over it).</summary>
    public float overlayAltitude = 0.04f;

    [Unsaved] Graphic? overlayGraphic;

    /// <summary>The overlay, made on first use on the main thread (pictures cannot be loaded while Defs are read).</summary>
    public Graphic? OverlayGraphic => overlay == null ? null : overlayGraphic ??= overlay.Graphic;

    public bool Researched => researchPrerequisite == null || researchPrerequisite.IsFinished;

    public bool Sets(string figure) => figures.Any(f => f.name == figure);

    /// <summary>The cost as plain pairs, for the rules.</summary>
    public IEnumerable<(ThingDef what, int count)> Cost => costList.Where(line => line.thingDef != null).Select(line => (line.thingDef, line.count));

    /// <summary>The cost in the player's words, "steel x40, components x2", or "nothing".</summary>
    public string CostText => costList.Count == 0 ? "PhobosUpgrade_CostNothing".Translate().Resolve()
        : costList.Where(line => line.thingDef != null).Select(line => line.thingDef.label + " x" + line.count).ToCommaList(useAnd: true);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (buildings.Count == 0) yield return "names no building to fit";
        foreach (ThingDef building in buildings)
        {
            if (building?.GetCompProperties<CompProperties_PhobosBuildingUpgrades>() == null)
            {
                yield return "names " + (building?.defName ?? "a missing building") + ", which has no CompProperties_PhobosBuildingUpgrades";
            }
        }
        if (work < 0f) yield return "has negative work";
        if (prerequisite == this) yield return "needs itself first";
        foreach (PhobosUpgradeFigure figure in figures)
        {
            if (figure.name.NullOrEmpty()) yield return "sets a figure with no name";
        }
    }

    /// <summary>The upgrades that fit each kind of building, worked out once.</summary>
    static Dictionary<ThingDef, List<PhobosBuildingUpgradeDef>>? byBuilding;

    static readonly List<PhobosBuildingUpgradeDef> None = new();

    public static List<PhobosBuildingUpgradeDef> For(ThingDef building)
    {
        if (byBuilding == null)
        {
            byBuilding = new Dictionary<ThingDef, List<PhobosBuildingUpgradeDef>>();
            foreach (PhobosBuildingUpgradeDef def in DefDatabase<PhobosBuildingUpgradeDef>.AllDefsListForReading)
            {
                foreach (ThingDef fits in def.buildings)
                {
                    if (fits == null) continue;
                    if (!byBuilding.TryGetValue(fits, out List<PhobosBuildingUpgradeDef>? list)) byBuilding[fits] = list = new List<PhobosBuildingUpgradeDef>();
                    list.Add(def);
                }
            }
        }
        return byBuilding.TryGetValue(building, out List<PhobosBuildingUpgradeDef>? found) ? found : None;
    }
}
