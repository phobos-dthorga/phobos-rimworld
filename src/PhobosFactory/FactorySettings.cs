using Verse;

namespace Phobos.Factory;

/// <summary>
/// The player's choices for this mod. They live in the core assembly, which never references an optional mod,
/// so the Dubs Bad Hygiene tie-in reads them and its options window has somewhere to keep them. Read every time
/// they matter, so a change takes effect at once. The saved keys never change.
/// </summary>
public class FactorySettings : ModSettings
{
    /// <summary>Whether factory bills that name a water use need piped water.</summary>
    public bool waterRequired = true;

    /// <summary>Whether a batch made with dirty water can carry food poisoning.</summary>
    public bool dirtyWaterPoisons = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref waterRequired, "PhobosFactory_WaterRequired", true);
        Scribe_Values.Look(ref dirtyWaterPoisons, "PhobosFactory_DirtyWaterPoisons", true);
    }
}
