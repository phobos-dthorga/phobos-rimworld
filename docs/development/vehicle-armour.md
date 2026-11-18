# Armour for our armed vehicles: Combat Extended first, Vehicle Framework as the fallback (18 Nov 2026)

**Status (18 Nov 2026):** a design record. No part of it is in code yet, and nothing here is tested in game. Its questions are
put to the owner with [the Osprey's](vehicle-osprey.md).

The owner decided on 18 November 2026 how armour works on the armed vehicles of [modern military vehicles](modern-military-vehicles.md):
"Combat Extended is the priority, with Vehicle Framework a fallback for other users who go without CE for when we finally publish
our mods". This is that design: row MV2 in [outstanding work](outstanding-work.md). It is written for every armed vehicle of ours
and worked through on the first, the [V-22 Osprey](vehicle-osprey.md).

Labels: **[code]** read in code on 18 Nov 2026 by research helpers, never run; **[P]** a primary source; **[S]** a secondary one;
**[I]** our inference; **[A]** an authored gameplay choice. Combat Extended's source was read at its public commit `d847807b` (15
Nov 2026) and was **not** compared with the installed build; Vehicle Framework's at `026420b5`, which matches build 1.7.2537.

## Read this first

- **One vehicle Def, two scales.** A Def of ours carries Vehicle Framework's ratings. A patch in a folder that loads only with
  Combat Extended replaces every part's armour with millimetres and megapascals. Every author checked does it this way.
- **A vehicle's armour is named by class, and each class is a threat it stops.** We source what a real machine is protected
  against, then use the class just above the Combat Extended round of that threat. The label on every figure is "sourced threat,
  authored millimetres".
- **A gate check keeps the two scales together**, so no vehicle ships with a plate that reads 0.8 mm under Combat Extended.
- **One outer part to a cell.** Under Combat Extended every outer part on a hit cell must be beaten in turn, so overlapping outer
  parts stack into armour nobody intended. Our Defs give each cell one outer part. No patch.
- **Under Combat Extended a vehicle is hit only through a box one cell wide at its centre**, whatever its footprint. One small
  postfix widens it for a Def that asks, as the Hind's and the Harrier's authors do.
- **An aircraft in the air is shot at, and shoots, as from the ground** in the first build (agent recommendation; a question for
  the owner). Real height under Combat Extended needs patches on code that runs for every bullet every tick.
- **Nothing here changes another author's vehicle.**

## What each behaviour lets, stops and changes

| Behaviour | Lets the player | Stops them | Changes how they play |
| --- | --- | --- | --- |
| **Armour by class, per part** | Know what a vehicle shrugs off and what goes through, in real units under Combat Extended | Treating an unarmoured aircraft as a tank | Where a vehicle is taken, and which gun the enemy has, matter |
| **An armour kit as an upgrade** | Buy protection for a job that needs it | Having it for nothing: it costs weight | A kit is fitted for a hot landing and left off for a long haul |
| **A worn part protects less** | See why a battered vehicle dies faster | Ignoring damage between fights | Repairs before the next sortie |
| **A wider target under Combat Extended** | Nothing new for the player; it is fairness | A large aircraft being missed by everything not aimed at its centre | Enemy fire on a big vehicle lands as its size suggests |

## What the code does

### With Combat Extended **[code]**

- **Who decides a hit.** Combat Extended's prefix on `VehicleStatHandler.ApplyDamage` replaces Vehicle Framework's armour step
  while its setting "Use New Vehicle Armor" is on, the default.
- **One part.** A part's armour is its rating times its efficiency; efficiency falls to nothing at a quarter of its health or less.

| Case | Result |
| --- | --- |
| Armour at least the round's penetration | Stopped. The part loses damage × (penetration ÷ armour)². Nothing passes on |
| Armour below it | The part loses damage × (armour ÷ penetration) × (2 − armour ÷ penetration). What passes on: penetration less armour, and damage × (1 − armour ÷ penetration) |
| Then the part's own bulk | The same rule again, with armour of its remaining health ÷ 50 |
| A blunt hit with under half the armour's figure | No damage at all |

- **Layers.** Every living outer part on the hit cell is met in turn, a smaller `hitWeight` tending to come first; then one inner
  part, a chance to hit crew, and the next cell along the shot's line.
- **Blasts** are blunt: penetration is the larger of three tenths of the damage and the blast's own figure, falling with distance.
  Vehicle Framework applies one hit per edge cell of the footprint the blast reaches.
- **Where a shot can land.** Combat Extended finds a vehicle from any cell of its footprint but tests the shot against one box at
  the vehicle's drawn centre: one cell wide, as tall as the Def's `fillPercent`. Rotation and footprint are ignored.
- **Which cell takes it.** Vehicle Framework then picks the footprint cell nearest the shooter, not where the shot struck.
- **A vehicle's turrets** fire through `Vehicles.CETurretDataDefModExtension` on the turret Def: the ammunition set, speed, sway,
  spread, recoil and a shot height. **Recoil defaults to −1 and is always added to the angle**, so every turret Def of ours sets it.

### With Vehicle Framework alone **[code]**

- Armour is a rating on the game's scale, where 1.0 is 100 %. The armour less the hit's penetration is the chance to deflect it
  or to halve it. A part's health adds nothing, and the **larger** `hitWeight` is hit first, the opposite of Combat Extended.

### What the rounds are

Combat Extended's own figures, as its installed Defs state them **[code]**:

| Round | Damage | Sharp (mm) | Blunt (MPa) |
| --- | --- | --- | --- |
| Small fragment | 12 | 2 | 14.4 |
| Large fragment | 34 | 4.5 | 122.5 |
| 7.62x39 mm ball | 17 | 5.5 | 41.08 |
| 5.56x45 mm ball | 14 | 6 | 34.18 |
| 7.62x51 mm ball | 20 | 7 | 66.72 |
| 7.62x51 mm armour-piercing | 12 | 14 | 66.72 |
| .50 BMG and 12.7x108 mm ball | 42 | 14 | about 360 |
| .50 BMG armour-piercing | 26 | 28 | 360.34 |
| 14.5x114 mm armour-piercing | 33 | 36 | 634 |
| 23x152 mm armour-piercing | 56 | 45 | 1,787.72 |
| 30x173 mm armour-piercing | 80 | 70 | 3,849.48 |
| RPG-7 shaped charge | 304 | 500 | 44.956 |

Only the .50 armour-piercing round was checked against the file of patched Defs, which dates from 8 Nov 2026; the rest are as
written. Combat Extended defines no shoulder-fired anti-air missile; the one on the owner's list is The Dead Man's Switch -
Expedition's, a base-game explosive with no Combat Extended figures.

### What other authors give their aircraft

Under Combat Extended, in the owner's game **[code]**, sharp then blunt:

| Aircraft | Skin or body | Engines | Cockpit | Notes |
| --- | --- | --- | --- | --- |
| Mi-38 (Rimarrow) | 1 / 2 | 2 / 4 | glazing 0.5 / 1 | 24 parts; vehicle-wide 1.5 / 3 |
| Smuggler, Mosquito (Vanilla Vehicles Expanded) | 1 / 2, 1.25 / 2.5 vehicle-wide | | | Combat Extended's own patch |
| Harrier | 2.5 / 6 | 3.5 / 8 | 5 / 10 | |
| Cherokee, Louie (VVE Tier 3) | 6 / 10, 6.5 / 11 vehicle-wide | | | Combat Extended's own patch |
| Mongoose | 9 / 18 | 7 / 14 | 6.5 / 11 | An attack helicopter |
| Hind | 11.25 / 22.5 | 8.75 / 17.5 | 8.1 / 13.75 | An armoured gunship |

**These are ratings, not measurements [I]**: a Chinook's skin does not stop a rifle bullet, and the Cherokee's 6 mm does. We do
not follow them where a source says otherwise, and we do not change them.

## The design

### Armour classes

Every armoured part of ours names one class. The millimetres are set just above the round the class stops, so the round is
stopped and the part still takes the bruise the first table row gives.

| Class | Stops (Combat Extended) | Sharp, mm | Blunt, MPa | Vehicle Framework rating, sharp / blunt |
| --- | --- | --- | --- | --- |
| **Skin** | Nothing aimed. Thin aluminium or composite | 2 | 4 | 0.10 / 0.08 |
| **Splinter** | Fragments; pistol rounds | 5 | 20 | 0.25 / 0.20 |
| **Rifle** | 5.56 and 7.62 mm ball | 8 | 40 | 0.45 / 0.35 |
| **Armour-piercing rifle** | 7.62 mm armour-piercing; .50 ball | 15 | 120 | 0.65 / 0.50 |
| **Heavy machine gun** | .50 armour-piercing | 30 | 250 | 0.85 / 0.65 |
| **Heavy machine gun, Eastern** | 14.5 mm armour-piercing | 38 | 400 | 0.95 / 0.75 |
| **Autocannon** | 23 mm armour-piercing | 48 | 1,000 | 1.10 / 0.90 |
| **Heavy autocannon** | 30 mm armour-piercing | 75 | 2,200 | 1.30 / 1.05 |

- **Sharp figures:** each is the stopped round's penetration plus a little **[A, from the code's figures]**.
- **Blunt figures are authored.** No real-world source gives megapascals for a plate. They rise with the class so that a class
  shrugs off the blunt part of what it stops; they are the first figures to tune in play.
- **Vehicle Framework ratings are authored**, on the scale its own vehicles use, and rise in the same order. They are for
  players without Combat Extended and cannot mean the same thing: there a rating is a chance, not a thickness.
- **A real machine's protection is sourced as a threat** ("resistant to 12.7 mm rounds"), never as a plate thickness, and the
  class is chosen from the threat. Where no source states one, the part is Skin and the record says so.

Worked on the Skin class, a 7.62x51 mm ball round (penetration 7, damage 20) on a 150-health panel: the skin's 2 mm takes 9.8
damage and passes on 5 mm and 14.3 damage; the panel's bulk (3 mm) takes 12 and passes on 2 mm and 5.7 damage to whatever is
inside. **Rifle fire goes through an unarmoured aircraft and hurts what is in it**, which is what the Osprey's sources describe.
The same round on the Rifle class (8 mm) is stopped; the plate loses 15.

### How a Def carries it

- **The vehicle Def** gives every part its Vehicle Framework rating, and no vehicle-wide armour: a part with its own armour
  ignores vehicle-wide upgrades **[code]**, and mixing the two is where surprises come from.
- **The Combat Extended folder** (`Mods/CombatExtended`, already in D'thorga Vehicles) holds one patch file per vehicle. It
  replaces each part's armour and each armour upgrade's figures, sets `fillPercent` as the body's height, adds the turret
  extensions and swaps each turret's projectile for Combat Extended's.
- **An armour kit is an upgrade node** that raises named parts by one or more classes and adds its weight to the mass stat. Its
  Combat Extended figures are in the same patch file.
- **`hitWeight` is written for both.** The two frameworks read it opposite ways, so the Combat Extended patch replaces it too.

### One outer part to a cell

Combat Extended makes a shot beat every living outer part on the hit cell. Our Defs lay outer parts out so that no cell has two:
the skin is cut into regions (nose, cabin, wings, nacelles, tail), each with its own hit box, and everything else is inner. The
Hind's and the Harrier's authors instead patch the penetration step to pick one outer part; we need no patch.

### A wider target

`PhobosCollisionExtension` on a vehicle Def gives a width in cells for Combat Extended's hit box. One postfix on
`CE_Utility.GetCollisionBodyFactors`, in our Combat Extended assembly, applies it: a type check and a look-up cached per Def. The
Hind's width is 4.5 and the Harrier's 5 **[code]**. Without it a large vehicle is hit only by shots through its centre cell.

This method runs often. The postfix is kept to those two steps, and its cost is part of the performance recording made when the
first armed vehicle is built.

### In the air

**Agent recommendation: in the first build an aircraft in the air has the same hit box it has on the ground, and its guns fire
from the turret Def's ordinary shot height.** A wall between a shooter and the aircraft blocks the shot both ways, as
[our own flight over the map](vehicle-flight-over-the-map.md#being-shot-at-and-shooting) already accepts for line of sight, and as
both existing flight mods do: neither raises its aircraft **[code]**.

What real height under Combat Extended would take **[code]**:

| Need | Where | How often it runs |
| --- | --- | --- |
| Raise the hit box | A postfix on the private `CollisionVertical.CalculateHeightRange` | For every projectile every tick, for every cell on its path and twice for each thing it might hit |
| Aim at the raised box | `Verb_LaunchProjectileCE.GetTargetHeight`: shooters aim at a share of the box's top, so a thin raised box is aimed under | Each aimed shot |
| Fire from height | Prefixes on `CE_Utility.LaunchProjectileCE` and `VehiclesCompat.ProjectileAngleCE`, never on Vehicle Framework's shared hooks | Each shot of ours |

Not traced: how Combat Extended's explosive rounds and fuses, its roof test above a height of 2, and raiders' choice of target
treat a raised vehicle. **What it would give:** guns that fire over walls and are fired at over walls, and targets under a roof
that an aircraft cannot hit. It is the more truthful model and the riskier one; it is [question 5](vehicle-osprey.md#questions-for-the-owner)
in the Osprey's record.

### What cannot reach it

[Our own flight](vehicle-flight-over-the-map.md#being-shot-at-and-shooting) refuses a hit that cannot reach an aircraft in the
air. Every ordinary hit, Combat Extended's included, ends in `VehicleStatHandler.ApplyDamage` **[code]**, where our prefix already
runs first. A melee hit carries a tool and a ranged one does not, so the rule can tell them apart. Vehicle Framework's own direct
part damage (collision scratches, its vaporise path, exploding parts) does not pass through there and is left alone.

### What the player sees

The Health tab shows each part's armour in the stat's own units (fixed in Framework 0.140.1): "mm RHA" and "MPa" with Combat
Extended, a percentage without. Plain mode adds the class's name and what it stops; nothing else is new.

### The gate check

`scripts/check-vehicle-armour.py`, part of the gate: for every vehicle Def of ours with a part above Skin or a turret, the Combat
Extended folder must replace that part's armour and that turret's projectile, each armour figure must be one of the classes'
(or carry a comment saying why not), and no cell may hold two outer parts. It reads XML only.

## Other factions' vehicles

| Where | What happens |
| --- | --- |
| **A raid on us** | Armour is in the Def, so it is the same on any faction's vehicle. Their gunners' rounds meet ours under the same rules |
| **Parked at a base we attack** | The same: armour needs nothing to tick |
| **One we capture** | Unchanged; its kit, if fitted, stays fitted |
| **Cost with several on a map** | None beyond Combat Extended's own. The width postfix is a look-up per call |
| **What the player is shown** | An enemy vehicle's armour as the game already shows any vehicle's |

## To read or prove before building

1. **That the installed Combat Extended matches the source read.** `scripts/assembly-archive.py` holds its builds.
2. **How Harmony takes a postfix on `GetCollisionBodyFactors` beside the Hind's and the Harrier's**, with `scripts/query-patches.py who`.
3. **Vehicle Framework's own ratings on its stock vehicles**, to set the fallback column against something a player has met.
4. **The blunt column in play**: a first sortie's log, with verbose logging on, says whether fragments and blasts behave.

## Who chose what

- **Owner (18 Nov 2026):** Combat Extended first, Vehicle Framework the fallback; vehicles armed as the real machine.
- **Agent recommendations:** classes named by the threat stopped; XML patches with a gate check, in place of a class table filled
  in by code at start-up; one outer part to a cell; the width extension; ground-level hit boxes in the air for the first build.
- **Authored:** every blunt figure; every Vehicle Framework rating; the margin of each sharp figure over its round.

## Sources

Findings are the sources'. The classes and every authored figure are ours; no source endorses them.

| Source | What it supports | How read |
| --- | --- | --- |
| Combat Extended (CE Team), public source at commit `d847807b`, 15 Nov 2026, and its installed Defs | The armour arithmetic, the hit box, turret firing, every round's figures | The clone and the Workshop folder; not compared with the installed assembly |
| Vehicle Framework (SmashPhil), public source at commit `026420b5` | Its armour step, part fields, upgrades, the turret extension | The clone |
| The Hind's, the Harrier's and VTOL Vehicle's assemblies, decompiled | That neither raises its aircraft under Combat Extended; their width and one-part patches | Local decompiles; summarised in our words, never copied |
| The aircraft Defs on the owner's list, with Combat Extended's patches | The table of other authors' armour | `scripts/query-defs.py`; four from the patched file, four as written with their patch files, because the patched file predates their updates |
| [Modern military vehicles](modern-military-vehicles.md), "Armour (decision 1)" | The first read this record builds on | Our own record |

**Unverified:** that Combat Extended's blunt megapascals correspond to any real plate; Vehicle Framework's stock vehicles' ratings
(not read for this record).
