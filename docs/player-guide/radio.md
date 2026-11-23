# Radio

**D'thorga Radio Framework** gives colonists hand-held radios and a base station, joined to antennas by coax. Every contact is
decided by a real link budget: distance, walls, rock, roofs and electrical noise all count, and doubling the distance costs 6 dB.
This is the first step, local radio on one map. [Back to the guide](README.md).

![A colonist with a hand-held radio on a hillside, a lattice mast and a lit cabin behind](../../mods/PhobosRadioFramework/About/Preview.png)

## Read this first (gotchas)

- **Not yet tested in game.** It has been built and installed, but not yet seen running.
- **Needs D'thorga Framework and Vanilla Expanded Framework** (the coax is the latter's pipe network; the upgrades are the former's).
- **The upgrades are experimental**: the desk, whip and mast each have an **Upgrades** button.
- **Decide Metres per map cell before a colony** (default 1). It changes every contact's range.
- **The hand-held is worn on the belt slot**, so a colonist can't wear a shield belt and a radio together.
- **Before removing it**, deconstruct every radio building and drop every hand-held.

## What to build

![Radio kit: hand-held radio, base station, whip antenna, mast antenna, coax feedline, and the radio button with its signal bars](images/radio-kit.png)

Research **local radio** (after Microelectronics basics).

| Thing | Where | What it is |
| --- | --- | --- |
| Hand-held radio, VHF or UHF | Machining table: 20 steel, 1 component | 5 W, worn on the belt. **VHF reaches further outdoors; UHF copes better with walls** |
| Radio base station | Architect, D'thorga, Radio: 100 steel, 3 components | 2 x 2, needs power, both bands, a built-in whip |
| Coax feedline | Architect, D'thorga, Radio: 2 steel a cell | Laid like conduit, from the station to an antenna. Loses signal along its length; runs under walls |
| Whip antenna | Architect, D'thorga, Radio: 15 steel | 3 m high. The cheap way to get the antenna outside |
| Mast antenna | Architect, D'thorga, Radio: 60 steel, 1 component | 10 m high, with a little gain. **Sees over walls** |

## How a contact is decided

- **Distance**: doubling it costs 6 dB, a quarter of the power.
- **Walls and closed doors on the straight line**, by material (wood least, plasteel most), but only below 2.5 m: **a tall antenna
  sees over walls**, and the last few cells near the hand-held bite.
- **Natural rock** blocks almost everything. **A mountain base needs coax out to an antenna on the surface.**
- **The roof over each end**: a built roof costs a little, thin mountain roof a lot, overhead mountain nearly everything.
- **Your own generators and batteries** raise the noise near them, fading out towards the edge of their reach.
- **Both directions count, and the worse one decides**: usually the 5 W hand-held, not the 25 W station.

A colonist wearing a hand-held has a **Radio** button with signal bars; click it for the full working. The station's **Net** tab
says who is in touch and which antenna it uses: whichever antenna on its coax gives the best link, or its built-in whip if that is
better or nothing is connected. **Select the station or an antenna and the map shows its coverage**: brighter where a hand-held
would reach it well, dark red where it would not, with every door counted as closed. It fills in roughly at once and finely over a
few seconds, and redraws when you build, so you can see a wall's radio shadow appear. **Select a colonist wearing a hand-held** and a line runs to the antenna they use, in
the colour of their signal, with a mark on everything in the way: a filled orange square on a wall that cost signal, a hollow ring on
one the line passed over, a red diamond on rock. The radio button's window shows the same path from the side. **The desk, whip and mast take upgrades** from their Upgrades button,
fitted by builders: a quieter receiver for the desk, a taller pole for the whip, and an extension or a longer antenna for the mast.
Select the station afterwards and the coverage shows what it bought. A colonist in good contact gets a small **in touch with home** mood, and a fainter one on a weak
link. **While placing** an antenna or the station, the cursor shows its height, the roof and the noise there; red means overhead
mountain.

**How much of the maths to show** (its settings): **Plain** shows bars and one reason in plain words; **Operator** adds the band, the
signal-to-noise ratio, the margin and which direction limits the link; **Engineer** shows every term. The window a radio's button
opens can show one level more than your setting.

## Try it

1. Research local radio. Build the station indoors under a built roof, craft a VHF hand-held and wear it.
2. Walk outside, then behind a stone wall, then into a mountain: watch the bars, the reason, and the line to the antenna.
3. Lay coax from the station out to a mast antenna, and see the station switch to it.
4. Stand near a running generator: the noise appears and the margin falls.
5. Select the station: the map shows its coverage. Build a stone wall near a mast and watch its shadow; select a mast near a running
   generator and see the whole map dim.
6. Open the Engineer sheet: every term should add up to the margin, and the limiting direction should make sense (the 5 W hand-held
   usually limits, not the 25 W station).
7. Check the needs tab for "in touch with home".

The figures, the settings and the notes for modders are in [D'thorga Radio Framework's README](../../mods/PhobosRadioFramework/README.md).
