# The prisoner transport trailer (13 Nov 2026)

**A research and design record. Nothing is built, and nothing here is tested in game.** The owner, 13 November 2026: "Something to
transport prisoners with." The owner chose all four parts the same day: **capture straight into it** with no prisoner bed;
**raiders use them on us** and carry downed colonists off; **captives to free at enemy bases**; and **harsh conditions matter**,
with upgrades that cost materials and fuel. It is trailer **Q2** in [vehicle-trailers.md](vehicle-trailers.md). Marks as there:
**[code]** read on 13 Nov 2026 and summarised in our own words; **[A]** authored; **[S]** a real-world source below; **[I]** our
inference. Vehicle Framework references are from its **1.6** decompile.

## Read this first

- **What it is:** a cage trailer with six barred cells and a guard's seat. Prisoners ride in the cells; nobody else can.
- **Most of it works today.** Vehicle Framework already lets a colony prisoner ride in a seat with no driving or shooting duty, and
  aboard **a prisoner cannot escape** (a prison break needs a prison cell) [code]. In a caravan the game feeds them, and a caravan
  can sell them at a settlement or gift them home for double goodwill [code].
- **What we add:** cells that take prisoners only; feeding on a map (nothing feeds a prisoner aboard there); **capture straight into
  the cage**; prisoners set down wait before they run; the cage's riders travel with the caravan when it is towed; and the three
  owner's parts: harsh conditions, raiders' cages, captives at bases.
- **Full consequences** (the owner's rule for extreme choices): an open cage in a blizzard freezes people, and the trailer warns
  plainly before and while it happens, then lets it happen.
- **Name, an agent pick for the owner to change:** the **Stockade prisoner trailer**. No Def on the owner's list uses the word
  (`scripts/query-defs.py sql`, 13 Nov 2026).

## Real prisoner transport, for scale

- Prisoner transport vehicles are usually vans or buses with **bars or wire mesh over the windows, segregated prisoner
  compartments and seats for escorting officers**; prisoners are usually restrained and may be secured to the vehicle; older
  buses offered "no way of escape"; one informal name is the **"sweat box"** [S]. Our cage takes the bars, the separate cells, the
  guard's seat and, through the closed box body, the sweat box.

## What the code says

| Question | Finding [code] |
| --- | --- |
| Can a prisoner ride? | Yes, in any seat with no driving or gunner duty; the one-argument add skips the faction test for such seats (`VehicleFramework-1.6:Vehicles.decompiled.cs:15106-15111`, `15184`). A walking prisoner is ordered aboard; a downed one is hauled in (`:16476-16480`, `:1028-1035`) |
| Can anyone else take that seat? | Yes: a seat has no setting for who may sit there (`:17526-17560`). Cells need a small patch of ours |
| Can they escape aboard? | No: a prison break needs the pawn in a prison cell, and a rider's room is the vehicle's cell outdoors (`full:all.cs:461627-461631`, `90959-90970`) |
| Who feeds them? | **On a map, nobody**: wardens deliver food only to a prisoner in a cell, and chat only with a spawned one (`full:all.cs:309821`, `309953`). **In a caravan, the game** feeds every member from its inventory (`full:all.cs:792629-792638`) |
| What happens when the vehicle is destroyed? | Vehicle Framework sets everyone aboard down where it stood (`VehicleFramework-1.6:Vehicles.decompiled.cs:15545-15551`) |
| What happens when they are set down? | A prisoner standing on the map outside a cell runs for the edge (`full:all.cs:302062-302093`). Drop pods, shuttles and arriving caravans first tell prisoners to wait (`WaitInsteadOfEscapingForDefaultTicks`, `full:all.cs:461526`; callers at `332093`, `602504`, `787261`); Vehicle Framework does not |
| How does capture work? | Only into a prisoner bed: the take-to-bed job casts its target to a bed, and the menu says "no prisoner bed" without one (`full:all.cs:289526`, `656513-656558`). Making a prisoner is one call, `CapturedBy` (`full:all.cs:461509-461524`), which the bed job and the caravan both use |
| Who takes a loose prisoner to bed? | Wardens, through the game's own take-to-bed work (`WorkGiver_Warden_TakeToBed`, `full:all.cs:310567-310646`) |
| How do raiders kidnap? | A raider able to reach the map edge picks a downed, humanlike colonist of a faction hostile to it (`KidnapAIUtility.TryFindGoodKidnapVictim`, `full:all.cs:298875-298913`), carries them off the map, and the game records them with the faction (`KidnappedPawnsTracker.Kidnap`, `full:all.cs:488945-488980`), which ransom demands later read |
| Do Prison Labor and Prisoner Realism change this? | Prison Labor's own escape rule runs only for spawned, uncarried prisoners, and its needs freeze off the map (`PrisonLabor-1.7`). Nothing of theirs runs aboard |

## How it holds people (recommendation)

**Agent recommendation: the cells are Vehicle Framework seats, not a holder of our own.**

- **Why:** as seats, the game and Vehicle Framework already do most of the work: riders' needs tick, a caravan feeds them, counts
  them and can sell or gift them, and a destroyed trailer sets them down. A holder of our own (the litter kit's pattern) would have
  to rebuild each of those.
- **Our patch:** a seat marked as a **cell** takes prisoners (and, on the raiders' side, captives) only, and refuses anyone else
  with a reason ("Cells are for prisoners").
- **The one real gap:** on the world map a towed trailer rides in the hitch's own holder, not in the caravan's list of vehicles, so
  **its riders would not be caravan members**: not fed, not counted, not sold [I, from `TowTravel.cs:67-85` and Vehicle Framework's
  caravan list at `:87565-87599`]. The fix, needed by every trailer that carries people or animals: **riders of a towed vehicle join
  the caravan's list** while it travels. The hitch record already lists "riders of a towed vehicle while travelling" as unchecked
  (`vehicle-hitch-design.md:271`); this record makes it a requirement.

## What the player does

1. **Build and tow it** like any trailer.
2. **Fill it:** walk a prisoner aboard from a cell at home, or **capture a downed enemy straight into it** in the field.
3. **Stock its ration locker** with meals, survival meals, pemmican or paste; haulers fill it like a stockpile.
4. **Travel:** prisoners ride in the cells. On a map they eat from the locker; in a caravan the game feeds them.
5. **Deliver:** let them out at home (they wait, then wardens take them to prison beds), sell them at a settlement, or gift them home.
6. **Mind the weather:** an open cage is the weather; fit the box body and heater for the cold.

## Capture straight into it (owner)

- **The order:** right-click a downed enemy: "Capture into the Stockade (4 cells free)". Refused with a reason when the trailer is
  full, not ours, unreachable, or the target cannot be captured by the game's own test.
- **The job:** carry them to the trailer, a short wait at the door [A: 250 ticks], then into a cell, and then the game's own
  `CapturedBy`, exactly as the prisoner bed would do it, with its tale, record and letter.
- **Prisoner Realism's arrests** still work at home; nothing of ours touches them.
- **Slaves (Ideology)** may ride in cells too [A]; nothing of the game's slave rules changes.

## Rations and the harsh side (owner)

### Rations

- **On a map:** a check of ours on the rare tick feeds each hungry prisoner from the **ration locker** [A: 20 meals], the way the
  game feeds a caravan member. Water from the locker's jerry cans where Dubs Bad Hygiene gives thirst [A: 40 L].
- **Empty locker:** they go hungry by the game's own rules (hunger, then malnutrition). The trailer says "No rations left" first.

### Conditions in the cage

The cage has no cab, so **its riders feel the weather of the cell it stands on, or the world tile** [code: the game asks a rider's
holders for a temperature and falls through to the cell or tile when nothing answers]. Everything below is the game's own
consequence of that temperature, food and wounds; we add only the cage's memories and the upgrades.

| Body | Temperature inside | Rain and sun | Real sense |
| --- | --- | --- | --- |
| **Open cage** (base) | The weather | Exposed | A cage on a flatbed |
| **Canvas cover** (upgrade) | The weather | Sheltered | A tarp over the bars |
| **Box body** (upgrade) | Moves towards 21 °C by a third [A] in the cold, but **8 °C hotter than outside in the heat** [A] | Sheltered | The "sweat box" |
| **Box body with vents** (upgrade) | As the box, without the extra heat in sun | Sheltered | |
| **Heater** (upgrade, box body only) | At least 10 °C [A] | — | A chemfuel heater with its own 20 L tank, 1 L a cold hour [A] |

- **What follows is the game's:** hypothermia and frostbite in the cold, heatstroke in the heat, hunger, and **wounds that nobody
  tends** (no doctor works in a cage): bleeding, infection, death. **Full consequences**, as the owner wants.
- **Memories [A]:** a prisoner caged in the open gets "Caged like an animal" (−8); in a box body "Locked in a box" (−4); none with a
  heater and a full belly. A colonist kidnapped into a raider's cage gets "Caged by raiders" (−10, lasting after rescue).
- **Warnings (plain words, in-world):** the inspect text names the worst of it ("Freezing in there", "No rations left", "Someone is
  bleeding"), and a letter the first time someone aboard is in danger of dying. Then it is the player's call.
- **A guard's seat** [A]: a colonist with a weapon. The guard can **tend a wound aboard** with medicine from the locker once a day
  [A], the one mercy the cage allows, and is the person raiders must deal with to open it.

| Behaviour | Lets the player | Stops the player | Changes how they play |
| --- | --- | --- | --- |
| **Cells** | Move prisoners anywhere without escapes | Riding colonists in them | Raids and base attacks can end with prisoners taken home |
| **Capture into it** | Take prisoners in the field with no bed | Nothing | Prisoners become a field decision, not only a home one |
| **Rations** | Keep prisoners fed on a map | Forgetting the locker | Prisoners are a load to supply |
| **Harsh conditions** | Choose cheap and cruel or dear and humane | Moving prisoners in a blizzard in an open cage without cost | Weather and kit decide whether prisoners arrive alive |

## Raiders use them on us (owner)

- **Which raids:** raids that arrive in vehicles ([vehicle-framework-mechanics.md](vehicle-framework-mechanics.md) says how other
  factions come to have them). A raiding tractor may arrive **with a Stockade already coupled** [A: 1 raid in 5 that brings
  vehicles, industrial factions and above, never a faction that is not hostile].
- **What they do:** raiders who would kidnap (the game's rule: a downed, humanlike colonist of a hostile faction) **carry the
  victim to the cage when it is nearer than the map edge** [A]. When the raid retreats, the tractor drives off with the trailer.
- **If it gets away:** everyone in the cage is recorded as kidnapped by that faction, exactly as the game records a kidnap, so the
  game's own ransom demands and later rescues work as they always have [I: the call exists; joining it to a vehicle leaving is ours].
- **How to stop it:**
  - **Stop the tractor:** down its driver or wreck it; the trailer is left where it stands.
  - **Break the coupling:** the drawbar is a part of the trailer that can be shot off (built).
  - **Open the cage:** a colonist next to it works the lock [A: 600 ticks], under fire. Everyone inside is set down where they are.
  - **Destroy the trailer:** Vehicle Framework sets everyone down [code], hurt by what destroyed it.
- **Engineering, honestly:** our hitch has **no AI**: nothing orders a raider to couple a trailer, and a raider's tow on our "lite
  tier" for other factions' vehicles has to keep moving its trailer. The raid must **spawn with the trailer coupled**, and the hitch
  must move a raider's trailer as it moves ours. Both are new work, and the riskiest part of this record.

## Captives at enemy bases (owner)

- **What it is:** when a faction base's map is made, it may have a **parked Stockade holding captives** near its heart [A: by tech
  level; 1 to 3 captives].
- **Who is inside, in order** [A]:
  1. **Colonists of yours that this faction kidnapped**, from the game's own record of them. Finding them is a reason to attack.
  2. **People of a third faction** this base is hostile to.
- **Freeing them:** open the cage (a colonist works the lock) or destroy it.
  - **Your own colonists** rejoin, and are removed from the faction's kidnapped list.
  - **A third faction's people** walk home as guests; their faction thanks you [A: +12 goodwill each]. Like the game's own prisoner
    rescue sites (`GenStep_PrisonerWillingToJoin`, `full:all.cs:413095-413170`), one may ask to join you instead [A: 1 in 4].
- **Or take the Stockade whole**, captives and all, and decide later.
- **How:** a map generation step of ours added to the faction base map, placing a parked trailer and its riders once. Nothing runs
  per tick afterwards.

## Other factions' vehicles

| Where | What happens | Lets the player | Stops the player |
| --- | --- | --- | --- |
| **A raid on us** | A raiding tractor may bring a Stockade and fill it with downed colonists | Stop the tractor, shoot the drawbar, open the cage | Losing a fight without consequence: a lost fight can now cost a colonist carried away |
| **A base we attack** | A Stockade may hold your kidnapped colonists or a third faction's people | Rescue them; earn goodwill; recruit | Nothing |
| **A vehicle we capture** | It works for us at once; anyone in it is set down and decided by the player | Use it | Nothing |

## When it is destroyed or opened

- **Destroyed:** everyone aboard is set down where it stood [code], hurt by what destroyed it. **They do not wait:** prisoners loose
  in the open run for the edge (the game's rule) unless recaptured. Full consequence.
- **Let out on purpose:** each prisoner set down is told to wait as drop pods do [code: about 25,000 ticks, roughly ten hours], so
  wardens can take them to prison beds.

## Upgrade tree

| Node | Does | Cost [A] |
| --- | --- | --- |
| Canvas cover | Shelter from rain and sun | Cloth 60 |
| Box body | The table above; replaces the canvas cover | Steel 120 |
| Vents | No extra heat in the box in sun | Steel 20 |
| Heater | At least 10 °C, burning its own chemfuel | Steel 30, components 2 |
| Extra cells | Six cells to ten, on the larger chassis only | Steel 80 |
| Bigger locker | 20 rations to 50 | Steel 30 |

**Parts bill:** wheels 4, suspension 2, frame 1, wiring 1, as the Pintle. **Chassis:** the Pintle's 2 x 5 [A].

## Cost to run

- **Feeding** on a map: the rare tick, only for trailers with someone in a cell.
- **Raiders' cages:** the kidnap choice is the game's own job with one more place to carry to; the tow runs only while the raid's
  tractor moves.
- **Captives at bases:** once, when the map is made.

## Optional tie-ins

| Mod | What changes |
| --- | --- |
| Prison Labor | Nothing aboard; its escape and labour rules resume when prisoners are set down |
| Prisoner Realism | Its arrests and carry job work as they do; our capture order sits beside them |
| VOE: Prisoner Patch | Dormant (last updated July 2025); not leaned on |
| Dubs Bad Hygiene | Water in the ration locker, for thirst |
| Ideology | Slaves may ride in cells |

## Art and sound

- **The body, bare:** the cage on the Pintle chassis; canvas cover, box body, vents and heater each their own overlay, planned
  before the first picture.
- **Sounds:** a barred door, a padlock, the heater's burner.

## Questions for the owner

1. **The name:** "Stockade prisoner trailer" (agent pick), or another?
2. **Cells as Vehicle Framework seats** (agent default), or a holder of our own?
3. **The guard's seat and one tending a day** (agent default), or no tending at all?
4. **Raiders' cages:** 1 raid in 5 that brings vehicles (agent default), rarer, or more often?
5. **Captives at bases:** your own kidnapped colonists first, then a third faction's people (agent default)?
6. **The memories** (−8 open cage, −4 box, −10 for a colonist caged by raiders): fine to start with, tuned in play?

## Who chose what

- **Owner (13 Nov 2026):** something to transport prisoners with; capture straight into it; raiders use them on us; captives at
  enemy bases; harsh conditions with upgrades that cost materials and fuel.
- **Agent defaults, owner to revise:** the name; cells as seats; the ration locker; the cage bodies and every figure marked [A]; the
  memories; the guard's tending; raiders carrying victims to a nearer cage; who is held at bases and what freeing them gives.

## Sources

- Wikipedia, [Prisoner transport vehicle](https://en.wikipedia.org/wiki/Prisoner_transport_vehicle), read in full on 13 Nov 2026
  (the article is tagged as needing more citations): bars and mesh, segregated compartments, seats for escorting officers,
  restraints, "sweat box".
- RimWorld 1.7 (Ludeon Studios), Vehicle Framework 1.6 build (Smash Phil), Prison Labor (decompiled 1.7 build): read from our
  decompiles, summarised in our own words.
