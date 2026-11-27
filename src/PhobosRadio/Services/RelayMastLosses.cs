namespace Phobos.Radio.Services;

/// <summary>Whether relay masts wear and can be raided (Radio 0.37.0): the setting, off by default. Read in one place.</summary>
public static class RelayMastLosses
{
    public static bool On => RadioMod.Settings.relayMastLosses;
}
