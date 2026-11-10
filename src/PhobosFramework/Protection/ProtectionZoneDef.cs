using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Protection;

/// <summary>
/// A kind of protective-gear line: what protects (a stat worn gear raises), how much is enough, and the colour its painted area shows
/// in. Content mods define one per hazard in XML (D'thorga Medical's radiation line first); control points and painted areas name it.
/// The label reads in sentences such as "radiation controlled area".
/// </summary>
public class ProtectionZoneDef : Def
{
    /// <summary>The stat worn gear must raise, read on the pawn, so genes, implants and every worn item count.</summary>
    public StatDef stat = null!;

    /// <summary>The stat's value a pawn needs to cross the line without stopping.</summary>
    public float required = 0.75f;

    /// <summary>The painted area's colour.</summary>
    public Color color = new(1f, 0.85f, 0.2f);

    public override System.Collections.Generic.IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (stat == null) yield return "names no stat for worn gear to raise";
        if (required <= 0f) yield return "requires nothing, so it would never stop anyone";
    }
}
