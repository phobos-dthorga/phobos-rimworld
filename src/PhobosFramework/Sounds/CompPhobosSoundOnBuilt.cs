using RimWorld;
using Verse;
using Verse.Sound;

namespace Phobos.Framework.Sounds;

/// <summary>
/// A building's own sound as it is finished, or placed again (Framework 0.338.0; first used for the radio's coax, the owner's card of
/// 27 Nov 2026: a coax connector screwed home as each run is built). The game plays no sound of a building's own when it is finished, so
/// this is a Def's way to give one: <c>&lt;li Class="Phobos.Framework.Sounds.CompProperties_PhobosSoundOnBuilt"&gt;&lt;sound&gt;...&lt;/sound&gt;&lt;/li&gt;</c>.
/// Not on a load, and only for the player's own: a raid's or a quest's buildings arrive silently.
/// </summary>
public class CompProperties_PhobosSoundOnBuilt : CompProperties
{
    public SoundDef? sound;

    public CompProperties_PhobosSoundOnBuilt() => compClass = typeof(CompPhobosSoundOnBuilt);

    public override System.Collections.Generic.IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (sound == null) yield return $"{parentDef.defName}: CompProperties_PhobosSoundOnBuilt needs a sound";
    }
}

public class CompPhobosSoundOnBuilt : ThingComp
{
    public CompProperties_PhobosSoundOnBuilt Props => (CompProperties_PhobosSoundOnBuilt)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        // A sound only: nothing is spawned here (src/AGENTS.md: never spawn a thing from PostSpawnSetup).
        if (respawningAfterLoad || parent.Faction != Faction.OfPlayer || Props.sound == null) return;
        Props.sound.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
        Seen.Note("framework.sound-on-built", parent);
    }
}
