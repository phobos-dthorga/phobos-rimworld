# Spall inside our vehicles: what comes through a beaten plate (21 Nov 2026)

**Status (21 Nov 2026):** design only; nothing is built. The owner chose a design record first (card, 21 Nov 2026); its questions
are below.

**The owner, 21 Nov 2026**, after an analysis of the Workshop mod "CE Additional Expansion for Armed Damage Combat" (which the owner
then passed on): "What if we implement armor that matters layer-by-layer? Would that be a difficult thing, especially if we were to
learn from this mod?" Then, on Combat Extended: "It will probably be difficult, or more difficult, to develop without CE."

How to read it: **[O]** is the owner's choice; **[A]** is the agent's choice or figure, for the owner to revise; **[I]** is our
inference; **[code]** was read in code on 21 Nov 2026 (Combat Extended's vehicle compatibility as installed, decompiled into
`.local/decompiled/CE-VehiclesCompat`; Vehicle Framework 1.7's decompile), never run; **[P]** a primary source; **[S]** a secondary one.

## Read this first

- **Layer-by-layer armour already exists in the owner's game, twice.** Combat Extended resolves a person's armour layer by layer
  (shield, then each worn layer from the outside in), and resolves a vehicle's armour part by part (each outer part on the hit
  cell, then inside, then crew, then the next cell) **[code]**. Building our own would be a second armour model fighting Combat
  Extended's, on code every bullet runs through. **This record does not do that.**
- **What is missing is what comes through a beaten plate.** When a round beats a vehicle's outer plate, nothing sprays debris
  into the compartment behind it. Combat Extended only throws one large fragment outward when a round leaves the far side **[code]**.
  Real armour throws debris inward, and real armoured vehicles carry **spall liners** to catch it **[P]**.
- **Combat Extended first; nothing without it** [O, 21 Nov 2026]. Without Combat Extended, Vehicle Framework's armour is a chance,
  not a thickness, so there is no margin to read. Players without Combat Extended see no change, as a stated choice.
- **No Harmony patch is needed** [code]. Combat Extended's own armour step calls Vehicle Framework's **reactors** on every part it
  damages, and we already put reactors of our own on parts at start-up (`Reactor_PhobosVehicleSystems`, our turret popper). A spall
  reactor goes on outer parts the same way.
- **People are left alone.** Combat Extended's layers and More Injuries' spalling already cover them; this record is for vehicles.
- **Risky for crews, so it has its own setting.** Graded high by the shared rule's wording (a crew member can die in one hit), so it
  starts off, unless the owner grades it otherwise (question 1).

## What Combat Extended already does to a vehicle [code]

Combat Extended's prefix on `VehicleStatHandler.ApplyDamage` replaces Vehicle Framework's own armour step (its setting "Use New
Vehicle Armor", on by default); `VehicleArmorPatch.ApplyDamageCE` then walks the shot's line through the vehicle's cells:

| Step | What happens |
| --- | --- |
| **Outside** | Each living outer part on the hit cell is hit in turn (`TryPenetrateComponents`): first its armour, then its bulk (health ÷ 50 as extra armour) |
| **Stopped outside** | A sharp round turns blunt and tries again; a blunt one bounces off |
| **Through** | The round goes inside the same cell and meets one inner part; if it gets through that, it may hit crew sitting on the cell (`HitPawn`), then moves to the next cell |
| **Inside, stopped** | Bouncing off an inner part sends it back to the inside face of the armour; bouncing off the inside face moves it on |
| **Out the far side** | One large fragment is thrown outward (Combat Extended's setting "fragments from vehicles") |

**One plate's armour step** (`DamageArmor`), the rule our figures borrow:

- The round is stopped when the plate's armour exceeds its penetration.
- Otherwise the part of the damage that passes on is damage × (penetration − armour) ÷ penetration, with penetration − armour left over.
- The plate itself takes the rest, scaled: the stopped share × min(1, penetration² ÷ armour²), plus the passed share × min(1, armour ÷
  penetration).

**What each reactor is told** (`DamageComponent`, then `Reactor.Hit`): the vehicle, the part, the hit **before** armour, and whether the
armour step stopped it (Diminished) or not (Penetrated). A part that is beaten is reported twice, once for its armour and once for its
bulk, both as Penetrated. This matches what our systems reactor already records (8 Nov 2026): "Combat Extended gives reactors the full
damage before armour, sometimes twice".

Worked with **illustrative figures, not a real round**: damage 40 against a 30 mm plate (our Heavy machine gun class), the plate's bulk 6 mm.

| Round's penetration | Plate's armour step | Plate's bulk | What the plate took (sum) | What goes inside |
| --- | --- | --- | --- | --- |
| **25 mm** (below the plate) | Stopped; the plate takes 27.8 | — | 27.8 | Nothing |
| **36 mm** (just over) | Beaten; 6 mm and 6.7 damage left; the plate takes 38.9 | Stops the rest; takes 6.7 | 45.6 | Nothing sharp: the round dies in the plate (Combat Extended then tries it once more as a blunt hit) |
| **90 mm** (well over) | Beaten; 60 mm and 26.7 left; the plate takes 22.2 | Beaten; 54 mm and 24.0 left; takes 5.1 | 27.3 | The round itself, with 24 damage |

Read from Combat Extended's own figures: **a round that only just beats the face puts the most into the plate and none of itself
inside**, and a round that beats it easily passes most of itself through. Our spall follows the first column of that: debris from
what the plate took.

## What the real thing does

- **Behind-armour debris** is "debris particles eroded from the penetrator of armor as well as spalled material ejected from the target
  itself", described by "the number, position, and size range of debris particles" (Wikipedia, "Behind-armor debris", revision of
  15 Apr 2022) **[S]**. Its one reference, Pedersen and Bless (2006), was not read.
- **Rheinmetall Protection Systems' brochure "Spall-Liner high-tech composite"** (B395e05.24) **[P, the maker's own claims]**:
  - spall can come off the inner face "without penetrating" the armour, from the shockwave, and its fragments are "ejected into the
    crew compartment" at very high speed;
  - its diagram gives a **spall cone of 87° behind bare armour and 17° with the liner**;
  - liners are made of "PE, Aramid or GRP", are meant "specifically" for "over-match-situations", and reduce spall "both in number and
    in distribution";
  - the liner is mounted directly on the base armour, so its own fixings make no fragments, and equipment is mounted on the liner.
- **Not sourced:** how the amount of debris depends on the margin a round beats a plate by. We follow Combat Extended's figures (above)
  and label it so. A US Army live-fire report on the M113A3's spall liner (DTIC ADA267255) and an Army Research Laboratory simulation
  report (DTIC ADA441907) refused automated reading on 21 Nov 2026; nothing here is taken from them.

## The design

### Spall from a beaten plate [A]

1. **Which parts.** Every outer part of a vehicle in scope (question 2) carries a spall reactor, added at start-up beside our systems
   reactor. At a hit it acts only when Combat Extended is active, the hit is sharp, and the part's sharp armour is the **Rifle class
   (8 mm) or more**: thin skin tears rather than spalls **[I]**, and our trucks' Skin parts throw nothing until a kit is fitted.
2. **When.** On the armour-step report (the first of the two), when the face is beaten (Penetrated). A hit stopped by the face throws
   nothing in the first build; the "without penetrating" spall Rheinmetall describes is question 4.
3. **How much.** We call Combat Extended's own `DamageArmor` on **copies** of the hit's figures (it changes nothing else), so our
   figures never drift from its rule: it gives what the plate took. **The spall pool is half of that** [A].
4. **Where it goes.** Into the compartment behind the plate: the crew whose seats' hit areas share the plate's cells, and the inner
   parts on those cells. **Unlined, the cone is wide**: the plate's cells and the next cell inward. **Lined, it is narrow**: the one
   cell of the plate nearest the shooter (the cell Vehicle Framework itself picks) [A, after Rheinmetall's 87° and 17°].
5. **As what.** The pool is split into **three fragments unlined, one lined, with the pool cut to 30 % by a liner** [A]. Each fragment
   picks one target in reach, crew and inner parts alike, or nothing at a **30 % chance** (empty space) [A].
   - **On crew:** Combat Extended's own `Fragment` damage, sharp, with penetration a third of the plate's sharp armour [A], so the
     crew's own body armour can stop it. The shooter is the instigator. The wounds are the game's own injuries.
   - **On an inner part:** through Combat Extended's own part step (`HitComponent`, called by name), so the part's armour counts and
     its reactors fire. A holed fuel tank leaks through Vehicle Framework's own fuel-leak reactor; a holed radiator loses coolant
     through our systems.
6. **A guard against loops.** The spall reactor ignores hits made while it is applying its own fragments.

### Spall liners [A]

- **An upgrade on our armoured trucks:** "Spall liner", which **needs the armoured cab** (it sits inside the cab, so it is "holds it
  on" under the tree rules), in the armour-kit columns.
- **Weight 200 kg** [A] as carried mass, under the rule for kits; about a fifth of the cab kit's 1,000 kg. No density source was read.
- **Made from liner panels**, a new part item [A] (the rule for intermediate parts), made at the machining bench from synthread, the
  game's synthetic fibre, standing in for aramid [A]. **It needs one item picture.**
- **Other authors' vehicles count as unlined**, unless their Def carries a small extension saying they are lined. D'thorga Patches may
  add it to real vehicles a source shows carry liners, one source per vehicle [A].

### What the player sees

- **Wounded crew**, in their Health tabs as fragment wounds from the shooter.
- **Holed inner parts**, in our vehicle Health tab as usual.
- **The liner's line** in the Health tab's armour notes: "Spall liner fitted" or nothing [A].
- **No message per hit**: combat already says enough, and a message for every hit would flood.

## The four questions

| Behaviour | Lets the player | Stops them | Changes how they play |
| --- | --- | --- | --- |
| **Spall from a beaten plate** | See armour matter beyond "did it get through": a plate only just beaten still hurts the crew behind it | Keeping a crew unhurt behind a plate a round has just beaten | Heavier armour, or keeping out of range of weapons that beat it, is chosen for the crew's sake; wounded crew are rotated out |
| **Spall liners** | Protect the crew of an armoured truck from its own plates | Fitting armour as if crews were safe behind any plate | A truck sent into fire gets a liner as well as a cab, and carries 200 kg more |

**What outside the vehicle notices:**

- **The colony's doctors**, who treat the wounds.
- **The vehicle's systems**: fuel leaks, lost coolant, and a wounded gunner who aims and reloads slower ([knocked-out systems, row C4](vehicle-knocked-out-systems.md#c-crew-and-cab)).
- **Raiders**: a raider's crew wounded by spall loses the gunner sooner, and the bail-out rule (C2) empties a vehicle that can no longer fight.
- **The game's own records**, which credit kills to the shooter.

## Risk and settings

- **Risk, graded high by the agent** (proposal): a crew member can be killed by one hit's spall, and the player cannot see a given hit
  coming. The shared rule calls losing a person at once high, so **it starts off**.
- **The case for moderate, which would start it on** (question 1): Combat Extended already wounds crew in these same hits; spall adds
  to wounds the player already accepts by driving into fire; the armour class on the Health tab shows what a plate stops; and the
  liner is the answer the game offers.
- **Settings proposed** (D'thorga Framework, vehicles section; not built):
  - **"Spall inside vehicles"**, default by the grade.
  - **"Spall on other mods' vehicles"**, if question 2 is answered "every vehicle".

## Other factions' vehicles

| Where | What happens |
| --- | --- |
| **A raid on us** | The same rule on any vehicle in scope: our shots that only just beat a raider's plates wound its crew. Unlined, unless their Def says otherwise |
| **Parked at a base we attack** | Nobody aboard, so spall reaches only inner parts. It costs nothing until a plate is beaten |
| **One we capture** | Unchanged. Its liner, if ours and fitted, stays fitted |

## Cost to run

- **Nothing per tick.** Each hit on an outer part costs the reactor's checks (Combat Extended active, sharp, armour class), all cheap.
- **Only a beaten plate does real work:** one call of Combat Extended's armour function, a look through the vehicle's few seats and
  the parts on two cells, and up to three damage calls.
- **No projectiles are spawned**, unlike the Workshop mod this started from.
- **Target:** within the vehicle-health budget of 0.02 ms of our code per hit, with no garbage made, checked with `perf.py` once built.

## Saves

- **Nothing is saved.** The wounds are the game's own and Combat Extended's injuries; the liner is an upgrade node and a part item in
  XML, keys stable once a save holds them.
- **Safe to add**, and to switch off at any time. Removing D'thorga Vehicles drops the liner node as with every other node of ours.

## What we take from that mod: ideas only

The Workshop mod has no licence and was read only as a decompile, so nothing of its code is used. Its ideas, in our own words:

- **Listen to Combat Extended's own armour results instead of redoing its maths.** It patched Combat Extended's armour function to
  record each layer. We need no patch: Vehicle Framework's reactors are already called, and we call Combat Extended's armour function
  on copies.
- **Make debris follow what was actually beaten.**

What it did that we will not:

- Made its conditions in code at the first hit, so saves lost them. Ours are the game's own injuries.
- Re-tuned every round in the game.
- Logged debug lines with its debug switch off.
- Had no idea vehicles exist, so its bursts ignored vehicle armour.
- Reset the player's settings after updates.

## To read or prove before building

1. **The crew's armour against our fragment:** how Combat Extended's person armour step treats a `Fragment` hit with an instigator and
   no weapon (it must apply, so a helmet and vest help).
2. **Calling `HitComponent` from inside Combat Extended's own walk**: that it is safe while the walk is running, and that the health
   recalculation at its end covers our damage.
3. **Other authors' prefixes on `ApplyDamage`** (the Harrier's, Hind's, Mongoose's and A-10's hit spread, and Rimarrow: Search &
   Rescue's, which may skip the original): whether the reactors still fire under them.
4. **Which upgrade-read pattern tells a vehicle has its liner**, as the litter kit is read once fitted.
5. **A recording with `perf.py`** in a fight against an armoured vehicle, before and after.

## The owner's questions

1. **The risk grade:** high, starting off (the agent's reading of the rule), or moderate, starting on?
2. **Which vehicles:** every vehicle with plates of the Rifle class or more under Combat Extended, other mods' tanks included (the
   agent's recommendation: that is where armour is thick, and raiders' tanks get it too), or ours only?
3. **The liner:** as proposed (200 kg, liner panels from synthread, needs the armoured cab), or a different cost or weight?
4. **Spall without penetration** (Rheinmetall's shockwave spall): leave it out of the first build (the agent's recommendation), or
   include it for heavy and explosive hits?

## Who chose what

- **Owner (21 Nov 2026):** a design record first (card); Combat Extended first, since a version without it would be harder to build;
  vehicles, not people (the card's option to cover people was not taken).
- **Agent:** everything marked [A]; the reactor route; the Rifle-class threshold; the high risk grade, as a proposal.

## Sources

- Combat Extended, `VehicleArmorPatch` (`ApplyDamageCE`, `TryPenetrateComponents`, `HitComponent`, `DamageArmor`, `DamageComponent`,
  `SpawnFragments`), read in the installed build's decompile and in the public source clone, 21 Nov 2026 **[code]**.
- Vehicle Framework 1.7, `Reactor`, `VehicleComponent.TakeDamage`, `VehicleStatHandler.HitPawn` and `TrySelectHandler`, read in the
  decompile, 21 Nov 2026 **[code]**.
- [Rheinmetall Protection Systems GmbH, "Spall-Liner high-tech composite", brochure B395e05.24](https://www.rheinmetall.com/Rheinmetall%20Group/brochure-download/Protection-Systems/B395e0524-Spall-liner-high-tech-composite.pdf)
  (the maker's own claims; read 21 Nov 2026) **[P]**.
- [Wikipedia, "Behind-armor debris"](https://en.wikipedia.org/wiki/Behind-armor_debris) (revision of 15 Apr 2022; read 21 Nov 2026) **[S]**.
- **Unverified, not used:** B. Pedersen and S. Bless (2006), "Behind-armor debris from the impact of hypervelocity tungsten
  penetrators", International Journal of Impact Engineering 33: 605–614; US Army live-fire report on the M113A3 spall liner (DTIC
  ADA267255); Army Research Laboratory, "Simulating the Formation and Evolution of Behind Armor Debris Fields" (DTIC ADA441907).
