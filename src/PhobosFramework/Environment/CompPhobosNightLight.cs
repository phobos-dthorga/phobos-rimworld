using System;
using RimWorld;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>
/// A light that lights only in the dark (Framework 0.231.0; docs/development/vehicle-accessories.md, "Built: camp lights"): a camp lantern,
/// a camp light on a pole. Beside the game's own glower comp on the same thing, it tells the glower to stay dark while the sky is light. A
/// lantern also burns its fuel only while lit (its refuelling set to use fuel only when used) and warms the air a little; a light fed from
/// elsewhere (<see cref="supplied"/>) asks whoever feeds it, so a camp light on a pole goes dark when its vehicle's battery gives out.
/// Generic: any lamp can carry it.
/// </summary>
public class CompProperties_PhobosNightLight : CompProperties
{
    /// <summary>It lights below this much daylight (the game's sky glow, 0 to 1; authored: 0.5, dusk).</summary>
    public float darkBelow = 0.5f;
    /// <summary>A flame's warmth while lit, as the game's heat pushers give it (authored; 0 for none).</summary>
    public float heatPerSecond;
    /// <summary>The warmth stops at this temperature (degrees C).</summary>
    public float heatPushMaxTemperature = 20f;
    /// <summary>Fed from elsewhere: lit only while whoever feeds it says so (a vehicle's battery).</summary>
    public bool supplied;
    /// <summary>What it draws while lit, in watts, for whoever feeds it (authored).</summary>
    public float watts;

    public CompProperties_PhobosNightLight() => compClass = typeof(CompPhobosNightLight);
}

public class CompPhobosNightLight : ThingComp, IThingGlower
{
    /// <summary>
    /// Whoever feeds supplied lights: given a light, whether it has power now. Registered by the mod that feeds them (D'thorga Framework's
    /// vehicles, for camp lights on a vehicle's battery); with none registered, a supplied light stays dark.
    /// </summary>
    public static Func<Thing, bool>? Supply;

    /// <summary>
    /// Why a supplied light is dark when its feeder knows (Framework 0.276.3: camp lights kept off so the vehicle can still start its engine),
    /// in the player's words, or null for the general "no power" line. Registered with <see cref="Supply"/>.
    /// </summary>
    public static Func<Thing, string?>? DarkWhy;

    const float RareTickHeatFactor = 4.1666665f;

    CompRefuelable? fuel;
    CompGlower? glower;

    public CompProperties_PhobosNightLight Props => (CompProperties_PhobosNightLight)props;

    /// <summary>Whether it would light now if it had what it needs: placed, switched on and dark.</summary>
    public bool Wants => parent.Spawned && FlickUtility.WantsToBeOn(parent) && parent.Map.skyManager.CurSkyGlow < Props.darkBelow;

    /// <summary>Whether it is lit now.</summary>
    public bool Lit => glower is { Glows: true };

    public bool ShouldBeLitNow() => Wants && (!Props.supplied || Supply?.Invoke(parent) == true);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        fuel = parent.TryGetComp<CompRefuelable>();
        glower = parent.TryGetComp<CompGlower>();
    }

    /// <summary>Every tick (the thing ticks normally, so its fuel counts by the tick): a lit flame uses its fuel; every 250 ticks the rest.</summary>
    public override void CompTick()
    {
        base.CompTick();
        if (fuel != null && Lit) fuel.Notify_UsedThisTick();
        if (parent.IsHashIntervalTick(GenTicks.TickRareInterval)) Look();
    }

    void Look()
    {
        if (!parent.Spawned) return;
        glower?.UpdateLit(parent.Map);
        if (Lit && fuel != null && Seen.Wants("framework.night-light-flame", parent)) Seen.Note("framework.night-light-flame", parent, fuel.Fuel.ToString("0.0") + " fuel");
        if (Lit && Props.heatPerSecond > 0f && parent.AmbientTemperature < Props.heatPushMaxTemperature)
        {
            GenTemperature.PushHeat(parent.PositionHeld, parent.MapHeld, Props.heatPerSecond * RareTickHeatFactor);
        }
    }

    public override string? CompInspectStringExtra()
    {
        if (!parent.Spawned || !FlickUtility.WantsToBeOn(parent)) return null;
        if (!Wants) return "PhobosNightLight_Daylight".Translate().Resolve();
        if (Props.supplied && !Lit) return DarkWhy?.Invoke(parent) ?? "PhobosNightLight_NoPower".Translate().Resolve();
        return null;
    }
}
