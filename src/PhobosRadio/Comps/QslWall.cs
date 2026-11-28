using Phobos.Radio.Services;
using Phobos.Radio.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio;

/// <summary>
/// The QSL wall (Radio 0.68.0; docs/development/radio-qsl-in-play.md, part 2, the owner's words of 28 Nov 2026: "make it like how the
/// bookshelf operates"): the game's own bookcase, holding QSL cards, so colonists take a card down to read it and the cards add beauty,
/// as books do; drawn as a cork board with each card's own front (part B's painting) in its six slots.
/// <para>Cost: drawn each frame as the game's bookcases are, six small quads at most; each card's front is painted once and kept.</para>
/// </summary>
public class Building_QslWall : Building_Bookcase
{
    /// <summary>The slots' centres and the card size, as shares of the board's picture (measured from the picked picture; agent's).</summary>
    static readonly Vector2[] Slots =
    {
        new(0.245f, 0.66f), new(0.5f, 0.66f), new(0.755f, 0.66f),
        new(0.245f, 0.35f), new(0.5f, 0.35f), new(0.755f, 0.35f),
    };
    static readonly Vector2 CardShare = new(0.21f, 0.25f);

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        Graphic.Draw(drawLoc, Rot4.North, this);
        Vector2 size = def.graphicData.drawSize;
        float alt = drawLoc.y + 0.01f;
        int slot = 0;
        foreach (Book book in HeldBooks)
        {
            if (slot >= Slots.Length) break;
            if (book is not QslCardBook { Card: { } card }) continue;
            Vector2 at = Slots[slot++];
            var pos = new Vector3(drawLoc.x + (at.x - 0.5f) * size.x, alt, drawLoc.z + (at.y - 0.5f) * size.y);
            Material mat = MaterialPool.MatFrom(new MaterialRequest(QslCardArt.Front(card), ShaderDatabase.Cutout));
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(CardShare.x * size.x, 1f, CardShare.y * size.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
        if (slot > 0 && Seen.Wants("radio.qsl-wall", this)) Seen.Note("radio.qsl-wall", this, slot + " cards shown");
    }
}

/// <summary>
/// An award's certificate, framed (Radio 0.68.0, part 2: "rewards ... observed from a distance"): art, so colonists admire it with the
/// game's own art viewing; its title is the award, not a made-up name. Posted by the QSL bureau with the cards.
/// </summary>
public class CompQslCertificate : CompArt
{
    public string award = "";

    public override bool Active => award.Length > 0;

    /// <summary>Names the certificate for its award.</summary>
    public void SetAward(string label)
    {
        award = label;
        titleInt = label;
    }

    protected override void InitializeArtInternal(Thing relatedThing, ArtGenerationContext source) => titleInt = award;

    public override TaggedString GenerateImageDescription() =>
        "PhobosRadioQsl_CertificateText".Translate(award.Named("AWARD"), Faction.OfPlayer.Name.Named("COLONY"));

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref award, "PhobosQslAward", "");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && award.Length > 0) titleInt = award;
    }
}
