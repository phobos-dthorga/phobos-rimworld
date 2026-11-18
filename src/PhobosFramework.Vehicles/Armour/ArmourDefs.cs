using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// An armour class, named by the threat it stops (Framework 0.154.0; docs/development/vehicle-armour.md, "Armour classes"): its figures on
/// both scales. A vehicle Def of ours gives every part the rating of a class; the patch that loads only with Combat Extended gives the same
/// part that class's millimetres and megapascals. `scripts/check-vehicle-armour.py` holds the two to these figures.
/// </summary>
public class PhobosArmourClassDef : Def
{
    /// <summary>What the class stops, in a player's words: shown under the part on the Health tab.</summary>
    [MustTranslate] public string stops = "";
    /// <summary>Under Combat Extended: millimetres of steel and megapascals.</summary>
    public float sharpMillimetres;
    public float bluntMegapascals;
    /// <summary>Without it: Vehicle Framework's ratings, on the game's scale. Authored.</summary>
    public float sharpRating;
    public float bluntRating;
    /// <summary>Lightest first.</summary>
    public int order;
}

/// <summary>
/// How a vehicle of ours is armoured (Framework 0.154.0; docs/development/vehicle-armour.md). Put on a VehicleDef whose parts carry armour by
/// class. It widens the box Combat Extended shoots at, and keeps the parts our systems append inside, so each cell keeps the one outer part
/// its design gives it.
/// </summary>
public class PhobosArmourExtension : DefModExtension
{
    /// <summary>The width in cells of the box Combat Extended tests shots against; 0 leaves Combat Extended's own one cell.</summary>
    public float hitWidthCells;
    /// <summary>Whether the parts our systems append (a radiator, a lamp bracket, a winch mount) go inside rather than over the design's
    /// own outer parts, which under Combat Extended would stack their armour on the same cell.</summary>
    public bool appendedPartsInside = true;
}

/// <summary>The armour classes, looked up once, and which one a part's armour is.</summary>
public static class ArmourClasses
{
    static List<PhobosArmourClassDef>? classes;
    static (double Sharp, double Blunt)[]? millimetres;
    static (double Sharp, double Blunt)[]? ratings;
    static DamageArmorCategoryDef? sharp;
    static DamageArmorCategoryDef? blunt;

    static void Load()
    {
        if (classes != null) return;
        classes = DefDatabase<PhobosArmourClassDef>.AllDefsListForReading.OrderBy(c => c.order).ToList();
        millimetres = classes.Select(c => ((double)c.sharpMillimetres, (double)c.bluntMegapascals)).ToArray();
        ratings = classes.Select(c => ((double)c.sharpRating, (double)c.bluntRating)).ToArray();
        sharp = DefDatabase<DamageArmorCategoryDef>.GetNamedSilentFail("Sharp");
        blunt = DefDatabase<DamageArmorCategoryDef>.GetNamedSilentFail("Blunt");
    }

    /// <summary>The class this part's armour is, on the scale in use, or null for a part with no class (another author's figures).</summary>
    public static PhobosArmourClassDef? For(VehicleComponent part)
    {
        Load();
        if (sharp == null || blunt == null || classes!.Count == 0) return null;
        double sharpFigure = part.ArmorRating(sharp, out _);
        double bluntFigure = part.ArmorRating(blunt, out _);
        bool combatExtended = OptionalMods.IsActive(OptionalMods.CombatExtended);
        int index = ArmourClassRule.Find(sharpFigure, bluntFigure, combatExtended ? millimetres! : ratings!);
        return index < 0 ? null : classes[index];
    }
}

/// <summary>The armour kits' own stats (Framework 0.158.0; vehicle-militarised-service-vehicles.md, "The kits").</summary>
[DefOf]
public static class ArmourDefOf
{
    /// <summary>Above zero once the EMP hardening kit is fitted: the vehicle's electrics take the military EMP rating.</summary>
    public static VehicleStatDef Phobos_EmpHardening = null!;

    static ArmourDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ArmourDefOf));
}
