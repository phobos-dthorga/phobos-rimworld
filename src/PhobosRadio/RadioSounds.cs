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
    // Radio 0.30.0, the owner's picks of 27 Nov 2026: a repeater's courtesy tail (S166), a station's channel knob (S169) and a coax
    // connector screwed home (S170). A radio's squelch as it engages is its own Def's engageSound (S163), not an entry here.
    public static SoundDef PhobosRadio_RepeaterCourtesy = null!;
    public static SoundDef PhobosRadio_ChannelKnob = null!;
    public static SoundDef PhobosRadio_CoaxConnector = null!;

    static RadioSoundDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioSoundDefOf));
}
