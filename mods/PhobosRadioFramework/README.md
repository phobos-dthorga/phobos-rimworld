# D'thorga Radio Framework

Radio that works like radio: every contact is decided by a real link budget, and doubling the distance costs 6 dB. This is **Phase
1**: local VHF and UHF on one map, with a first step beyond it from 0.21.0: a convoy's reach home on the world map.

One of House D'thorga's mods (also spelt dthorga).

Version 0.67.0, built 28 November 2026 for House D'thorga. **Not yet tested in game.** It was built and deployed on 19 October
2026, but the Def loading, the coax net, the gizmos and the mood have not been seen running.

## Read this first (gotchas)

- **Not yet tested in game.** The owner has not passed any of it yet: it builds and installs, but its buildings, its coax network,
  its buttons and its mood have not been seen running in a colony.
- **Two stations, from 0.5.0**: the radio base station (25 W, a built-in whip) and the **radio terminal** (10 W, a very quiet
  receiver, a weak stub, and **its own computer noise**, which the setting *Radio terminals make radio noise* turns off).
  **Linear amplifiers only help antennas whose coax passes through them.**
- **Needs D'thorga Framework and Vanilla Expanded Framework.** The coax is Vanilla Expanded Framework's pipe network; the upgrades
  are D'thorga Framework's. Load it after Core, the DLCs, Harmony, Vanilla Expanded Framework and D'thorga Framework; its About
  file asks for that order, so RimSort sorts it for you. Harmony comes with D'thorga Framework.
- **RimWorld 1.7 only.**
- **On the world map (from 0.21.0) a tile counts as 1 km for radio**, a gameplay choice: a convoy with a radio fitted reaches a home
  station with a mast antenna from about 20 tiles, 26 with a Datum's mast up. Needs D'thorga Framework's vehicles.
- **Relays, from 0.22.0**: a halted convoy or a vehicle at a depot with the **repeater kit**, or a relay mast, passes calls on, up to two
  relays; hilltops reach further. **From 0.24.0** a relay has two channels (five convoys talking, or two driven trucks), a colony's base
  station relays with the **repeater** upgrade, and a **radio relay post** can be founded from Create outpost (needs an outposts mod).
- **Decide before you start a colony: Metres per map cell** (default 1, from 0.5 to 30). It changes every distance and coax loss,
  so changing it in a running colony changes which radios reach each other.
- **HF (0.62.0, by the sky from 0.64.0)**: an HF station reaches far settlements when a band is open, by the hour, the season and the
  sun's cycle; along the ground a tile or two. Settlements answer on HF, so faction features reach much further. The HF tables add
  about 3.3 MB to the mod.
- **The hand-held is worn on the belt slot**, so a colonist can't wear a shield belt and a hand-held together. **From 0.67.0 one
  carried in the inventory works as a worn one** (a Combat Extended loadout puts it there): wear it or carry it, not both needed.
- **Channels, from 0.7.0**: each station serves as many radios as its channels hold (the desk 5, the terminal 20, five to a
  25 kHz channel for voice). Past that, a colonist in touch reads **net busy** and gets no "in touch" mood: add a station or a
  second transceiver. **From 0.8.0** the station lists each kind of use apart (local voice for now), with the room left for it. **From 0.9.0**
  the ledger knows two kinds for vehicles driven from a station, whose orders can take a channel from talk. **From 0.10.0**,
  with Vehicle Framework, a vehicle can carry a radio that joins the net and shows its link; **from 0.11.0**, with D'thorga
  Framework's remote control, a station drives it with nobody aboard; **from 0.12.0** a colonist assigned to **Remote operation**
  must work the station (two vehicles each), and D'thorga Vehicles' service trucks take the kit after **remote vehicle control**.
- **The upgrades are experimental**: the desk, the whip and the mast each have an **Upgrades** button.
- **Safe to add to a running colony.**
- **Safe to remove once you deconstruct every radio building and drop every hand-held.** The first load afterwards shows one
  harmless "could not find class" error. Every mod's steps are in [Removing a
  mod](../../docs/player-guide/getting-started.md#removing-a-mod).

## What it adds

How to build, read the signal and try it out are in the player guide's [Radio chapter](../../docs/player-guide/radio.md); the
figures are here.

- **Hand-held radios**, VHF and UHF, a **base station** with a built-in whip, **coax feedline**, and **whip and mast antennas**,
  after the **Local radio** research.
- **A radio terminal**, **linear amplifiers** of 100 W and 400 W for the coax, and a **full-size ground-plane** (0.5.0), after
  **Software radio**, **Linear amplifiers** and **High-power amplifiers**.
- **A remote gun turret** (0.18.0), after **Remote turrets**: a .50 on a pedestal, connected to a station by coax or, from 0.20.0, in its own radio's reach, firing while a colonist
  at the station's console watches it ([the guide](../../docs/player-guide/radio.md#what-to-build)). With Combat Extended (0.19.0) it
  fires .50 BMG from a magazine colonists reload.
- **A link budget for every contact**, both directions, the worse one deciding: workable, faint or no contact.
- **What a colonist sees**: a Radio button with signal bars and the reason, the station's **Net** tab, coverage painted on the map
  when the station or an antenna is selected, and a line from a selected colonist to the antenna they use.
- **Signs of life (0.6.0)**: the link-budget window draws the budget as steps; the desk and amplifiers light a lamp and swing a
  needle while sending; the mast flashes a red light at night; the coax glows with its signal while its station is selected; the
  radio terminal has a **Waterfall** tab (the desk too, with the setting *Show the waterfall on the radio desk*, off by default).
- **An "In touch with home" mood** for a colonist whose radio reaches a base station.
- **Jamming (0.45.0)**: a jammer switched to radios (the Busbar's jammer mast, with D'thorga Vehicles) drowns every radio for a long
  way around, **your own included**; Morse and a radio close to its station hold out longest. **From 0.46.0, with The Dead Man's
  Switch**, it also stops an enemy base's reinforcement uplink and fire support uplink calling for help when the alarm trips
  ([the guide](../../docs/player-guide/radio.md#jamming)).
- **Radios in every vehicle (0.49.0, with Vehicle Framework)**: a **Comms** system on every powered vehicle's Systems tab (radio fitted?
  in touch?), radio bays on aircraft and boats, and radios from the factory: an **aviation radio** in every aircraft, a **dual-band radio**
  in military vehicles, and (0.57.0) a **marine radio** in every new boat, on the **marine band**: a station needs the **marine
  transceiver** and a mast's **marine antenna** to hear boats ([the guide](../../docs/player-guide/radio.md#talking-to-boats)). Aircraft talk on the **air band and military UHF**: a station needs the **aviation transceiver** and a
  mast's **aviation antennas** to hear them ([the guide](../../docs/player-guide/radio.md#talking-to-aircraft)).
- **Advice by radio (0.23.0)**: a more skilled colonist in touch talks another through tending, an operation, a vehicle repair or
  trade talks, at home or from a convoy in reach: half the skill gap closed, a quarter on a marginal link, a little slower
  ([the guide](../../docs/player-guide/radio.md#advice-by-radio)). A **numbered medical chest** closes a sixth more for medicine.
  **From 0.26.0** the carer learns the skill half again as fast while talked through, and when none of your colonists can help, an
  allied or neutral faction's doctor in radio reach may (its mechanic too, for vehicle repairs, from 0.27.0): allies free once a day,
  neutral factions for 3 goodwill.
- **A radio friend (0.29.0)**: a colonist with a radio chats as recreation with a named friend at another faction's settlement in reach,
  for a mood lift that grows with the friendship and a little goodwill ([the guide](../../docs/player-guide/radio.md#a-radio-friend)).
- **School of the Air (0.35.0, needs Biotech)**: children learn at radio school, at a station's desk or on a hand-held, and take
  lessons from a teacher in radio touch, even one away with a convoy ([the guide](../../docs/player-guide/radio.md#school-of-the-air)).
- **Neighbours ask for yours (0.36.0)**: now and then a friendly neighbour in radio reach asks for your best doctor or mechanic on the
  radio for an hour, for 5 goodwill ([the guide](../../docs/player-guide/radio.md#neighbours-ask-for-yours)).
- **The station logbook (0.38.0)**: a Log tab on the station with radio friends, settlements in reach, every call, and QSL cards
  drawn in each faction's colours; awards for sets of cards lift the mood of those who work the radio
  ([the guide](../../docs/player-guide/radio.md#the-logbook-and-qsl-cards)).
- **Calling other factions on the radio (0.53.0)**: right-click a radio station with a colonist selected to call a faction your radio
  reaches: a trader caravan (sooner from a near settlement, cheaper on a clean line), military aid, or a check-in. A marginal line can
  mishear; hostile listeners may ambush the trader. Allies in reach warn of raids. **Two settings, both on.** The console is untouched.
- **Seats at the radio (0.51.0)**: the base station has a seat in front of it and the terminal two; put a chair there. **A station built
  earlier may have something standing on its new seat**: its pane says so; clear the spot or move the station. A call at the desk now
  takes a channel, as a hand-held does.
- **The path home from the side, a relay planner, full relays marked (0.48.0)**: a caravan's **Radio** button on the world map shows
  each hop home in side view with the ground that decides; the Radio coverage menu gains a **planner** for a new relay mast or post;
  a relay with every channel in use is marked in pink.
- **Caravans on foot call home by hand-held (0.44.0)**: a hand-held worn or packed in the caravan reaches a station about 12 tiles
  off on flat ground, more from hills and through relays; in reach, its colonists feel in touch and it can take airdrops and a
  medevac. Needs Vehicle Framework.
- **Radio coverage on the world map (0.42.0)**: select a colony with a station, a relay mast, a relay post or a convoy with a radio,
  press **Radio coverage** and pick the far-end radio; the globe is coloured by signal to spare. From 0.43.0 **Home, through
  relays** paints everywhere a call could get home from. **Off until you pick one**
  ([the guide](../../docs/player-guide/radio.md#convoys-on-the-world-map)).
- **High ground blocks radio between tiles (0.41.0)**: hills and mountains on the way between a convoy and home cost signal. **A
  convoy that reached home over hills before may not now**; a higher aerial, a relay on the high ground or a way round gets through
  ([the guide](../../docs/player-guide/radio.md#convoys-on-the-world-map)).
- **The Morse key (0.40.0)**: a station upgrade and a vehicle fitting that keep a convoy in touch by Morse three to four times as far as
  voice, with advice at a quarter of the gap ([the guide](../../docs/player-guide/radio.md#morse)).
- **HF radio (0.62.0)**, after **HF radio**: an HF station (100 W, SSB voice on 40 m and 20 m), three kinds of dipole for each band
  and a manpack. Between tiles HF goes by the ground wave over each end's ground (ITU-R P.368, by NTIA's LF/MF model), with the
  sky's noise by the sun's cycle, season and hour (VOACAP's predictions). **Masts (0.64.0)**: a dipole's Upgrades button raises
  it on a telescopic mast (15 m) or a lattice tower (25 m): higher for far places by the sky, lower for the region round you.
- **Upgrades** for the desk and the antennas, fitted by builders.

| Thing | Where | What it is |
| --- | --- | --- |
| Hand-held radio (VHF) and (UHF) | Machining table, 20 steel + 1 component | 5 W, worn on the belt, ten to a stack. VHF reaches further outdoors; UHF copes better with walls |
| Radio base station | Architect → D'thorga → Radio, 100 steel + 3 components | 2 × 2, needs power (60 W idle, more while in use), both bands, built-in whip, 25 W |
| Coax feedline | Architect → D'thorga → Radio, 2 steel per cell | Laid like power conduit, from the station to an antenna. Runs under walls |
| Compact ground-plane (the whip) | Architect → D'thorga → Radio, 15 steel | 1 × 1, 3 m high, no gain |
| Full-size ground-plane (0.5.0) | Architect → D'thorga → Radio, 25 steel | 1 × 1, 3 m high, about 2 dBi: a full quarter-wave radiator |
| Mast antenna | Architect → D'thorga → Radio, 60 steel + 1 component | 1 × 1, 10 m high, 3 dBi. Sees over walls |
| Radio terminal (0.5.0) | Architect → D'thorga → Radio, 120 steel + 6 components + 1 advanced component | 3 × 2, 150 W, 10 W out, a 2 dB receiver, a weak stub antenna (about −5 dBi at 1 m); its computer makes noise out to 8 cells |
| Linear amplifier (0.5.0) | Architect → D'thorga → Radio, 40 steel + 2 components | 1 × 1, in the coax: ten times what the station sends, up to 100 W; 20 W on standby, more while sending; 0.5 dB lost on receive |
| Numbered medical chest (0.23.0) | Drug lab, 20 steel + 15 cloth + 1 medicine | An item: carried, in a convoy or kept nearby, it lets advice by radio close a sixth more of the skill gap for medicine. Never used up |
| Slow-scan camera kit (0.39.0) | Machining table, 15 steel + 3 components | An item: at hand, it lets advice by radio close a sixth more of the gap on a clean link, for tending, surgery and vehicle repairs. Never used up |
| Heavy linear amplifier (0.5.0) | Architect → D'thorga → Radio, 80 steel + 4 components + 1 advanced component | The same, up to 400 W; 40 W on standby, four times the draw and heat while sending |

**What decides a link**, with its figures:

- **Distance**, by the inverse-square law: doubling it costs 6 dB.
- **Walls and closed doors on the straight line**, by material (wood least, plasteel most), only where that line is below 2.5 m.
- **Natural rock**, 30 dB a cell, whatever the heights.
- **The roof over each antenna**: a built roof a little, thin mountain roof a lot, overhead mountain almost everything.
- **Coax length**: 8 dB per 100 m on VHF, more on UHF.
- **Your own generators and batteries** raise the noise near them, fading smoothly to nothing over the last third of their reach:
  12 cells for a generator, 6 for a battery (an authored simplification).

**Coverage** counts every door as closed and takes the better of the VHF and UHF hand-helds on each spot. It costs nothing until
something is selected; then it works for 1.5 ms of each frame, and again only when a wall, roof, antenna or noise source changes.
**The line on the map** follows the colonist about once a second, for up to 6 selected colonists.

| Upgrade | Fits | What it changes | Cost |
| --- | --- | --- | --- |
| Receive preamplifier | Radio desk | Its receiver from 6 dB to 3 dB of noise: hears faint hand-helds better where it is quiet | 10 steel, 1 component |
| Taller pole | Whip | 3 m to 5 m: clears more walls | 20 steel |
| Mast extension | Mast | 10 m to 15 m: clears walls for longer | 40 steel |
| Longer collinear | Mast | 3 dBi to 6 dBi of gain (approximate): about twice the signal both ways | 20 steel, 1 component |

An upgrade can be taken off again for about half its materials back.

## Settings

In **Options > Mod settings > D'thorga Radio Framework**.

| Setting | What it does | Default |
| --- | --- | --- |
| Metres per map cell | What one map cell measures. **Decide before you start a colony**: changing it later changes every distance and coax loss, so links that worked may stop. 1 m matches the furniture, about 26 m matches walking speed | 1 (0.5 to 30) |
| "In touch with home" mood | A small mood bonus for a free colonist wearing a hand-held with a working link to a base station. Off keeps the radios a pure readout | On |
| Colonists talk on the radio by themselves | Colonists with a hand-held in touch chat across the map now and then (most often when working alone), call someone close who is away in a caravan in the evening, and keep a colonist in bed company through the night. A colonist's hand-held lights the station's lamp only while they really talk. Off, the radio is quiet as before | On |
| Display mode | How much of the working to show: **Plain** (bars and a reason), **Operator** (adds the band and the margin) or **Engineer** (every term). The outcome is the same in each | Plain |
| Other factions' doctors and mechanics give advice by radio | When none of your colonists can help, a doctor or mechanic at another faction's settlement in radio reach talks your colonist through tending, an operation or a vehicle repair: allies free once a day, a neutral faction for 3 goodwill a call, never below 0 | On |
| Neighbours ask for help by radio | About once a fortnight, a friendly neighbour in radio reach asks by letter for your best doctor or mechanic on the radio for an hour. Yes earns 5 goodwill once the hour is done; no costs nothing | On |
| Radio advice during surgery | A more skilled colonist in radio touch can talk a surgeon through an operation, raising its chance of success; a dropped call leaves the surgeon at their own skill | On |
| Jamming reaches radios | A jammer switched to radios (the Busbar's jammer mast, with D'thorga Vehicles) drowns every radio for a long way around, **your own included**: talk, remote-driven vehicles and radio-linked turrets fail, while a radio close to its station, a strong one or Morse gets through. Off, jammers blind radar only | On |
| The comms console needs radio reach | On, the comms console lists only the factions your stations reach by radio (through relays and, with HF, by the sky) | Off |
| Our station's callsign | The callsign on every QSL card the colony receives: letters, digits and a stroke (VK3ABC/P), up to ten. Empty works one out from the colony's place on the planet | Empty |

## More

- [The player guide's Radio chapter](../../docs/player-guide/radio.md): building, reading the signal, a short try-out, and what is
  tested in game.
- [The changelog](CHANGELOG.md): what changed in each version.
- [For modders](../../docs/modding/radio-framework.md): radios, bands, antennas and noisy machines in XML, the dev actions, and
  the checks done.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of
Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
