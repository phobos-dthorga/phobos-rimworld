using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Framework.Interface;
using Phobos.Framework.Vehicles.Rules;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The Systems tab's engine figures as rows (Framework 0.260.0; the owner's choice of 23 Nov 2026, after the card read as "a clump of
/// words"): the engine's speed, load, power, fuel and heat one to a row, and what the engine drives besides the wheels as a table, each
/// load with its own power and its own share of the fuel. Presentation only: every figure is the vehicle's own, read as the sentences
/// they replace read them.
/// </summary>
public partial class ITab_VehicleSystems
{
    /// <summary>Where the load's bar begins, as a share of the card's width: where a reading's bar begins in the cards above.</summary>
    const float LoadBarStart = 0.38f;
    /// <summary>A load under this many kilowatts shows a second decimal, so a small one does not read as nothing.</summary>
    const float SmallKilowatts = 0.1f;
    static readonly Color LedgerRule = new(1f, 1f, 1f, 0.14f);

    /// <summary>The ledger's loads, biggest first, reused every frame.</summary>
    static readonly List<ShaftLoadNow> ledgerScratch = new();
    static readonly System.Comparison<ShaftLoadNow> BiggestFirst = (a, b) =>
        a.Kilowatts != b.Kilowatts ? b.Kilowatts.CompareTo(a.Kilowatts) : string.CompareOrdinal(a.Label, b.Label);

    /// <summary>
    /// A ground vehicle's engine as rows: its gear where it has one, its speed, its load as a bar, and for engineers the shaft power, the
    /// fuel burned for it, the heat into the coolant and the vehicle's mass. The figures are the ones the inspect pane's power line gives.
    /// </summary>
    static void DrawEngineRows(CompPhobosVehicleSystems comp, Powertrain train, ref float y, float width, bool engineer)
    {
        DriveResult r = train.Result;
        if (r.Gear > 0) PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowGear".Translate(), r.Gear.ToString());
        PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowSpeed".Translate(),
            "PhobosVehicleSystems_SysRpm".Translate(r.Rpm.ToString("N0").Named("RPM")).Resolve());

        float load = comp.Engine.Load;
        Rect loadRow = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowLoad".Translate(), load.ToStringPercent());
        float barStart = width * LoadBarStart;
        PhobosWidgets.Gauge(new Rect(barStart, loadRow.y + 4f, width - barStart - PhobosWidgets.FigureColumnWidth - 12f, loadRow.height - 8f), load,
            load > InspectText.LabouringLoad ? PhobosWidgets.Caution : PhobosWidgets.Good);
        TooltipHandler.TipRegion(loadRow, "PhobosVehicleSystems_SysRowLoadTip".Translate());
        if (!engineer) return;

        double fuelKw = train.FuelKwNow(comp.Engine);
        Rect shaft = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowShaft".Translate(), Kilowatts(train.ShaftKwNow(comp.Engine), "0"));
        TooltipHandler.TipRegion(shaft, "PhobosVehicleSystems_SysRowShaftTip".Translate());
        Rect fuel = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowFuel".Translate(), Kilowatts(fuelKw, "0"));
        TooltipHandler.TipRegion(fuel, "PhobosVehicleSystems_SysRowFuelTip".Translate());
        Rect heat = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowHeat".Translate(), Kilowatts(fuelKw * train.Spec.CoolantHeatShare, "0"));
        TooltipHandler.TipRegion(heat, "PhobosVehicleSystems_SysRowHeatTip".Translate());
        Rect mass = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowMass".Translate(),
            "PhobosVehicleSystems_SysKilograms".Translate(train.MassKg().ToString("N0").Named("MASS")).Resolve());
        TooltipHandler.TipRegion(mass, "PhobosVehicleSystems_SysRowMassTip".Translate());
    }

    /// <summary>
    /// What the engine is driving besides the wheels (the engine-load ledger, Framework 0.131.0), as a table, biggest first: Operator the
    /// names, Engineer each with its kilowatts; and for a piston engine on the full model each load's share of the fuel burned now and all
    /// of them together (Framework 0.243.0), where each shaft kilowatt costs one over the indicated efficiency. A turbine's fuel has a
    /// floor of its own, so it gets no fuel column. Nothing with nothing driven. Reads the ledger the vehicle worked out at its last look.
    /// </summary>
    static void DrawLedger(CompPhobosVehicleSystems comp, ref float y, float width, bool engineer)
    {
        IReadOnlyList<ShaftLoadNow> loads = comp.ShaftLoads;
        if (loads.Count == 0) return;
        ledgerScratch.Clear();
        for (int i = 0; i < loads.Count; i++) ledgerScratch.Add(loads[i]);
        ledgerScratch.Sort(BiggestFirst);

        double fuelKw = 0, efficiency = 0;
        if (comp.Powertrain is { Full: true, Ground: true, IsAircraft: false } train && train.Spec.IdleFuelShare <= 0)
        {
            fuelKw = train.FuelKwNow(comp.Engine);
            efficiency = train.Spec.IndicatedEfficiency;
        }
        double allShare = ShaftLoadRule.FuelShare(comp.LedgerKw, fuelKw, efficiency);
        bool shares = allShare > 0;
        Seen.Note("vehicles.systems-tab.ledger-rows", comp.Vehicle);

        y += 4f;
        float top = y;
        string fuelHead = "PhobosVehicleSystems_SysLedgerFuel".Translate();
        LedgerRow(ref y, width, "PhobosVehicleSystems_SysLedger".Translate(), engineer ? "PhobosVehicleSystems_SysLedgerPower".Translate().Resolve() : null,
            shares ? fuelHead : null, DetailColour, GameFont.Tiny);
        foreach (ShaftLoadNow load in ledgerScratch)
        {
            LedgerRow(ref y, width, load.Label.CapitalizeFirst(), engineer ? Kilowatts(load.Kilowatts) : null,
                shares ? ((float)ShaftLoadRule.FuelShare(load.Kilowatts, fuelKw, efficiency)).ToStringPercent() : null);
        }
        // All of them together: only where there is a figure to add up, and more than one load to add.
        if (ledgerScratch.Count > 1 && (engineer || shares))
        {
            Widgets.DrawBoxSolid(new Rect(6f, y, width - 12f, 1f), LedgerRule);
            y += 1f;
            LedgerRow(ref y, width, "PhobosVehicleSystems_SysLedgerAll".Translate(), engineer ? Kilowatts(comp.LedgerKw) : null,
                shares ? ((float)allShare).ToStringPercent() : null);
        }
        var table = new Rect(0f, top, width, y - top);
        if (Mouse.IsOver(table))
        {
            string tip = "PhobosVehicleSystems_SysLedgerTip".Translate();
            if (shares) tip += "\n\n" + "PhobosVehicleSystems_SysLedgerFuelTip".Translate().Resolve();
            TooltipHandler.TipRegion(table, tip);
        }
    }

    /// <summary>One row of the ledger: its name, then whichever of its power and its share of the fuel this table shows, the last against
    /// the right edge.</summary>
    static void LedgerRow(ref float y, float width, string label, string? power, string? share, Color? colour = null, GameFont font = GameFont.Small)
    {
        if (power != null && share != null) PhobosWidgets.FigureRow(ref y, 0f, width, label, power, share, colour, font);
        else PhobosWidgets.FigureRow(ref y, 0f, width, label, power ?? share ?? "", colour: colour, font: font);
    }

    /// <summary>A power in kilowatts with its unit, to one decimal, and to two where it is small.</summary>
    static string Kilowatts(double kilowatts, string? format = null) =>
        "PhobosVehicleSystems_SysKilowatts".Translate(kilowatts.ToString(format ?? (kilowatts < SmallKilowatts ? "0.##" : "0.#")).Named("KILOWATTS")).Resolve();
}
