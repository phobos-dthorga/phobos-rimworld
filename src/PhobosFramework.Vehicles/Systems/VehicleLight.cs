using System.Collections.Generic;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// One light a vehicle casts on the map (Framework 0.104.0): the one place our vehicles register, move and clear light, used by the work
/// lamps and the headlights. It goes through the game's own light grid as a custom glower: a pool of light round one cell, with no thing
/// behind it, so nothing is added to a vehicle's Def and nothing is saved. The handle lives only as long as the map's light grid does; a
/// loaded game starts with every light out, and the owners light them again from their own state.
/// <para>Framework 0.120.1 (the owner's report of 17 Nov 2026: a white glare over the market that never went out): every lit light is
/// kept in a list with the vehicle it belongs to, so a light whose vehicle has gone can be found and put out (<see cref="PutOutOrphans"/>),
/// and a dev action can say what each light is. A custom glower has no thing behind it, so one left behind burns until the game is loaded
/// again, and the game cannot say whose it was.</para>
/// </summary>
public sealed class VehicleLight
{
    /// <summary>Every light of ours lit now, on any map. Derived, never saved.</summary>
    static readonly List<VehicleLight> lit = new();

    public static IReadOnlyList<VehicleLight> AllLit => lit;

    readonly string kind;
    Map? map;
    int handle;
    IntVec3 cell = IntVec3.Invalid;
    VehiclePawn? owner;
    float radius;

    /// <param name="kind">What the light is, for the dev action and the log: "headlights", "work lamps".</param>
    public VehicleLight(string kind = "light") => this.kind = kind;

    /// <summary>Whether the light is on a map now.</summary>
    public bool Lit => map != null;

    /// <summary>The cell the light is centred on, or Invalid when out.</summary>
    public IntVec3 Cell => cell;

    public Map? Map => map;
    public VehiclePawn? Owner => owner;
    public string Kind => kind;
    public float Radius => radius;
    public int Handle => handle;

    /// <summary>Whether the vehicle this light belongs to no longer stands on the light's map: destroyed, gone, or on another map.</summary>
    public bool Orphaned => map != null && (owner == null || owner.Destroyed || !owner.Spawned || owner.Map != map);

    /// <summary>Lights the pool at <paramref name="at"/>, moving it there if it was elsewhere. Does nothing if it is already there; a cell
    /// off the map puts it out.</summary>
    public void Place(VehiclePawn by, Map on, IntVec3 at, float glowRadius, ColorInt colour)
    {
        if (map == on && cell == at) return;
        PutOut();
        if (on == null || on.Disposed || !at.InBounds(on)) return;
        handle = on.glowGrid.RegisterCustomGlower(at, glowRadius, colour);
        map = on;
        cell = at;
        owner = by;
        radius = glowRadius;
        lit.Add(this);
    }

    /// <summary>Takes the light off its map, if it is on one.</summary>
    public void PutOut()
    {
        if (map == null) return;
        if (!map.Disposed) map.glowGrid.DeregisterCustomGlower(handle);
        map = null;
        cell = IntVec3.Invalid;
        owner = null;
        lit.Remove(this);
    }

    /// <summary>
    /// Puts out every light of ours whose vehicle no longer stands on its map, and says which in the log, so the way it was left behind can
    /// be found. A safety net: every known way for a vehicle to leave already puts its lights out. Returns how many it put out.
    /// </summary>
    public static int PutOutOrphans()
    {
        int count = 0;
        for (int i = lit.Count - 1; i >= 0; i--)
        {
            VehicleLight light = lit[i];
            if (light.map is { Disposed: true })
            {
                // The map has gone and its light grid with it: only our record is left.
                light.map = null;
                light.owner = null;
                lit.RemoveAt(i);
                continue;
            }
            if (!light.Orphaned) continue;
            VehiclePawn? was = light.owner;
            string who = was == null ? "no vehicle" : was.LabelShort + " (" + was.ThingID + ", " + (was.Faction?.Name ?? "no faction") + ", "
                + (was.Destroyed ? "destroyed" : !was.Spawned ? "not on a map" : "on another map") + ")";
            VehicleSystemsMod.Logger.Error("a vehicle's " + light.kind + " were left lit at " + light.cell + " after it had gone: " + who
                + ". Put out now. Please report this line: it says a way of leaving the map that does not put the lights out.");
            light.PutOut();
            count++;
        }
        return count;
    }

    /// <summary>Forgets the lights of maps that are no longer in play (a game loaded over another in one session): their light grids went
    /// with them. Lights on the maps now in play are kept, in case one was lit while the game loaded.</summary>
    public static void ForgetGoneMaps()
    {
        for (int i = lit.Count - 1; i >= 0; i--)
        {
            Map? on = lit[i].map;
            if (on != null && !on.Disposed && Find.Maps.Contains(on)) continue;
            lit[i].map = null;
            lit[i].owner = null;
            lit.RemoveAt(i);
        }
    }
}

/// <summary>
/// Looks for lights of ours left behind by a vehicle that has gone, every few seconds, and puts them out (Framework 0.120.1). A handful of
/// list entries to read; nothing while no vehicle light is lit. Saves nothing.
/// </summary>
public class GameComponent_PhobosVehicleLights : GameComponent
{
    const int Interval = GenTicks.TickRareInterval;

    public GameComponent_PhobosVehicleLights(Game game)
    {
    }

    public override void StartedNewGame() => VehicleLight.ForgetGoneMaps();

    public override void LoadedGame() => VehicleLight.ForgetGoneMaps();

    public override void GameComponentTick()
    {
        if (VehicleLight.AllLit.Count == 0 || Find.TickManager.TicksGame % Interval != 0) return;
        VehicleLight.PutOutOrphans();
    }
}
