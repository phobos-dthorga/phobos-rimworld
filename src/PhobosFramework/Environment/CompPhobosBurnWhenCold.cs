using RimWorld;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>A fuelled heater that burns only while it is colder than this where it stands (Framework 0.122.0; owner, 17 Nov 2026: Billy's tent heater).</summary>
public class CompProperties_PhobosBurnWhenCold : CompProperties
{
    /// <summary>It burns below this temperature, in degrees Celsius (authored: Billy's tent heater keeps its tent at about 15 °C).</summary>
    public float belowCelsius = 15f;

    public CompProperties_PhobosBurnWhenCold() => compClass = typeof(CompPhobosBurnWhenCold);
}

/// <summary>
/// Burns a heater's fuel only while it is cold where it stands. The heater's refuelling is set to use fuel only when used (the game's
/// consumeFuelOnlyWhenUsed), and this comp marks it used each tick it is switched on, fuelled and below the threshold; the game's heat pusher
/// stops warming at its own ceiling. So a heater lit in summer burns nothing. Generic: any fuelled heater can carry it.
/// </summary>
public class CompPhobosBurnWhenCold : ThingComp
{
    CompRefuelable? fuel;

    public CompProperties_PhobosBurnWhenCold Props => (CompProperties_PhobosBurnWhenCold)props;

    /// <summary>Whether it is burning now: switched on, fuelled and cold.</summary>
    public bool Burning => parent.Spawned && fuel is { HasFuel: true } && FlickUtility.WantsToBeOn(parent) && parent.AmbientTemperature < Props.belowCelsius;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        fuel = parent.TryGetComp<CompRefuelable>();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (Burning) fuel!.Notify_UsedThisTick();
    }

    public override string? CompInspectStringExtra() =>
        parent.Spawned && fuel is { HasFuel: true } && !Burning && FlickUtility.WantsToBeOn(parent)
            ? "PhobosBurnWhenCold_Idle".Translate(Props.belowCelsius.ToStringTemperature("F0").Named("TEMPERATURE")).Resolve()
            : null;
}
