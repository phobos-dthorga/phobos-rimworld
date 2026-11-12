using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What hangs from helicopters (Framework 0.99.0). A slung vehicle is held inside its helicopter's sling, so anything that lists vehicles
/// by place (the world map's tabs, the fleet board) must ask here to see it: a list of vehicles, each followed by its slung load.
/// </summary>
public static class SlingLoads
{
    /// <summary>The vehicle hanging from this one's sling, or null.</summary>
    public static VehiclePawn? Of(VehiclePawn? carrier) => carrier?.GetComp<CompPhobosSling>()?.Carried;

    /// <summary>The same vehicles, each followed by the load on its sling.</summary>
    public static IEnumerable<VehiclePawn> WithLoads(IEnumerable<VehiclePawn> vehicles)
    {
        foreach (VehiclePawn vehicle in vehicles)
        {
            yield return vehicle;
            if (Of(vehicle) is { } load) yield return load;
        }
    }

    /// <summary>The vehicles a world object itself holds: an aircraft in flight, or a convoy's.</summary>
    public static IEnumerable<VehiclePawn> CarriersIn(WorldObject? place) => place switch
    {
        AerialVehicleInFlight aircraft when aircraft.Vehicle != null => new[] { aircraft.Vehicle },
        VehicleCaravan convoy => convoy.VehiclesListForReading,
        _ => Enumerable.Empty<VehiclePawn>(),
    };
}

/// <summary>
/// The slung load in a helicopter's take-off and landing (Framework 0.99.0; the owner's report of 12 Nov 2026 that a lifted vehicle
/// "disappears into the aether"). Vehicle Framework draws the flying helicopter from a stand-in on the map; this draws what it carries
/// below it, with two strops, each frame the stand-in is drawn. Presentation only: where the load is and what a lift does are the sling
/// comp's. SlingRule.Hang places it.
/// </summary>
public static class SlingDrawing
{
    /// <summary>How far below the helicopter's picture the load hangs, in cells (authored).</summary>
    public const float StropCells = 3f;
    /// <summary>How far along the load, from its middle, each strop is made fast, as a share of its length (authored).</summary>
    const float StropSpread = 0.3f;
    const float StropWidth = 0.09f;
    /// <summary>Strops are not drawn until the hook is this far from the load, in cells: nothing to see before that.</summary>
    const float StropsFromCells = 0.6f;
    const string GuardKey = "A slung load in flight";

    static Material? stropMaterial;
    static Material Strop => stropMaterial ??= SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.13f, 0.13f, 0.12f));

    // Vehicle Framework's build names Unity's types from another Unity version, so the compiler refuses its members that take or return a
    // Vector3, though at run time it is the one Vector3 (as OverlayPosition and VehiclePicture found). These two are called by name.
    static MethodInfo? drawAt;
    static MethodInfo? trueCentre;
    static bool resolved;
    static readonly object[] drawArguments = new object[3];

    static void Resolve()
    {
        resolved = true;
        drawAt = AccessTools.Method(typeof(VehiclePawn), "DrawAt", new[] { typeof(Vector3).MakeByRefType(), typeof(Rot8), typeof(float) });
        trueCentre = AccessTools.Method(typeof(VehiclePawn), "TrueCenter", new[] { typeof(IntVec3), typeof(float?) });
        if (drawAt == null) VehicleSystemsMod.Logger.Warn("A vehicle's drawing call could not be found, so a slung load is not drawn under its helicopter. The load itself is carried as before.");
    }

    /// <summary>Draws the load of the helicopter this stand-in flies, if it has one. Guarded: it runs every frame (UiGuard).</summary>
    public static void Draw(VehicleSkyfaller flight)
    {
        VehiclePawn? helicopter = flight.vehicle;
        CompPhobosSling? sling = helicopter?.GetComp<CompPhobosSling>();
        VehiclePawn? load = sling?.Carried;
        if (helicopter == null || sling == null || load == null || UiGuard.Resting(GuardKey)) return;
        try
        {
            if (!resolved) Resolve();
            if (drawAt == null) return;
            Vector3 hook = ((Thing)flight).DrawPos;
            if (sling.pictureFlight != flight.thingIDNumber)
            {
                // The spot on the ground this flight left or comes down to: found once per flight, as Vehicle Framework finds it.
                sling.pictureGround = GroundUnder(helicopter, flight, hook.y);
                sling.pictureFlight = flight.thingIDNumber;
            }
            Vector3 ground = sling.pictureGround;
            Vector3? stood = sling.LiftedFrom;
            (double x, double z, double paidOut) = SlingRule.Hang(hook.x - ground.x, hook.z - ground.z, StropCells,
                stood.HasValue ? stood.Value.x - ground.x : null, stood.HasValue ? stood.Value.z - ground.z : null);

            // One layer below the helicopter, so its body and rotors cover the load as they would from above.
            var at = new Vector3(ground.x + (float)x, hook.y - Altitudes.AltInc, ground.z + (float)z);
            drawArguments[0] = at;
            drawArguments[1] = load.FullRotation;
            drawArguments[2] = 0f;
            drawAt.Invoke(load, drawArguments);

            if (paidOut <= 0 || (hook - at).MagnitudeHorizontal() < StropsFromCells) return;
            float between = hook.y - Altitudes.AltInc / 2f;
            Vector3 along = load.Rotation.FacingCell.ToVector3() * (load.VehicleDef.Size.z * StropSpread);
            var from = new Vector3(hook.x, between, hook.z);
            var middle = new Vector3(at.x, between, at.z);
            GenDraw.DrawLineBetween(from, middle + along, Strop, StropWidth);
            GenDraw.DrawLineBetween(from, middle - along, Strop, StropWidth);
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); }
    }

    static Vector3 GroundUnder(VehiclePawn helicopter, VehicleSkyfaller flight, float altitude)
    {
        if (trueCentre?.Invoke(helicopter, new object?[] { flight.Position, (float?)altitude }) is Vector3 found) return found;
        return GenThing.TrueCenter(flight.Position, flight.Rotation, helicopter.VehicleDef.Size, altitude);
    }
}

/// <summary>
/// The line on the world map that says what an aircraft in flight, or a convoy's helicopter, carries on its sling (Framework 0.99.0).
/// Presentation only. Added to Vehicle Framework's two world objects by patch.
/// </summary>
public class WorldObjectComp_PhobosSling : WorldObjectComp
{
    const string GuardKey = "A slung load's line on the world map";

    public override string? CompInspectStringExtra()
    {
        // Guarded: the game asks for this every frame while the thing is selected (UiGuard).
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try { return InspectLine(); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    /// <summary>Emergency release over the world map (sling slice 2): on our helicopter in flight carrying a load. It asks first, since the
    /// load falls and becomes a wreck; a disabled button says why.</summary>
    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
        if (parent is not AerialVehicleInFlight aircraft || aircraft.Vehicle is not { } helicopter || helicopter.Faction != Faction.OfPlayer) yield break;
        if (helicopter.GetComp<CompPhobosSling>() is not { Carried: { } load } sling) yield break;
        var release = new Command_Action
        {
            defaultLabel = "PhobosSling_ReleaseGizmo".Translate(),
            defaultDesc = "PhobosSling_ReleaseGizmoDesc".Translate(load.LabelShort.Named("LOAD")),
            icon = SystemIcons.For("Phobos/VehicleSystems/Sling/Release"),
            action = () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "PhobosSling_ReleaseConfirm".Translate(load.LabelShort.Named("LOAD"), load.AllPawnsAboard.Count().Named("PEOPLE")),
                () => sling.ReleaseInFlight(PhobosSlingWreck.TileUnder(aircraft.Tile, ((WorldObject)aircraft).DrawPos)), destructive: true)),
        };
        if (sling.ReleaseRefusal() is { } refusal) release.Disable(refusal);
        yield return release;
    }

    string? InspectLine()
    {
        StringBuilder? lines = null;
        foreach (VehiclePawn carrier in SlingLoads.CarriersIn(parent))
        {
            if (SlingLoads.Of(carrier) is not { } load) continue;
            lines ??= new StringBuilder();
            if (lines.Length > 0) lines.AppendLine();
            lines.Append("PhobosSling_WorldCarrying".Translate(carrier.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD"),
                CompPhobosSling.LoadKilograms(load).ToString("N0").Named("WEIGHT")).Resolve());
            if (parent is AerialVehicleInFlight && carrier.GetComp<CompPhobosSling>() is { SlungFlightNow: true })
            {
                (double speed, double fuelPerTick) = SlungFlight.Factors;
                lines.AppendLine();
                lines.Append("PhobosSling_WorldSlowFlight".Translate(((float)speed).ToStringPercent().Named("SPEED"),
                    ((float)(fuelPerTick / speed)).ToString("0.##").Named("FUEL")).Resolve());
            }
        }
        return lines?.ToString();
    }
}
