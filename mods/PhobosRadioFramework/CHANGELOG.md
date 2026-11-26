# D'thorga Radio Framework changelog

Maintained from 19 October 2026. Draft entries record preparation, not Steam publication; nothing is
published yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

### Changed

- **The art and sound records no longer ship in the mod's folder** (the owner's choice of 23 Nov 2026, in the docs rewrite).
  The notes saying how each picture and sound was made moved to the repository's assets folder, so installing the mod no longer
  copies them into the game's Mods folder; the next install leaves the old copies in its backup. Nothing a player sees changes.
- **The exporters' records no longer ship in the mod's folder either** (the owner's word of 23 Nov 2026). The notes saying
  what each picture was cut from moved beside the art records, in the repository's assets folder; nothing a player sees
  changes.
- **The README is slimmer**: what the mod needs, its real gotchas, what it adds with its figures, its settings and links; the
  notes for modders, the dev actions and the checks done are on the repository's modders' page for the radio. The Workshop
  draft now says it needs D'thorga Framework, as it has since 0.4.0, and names the upgrades. Nothing in play changes.



## [0.22.0] - 2026-11-26 - Draft

Relays on the world map

### Added

- **Calls home pass through relays**: a convoy of yours standing still, or a vehicle left at a depot, with a radio and the new **repeater
  kit** fitted (the owner's card: a relay between tiles needs a repeater), and a relay mast. Each hop is decided by the same link budget,
  and a chain is as good as its worst hop; at most two relays (three hops). The convoy's line names the relays it reaches home through.
  Not yet tested in game: row 609.
- **The repeater kit**, a fitting for any land vehicle (15 steel, 2 components, after remote-controlled vehicles; 1 A), in a new repeater
  bay every land vehicle gains. Its figures are the agent's.
- **Hilltops reach further** (the owner's card: by hilliness): a tile lifts an aerial by 30 m on small hills, 100 m on large hills and
  300 m in mountains, for the horizon and the path loss.
- **Plane-earth fading between tiles** (the owner's card): the path loss is the greater of free space and plane earth, still cut at the
  horizon, so a weak radio fades a little before the horizon. Vehicle radios barely change; hand-helds fade sooner.
- **A relay mast**: a stopped convoy with a radio puts one up on its tile from 60 steel and 4 components (Put up a relay mast); a 10 m
  solar mast that stands until taken down from the world map. It borrows the mast antenna's picture for now. Not yet tested in game: row 610.

### Compatibility and limits

- Relayed calls take no channels at a relay yet (a repeater would hold two), and a mast needs no power and never wears: limits of this
  first slice. Home stations and outposts as relays are not built. The relay design is radio-autonomy's record,
  docs/development/radio-advice-and-relays.md.

## [0.21.0] - 2026-11-26 - Draft

A first step beyond the map: a convoy's reach home

### Added

- **A convoy's radio reaches home on the world map** (with D'thorga Framework's vehicles): its best fitted vehicle radio against your
  home's powered stations, through the same link budget as every contact, out to the radio horizon of the two aerials. A Datum's mast
  lifts the convoy's end. Not yet tested in game: rows 600 and 601.
- **For radio, a world tile counts as 1 km**, a gameplay choice (the owner: "I'd even treat it as 1 tile = 1 km for the sake of
  radios"); the world is about 30 km a tile for everything else, where VHF would never leave home's tile. So a convoy reaches home from
  about 20 tiles, 26 with a 10 m mast.

### Compatibility and limits

- Between tiles there are no walls, hills or local noise yet, and nothing past the horizon: simplifications. HF is still to come.

## [0.20.2] - 2026-11-26 - Draft

Stations turn as they are placed

### Fixed

- **The radio base station and the radio terminal can be turned while placing them** (the owner's report of 26 November 2026), as
  most furniture can; the transmit lamp and meter needle turn with the desk. Stations already built stay as they are. Not yet tested in
  game: row 581.

## [0.20.1] - 2026-11-26 - Draft

A lost link's reason

### Changed

- **A vehicle driven from a station that loses its link says why in its letter** (with D'thorga Framework 0.314.0): nobody is working
  the console any more, the operator has their hands full with other vehicles, every channel that can hear it is in use, or no working
  station can hear it. Not yet tested in game.

## [0.20.0] - 2026-11-26 - Draft

The remote gun turret by its own radio

### Added

- **A remote gun turret no coax reaches links by its own radio** (the owner's card of 26 November 2026, "Either"). A whip on its mount
  joins the radio net as a driven vehicle's radio does: while enemies are about it asks the station in reach for a control channel and
  its picture, and fires once the station serves both and an operator has a hand free for it; the rest of the time it stands by,
  holding no channel. Walls, range and noise decide its link as for any radio. A turret on a station's coax uses the coax, and its
  radio stays quiet. Its whip's figures are authored. Not yet tested in game: row 541.

### Compatibility and limits

- Its cost to the net (one more link per station for each turret not on coax, worked out with the net's pass) is to be measured on
  the owner's colony.

## [0.19.0] - 2026-11-26 - Draft

The remote gun turret under Combat Extended

### Added

- **The remote gun turret takes Combat Extended's ammunition when Combat Extended is installed** (the owner's question of 26 November
  2026: everything appropriate supported by it). It becomes Combat Extended's own turret, keeping the radio's rule that it fires only
  while a station watches it (or on its own with the setting), and fires .50 BMG from a 200-round magazine that colonists reload, in
  place of the steel belt. Its aim, spread and range are authored. Not yet tested in game: row 540.

## [0.18.0] - 2026-11-26 - Draft

The remote gun turret

### Added

- **A remote gun turret for the colony's defences**, after the new research remote turrets (the owner's word of 25 November 2026,
  "use both", and the cards of 26 November 2026): the trucks' remote weapon station's second try, a .50 on a powered, camera-sighted
  mount, on an armoured pedestal (the owner's pick). Connect it to a radio station with coax: it fires only while a colonist at that
  station's console watches it, and then can be aimed by hand, or by itself when D'thorga Framework's "Remote guns fire on their own"
  is on. It takes one of the operator's two hands, after the station's vehicles. While enemies are about, a station with turrets calls
  a colonist on remote operation to its console. Fed a 200-round belt from steel by haulers; holds fire without power. Not yet tested
  in game: row 540.

### Compatibility and limits

- Linked by coax only for now; its own radio, for a turret no coax reaches, is the next step.
- Without Vehicle Framework there is no "fire on their own" switch, so it fires only while a station watches it.
- Under Combat Extended it fires the game's own .50 round from its steel belt; Combat Extended ammunition came in 0.19.0.

## [0.17.0] - 2026-11-26 - Draft

Channels, seen in play

### Added

- **Seen-in-play notes for the channels** (the owner's word, 26 November 2026, to give the test list's remaining rows evidence): a
  radio in reach with no channel left for it is net busy, and a second station takes a radio its best station had no channel for.
  Written only with D'thorga Framework's seen-in-play setting on; nothing in play changes. Not yet tested in game.

## [0.16.1] - 2026-11-25 - Draft

Fixes from the fault-read of 25 November 2026

### Fixed

- **A vehicle whose remote control kit fails holds, and the player is told** (with D'thorga Framework 0.284.0). With the kit's power
  shed or its part shot away, a vehicle a station drove stopped without a word and took up its link again silently. Now it holds with
  a letter saying its kit failed, keeps its buttons, and is told when it works again. Not yet tested in game: row 508.
- **The coverage overlay paints a hand-held's reach** (F1): it counted every vehicle's radio as a hand-held, about 13 dB too good, and
  worked one picture for each vehicle design. Not yet tested in game: row 511.
- **Smaller fixes**: an unpowered desk paints no coverage, and cells revealed from fog are painted (F4); the desk's and amplifier's
  lamps are worked out once a tick, not each frame (F5); the coax glow lifts at an amplifier to its own output, never chained (F6); the
  link budget's levels are translatable, and the waterfall opens on its station's floor, naming every band (F7).

## [0.16.0] - 2026-11-25 - Draft

The radio's own sounds

### Added

- **Radios make sounds now**, all kept low: a station's set gives a quiet squelch and static while it is in touch with someone; a
  linear amplifier's relay clacks in as it starts sending and out as it stops, and its fan runs while it sends; the radio terminal's
  computer hums whenever it has power; and pressing a hand-held's Radio button keys the set up, with its squelch tail. Made with
  generative AI and picked by the owner from two tries each (25 November 2026). D'thorga Framework's sound volume setting scales
  them all. Not yet tested in game: row 505.

### Compatibility and limits

- Safe with a running save; nothing is saved. No new settings.
- Each sound starts or stops only when a radio's state changes, worked out every 250 ticks as before, so a station or amplifier
  can take up to about four seconds to fall quiet after its last contact goes.

## [0.15.1] - 2026-11-25 - Draft

A coax icon that looks like coax

### Changed

- **The coax feedline's icon is redrawn**: a short bend of black cable with a screw-on connector at one end and its layers stripped
  back at the other, in place of the coil that read as a tyre. Made with generative AI and picked by the owner (25 November 2026).
  Not yet tested in game: row 495.

### Compatibility and limits

- Safe with a running save; only a picture changes. No new settings.

## [0.15.0] - 2026-11-25 - Draft

The remote control kit for any vehicle, and the vehicle's picture

### Added

- **The remote control kit is a fitting for any vehicle** with a D'thorga loadout (the owner's card of 25 November 2026): fit it at
  the vehicle workshop, in the control radio bay, after the research remote vehicle control. It costs what the service trucks' upgrade
  does, draws a little on the battery, and stops with it flat. With it, the L-ATV, the MaxxPro, the HUMVEE and the rest can be driven
  from a station. Not yet tested in game: row 493.
- **A vehicle driven from a station and drafted asks for its picture too**: two more channels of the vehicle picture kind at the
  same station. The picture needs a stronger signal than orders do, so it fails first as the vehicle goes further or behind cover.
  Its guns that no seat works fire only while the picture holds. Not yet tested in game: row 494.

### Compatibility and limits

- Safe with a running save: nothing new is saved. The fitting loads only with Vehicle Framework.

## [0.14.0] - 2026-11-25 - Draft

Vehicles driven from afar keep to radio cover

### Added

- **A vehicle driven from a station knows where its stations reach** (the owner's go-ahead of 25 November 2026, the autonomous vehicles record's step 8, with D'thorga Framework 0.279.0). While one is asked to be driven, the map is worked out a little each tick, one sample per 4 by 4 block, through the very sums every contact uses, from the vehicle's own radio to every powered station's antennas. The vehicle's routes then go round ground where it would lose its link. Whether a station has an operator is left out, so routes do not change each time the operator eats. A dev action, "Time radio cover for selected vehicle", times a whole pass. Not yet tested in game.

### Compatibility and limits

- **The first drive after asking may not use it**: the map takes some seconds to work out, and is worked out again after a station, an antenna, a wall, a roof or a noise source changes.

### Save compatibility

- Safe on an existing save: nothing new is saved, and no map component is added.

## [0.13.0] - 2026-11-25 - Draft

Signal bars on the Radio button

### Changed

- **The Radio button shows its signal as bars**: four bars rising left to right, lit orange for a marginal contact, yellow-green
  for a workable one and green for a clean one, and four grey bars under a red slash with no contact, no station or no power. The
  button's label now says the level in words ("Radio: workable") in place of the text bars. The same button on a vehicle's built-in
  radio shows them too. The bars are drawn by script, in the colours the owner picked. Not yet tested in game.

### Compatibility and limits

- Safe with a running save; nothing is saved. No new settings.
- The station's button keeps its antenna icon, and the Net tab and tooltips keep their text bars.

## [0.12.0] - 2026-11-24 - Draft

A colonist at the console: remote operation, and the research for the vehicle kit

### Added

- **A new work type, Remote operation**: a colonist assigned to it works a radio station while it serves a vehicle asked to be
  driven from it, standing at the console until food, sleep or something more urgent calls them away. **Turn it on for someone in
  the Work tab.** A station drives a vehicle only while a colonist works it; one colonist drives two vehicles at once, and a third
  waits for another station's operator. The station says who works it and how many vehicles it serves, and the Net tab and the
  vehicle's radio button say when a vehicle waits for an operator. Not yet tested in game: row 490.
- **A new research, remote vehicle control**, after software radio: D'thorga Vehicles' remote control kit for its service trucks
  needs it. Not yet tested in game: row 489.

### Compatibility and limits

- Safe with a running save: the operator's job saves whether they have reached the console. No new settings.
- When the operator leaves, the vehicle's letter says its link was lost, not why.

## [0.11.0] - 2026-11-24 - Draft

Driven from a station: a vehicle's radio takes its channel while it is driven

### Added

- **A station can drive a vehicle that carries the control kit**, with D'thorga Framework's remote control (its 0.278.0): while the
  player has asked for the vehicle to be driven from a station, its radio asks a control channel instead of standing by, and the
  vehicle is in touch for as long as a station serves it. The dev action that fits the radio for a session now fits the remote
  control with it. Not yet tested in game: rows 487 and 488.

### Compatibility and limits

- Safe with a running save: nothing new is saved by the radio. No new settings of the radio's own; the remote control's setting is
  D'thorga Framework's.

## [0.10.0] - 2026-11-24 - Draft

Radios fitted to vehicles: a vehicle's radio joins the net and shows its link

### Added

- **A vehicle can carry a radio of its own**, the second step of the remote control of vehicles the owner asked for on 24 November
  2026. With Vehicle Framework active, a vehicle that names a radio fitting is built with that radio, and once its upgrade is done
  it joins the net: its own signal button shows its bars, the station's Net tab lists it, and selecting it draws the line to the
  antenna. Until vehicles can be driven from a station it stands by and takes no channel. No vehicle carries one yet; a dev action
  fits one to a selected vehicle until the game is loaded again. Not yet tested in game: row 486.

### Changed

- **The station counts radios, not hand-helds**: "In touch: 2 of 3 radios", as a vehicle's radio counts too.

### Compatibility and limits

- Safe with a running save: nothing new is saved. No new settings. The Vehicle Framework part loads only with that mod.

## [0.9.0] - 2026-11-24 - Draft

Vehicle kinds: orders to vehicles can take a channel from talk

### Added

- **Two new kinds of channel use for vehicles driven from a station**, the first step of the remote control of vehicles the owner
  asked for on 24 November 2026: vehicle control (one channel shared by two vehicles) and vehicle picture (two channels for one
  vehicle). No radio uses them yet, so a station still lists only local voice; the vehicle kit comes in a later version.
- **A kind can take channels from others when every station is full**, as an emergency call does on a trunked radio system.
  Vehicle control does: it puts the weakest talking colonists off, no more than it needs, and they try the other stations in
  reach. Other kinds never do. Not yet tested in game: row 484.

### Compatibility and limits

- Safe with a running save: nothing new is saved. No new settings.

## [0.8.0] - 2026-11-24 - Draft

Channels by kind: the station counts each use of its channels apart

### Added

- **The channel ledger counts by kind**, as D'thorga Mechanised Specialist Expansion's BVR channels do (the owner's go of 24 November
  2026). Under its channels line a station lists each kind in use: "Local voice: 1 in use by 3 radios; room for 2 more". Each kind
  fills channels of its own, with its own mode setting how many one use takes and how many radios share them. Local voice is the
  only kind for now; other mods, and our coming remote control of vehicles, add kinds in XML. Not yet tested in game: row 478.

### Changed

- **The channels line says how many are free** instead of how many radios are served of how many could be; each kind's line now
  carries those counts.

### Compatibility and limits

- Safe with a running save: nothing new is saved. No new settings.

## [0.7.0] - 2026-11-24 - Draft

Channels: a station serves as many radios as its channels hold

### Added

- **Stations have channels, 25 kHz each, as on a trunked radio system** (the owner's go of 24 November 2026): the desk has one, the
  terminal four. Radios share a channel by taking turns, five to a channel for voice. A colonist in touch whose stations are all full
  reads "Radio: net busy" and gets no "in touch" mood; once served, a radio keeps its station. The station's text, button and Net tab
  count the channels. Not yet tested in game: rows 474 and 475.
- **The desk takes a second transceiver**, an upgrade for a second channel. Not yet tested in game: row 476.

### Changed

- **The waterfall puts each radio on the channel it really holds**, with the channels named under it, and the lamp and needle light
  for each served radio's turn. Not yet tested in game: row 477.

### Compatibility and limits

- Safe with a running save: nothing new is saved. No new settings.

## [0.6.0] - 2026-11-24 - Draft

The radio comes alive: a step chart, lamps and needles, a mast light, the coax glowing, and a waterfall

### Added

- **The link-budget window draws the budget as steps** (the owner's go of 24 November 2026): the power sent, each gain a step up and
  each loss a step down, to what arrives, with the noise and what the mode needs as lines. Operator shows the direction that
  decides; Engineer both. Not yet tested in game: row 468.
- **The desk and the amplifiers show when they send**: a red lamp lights and the meter's needle rises while the station is in use,
  for the fifth of the time voice is keyed; an amplifier's needle shows its output against its rating. Not yet tested in game:
  row 469.
- **The mast has a red warning light that flashes at night.** Not yet tested in game: row 470.
- **The coax glows with the signal it carries** while its station or an antenna is selected: bright at the station, dimmer along
  the run as the cable loses it, bright again past a powered amplifier. Not yet tested in game: row 471.
- **The radio terminal has a Waterfall tab**, as a software radio shows its band: each colonist's radio a streak as bright as it
  arrives, sending about a fifth of the time, over the noise the station hears, with the line voice needs. Each radio has its own
  spot on the band only to tell them apart; the radios have no channels yet. Not yet tested in game: row 472.
- **A new setting, Show the waterfall on the radio desk** (off by default, the owner's choice of 24 November 2026), gives the desk
  the Waterfall tab too. Not yet tested in game: row 473.

## [0.5.0] - 2026-11-24 - Draft

The radio terminal, linear amplifiers and a full-size ground-plane

### Added

- **The radio terminal**, a second station (the owner's go-ahead of 24 November 2026): a computer driving a software-defined radio, 10 W
  of its own with a very quiet receiver and a weak stub antenna. Its computer is itself a source of radio noise, so its antennas
  want to be a few metres away. Research software radio. Not yet tested in game: row 464.
- **Two linear amplifiers for the coax**: one up to 100 W, a heavy one up to 400 W. Each counts only for antennas whose cable passes
  through it, draws power and warms its room while its station is sending, and costs a little on receive. Not yet tested in game:
  row 465.
- **A full-size ground-plane**, about 2 dB better than the compact one for a little more steel; the taller pole fits both. Not yet
  tested in game: row 466.
- **A new setting, Radio terminals make radio noise** (on by default), to treat the terminal as quiet. Not yet tested in game: row 467.

### Changed

- **The whip is now called the compact ground-plane**, beside the new full-size one. Its figures are unchanged.

### Compatibility and limits

- Safe with a running save: nothing new is saved beyond the setting. Where the research screen puts the three new projects is not
  yet seen in game (row 464).

## [0.4.0] - 2026-11-24 - Draft

Upgrades for the radio desk and the antennas (experimental)

### Added

- **The radio desk, the whip and the mast take upgrades** (the owner's word of 24 November 2026, experimental): a receive
  preamplifier for the desk, a taller pole for the whip, a mast extension and a longer collinear for the mast. A builder fits each one
  from the building's Upgrades button. Not yet tested in game: rows 461 and 462.

### Changed

- **New pictures for the radio desk, the whip and the mast**, the owner's picks of 24 November 2026: a desk transceiver with its
  microphone, power supply, meter and logbook; a ground-plane whip with four radials; a guyed lattice mast with a white collinear on
  top. The antennas are drawn as the game draws trees, from above and a little in front, so they are drawn larger than their one
  cell, their guy wires and radials reaching past it. Not yet tested in game: row 463.

### Dependencies

- **Now needs D'thorga Framework**, for its building upgrades. Load it after D'thorga Framework.

### Compatibility and limits

- Safe with a running save: existing stations and antennas gain the Upgrades button. No new settings.

## [0.3.0] - 2026-11-24 - Draft

A line from each colonist's radio to its antenna, and a side view of the path

### Added

- **Selecting a colonist who wears a hand-held draws a line to the antenna their radio uses** (the owner's go-ahead of 24 November
  2026): coloured by the signal, thick where walls count, with a mark on each wall, door and patch of rock it meets. Operator adds
  the signal to spare, Engineer what each crossing cost. Not yet tested in game: row 459.
- **The link-budget window shows the path from the side**: the antenna and the colonist at their heights, wall height, the walls
  and rock between and the line over them. Not yet tested in game: row 460.

### Compatibility and limits

- Safe with a running save: nothing new is saved. No new settings.

## [0.2.0] - 2026-11-24 - Draft

Radio coverage painted on the map, and noise that fades out

### Added

- **Selecting the base station or an antenna paints its radio coverage on the map** (the owner's choice of 23 November 2026):
  brighter where a hand-held would have more signal to spare, dark red where it has no contact. It uses the same rules as a real
  contact, costs nothing until something is selected, and works only a little each frame. Not yet tested in game: row 456.

### Changed

- **Electrical noise now fades smoothly to nothing at the edge of its reach** instead of stopping dead (the owner's choice of 23
  November 2026). Links near the edge of a generator's reach get slightly better. Not yet tested in game: row 457.
- **The README no longer says the radio framework is on the back burner**: the owner reopened it on 23 November 2026, with
  performance as its first concern. No change in play.

### Compatibility and limits

- Safe with a running save: nothing new is saved. No new settings.

## [0.1.9] - 2026-11-21 - Draft

Radio's own Architect tab, and art records cleaned

### Changed

- **The radio base station, antennas and coax feedline are in their own Radio tab, inside the Architect's D'thorga tab** (the owner's choice of 21 November 2026, though radio is on the back burner: a tab only, nothing else changed). Without D'thorga Framework the Radio tab sits on the bar by itself. They were in the game's Misc tab.

### Compatibility and limits

- A layout change only. Safe with a running save. Not yet tested in game: row 301 of the test pass.

### Fixed

- **Art records no longer hold a path from the developer's PC.** The export records beside some textures named the full folder they were made in; they now name the file within the project. Nothing a player sees changes.

### Changed

- The README says the radio framework is on the back burner since 9 November 2026, and its dated checks note that RimWorld 1.6 was retired. No change in play.
- The README no longer repeats the player guide: the play test and how to read the signal are in the guide's Radio chapter. No change in play.
- The coax feedline atlas and its blueprint atlas have asset records: drawn in code on 19 October 2026, and meant to stay drawn by script. No change in play.

## [0.1.8] - 2026-11-13 - Draft

A clean background on the base station

### Fixed

- White left between the base station's parts is gone, and the signal button's edge is slightly cleaner. A fix to existing pictures only; the radio framework stays on hold.

### Save compatibility

- Safe to add to a running game: only pictures change.

### Compatibility and limits

- Not yet tested in game. Each picture was checked by eye on dark ground before and after, and the owner approved the set (13 Nov 2026).

## [0.1.7] - 2026-11-12 - Draft

The base station has its own picture, chosen by the owner on 12 November 2026. **Not yet tested in game.**

### Changed

- **A new picture for the base station**: a radio set with a screen and a hand microphone on a coiled lead, in place of the flat placeholder. Nothing changes in play. The whip antenna keeps its stand-in until work on the radio resumes (owner, 12 November 2026).
- The mod's description and README now carry the spelling dthorga beside D'thorga, so a search without the apostrophe finds it. The description says it is community-created by House D'thorga.

## [0.1.6] - 2026-11-09 - Draft

### Balance

- UHF's easier time now applies only to walls and built roofs, which it gets through by way of openings. Rock and natural roofs cost UHF the same as VHF, which also makes the siting preview's figure for them right on either band (owner's decision). **Not yet tested in game.**

## [0.1.5] - 2026-11-09 - Draft

### Fixed

- A base station with no power no longer makes contacts: hand-helds said in touch, and colonists kept the mood, with the station off. A hand-held now says the station has no power. **Not yet tested in game.**
- The false No storage connected alert on every feedline is gone, and coax no longer reads as storing zero decibels. **Not yet tested in game.**
- A generator out of fuel no longer raises the radio noise floor. **Not yet tested in game.**
- A new coax route, or a change to the scale of a cell, now takes effect at once instead of at the next reload. **Not yet tested in game.**
- A port with no cable says so. Clicking a station's gizmo opens its Net tab instead of deselecting it. A slave's radio no longer claims there is no station. The rock obstruction patch now takes effect. The antenna line names the strongest antenna, and a mast on the coax before anyone carries a radio. **Not yet tested in game.**
- Loading a game no longer keeps the previous game's map or pipe networks in memory. **Not yet tested in game.**

### Changed

- Dates in this mod's records that were written a day ahead now read 8 November 2026. Text only; nothing in game changes.

## [0.1.4] - 2026-11-09 - Draft

### Fixed

- Loading a game within a session no longer keeps the previous game's pipe networks, and through them its maps, in memory. **Not yet tested in game.**

## [0.1.3] - 2026-11-08 - Draft

No change in play on 1.7. Not yet tested in game.

### Compatibility and limits

- RimWorld 1.7 only: the 1.6 build, its load folder and 1.6 in the About file are gone (owner, 8 November 2026: 1.6 is a dying version). Players on 1.6 keep the last version that had it.
- The About file now lists 1.7, so the mod list no longer calls it outdated on 1.7.

### Changed

- Generated art the owner picked (7 November 2026) in place of five placeholders: the hand-held radio, the mast (a guyed lattice tower), the signal and feedline icons, and the whip antenna, which uses a thin lattice mast as a stand-in until a whip is picked. The mast and whip are drawn at their real shape, tall and narrow, instead of a square. Each passed the near-copy check; the record beside each texture says how it was made. The base station and the feedline keep their placeholders.
- The Workshop preview is a generated background the owner picked, with the current name added by script.

### Fixed

- The assembly's own version said 0.1.1 since 0.1.2; it now matches. No change in play.

### Compatibility and limits

- README brought up to date: the supported versions (a build for 1.7 and 1.6; About.xml still lists 1.6 only), no Harmony
  needed, the settings' range and the mood switch, the one-step rebuild, and a diagram of what decides a contact.

## [0.1.2] - 2026-11-05 - Draft

No change in play. Not yet tested in game.

### Changed

- Messages use named placeholders, so translators can see what each one means.
- The description now carries the disclaimer in the wording Ludeon's EULA requires. It said only that the mod is not official and not endorsed. No behaviour change.

## [0.1.1] - 2026-11-04 - Draft

A native RimWorld 1.7 build, which now leads (owner, 4 November 2026); the 1.6 build stays as the minimum backwards support
until 1.6 is retired. No behaviour change. Not yet tested in game on either version since this build.

### Changed

- The assembly is built twice, against the 1.7.4998 reference on .NET 10 and against the 1.6 reference as before;
  the game loads its own version's folder (1.7/Assemblies or 1.6/Assemblies). Until now 1.7 loaded the 1.6 build.

### Compatibility and limits

- The 1.7 build references Vanilla Expanded Framework's own 1.7 PipeSystem assembly, the 1.6 build its 1.6 one.

## [0.1.0] - 2026-10-19 - Draft

Phase 1 of the design: local VHF and UHF on one map. Built and deployed to the owner's game on
19 October 2026; the owner's play test is what decides whether the 6 dB rule feels right.

### Added

- The link-budget window (click a radio's gizmo, or a row of the station's Net tab): opens at your
  display mode and expands one level deeper on request, following the pawn while open.
- The station's Net tab: every worn hand-held against this station, with its outcome, the reason or
  the Operator summary, and which station is each pawn's best.
- The siting preview while placing an antenna or the station: antenna height, the roof over the cell
  and what it costs, and the colony noise there. Warns under thick roof; never blocks.
- Dev-mode actions under "Phobos Radio": log the selected pawns' full sheets, log the noise at a cell,
  recompute the net now. All read-only.
- Coax feedline on Vanilla Expanded Framework's pipe network: lay it like power conduit from the base
  station to an antenna (2 steel per cell, under walls if need be). The station walks the cable to each
  antenna and charges the run's loss: 8 dB per 100 m on VHF, rising with the square root of frequency.
- Whip antenna (3 m, no gain, 15 steel) and mast antenna (10 m, 3 dBi, 60 steel + 1 component). The
  station uses whichever antenna on its coax gives the best link, or its built-in whip; the inspect text
  names the one in use. Ports say whether coax is connected and what is on it.
- Local radio: the research project (after Microelectronics Basics), two hand-held radios (VHF and
  UHF, utility apparel on the belt slot, 5 W) and the radio base station (2 x 2, 60 W idle, 25 W
  transmitter with a built-in whip; draws more power and warms the room while in use).
- The link budget decides every contact in both directions, on the best band both ends share: free-space
  loss by distance, walls and doors on the straight line (counted only where that line is below 2.5 m,
  so a tall antenna sees over them), natural rock per cell, the roof over each antenna (overhead
  mountain is near-total), and the colony's electrical noise at the receiver.
- "In touch with home": a small mood thought for a free colonist whose worn hand-held has a workable
  link to a base station (+2), or a faint one (+1). Off by a setting if you prefer a pure readout.
- A signal-bars gizmo on every worn hand-held and on the station, with a Plain, Operator or Engineer
  description by the display-mode setting; clicking shows the full sheet.
- Band, mode and rules Defs, and two Def extensions other mods can use: obstruction loss on materials
  (patched onto the vanilla stuffs and natural rock) and noise on buildings (patched onto the vanilla
  generators and the battery).
- Mod settings: display mode, metres per cell, the thought on or off.
- Placeholder sprites for the hand-held, the station and the gizmo icon, drawn by a script.
- The mod skeleton: About.xml (packageId PhobosDthorga.RadioFramework, RimWorld 1.6), load folders for
  1.6 and 1.7 (1.7 borrows the 1.6 build), a placeholder preview image, this changelog and the README.
- The game-free physics core (decibels, free-space path loss, noise floor, coax loss, ray clearance,
  the link-budget sheet and the margin outcomes), with unit tests that run without the game.
- The build project (RimWorld 1.6, .NET Framework 4.7.2, PolySharp) and scripts/deploy.py, which copies
  the mod into the game's Mods folder with RimWorld closed.

### Balance

- Every figure is an authored starting value unless its Def comment names a source: 12 dB for FM voice
  (the SINAD convention), 0.6 transmitter efficiency, wall losses of 6 (wood) to 30 (plasteel) dB per
  crossing, 30 dB per cell of rock, roof losses of 6/20/60 dB, generators at -60 dBm at 1 m.

### Compatibility and limits

- A hand-held takes the belt slot, so a colonist cannot wear one with a shield belt.
- Only free colonists count for "in touch"; slaves, prisoners, guests and caravans do not. Downed
  colonists do. A radio in a pawn's inventory does nothing; it must be worn.
- Requires Vanilla Expanded Framework (declared in About.xml). Nothing from VEF ships with this mod.
- Coax water-ingress degradation (in the design) is not built yet.
- The coax sprite is a placeholder atlas; if its tiles look rotated, that is the atlas, not the net.
- Safe to add to a running save. Remove it only after deconstructing the station and dropping every
  hand-held; expect one harmless "could not find class" error on the next load.
- Metres per cell: decide it before starting a colony; changing it mid-save changes every distance.
- Not yet tested in game.
