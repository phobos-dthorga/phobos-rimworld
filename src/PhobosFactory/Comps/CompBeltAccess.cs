using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Factory;

/// <summary>Gives a storage building the "Belts may take from here" switch. Added in code, see <see cref="BeltAccessSetup"/>.</summary>
public class CompProperties_BeltAccess : CompProperties
{
    public CompProperties_BeltAccess()
    {
        compClass = typeof(CompBeltAccess);
    }
}

/// <summary>
/// Whether VFE Factory's belts and factory machines may take from this storage. Off by default, so a belt
/// beside a shelf never empties it until the player says so. The switch is the only saved state;
/// <see cref="BeltAccess"/> decides what it means.
/// </summary>
public class CompBeltAccess : ThingComp
{
    /// <summary>Saved key; never rename once saves can hold it.</summary>
    public const string SaveKey = "PhobosFactory_BeltsMayTake";

    private static Texture2D? icon;

    private bool beltsMayTake;

    public bool BeltsMayTake
    {
        get => beltsMayTake;
        internal set => beltsMayTake = value;
    }

    /// <summary>VFE Factory's own belt icon, read from its installed Def at runtime (nothing is copied).</summary>
    private static Texture2D Icon => icon ??= DefDatabase<ThingDef>.GetNamedSilentFail("VFEFactory_Conveyor")?.uiIcon ?? BaseContent.BadTex;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (parent.Faction != Faction.OfPlayer) yield break;
        yield return new Command_Toggle
        {
            defaultLabel = "PhobosFactory_BeltsMayTake".Translate(),
            defaultDesc = "PhobosFactory_BeltsMayTakeDesc".Translate(),
            icon = Icon,
            isActive = () => beltsMayTake,
            toggleAction = () => BeltAccess.SetBeltsMayTake(this, !beltsMayTake),
        };
    }

    public override string? CompInspectStringExtra()
    {
        if (!beltsMayTake) return null;
        return "PhobosFactory_BeltsMayTakeInspect".Translate();
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref beltsMayTake, SaveKey, false);
    }
}
