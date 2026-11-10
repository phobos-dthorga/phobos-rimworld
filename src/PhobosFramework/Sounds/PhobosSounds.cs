using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Phobos.Framework.Sounds;

/// <summary>Marks a SoundDef as one of ours, so the framework's sound volume setting scales it. Every D'thorga SoundDef carries it, in
/// whichever mod defines it; the sound's authored volume is set against a comparable game sound (docs/development/sound-generation.md).</summary>
public class PhobosSoundVolumeExtension : DefModExtension
{
}

/// <summary>The one volume setting for every D'thorga sound (AGENTS.md: quiet, never alarming, with a volume setting). It scales each
/// marked SoundDef's sub-sounds from their authored volume, which it remembers, so applying it again never compounds. A sound already
/// playing takes the new volume when its next sample starts.</summary>
public static class PhobosSoundVolume
{
    static readonly Dictionary<SubSoundDef, FloatRange> Authored = new();

    /// <summary>How many SoundDefs the setting reached the last time it was applied, for the load line.</summary>
    public static int Marked { get; private set; }

    public static void Apply(float factor)
    {
        int marked = 0;
        foreach (SoundDef def in DefDatabase<SoundDef>.AllDefsListForReading)
        {
            if (!def.HasModExtension<PhobosSoundVolumeExtension>()) continue;
            marked++;
            foreach (SubSoundDef sub in def.subSounds)
            {
                if (!Authored.TryGetValue(sub, out FloatRange authored)) Authored[sub] = authored = sub.volumeRange;
                sub.volumeRange = new FloatRange(SoundVolumeRule.Scale(authored.min, factor), SoundVolumeRule.Scale(authored.max, factor));
            }
        }
        Marked = marked;
    }
}

/// <summary>A looping sound that plays while a condition holds: started when it becomes true, ended when it becomes false or its thing
/// leaves. Nothing about it is saved; the owner calls <see cref="Set"/> when its state is worked out again after a load.</summary>
public sealed class SustainedSound
{
    Sustainer? sustainer;
    SoundDef? playing;

    public bool Playing => sustainer is { Ended: false };

    /// <summary>Starts or ends the loop. A different sound while one plays ends the old one and starts the new in its place.</summary>
    public void Set(SoundDef? def, bool on, Thing where)
    {
        if (on && def != null && where.Spawned)
        {
            if (Playing && playing == def) return;
            End();
            sustainer = def.TrySpawnSustainer(SoundInfo.InMap(where, MaintenanceType.None));
            playing = def;
            return;
        }
        End();
    }

    public void End()
    {
        if (Playing) sustainer!.End();
        sustainer = null;
        playing = null;
    }

    /// <summary>Sets an external parameter the playing sound's SoundDef maps (its paramMappings, from SoundParamSource_External), such as
    /// an engine's pitch; nothing when it is not playing. The game reads it every frame for a Constant mapping.</summary>
    public void SetParam(string name, float value)
    {
        if (Playing) sustainer!.externalParams[name] = value;
    }
}

/// <summary>What other mods already sound, so a sound of ours can stand down rather than double one (owner, 11 Nov 2026, for coughs: never
/// double up with another audio mod, the owner's or any player's).</summary>
public static class SoundsElsewhere
{
    /// <summary>The name of the first active mod, other than <paramref name="ours"/>, with a SoundDef whose defName holds one of these
    /// words (any case), or with one of these package IDs active; null if none. Read once Defs are loaded.</summary>
    public static string? FirstMod(IEnumerable<string> words, IEnumerable<string> packageIds, ModContentPack? ours)
    {
        foreach (string packageId in packageIds)
        {
            if (OptionalMods.IsActive(packageId)) return ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true)?.Name ?? packageId;
        }
        var wanted = new List<string>(words);
        foreach (SoundDef def in DefDatabase<SoundDef>.AllDefsListForReading)
        {
            if (def.modContentPack == null || def.modContentPack == ours || def.modContentPack.IsOfficialMod) continue;
            foreach (string word in wanted)
            {
                if (def.defName.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0) return def.modContentPack.Name;
            }
        }
        return null;
    }
}

[DefOf]
public static class PhobosSoundDefOf
{
    public static SoundDef PhobosFramework_SwitchOn = null!;
    public static SoundDef PhobosFramework_SwitchOff = null!;

    static PhobosSoundDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(PhobosSoundDefOf));
}

/// <summary>The click of a physical switch, for toggles that stand for one on a machine (a vehicle's lights, a rig's mode). Orders and
/// requests keep the game's own tick, as they are not switches.</summary>
public static class SwitchSounds
{
    public static Command_Toggle AsPhysicalSwitch(this Command_Toggle toggle)
    {
        toggle.turnOnSound = PhobosSoundDefOf.PhobosFramework_SwitchOn;
        toggle.turnOffSound = PhobosSoundDefOf.PhobosFramework_SwitchOff;
        return toggle;
    }
}
