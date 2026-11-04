using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Gases;

/// <summary>An XML-only gas source: a building or thing that adds an existing gas kind, or a framework gas, every rare tick.</summary>
public class CompProperties_PhobosGasEmitter : CompProperties
{
    /// <summary>An existing gas kind to add through the carriers, as a fraction of a full cell per rare tick.</summary>
    public GasKind kind = GasKind.None;
    /// <summary>A framework gas to add to the room, in cells of gas per rare tick.</summary>
    public PhobosGasDef? gas;
    public float amountPerRareTick = 0.05f;
    /// <summary>Only while a power comp reports power on.</summary>
    public bool onlyWhenPowered;
    /// <summary>Only while a flick switch is on.</summary>
    public bool onlyWhenSwitchedOn;
    /// <summary>Only while the thing has fuel.</summary>
    public bool onlyWhenFuelled;

    public CompProperties_PhobosGasEmitter() => compClass = typeof(CompPhobosGasEmitter);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (kind == GasKind.None && gas == null) yield return parentDef.defName + ": PhobosGasEmitter needs a kind or a gas";
        if (amountPerRareTick <= 0f) yield return parentDef.defName + ": PhobosGasEmitter amountPerRareTick must be positive";
    }
}

public class CompPhobosGasEmitter : ThingComp
{
    CompProperties_PhobosGasEmitter Props => (CompProperties_PhobosGasEmitter)props;

    public override void CompTickRare()
    {
        if (!parent.Spawned || !Running()) return;
        if (Props.kind != GasKind.None) PhobosGases.Add(parent.Position, parent.Map, Props.kind, Props.amountPerRareTick);
        if (Props.gas != null && PhobosGases.Source(Props.gas) == Rules.GasSource.Own)
        {
            PhobosGases.AddOwn(parent.Position, parent.Map, Props.gas, Props.amountPerRareTick);
        }
    }

    bool Running()
    {
        if (Props.onlyWhenPowered && !(parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? false)) return false;
        if (Props.onlyWhenSwitchedOn && !FlickUtility.WantsToBeOn(parent)) return false;
        if (Props.onlyWhenFuelled && !(parent.TryGetComp<CompRefuelable>()?.HasFuel ?? false)) return false;
        return true;
    }
}
