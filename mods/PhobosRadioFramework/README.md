# D'thorga Radio Framework

Radio that works like radio: every contact is decided by a real link budget, and doubling the distance costs 6 dB. This is **Phase
1**: local VHF and UHF on one map.

One of House D'thorga's mods (also spelt dthorga).

Version 0.20.1, built 26 November 2026 for House D'thorga. **Not yet tested in game.** It was built and deployed on 19 October
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
- **Decide before you start a colony: Metres per map cell** (default 1, from 0.5 to 30). It changes every distance and coax loss,
  so changing it in a running colony changes which radios reach each other.
- **The hand-held is worn on the belt slot**, so a colonist can't wear a shield belt and a hand-held together.
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
- **Upgrades** for the desk and the antennas, fitted by builders.

| Thing | Where | What it is |
| --- | --- | --- |
| Hand-held radio (VHF) and (UHF) | Machining table, 20 steel + 1 component | 5 W, worn on the belt. VHF reaches further outdoors; UHF copes better with walls |
| Radio base station | Architect → D'thorga → Radio, 100 steel + 3 components | 2 × 2, needs power (60 W idle, more while in use), both bands, built-in whip, 25 W |
| Coax feedline | Architect → D'thorga → Radio, 2 steel per cell | Laid like power conduit, from the station to an antenna. Runs under walls |
| Compact ground-plane (the whip) | Architect → D'thorga → Radio, 15 steel | 1 × 1, 3 m high, no gain |
| Full-size ground-plane (0.5.0) | Architect → D'thorga → Radio, 25 steel | 1 × 1, 3 m high, about 2 dBi: a full quarter-wave radiator |
| Mast antenna | Architect → D'thorga → Radio, 60 steel + 1 component | 1 × 1, 10 m high, 3 dBi. Sees over walls |
| Radio terminal (0.5.0) | Architect → D'thorga → Radio, 120 steel + 6 components + 1 advanced component | 3 × 2, 150 W, 10 W out, a 2 dB receiver, a weak stub antenna (about −5 dBi at 1 m); its computer makes noise out to 8 cells |
| Linear amplifier (0.5.0) | Architect → D'thorga → Radio, 40 steel + 2 components | 1 × 1, in the coax: ten times what the station sends, up to 100 W; 20 W on standby, more while sending; 0.5 dB lost on receive |
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
| Display mode | How much of the working to show: **Plain** (bars and a reason), **Operator** (adds the band and the margin) or **Engineer** (every term). The outcome is the same in each | Plain |

## More

- [The player guide's Radio chapter](../../docs/player-guide/radio.md): building, reading the signal, a short try-out, and what is
  tested in game.
- [The changelog](CHANGELOG.md): what changed in each version.
- [For modders](../../docs/modding/radio-framework.md): radios, bands, antennas and noisy machines in XML, the dev actions, and
  the checks done.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of
Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
