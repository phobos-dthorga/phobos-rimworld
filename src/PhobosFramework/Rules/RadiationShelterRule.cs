namespace Phobos.Framework.Rules;

/// <summary>What stood over and around a person when a blast's light, heat and dose arrived, before any roof fell.</summary>
public enum ShelterCover
{
    /// <summary>Under the open sky.</summary>
    Open,
    /// <summary>Under a built roof, in a room walled with anything but stone: a frame house.</summary>
    LightBuilding,
    /// <summary>Under a built roof, in a room walled mostly with stone or concrete.</summary>
    StoneBuilding,
    /// <summary>Under a thin natural rock roof: an overhang or the edge of a mountain.</summary>
    ThinRock,
    /// <summary>Under thick rock: deep in a mountain.</summary>
    ThickRock,
}

/// <summary>
/// How much of a blast's prompt radiation reaches someone under cover (moved from D'thorga Patches' strike, Framework 0.358.0, so the strike
/// and every radiation burst share one copy; docs/development/radiation-bursts.md, and nuclear-strike-effects.md, NK12 and NK17). The
/// dose arrives before the blast, so what stood overhead first decides it; mass cuts it, as Glasstone and Dolan's Table 8.72 gives for
/// buildings. Game-free.
/// </summary>
public static class RadiationShelterRule
{
    /// <summary>
    /// The share of the outside dose that reaches someone under this cover. Gamma rays, from Glasstone and Dolan, Table 8.72: a frame
    /// house passes 0.8 to 1.0 (0.9 here), a concrete blockhouse with 9-inch walls 0.1 to 0.2 (0.15), three feet underground 0.002 to
    /// 0.004 (0.003). A thin rock roof is between, 0.05 (agent's figure). Neutrons are left out. Where <paramref name="buildingsShield"/> is
    /// false (a weapon whose rays pass through built roofs, such as IRBM's gamma-ray burst), a building counts as the open sky; rock still cuts.
    /// </summary>
    public static float DosePass(ShelterCover cover, bool buildingsShield = true) => cover switch
    {
        ShelterCover.LightBuilding => buildingsShield ? 0.9f : 1f,
        ShelterCover.StoneBuilding => buildingsShield ? 0.15f : 1f,
        ShelterCover.ThinRock => 0.05f,
        ShelterCover.ThickRock => 0.003f,
        _ => 1f,
    };

    /// <summary>Whether this cover shadows a person from a fireball's heat and flash: any roof does (windows are not yet modelled).</summary>
    public static bool Shadowed(ShelterCover cover) => cover != ShelterCover.Open;

    /// <summary>The cover from what was overhead: no roof, a natural roof (thick or thin), or a built one with stone walls or not.</summary>
    public static ShelterCover Cover(bool roofed, bool natural, bool thick, bool stoneWalls) =>
        !roofed ? ShelterCover.Open
        : natural ? (thick ? ShelterCover.ThickRock : ShelterCover.ThinRock)
        : stoneWalls ? ShelterCover.StoneBuilding
        : ShelterCover.LightBuilding;
}
