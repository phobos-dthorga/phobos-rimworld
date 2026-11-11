using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How long a colonist takes to claim another faction's vehicle, game-free (owner, 11 Nov 2026: unattended vehicles with no crew may be
/// claimed, taking time, longer for bigger ones; docs/development/vehicle-other-factions-audit.md). A base time to get in and make it
/// answer to us, then a little more for every cell the vehicle covers, up to a limit. Every figure comes from the rules Def and is authored.
/// </summary>
public static class ClaimRule
{
    public static double Hours(int cells, double baseHours, double hoursPerCell, double maxHours) =>
        Math.Min(Math.Max(0, maxHours), Math.Max(0, baseHours) + Math.Max(0, cells) * Math.Max(0, hoursPerCell));
}
