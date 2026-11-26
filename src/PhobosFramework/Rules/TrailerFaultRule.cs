using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// A trailer towed on wrecked running gear (EM42; docs/development/vehicle-road-work.md; the owner's card of 26 Nov 2026: "warn, then
/// allow"), game-free: the tow goes ahead, crawling, and the dragged gear wears further. Every figure is the agent's.
/// </summary>
public static class TrailerFaultRule
{
    /// <summary>The share of its speed a vehicle keeps while it drags a trailer on wrecked gear.</summary>
    public const float CrawlShare = 0.25f;
    /// <summary>The share of each damaged running part's full health lost for every hour it is dragged.</summary>
    public const float DragWearPerHour = 0.02f;

    /// <summary>A running part's health after an hour's share of dragging; never below nothing.</summary>
    public static float AfterDrag(float health, float maxHealth, float hours) => Math.Max(0f, health - DragWearPerHour * maxHealth * Math.Max(0f, hours));
}
