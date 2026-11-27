# Radio

**D'thorga Radio Framework** gives colonists hand-held radios and a base station, joined to antennas by coax. Every contact is
decided by a real link budget: distance, walls, rock, roofs and electrical noise all count, and doubling the distance costs 6 dB.
This is the first step, local radio on one map. [Back to the guide](README.md).

![A colonist with a hand-held radio on a hillside, a lattice mast and a lit cabin behind](../../mods/PhobosRadioFramework/About/Preview.png)

## Read this first (gotchas)

- **Needs D'thorga Framework and Vanilla Expanded Framework** (the coax is the latter's pipe network; the upgrades are the former's).
- **The radio terminal is noisy by design**: its computer drowns antennas within a few metres, its own stub included. Site antennas
  away from it, or switch **Radio terminals make radio noise** off in the mod's settings. **Amplifiers only help antennas whose coax
  passes through them.**
- **A station serves only as many radios as its channels hold**: the desk five (ten with its second transceiver), the terminal
  twenty. Past that, a colonist in reach reads **Radio: net busy** and misses the "in touch" mood.
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
| Remote gun turret | Architect, D'thorga, Radio: 120 steel, 6 components, 1 advanced component; after **remote turrets** | A .50 that **fires only while a colonist at a station watches it**: connect it to the station **with coax**, or put it **within radio reach** of one (it then needs its picture through the air, so walls and distance count). That colonist can aim it. **While enemies are about, a colonist with Remote operation takes the console.** It takes one of their two hands, after any vehicles they drive. Haulers feed its belt with steel (**with Combat Extended, colonists reload it with .50 BMG**); it needs power |

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
Select the station afterwards and the coverage shows what it bought. **A second station, and amplifiers:** the **radio terminal**
(research software radio) is a computer driving a software-defined radio, only 10 W but able to hear faint hand-helds the desk
misses; its little stub antenna works poorly, and its computer makes noise that spoils antennas close to it, so run coax to a whip
or mast a few metres away. A **linear amplifier** (up to 100 W) or **heavy linear amplifier** (up to 400 W) joined into the coax
raises what either station sends, for the antennas whose cable runs through it, and draws power and warms its room while the
station is sending. The **full-size ground-plane** is a slightly better whip.

**Channels.** Each station has channels, 25 kHz each, as a trunked radio system does: the desk one, the terminal four. Radios
share a channel by taking turns, five to a channel for voice. The station's text and **Net** tab say how many are in use and who is
on which channel; a colonist with no free channel reads **net busy**. A vehicle with a radio fitted is listed too, as standing
by until you ask for it to be driven from a station; then it takes a control channel, and **a colonist assigned to Remote
operation must work the station** (one each, driving two vehicles at once). How driving from a station works is in [vehicles on the road](vehicles-on-the-road.md#driven-from-a-station). Under the count, a line for each kind of use (local voice
for now) says how many channels it holds, for how many radios, and how many more it has room for. The desk's **second transceiver** upgrade adds a channel.

**Signs of life.** The desk's lamp lights and its needle swings while it sends; an amplifier's needle shows its output; the
mast flashes red at night. Select a station and its coax glows, dimming along the cable as it loses the signal. The radio
terminal's **Waterfall** tab (the desk's too, with the setting **Show the waterfall on the radio desk**, off by default) shows each
colonist's radio as a streak, brighter for a stronger signal, over the noise it hears; at
Operator and Engineer the link-budget window adds the budget as steps. A colonist in good contact gets a small **in touch with home** mood, and a fainter one on a weak
link. **While placing** an antenna or the station, the cursor shows its height, the roof and the noise there; red means overhead
mountain.

**How much of the maths to show** (its settings): **Plain** shows bars and one reason in plain words; **Operator** adds the band, the
signal-to-noise ratio, the margin and which direction limits the link; **Engineer** shows every term. The window a radio's button
opens can show one level more than your setting.

## Convoys on the world map

**A convoy with a radio fitted** (the remote control kit on any of its vehicles) **can reach home** while it is near: the convoy's line
says whether it is in reach, how far home is, and the horizon of the two aerials. A powered **radio station** at home with a **mast
antenna** reaches furthest. **For radio, a world tile counts as 1 km** (a gameplay choice), so a convoy reaches home from about **20
tiles**, or **26 with a Datum's mast up** (Raise the mast, at a halt). In reach:

- **Home warns of ambushes** ahead: each way out is a little likelier.
- **Calls pass through relays**: a convoy of yours **standing still**, or a vehicle **left at a depot**, with a radio and the **repeater
  kit** fitted (a Datum's mast lifts it), or a **relay mast** (Put up a relay mast, on a stopped convoy: 60 steel and 4 components). Up to
  two relays; the convoy's line names them. A relay mast stands until you take it down from the world map. **Hills reach further**: small
  hills lift an aerial 30 m, large hills 100 m, mountains 300 m.
- **See reach on the world map** (from 0.37.0): select a convoy, a relay mast or a relay post there. A ring shows how far its radio
  reaches a home station's mast on level ground; a convoy also shows its chain home, hop by hop, **the weakest hop in amber**.
- **Relay masts wear and can be raided** (from 0.37.0; a setting, **off by default**): on, a mast stops passing calls on after about a
  year until a convoy on its tile presses **Service the relay mast** (one component), and raiders near their own lands may knock it
  down. **A worked example of the whole network:** [Setting up a supply network](supply-network.md).
- **A radio relay post** (from 0.24.0): an outpost you found from **Create outpost** with **120 steel and 8 components**, leaving at
  least one colonist to keep it. A 20 m mast and four channels; **put it on a hill**. With nobody left there, it passes nothing on.
- **Your other colonies' stations** pass calls on once their base station has the **repeater** upgrade (25 steel, 3 components, after
  remote-controlled vehicles). This only helps a call meant for one colony, such as **advice by radio** from a doctor there: a call that
  reaches any of your colonies is home already.
- **A relay has only so many channels** (from 0.24.0): a repeater has two, enough for **five convoys talking by turns, or two vehicles
  driven by radio**; a relay post has four. **Orders to a vehicle push talk aside.** A full relay is passed over, and the convoy's line
  says so; the relay's own pane says how busy it is.
- **An aircraft can drop to the convoy** (Airdrop to the convoy, when choosing where to fly): the convoy talks it in, and everything
  dropped joins it.

### Talking home from a camp or a site (from 0.25.0)

**Fit the dual-band radio** to a vehicle (a voice radio bay every land vehicle has; 12 steel, 2 components, after local radio; about
2 A on the battery). **A colonist's hand-held, VHF or UHF, talks home through it** from anywhere on the same map it reaches, through any
relay, as a real cross-band set does. **Your colonies' maps don't need it**: there, the station serves. **From 0.34.0** colonists there
get the **in touch with home** mood through it, and the vehicle's pane says whom it passes calls home for.

## Advice by radio

**A more skilled colonist can talk another through their work by radio**: tending, an operation, a repair of one of our vehicles, or
trade talks. Nothing to press: it happens by itself when both are in radio touch.

- **Who gives advice**: a colonist more skilled in the work, with that work **switched on in the Work tab** (Doctor for tending and
  operations, Construction for repairs; anyone not incapable of social work for trade), awake, and not busy with something more
  urgent. They stop and take the call **on a hand-held where they stand**, **in bed** if they are resting there, or **at a radio
  station's desk** if they carry no radio.
- **Who is helped**: a colonist wearing a hand-held in touch with a station, or anyone in a **convoy whose radio reaches home**, or
  (from 0.25.0) a colonist with a hand-held **on a camp, site or ambush map** within reach of a vehicle of yours with the **dual-band
  radio** fitted, when that vehicle reaches home.
- **How much**: the carer works as if **half the gap** to the adviser's skill were closed, a **quarter** on a marginal link, a little
  slower while every step is spoken. A Medicine 3 colonist talked through by a Medicine 16 doctor works as Medicine 9. The carer's
  stat card shows the help.
- **A numbered medical chest** (drug lab, after local radio and medicine production: 20 steel, 15 cloth, 1 medicine), carried, in the
  convoy or kept nearby, closes **a sixth more** for medical advice. It is never used up.
- **Repairs**: a repair that needs more Construction than your mechanic has can be done while someone better talks them through it,
  at home or with the convoy's repair button.
- **On the road and in trade talks**, which have no job of their own, the adviser stays on the radio for a while afterwards: about
  half an hour after a convoy's tend, an hour after a repair or trade.
- **If the call drops** (out of touch, or the adviser called away), the carer carries on alone, at their own skill.
- **Surgery talked through** has its own setting, **Radio advice during surgery**, on by default.
- **The carer learns while talked through**: the skill rises half again as fast during the call, and the adviser gains a little Social.
- **Other factions' doctors and mechanics**: when none of your colonists can help with tending, an operation or a repair of one of our
  vehicles, a doctor or mechanic at another faction's settlement in radio reach may. **Allies help free, once a day**; a **neutral** faction asks **3 goodwill** a call and never takes its
  goodwill below 0; hostile factions and those without radio (tribes, medieval kingdoms) never help. Masts, hilltops and relays bring
  more settlements into reach. A call stays open for an hour after its last use, so a convoy's tends an hour apart cost once. Setting:
  **Other factions' doctors and mechanics give advice by radio**, on by default.

## A radio friend

**A colonist with a radio can make a friend at another faction's settlement and chat with them as recreation.** Nothing to press: a
colonist picks it like any other recreation.

- **What it needs**: a hand-held in touch with a station (in bed too, awake), or a powered station's desk to walk to; and a faction
  that is not hostile and has radio (not tribes or medieval kingdoms), with a settlement your stations reach. Masts, hilltops and
  relays bring more settlements into reach.
- **The friend** is a named person of that faction, the same each time.
- **After each chat** the colonist's mood lifts: **+2** with a new friend, **+4** with a friend (about 10 hours of chats), **+6** with
  a close friend (about 30 hours).
- **A chat of an hour or more** raises the friend's faction's goodwill by **1**, at most **3 times a quadrum** for each faction.
- **If the friend dies or their faction turns hostile**, the colonist mourns them (**-6** for 6 days), and finds a new friend next time.

## School of the Air

**Children can learn by radio** (needs Biotech). After the microelectronics research, a child may choose **radio school**: at a radio
station's desk, or anywhere with a hand-held in touch with a station.

- **With nobody to teach**, it fills their learning, as the game's radio talking at a comms console does.
- **With a teacher in radio touch** it is a lesson: an adult **assigned to childcare**, on the map with a hand-held or at a desk, or
  **away in a convoy whose radio reaches home**. The child learns one of the teacher's best skills, at three quarters of a lesson at a
  school desk; the teacher gains Social. A parent is asked first.

## Neighbours ask for yours

**Now and then, about once a fortnight, a friendly neighbour asks by radio for your best doctor or mechanic for an hour.** Only a
faction that is not hostile and has radio, with a settlement your stations reach, asks; and only for a colonist with Medicine or
Construction **6 or more**, free and in radio touch.

- **Yes**: the colonist goes to the radio for an hour (a hand-held, or a station's desk), gains a little of that skill and Social, and
  the faction's goodwill rises by **5** once the hour is done.
- **No** costs nothing. A letter left unanswered goes after 6 hours.
- Setting: **Neighbours ask for help by radio**, on by default.

## The logbook and QSL cards

**Select a radio station or terminal and open its Log tab.** It lists your colonists' radio friends, the settlements in reach, and the
log, newest first: advice calls, other factions' help, friends made and lost, radio lessons, neighbours' calls and medevacs.

- **QSL cards**: the first contact with each settlement (a faction's doctor or mechanic, a radio friend's chat, a neighbour's call)
  earns its card, shown on the tab in that faction's colours.
- **Awards**: cards from three factions, from every faction on the air, from 50 km away or more, or from ten settlements each earn an
  award. Colonists who work the radio (wearing a hand-held, or with a radio friend) are proud of it: **+2** an award, up to **+6**.

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

The figures and the settings are in [D'thorga Radio Framework's README](../../mods/PhobosRadioFramework/README.md); the notes
for modders are in [Radio Framework for modders](../modding/radio-framework.md).

## Tested in game

The owner has not yet passed anything in this chapter ([the test list](../development/in-game-test-pass.md)).

Everything in this chapter is not yet tested in game.
