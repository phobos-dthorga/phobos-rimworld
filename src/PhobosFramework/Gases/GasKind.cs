namespace Phobos.Framework.Gases;

/// <summary>The gases that already exist in the game and in other mods, which the framework reads and writes before making
/// any of its own. A framework gas Def can name the kind it defers to.</summary>
public enum GasKind
{
    None,
    /// <summary>The game's blinding smoke, or Combat Extended's black smoke where that runs.</summary>
    Smoke,
    /// <summary>Biotech's tox gas.</summary>
    Toxic,
    /// <summary>Rot stink from corpses.</summary>
    RotStink,
    /// <summary>Anomaly's deadlife dust.</summary>
    DeadlifeDust,
}
