using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Whether an aircraft can ever be in a flight phase (Framework 0.265.2; seen by the owner, 23 Nov 2026: an A-10 read "cannot hover",
/// because it shares the jump jet's flight profile and that profile has a hover phase). A profile is shared by many aircraft, and one of
/// its phases may be read from a part only some of them have: the jump jet's vertical landing is read from its own landing part. A phase
/// is reachable where some rule reports it from the vehicle itself, or from a part the vehicle has. Game-free.
/// </summary>
public static class PhaseReachRule
{
    /// <summary>
    /// Whether any rule reports <paramref name="phase"/> for a vehicle with these parts. Each rule is the phase it reports and the part
    /// it reads (null or empty for the vehicle itself); <paramref name="parts"/> are the names of the vehicle's parts.
    /// </summary>
    public static bool Reachable(string phase, IEnumerable<(string phase, string? part)> rules, ICollection<string> parts)
    {
        foreach ((string reported, string? part) in rules)
        {
            if (reported != phase) continue;
            if (string.IsNullOrEmpty(part) || parts.Contains(part!)) return true;
        }
        return false;
    }
}
