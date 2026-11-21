using System.Collections.Generic;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;

namespace Phobos.VehicleSystems;

/// <summary>
/// A large blast on a vehicle (Framework 0.218.0; docs/development/nuclear-strike-effects.md, NK2, NK3, NK8, NK9; owner, 22 Nov 2026):
/// how weight and distance decide a vehicle's fate, which soft parts burn, how the people aboard are hurt, and what share of the dose
/// reaches them. The first loaded instance wins; another mod may patch it in XML. Every figure is authored, a gameplay choice shaped by
/// the ratios in the record's sources.
/// </summary>
public class PhobosVehicleBlastRulesDef : PhobosRulesDef
{
    public float referenceTonnes = 20f;
    public float lightestTonnes = 0.5f;
    public float least = 0.02f;
    public float outerShare = 0.8f;
    public float innerShare = 0.2f;
    public float spread = 0.5f;
    public float softFrom = 0.3f;
    public float softShare = 1f;
    /// <summary>The part roles that count as soft: rubber, plastic and canvas that heat takes first.</summary>
    public List<string> softRoles = new();
    public float crewHarm = 25f;
    public float crewMostSeverity = 2f;
    public float crewBlowMost = 10f;
    public float hullOpen = 1f;
    public float hullLight = 0.9f;
    public float hullMedium = 0.7f;
    public float hullHeavy = 0.35f;
    public float mediumTonnes = 15f;
    public float heavyTonnes = 40f;

    VehicleBlastFigures? figures;

    public VehicleBlastFigures Figures => figures ??= new VehicleBlastFigures(referenceTonnes, lightestTonnes, least, outerShare, innerShare,
        spread, softFrom, softShare, crewHarm, crewMostSeverity, crewBlowMost, hullOpen, hullLight, hullMedium, hullHeavy, mediumTonnes,
        heavyTonnes);
}
