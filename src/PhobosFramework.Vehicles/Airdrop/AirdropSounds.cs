using RimWorld;
using Verse;
using Verse.Sound;

namespace Phobos.VehicleSystems;

/// <summary>
/// The airdrop's sounds (Framework 0.306.0; docs/development/sound-catalogue.md, S150 to S152): a canopy opening, a bundle landing and a
/// platform landing. Each plays only once its SoundDef ships, after the owner picks it from the night run; until then nothing plays and
/// nothing is logged.
/// </summary>
public static class AirdropSounds
{
    public const string CanopyOpens = "Phobos_Airdrop_CanopyOpens";
    public const string BundleLands = "Phobos_Airdrop_BundleLands";
    public const string PlatformLands = "Phobos_Airdrop_PlatformLands";

    public static void Play(string defName, Map? map, IntVec3 cell)
    {
        if (map == null || !cell.InBounds(map) || DefDatabase<SoundDef>.GetNamedSilentFail(defName) is not { } sound) return;
        sound.PlayOneShot(new TargetInfo(cell, map));
    }
}
