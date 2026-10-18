using Verse;

namespace Phobos.Radio;

/// <summary>
/// On a stuff or building Def: what one crossing of it costs a signal, in dB. Patched onto the vanilla
/// stuffs and natural rock in Common/Patches; other mods add their own materials the same way.
/// </summary>
public class RadioObstructionExtension : DefModExtension
{
    public float lossDb;
}

public enum NoiseWhen
{
    /// <summary>Only while the building's power trader reports power on (generators, machines).</summary>
    PowerOn,
    /// <summary>While the building is connected to a power net at all (batteries, which have no trader).</summary>
    Connected,
    Always,
}

/// <summary>On a building Def: it raises the local noise floor around it while active.</summary>
public class RadioNoiseExtension : DefModExtension
{
    /// <summary>The noise it radiates, as a level at one metre in dBm in a 12.5 kHz channel. Authored.</summary>
    public float noiseDbmAt1m = -70f;
    /// <summary>How far it is counted, in cells; 0 means the rules Def's default.</summary>
    public int radiusCells;
    public NoiseWhen noiseWhen = NoiseWhen.PowerOn;
}

/// <summary>On a coax Def: its loss per 100 m at a reference frequency, scaled by √(f/fref).</summary>
public class RadioCoaxExtension : DefModExtension
{
    public float lossDbPer100m = 8f;
    public float referenceMHz = 146f;
}
