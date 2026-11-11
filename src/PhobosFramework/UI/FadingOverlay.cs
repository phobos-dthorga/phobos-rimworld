using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Verse;

// Not "Phobos.Framework.UI": a namespace named UI would hide the game's Verse.UI class from every Phobos.Framework.* namespace.
namespace Phobos.Framework.Interface;

/// <summary>
/// A picture over a building that builds and fades with its state, such as frost on a cold store (owner, 12 Nov 2026). It is one
/// long-lived mote of the given Def, made more or less see-through through the colour the game multiplies a mote by, so it needs no
/// realtime drawing on the building and makes no new material. Motes are never saved, so after a load the owner calls
/// <see cref="Update"/> with <c>snap</c> and the picture starts at the strength the building's state calls for. The mote Def should be a
/// <c>MoteBase</c> child with a long solid time and no fades of its own. Presentation only: nothing here changes play.
/// </summary>
public sealed class FadingOverlay
{
    readonly ThingDef moteDef;
    Mote? mote;
    float level;

    public FadingOverlay(ThingDef moteDef) => this.moteDef = moteDef;

    public float Level => level;

    /// <summary>Moves the strength by the ticks passed (<see cref="FadeRule"/>), or jumps there with <paramref name="snap"/>, and shows it.</summary>
    public void Update(Thing thing, bool wanted, float ticks, float buildTicks, float fadeTicks, bool snap = false)
    {
        level = snap ? (wanted ? 1f : 0f) : FadeRule.Step(level, wanted, ticks, buildTicks, fadeTicks);
        if (level <= 0f || !thing.Spawned)
        {
            End();
            return;
        }
        if (mote == null || mote.Destroyed)
        {
            // Made even off screen, so a store out of view still wears its frost when the camera arrives; a full mote counter
            // gives nothing back, and the next update tries again.
            mote = MoteMaker.MakeStaticMote(thing.DrawPos, thing.Map, moteDef, 1f, makeOffscreen: true);
            if (mote == null) return;
        }
        mote.instanceColor = new Color(1f, 1f, 1f, level);
    }

    public void End()
    {
        if (mote is { Destroyed: false }) mote.Destroy();
        mote = null;
    }
}
