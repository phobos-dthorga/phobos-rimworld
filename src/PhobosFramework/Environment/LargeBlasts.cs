using System;
using System.Collections.Generic;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>Where a thing stands relative to a large blast's centre.</summary>
public enum LargeBlastBand
{
    /// <summary>Inside the fireball: nothing is left.</summary>
    Fireball,
    /// <summary>The blast zone: the blast wind throws, rolls and wrecks.</summary>
    Blast,
    /// <summary>Beyond it: heat, the pulse and what the blast still carries.</summary>
    Thermal,
}

/// <summary>One large blast's effects at one thing, as figures from 0 to 1 (1 at the fireball's edge), so any blast can describe itself.</summary>
public sealed class LargeBlastHit
{
    public LargeBlastBand Band;
    /// <summary>The blast wind's strength here: 1 at the fireball's edge, falling with distance.</summary>
    public float Wind;
    /// <summary>The heat's strength here: 1 at the fireball's edge, falling with distance.</summary>
    public float Heat;
    /// <summary>An electromagnetic pulse's damage, or 0 for none.</summary>
    public float PulseAmount;
    public Thing? Launcher;
    public ThingDef? Weapon;
}

/// <summary>What a large blast did to one thing a taker handled.</summary>
public sealed class LargeBlastOutcome
{
    public bool Gone;
    public bool Wrecked;
    public bool Battered;
    /// <summary>The pulse reached it (a running engine); whether it stalled is the vehicle's own EMP rule.</summary>
    public bool Pulsed;
    /// <summary>People who were inside and were killed, so the blast can treat their bodies as it treats others'.</summary>
    public readonly List<Pawn> Killed = new();
    public int Hurt;
    /// <summary>People still inside and alive, with the share of the outside radiation dose that reaches them through the hull.</summary>
    public readonly List<(Pawn pawn, float doseShare)> Inside = new();
}

/// <summary>A kind of thing a large blast should treat in its own way (a vehicle), registered by the mod that knows it.</summary>
public interface ILargeBlastTaker
{
    /// <summary>Whether this taker handles the thing.</summary>
    bool Takes(Thing thing);

    /// <summary>Whether the whole thing is sheltered, given which of its cells are.</summary>
    bool Sheltered(Thing thing, Func<IntVec3, bool> cellSheltered);

    /// <summary>Applies the blast to the thing and says what it did.</summary>
    LargeBlastOutcome Take(Thing thing, LargeBlastHit hit);
}

/// <summary>
/// Large blasts and the things they need to treat in their own way (Framework 0.218.0; docs/development/nuclear-strike-effects.md, NK1 to
/// NK9; owner, 22 Nov 2026). A blast of ours, or another mod's, asks <see cref="TakerFor"/> before it hits a thing: where a taker answers,
/// the taker applies the blast; where none does, the blast carries on as before. D'thorga Framework's vehicles register one for every
/// vehicle, so a strike reaches a vehicle as a machine and the people inside it, not as a body. Nothing here needs Vehicle Framework.
/// </summary>
public static class LargeBlasts
{
    static readonly List<ILargeBlastTaker> takers = new();

    /// <summary>Adds a taker; a taker already registered is not added twice.</summary>
    public static void Register(ILargeBlastTaker taker)
    {
        if (!takers.Contains(taker)) takers.Add(taker);
    }

    public static bool Any => takers.Count > 0;

    /// <summary>The taker for this thing, or null where the blast should treat it as it always has.</summary>
    public static ILargeBlastTaker? TakerFor(Thing thing)
    {
        for (int i = 0; i < takers.Count; i++)
        {
            if (takers[i].Takes(thing)) return takers[i];
        }
        return null;
    }
}
