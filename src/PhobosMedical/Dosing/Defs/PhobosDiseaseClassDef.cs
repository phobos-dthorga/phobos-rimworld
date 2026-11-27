using Verse;

namespace Phobos.Dosing.Defs;

/// <summary>
/// A class of disease by its cause (Medical 0.33.0; docs/development/medical-care-round-two.md): bacterial, viral, protozoan, worms,
/// mechanites. A disease names its class through <see cref="PhobosDiseaseClassExtension"/>; a drug for fighting disease names the classes
/// it acts on in <see cref="PhobosDosingExtension.actsOn"/>. Another mod adds a class with a Def like ours.
/// </summary>
public class PhobosDiseaseClassDef : Def
{
}

/// <summary>On a disease's HediffDef: its class. A disease with none takes any drug for fighting disease (the owner's card of 27 Nov 2026).</summary>
public class PhobosDiseaseClassExtension : DefModExtension
{
    public PhobosDiseaseClassDef? diseaseClass;
}

public static class DiseaseClasses
{
    /// <summary>A disease's class, or null when nobody has classed it.</summary>
    public static PhobosDiseaseClassDef? Of(HediffDef def) => def.GetModExtension<PhobosDiseaseClassExtension>()?.diseaseClass;
}
