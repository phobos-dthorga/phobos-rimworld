using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Where the heat of work goes where there is no coolant to carry it, game-free. Every figure is passed in from the Defs, which label
/// them as authored (Systems_Chassis.xml, Systems_Lubrication.xml).
/// </summary>
public static class DriveHeatRules
{
    /// <summary>
    /// The share of a gearbox's full-work heat being made now. Off, none; standing with the engine running, a little. Moving without
    /// the drivetrain model, all of it. Moving with the model, <paramref name="cruising"/> at no load rising to
    /// <paramref name="flatOut"/> at full load: most of a gearbox's heat at ordinary loads comes from churning its oil, so it is not in
    /// proportion to the power it carries, and a light cruise (a tenth of a tank's rated power) still warms it.
    /// </summary>
    public static double GearboxWork(bool running, bool moving, bool modelled, double load, double stopped, double cruising, double flatOut)
    {
        if (!running) return 0;
        if (!moving) return stopped;
        if (!modelled) return flatOut;
        return cruising + (flatOut - cruising) * Clamp01(load);
    }

    /// <summary>
    /// The temperature oil settles at in an engine with no coolant for it to follow (a gas turbine, an air-cooled engine): the air
    /// while it is off; running, from <paramref name="idleCelsius"/> at no load to <paramref name="fullCelsius"/> at full load, as its
    /// bearings heat it and its cooler holds it down, and never below the air.
    /// </summary>
    public static double OilTargetWithoutCoolant(bool running, double ambient, double load, double idleCelsius, double fullCelsius)
    {
        if (!running) return ambient;
        return Math.Max(ambient, idleCelsius + (fullCelsius - idleCelsius) * Clamp01(load));
    }

    static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
