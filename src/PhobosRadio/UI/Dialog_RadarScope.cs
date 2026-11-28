using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The radar scope (Radio 0.74.0; docs/development/radar-and-air-defence.md, row RD6): a plan-position picture, north up, a sweep turning
/// once every four seconds of real time, range rings in kilometres, and each contact as a diamond in its identity's colour, brightest just
/// after the sweep passes it. Two scales: the sky around the colony, between world tiles; and over the colony itself, the map. A list of
/// the contacts beside it says what each is. It only reads the colony's radar picture; nothing is worked out here.
/// <para>Cost: nothing while closed. Open, it asks the picture for its contacts, which works them out at most every 30 ticks.</para>
/// </summary>
public class Dialog_RadarScope : Window
{
    private const float SweepDegreesPerSecond = 90f;
    private const float ScopeSize = 460f;
    private const int RingSegments = 72;
    private static readonly float[] RingStepsKm = { 5f, 10f, 25f, 50f, 100f, 200f, 400f, 800f };
    private static readonly Color Ring = new(0.35f, 1f, 0.45f, 0.35f);

    private readonly Map map;
    private bool overColony;
    private Vector2 listScroll;
    private readonly List<OtherRadar> others = new();

    public static void Open(Map map)
    {
        if (Find.WindowStack.Windows.OfType<Dialog_RadarScope>().FirstOrDefault(w => w.map == map) is { } open)
        {
            Find.WindowStack.Notify_ClickedInsideWindow(open);
            return;
        }
        Find.WindowStack.Add(new Dialog_RadarScope(map));
        if (Seen.Wants("radio.radar-scope")) Seen.Note("radio.radar-scope", null, map.Parent.LabelCap);
    }

    private Dialog_RadarScope(Map map)
    {
        this.map = map;
        doCloseX = true;
        draggable = true;
        forcePause = false;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        closeOnClickedOutside = false;
    }

    public override Vector2 InitialSize => new(ScopeSize + 330f, ScopeSize + 110f);

    public override void DoWindowContents(Rect inRect)
    {
        if (map == null || !Find.Maps.Contains(map))
        {
            Close();
            return;
        }
        var picture = MapComponent_PhobosRadar.For(map);
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 30f, 32f), "PhobosRadar_ScopeTitle".Translate(map.Parent.LabelCap.Named("COLONY")));
        Text.Font = GameFont.Small;

        Rect buttons = new(inRect.x, inRect.y + 36f, ScopeSize, 28f);
        if (Widgets.ButtonText(buttons.LeftHalf().ContractedBy(2f, 0f), "PhobosRadar_ScaleSky".Translate(), active: overColony)) overColony = false;
        if (Widgets.ButtonText(buttons.RightHalf().ContractedBy(2f, 0f), "PhobosRadar_ScaleColony".Translate(), active: !overColony)) overColony = true;

        List<RadarContact> contacts = overColony ? picture.OverMap() : picture.Contacts();
        float maxKm = overColony ? ColonyRadiusKm() : SkyRadiusKm(picture, contacts);
        Rect scope = new(inRect.x, buttons.yMax + 6f, ScopeSize, ScopeSize);
        DrawScope(scope, contacts, maxKm);

        Rect side = new(scope.xMax + 12f, inRect.y + 36f, inRect.width - scope.width - 12f, inRect.height - 36f);
        DrawSide(side, picture, contacts);
    }

    /// <summary>The sky scale: the next ring step past the farthest contact, or past what the best radar sees of a fighter.</summary>
    private static float SkyRadiusKm(MapComponent_PhobosRadar picture, List<RadarContact> contacts)
    {
        float want = contacts.Count == 0 ? 0f : (float)(contacts.Max(c => c.RangeMetres) / 1000.0) * 1.1f;
        foreach (CompRadar radar in picture.Watching())
            if (radar.parent.Map == picture.map) want = Mathf.Max(want, (float)(radar.SeesMetres(5.0, 3000.0) / 1000.0));
        foreach (float step in RingStepsKm)
            if (step * 4f >= want) return step * 4f;
        return RingStepsKm[RingStepsKm.Length - 1] * 4f;
    }

    private float ColonyRadiusKm() => Mathf.Max(map.Size.x, map.Size.z) * 0.5f * RadioMod.Settings.metresPerCell / 1000f;

    private static void DrawScope(Rect rect, List<RadarContact> contacts, float maxKm)
    {
        Vector2 centre = rect.center;
        float radius = rect.width / 2f - 4f;
        Widgets.DrawBoxSolid(rect, Color.black);
        DrawDisc(centre, radius);
        for (int i = 1; i <= 4; i++) DrawCircle(centre, radius * i / 4f, Ring);
        for (int degrees = 0; degrees < 360; degrees += 30)
            Widgets.DrawLine(centre, centre + Direction(degrees) * radius, new Color(Ring.r, Ring.g, Ring.b, 0.15f), 1f);
        Text.Font = GameFont.Tiny;
        GUI.color = RadarSymbols.Phosphor;
        for (int i = 1; i <= 4; i++)
        {
            float km = maxKm * i / 4f;
            Widgets.Label(new Rect(centre.x + 3f, centre.y - radius * i / 4f, 80f, 18f), km < 1f ? (km * 1000f).ToString("0") + " m" : km.ToString("0") + " km");
        }
        Widgets.Label(new Rect(centre.x - 6f, rect.y + 2f, 20f, 18f), "PhobosRadar_North".Translate());
        GUI.color = Color.white;

        float sweep = Time.realtimeSinceStartup * SweepDegreesPerSecond % 360f;
        for (int trail = 0; trail < 12; trail++)
        {
            float alpha = 0.5f * (1f - trail / 12f);
            Widgets.DrawLine(centre, centre + Direction(sweep - trail * 2.5f) * radius, new Color(0.35f, 1f, 0.45f, alpha), trail == 0 ? 2f : 3f);
        }

        foreach (RadarContact contact in contacts)
        {
            float r = Mathf.Min(1f, (float)(contact.RangeMetres / 1000.0 / maxKm)) * radius;
            Vector2 at = centre + Direction(contact.BearingDegrees) * r;
            float since = ((sweep - contact.BearingDegrees) % 360f + 360f) % 360f;
            float glow = 1f - since / 360f * 0.75f;
            Color colour = RadarSymbols.ColourOf(contact.Identity);
            GUI.color = new Color(colour.r, colour.g, colour.b, glow);
            GUI.DrawTexture(new Rect(at.x - 7f, at.y - 7f, 14f, 14f), RadarSymbols.Blip(contact.Identity));
            Widgets.Label(new Rect(at.x + 8f, at.y - 8f, 140f, 18f), contact.Count != null ? contact.Label + " (" + contact.Count + ")" : contact.Label);
        }
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
    }

    private void DrawSide(Rect rect, MapComponent_PhobosRadar picture, List<RadarContact> contacts)
    {
        float y = rect.y;
        int own = picture.WorkingRadarCount;
        int linked = picture.LinkedRadarCount;
        Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "PhobosRadar_ScopeRadars".Translate(own.Named("OWN"), linked.Named("LINKED")));
        y += 24f;
        int? skill = picture.WatchSkill();
        Widgets.Label(new Rect(rect.x, y, rect.width, 24f),
            skill == null ? "PhobosRadar_ScopeNoWatch".Translate() : "PhobosRadar_ScopeWatch".Translate(skill.Value.Named("SKILL")));
        y += 24f;
        others.Clear();
        RadarTargets.Others(map, others);
        foreach (OtherRadar other in others)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, y, rect.width, 20f), "PhobosRadar_OtherRadar".Translate(other.Thing.LabelShortCap.Named("RADAR"),
                (other.Working ? "PhobosRadar_OtherWorking" : "PhobosRadar_OtherOff").Translate().Named("STATE"), other.Note.Named("NOTE")).Resolve());
            Text.Font = GameFont.Small;
            y += 20f;
        }
        y += 6f;
        if (own == 0 && linked == 0)
        {
            Widgets.Label(new Rect(rect.x, y, rect.width, 60f), "PhobosRadar_ScopeNoRadar".Translate());
            return;
        }
        if (contacts.Count == 0)
        {
            Widgets.Label(new Rect(rect.x, y, rect.width, 40f), "PhobosRadar_ScopeEmpty".Translate());
            return;
        }
        Rect outer = new(rect.x, y, rect.width, rect.yMax - y);
        Rect inner = new(0f, 0f, outer.width - 16f, contacts.Count * 46f);
        Widgets.BeginScrollView(outer, ref listScroll, inner);
        float row = 0f;
        foreach (RadarContact contact in contacts.OrderBy(c => c.RangeMetres))
        {
            Rect line = new(0f, row, inner.width, 44f);
            GUI.color = RadarSymbols.ColourOf(contact.Identity);
            GUI.DrawTexture(new Rect(line.x, line.y + 4f, 12f, 12f), RadarSymbols.Blip(contact.Identity));
            GUI.color = Color.white;
            string title = contact.Count != null ? contact.Label + " (" + contact.Count + ")" : contact.Label;
            Widgets.Label(new Rect(line.x + 16f, line.y, line.width - 16f, 22f), title);
            Text.Font = GameFont.Tiny;
            string where = "PhobosRadar_ContactLine".Translate(("PhobosRadar_Identity" + contact.Identity).Translate().Named("IDENTITY"),
                RadarWarning.CompassOf(contact.BearingDegrees).Named("BEARING"), Distance(contact.RangeMetres).Named("RANGE"),
                contact.HeightMetres.ToString("0").Named("HEIGHT")).Resolve();
            if (contact.Remote) where += " " + "PhobosRadar_ByDataLink".Translate();
            Widgets.Label(new Rect(line.x + 16f, line.y + 20f, line.width - 16f, 22f), where);
            Text.Font = GameFont.Small;
            row += 46f;
        }
        Widgets.EndScrollView();
    }

    private static string Distance(double metres) => metres < 1000.0 ? metres.ToString("0") + " m" : (metres / 1000.0).ToString("0") + " km";

    /// <summary>A screen direction for a bearing: north up, clockwise.</summary>
    private static Vector2 Direction(float bearing)
    {
        float radians = bearing * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians));
    }

    private static void DrawDisc(Vector2 centre, float radius) =>
        GUI.DrawTexture(new Rect(centre.x - radius, centre.y - radius, radius * 2f, radius * 2f), RadarSymbols.ScopeDisc);

    private static void DrawCircle(Vector2 centre, float radius, Color colour, float width = 1f)
    {
        Vector2 previous = centre + new Vector2(0f, -radius);
        for (int i = 1; i <= RingSegments; i++)
        {
            Vector2 next = centre + Direction(i * 360f / RingSegments) * radius;
            Widgets.DrawLine(previous, next, colour, width);
            previous = next;
        }
    }
}
