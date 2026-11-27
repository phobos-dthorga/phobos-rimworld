using System;
using System.Collections.Generic;
using System.Text;
using Phobos.Framework.Interface;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The side view of one hop between world tiles (Radio 0.48.0; the owner's pick of 27 Nov 2026, "Path profile"; the radio visuals record's
/// RV4, between tiles): the first end on the left and the second on the right, each aerial on its tile's high ground; between them the
/// ground of each tile on the way, raised by the planet's bulge so the line between the aerials is drawn straight; the ground that decides
/// in orange. The line is coloured by how good the link is. Presentation only, from the hop's <see cref="WorldLinkPath"/>; it costs
/// nothing while the window is shut.
/// </summary>
public static class WorldPathProfile
{
    public const float Height = 150f;
    private const float LeftMargin = 52f;
    private const float RightMargin = 14f;
    private const float TopMargin = 12f;
    private const float BottomMargin = 24f;
    private const float MinScaleMetres = 40f;

    private static readonly Color Background = new(0.05f, 0.06f, 0.08f);
    private static readonly Color Grid = new(0.18f, 0.22f, 0.27f);
    private static readonly Color Planet = new(0.16f, 0.19f, 0.14f);
    private static readonly Color Hills = new(0.34f, 0.36f, 0.33f);
    private static readonly Color Muted = new(0.58f, 0.63f, 0.70f);
    private static readonly Color Aerial = Color.white;

    public static void Draw(Rect rect, WorldLinkPath path, DisplayMode depth)
    {
        Widgets.DrawBoxSolid(rect, Background);
        var plot = new Rect(rect.x + LeftMargin, rect.y + TopMargin, rect.width - LeftMargin - RightMargin, rect.height - TopMargin - BottomMargin);
        int between = path.Ground.Count;
        // Where each tile on the way stands, and how high its ground is drawn: the planet's bulge there, then its hills.
        float highest = Mathf.Max((float)path.HeightAMetres, (float)path.HeightBMetres);
        for (int i = 0; i < between; i++) highest = Mathf.Max(highest, (float)(Bulge(path, i) + path.Ground[i]));
        float topMetres = Mathf.Max(MinScaleMetres, highest * 1.15f);
        float Y(double metres) => plot.yMax - Mathf.Clamp01((float)metres / topMetres) * plot.height;
        float X(double fraction) => plot.x + (float)fraction * plot.width;
        float slot = plot.width / (between + 1);

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleRight;
        foreach (float metres in new[] { 0f, Mathf.Round(topMetres / 2f), Mathf.Floor(topMetres) })
        {
            float y = Y(metres);
            Widgets.DrawLine(new Vector2(plot.x, y), new Vector2(plot.xMax, y), Grid, 1f);
            GUI.color = Muted;
            Widgets.Label(new Rect(rect.x, y - 9f, LeftMargin - 6f, 18f), "PhobosRadio_ProfileMetres".Translate(metres.ToString("0").Named("METRES")));
        }
        GUI.color = Color.white;

        for (int i = 0; i < between; i++)
        {
            float centre = X((i + 1) / (double)(between + 1));
            float left = Mathf.Max(plot.x, centre - slot / 2f);
            float width = Mathf.Max(1f, Mathf.Min(slot, plot.xMax - left) - (slot > 3f ? 1f : 0f));
            double bulge = Bulge(path, i);
            float bulgeTop = Y(bulge);
            if (bulge > 0) Widgets.DrawBoxSolid(new Rect(left, bulgeTop, width, plot.yMax - bulgeTop), Planet);
            if (path.Ground[i] <= 0) continue;
            float hillTop = Y(bulge + path.Ground[i]);
            Widgets.DrawBoxSolid(new Rect(left, hillTop, width, bulgeTop - hillTop),
                i == path.EdgeIndex && path.TerrainLossDb > 0 ? RadioPathView.CrossingColor(CrossingKind.Built) : Hills);
        }
        Widgets.DrawBoxSolid(new Rect(plot.x, plot.yMax, plot.width, 3f), Planet);

        // The two ends on their own high ground, then the line between the aerials.
        Vector2 topA = End(X(0), plot, Y, path.A.Airborne ? 0 : path.A.SiteMetres, path.HeightAMetres, slot, path.A.Airborne);
        Vector2 topB = End(X(1), plot, Y, path.B.Airborne ? 0 : path.B.SiteMetres, path.HeightBMetres, slot, path.B.Airborne);
        Widgets.DrawLine(topA, topB, RadioPathView.OutcomeColor(path.Outcome), 2f);

        if (depth == DisplayMode.Engineer && path.EdgeIndex >= 0 && path.TerrainLossDb > 0)
        {
            string cost = "PhobosRadioWorld_ProfileEdge".Translate(path.TerrainLossDb.ToString("0").Named("LOSS"));
            float width = Text.CalcSize(cost).x + 6f;
            float centre = X((path.EdgeIndex + 1) / (double)(between + 1));
            float left = Mathf.Clamp(centre - width / 2f, plot.x, plot.xMax - width);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.DrawBoxSolid(new Rect(left, plot.y + 2f, width, 16f), new Color(0f, 0f, 0f, 0.6f));
            GUI.color = RadioPathView.CrossingColor(CrossingKind.Built);
            Widgets.Label(new Rect(left, plot.y + 2f, width, 16f), cost);
            GUI.color = Color.white;
        }

        GUI.color = Muted;
        var labels = new Rect(plot.x, rect.yMax - 20f, plot.width, 18f);
        Text.Anchor = TextAnchor.LowerLeft;
        Widgets.Label(labels, "PhobosRadioWorld_ProfileEnd".Translate(path.A.Label.Named("NAME"), path.HeightAMetres.ToString("0").Named("HEIGHT")));
        Text.Anchor = TextAnchor.LowerCenter;
        Widgets.Label(labels, "PhobosRadioWorld_ProfileDistance".Translate(WorldLinkText.Km(path.Metres).Named("KM")));
        Text.Anchor = TextAnchor.LowerRight;
        Widgets.Label(labels, "PhobosRadioWorld_ProfileEnd".Translate(path.B.Label.Named("NAME"), path.HeightBMetres.ToString("0").Named("HEIGHT")));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    /// <summary>The planet's bulge under tile i of the way, the tiles taken as evenly spaced, as a contact takes them.</summary>
    static double Bulge(WorldLinkPath path, int i)
    {
        double fromA = path.Metres * (i + 1) / (path.Ground.Count + 1);
        return TerrainRule.BulgeMetres(fromA, path.Metres - fromA);
    }

    /// <summary>An end: its tile's high ground, and its aerial on top, drawn at least a few pixels tall so it is seen. Returns the aerial's tip.</summary>
    static Vector2 End(float x, Rect plot, Func<double, float> y, double siteMetres, double tipMetres, float slot, bool airborne)
    {
        float half = Mathf.Clamp(slot / 2f, 2f, 10f);
        float left = Mathf.Clamp(x - half, plot.x, plot.xMax - half * 2f);
        float siteTop = y(siteMetres);
        if (siteMetres > 0) Widgets.DrawBoxSolid(new Rect(left, siteTop, half * 2f, plot.yMax - siteTop), Hills);
        float tip = Mathf.Min(y(tipMetres), siteTop - 6f);
        float at = Mathf.Clamp(x, plot.x + 1f, plot.xMax - 1f);
        // An end in the air floats: its marker alone, no mast down to the ground.
        if (!airborne) Widgets.DrawLine(new Vector2(at, siteTop), new Vector2(at, tip), Aerial, 2f);
        Widgets.DrawBoxSolid(new Rect(at - 3f, tip - 3f, 6f, 6f), Aerial);
        return new Vector2(at, tip);
    }
}

/// <summary>Turns a hop between tiles into words at the chosen depth (Radio 0.48.0). Presentation only; nothing here decides.</summary>
public static class WorldLinkText
{
    public static string Km(double metres) => (metres / 1000.0).ToString(metres < 9500 ? "0.#" : "0");

    /// <summary>Whether the hop has ground worth naming: something stands high enough to cost signal on its band.</summary>
    public static bool HasGround(WorldLinkPath path) => path.EdgeIndex >= 0 && path.Blocking.Any && (path.TerrainLossDb > 0 || path.OverHorizon && path.Blocking.OverLineMetres > 0);

    static NamedArgument Ground(WorldLinkPath path) => WorldTerrain.GroundLabel(path.Blocking.Tile).Named("GROUND");
    static NamedArgument Away(WorldLinkPath path) => Km(path.EdgeFromAMetres).Named("AWAY");
    static NamedArgument From(WorldLinkPath path) => path.A.Label.Named("FROM");

    /// <summary>Why a hop is not in touch, in plain words; or, in touch, what the ground costs it, if anything. Null with nothing to say.</summary>
    public static string? Reason(WorldLinkPath path, RadioRulesDef rules)
    {
        if (path.Link == null) return "PhobosRadioWorld_ReasonBands".Translate();
        if (path.Link.InTouch)
            return path.TerrainLossDb >= 1 ? "PhobosRadioWorld_GroundCosts".Translate(Ground(path), Away(path), From(path)).Resolve() : null;
        if (path.OverHorizon)
        {
            string horizon = "PhobosRadioWorld_ReasonHorizon".Translate(Km(path.Metres).Named("KM"), Km(path.HorizonMetres).Named("HORIZON")).Resolve();
            return HasGround(path) ? horizon + " " + "PhobosRadioWorld_ReasonHorizonGround".Translate(Ground(path), Away(path), From(path)).Resolve() : horizon;
        }
        if (path.GroundLosesIt(rules)) return "PhobosRadioWorld_ReasonGround".Translate(Ground(path), Away(path), From(path)).Resolve();
        string weak = "PhobosRadioWorld_ReasonWeak".Translate(Km(path.Metres).Named("KM")).Resolve();
        return path.TerrainLossDb >= 1 ? weak + " " + "PhobosRadioWorld_GroundCosts".Translate(Ground(path), Away(path), From(path)).Resolve() : weak;
    }

    public static string Describe(WorldLinkPath path, DisplayMode depth)
    {
        RadioRulesDef rules = RadioRulesDef.Current;
        var sb = new StringBuilder(LinkText.Bars(path.Outcome) + " " + LinkText.OutcomeLabel(path.Outcome));
        if (Reason(path, rules) is { } reason) sb.Append('\n').Append(reason);
        if (depth == DisplayMode.Plain || path.Link is not { } link) return sb.ToString();

        sb.Append('\n').Append("PhobosRadioWorld_Operator".Translate(link.Band.LabelCap.ToString().Named("BAND"), link.Mode.LabelCap.ToString().Named("MODE"),
            link.MarginDb.ToString("0.0").Named("MARGIN"), Km(path.Metres).Named("KM"), Km(path.HorizonMetres).Named("HORIZON")));
        if (path.EdgeIndex >= 0 && path.TerrainLossDb > 0)
        {
            sb.Append('\n').Append(path.Blocking.OverLineMetres > 0
                ? "PhobosRadioWorld_OperatorGroundOver".Translate(Ground(path), Away(path), From(path),
                    path.Blocking.OverLineMetres.ToString("0").Named("OVER"), path.TerrainLossDb.ToString("0.0").Named("LOSS"))
                : "PhobosRadioWorld_OperatorGroundNear".Translate(Ground(path), Away(path), From(path),
                    (-path.Blocking.OverLineMetres).ToString("0").Named("UNDER"), path.TerrainLossDb.ToString("0.0").Named("LOSS")));
        }
        if (depth != DisplayMode.Engineer) return sb.ToString();

        sb.Append('\n');
        AppendDirection(sb, "PhobosRadioWorld_Direction".Translate(path.A.Label.Named("FROM"), path.B.Label.Named("TO")), link.Budget.Forward, !link.Budget.ReverseLimits);
        AppendDirection(sb, "PhobosRadioWorld_Direction".Translate(path.B.Label.Named("FROM"), path.A.Label.Named("TO")), link.Budget.Reverse, link.Budget.ReverseLimits);
        return sb.ToString().TrimEnd();
    }

    /// <summary>One direction's sheet, as the map's link sheet writes it, with the two terms that mean something else between tiles renamed.</summary>
    static void AppendDirection(StringBuilder sb, string title, LinkDirection direction, bool limits)
    {
        sb.AppendLine();
        sb.AppendLine(limits ? "PhobosRadio_SheetDirectionLimiting".Translate(title.Named("DIRECTION")) : title);
        foreach (LinkTerm term in direction.Terms)
        {
            string value = term.IsLevel ? $"{term.Value:0.0} dBm" : $"{(term.Value >= 0 ? "+" : "")}{term.Value:0.0} dB";
            string name = term.Key == LinkDirection.KeyObstruction ? "PhobosRadioWorld_TermGround".Translate()
                : term.Key == LinkDirection.KeyPath ? "PhobosRadioWorld_TermPath".Translate()
                : term.Key.Translate();
            sb.AppendLine($"  {name}: {value}");
        }
    }
}

/// <summary>
/// The link sheet for a call between world tiles (Radio 0.48.0): each hop of the way home, with its side view, its words at the player's
/// display mode (one level deeper on request, as the map's sheet), and a button that shows the ground in the way on the globe. It reads
/// what its opener hands it each frame, so it follows the caravan. Presentation only.
/// </summary>
public class Dialog_WorldLink : Window
{
    const string GuardKey = "The radio path window";
    const float HopGap = 14f;

    readonly string title;
    readonly Func<IReadOnlyList<WorldLinkPath>> hops;
    DisplayMode depth;
    Vector2 scroll;
    float viewHeight = 500f;

    public Dialog_WorldLink(string title, Func<IReadOnlyList<WorldLinkPath>> hops)
    {
        this.title = title;
        this.hops = hops;
        depth = RadioMod.Settings.displayMode;
        doCloseX = true;
        draggable = true;
        forcePause = false;
        absorbInputAroundWindow = false;
        closeOnClickedOutside = false;
        preventCameraMotion = false;
    }

    public override Vector2 InitialSize => new(680f, 600f);

    public override void DoWindowContents(Rect inRect)
    {
        // Guarded: a throw is logged once and the window rests, where the game would log it every frame (UiGuard).
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(inRect); return; }
        try { Contents(inRect); }
        catch (Exception e) { UiGuard.Report(GuardKey, e); }
    }

    void Contents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width - 40f, 36f), title);
        Text.Font = GameFont.Small;

        IReadOnlyList<WorldLinkPath> current = hops();
        var body = new Rect(0f, 40f, inRect.width, inRect.height - 40f - 40f);
        var view = new Rect(0f, 0f, body.width - 20f, viewHeight);
        Widgets.BeginScrollView(body, ref scroll, view);
        float y = 0f;
        if (current.Count == 0)
        {
            Widgets.Label(new Rect(0f, y, view.width, 40f), "PhobosRadioWorld_NoPath".Translate());
            y += 40f;
        }
        for (int i = 0; i < current.Count; i++)
        {
            WorldLinkPath hop = current[i];
            string heading = "PhobosRadioWorld_HopTitle".Translate(hop.A.Label.Named("FROM"), hop.B.Label.Named("TO"));
            using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft, RadioPathView.OutcomeColor(hop.Outcome))) Widgets.Label(new Rect(0f, y, view.width, 24f), heading);
            y += 24f;
            WorldPathProfile.Draw(new Rect(0f, y, view.width, WorldPathProfile.Height), hop, depth);
            y += WorldPathProfile.Height + 6f;
            string words = WorldLinkText.Describe(hop, depth);
            float height = Text.CalcHeight(words, view.width);
            Widgets.Label(new Rect(0f, y, view.width, height), words);
            y += height + 4f;
            if (WorldLinkText.HasGround(hop))
            {
                var show = new Rect(0f, y, 220f, 28f);
                if (Widgets.ButtonText(show, "PhobosRadioWorld_ShowGround".Translate())) CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(hop.Blocking.Tile));
                TooltipHandler.TipRegion(show, "PhobosRadioWorld_ShowGroundTip".Translate());
                y += 32f;
            }
            y += HopGap;
        }
        Widgets.EndScrollView();
        viewHeight = y;

        var buttons = new Rect(0f, inRect.height - 32f, inRect.width, 32f);
        if (depth < DisplayMode.Engineer && Widgets.ButtonText(new Rect(buttons.x, buttons.y, 160f, 32f), "PhobosRadio_ShowMore".Translate())) depth++;
        if (depth > DisplayMode.Plain && Widgets.ButtonText(new Rect(buttons.x + 170f, buttons.y, 160f, 32f), "PhobosRadio_ShowLess".Translate())) depth--;
        if (Widgets.ButtonText(new Rect(buttons.xMax - 120f, buttons.y, 120f, 32f), "CloseButton".Translate())) Close();
    }
}

/// <summary>
/// The Radio button of a caravan on the world map (Radio 0.48.0): the signal bars a hand-held has on a map, by how well the caravan
/// reaches home, opening the link sheet between tiles. A tie-in that knows the caravan's chain home hands it over; nothing here decides.
/// </summary>
public static class WorldLinkGizmo
{
    public static Gizmo Make(string place, LinkOutcome? outcome, string? line, Func<IReadOnlyList<WorldLinkPath>> hops)
    {
        return new Command_Action
        {
            defaultLabel = "PhobosRadio_GizmoLabel".Translate(LinkText.OutcomeLabel(outcome ?? LinkOutcome.NoContact).ToString().Named("OUTCOME")),
            defaultDesc = "PhobosRadioWorld_GizmoDesc".Translate((line ?? "").Named("LINE")).Resolve().Trim(),
            icon = RadioTextures.SignalFor(outcome),
            action = () =>
            {
                Find.WindowStack.Add(new Dialog_WorldLink("PhobosRadioWorld_SheetTitle".Translate(place.Named("PLACE")), hops));
                Seen.Note("radio.world-link-sheet", null, place);
            },
        };
    }
}
