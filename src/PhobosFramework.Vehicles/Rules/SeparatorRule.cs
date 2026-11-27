namespace Phobos.Framework.Vehicles.Rules;

/// <summary>How an intake separator is set: by the ground (the default), held on, or held off.</summary>
public enum SeparatorMode
{
    Automatic,
    On,
    Off,
}

/// <summary>
/// An intake separator for turbine aircraft, and the inlet barrier filter that clogs (Framework 0.365.0;
/// docs/development/vehicle-inlet-separator.md; the owner's answers of 28 Nov 2026: a switch, automatic by default, and the barrier clogs and
/// the compressor wash cleans it). Game-free. Every figure is authored for play: no real engine's separator was measured.
/// </summary>
public static class SeparatorRule
{
    /// <summary>The extra share of the engine's work a separator costs while it is on (IS1): the same flight asks the engine for 3 % more.</summary>
    public const double DemandShare = 0.03;
    /// <summary>The share of landing dust, grit strikes and rough-ground compressor fouling it keeps out while on (IS2).</summary>
    public const double KeepsOut = 0.5;
    /// <summary>Under an automatic separator, this share of rough ground is enough to switch it on.</summary>
    public const double RoughEnough = 0.01;
    /// <summary>A barrier filter's clogging adds this much of its own fitted cost again when it is wholly clogged (IS4: twice the cost).</summary>
    public const double ClogDoublesCost = 1;

    /// <summary>Whether a separator is on now: held on, held off, or, automatic, over rough ground on a map.</summary>
    public static bool On(SeparatorMode mode, bool onMap, double roughShare) => mode switch
    {
        SeparatorMode.On => true,
        SeparatorMode.Off => false,
        _ => onMap && roughShare >= RoughEnough,
    };

    /// <summary>What reaches the engine of something a separator can keep out: all of it while off, half while on.</summary>
    public static double Reaching(bool on) => on ? 1 - KeepsOut : 1;

    /// <summary>The extra demand from the intake now: the separator's share while on, and a clogged barrier's extra cost (its fitted cost times
    /// how clogged it is).</summary>
    public static double ExtraDemand(bool separatorOn, double barrierCost, double barrierClog) =>
        (separatorOn ? DemandShare : 0) + System.Math.Max(0, barrierCost) * ClogDoublesCost * System.Math.Min(1, System.Math.Max(0, barrierClog));

    /// <summary>The next position of the switch, as a press of its button turns it: automatic, held on, held off, and round again.</summary>
    public static SeparatorMode Next(SeparatorMode mode) => mode switch
    {
        SeparatorMode.Automatic => SeparatorMode.On,
        SeparatorMode.On => SeparatorMode.Off,
        _ => SeparatorMode.Automatic,
    };
}
