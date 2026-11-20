using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Spall inside vehicles (Framework 0.207.0; docs/development/vehicle-spall.md; owner, 21 Nov 2026): what a beaten plate throws into the
/// compartment behind it, and what a spall liner catches. Combat Extended only. The first loaded instance wins; another mod may patch it
/// in XML. Every figure is authored, a gameplay choice.
/// </summary>
public class PhobosSpallRulesDef : PhobosRulesDef
{
    /// <summary>The lightest armour class a plate must reach to throw spall: thinner plate tears rather than spalls (agent's inference).</summary>
    public PhobosArmourClassDef? minimumClass;
    /// <summary>The share of what the plate took that becomes spall when the face is beaten, and for a stopped heavy hit.</summary>
    public float beatenShare = 0.5f;
    public float shockShare = 0.25f;
    /// <summary>A sharp hit the face stopped is heavy when its penetration came within this share of the plate's armour.</summary>
    public float heavySharpShare = 0.75f;
    /// <summary>An explosive hit is heavy at this damage or more.</summary>
    public float heavyBlastDamage = 30f;
    public int fragmentsUnlined = 3;
    public int fragmentsLined = 1;
    /// <summary>The chance a fragment finds nothing in the compartment.</summary>
    public float missChance = 0.3f;
    /// <summary>A fragment's penetration as a share of the plate's sharp armour.</summary>
    public float penetrationShareOfPlate = 0.3333f;
    /// <summary>The liner: what it stops at full health, where it is spent, above what it keeps the spray narrow, and its wear.</summary>
    public float linerStopsAtFull = 0.7f;
    public float linerSpentAt = 0.25f;
    public float linerNarrowAbove = 0.5f;
    public float linerWearPerStopped = 0.5f;
    /// <summary>Liner panels that line one vehicle; a re-line takes one for each such share of the liner's health missing.</summary>
    public int panelsPerLiner = 4;
    /// <summary>The panel item, by name: it is D'thorga Vehicles' and loads only with Combat Extended, so the Framework never needs it.</summary>
    public string linerPanel = "PhobosVehicles_SpallLinerPanel";
    /// <summary>The work in ticks a re-line takes, for a mechanic of construction speed 1.</summary>
    public float relineWork = 2400f;

    SpallFigures? figures;

    public SpallFigures Figures => figures ??= new SpallFigures(beatenShare, shockShare, heavySharpShare, heavyBlastDamage, fragmentsUnlined,
        fragmentsLined, missChance, penetrationShareOfPlate, linerStopsAtFull, linerSpentAt, linerNarrowAbove, linerWearPerStopped, panelsPerLiner);

    /// <summary>The plate's sharp armour, in Combat Extended's millimetres, below which nothing spalls.</summary>
    public float MinimumMillimetres => minimumClass?.sharpMillimetres ?? 8f;

    ThingDef? panel;
    bool panelLooked;

    /// <summary>The liner panel, or null where D'thorga Vehicles' Combat Extended folder is not loaded.</summary>
    public ThingDef? Panel
    {
        get
        {
            if (!panelLooked)
            {
                panelLooked = true;
                panel = DefDatabase<ThingDef>.GetNamedSilentFail(linerPanel);
            }
            return panel;
        }
    }
}

/// <summary>
/// Listed on a spall liner's upgrade node (Framework 0.207.0): fitting the upgrade puts a whole liner in, removing it takes the liner out.
/// The liner is a part of its own whose health is its state ("not fitted" and "spent" are both nothing), so this never runs after a load:
/// the saved health stands. Never Vehicle Framework's health upgrade, which would repair the part at every load.
/// </summary>
public class PhobosSpallLinerUpgrade : Upgrade
{
    public override bool UnlockOnLoad => false;

    public override void Unlock(VehiclePawn vehicle, bool unlockingPostLoad)
    {
        if (!unlockingPostLoad) Spall.SetLiner(vehicle, whole: true, "fitted");
    }

    public override void Refund(VehiclePawn vehicle) => Spall.SetLiner(vehicle, whole: false, "removed");
}
