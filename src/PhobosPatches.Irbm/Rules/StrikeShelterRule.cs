namespace Phobos.MissilePatches.Rules;

/// <summary>What stood over and around a person when the strike's light, heat and dose arrived, before the blast stripped any roof.</summary>
public enum StrikeCover
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
/// Shadow and shelter from a strike (D'thorga Patches 0.36.0; docs/development/nuclear-strike-effects.md, NK12, NK17, NK18; the owner's
/// choice of 22 Nov 2026 to wait for our own radiation model, which now exists). The light, heat and dose arrive before the blast, so
/// what stood overhead first decides them: any roof shadows a person from the heat and the flash ("any solid, opaque material"); the
/// dose is cut by mass, as Glasstone and Dolan's Table 8.72 gives for buildings; and flash blindness is likelier and lasts longer at
/// night, when the pupils are open. Game-free.
/// </summary>
public static class StrikeShelterRule
{
    /// <summary>
    /// The share of the outside dose that reaches someone under this cover. Gamma rays, from Glasstone and Dolan, Table 8.72: a frame
    /// house passes 0.8 to 1.0 (0.9 here), a concrete blockhouse with 9-inch walls 0.1 to 0.2 (0.15), three feet underground 0.002 to
    /// 0.004 (0.003). A thin rock roof is between, 0.05 (agent's figure). Neutrons are left out.
    /// </summary>
    public static float DosePass(StrikeCover cover) => cover switch
    {
        StrikeCover.LightBuilding => 0.9f,
        StrikeCover.StoneBuilding => 0.15f,
        StrikeCover.ThinRock => 0.05f,
        StrikeCover.ThickRock => 0.003f,
        _ => 1f,
    };

    /// <summary>Whether this cover shadows a person from the fireball's heat and flash: any roof does (windows are not yet modelled).</summary>
    public static bool Shadowed(StrikeCover cover) => cover != StrikeCover.Open;

    /// <summary>The cover from what was overhead: no roof, a natural roof (thick or thin), or a built one with stone walls or not.</summary>
    public static StrikeCover Cover(bool roofed, bool natural, bool thick, bool stoneWalls) =>
        !roofed ? StrikeCover.Open
        : natural ? (thick ? StrikeCover.ThickRock : StrikeCover.ThinRock)
        : stoneWalls ? StrikeCover.StoneBuilding
        : StrikeCover.LightBuilding;

    /// <summary>How much likelier flash blindness is by the light: half as likely again in full dark as at noon (authored; the
    /// sources say only that it is worse at night, the pupil being larger).</summary>
    public static float FlashChanceFactor(float sunGlow) => Lerp(1.5f, 1f, sunGlow);

    /// <summary>
    /// The starting severity of the game's flash blindness, which heals about one a game hour: by day it "does not persist for more than
    /// about 2 minutes" (0.04); at night full night vision takes 15 to 35 minutes to return (0.5) (US Army FM 8-9, paragraph 412).
    /// </summary>
    public static float FlashSeverity(float sunGlow) => Lerp(0.5f, 0.04f, sunGlow);

    static float Lerp(float a, float b, float t) => a + (b - a) * (t < 0 ? 0 : t > 1 ? 1 : t);
}
