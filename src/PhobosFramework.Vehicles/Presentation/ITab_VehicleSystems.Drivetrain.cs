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
        float barStart = width * LoadBarStart;
        Rect loadRow = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowLoad".Translate(), load.ToStringPercent(), nameEnd: barStart);
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
        Rect mass = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowMass".Translate(), Kilograms(train.MassKg()));
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

    /// <summary>
    /// Thin air at height (Framework 0.132.0), as rows (0.262.0), while it takes power worth a look: the share of its power the engine
    /// has left, and for engineers the height and the air's pressure against the sea's.
    /// </summary>
    static void DrawThinAir(CompPhobosVehicleSystems comp, ref float y, float width, bool engineer)
    {
        if (InspectText.ThinAirPower(comp) is not float left) return;
        Rect row = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowThinAir".Translate(), left.ToStringPercent(), colour: PhobosWidgets.Caution);
        TooltipHandler.TipRegion(row, "PhobosVehicleSystems_SysRowThinAirTip".Translate());
        if (!engineer) return;
        PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowHeight".Translate(),
            "PhobosVehicleSystems_SysMetres".Translate(comp.Elevation.ToString("N0").Named("METRES")).Resolve());
        PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowPressure".Translate(),
            ((float)Phobos.Framework.Rules.AirRule.PressureRatio(comp.Elevation)).ToStringPercent());
    }

    /// <summary>
    /// What the vehicle tows (hitch slice 3, Framework 0.104.0), as rows (0.262.0): the towed vehicle and its weight, and for engineers
    /// the whole train's on the full model. Off the full model, the speed the weight behind holds it to. What holds a full-model tow
    /// back is the word on <see cref="DrawHeldBack"/>'s row.
    /// </summary>
    internal static void DrawTowing(CompPhobosHitch hitch, Powertrain? train, ref float y, float width, bool engineer, float x = 0f)
    {
        if (hitch.Trailer is not { } towed) return;
        PhobosWidgets.FigureRow(ref y, x, width, "PhobosVehicleSystems_SysRowTowing".Translate(towed.LabelShort.Named("TRAILER")), Kilograms(hitch.TowedKilograms));
        if (!hitch.FullModel)
        {
            if (hitch.PlainSpeedCap >= 0.995f) return;
            string word = "PhobosVehicleSystems_SysHeldByWeight".Translate();
            string label = "PhobosVehicleSystems_SysRowHeld".Translate();
            Rect row = WordRow(ref y, width, label, word, PhobosWidgets.Caution, hitch.PlainSpeedCap.ToStringPercent(), WordAfterShortName, out bool cut, x);
            WordTip(row, label, word, cut, null);
            return;
        }
        if (engineer && train != null)
        {
            PhobosWidgets.FigureRow(ref y, x, width, "PhobosVehicleSystems_SysRowTrain".Translate(), Kilograms(train.MassKg() + hitch.TowedKilograms));
        }
    }

    /// <summary>
    /// The share of its speed the vehicle is held to, while it is held, with what holds it as the row's word: towing, the hitch's own
    /// words (crawling or held, by grip, a gear or power); otherwise its engine or its grip on the ground. Without
    /// <paramref name="figures"/> (the Dashboard in Plain mode) the row says only that it is slowed, and by what.
    /// </summary>
    internal static void DrawHeldBack(CompPhobosVehicleSystems comp, Powertrain train, ref float y, float width, bool figures = true, float x = 0f)
    {
        if (train.Cap >= 0.99) return;
        string word = comp.Hitch is { Trailer: not null, FullModel: true } && TowText.HeldBack(train) is { } towing ? towing
            : "PhobosVehicleSystems_SysHeldBy".Translate(
                (train.Result.Limit == DriveLimit.Traction ? "PhobosVehicleSystems_LimitTraction" : "PhobosVehicleSystems_LimitPower").Translate().Named("LIMIT")).Resolve();
        string label = (figures ? "PhobosVehicleSystems_SysRowHeld" : "PhobosVehicleSystems_SysRowSlowed").Translate();
        Rect row = WordRow(ref y, width, label, word, PhobosWidgets.Caution, figures ? ((float)train.Cap).ToStringPercent() : "", WordAfterShortName, out bool cut, x);
        WordTip(row, label, word, cut, figures ? "PhobosVehicleSystems_SysRowHeldTip".Translate().Resolve() : null);
    }

    /// <summary>
    /// A turbine as rows (Framework 0.262.0): its speed as a bar, its temperature, the flight phase where there is one, its rating and
    /// how many of its engines work. The figures are the ones the inspect pane's turbine line gives.
    /// </summary>
    static void DrawTurbineRows(Powertrain train, ref float y, float width)
    {
        float spool = (float)train.TurbineNow.SpeedPercent / 100f;
        float barStart = width * LoadBarStart;
        Rect spoolRow = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowSpool".Translate(), spool.ToStringPercent("F0"), nameEnd: barStart);
        PhobosWidgets.Gauge(new Rect(barStart, spoolRow.y + 4f, width - barStart - PhobosWidgets.FigureColumnWidth - 12f, spoolRow.height - 8f), spool, PhobosWidgets.Good);
        PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowTurbineHeat".Translate(), train.ShownTurbineTemperature.ToStringTemperature("F0"));
        if (train.PhaseSpec != null && !train.PhaseSpec.label.NullOrEmpty())
        {
            PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowPhase".Translate(), train.PhaseSpec.label);
        }
        PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowRating".Translate(), train.RatingLabel);
        (int working, int engines) = train.Engines();
        PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowEngines".Translate(),
            "PhobosVehicleSystems_SysEnginesOf".Translate(working.Named("WORKING"), engines.Named("ENGINES")).Resolve(),
            colour: working < engines ? PhobosWidgets.Caution : null);
    }

    /// <summary>
    /// For engineers, what a turbine's bled air costs it (the share harder it works for the same thrust), and its lift for a hover
    /// against what a hover needs at this weight, with whether it can hover as the row's word.
    /// </summary>
    static void DrawTurbineEngineerRows(Powertrain train, ref float y, float width)
    {
        if (train.BleedShare > 0)
        {
            Rect bleed = PhobosWidgets.FigureRow(ref y, 0f, width, "PhobosVehicleSystems_SysRowBleed".Translate(), ((float)train.BleedShare).ToStringPercent("0.#"));
            TooltipHandler.TipRegion(bleed, "PhobosVehicleSystems_SysRowBleedTip".Translate());
        }
        if (!train.JudgesHover) return;
        (bool dry, bool wet, double margin) = train.HoverCheck();
        string word = (dry ? "PhobosVehicleSystems_HoverDry" : wet ? "PhobosVehicleSystems_HoverWet" : "PhobosVehicleSystems_HoverNone").Translate();
        string label = "PhobosVehicleSystems_SysRowHover".Translate();
        Rect hover = WordRow(ref y, width, label, word, dry ? PhobosWidgets.Good : wet ? PhobosWidgets.Caution : PhobosWidgets.Bad,
            ((float)margin).ToStringPercent(), WordAfterShortName, out bool cut);
        WordTip(hover, label, word, cut, "PhobosVehicleSystems_SysRowHoverTip".Translate());
    }

    /// <summary>A weight in kilograms with its unit.</summary>
    static string Kilograms(double kilograms) => "PhobosVehicleSystems_SysKilograms".Translate(kilograms.ToString("N0").Named("MASS")).Resolve();

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
