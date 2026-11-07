using System.Collections.Generic;
using Phobos.Framework.Defs;
using Verse;

namespace Phobos.BurnsAndSmoke.Defs;

/// <summary>
/// The starting values the design says to tune in play. One Def; the first loaded wins. Every figure is authored unless its
/// comment names a source. Player preferences (the toggles, the display mode) are mod settings, not rules.
/// </summary>
public class PhobosBurnRulesDef : PhobosRulesDef
{
    /// <summary>How often a pawn's burns, shock, smoke and carbon monoxide are recomputed. Givers run every 60 ticks; this is a multiple.</summary>
    public int recomputeIntervalTicks = 300;

    // Burned body fraction (the game's version of total body surface area).
    public float burnLoadVisibleTbsa = 0.05f;
    /// <summary>Burn shock starts here: 15 to 20% is the threshold for fluid resuscitation (Parkland formula). Approximate.</summary>
    public float shockStartTbsa = 0.15f;
    /// <summary>
    /// A part burned off counts as its whole skin burned while the stump is fresh (untended); once tended it fades to nothing
    /// over this many days. Authored: a grafted or granulating stump takes weeks to close.
    /// </summary>
    public float burnedOffHealDays = 15f;
    /// <summary>
    /// Past the start threshold the shock target is at least this, so a young pawn whose Baux score is under the no-death mark
    /// still shows the compensated stage and the fluids job. Authored.
    /// </summary>
    public float shockThresholdFloor = 0.1f;

    // Revised Baux score (Osler and colleagues, 2010): age + percent burned + 17 with an inhalation injury.
    public float bauxNoDeathScore = 36f;
    public float bauxNoSurvivorScore = 110f;
    public float inhalationBauxPoints = 17f;
    /// <summary>Carbon monoxide severity from which the score counts an inhalation injury.</summary>
    public float inhalationCarbonMonoxideThreshold = 0.2f;
    public float shockRisePerDay = 1f;
    public float shockFallPerDay = 0.35f;
    // Treatment: each term takes a slice off the untreated target; fluids scale with the tend quality of the shock's own
    // tending (medicine potency times the doctor's skill). Authored so that industrial medicine with bed rest holds the worst
    // scores at the "shock" stage and glitterworld at "compensated" (owner's choice, 4 Nov 2026).
    public float tendedShockRelief = 0.2f;
    public float bedRestShockRelief = 0.15f;
    public float fluidsShockReliefPerQuality = 0.35f;
    /// <summary>The game's best tend quality (glitterworld medicine's ceiling); relief stops growing past it.</summary>
    public float fluidsQualityCap = 1.3f;
    public float shockTreatmentFloor = 0.1f;
    /// <summary>Where burn shock starts, and the severity below which it leaves once the burned fraction is under the threshold.</summary>
    public float shockInitialSeverity = 0.01f;
    public float shockRemoveBelow = 0.02f;

    // Airway burn: delayed swelling over hours, resolving in about two days (approximate).
    public float airwayBaseChance = 0.15f;
    /// <summary>Fresh burn damage to the head and neck that gives half the base chance.</summary>
    public float airwayReferenceDamage = 10f;
    /// <summary>The share of the base chance that the reference damage earns.</summary>
    public float airwayChanceAtReferenceDamage = 0.5f;
    /// <summary>The peak of the lightest exposure, as a share of the full stage table; the worst reaches all of it.</summary>
    public float airwayLightestPeak = 0.5f;

    // Carbon monoxide: half-life about 320 minutes on room air (Weaver and colleagues, 2000); faster in open air (authored).
    public float coUptakePerHourAtFullGas = 0.3f;
    public float coCap = 0.8f;

    // Lung injury and the acute smoke condition (the acute one runs only when Combat Extended's does not).
    public float lungInjuryPerHourAtFullSmoke = 0.15f;
    /// <summary>Combat Extended's own figure per 50-tick check, reused for ours.</summary>
    public float acuteSmokePerCheck = 0.045f;

    // Fire heat in the open: the hot zone around a fire whose heat the game would otherwise throw away outdoors.
    /// <summary>How often the zones are rebuilt from the map's fires; the game's own fire does its heat every 150 ticks.</summary>
    public int fireHeatIntervalTicks = 150;
    /// <summary>The game's largest fire (its MaxFireSize, 1.75); a fire this big gives the full peak.</summary>
    public float fullFireSize = 1.75f;
    /// <summary>
    /// Degrees above the air at the flame itself for a full-size fire. Authored, approximate: wood flames run about 600 to 1000 C
    /// (Babrauskas, "Temperatures in flames and fires"); a cell beside one is far cooler, which the falloff gives.
    /// </summary>
    public float fireHeatPeakRise = 600f;
    /// <summary>How far the zone reaches, in cells. At the edge a full fire adds a thirty-sixth of its peak (authored).</summary>
    public float fireHeatRadiusCells = 5f;
    /// <summary>The most several fires can add together; the game's display holds up to 999 C (authored).</summary>
    public float fireHeatMaxRise = 900f;
    /// <summary>
    /// Fire heat in rooms: a room this many cells or smaller gets no extra local heat, as the game's even spread already heats it
    /// through; larger rooms get the part the spread misses. Authored: a 5 by 6 room.
    /// </summary>
    public float fireHeatRoomCellsWarmedEvenly = 30f;
    /// <summary>How far a pawn looks for a cooler cell when the heat would burn them (authored).</summary>
    public float fireHeatFleeSearchRadiusCells = 12f;
    // Acid on the skin (D'thorga Medical 0.7.0, with the safety shower).
    /// <summary>Burn damage an hour, spread over the acid burns, while fresh acid is still on the skin (authored).</summary>
    public float chemicalDeepenPerHourAtFull = 2f;

    // Eschar (burns phase 2, owner's go of 7 Nov 2026).
    /// <summary>A limb whose open burn damage reaches this share of its health forms a constricting eschar (owner's go: 60%).</summary>
    public float escharPartHealthFraction = 0.6f;
    /// <summary>Eschar severity from which blood flow fails and, without More Injuries, ischaemic damage starts (authored).</summary>
    public float escharIschaemiaFromSeverity = 0.7f;
    /// <summary>Ischaemic damage per hour at the tightest eschar, without More Injuries (authored: a human leg is lost in about three days).</summary>
    public float ischaemicDamagePerHourAtFull = 0.4f;
    /// <summary>
    /// The care a fluids bill counts as for burn shock, as a tend quality (authored: a measured drip is the best fluid replacement there
    /// is, so the top of the scale).
    /// </summary>
    public float fluidBillTendQuality = 1f;
    /// <summary>The care a fluids kit counts as for burn shock (authored: a bag hung by any doctor, just short of a measured drip).</summary>
    public float fluidKitTendQuality = 0.9f;

    /// <summary>
    /// Mods that model the air cell by cell themselves; while one is active our zones are not computed, so heat is never
    /// counted twice. Other such mods can be added here in XML.
    /// </summary>
    public List<string> perCellTemperatureMods = new();

    public static PhobosBurnRulesDef Current => Current<PhobosBurnRulesDef>();
}

/// <summary>
/// Marks an injury Def as a chemical burn whose chemical stays on the skin until rinsed off with water: while it does, the burns deepen.
/// On the game's acid burn; other mods add it in XML.
/// </summary>
public class PhobosRinsedByWaterExtension : DefModExtension
{
}

/// <summary>
/// Marks an injury Def as a burn for the burn load and says how its damage turns into burned area. Patched onto the game's
/// burns and Combat Extended's; other mods add it in XML. Depth is the Def's own severity; these two fields are area.
/// </summary>
public class PhobosBurnLikeExtension : DefModExtension
{
    /// <summary>Fraction of the part one point of damage covers. Default: a wide, shallow fire burn, 12 points for the whole part (authored).</summary>
    public float areaPerDamage = 1f / 12f;
    /// <summary>The most of a part one burn of this kind can cover; 1 for fire, small for a round or a beam (authored).</summary>
    public float maxArea = 1f;
}
