using System.Collections.Generic;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio;

/// <summary>A dipole's shape and its own mast's drawn scale (Radio 0.64.0), on each dipole Def.</summary>
public class DipoleExtension : DefModExtension
{
    public DipoleShape shape = DipoleShape.Flat;
    /// <summary>The standard pole's drawn height against the flat dipole's 10 m pole (0.8 for the 8 m masts of the V and the short).</summary>
    public float mastScale = 1f;
}

/// <summary>
/// A dipole drawn from its parts into the map's mesh (Radio 0.64.0; docs/development/radio-hf.md, the masts): the masts upright, the
/// wire, the coax and the small parts, laid out by <see cref="DipoleLayout"/>, with the mast its fitted upgrade names (the figure
/// <see cref="MastKindFigure"/>: 0 the guyed pole, 1 the telescopic mast, 2 the lattice tower). The Def's own picture is only the
/// placement ghost and the menu icon.
/// <para>Cost: printed with the map's mesh, as any building; nothing per frame. A picture for every dipole, mast and facing would hold
/// about 160 MB of graphics memory; the parts hold under 1 MB.</para>
/// </summary>
public class Building_PhobosDipole : Building
{
    public const string MastKindFigure = "mastKind";
    const string Parts = "Phobos/Radio/Things/HfParts/";
    /// <summary>Each mast's drawn height in cells (the parts' own, compose_dipoles.py's MASTS): 10, 15 and 25 m, foreshortened.</summary>
    static readonly float[] MastCells = { 4.5f, 6f, 8f };
    static readonly string[] MastPaths = { "HfMastPole", "HfMastTelescopic", "HfMastLattice" };
    static readonly Color WireDark = new(0.09f, 0.09f, 0.10f);
    static readonly Color WireCore = new(0.50f, 0.52f, 0.54f);
    static readonly Color Coax = new(0.06f, 0.06f, 0.07f);

    static readonly Dictionary<string, Material> materials = new();

    static Material Mat(string path) =>
        materials.TryGetValue(path, out Material? held) && held != null ? held : materials[path] = MaterialPool.MatFrom(Parts + path, ShaderDatabase.Cutout);

    static Material Tinted(string key, Color colour) =>
        materials.TryGetValue(key, out Material? held) && held != null ? held
            : materials[key] = MaterialPool.MatFrom(Parts + "HfWire", ShaderDatabase.Cutout, colour);

    static (double w, double h) SizeOf(Material mat, double widthCells)
    {
        Texture tex = mat.mainTexture;
        return tex == null ? (widthCells, widthCells) : (widthCells, widthCells * tex.height / tex.width);
    }

    static (double w, double h) TallAs(Material mat, double heightCells)
    {
        Texture tex = mat.mainTexture;
        return tex == null ? (heightCells / 2, heightCells) : (heightCells * tex.width / tex.height, heightCells);
    }

    public int MastKind => Mathf.Clamp((int)RadioUpgrades.Figure(this, MastKindFigure, 0f), 0, MastPaths.Length - 1);

    public override void Print(SectionLayer layer)
    {
        var ext = def.GetModExtension<DipoleExtension>() ?? new DipoleExtension();
        int kind = MastKind;
        Material mast = Mat(MastPaths[kind]);
        float mastCells = MastCells[kind] * (kind == 0 ? ext.mastScale : 1f);
        Texture tex = mast.mainTexture;
        double mastWidth = tex == null ? mastCells / 3 : mastCells * tex.width / tex.height;
        bool alongZ = Rotation.IsHorizontal;
        int length = def.size.x;
        Material feed = Mat("HfFeed"), coil = Mat("HfCoil"), insulator = Mat("HfInsulator"), peg = Mat("HfPeg");
        double foot = kind == 2 ? DipoleLayout.TowerFoot : DipoleLayout.Foot;
        List<DipolePiece> pieces = DipoleLayout.Pieces(ext.shape, length, mastCells, mastWidth, foot, alongZ, SizeOf(feed, 0.32), SizeOf(coil, 0.42),
            SizeOf(insulator, 0.26), TallAs(peg, 0.32));
        if (kind > 0 && Seen.Wants("radio.hf-mast", this, MastPaths[kind]))
            Seen.Note("radio.hf-mast", this, def.label + " on " + MastPaths[kind], MastPaths[kind]);
        Vector3 centre = DrawPos;
        float baseAlt = AltitudeLayer.BuildingOnTop.AltitudeFor();
        foreach (DipolePiece piece in pieces)
        {
            Material mat = piece.Part switch
            {
                "mast" => mast,
                "feed" => feed,
                "coil" => coil,
                "insulator" => insulator,
                "peg" => peg,
                "coax" => Tinted("coax", Coax),
                _ => Tinted("wireDark", WireDark),
            };
            var at = new Vector3(centre.x + (float)piece.X, baseAlt + piece.Order * 0.002f, centre.z + (float)piece.Z);
            var size = new Vector2((float)piece.Width, (float)piece.Height);
            // The game turns a plane clockwise by its angle; the layout's angles run anticlockwise from east.
            float rot = -(float)piece.AngleDegrees;
            Printer_Plane.PrintPlane(layer, at, size, mat, rot);
            if (piece.Part == "wire")
                Printer_Plane.PrintPlane(layer, at + new Vector3(0, 0.001f, 0), new Vector2(size.x, size.y * 0.4f), Tinted("wireCore", WireCore), rot);
        }
    }
}
