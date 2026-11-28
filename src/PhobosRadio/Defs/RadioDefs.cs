using System.Collections.Generic;
using Verse;

namespace Phobos.Radio;

/// <summary>A frequency band. Other mods add bands in XML; the propagation model is chosen per band.</summary>
public class BandDef : Def
{
    public float lowMHz;
    public float highMHz;
    /// <summary>The frequency every loss is computed at. Defaults to the middle of the band.</summary>
    public float centreMHz;
    /// <summary>Scales the loss through walls and built roofs: below 1 copes better indoors (UHF), above 1 worse. Rock and natural roofs are not scaled.</summary>
    public float obstructionFactor = 1f;
    /// <summary>
    /// A band only an upgrade gives a station or an antenna (Radio 0.49.0; docs/development/vehicle-standard.md, batch 1): it has the band
    /// while one of its fitted upgrades sets this figure to 1 or more. Empty for a band a radio or antenna names in its own Def. A radio's
    /// built-in antenna works only its own Def's bands.
    /// </summary>
    public string? upgradeFigure;
    /// <summary>How a signal travels between tiles on this band (Radio 0.62.0): line of sight (VHF and up, the default), or HF's ground
    /// wave over the tiles' grounds at HF's own scale, with the atmosphere's noise at the receiver.</summary>
    public BandPath path = BandPath.LineOfSight;

    public override void PostLoad()
    {
        base.PostLoad();
        if (centreMHz <= 0) centreMHz = (lowMHz + highMHz) / 2f;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (lowMHz <= 0 || highMHz <= lowMHz) yield return $"{defName}: lowMHz and highMHz must be positive and in order";
        if (obstructionFactor <= 0) yield return $"{defName}: obstructionFactor must be positive";
    }
}

/// <summary>How a band's signal travels between tiles (Radio 0.62.0; docs/development/radio-hf.md).</summary>
public enum BandPath
{
    /// <summary>Free space or plane earth to the horizon, and the high ground between: VHF and up.</summary>
    LineOfSight,
    /// <summary>HF: the ground wave over a smooth Earth (ITU-R P.368, by NTIA's LFMF), at HF's own scale; skywave waits for slice 2.</summary>
    Hf,
}

/// <summary>A mode: what a signal carries, how much bandwidth it needs and how weak it may be.</summary>
public class ModeDef : Def
{
    public float bandwidthHz;
    /// <summary>Signal-to-noise ratio the mode needs in its own bandwidth, in dB.</summary>
    public float requiredSnrDb;
    /// <summary>Fraction of the time a transmitter is keyed while a link is in use.</summary>
    public float dutyCycle = 0.2f;
    /// <summary>Transmitter efficiency for this mode's amplifier class; the rest becomes heat.</summary>
    public float transmitEfficiency = 0.6f;
    /// <summary>A keyed tone, as Morse (Radio 0.62.0): its signal report has a third figure, the tone (RST), on a QSL card.</summary>
    public bool keyed;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (bandwidthHz <= 0) yield return $"{defName}: bandwidthHz must be positive";
        if (dutyCycle <= 0 || dutyCycle > 1) yield return $"{defName}: dutyCycle must be in (0, 1]";
        if (transmitEfficiency <= 0 || transmitEfficiency > 1) yield return $"{defName}: transmitEfficiency must be in (0, 1]";
    }
}

/// <summary>
/// A kind of channel use in the ledger (Radio 0.8.0; the owner, 24 Nov 2026: "Start on Phase 2's ledger kinds", after D'thorga
/// Mechanised Specialist Expansion's BVR channels, which count artillery, local, world and strike apart). A radio names its kind; the
/// kind's mode sets how many channels one use takes and how many radios share it by turns. Kinds never share a block of channels, so a
/// station's tooltip can count them apart. Other mods add kinds in XML, never a second system of frequencies (the radio's rules).
/// </summary>
public class ChannelKindDef : Def
{
    /// <summary>The defName of the kind a radio has when it names none: the colony's local voice net.</summary>
    public const string LocalVoiceName = "PhobosRadio_KindLocalVoice";

    /// <summary>What a use of this kind carries: its bandwidth and duty cycle set the channels and the turns.</summary>
    public ModeDef? mode;
    /// <summary>Kinds with a lower order are served first when channels are short, and listed first (authored).</summary>
    public int order = 100;
    /// <summary>
    /// Whether a use of this kind, finding every station it reaches full, takes channels from the kinds served after it that do not
    /// pre-empt (Radio 0.9.0), as a trunked system's emergency call does: the remote control of vehicles (authored).
    /// </summary>
    public bool preempts;

    private static List<ChannelKindDef>? inOrder;
    private static ChannelKindDef? localVoice;

    public ModeDef Mode => mode!;

    /// <summary>Every kind, lowest order first, then by defName; its index is the kind's place in the ledger.</summary>
    public static List<ChannelKindDef> InOrder
    {
        get
        {
            if (inOrder != null) return inOrder;
            inOrder = new List<ChannelKindDef>(DefDatabase<ChannelKindDef>.AllDefsListForReading);
            inOrder.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.defName, b.defName));
            for (var i = 0; i < inOrder.Count; i++) inOrder[i].ledgerIndex = i;
            return inOrder;
        }
    }

    /// <summary>The local voice net's kind, or the first kind there is.</summary>
    public static ChannelKindDef LocalVoice => localVoice ??= DefDatabase<ChannelKindDef>.GetNamedSilentFail(LocalVoiceName) ?? InOrder[0];

    /// <summary>The defName of the guard net's kind (Radio 0.63.0; docs/development/radio-life-on-the-air.md, RN2 and RN6).</summary>
    public const string GuardVoiceName = "PhobosRadio_KindGuardVoice";
    private static ChannelKindDef? guardVoice;
    private static bool guardLooked;

    /// <summary>
    /// The guard net's kind (Radio 0.63.0): a drafted colonist's hand-held, which takes a channel from colonists talking at home when a station
    /// is full. Null if another mod has removed it, when drafted colonists stay on the local voice net as before.
    /// </summary>
    public static ChannelKindDef? GuardVoice
    {
        get
        {
            if (!guardLooked)
            {
                guardVoice = DefDatabase<ChannelKindDef>.GetNamedSilentFail(GuardVoiceName);
                guardLooked = true;
            }
            return guardVoice;
        }
    }

    [Unsaved] private int ledgerIndex = -1;

    /// <summary>This kind's place in <see cref="InOrder"/>.</summary>
    public int LedgerIndex
    {
        get
        {
            _ = InOrder;
            return ledgerIndex;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (mode == null) yield return $"{defName}: a channel kind needs a mode";
    }
}

/// <summary>
/// A radio fitted to something that does not wear one (Radio 0.10.0; the autonomous vehicles record, step 2): a vehicle's control
/// radio, written once and named by every vehicle that carries it. Its <see cref="radio"/> is the comp's figures, as on any radio:
/// bands, mode, power, antenna height and the kind of channel use it asks for. A tie-in puts it on the things that name it.
/// </summary>
public class RadioFittingDef : Def
{
    public CompProperties_Radio? radio;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (radio == null)
        {
            yield return $"{defName}: a radio fitting needs a radio";
            yield break;
        }
        if (radio.bands.Count == 0) yield return $"{defName}: its radio needs at least one band";
        if (radio.mode == null) yield return $"{defName}: its radio needs a mode";
        if (radio.isStation) yield return $"{defName}: a fitted radio is never a station";
    }
}

/// <summary>
/// A radio the world map's cover picture can be drawn for, as the far end of a link to the place selected (Radio 0.42.0; the owner's card
/// of 27 Nov 2026, "Buttons to switch"; docs/development/radio-world-coverage.md). Each is one choice in the picture's menu. Its radio is
/// a fitting's or a thing's, so its figures are the ones a real contact uses; the Def only says which radio, and what differs from it.
/// </summary>
public class RadioCoverKindDef : Def
{
    /// <summary>The fitted radio it stands for (a vehicle's), or</summary>
    public RadioFittingDef? fitting;
    /// <summary>the thing whose radio comp it stands for (a relay's radio unit).</summary>
    public ThingDef? radioThing;
    /// <summary>Its aerial's height in metres where that is not the radio's own (a raised mast); 0 keeps the radio's.</summary>
    public float antennaHeightMetres;
    /// <summary>The mode it is worked in where that is not the radio's own (Morse).</summary>
    public ModeDef? mode;
    /// <summary>Whether the place selected needs a Morse key at a station for it, as a real contact by Morse does. A relay never has one.</summary>
    public bool needsKey;
    /// <summary>
    /// Whether it is the one that calls (a vehicle), so the link is worked in its mode; a relay is called through, and takes the mode of
    /// a convoy selected.
    /// </summary>
    public bool caller = true;
    /// <summary>Its place in the menu, lowest first.</summary>
    public int order;

    /// <summary>The radio's figures, or null when the Def names none.</summary>
    public CompProperties_Radio? Radio => fitting?.radio ?? radioThing?.GetCompProperties<CompProperties_Radio>();

    /// <summary>Its aerial's height: the Def's own where given, else the radio's.</summary>
    public float AerialMetres => antennaHeightMetres > 0f ? antennaHeightMetres : Radio?.antennaHeightMetres ?? 0f;

    private static List<RadioCoverKindDef>? inOrder;

    /// <summary>Every kind, in the menu's order.</summary>
    public static IReadOnlyList<RadioCoverKindDef> InOrder
    {
        get
        {
            if (inOrder != null) return inOrder;
            inOrder = new List<RadioCoverKindDef>(DefDatabase<RadioCoverKindDef>.AllDefsListForReading);
            inOrder.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.defName, b.defName));
            return inOrder;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        var radio = Radio;
        if (radio == null)
        {
            yield return $"{defName}: a cover kind needs a fitting or a thing with a radio";
            yield break;
        }
        if (radio.bands.Count == 0) yield return $"{defName}: its radio needs at least one band";
        if (mode == null && radio.mode == null) yield return $"{defName}: its radio needs a mode";
    }
}

/// <summary>
/// The starting values the design says to tune in play. One Def; the first one loaded wins.
/// Player preferences (display mode, metres per cell, the thought) are mod settings, not rules.
/// </summary>
public class RadioRulesDef : Def
{
    /// <summary>
    /// What one world tile counts as for radio reach between tiles, in metres (Radio 0.21.0). <b>A gameplay choice, not the world's size</b>
    /// (owner, 26 Nov 2026: "We'll have to make adjustments for the sake of gameplay. Sometimes it's not always about realism but more about
    /// gameplay", and then: "I'd even treat it as 1 tile = 1 km for the sake of radios"): the world is about 30 km a tile for everything
    /// else (owner, 18 Nov 2026), at which VHF would never leave home's tile. At 1 km a convoy's VHF reaches home from about 20 tiles, 26
    /// with a 10 m mast.
    /// </summary>
    public float metresPerWorldTile = 1000f;
    /// <summary>What a world tile measures for HF (Radio 0.62.0; the owner's card of 28 Nov 2026: HF at the world's own scale, about 30 km a
    /// tile, so a planet is about 40,000 km round), while VHF keeps <see cref="metresPerWorldTile"/>.</summary>
    public float hfMetresPerWorldTile = 30000f;
    /// <summary>The sun's cycle for HF, in game years (the owner's card of 28 Nov 2026: about one cycle in 3 game years; authored).</summary>
    public float hfSolarCycleYears = 3f;
    /// <summary>Where the game's year begins on Earth's, as a fraction from 1 January, for the HF tables' months (agent's choice: Aprimay
    /// opens the northern spring, so day 0 is about 20 March, 0.214).</summary>
    public float hfYearStartFraction = 0.214f;
    /// <summary>What lies past the radio horizon between two tiles, in dB: enough that no VHF or UHF link closes (authored; diffraction
    /// beyond the horizon is not modelled).</summary>
    public float overHorizonLossDb = 200f;
    /// <summary>
    /// Whether high ground between two tiles costs a link signal (Radio 0.41.0; the owner's card of 27 Nov 2026, "Add terrain blocking"):
    /// the hills of the tiles on the way, by the knife-edge rule (Physics.TerrainRule). A rule, never a player's setting; here so a pack
    /// of Defs can switch it off.
    /// </summary>
    public bool terrainBlocksBetweenTiles = true;

    // Calling other factions by radio (Radio 0.53.0; docs/development/radio-comms-console.md). The owner chose the benefits and the
    // downsides by card on 27 Nov 2026; every figure here is the agent's, for the owner to revise.
    /// <summary>A trader caravan asked for over a clean line costs this much goodwill (B2), where the console takes 15.</summary>
    public float radioTraderGoodwillClean = 10f;
    /// <summary>Military aid asked for over a clean line costs this much goodwill (B2), where the console takes 25.</summary>
    public float radioAidGoodwillClean = 20f;
    /// <summary>An allied supply drop asked for over a clean line costs this much goodwill (Radio 0.54.0), where the console takes 15.</summary>
    public float radioSupplyGoodwillClean = 10f;
    /// <summary>The chance a request over a marginal line is misheard, the goodwill spent (the owner's card: one in four).</summary>
    public float radioMishearChance = 0.25f;
    /// <summary>A check-in's goodwill (B5), how often one faction can be checked in with, in days, and the goodwill above which it adds none.</summary>
    public int radioCheckInGoodwill = 2;
    public float radioCheckInDays = 5f;
    public int radioCheckInGoodwillCap = 75;
    /// <summary>The Social experience a check-in gives the one who called.</summary>
    public float radioCheckInSocialXp = 200f;
    /// <summary>The chance each hostile listener ambushes a trader caravan asked for by radio, and the share of ambushes it is lost in.</summary>
    public float radioListenerAmbushChance = 0.25f;
    public float radioAmbushLostShare = 0.5f;
    /// <summary>How many game hours an ally's warning holds a raid back (B4).</summary>
    public float radioRaidWarningHours = 3f;
    /// <summary>A walking caravan's ticks a tile are multiplied by this for the ground (B1): an approximation, the path not walked.</summary>
    public float radioTraderTravelFactor = 1.5f;

    // The cover picture on the world map (Radio 0.42.0). Performance is the mod's first concern: these bound what it costs. All authored.
    /// <summary>The most real time the world picture spends working out tiles in one frame, in milliseconds.</summary>
    public float worldCoverBudgetMsPerFrame = 1.5f;
    /// <summary>The furthest the world picture looks from its site, in tiles, however high the aerials.</summary>
    public int worldCoverMaxTiles = 200;
    /// <summary>Signal to spare, in dB, at which the world picture reaches its brightest colour: less than on a map, as tiles are far apart.</summary>
    public float worldCoverFullScaleDb = 60f;
    /// <summary>How opaque the world picture is over the globe, from 0 to 1.</summary>
    public float worldCoverOpacity = 0.55f;
    /// <summary>The most places the whole network's picture is worked out from, colonies first, then relays (Radio 0.43.0).</summary>
    public int worldCoverMaxPlaces = 24;
    /// <summary>The most hops a relayed call may take (Radio 0.22.0): two relays, three hops (radio-autonomy's lean, agent default).</summary>
    public int maxRelayHops = 3;
    /// <summary>How many 25 kHz channels a repeater has between tiles (Radio 0.24.0): one pair, in and out, as the relay design leans
    /// (agent default). A relay node can name its own (a relay post's two repeaters).</summary>
    public int repeaterChannels = 2;
    /// <summary>How long a call keeps its place at a relay without being asked about again, in ticks (Radio 0.24.0; agent default: a game
    /// hour, ten times the longest a convoy's reach is kept).</summary>
    public int relayLeaseTicks = 2500;

    // Margin bands, in dB, above which each outcome starts.
    public float marginalDb = 0f;
    public float workableDb = 6f;
    public float cleanDb = 15f;

    // What a roof over an antenna costs, in dB, by roof kind.
    public float roofConstructedLossDb = 6f;
    public float roofThinNaturalLossDb = 20f;
    public float roofThickLossDb = 60f;

    // Obstructions on the line between two antennas.
    public float rockLossDb = 30f;
    public float defaultStuffLossDb = 10f;
    /// <summary>A wall only blocks where the straight line between the antennas is below this height.</summary>
    public float wallHeightMetres = 2.5f;

    /// <summary>
    /// The width of one channel, in Hz: 25 kHz, as on a trunked radio system (the owner, 24 Nov 2026). A mode takes as many as its
    /// bandwidth needs, rounded up (Radio 0.7.0).
    /// </summary>
    public float channelWidthHz = 25000f;

    /// <summary>
    /// How many vehicles one colonist at a station can drive at once (Radio 0.12.0; docs/development/autonomous-vehicles.md: an agent
    /// default). A station takes one operator, so this is also how many vehicles one station drives.
    /// </summary>
    public int vehiclesPerOperator = 2;

    // Noise sources without a radius of their own reach this far.
    public int noiseRadiusDefaultCells = 12;
    /// <summary>
    /// Where a source's noise starts to fade, as a fraction of its radius; it reaches nothing at the radius (the owner's choice of a
    /// taper, 23 Nov 2026). 1 keeps the old hard edge. Authored.
    /// </summary>
    public float noiseFadeStartFraction = 0.67f;
    /// <summary>
    /// How far jamming must raise a receiver's floor, in dB over what it hears anyway, to be called the reason a link is held back (Radio
    /// 0.45.0, EW1): the plain reason, the lost-link letter. The same 6 dB the plain reason already uses for local noise. Authored.
    /// </summary>
    public float jammingRaisesFloorDb = 6f;
    /// <summary>
    /// A call sent off the map by another mod's building (Radio 0.46.0; docs/development/electronic-warfare.md, EW18: The Dead Man's
    /// Switch's reinforcement and fire support uplinks). Its band and mode, and how strong the far end's reply arrives: jamming at the
    /// building that drowns the reply stops the call. All authored: a data link on VHF, its reply arriving at -90 dBm (a long link,
    /// well above a quiet floor of about -125 dBm), heard through a 0 dBi antenna and an 8 dB noise figure.
    /// </summary>
    public BandDef? callOutBand;
    public ModeDef? callOutMode;
    public float callOutReplyDbm = -90f;
    public float callOutNoiseFigureDb = 8f;
    /// <summary>
    /// The radio jamming pod for any aircraft (Radio 0.75.0; docs/development/electronic-warfare.md, EW10), all authored: the watts it sends
    /// out, the frequencies it covers (30 to 512 MHz: VHF and UHF, never HF, as a pod's aerial is far too short for HF's wavelengths), and the
    /// grid its place is rounded to over a map, in cells, so a pod crossing the map changes the noise picture only at each grid line.
    /// </summary>
    public float jamPodRadiatedWatts = 150f;
    public List<FloatRange> jamPodMHz = new();
    public int jamPodGridCells = 8;
    /// <summary>How often the jammers in flight over the world map are read again, in ticks (Radio 0.75.0, EW3; authored).</summary>
    public int worldJammerRefreshTicks = 250;

    // The coverage overlay (Radio 0.2.0). Performance is the mod's first concern: these bound what it costs.
    /// <summary>Signal to spare, in dB, at which the overlay reaches its brightest colour. Authored from the mock-up's open ground.</summary>
    public float coverageFullScaleDb = 130f;
    /// <summary>The most real time the overlay spends working out cells in one frame, in milliseconds. Authored; measure and tune.</summary>
    public float coverageBudgetMsPerFrame = 1.5f;
    /// <summary>
    /// The most real time the overlay spends a frame finishing the last picture shown after its station or antenna is deselected, in
    /// milliseconds (Radio 0.44.1). Smaller than while shown, as the player is looking at something else. Authored; measure and tune.
    /// </summary>
    public float coverageBackgroundBudgetMsPerFrame = 0.5f;
    /// <summary>The first, quick pass works out one cell per square block this many cells wide and paints the block. Authored.</summary>
    public int coverageCoarseBlockCells = 4;
    /// <summary>How opaque the overlay is over the map, from 0 to 1. Authored.</summary>
    public float coverageOpacity = 0.45f;

    // Keeping to radio cover (Radio 0.14.0; autonomous-vehicles.md, step 8), worked out by the Vehicle Framework tie-in. Performance first:
    // these bound what it costs. All authored; measure and tune.
    /// <summary>The most real time the cover map spends in one tick, in milliseconds.</summary>
    public float remoteCoverBudgetMsPerTick = 0.5f;
    /// <summary>The cover map is worked out only while a vehicle on the map was asked to be driven from afar within this many ticks.</summary>
    public int remoteCoverWantedTicks = 1000;
    /// <summary>How often the cover map looks for a change it must be worked out again for, in ticks.</summary>
    public int remoteCoverLookTicks = 250;

    /// <summary>How often links are recomputed. A pawn walks about four cells in 60 ticks.</summary>
    public int recomputeIntervalTicks = 60;

    // Advice by radio (Radio 0.23.0; docs/development/radio-advice-and-relays.md). The shares are the owner's card of 26 Nov 2026 ("Half");
    // the rest are agent defaults, authored, for the owner to revise.
    /// <summary>The share of the skill gap a call closes on a workable or clean link: half (the owner's card).</summary>
    public float adviceShareSolid = 0.5f;
    /// <summary>The share on a marginal link: a quarter (the owner's card).</summary>
    public float adviceShareMarginal = 0.25f;
    /// <summary>Work speed while talked through, every step spoken: about 30% longer (agent default).</summary>
    public float adviceWorkSpeedFactor = 0.77f;
    /// <summary>How often colonists' work is looked at for a call to open or end, in ticks (agent default; a tend takes several hundred).</summary>
    public int adviceLookTicks = 60;
    /// <summary>How long a colonist no adviser was found for waits before the next search, in ticks (agent default).</summary>
    public int adviceRetryTicks = 250;
    /// <summary>How long an offer of help found for an act with no job is kept before it is looked for again, in ticks (agent default).</summary>
    public int adviceOfferKeepTicks = 250;

    // The Elmer and another faction's doctor (Radio 0.26.0; the owner's cards of 27 Nov 2026). The learning factor, the Social to the adviser,
    // the ally's day and the goodwill model are the owner's; the figures marked agent default are authored, for the owner to revise.
    /// <summary>How much faster a carer learns the skill while talked through: half again (the owner's card).</summary>
    public float adviceLearnFactor = 1.5f;
    /// <summary>Social experience the adviser gains a tick on a call, "a little" (the owner's card): half what a crafter gains at a bench
    /// (agent default; the game gives 0.1 a tick of work at 1x).</summary>
    public float adviceTeachSocialPerTick = 0.05f;
    /// <summary>A faction's doctor's Medicine by how advanced it is (agent defaults): industrial, spacer, ultra and beyond. None before
    /// industry: no radio.</summary>
    public int factionDoctorIndustrial = 10;
    public int factionDoctorSpacer = 14;
    public int factionDoctorUltra = 16;
    /// <summary>An ally opens a new call at most once a day (the owner's card), in ticks.</summary>
    public int factionAllyEveryTicks = 60000;
    /// <summary>Goodwill a neutral faction asks for a new call (agent default; the owner's card: "a small goodwill cost").</summary>
    public int factionNeutralGoodwillCost = 3;
    /// <summary>A neutral faction never takes goodwill below this for its doctor (agent default), so the help never turns it hostile.</summary>
    public int factionGoodwillFloor = 0;
    /// <summary>A call stays open this long after its last use, so a convoy's tends an hour apart are one call (agent default), in ticks.</summary>
    public int factionCallWindowTicks = 2500;
    /// <summary>How long a search for a faction's settlement in reach is kept, for a map or a convoy (agent default), in ticks.</summary>
    public int factionReachKeepTicks = 2500;
    /// <summary>Settlements further than this in tiles are not looked at (agent default): past three hops of the best masts.</summary>
    public int factionSearchTiles = 100;

    // A radio friend (Radio 0.29.0; the owner's card of 27 Nov 2026: "Mood, and a little goodwill"). Every figure an agent default, authored.
    /// <summary>Hours of chat before a new friend is a friend, and before a friend is a close friend.</summary>
    public float friendHours = 10f;
    public float friendCloseHours = 30f;
    /// <summary>Goodwill a chat of at least <see cref="friendGoodwillChatTicks"/> gives the friend's faction, at most
    /// <see cref="friendGoodwillPerQuadrum"/> times a quadrum for each faction.</summary>
    public int friendGoodwill = 1;
    public int friendGoodwillChatTicks = 2500;
    public int friendGoodwillPerQuadrum = 3;

    /// <summary>School of the Air (Radio 0.31.0): a radio lesson teaches at this share of the game's lesson rate, not being face to face
    /// (agent default, authored).</summary>
    public float schoolLessonFactor = 0.75f;
    /// <summary>A lesson with the teacher beside the child at the desk (Radio 0.56.0; the owner's answer of 28 Nov 2026, "The full lesson rate"):
    /// the game's own lesson rate, face to face.</summary>
    public float schoolInPersonFactor = 1f;
    /// <summary>Radio work in a ham shack (Radio 0.68.0; the owner's card of 28 Nov 2026: a room role with a work bonus): how much faster a
    /// lesson, a neighbour's call and work under advice go there (authored).</summary>
    public float hamShackWorkFactor = 1.1f;
    /// <summary>How impressive a ham shack must be before radio operators enjoy it (authored; the game's own scale, 30 "decent").</summary>
    public float hamShackMoodImpressiveness = 30f;

    // Neighbours ask for yours (Radio 0.32.0; the owner's card of 27 Nov 2026: "Goodwill, now and then"). The goodwill is the card's figure;
    // the rest are agent defaults, authored.
    /// <summary>Days between neighbours' calls on average, across every faction.</summary>
    public float neighbourMtbDays = 15f;
    /// <summary>The least Medicine or Construction a colonist they ask for has.</summary>
    public int neighbourMinSkill = 6;
    /// <summary>Goodwill a call done in full earns (the owner's card).</summary>
    public int neighbourGoodwill = 5;
    /// <summary>How long the call lasts, and how long an unanswered request waits, in ticks.</summary>
    public int neighbourCallTicks = 2500;
    public int neighbourExpiryTicks = 15000;
    /// <summary>Experience a tick in the skill asked for: half an adviser's Social rate.</summary>
    public float neighbourSkillXpPerTick = 0.025f;

    // Life on the air (Radio 0.63.0; docs/development/radio-life-on-the-air.md, slice 1). "Lone workers chat often" is the owner's card of
    // 28 Nov 2026; every figure is an agent default, authored, for the owner to revise.
    /// <summary>How often the talk pass looks, in ticks, and how many colonists of a map it looks at each time (the record's budget).</summary>
    public int talkLookTicks = 250;
    public int talkPeoplePerLook = 8;
    /// <summary>Hours between chats on average: for someone working alone, and for someone with others near.</summary>
    public float talkChatGapLoneHours = 3f;
    public float talkChatGapHours = 12f;
    /// <summary>Nobody else within this many cells: a lone worker. Two people closer than the second figure talk face to face, not by radio.</summary>
    public int talkLoneCells = 12;
    public int talkMinCells = 15;
    /// <summary>The least opinion either may have of the other for a chat; the opinion a friend has, for an evening call.</summary>
    public int talkFloorOpinion = -20;
    public int talkFriendOpinion = 20;
    /// <summary>Recreation a chat gives each who is short of it (a tenth of a full bar is 0.1).</summary>
    public float talkChatJoy = 0.02f;
    /// <summary>How long each kind of over lasts, in ticks, and each speaker's turn within it.</summary>
    public int talkChatTicks = 300;
    public int talkEveningCallTicks = 1250;
    public int talkCompanyTicks = 2500;
    public int talkTurnTicks = 60;
    /// <summary>The evening hours for calls to colonists away, and the night hours for company on the radio (the map's own hours).</summary>
    public int talkEveningFrom = 19;
    public int talkEveningTo = 21;
    public int talkNightFrom = 22;
    public int talkNightTo = 6;
    /// <summary>Ticks between two nights' company for one colonist (half a day).</summary>
    public int talkCompanyGapTicks = 30000;

    /// <summary>
    /// Heat pushed into the room per watt of waste per second. The vanilla heater turns 175 W into
    /// 21 heat per second, so 0.12 (read from Core's Buildings_Temperature.xml, 19 Oct 2026).
    /// </summary>
    public float heatPerWattSecond = 0.12f;

    // Radar (Radio 0.74.0; docs/development/radar-and-air-defence.md, slices 1 and 2). Every figure authored unless it names a source.

    /// <summary>A world tile for radar, in metres: 30 km, the planet's own scale, as HF uses (the owner's card 1 of 28 Nov 2026).</summary>
    public float radarMetresPerWorldTile = 30000f;
    /// <summary>How often a running radar looks at what is in the sky, in ticks (RD6: every 30 ticks while a radar runs).</summary>
    public int radarLookTicks = 30;
    /// <summary>A drop pod's radar size, in m² [authored]; a drop raid's is one pod a raider.</summary>
    public float radarDropPodSquareMetres = 2f;
    /// <summary>A person's radar size, in m² (1 m²: the worked example's source, Skolnik's table as quoted by the record).</summary>
    public float radarPersonSquareMetres = 1f;
    /// <summary>An aircraft's radar size when its loadout names none, in m² per body size [authored].</summary>
    public float radarAircraftSquareMetresPerBodySize = 2f;
    /// <summary>How high a drop raid's carrier comes in, for the horizon, in metres [authored].</summary>
    public float radarDropCarrierMetres = 5000f;
    /// <summary>How high an air raid's aircraft come in, for the horizon, in metres [authored]: low, under the radar where they can.</summary>
    public float radarAirRaidMetres = 300f;
    /// <summary>
    /// How fast a raid closes on the colony in game time, in km a game hour [authored, the owner's card of 28 Nov 2026: "by the sum, at
    /// play speed"]: an air-search radar seeing a ten-pod drop at about 71 km gives about an hour and a half.
    /// </summary>
    public float radarApproachKmPerGameHour = 45f;
    /// <summary>A warning shorter than this is not worth holding the raid for; it is announced as it lands [authored].</summary>
    public float radarMinimumWarningHours = 0.25f;
    /// <summary>The longest a radar holds a raid, in game hours [authored]: a long-range radar would otherwise hold one for most of a day.</summary>
    public float radarMaximumWarningHours = 4f;
    /// <summary>Raid points for each raider the warning counts, to blur into a range [authored].</summary>
    public float radarPointsPerRaider = 90f;
    /// <summary>How much the counted raiders are blurred either way, as a share (0.3: "7 to 13" for ten) [authored].</summary>
    public float radarCountBlur = 0.3f;
    /// <summary>
    /// How far through the warning an unknown raid is identified with nobody on watch, as a share of it [authored; the owner's card of 28
    /// Nov 2026, "unknown, then identified"].
    /// </summary>
    public float radarIdentifyShareUnmanned = 0.6f;
    /// <summary>How much sooner each level of the watch's Intellectual skill identifies it, as a share of the warning [authored].</summary>
    public float radarIdentifySharePerSkill = 0.04f;
    /// <summary>The soonest a raid can be identified, as a share of the warning [authored].</summary>
    public float radarIdentifyShareBest = 0.15f;
    /// <summary>How long an aircraft or missile in sight takes to identify with nobody on watch, before the share is applied, in ticks [authored: a game hour].</summary>
    public int radarIdentifyBaseTicks = 2500;
    /// <summary>The skill the automatic plot extractor identifies as, with nobody on watch [authored].</summary>
    public int radarPlotExtractorSkill = 8;
    /// <summary>How long a colonist's watch counts after they leave the console, in ticks [authored]: a short break does not blind it.</summary>
    public int radarWatchKeepTicks = 2500;
    /// <summary>How long the data link's reach between two colonies is kept before it is asked again, in ticks (a game hour).</summary>
    public int radarDataLinkKeepTicks = 2500;

    public Physics.MarginBands MarginBands => new(marginalDb, workableDb, cleanDb);

    private static RadioRulesDef? current;

    public static RadioRulesDef Current
    {
        get
        {
            if (current == null)
            {
                var all = DefDatabase<RadioRulesDef>.AllDefsListForReading;
                current = all.Count > 0 ? all[0] : new RadioRulesDef { defName = "PhobosRadio_RulesFallback" };
                if (all.Count == 0) Log.Warning("[PhobosRadio] No RadioRulesDef loaded; using built-in defaults.");
            }
            return current;
        }
    }
}
