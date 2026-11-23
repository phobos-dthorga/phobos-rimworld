using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Phobos.Radio;

/// <summary>What a mark shows: a transmit lamp, a meter's needle, or an aviation warning light.</summary>
public enum RadioMarkKind
{
    TransmitLamp,
    Needle,
    WarningLight,
}

/// <summary>One mark laid over a building's picture, at an offset from its centre in cells (x right, z up the screen).</summary>
public class RadioMark
{
    public RadioMarkKind kind;
    public Vector3 offset;
    /// <summary>A lamp's or light's size, in cells.</summary>
    public float size = 0.05f;
    /// <summary>A needle's length from its pivot, in cells.</summary>
    public float length = 0.1f;
}

/// <summary>
/// Marks laid over a radio building's picture (Radio 0.6.0, the radio visuals record's RV6 and RV7; the owner's rule for overlays:
/// marks over the art as drawn, never replacing it): a transmit lamp and a meter's needle on the desk and the amplifiers, a red
/// warning light on the mast at night. Data, so other modders' buildings can carry marks too. The building must be drawn each frame
/// (<c>drawerType</c> MapMeshAndRealTime), as the game's lamps are.
/// </summary>
public class CompProperties_RadioMarks : CompProperties
{
    public List<RadioMark> marks = new();

    public CompProperties_RadioMarks() => compClass = typeof(CompRadioMarks);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (var error in base.ConfigErrors(parentDef)) yield return error;
        if (parentDef.drawerType == DrawerType.MapMeshOnly) yield return $"{parentDef.defName}: radio marks need drawerType MapMeshAndRealTime";
    }
}

public class CompRadioMarks : ThingComp
{
    /// <summary>How long one cycle of sending and listening is shown to last, in ticks; the mode's duty cycle sets the share keyed.</summary>
    private const int KeyCycleTicks = 600;
    /// <summary>The warning light's flash: on for a third of each 1.5 s, about 40 flashes a minute (authored).</summary>
    private const int FlashTicks = 90;
    private const int FlashOnTicks = 30;
    /// <summary>Below this glow of the sky, the warning light shows (authored: dusk to dawn).</summary>
    private const float NightGlow = 0.35f;
    private const float NeedleRestDegrees = -50f;
    private const float NeedleSweepDegrees = 100f;
    private const float NeedleThickness = 0.012f;

    private static readonly Color LampColour = new(1f, 0.32f, 0.18f);
    private static readonly Color NeedleColour = new(0.08f, 0.08f, 0.09f);
    private static readonly Color WarningColour = new(1f, 0.12f, 0.1f);
    private static readonly Color WarningGlowColour = new(1f, 0.15f, 0.1f, 0.28f);

    public CompProperties_RadioMarks Props => (CompProperties_RadioMarks)props;

    public override void PostDraw()
    {
        if (!parent.Spawned || Props.marks.Count == 0) return;
        var centre = parent.DrawPos;
        centre.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
        foreach (var mark in Props.marks)
        {
            var at = centre + mark.offset;
            switch (mark.kind)
            {
                case RadioMarkKind.TransmitLamp:
                    if (Keyed()) Square(at, mark.size, SolidColorMaterials.SimpleSolidColorMaterial(LampColour), 45f);
                    break;
                case RadioMarkKind.Needle:
                    Needle(at, mark.length, NeedleFraction());
                    break;
                case RadioMarkKind.WarningLight:
                    if (parent.Map.skyManager.CurSkyGlow < NightGlow && (Find.TickManager.TicksGame + Seed) % FlashTicks < FlashOnTicks)
                    {
                        Square(at + new Vector3(0f, 0.001f, 0f), mark.size * 3f,
                            MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, WarningGlowColour), 45f);
                        Square(at + new Vector3(0f, 0.002f, 0f), mark.size, SolidColorMaterials.SimpleSolidColorMaterial(WarningColour), 45f);
                    }
                    break;
            }
        }
    }

    private int Seed => (StationThing?.thingIDNumber ?? parent.thingIDNumber) * 37;

    /// <summary>The station this building sends for: itself, or the station on an amplifier's coax.</summary>
    private Thing? StationThing => parent.TryGetComp<CompRadio>() is { Props.isStation: true } radio
        ? radio.parent
        : parent.TryGetComp<CompAmplifier>()?.StationNow?.parent;

    /// <summary>
    /// Whether the transmitter is keyed now: only while the station is in use (in touch with someone), and then for the mode's duty
    /// cycle, a fifth of each ten seconds for voice, so the lamp shows what the power draw already charges.
    /// </summary>
    private bool Keyed()
    {
        CompRadio? station;
        if (parent.TryGetComp<CompRadio>() is { Props.isStation: true } own)
        {
            if (!own.Transmitting) return false;
            station = own;
        }
        else if (parent.TryGetComp<CompAmplifier>() is { } amplifier)
        {
            if (!amplifier.Sending) return false;
            station = amplifier.StationNow;
        }
        else
        {
            return false;
        }
        if (station == null) return false;
        var keyedTicks = KeyCycleTicks * Mathf.Clamp01(station.Mode.dutyCycle);
        return (Find.TickManager.TicksGame + Seed) % KeyCycleTicks < keyedTicks;
    }

    /// <summary>How far the needle swings, from rest to full: a station's while keyed; an amplifier's output against its rating.</summary>
    private float NeedleFraction()
    {
        if (!Keyed()) return 0.04f;
        if (parent.TryGetComp<CompAmplifier>() is { } amplifier && amplifier.StationNow is { } station)
        {
            return Mathf.Clamp01(amplifier.OutputWatts(station) / amplifier.Props.ratedOutputWatts);
        }
        // A voice signal's power swings with the speech: a gentle flutter about three quarters.
        return 0.75f + 0.08f * Mathf.Sin((Find.TickManager.TicksGame + Seed) * 0.35f);
    }

    private static void Square(Vector3 at, float size, Material material, float degrees) =>
        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(at, Quaternion.Euler(0f, degrees, 0f), new Vector3(size, 1f, size)), material, 0);

    private static void Needle(Vector3 pivot, float length, float fraction)
    {
        var degrees = NeedleRestDegrees + NeedleSweepDegrees * fraction;
        var rotation = Quaternion.Euler(0f, degrees, 0f);
        var middle = pivot + rotation * new Vector3(0f, 0f, length / 2f);
        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(middle, rotation, new Vector3(NeedleThickness, 1f, length)),
            SolidColorMaterials.SimpleSolidColorMaterial(NeedleColour), 0);
    }
}
