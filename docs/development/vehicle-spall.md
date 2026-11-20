# Spall inside our vehicles: what comes through a beaten plate (21 Nov 2026)

**Status (21 Nov 2026):** design only; nothing is built. The owner chose a design record first and answered its questions the
same day ([the answers](#the-owners-answers-cards-21-nov-2026)): moderate risk, on by default; every vehicle; the liner as proposed;
shock spall for heavy hits; then the liner as its own part that wears and is re-lined with liner panels.

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
- **Risky for crews, so it has its own setting.** **Graded moderate by the owner, so it starts on** [O, 21 Nov 2026]; the agent had
  proposed high.
- **Every vehicle** with plates of the Rifle class or more, other mods' included [O], and **stopped heavy hits throw shock spall
  too** [O].

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

1. **Which parts.** Every outer part of every vehicle (owner) carries a spall reactor, added at start-up beside our systems
   reactor. At a hit it acts only when Combat Extended is active, the hit is sharp, and the part's sharp armour is the **Rifle class
   (8 mm) or more**: thin skin tears rather than spalls **[I]**, and our trucks' Skin parts throw nothing until a kit is fitted.
2. **When.** On the armour-step report (the first of the two), when the face is beaten (Penetrated). **A heavy hit the face stops
   throws shock spall** [O], the spall "without penetrating" Rheinmetall describes (below).
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

### Shock spall from a stopped heavy hit [O, figures A]

The owner's choice (21 Nov 2026), after Rheinmetall's description of spall thrown off the inner face by the shockwave of a hit that
does not get through **[P]**.

- **What counts as heavy** [A]: an explosive hit (a damage type the game marks explosive, such as a shell's blast) of **30 damage or
  more**, or a sharp hit stopped with a penetration of **at least three quarters** of the plate's armour. Rifle fire bouncing off
  thick plate throws nothing.
- **How much** [A]: a **quarter** of what the plate took, against a half for a beaten plate. **One fragment**, lined or not, so it is
  a wound rather than a spray; the liner cuts the pool to 30 % and narrows the reach as for ordinary spall.
- **Read before building:** how Combat Extended reports a blast to the reactors. Blasts reach a vehicle as blunt hits, one per edge
  cell of its footprint [code, the armour record], so a single shell may report several cells; shock spall is counted once per part
  per hit.

### Spall liners [A]

- **An upgrade on our armoured trucks:** "Spall liner", which **needs the armoured cab** (it sits inside the cab, so it is "holds it
  on" under the tree rules), in the armour-kit columns.
- **Weight 200 kg** [A] as carried mass, under the rule for kits; about a fifth of the cab kit's 1,000 kg. No density source was read.
- **Made from liner panels**, a new part item [A] (the rule for intermediate parts), made at the machining bench from synthread, the
  game's synthetic fibre, standing in for aramid [A]. **It needs one item picture.**
- **Other authors' vehicles count as unlined**, unless their Def carries a small extension saying they are lined. D'thorga Patches may
  add it to real vehicles a source shows carry liners, one source per vehicle [A].

### The liner is a part that wears [O, figures A]

The owner's choice (cards, 21 Nov 2026): the liner is **its own tracked part, with health, that wears as it catches spall**, and **a
worn liner is put right only with liner panels**.

- **The part.** An inner part, "spall liner", appended by the loadout on the cab's cells, as our radiators and mounts are appended.
  **60 health** [A]. It does not count toward the vehicle's destruction: a spent liner neither keeps a wreck alive nor kills a truck.
- **Not fitted and spent are one state: zero health.** A truck without the upgrade carries the part at zero, shown as "not fitted".
  Combat Extended's walk skips a part at zero health **[code]**, so an unfitted liner is never hit and stops nothing. Fitting the
  upgrade brings it to full.
- **What it catches, by its health** [A]: at full health it cuts the spall to 30 %, as above. The share it stops falls in a straight
  line to **nothing at a quarter of its health**, the same point where Combat Extended counts a part's armour as gone **[code]**.
  The reach stays narrow above half health and is wide below it.
- **How it wears** [A]: the liner loses health equal to **half of the spall it stopped**. In the worked hit above (the plate took
  45.6, so a pool of 22.8), a full liner stops 16 and loses 8 of its 60.
- **The round itself can hit it.** After a round beats the plate, Combat Extended makes it meet one inner part on that cell, and the
  liner is one of them **[code]**. That is fair: the round does pass through it. At 60 health its bulk counts as 1.2 mm, so it is no
  armour worth the name; a labelled side effect.
- **Three states on the Health tab** [A]: sound; **worn** (under half: "catches less"); **spent** (a quarter or less: "catches
  nothing").
- **Re-lining** [O]: wear is repaired **only with liner panels**, in proportion to the health missing, never by ordinary free repair
  at any health, and whatever the "destroyed parts need supplies" setting says. **Four panels line a cab** [A], so the upgrade costs
  four and a re-line costs one for each quarter missing, rounded up. The job is the one that fits any replacement part
  (`VehicleRepairs`); the supply is a `PhobosVehiclePartSupplyDef` for the liner's role, with a new flag for "wear needs supplies".
- **Weight stays 200 kg** however worn: a labelled simplification.
- **Other authors' vehicles marked as lined** get the same part from their loadout, born at full health, so their liners wear too.

### Armour plates stay the parts they are [O]

The owner's question covered plates too ("spall plates and the like"). **Plates are not split into parts of their own:** an armour kit
raises the armour of a part the truck already has (the cab, the engine, the tank), and that part's health is already the plate's. Combat
Extended scales a part's armour by its efficiency, which is nothing at a quarter of its health or less **[code]**, so a battered cab
already protects less. A plate as a second outer part on the cab's cells would break "one outer part to a cell": Combat Extended makes
a round beat every outer part on a cell, so the two would stack into armour nobody intended
([the armour design](vehicle-armour.md#one-outer-part-to-a-cell)).

### What the player sees

- **Wounded crew**, in their Health tabs as fragment wounds from the shooter.
- **Holed inner parts**, in our vehicle Health tab as usual.
- **The liner's own row** in the Health tab, with its health and its state: not fitted, sound, worn or spent [O, labels A].
- **No message per hit**: combat already says enough, and a message for every hit would flood.

## The four questions

| Behaviour | Lets the player | Stops them | Changes how they play |
| --- | --- | --- | --- |
| **Spall from a beaten plate** | See armour matter beyond "did it get through": a plate only just beaten still hurts the crew behind it | Keeping a crew unhurt behind a plate a round has just beaten | Heavier armour, or keeping out of range of weapons that beat it, is chosen for the crew's sake; wounded crew are rotated out |
| **Shock spall** | See a heavy shell that bounced still hurt the crew | Sitting out shellfire unhurt behind a plate that stops it | A vehicle under heavy fire is pulled back even when nothing gets through |
| **Spall liners** | Protect the crew of an armoured truck from its own plates | Fitting armour as if crews were safe behind any plate | A truck sent into fire gets a liner as well as a cab, and carries 200 kg more |
| **A liner that wears** | See how much protection is left, and restore it | Fitting a liner once and forgetting it | Liner panels are stocked and carried; a truck is re-lined after a hard fight |

**What outside the vehicle notices:**

- **The colony's doctors**, who treat the wounds.
- **The vehicle's systems**: fuel leaks, lost coolant, and a wounded gunner who aims and reloads slower ([knocked-out systems, row C4](vehicle-knocked-out-systems.md#c-crew-and-cab)).
- **Raiders**: a raider's crew wounded by spall loses the gunner sooner, and the bail-out rule (C2) empties a vehicle that can no longer fight.
- **The game's own records**, which credit kills to the shooter.

## Risk and settings

- **Risk: moderate, graded by the owner (21 Nov 2026), so it starts on.** The agent had proposed high: a crew member can be killed by
  one hit's spall, and the shared rule calls losing a person at once high. The case the owner's grade rests on: Combat Extended already
  wounds crew in these same hits; spall adds to wounds the player already accepts by driving into fire; the armour class on the Health
  tab shows what a plate stops; and the liner is the answer the game offers.
- **Settings** (D'thorga Framework, vehicles section; not built), each on by default [O for the first; A for the others, following it]:
  - **"Spall inside vehicles"**: the whole behaviour.
  - **"Spall on other mods' vehicles"**: every vehicle not ours.
  - **"Shock spall from heavy hits"**: spall from a stopped heavy hit.

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

- **Nothing new is saved.** The wounds are the game's own and Combat Extended's injuries; the liner's health is a part's health,
  which Vehicle Framework saves; the upgrade node and the part item are XML, keys stable once a save holds them.
- **An existing truck gains the liner part at zero health** ("not fitted"). Vehicle Framework saves parts by position and appended
  parts move, which our systems already put right by part key; that this holds for one more appended part is read before building.
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
4. **The liner part's life** (it replaces "how a vehicle knows it has its liner": the part's health says so):
   - a truck is born with every part at full health, so an unfitted liner must be set to zero when the vehicle is made;
   - fitting the upgrade must bring it to full once, never through a Vehicle Framework health upgrade, which repairs the part at
     every load;
   - Vehicle Framework's free repair must leave it alone at any health, and the re-line job must charge panels by the share missing;
   - a part at zero must not read as "destroyed" in alerts, the Fleet board or bay trips while the liner is simply not fitted;
   - existing saves keep every other part's health when the liner part is appended.
5. **A recording with `perf.py`** in a fight against an armoured vehicle, before and after.

## The owner's answers (cards, 21 Nov 2026)

| # | Question | The owner's answer | The agent's recommendation |
| --- | --- | --- | --- |
| 1 | The risk grade | **Moderate, starts on** | High, starts off |
| 2 | Which vehicles | **Every vehicle** with plates of the Rifle class or more, other mods' included, behind their own setting | The same |
| 3 | The liner | **As proposed**: needs the armoured cab, 200 kg carried mass, liner panels from synthread (one item picture) | The same |
| 4 | Spall without penetration | **Include it for heavy hits** | Leave it out of the first build |
| 5 | The liner (and the plates) as tracked parts with health | **The liner: yes, a part that wears.** The plates stay the parts they already are | The same |
| 6 | Putting a worn liner right | **Liner panels to re-line**, in proportion to the wear | The same |

## Who chose what

- **Owner (21 Nov 2026):** a design record first (card); Combat Extended first, since a version without it would be harder to build;
  vehicles, not people (the card's option to cover people was not taken); then the four answers above: moderate risk and on, every
  vehicle, the liner as proposed, shock spall for heavy hits; then the liner as a tracked part that wears, re-lined with liner
  panels, after asking whether "these spall plates and the like" should be "their own, individually, tracked parts in our vehicle
  code with health, status, etc.".
- **Agent:** everything marked [A]; the reactor route; the Rifle-class threshold; what counts as a heavy hit; the defaults of the
  second and third settings; the liner's health, wear and states; leaving the plates as the parts they are (recommended, and taken).

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
