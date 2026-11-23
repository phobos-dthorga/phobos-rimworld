using System.Collections.Generic;
using Phobos.Framework.Interface;
using Phobos.Framework.Vehicles.Rules;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The Systems tab's cards for the electrical loads, the weapons, the running gear and the crew (Framework 0.117.0; owner, 17 Nov 2026:
/// "Shouldn't much of the newer systems show in, well, Systems?"). Until now these showed only as lines in the inspect pane. Each card
/// reads the services that decide (CompPhobosVehicleSystems.StateOf, TurretPace, TurretPower, Mobility, CrewBailOut) and decides nothing.
/// Under the "needs attention" filter, only what is wrong is drawn. Presentation only.
/// </summary>
public partial class ITab_VehicleSystems
{
    /// <summary>The four cards, drawn after the systems' own and before the drivetrain's.</summary>
    static void DrawKnockOutCards(CompPhobosVehicleSystems comp, ref float y, float width, bool engineer, ref bool anything, ref VehicleComponent? hovered)
    {
        DrawCrew(comp, ref y, width, ref anything);
        DrawWeapons(comp, ref y, width, ref anything, ref hovered);
        DrawRunningGear(comp, ref y, width, ref anything, ref hovered);
        DrawLoads(comp, ref y, width, engineer, ref anything);
    }

    // ---- Crew ----

    /// <summary>A crew in danger, as one red card at the top: the alert's words, here too.</summary>
    static void DrawCrew(CompPhobosVehicleSystems comp, ref float y, float width, ref bool anything)
    {
        CrewDanger danger = CrewBailOut.Danger(comp);
        if (danger == CrewDanger.None) return;
        anything = true;
        float top = y;
        PhobosWidgets.CardHeader(ref y, 0f, width, "PhobosVehicleSystems_SysCrew".Translate(), PhobosWidgets.Bad);
        if (Seen.Wants("vehicles.systems-tab.crew-card", comp.Vehicle)) Seen.Note("vehicles.systems-tab.crew-card", comp.Vehicle, danger.ToString());
        string line = (danger == CrewDanger.Burning ? "PhobosVehicleSystems_SysCrewBurning" : "PhobosVehicleSystems_SysCrewKnockedOut").Translate().Resolve();
        y += PhobosWidgets.WrappedLine(6f, y + 3f, width - 12f, line.Colorize(PhobosWidgets.Bad)) + 6f;
        PhobosWidgets.CardEnd(top, ref y, 0f, width);
    }

    // ---- Weapons ----

    static readonly List<VehicleTurret> weaponScratch = new();

    /// <summary>
    /// Each weapon as a row: its name; the part it stops with as a chip (hovering lights it on the vehicle); whether it is ready, and if
    /// not why; how fast it works now and why it is slow; and how it is turned.
    /// </summary>
    static void DrawWeapons(CompPhobosVehicleSystems comp, ref float y, float width, ref bool anything, ref VehicleComponent? hovered)
    {
        var turrets = comp.Vehicle.CompVehicleTurrets?.Turrets;
        if (turrets == null || turrets.Count == 0) return;
        weaponScratch.Clear();
        bool anyOut = false, anySlow = false;
        foreach (VehicleTurret turret in turrets)
        {
            if (turret.def == null) continue;
            bool out_ = Disabled(turret, out _);
            TurretPace.Pace pace = TurretPace.For(turret);
            bool slow = pace.damaged || pace.gunnerHurt || pace.handCranked;
            VehicleComponent? mount = TurretPace.Mount(turret);
            bool hurt = mount != null && mount.HealthPercent < WholeShare;
            if (attentionOnly && !out_ && !slow && !hurt) continue;
            anyOut |= out_;
            anySlow |= slow || hurt;
            weaponScratch.Add(turret);
        }
        if (weaponScratch.Count == 0) return;
        anything = true;
        float top = y;
        Seen.Note("vehicles.systems-tab.weapons-card", comp.Vehicle);
        PhobosWidgets.CardHeader(ref y, 0f, width, "PhobosVehicleSystems_SysWeapons".Translate(),
            anyOut ? PhobosWidgets.Bad : anySlow ? PhobosWidgets.Caution : PhobosWidgets.Good);
        foreach (VehicleTurret turret in weaponScratch) DrawWeapon(comp, turret, ref y, width, ref hovered);
        PhobosWidgets.CardEnd(top, ref y, 0f, width);
    }

    static void DrawWeapon(CompPhobosVehicleSystems comp, VehicleTurret turret, ref float y, float width, ref VehicleComponent? hovered)
    {
        string name = turret.gizmoLabel.NullOrEmpty() ? turret.def.LabelCap.Resolve() : turret.gizmoLabel;
        VehicleComponent? mount = TurretPace.Mount(turret);
        float nameEnd = width * 0.38f;
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft))
        {
            Widgets.Label(new Rect(6f, y, nameEnd - 10f, Row), name.Truncate(nameEnd - 10f));
        }
        if (mount != null) PartChip(comp, mount, new Rect(nameEnd, y + 2f, width - nameEnd - 6f, ChipHeight), ref hovered);
        y += Row;

        // What it is doing: out and why, else unmanned, else ready; then its pace and drive.
        string status;
        Color colour;
        if (Disabled(turret, out string? reason))
        {
            status = reason.NullOrEmpty() ? "PhobosVehicleSystems_SysWeaponOut".Translate().Resolve()
                : "PhobosVehicleSystems_SysWeaponOutWhy".Translate(reason!.Named("WHY")).Resolve();
            colour = PhobosWidgets.Bad;
        }
        else if (!turret.IsManned)
        {
            status = "PhobosVehicleSystems_SysWeaponUnmanned".Translate().Resolve();
            colour = DetailColour;
        }
        else
        {
            status = "PhobosVehicleSystems_SysWeaponReady".Translate().Resolve();
            colour = PhobosWidgets.Good;
        }
        var parts = new List<string> { status.Colorize(colour) };
        TurretPace.Pace pace = TurretPace.For(turret);
        if (pace.damaged || pace.gunnerHurt || pace.handCranked)
        {
            var why = new List<string>();
            if (pace.handCranked) why.Add("PhobosVehicleSystems_WeaponHandCranked".Translate().Resolve());
            if (pace.damaged && mount != null) why.Add("PhobosVehicleSystems_WeaponMountDamaged".Translate((mount.props.label ?? mount.props.key).Named("PART")).Resolve());
            if (pace.gunnerHurt) why.Add("PhobosVehicleSystems_WeaponGunnerHurt".Translate().Resolve());
            parts.Add("PhobosVehicleSystems_SysWeaponPace".Translate(Mathf.Min(pace.traverse, Mathf.Min(pace.aim, pace.reload)).ToStringPercent().Named("PACE"),
                string.Join(", ", why).Named("WHY")).Resolve().Colorize(PhobosWidgets.Caution));
        }
        WeaponDrive driveKind = TurretPower.DriveOf(comp, turret);
        if (driveKind == WeaponDrive.HandSwung) Seen.Note("vehicles.systems-tab.hand-swung-weapon", comp.Vehicle, turret.key);
        else if (driveKind == WeaponDrive.TurnsWithTurret) Seen.Note("vehicles.systems-tab.weapon-with-turret", comp.Vehicle, turret.key);
        string? drive = driveKind switch
        {
            WeaponDrive.HandSwung => "PhobosVehicleSystems_SysDriveHandSwung".Translate().Resolve(),
            WeaponDrive.Powered => "PhobosVehicleSystems_SysDrivePowered".Translate().Resolve(),
            // Cranked by hand is already the reason in the pace just before it.
            WeaponDrive.HandCranked => null,
            WeaponDrive.TurnsWithTurret => "PhobosVehicleSystems_SysDriveWithTurret".Translate(ParentName(turret.attachedTo).Named("TURRET")).Resolve(),
            _ => null,
        };
        if (drive != null) parts.Add(drive.Colorize(DetailColour));
        y += PhobosWidgets.WrappedLine(ArrowSize + 8f, y, width - ArrowSize - 14f, string.Join(" · ", parts)) + 4f;
    }

    static string ParentName(VehicleTurret? parent) =>
        parent == null ? "" : parent.gizmoLabel.NullOrEmpty() ? parent.def?.LabelCap.Resolve() ?? parent.key : parent.gizmoLabel;

    /// <summary>Vehicle Framework's own answer to whether a weapon is out of action, and why. Its check throws on a state it does not
    /// expect (a disabled part it cannot name), so that reads as out with no reason.</summary>
    static bool Disabled(VehicleTurret turret, out string? reason)
    {
        // Framework 0.120.5 (fault-read K7): Vehicle Framework's walk up a weapon's mounts steps by the first weapon's own parent each time,
        // so for a weapon three deep whose mount is out it never ends and the game hangs. None on the owner's list is that deep; such a
        // weapon is shown as out with no reason rather than asked.
        if (turret.attachedTo?.attachedTo != null && turret.ComponentDisabled)
        {
            reason = null;
            return true;
        }
        try
        {
            return turret.IsDisabled(out reason);
        }
        catch (System.InvalidOperationException)
        {
            reason = null;
            return true;
        }
    }

    // ---- Running gear ----

    static readonly List<VehicleComponent> trackScratch = new();

    /// <summary>The tracks (where the vehicle has them) as chips, a thrown one in red, and why the vehicle cannot move when it cannot.</summary>
    static void DrawRunningGear(CompPhobosVehicleSystems comp, ref float y, float width, ref bool anything, ref VehicleComponent? hovered)
    {
        string? cannotMove = Mobility.Line(comp.Vehicle, comp.Props);
        trackScratch.Clear();
        if (comp.Vehicle.statHandler != null)
        {
            foreach (string key in comp.Props.trackKeys)
            {
                VehicleComponent? track = comp.Vehicle.statHandler.GetComponent(key);
                if (track == null || (attentionOnly && track.HealthPercent >= WholeShare)) continue;
                trackScratch.Add(track);
            }
        }
        if (cannotMove == null && trackScratch.Count == 0) return;
        anything = true;
        VehicleComponent? thrown = Mobility.ThrownTrack(comp.Vehicle, comp.Props);
        float top = y;
        Seen.Note("vehicles.systems-tab.running-gear-card", comp.Vehicle);
        PhobosWidgets.CardHeader(ref y, 0f, width, "PhobosVehicleSystems_SysRunningGear".Translate(),
            cannotMove != null || thrown != null ? PhobosWidgets.Bad : trackScratch.Exists(t => t.HealthPercent < WholeShare) ? PhobosWidgets.Caution : PhobosWidgets.Good);
        if (cannotMove != null) y += PhobosWidgets.WrappedLine(6f, y + 2f, width - 12f, cannotMove.Colorize(PhobosWidgets.Bad)) + 4f;
        if (thrown != null)
        {
            y += PhobosWidgets.WrappedLine(6f, y, width - 12f, "PhobosVehicleSystems_SysTrackThrown".Translate((thrown.props.label ?? thrown.props.key).Named("PART")).Resolve()
                .Colorize(DetailColour)) + 4f;
        }
        if (trackScratch.Count > 0)
        {
            partScratch.Clear();
            foreach (VehicleComponent track in trackScratch) partScratch.Add(("", track));
            DrawParts(comp, ref y, width, ref hovered);
        }
        PhobosWidgets.CardEnd(top, ref y, 0f, width);
    }

    // ---- Electrical loads ----

    static readonly List<ElectricalLoadSpec> loadScratch = new();

    /// <summary>
    /// Every load this vehicle has, as a row: its name and what it is doing (running, standing by, switched off, cut to save the battery,
    /// or wanting power the battery cannot give), with its draw in Engineer mode. Loads the vehicle is not fitted with are left out.
    /// </summary>
    static void DrawLoads(CompPhobosVehicleSystems comp, ref float y, float width, bool engineer, ref bool anything)
    {
        // The loadout's loads and the fittings' (Framework 0.129.0, vehicle accessories).
        IReadOnlyList<ElectricalLoadSpec> loads = comp.Loads;
        if (loads.Count == 0) return;
        loadScratch.Clear();
        bool anyShed = false, anyNoPower = false;
        foreach (ElectricalLoadSpec load in loads)
        {
            LoadState state = comp.StateOf(load);
            if (state == LoadState.NotFitted) continue;
            bool wrong = state is LoadState.Shed or LoadState.NoPower or LoadState.Shorted;
            if (attentionOnly && !wrong) continue;
            anyShed |= state == LoadState.Shed;
            anyNoPower |= state is LoadState.NoPower or LoadState.Shorted;
            loadScratch.Add(load);
        }
        if (loadScratch.Count == 0) return;
        anything = true;
        float top = y;
        Seen.Note("vehicles.systems-tab.loads-card", comp.Vehicle);
        PhobosWidgets.CardHeader(ref y, 0f, width, "PhobosVehicleSystems_SysLoads".Translate(),
            anyNoPower ? PhobosWidgets.Bad : anyShed ? PhobosWidgets.Caution : PhobosWidgets.Good);
        float nameEnd = width * LoadNameShare;
        foreach (ElectricalLoadSpec load in loadScratch)
        {
            LoadState state = comp.StateOf(load);
            (string key, Color colour) = state switch
            {
                LoadState.Running => ("PhobosVehicleSystems_SysLoadRunning", PhobosWidgets.Good),
                LoadState.SwitchedOff => ("PhobosVehicleSystems_SysLoadSwitchedOff", PhobosWidgets.Off),
                LoadState.Shed => ("PhobosVehicleSystems_SysLoadShed", PhobosWidgets.Caution),
                LoadState.NoPower => ("PhobosVehicleSystems_SysLoadNoPower", PhobosWidgets.Bad),
                LoadState.Shorted => ("PhobosVehicleSystems_SysLoadShorted", PhobosWidgets.Bad),
                _ => ("PhobosVehicleSystems_SysLoadIdle", DetailColour),
            };
            string label = load.label.NullOrEmpty() ? load.name : load.label.CapitalizeFirst();
            string stateText = key.Translate().Resolve();
            // Standing by because another load has its job (climate control works the heater): say so (Framework 0.119.1).
            if (state == LoadState.Idle && load.yieldsTo != null && comp.LoadActive(load.yieldsTo))
            {
                ElectricalLoadSpec? other = comp.LoadNamed(load.yieldsTo);
                stateText = "PhobosVehicleSystems_SysLoadYields".Translate((other?.label ?? load.yieldsTo).Named("LOAD")).Resolve();
            }
            // Switched on but waiting (Framework 0.171.0; the owner's report of 19 Nov 2026: switched-on work lamps that lit nothing by day read
            // as broken): the load says what it waits for, the longer list when it switches itself.
            else if (state == LoadState.Idle)
            {
                string waits = comp.Automatic(load) && !load.autoStandbyLabel.NullOrEmpty() ? load.autoStandbyLabel : load.standbyLabel;
                if (!waits.NullOrEmpty()) stateText = waits;
            }
            // Framework 0.261.0 (the owner's word of 23 Nov 2026: "do the same" as the engine's rows): what a running load draws stands in
            // a column of its own against the right edge, so the figures line up and add up. A load with no power still says what it
            // wants in its words, never in the column: the column is what is drawn.
            string amps = "";
            if (engineer && state == LoadState.Running) amps = Amps(comp.AmpsNow(load));
            else if (engineer && state == LoadState.NoPower)
                stateText = "PhobosVehicleSystems_SysLoadAmps".Translate(stateText.Named("STATE"), comp.AmpsNow(load).ToString("0.#").Named("AMPS")).Resolve();
            Rect row = PhobosWidgets.FigureRow(ref y, 0f, width, label, amps, nameEnd: nameEnd);
            // Its state runs to the edge where the row has no figure, and stops short of the figure where it has one.
            float stateWidth = width - nameEnd - 6f - (amps.Length > 0 ? PhobosWidgets.FigureColumnWidth : 0f);
            string shownState;
            using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
            {
                shownState = stateText.Truncate(stateWidth);
                Widgets.Label(new Rect(nameEnd, row.y, stateWidth, row.height), shownState.Colorize(colour));
            }
            if (Mouse.IsOver(row))
            {
                // Its state in full where it was cut short, then what the load is.
                string tip = shownState != stateText ? "PhobosVehicleSystems_SysLoadTip".Translate(label.Named("LOAD"), stateText.Named("STATE")).Resolve() : "";
                if (!load.description.NullOrEmpty()) tip += (tip.Length > 0 ? "\n\n" : "") + load.description;
                if (tip.Length > 0) TooltipHandler.TipRegion(row, tip);
            }
        }
        // All that are running, together: the vehicle's own sums (the loads on its battery, and the camp's on a house battery), which
        // count exactly the loads that read as running. Left out under the filter, which shows only what is wrong.
        if (engineer && !attentionOnly && loadScratch.Count > 1)
        {
            Widgets.DrawBoxSolid(new Rect(6f, y, width - 12f, 1f), LedgerRule);
            y += 1f;
            Rect all = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysLoadsAll".Translate(), Amps(comp.ActiveLoadAmps() + comp.CampLoadAmps));
            TooltipHandler.TipRegion(all, "PhobosVehicleSystems_SysLoadsAllTip".Translate());
            Seen.Note("vehicles.systems-tab.loads-total", comp.Vehicle);
        }
        PhobosWidgets.CardEnd(top, ref y, 0f, width);
    }

    /// <summary>Where a load's name ends and its state begins, as a share of the card's width.</summary>
    const float LoadNameShare = 0.5f;

    /// <summary>A current in amps with its unit.</summary>
    static string Amps(float amps) => "PhobosVehicleSystems_SysAmps".Translate(amps.ToString("0.#").Named("AMPS")).Resolve();

    // ---- Shared ----

    /// <summary>One part as a chip in <paramref name="rect"/> (name and health bar), as the systems' part strips draw them.</summary>
    static void PartChip(CompPhobosVehicleSystems comp, VehicleComponent part, Rect rect, ref VehicleComponent? hovered)
    {
        string label = PartLabel(part.props.label);
        float health = part.HealthPercent;
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
        {
            float chip = Mathf.Min(rect.width, Text.CalcSize(label).x + ChipBar + 14f);
            var chipRect = new Rect(rect.x, rect.y, chip, ChipHeight);
            if (Mouse.IsOver(chipRect))
            {
                Widgets.DrawHighlight(chipRect);
                hovered = part;
                string tip = "PhobosVehicleSystems_SysPartTip".Translate(label.Named("PART"), health.ToStringPercent().Named("HEALTH")).Resolve();
                if (comp.Repairs.NoteFor(part) is { } note) tip += "\n\n" + note;
                TooltipHandler.TipRegion(chipRect, tip);
            }
            Widgets.Label(new Rect(chipRect.x + 2f, chipRect.y, chip - ChipBar - 8f, ChipHeight), label.Truncate(chip - ChipBar - 8f));
            PhobosWidgets.Gauge(new Rect(chipRect.xMax - ChipBar - 2f, chipRect.y + 5f, ChipBar, ChipHeight - 10f), health, PhobosWidgets.HealthColour(health));
        }
    }
}
