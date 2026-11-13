using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A trailer (Framework 0.101.0, the hitch's slice 2; docs/development/vehicle-hitch-design.md): a vehicle with no engine and no seats that a
/// vehicle with a hitch can tow. Its two lengths set how it follows: from the hitch to its axle, and from the axle to the middle of its
/// picture. Named in a trailer's own Def, so other modders can make trailers in XML.
/// </summary>
public sealed class CompProperties_PhobosTrailer : CompProperties
{
    /// <summary>From the hitch to the trailer's axle, in cells (the drawbar and the front of the deck).</summary>
    public float hitchToAxle = 3.2f;
    /// <summary>From the axle forward to the middle of the trailer's picture, in cells.</summary>
    public float axleToCentre = 0.2f;
    /// <summary>The most the trailer may stand turned from the vehicle towing it, in degrees (authored, not a force).</summary>
    public float jackKnifeDegrees = 70f;
    /// <summary>The trailer's part whose loss leaves nothing to hitch to; empty for none.</summary>
    public string drawbarPart = "Drawbar";
    /// <summary>A working trailer is set up by a colonist, not by a crew it does not have (Framework 0.111.0; docs/development/vehicle-trailers.md, B0.1):
    /// the work to set it up and to pack it up, at a colonist's construction speed (authored).</summary>
    public float setUpWork = 1500f;
    public float packUpWork = 1000f;

    public CompProperties_PhobosTrailer() => compClass = typeof(CompPhobosTrailer);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (hitchToAxle <= 0f) yield return "hitchToAxle must be above 0";
        if (jackKnifeDegrees <= 0f || jackKnifeDegrees > 180f) yield return "jackKnifeDegrees must be between 0 and 180";
        if (setUpWork <= 0f || packUpWork <= 0f) yield return "setUpWork and packUpWork must be above 0";
    }
}

/// <summary>
/// The trailer's side of a hitch: what it says, and the unhitch button. The vehicle towing it holds the hitch (CompPhobosHitch). Since Framework 0.111.0 it
/// also holds the trailer's own pipes beneath it (a trailer has none of our vehicle systems to hold them): a water bowser parked on its own lays plumbing
/// across its footprint as any vehicle of ours does, and takes it up when it is hitched (owner's rule: anything that takes water or piping has its own pipes
/// beneath it).
/// </summary>
public sealed class CompPhobosTrailer : VehicleComp, VehiclePipes.IHost
{
    /// <summary>How often, in rare ticks, a parked trailer looks at its pipes, as our vehicles do (about every half an hour).</summary>
    const int RareTicksPerPipeLook = 8;

    readonly VehiclePipes.Store pipes = new();
    int pipeLooks;

    public CompProperties_PhobosTrailer Props => (CompProperties_PhobosTrailer)props;

    /// <summary>The vehicle towing this trailer now, or null.</summary>
    public CompPhobosHitch? TowedBy => Hitches.Towing(Vehicle);

    /// <summary>Whether the trailer's drawbar is gone, so there is nothing to hitch to.</summary>
    public bool DrawbarBroken =>
        !Props.drawbarPart.NullOrEmpty() && Vehicle.statHandler?.GetComponent(Props.drawbarPart) is { } part && part.HealthPercent <= 0f;

    // ---- Its own pipes (VehiclePipes.IHost) ----

    public Phobos.Framework.Persistence.PipeUnderlay Underlay(string key) => pipes.Get(key);

    public List<KeyValuePair<string, Phobos.Framework.Persistence.PipeUnderlay>> UnderlaysStartingWith(string prefix) => pipes.StartingWith(prefix);

    /// <summary>The player's, on a map, not towed: a trailer has no engine, so it is parked whenever nothing pulls it.</summary>
    public bool ParkedForPipes => Phobos.Framework.Rules.UnderlayRule.Lays(Vehicle.Spawned, Vehicle.Faction == Faction.OfPlayer, TowedBy != null, false, false);

    /// <summary>A trailer has no fuel tank of its own for a line to fill.</summary>
    public string? PipeFuelLine { get; set; }

    /// <summary>Lays or takes up its pipes now: hitched, unhitched, set up or a load chosen.</summary>
    public void LookAtPipes()
    {
        pipeLooks = 0;
        VehiclePipes.Update(this);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (++pipeLooks < RareTicksPerPipeLook) return;
        pipeLooks = 0;
        VehiclePipes.Update(this);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        // Never from here directly (AGENTS.md: nothing is spawned from PostSpawnSetup): once every thing stands.
        if (respawningAfterLoad) LongEventHandler.ExecuteWhenFinished(SettlePipesAfterLoad);
    }

    void SettlePipesAfterLoad()
    {
        if (!Vehicle.Spawned) return;
        VehiclePipes.AfterLoad(this);
        VehiclePipes.Update(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (mode != DestroyMode.WillReplace) VehiclePipes.Leave(this);
        base.PostDeSpawn(map, mode);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        pipes.Expose("PhobosTrailer_PipeUnderlays");
    }

    // ---- What the player sees ----

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || TowedBy is not { } hitch) yield break;
        yield return hitch.UnhitchGizmo();
    }

    public override string? CompInspectStringExtra()
    {
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            if (TowedBy is { } hitch) return "PhobosHitch_InspectTowedBy".Translate(hitch.Vehicle.LabelShort.Named("TRACTOR")).Resolve();
            if (DrawbarBroken) return "PhobosHitch_InspectDrawbarBroken".Translate().Resolve();
            return null;
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A trailer's line";
}
