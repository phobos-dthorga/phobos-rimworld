using Phobos.Framework.WorldMap;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>What gave a thing its trouble: a hit (and the kind of damage), or a strain the thing itself went through.</summary>
public enum CauseKind
{
    Unknown,
    Hit,
    Strain,
}

/// <summary>
/// What caused a condition, when and where (Framework 0.181.0; docs/development/vehicle-emergent-play.md, EM24, "say why"). The cause is
/// known at the moment a condition begins and was dropped there; this keeps it, so a report can say "a bullet hit, 3 days ago, near Amber
/// Anchorage" instead of only "radiator holed". Generic: a strain's <see cref="detail"/> is a key its owner reads back in its own words
/// (for a vehicle, the system stage that gave it), so any mod's conditions can carry one. Saved with the condition it explains.
/// </summary>
public sealed class CauseNote : IExposable
{
    public CauseKind kind;
    public DamageDef? damage;
    /// <summary>A strain's key, for its owner to describe; empty for a hit.</summary>
    public string detail = "";
    public int tick = -1;
    public PlanetTile tile = PlanetTile.Invalid;

    public static CauseNote Hit(DamageDef? damage, PlanetTile tile) => new() { kind = CauseKind.Hit, damage = damage, tick = Now, tile = tile };

    public static CauseNote Strain(string detail, PlanetTile tile) => new() { kind = CauseKind.Strain, detail = detail ?? "", tick = Now, tile = tile };

    /// <summary>The prefix of a strain's key that is a translation key of its own words (Framework 0.187.0: a surge from the grid).</summary>
    public const string WordsPrefix = "words:";

    /// <summary>A strain told in its own words: the key is translated whenever the cause is shown.</summary>
    public static CauseNote Words(string translationKey, PlanetTile tile) => Strain(WordsPrefix + translationKey, tile);

    /// <summary>The words of a strain told in its own words; null for any other cause.</summary>
    public string? OwnWords() => kind == CauseKind.Strain && detail.StartsWith(WordsPrefix) ? detail.Substring(WordsPrefix.Length).Translate().Resolve() : null;

    static int Now => Find.TickManager?.TicksGame ?? 0;

    /// <summary>A hit in words ("a bullet hit"); an unknown kind of damage reads as a plain hit.</summary>
    public string HitText() => damage != null ? "PhobosCause_HitBy".Translate(damage.label.Named("DAMAGE")) : "PhobosCause_Hit".Translate();

    /// <summary>When and where, in words: "3 days ago, near Amber Anchorage".</summary>
    public string WhenAndWhere()
    {
        int now = Now;
        string ago = tick < 0 || tick > now ? "PhobosCause_JustNow".Translate() : "PhobosCause_Ago".Translate((now - tick).ToStringTicksToPeriod().Named("TIME"));
        return "PhobosCause_WhenWhere".Translate(ago.Named("WHEN"), WorldPlaces.Describe(tile).Named("PLACE"));
    }

    /// <summary>The whole line, given the owner's words for a strain: "Cause: a bullet hit, 3 days ago, near Amber Anchorage".</summary>
    public string Line(string? strainWords) =>
        "PhobosCause_Line".Translate((kind == CauseKind.Hit ? HitText() : OwnWords() ?? (strainWords.NullOrEmpty() ? "PhobosCause_Unknown".Translate().Resolve() : strainWords!)).Named("WHAT"),
            WhenAndWhere().Named("WHENWHERE"));

    public void ExposeData()
    {
        Scribe_Values.Look(ref kind, "kind");
        Scribe_Defs.Look(ref damage, "damage");
        Scribe_Values.Look(ref detail, "detail", "");
        Scribe_Values.Look(ref tick, "tick", -1);
        Scribe_Values.Look(ref tile, "tile", PlanetTile.Invalid);
    }
}
