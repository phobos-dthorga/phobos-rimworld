using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>What a hit on a vehicle's outer plate throws inside, if anything.</summary>
public enum SpallKind
{
    None,
    /// <summary>The round beat the plate's face: debris from what the plate took.</summary>
    Beaten,
    /// <summary>A heavy hit the face stopped: debris knocked off the inside by the shock (the owner's choice, 21 Nov 2026).</summary>
    Shock,
}

/// <summary>The figures spall is worked with, from the rules Def (all authored; docs/development/vehicle-spall.md).</summary>
public readonly struct SpallFigures
{
    /// <summary>The share of what the plate took that becomes spall when the face is beaten.</summary>
    public readonly double beatenShare;
    /// <summary>The same for a stopped heavy hit.</summary>
    public readonly double shockShare;
    /// <summary>A sharp hit stopped by the face is heavy when its penetration is at least this share of the plate's armour.</summary>
    public readonly double heavySharpShare;
    /// <summary>An explosive hit is heavy at this damage or more.</summary>
    public readonly double heavyBlastDamage;
    /// <summary>Fragments from a beaten plate without a working liner, and with one.</summary>
    public readonly int fragmentsUnlined;
    public readonly int fragmentsLined;
    /// <summary>The chance a fragment finds nothing (empty space in the compartment).</summary>
    public readonly double missChance;
    /// <summary>A fragment's penetration as a share of the plate's sharp armour.</summary>
    public readonly double penetrationShareOfPlate;
    /// <summary>The share of the spall a liner at full health stops.</summary>
    public readonly double linerStopsAtFull;
    /// <summary>The liner's health share at or below which it stops nothing (Combat Extended's own point where a part's armour is gone).</summary>
    public readonly double linerSpentAt;
    /// <summary>Above this health share a liner keeps the spray narrow.</summary>
    public readonly double linerNarrowAbove;
    /// <summary>The liner's health lost per point of spall it stops.</summary>
    public readonly double linerWearPerStopped;
    /// <summary>Liner panels that line one vehicle; a re-line takes one for each such share of the liner's health missing, rounded up.</summary>
    public readonly int panelsPerLiner;

    public SpallFigures(double beatenShare, double shockShare, double heavySharpShare, double heavyBlastDamage, int fragmentsUnlined,
        int fragmentsLined, double missChance, double penetrationShareOfPlate, double linerStopsAtFull, double linerSpentAt,
        double linerNarrowAbove, double linerWearPerStopped, int panelsPerLiner)
    {
        this.beatenShare = beatenShare;
        this.shockShare = shockShare;
        this.heavySharpShare = heavySharpShare;
        this.heavyBlastDamage = heavyBlastDamage;
        this.fragmentsUnlined = fragmentsUnlined;
        this.fragmentsLined = fragmentsLined;
        this.missChance = missChance;
        this.penetrationShareOfPlate = penetrationShareOfPlate;
        this.linerStopsAtFull = linerStopsAtFull;
        this.linerSpentAt = linerSpentAt;
        this.linerNarrowAbove = linerNarrowAbove;
        this.linerWearPerStopped = linerWearPerStopped;
        this.panelsPerLiner = panelsPerLiner;
    }

    /// <summary>The design's figures (docs/development/vehicle-spall.md), for tests and as the Def's defaults.</summary>
    public static SpallFigures Design => new(0.5, 0.25, 0.75, 30, 3, 1, 0.3, 1.0 / 3.0, 0.7, 0.25, 0.5, 0.5, 4);
}

/// <summary>A liner's state, as the Health tab names it.</summary>
public enum LinerState
{
    NotFitted,
    Sound,
    /// <summary>Under the narrow share: it catches less and the spray is wide again.</summary>
    Worn,
    /// <summary>At or under the spent share: it catches nothing.</summary>
    Spent,
}

/// <summary>
/// Spall inside vehicles (docs/development/vehicle-spall.md; owner, 21 Nov 2026), game-free. When a round beats a vehicle's outer plate,
/// debris from what the plate took sprays into the compartment behind it; a heavy hit the face stops can still knock a little off the
/// inside. A spall liner catches a share of it and wears as it does. What the plate took comes from Combat Extended's own armour function,
/// called by the game side; everything here works from that figure. Every figure is authored, a gameplay choice: no source read gives
/// how the amount of debris depends on the margin a round beats a plate by (the record says so), and the liner's 87 and 17 degree cones
/// (Rheinmetall's brochure) are drawn as a wide or a narrow reach, not as angles.
/// </summary>
public static class SpallRule
{
    /// <summary>
    /// What a reported hit throws: a beaten face throws spall; a stopped one only when the hit is heavy, which is an explosive hit of
    /// <see cref="SpallFigures.heavyBlastDamage"/> or more, or a sharp hit whose penetration came within <see cref="SpallFigures.heavySharpShare"/>
    /// of the plate's armour.
    /// </summary>
    public static SpallKind KindOf(bool faceBeaten, bool sharp, bool explosive, double damage, double penetration, double plateArmour, in SpallFigures f)
    {
        if (faceBeaten && sharp) return SpallKind.Beaten;
        // A blast is blunt under Combat Extended, beaten face or not: heavy enough, it shocks the plate.
        if (explosive && damage >= f.heavyBlastDamage) return SpallKind.Shock;
        if (!faceBeaten && sharp && plateArmour > 0 && penetration >= f.heavySharpShare * plateArmour) return SpallKind.Shock;
        return SpallKind.None;
    }

    /// <summary>The spall pool: a share of what the plate took, by kind.</summary>
    public static double Pool(SpallKind kind, double plateTook, in SpallFigures f) => kind switch
    {
        SpallKind.Beaten => Math.Max(0, plateTook) * f.beatenShare,
        SpallKind.Shock => Math.Max(0, plateTook) * f.shockShare,
        _ => 0,
    };

    /// <summary>The share of the spall a liner at this health share stops: full at full health, falling in a straight line to nothing at
    /// the spent share. No liner (or one not fitted) is a share of 0.</summary>
    public static double LinerStops(double healthShare, in SpallFigures f)
    {
        if (healthShare <= f.linerSpentAt) return 0;
        double span = 1 - f.linerSpentAt;
        double scale = span <= 0 ? 1 : Math.Min(1, (healthShare - f.linerSpentAt) / span);
        return f.linerStopsAtFull * scale;
    }

    /// <summary>Whether the liner keeps the spray to one cell.</summary>
    public static bool Narrow(double healthShare, in SpallFigures f) => healthShare > f.linerNarrowAbove;

    /// <summary>How many fragments the pool is split into: one for shock spall; for a beaten plate, fewer behind a narrow liner.</summary>
    public static int Fragments(SpallKind kind, bool narrow, in SpallFigures f) => kind switch
    {
        SpallKind.Shock => 1,
        SpallKind.Beaten => Math.Max(1, narrow ? f.fragmentsLined : f.fragmentsUnlined),
        _ => 0,
    };

    /// <summary>A fragment's penetration from the plate's sharp armour.</summary>
    public static double FragmentPenetration(double plateSharpArmour, in SpallFigures f) => Math.Max(0, plateSharpArmour) * f.penetrationShareOfPlate;

    /// <summary>What a liner loses for the spall it stopped.</summary>
    public static double LinerWear(double stopped, in SpallFigures f) => Math.Max(0, stopped) * f.linerWearPerStopped;

    /// <summary>The state the Health tab names, by the liner's health share and whether it is fitted.</summary>
    public static LinerState StateOf(bool fitted, double healthShare, in SpallFigures f)
    {
        if (!fitted) return LinerState.NotFitted;
        if (healthShare <= f.linerSpentAt) return LinerState.Spent;
        return Narrow(healthShare, f) ? LinerState.Sound : LinerState.Worn;
    }

    /// <summary>
    /// Liner panels a re-line takes: one for each share of the liner's health missing, rounded up. None until a whole share is gone
    /// (Framework 0.209.0, an agent's default after the fault-read: a panel for a scratch sent trucks to be re-lined after every fight).
    /// </summary>
    public static int PanelsToReline(double healthShare, in SpallFigures f) => PanelsToReline(healthShare, f.panelsPerLiner);

    /// <summary>The same for a liner of its own size (Framework 0.209.0: the Osprey's lines more than a truck's cab).</summary>
    public static int PanelsToReline(double healthShare, int panelsPerLiner)
    {
        double missing = 1 - Math.Max(0, Math.Min(1, healthShare));
        int perLiner = Math.Max(1, panelsPerLiner);
        if (missing * perLiner < 1 - 1e-9) return 0;
        return (int)Math.Ceiling(missing * perLiner - 1e-9);
    }

    /// <summary>The panels that come back when a liner is taken off: those paid, by the share of the liner left, rounded down.</summary>
    public static int PanelsBack(int paid, double healthShare) => Math.Max(0, (int)Math.Floor(paid * Math.Max(0, Math.Min(1, healthShare)) + 1e-9));

    /// <summary>
    /// The liner's health when a vehicle is first looked at: nothing when it is not fitted, whatever a save holds (a save may hold a whole
    /// liner that Vehicle Framework made for a vehicle off the map); otherwise the saved health, or full where a save holds none. Vehicle
    /// Framework makes every part whole, so a new truck's liner starts at zero, "not fitted", until the upgrade is fitted.
    /// </summary>
    public static double StartingHealth(double? saved, bool fitted, double maxHealth)
    {
        if (!fitted) return 0;
        if (saved.HasValue) return Math.Max(0, Math.Min(maxHealth, saved.Value));
        return maxHealth;
    }
}
