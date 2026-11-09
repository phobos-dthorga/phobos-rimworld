using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// What the rest of the game asks about boats that are under, and the one place orders to a whole caravan go through. The set of hidden hulls is derived and never saved: each comp puts itself
/// back when it spawns. It is here so that the patches, some of which run for every pawn in the game, cost one empty check while no boat is under.
/// </summary>
public static class Submersibles
{
    const string DiveIcon = "Phobos/VehicleSystems/Dive";
    const string SurfaceIcon = "Phobos/VehicleSystems/Surface";
    const string StayDownIcon = "Phobos/VehicleSystems/StayDown";

    static readonly HashSet<VehiclePawn> hidden = new();
    static Game? owner;
    static int lastLandingRefusalTick = -1;

    public static bool AnyHidden => hidden.Count > 0;

    public static bool IsHidden(VehiclePawn vehicle) => hidden.Contains(vehicle);

    internal static void SetHidden(VehiclePawn vehicle, bool on)
    {
        // The set outlives a game; hulls of the last one must not linger in it.
        if (!ReferenceEquals(owner, Current.Game))
        {
            hidden.Clear();
            owner = Current.Game;
        }
        if (on) hidden.Add(vehicle);
        else hidden.Remove(vehicle);
    }

    public static CompPhobosSubmersible? CompOf(VehiclePawn vehicle) => vehicle.GetCachedComp<CompPhobosSubmersible>();

    /// <summary>A hull that is under keeps what cannot reach it from reaching it; true if the hit is wholly stopped.</summary>
    public static bool AbsorbHit(VehiclePawn vehicle, ref DamageInfo dinfo) => CompOf(vehicle)?.AbsorbBelow(ref dinfo) ?? false;

    /// <summary>Whether this searcher must leave a hull that is under alone. A hull with no comp is left alone, as it was when it went into the set.</summary>
    public static bool OutOfReach(VehiclePawn vehicle, IAttackTargetSearcher? searcher) => CompOf(vehicle)?.OutOfReachOf(searcher) ?? true;

    public static bool TurretHeld(VehicleTurret turret) => turret.vehicle != null && IsHidden(turret.vehicle) && (CompOf(turret.vehicle)?.HoldsBelow(turret) ?? false);

    /// <summary>Refuses to land anyone from a hull that is under, once however many are being landed.</summary>
    public static void RefuseLanding(VehiclePawn vehicle)
    {
        int now = Find.TickManager.TicksGame;
        if (now == lastLandingRefusalTick) return;
        lastLandingRefusalTick = now;
        Messages.Message("PhobosSubmersible_RefuseLanding".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), vehicle, MessageTypeDefOf.RejectInput, historical: false);
    }

    /// <summary>The boats of a caravan that can dive; empty if any vehicle of it cannot or anyone is walking, because a caravan goes under whole or not at all.</summary>
    public static List<CompPhobosSubmersible> DivingBoats(VehicleCaravan caravan)
    {
        List<VehiclePawn> vehicles = caravan.VehiclesListForReading;
        var boats = vehicles.Select(CompOf).Where(c => c != null).Select(c => c!).ToList();
        return boats.Count == vehicles.Count && caravan.PawnsListForReading.All(p => p is VehiclePawn) ? boats : new List<CompPhobosSubmersible>();
    }

    /// <summary>A caravan is out of reach only when all of it is under.</summary>
    public static bool CaravanHidden(VehicleCaravan caravan)
    {
        List<CompPhobosSubmersible> boats = DivingBoats(caravan);
        return boats.Count > 0 && boats.All(b => b.Hidden);
    }

    /// <summary>The whole caravan goes down or comes up: the first refusal stops all of it, and says why.</summary>
    public static void ToggleCaravan(VehicleCaravan caravan)
    {
        List<CompPhobosSubmersible> boats = DivingBoats(caravan);
        if (boats.Count == 0) return;
        if (boats.Any(b => DiveRule.GoingOrGone(b.Phase)))
        {
            string? stuck = boats.Select(b => b.SurfaceRefusal()).FirstOrDefault(r => r != null);
            if (stuck != null) Messages.Message(stuck, caravan, MessageTypeDefOf.RejectInput, historical: false);
            else foreach (CompPhobosSubmersible boat in boats) boat.Surface();
            return;
        }
        string? refused = boats.Select(b => b.DiveRefusal()).FirstOrDefault(r => r != null);
        if (refused != null)
        {
            Messages.Message(refused, caravan, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        foreach (CompPhobosSubmersible boat in boats) boat.TryDive();
    }

    /// <summary>One standing order for the whole caravan, so no boat of it comes up alone.</summary>
    public static void SetCaravanStayDown(VehicleCaravan caravan, bool on)
    {
        foreach (CompPhobosSubmersible boat in DivingBoats(caravan)) boat.SetStayDown(on);
    }

    /// <summary>The standing order to stay under once the air and charge run out. Presentation only: the action is what the caller says it is.</summary>
    public static Command_Toggle StayDownGizmo(bool on, string? refused, Action toggle)
    {
        var command = new Command_Toggle
        {
            defaultLabel = "PhobosSubmersible_GizmoStayDown".Translate().Resolve(),
            defaultDesc = "PhobosSubmersible_GizmoStayDownDesc".Translate().Resolve(),
            icon = SystemIcons.For(StayDownIcon),
            isActive = () => on,
            toggleAction = toggle,
        };
        if (refused != null) command.Disable(refused);
        return command;
    }

    /// <summary>The dive or surface button: one look for a boat and for a caravan. Presentation only: the action is what the caller says it is.</summary>
    public static Command_Action ToggleGizmo(bool down, string? refused, CompProperties_PhobosSubmersible props, Action action)
    {
        var command = new Command_Action
        {
            defaultLabel = (down ? "PhobosSubmersible_GizmoSurface".Translate() : "PhobosSubmersible_GizmoDive".Translate()).Resolve(),
            defaultDesc = down
                ? "PhobosSubmersible_GizmoSurfaceDesc".Translate(props.surfaceSeconds.ToString("0").Named("SECONDS")).Resolve()
                : "PhobosSubmersible_GizmoDiveDesc".Translate(props.diveSeconds.ToString("0").Named("SECONDS")).Resolve(),
            icon = SystemIcons.For(down ? SurfaceIcon : DiveIcon),
            action = action,
        };
        if (refused != null) command.Disable(refused);
        return command;
    }
}

/// <summary>
/// A hull that is under. Once spotted it shows as a dark shape in the water; until then a boat of ours shows only a faint mark at its middle, so the player can find it and click it, and anyone else's
/// shows nothing. The picture is the game's own shadow for a falling crate.
/// </summary>
[StaticConstructorOnStartup]
public static class SubmersibleDraw
{
    /// <summary>A shadow reads as a hull when it is a little wider than the footprint.</summary>
    const float Overhang = 1.15f;
    /// <summary>The own boat's mark: small and faint, so it reads as a periscope's ripple and never as the hull.</summary>
    const float MarkSize = 1.5f;
    const float MarkOpacity = 0.35f;
    const string ShadowTexture = "Things/Skyfaller/SkyfallerShadowRectangle";
    static readonly Material Shadow = MaterialPool.MatFrom(ShadowTexture, ShaderDatabase.Transparent);
    static readonly Material Mark = MaterialPool.MatFrom(ShadowTexture, ShaderDatabase.Transparent, new Color(1f, 1f, 1f, MarkOpacity));

    public static void Draw(VehiclePawn vehicle)
    {
        bool spotted = Submersibles.CompOf(vehicle)?.Tracked ?? false;
        if (!spotted && vehicle.Faction != Faction.OfPlayer) return;
        Vector3 at = ((Thing)vehicle).DrawPos;
        at.y = AltitudeLayer.Shadows.AltitudeFor();
        IntVec2 size = vehicle.def.size;
        Vector3 scale = spotted ? new Vector3(size.x * Overhang, 1f, size.z * Overhang) : new Vector3(MarkSize, 1f, MarkSize);
        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(at, Quaternion.AngleAxis(vehicle.FullRotation.AsAngle, Vector3.up), scale), spotted ? Shadow : Mark, 0);
    }
}

/// <summary>On every vehicle caravan: the reserve of any boats in it, the time passing for them, and the order to go under as one (added by an XML patch).</summary>
public class WorldObjectComp_PhobosSubmersibleCaravan : WorldObjectComp
{
    const int AdvanceInterval = GenTicks.TickRareInterval;

    VehicleCaravan? Caravan => parent as VehicleCaravan;

    public override void CompTickInterval(int delta)
    {
        base.CompTickInterval(delta);
        if (Caravan is not { } caravan || !parent.IsHashIntervalTick(AdvanceInterval, delta)) return;
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading) Submersibles.CompOf(vehicle)?.Advance();
    }

    public override string? CompInspectStringExtra()
    {
        if (Caravan is not { } caravan) return null;
        var lines = caravan.VehiclesListForReading.Select(v => (vehicle: v, comp: Submersibles.CompOf(v))).Where(p => p.comp != null)
            .Select(p => "PhobosSubmersible_CaravanLine".Translate(p.vehicle.LabelShort.Named("VEHICLE"), p.comp!.StateLine().Named("STATE")).Resolve()).ToList();
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
        if (Caravan is not { } caravan || caravan.Faction != Faction.OfPlayer) yield break;
        List<CompPhobosSubmersible> boats = Submersibles.DivingBoats(caravan);
        if (boats.Count == 0) yield break;
        bool down = boats.Any(b => DiveRule.GoingOrGone(b.Phase));
        yield return Submersibles.ToggleGizmo(down, boats.Select(b => down ? b.SurfaceRefusal() : b.DiveRefusal()).FirstOrDefault(r => r != null), boats[0].Props, () => Submersibles.ToggleCaravan(caravan));
        bool stayDown = boats.All(b => b.StayDown);
        yield return Submersibles.StayDownGizmo(stayDown, boats.Select(b => b.SurfaceRefusal()).FirstOrDefault(r => r != null), () => Submersibles.SetCaravanStayDown(caravan, !stayDown));
    }
}
