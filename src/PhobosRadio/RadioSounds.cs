using RimWorld;
using Verse;

namespace Phobos.Radio;

/// <summary>
/// The radio's own sounds (Radio 0.16.0; docs/development/sound-catalogue.md, S102, S103, S147, S148; the owner's picks of 25 Nov 2026).
/// The stations and amplifiers start and end them when their state changes at their rare tick, through D'thorga Framework's
/// SustainedSound; the terminal's hum (S149) is its power comp's own soundAmbientPowered, so it has no entry here.
/// </summary>
[DefOf]
public static class RadioSoundDefOf
{
    public static SoundDef PhobosRadio_StationSquelch = null!;
    public static SoundDef PhobosRadio_HandheldKeyUp = null!;
    public static SoundDef PhobosRadio_AmplifierRelay = null!;
    public static SoundDef PhobosRadio_AmplifierFan = null!;

    static RadioSoundDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioSoundDefOf));
}
