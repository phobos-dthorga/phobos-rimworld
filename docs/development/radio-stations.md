# Radio stations: the radio desk and the radio terminal (24 Nov 2026)

**Status (24 Nov 2026):** partly built. The channel ledger is built in Radio Framework 0.7.0
([below](#channels-radio-070)) and counts by kind since 0.8.0 ([kinds](#kinds-radio-080)), not yet tested in game. The radio terminal, both linear amplifiers and the full-size ground-plane are built in Radio
Framework 0.5.0 ([as built](#as-built-radio-050-24-nov-2026)), not yet tested in game. The building upgrades for the radio desk and
both antennas are built in Radio Framework 0.4.0
([below](#building-upgrades-radio-040)), not yet tested in game. The radio terminal and the linear amplifier are designed, not built;
their pictures, the desk's, the whip's and the mast's are bought and wait for the owner's picks
(`assets/phobos-radio/requests/paid-1-record.md`).

The owner, 24 Nov 2026, of the base station's picture: "they come in so many varieties these days ^ ^ A computer terminal can
effectively be a base station with an SDR and hefty amplifier." Then, by card: the two paid tries for the base station's picture
are "One of each, they could each have wildly different stats to each of them :)", and the SDR terminal is to be designed now, as
its own kit. The radio's rules apply: **performance first**, and **physics sets the kit, the kit sets the play**
(`src/PhobosRadio/AGENTS.md`).

**How to read the marks.** **[O]** the owner's word. **[code]** read in our code or Defs on 24 Nov 2026. **[S]** a source named
beside it. **[A]** an agent's proposal or authored figure, for the owner to revise. Every figure not marked **[S]** is authored.

## Read this first

- **Two stations, each its own kit [O].** The radio desk is today's base station with a new picture; the radio terminal is new: a
  computer with a software-defined radio (SDR) and a linear amplifier.
- **On one map the terminal's edge is modest, and that is said plainly.** The hand-held's 5 W usually decides a link, so the
  terminal's power helps the weaker direction only rarely; its quieter receiver gains about 4 dB where the colony is quiet. Its
  real identity (more bands, digital modes, the waterfall) arrives with Phase 2 **[A]**. In Phase 1 it is a better receiver that
  needs an outside antenna and makes its own noise.
- **No risk to the player's things or people**, so no risk setting is needed (the shared design rules).

## The two stations

| | Radio desk (today's base station) | Radio terminal (new) |
| --- | --- | --- |
| What it is | A desk transceiver with a power supply, desk microphone, power meter and logbook | A computer driving a software-defined radio, with a linear amplifier in a cabinet beside it |
| defName | `PhobosRadio_BaseStation` (kept; a label may change, never a defName) **[code]** | `PhobosRadio_RadioTerminal` **[A]** |
| Footprint | 2 × 2 **[code]** | 3 × 2, the vanilla comms console's footprint **[A]** |
| Power, idle | 60 W **[code]** | 250 W, more than the comms console's 200 W for the amplifier on standby **[A]** |
| Transmit power | 25 W **[code]** | 100 W, the Australian Standard licence's limit **[S]** ([reference checks](radio-reference-checks.md#australian-licence-limits)) |
| Receiver noise figure | 6 dB **[code]** | 2 dB, a quiet front end with a preamplifier **[A]**, unverified against a source |
| Antenna | A built-in whip at 2 m, or the best antenna on its coax **[code]** | **None of its own**: it reaches nobody until coax joins it to a whip or mast **[A]** |
| Its own noise | None | **A noise source itself while powered**: −75 dBm at 1 m, out to 8 cells, fading like every source **[A]**. Computers and switch-mode power supplies are well-known radio noise makers; the figure is authored |
| Bands | VHF and UHF **[code]** | VHF and UHF now; the wider bands an SDR covers join in Phase 2 **[A]** |
| Heat | Waste heat while in use **[code]** | The same rule, four times the power: more heat in the shack **[A]** |
| Research | Local radio **[code]** | A new project, **Software radio**, after Local radio and the game's Microelectronics **[A]** |
| Cost | 100 steel, 3 components **[code]** | 120 steel, 6 components, 1 advanced component **[A]** |

**What the figures do on one map** (the link-budget arithmetic of `LinkMath`, worked by hand **[A]**): the forward direction, hand-held
to station, usually decides. The terminal's receiver is 4 dB quieter than the desk's, so where nothing noisy is near its antenna a
hand-held gains about 4 dB of signal to spare. Its 100 W adds 6 dB only to the station-to-hand-held direction, which rarely decides.
**Its own noise can undo all of it**: an antenna within a few cells of the terminal hears the computer, so the terminal wants its
antenna well away, which is the siting lesson the radio already teaches with generators.

## The four questions

| Question | Answer **[A]** |
| --- | --- |
| What it lets the player do | Run a quieter, stronger station; later (Phase 2) reach further bands, use digital modes, and see the waterfall on its screen (RV3). |
| What it stops them doing | Running it on its own: it has no antenna, so it needs coax to an outside whip or mast. Siting an antenna near it, which its own noise spoils. |
| How it changes the way they play | The shack goes indoors and the antenna goes out and away; the power grid feels a 250 W machine; the room warms. |
| What outside it notices | The power grid and the room's temperature, through the game's own systems. Nothing else in Phase 1, by choice; Phase 3's direction finding would make a 100 W station easier to trace. |

## Its cost (the radio's performance rule)

| What | How much | How often |
| --- | --- | --- |
| A second station in the net | One more station in the loop that already pairs every worn hand-held with every station's antennas | Every recompute, about once a second **[code]** |
| Its own noise | One more source in the noise snapshot, summed like the others, no allocation | Each noise lookup **[code]** |
| Its coverage picture | The same overlay as the desk's, while it is selected | Only while selected |

Nothing new runs per tick or per frame. Measuring: the coverage dev action and, asked for first, `perf.py`.

## What it needs in code [A]

- `CompProperties_Radio` gains `hasBuiltInAntenna` (default true), so a station can have none; `RadioNet.StationEndpoints` then offers
  only the antennas on its coax, and its inspect text says it needs one.
- The terminal carries a `RadioNoiseExtension` in its own Def (`noiseWhen` PowerOn), so it joins the noise snapshot with no new code.
- One `ThingDef`, one `ResearchProjectDef`, the Keyed text; the Architect's Radio tab.

## Questions for the owner

1. The amplifier: built into the terminal, or a separate building on the coax that either station can use?
   **Answered: separate, on the coax [O]** (card, 24 Nov 2026), against the agent's recommendation of built in.
2. The terminal's own noise: on, or left out? **Answered: "Yes, noisy by default, but enable/disable via a setting." [O]**
3. The figures above: as proposed, or changed? Open; revised below for the separate amplifier.

## The amplifier on the coax (the owner's choice, 24 Nov 2026)

With the amplifier its own building, the terminal and the amplifier change as follows. All figures **[A]**, to revise.

| | Radio terminal | Linear amplifier (new) |
| --- | --- | --- |
| What it is | A computer and a software-defined radio: the station alone | A heavy amplifier cabinet that sits in the coax run between a station and its antennas |
| defName | `PhobosRadio_RadioTerminal` | `PhobosRadio_LinearAmplifier` |
| Footprint | 3 × 2 | 1 × 1 |
| Power, idle | 150 W, a computer and radio | 20 W on standby |
| Transmit power | **10 W on its own**, the Australian Foundation licence's limit **[S]** ([reference checks](radio-reference-checks.md#australian-licence-limits)) | Raises what reaches it by 10 dB, **up to 100 W**, the Standard licence's limit **[S]**: the desk's 25 W and the terminal's 10 W both come out at 100 W |
| On receive | | Switched out of the line: a small loss of 0.5 dB, nothing more |
| Draw while sending | The mode's rule | Its output over the mode's efficiency, times the duty cycle, as stations already draw; the waste is heat in its own room |
| Research | Software radio | **Linear amplifiers**, after Local radio |
| Noise | Its own, while powered, **with a setting to switch it off** (on by default) | None of its own **[A]** |

- **In the run, not just on the net [A]:** the amplifier counts for an antenna only when the cable path from the station to that antenna
  passes through it, so where it is placed matters, as on a real feedline. `FeedlineService` already traces that path; it would report
  the amplifiers it passed.
- **What this buys:** the two stations stay wildly different, as the owner asked, and the amplifier is a choice either can make: the
  desk at 100 W for a powered shack, or the terminal's quiet receiver at 100 W.
- **The setting (to report when built):** D'thorga Radio Framework, "Radio terminals make radio noise", on by default.
- **The art:** the terminal's picture shows no amplifier; the amplifier has its own picture in the same paid batch.

## The owner's picks and models (24 Nov 2026)

From the paid pictures ([batch record](../../assets/phobos-radio/requests/paid-1-record.md)) and the owner's cards the same day.
Nothing here is built yet; the desk's, whip's and mast's pictures ship in Radio 0.4.0.

| Kit | The owner's word | What it means **[A]** figures, to tune |
| --- | --- | --- |
| Radio terminal | Picked try 1, whose radio box has a stub antenna; by card: **"A weak stub that works"** **[O]** | The terminal gains a built-in stub: about −5 dBi at 1 m, indoors under its roof, so it reaches hand-helds on its own, poorly; coax to a whip or mast is far better. This replaces "None of its own" in the tables above |
| Whips | "Both with varying specs, just like real life! ^ ^"; by card: **compact vs full-size** **[O]** | **Compact ground-plane** (try 1, today's whip): 0 dBi, 15 steel, as now. **Full-size ground-plane** (try 2, its radiator a full quarter wave): about 2 dBi on both bands (a quarter-wave ground-plane's usual figure, approximate), 25 steel and more work. Both 3 m and both take the taller pole |
| Linear amplifiers | "Offer both as alternate models with varying specs to each ^ ^"; by card: **100 W and 400 W** **[O]** | **Linear amplifier** (try 1): up to 100 W (Standard licence), 20 W on standby. **Heavy linear amplifier** (try 2): up to 400 W (Advanced licence), 40 W on standby, four times the draw and heat while sending, behind a later research step and an advanced component |

## Channels (Radio 0.7.0)

The owner, 24 Nov 2026, with a screenshot of D'thorga Mechanised Specialist Expansion's BVR system tooltip (search and fire control
radars, "Channels occupied: 0 / 8", by kind: artillery, local, world, strike; free): "a simplification of radio channels to pretty
much what the DMS BVR mod does for its fire control solution channels ... will serve us well and keep the most amount of players
happy, without us having to debug several differing systems all at once". Then: "go ahead and build the channel ledger", and "We
could even assign multiple channels to systems that are 'high in bandwidth' ... or even say that a channel is 25 kHz each just like a
trunked system would be." **[O]**

**How it works [code]:**

- **A channel is capacity, never physics.** The link budget still decides every contact. Channels only decide which of the radios a
  station can hear it serves, as a trunked system hands its channels out by turn. No frequencies, no tuning, no interference between
  channels.
- **Each channel is 25 kHz wide** (`RadioRulesDef.channelWidthHz`) **[O]**. A mode takes as many channels as its bandwidth needs,
  rounded up: FM voice (12.5 kHz) takes one; a future wideband data or video mode would take several.
- **Radios share a channel by taking turns**, as many as the mode's duty cycle allows: voice, keyed a fifth of the time, lets five
  share one. Each takes its turn of a ten-second cycle; the stations' cycles start at their own points.
- **Stations bring channels**: the radio desk 1, the radio terminal 4 **[A]**; the desk's **second transceiver** upgrade makes it 2.
  So the desk serves 5 radios, 10 with the upgrade, and the terminal 20.
- **Who is served [A]:** every radio in touch (workable or clean) goes to the best station that has room; **a radio already served
  keeps its station** while it stays in touch with it, so two near the limit never swap back and forth; the strongest are served
  first. A radio in touch that no station has room for is **busy**: its button reads "Radio: net busy", and it gets no "in touch"
  mood. A marginal link takes no channel, as before.
- **What reads it:** the station's inspect text, its button and its Net tab ("Channels: 1 of 1 in use, 25 kHz each; 0 free", then
  a line for each kind, below; and each colonist's channel or "net busy"); the lamp and needle (lit while a served radio takes its
  turn); the waterfall, whose streaks sit on the channel each radio really holds, so its earlier fiction ("its own spot of the band")
  is gone.
- **Game-free and tested:** `Physics/Channels.cs` (`PerUse`, `RadiosPerUse`, `ChannelsHeld`, `Room`, `Allocate`, `KeyedNow`,
  `ChannelOf`), with tests for the counts, the best station with room, a holder keeping its channel, kinds in blocks of their own,
  and exactly one radio keyed at a time on a full channel.

**The four questions [A].** It lets the player plan capacity: more stations, a second transceiver, or a terminal for a large colony.
It stops a single desk serving everyone: past five radios on one desk, the rest wait. It changes play little in a small colony and
more in a large one; its real use comes with Phase 2's calls. What outside notices: the colonists' mood, through the "in touch"
thought; nothing else, by choice.

**Its cost (the radio's performance rule):** one pass over the links already worked out, when the net works them out (about once a
second): a sort and a few dictionary lookups per radio. Asking who is keyed is arithmetic on the tick. Nothing per frame or per cell.

### Kinds (Radio 0.8.0)

The owner, 24 Nov 2026: "Start on Phase 2's ledger kinds next". **[O]** Built in Radio Framework 0.8.0, not yet tested in game.

- **A kind is a Def** (`Phobos.Radio.ChannelKindDef`: a label, a mode, an order) **[code]**. A radio names its kind
  (`CompProperties_Radio.channelKind`); one that names none is **local voice**, the only kind shipped today. The kind's mode sets
  its shape: how many channels one use takes (its bandwidth over 25 kHz) and how many radios take turns on it (its duty cycle).
- **Kinds never share a block [A]:** each kind's radios fill whole uses of their own, so the counts can be told apart, as the BVR
  system's are. Three voices and one 100 kHz data link on the terminal hold 1 + 4 = 5 channels, one more than it has.
- **Who is served first [A]:** kinds with a lower order first, then the strongest; **a holder still keeps its station**, whatever
  its kind, unless a kind that pre-empts needs its channel (below).
- **Pre-emption (Radio 0.9.0) [A]:** a kind with `preempts` set, finding every station it reaches full, takes channels from the
  kinds served after it that do not pre-empt, the weakest of the last kind first and no more than it needs; those put off try
  other stations. Vehicle control is the first kind that does, so a truck is never kept off the net by talk
  ([autonomous vehicles by radio](autonomous-vehicles.md#the-link-is-a-real-link)).
- **The kinds shipped (0.9.0) [A]:** vehicle control (order 50, pre-empts: one channel shared by two vehicles), vehicle picture
  (order 60: two channels for one), local voice (order 100: one channel shared by five). No radio uses the two vehicle kinds
  yet, so a station lists only local voice until the vehicle kit comes.
- **Every station serves every kind [A].** A station limited to some kinds (a desk with no data modem) waits for a kind that needs
  the difference.
- **What reads it:** under the channels line, a line for each kind in use, as in the owner's screenshot: "Local voice: 1 in use by
  3 radios; room for 2 more", where "room" counts the turns left in the kind's last block and the whole blocks that fit in the free
  channels. With nobody served, the local voice line says how many it has room for, so the player can plan.
- **The waterfall** draws each kind's block after the one before it, and a wide use across the middle of the channels it takes.

**Its cost:** unchanged in kind: the same pass about once a second, now with a count per kind per station (an array as long as the
kinds) and one sort per station by kind. Asking a radio's channel or turn is a dictionary lookup.

**What comes next [A]:** the remote control of vehicles is the first new kind the owner wants ("the real feature I wish to
implement soon is autonomous vehicles and perhaps their weapon systems too, all via our radio framework", 24 Nov 2026), designed in
[autonomous vehicles by radio](autonomous-vehicles.md). Caravans by HF and world calls to other factions follow in Phase 2 proper; jamming
(Phase 3) would take channels away.

## As built: Radio 0.5.0 (24 Nov 2026)

The owner, 24 Nov 2026: "Go ahead and build the terminal, amplifiers and full-size whip". All **[code]**; nothing is tested in game.

| Kit | defName | Figures as built **[A]** | Research |
| --- | --- | --- | --- |
| Radio terminal | `PhobosRadio_RadioTerminal` | 3 x 2, 150 W idle; 10 W out; noise figure 2 dB; stub at 1 m, −5 dBi; its own noise −75 dBm at 1 m out to 8 cells (`computerNoise`) | Software radio (1,200), after Local radio |
| Linear amplifier | `PhobosRadio_LinearAmplifier` | 1 x 1, 20 W standby; +10 dB up to 100 W; 0.5 dB on receive | Linear amplifiers (900), after Local radio |
| Heavy linear amplifier | `PhobosRadio_HeavyLinearAmplifier` | 1 x 1, 40 W standby; +10 dB up to 400 W; 0.5 dB on receive | High-power amplifiers (1,600), after Linear amplifiers |
| Full-size ground-plane | `PhobosRadio_WhipFullSize` | 1 x 1, 3 m, 2 dBi; 25 steel; takes the taller pole | Local radio |

- **In the run, not on the net.** `FeedlineService.Path` walks the cable from the station to each antenna and returns the amplifiers it
  passes through with the coax (`FeedPath`); an amplifier on a branch is not in it. The route is kept per net as before; whether an
  amplifier is powered is read each time links are worked out.
- **Several in one run [A]:** the strongest powered one decides (`Amplifier.ThroughWatts`, game-free and tested); they are not
  chained. Unpowered, a signal passes through less its loss. On receive every amplifier in the run costs its loss, as each is
  switched out of the line (`EndSide.ReceiveOnlyLossDb`, shown in the sheet's receive cable line).
- **Draw and heat [A]:** while its station is in touch with anyone, an amplifier draws its output over the mode's efficiency times
  the duty cycle, and pushes the rest into its room as heat; every amplifier on a station's coax counts as sending, whichever antenna
  is in use.
- **Power on and off** reaches the net and the coverage picture through the game's own power signals, so nothing polls.
- **The terminal's noise** is a `RadioNoiseExtension` in its own Def with `computerNoise`, counted only while the setting **Radio
  terminals make radio noise** is on (default on, the owner's card). It reaches its own stub, as a real computer beside its antenna
  would.
- **The whip's label** is now "compact ground-plane"; its defName and figures are unchanged.
- **Cost (the performance rule):** an amplifier adds a few multiplications per endpoint each time links are worked out, about once a
  second; its rare tick reads its station's in-touch count. Nothing per frame or per cell.

## Building upgrades (Radio 0.4.0)

The owner, 24 Nov 2026: "I want you to use the code we created earlier to allow for upgradeable buildings. Experimental, but I wish to
explore it further." The code is D'thorga Framework's building upgrades (Framework 0.210.0, made for the service yard): a building with
the upgrades comp has an Upgrades button; a builder brings the materials and fits each one under Construction; an upgrade sets named
figures the building's own code reads. **The radio now depends on D'thorga Framework [A]**, as the shared rule for our C# mods already
says every one should (`src/AGENTS.md`); it brings Harmony with it, which the radio itself still does not use.

| Upgrade | Fits | What it changes | Cost and work | Why it is physically right |
| --- | --- | --- | --- | --- |
| Receive preamplifier | Radio desk | Noise figure 6 dB down to 3 dB | 10 steel, 1 component; 600 work; Construction 4; Local radio | A low-noise amplifier ahead of the receiver; it cannot help against a generator's noise, which arrives with the signal (said in its description) |
| Taller pole | Whip | Height 3 m to 5 m | 20 steel; 400 work; Construction 3 | Height clears walls through the ray rule |
| Mast extension | Mast | Height 10 m to 15 m | 40 steel; 1,200 work; Construction 6 | The same rule, for longer |
| Longer collinear | Mast | Gain 3 dBi to 6 dBi | 20 steel, 1 component; 800 work; Construction 5 | Longer stacked collinears are commonly rated about 6 dBi on 2 m; **unverified against a source** |

All costs, work and skills are authored **[A]**. No upgrade has a picture yet; the inspect text and the Upgrades window say what is
fitted (the framework allows an overlay picture per upgrade, for later).

**The four questions [A].** It lets the player improve a station or antenna where it stands, without rebuilding. It stops nothing. It
changes play by giving each piece of kit a path to grow, and the coverage picture shows each upgrade's effect at once. Outside the
radio, builders' time and materials notice, under Construction; nothing else, by choice.

**Its cost (the radio's performance rule).** Figures are read when the net works links out (about once a second) and when the coverage
picture collects its antennas, never per frame or per cell. The framework's figure lookup was walking every fitted upgrade on each
call; **Framework 0.275.0 keeps a table** rebuilt only when an upgrade is fitted or taken off, so a lookup is one dictionary read. On a
change the framework now **sends the building's comps a signal** (`CompPhobosBuildingUpgrades.ChangedSignal`, through the game's own
`ReceiveCompSignal`), and the radio's comps mark the net and the coverage picture to work again: nothing polls.

**What is not upgraded yet [A].** Hand-helds (apparel, not buildings); the desk's built-in whip; the radio terminal and the linear
amplifier, which are not built (an amplifier upgrade to 400 W, the Australian Advanced licence's limit, would suit it).

## The art

Both stations' pictures are in one paid batch with the new whip and mast ([radio visuals](radio-visuals.md)): two tries each, the
owner picking one card each. The radio desk keeps the 2 × 2 picture; the terminal is drawn at 3 × 2 with its amplifier.
