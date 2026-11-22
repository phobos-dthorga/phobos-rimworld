using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The glass's figures that are not in its game-free rule (Framework 0.238.0; docs/development/vehicle-glass.md). The first loaded instance
/// wins; another mod may patch it in XML. Every figure is authored.
/// </summary>
public class PhobosGlassRulesDef : PhobosRulesDef
{
    /// <summary>The glass part's health: its share of whole glass is what counts, so one figure serves every kind.</summary>
    public int partHealth = 40;
    /// <summary>The glass part's name on the Health tab.</summary>
    [MustTranslate] public string partLabel = "glass";
    /// <summary>The goods that re-glaze it, from the glass chain.</summary>
    public ThingDef? laminatedPane;
    public ThingDef? glassPane;
    public ThingDef? armouredPane;
    public ThingDef? visionBlock;
    /// <summary>The work of re-glazing: a set amount for the job, and this much more for all of it.</summary>
    public float reglazeBaseWork = 400f;
    public float reglazeWorkForAll = 1200f;
    /// <summary>The upgrade node that makes a cab armoured, and so its glass armoured glass.</summary>
    public string armouredCabNode = "PhobosArmouredCab";

    public static PhobosGlassRulesDef Current => Current<PhobosGlassRulesDef>();
}

