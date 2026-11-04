using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Gases;

/// <summary>
/// One of the framework's own additive room gases. Other mods add gases in XML. A gas exists in a room only while something
/// adds it; the engine spreads it through open doors and vents, lets it out through gaps in the roof, and fades it at its
/// half-life. Add one only where it earns more than an effect inferred from an existing gas (owner's rule, 4 Nov 2026).
/// </summary>
public class PhobosGasDef : Def
{
    /// <summary>Half-life indoors, in ticks (2500 ticks is one in-game hour). 0 or less: never fades on its own.</summary>
    public float decayHalfLifeTicks = 2500f;
    /// <summary>Half-life of the part of a room that has no roof over it, in ticks.</summary>
    public float openRoofHalfLifeTicks = 600f;
    /// <summary>The thickest a room can get, in the engine's unit (1 = a full room). Sources beyond this are lost.</summary>
    public float maxConcentration = 1f;
    /// <summary>Whether a filter mask (the game's toxic environment resistance, a gas-mask tag) keeps the gas out. Carbon monoxide: no.</summary>
    public bool filteredByMask = true;
    /// <summary>The existing gas kind this one defers to in Automatic source mode, or None when nothing else carries it.</summary>
    public GasKind preferExisting = GasKind.None;
    /// <summary>The owner's pin: this gas is always the framework's, whatever the source mode.</summary>
    public bool alwaysOwn;
    /// <summary>Colour in the dev overlay.</summary>
    public Color overlayColor = new(0.8f, 0.8f, 0.2f, 1f);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (maxConcentration <= 0f) yield return defName + ": maxConcentration must be positive";
    }
}

/// <summary>The engine's starting values, one Def, first loaded wins. Every figure is authored unless its comment names a source.</summary>
public class PhobosGasRulesDef : Defs.PhobosRulesDef
{
    /// <summary>The game's open door: rate 1 every 34 ticks (Core, BuildingProperties.doorTempEqualizeIntervalOpen).</summary>
    public float doorOpenRate = 1f;
    public int doorOpenIntervalTicks = 34;
    /// <summary>The game's closed door: rate 1 every 375 ticks (doorTempEqualizeIntervalClosed).</summary>
    public float doorClosedRate = 1f;
    public int doorClosedIntervalTicks = 375;
    /// <summary>The game's vent: rate 14 every rare tick, 250 ticks (Building_Vent.TickRare).</summary>
    public float ventRate = 14f;
    public int ventIntervalTicks = 250;
    /// <summary>Below this concentration a room's gas is dropped.</summary>
    public float minConcentration = 0.001f;
    /// <summary>A room this far into vacuum holds nothing (0 to 1, the game's room vacuum).</summary>
    public float vacuumClears = 0.5f;
    /// <summary>Without Combat Extended, toxic environment resistance from this up counts as sealed air.</summary>
    public float sealedToxicResistance = 0.999f;
    /// <summary>How enclosed a roofed cell is in a room that counts as outdoors.</summary>
    public float enclosureRoofedButOutdoors = 0.25f;

    /// <summary>Fire smoke on the game's grid, per fire size per pass, as a fraction of a full cell (authored: a size-1 fire fills its cell in about 12 passes).</summary>
    public float fireSmokePerSizePerPass = 0.08f;
    public float fireSmokeMaxPerPass = 0.2f;
    /// <summary>Carbon monoxide per fire size per pass, in cells of gas (authored: a size-1 fire fills a 20-cell room in about 50 passes, 50 minutes).</summary>
    public float fireCarbonMonoxidePerSizePerPass = 0.4f;
    public float fireCarbonMonoxideMaxPerPass = 1f;
    /// <summary>The framework's carbon monoxide gas, which fires feed. Null if the Def is missing.</summary>
    public PhobosGasDef? fireCarbonMonoxideGas;

    public float DoorOpenCoefficient => Rules.GasMixRule.OpeningCoefficient(doorOpenRate, doorOpenIntervalTicks);
    public float DoorClosedCoefficient => Rules.GasMixRule.OpeningCoefficient(doorClosedRate, doorClosedIntervalTicks);
    public float VentCoefficient => Rules.GasMixRule.OpeningCoefficient(ventRate, ventIntervalTicks);

    public static PhobosGasRulesDef Current => Current<PhobosGasRulesDef>();
}
