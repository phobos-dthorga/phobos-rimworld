# Vaccines and prevention for healthy pawns: design (7 Nov 2026)

A design record for D'thorga Medical. **Nothing is built and nothing is tested in game.** The owner asked for it on 7 November 2026
("design record now"); building waits for the owner's go.

## Read this first

- **What exists already** (read from the game's Defs and the owner's mods, 7 Nov 2026):
  - **The game's penoxycyline** makes a pawn immune to new **malaria, sleeping sickness and plague** for about five days (its condition
    falls 0.18 a day); players schedule it through drug policies. It does nothing for flu, gut worms, muscle parasites or mechanites, and
    nothing for someone already infected.
  - **Ushanka's Biological Warfare** (in the owner's list) has **vaccines for its own weaponised diseases** (its flu, malaria, plague,
    sleeping sickness and others, separate Defs from the game's): drug-lab items that give an 8-day immunity condition, each behind its
    own research. They do not touch the game's own diseases.
  - **D'thorga Medical's dosing part** already gives existing drugs by their effect, and gives drugs that help fight a disease already
    caught. It does nothing for a healthy pawn.
- **Existing effects first** (owner's rule): before anything new, doctors could give the drugs above to healthy pawns at the right time.
- Every figure here is a proposal, not tuned, and none is medical advice.

## Proposals (agent ideas)

| # | Proposal | What it adds | Uses |
| --- | --- | --- | --- |
| 1 | **Doctors give prevention drugs by themselves** | When a disease that a drug in stock prevents is around (an outbreak on the map, or the season's), doctors give that drug to healthy colonists whose prevention has run out, as the dosing part does for pain | Penoxycyline, Biological Warfare's vaccines, any mod's drug whose condition carries the game's `makeImmuneTo`; found by effect, never by name |
| 2 | **A head start, not immunity, for the game's other diseases** | A "primed immunity" condition from a vaccine of our own: when the disease strikes, immunity starts part-way up instead of at zero, so the patient usually wins the race | The game's immunity records; a hook when a disease is added |
| 3 | **Hygiene that prevents** | Clean water and washing lower the chance of gut worms and food poisoning | Dubs Bad Hygiene's water quality and hygiene need, where active; nothing without it |
| 4 | **Masks against airborne disease** | A worn mask lowers the chance of catching flu or plague from a sick colonist | Masks that already exist (the game's and Combat Extended's), found by their toxic environment resistance or gas mask tag |

## Proposal 1 in more detail (the cheapest, and all existing effects)

- **Who:** colonists (and, by setting, prisoners and guests) who are healthy, awake, not already protected, and not set to "no
  medicine". **When:** a disease the drug prevents is on the map (a sick pawn) or a season brings it (the game's own disease incidents
  are seasonal and biome-bound). **What:** the drug in stock whose condition lists that disease in `makeImmuneTo`, from the doctor's
  inventory first. **Not:** a pawn whose drug policy forbids it, or one with an aversion, unless the dosing part's existing setting allows medical drugs.
- **Why it fits:** it is the dosing part's own index and job, aimed at a new need; one more line in the load report; no new items.
- **Limit:** it only reaches diseases a drug already prevents. Flu, worms, parasites and mechanites need proposal 2.

## Proposal 2 in more detail (our own effect, where nothing exists)

- **Primed immunity:** a long-lasting condition (a season or a year, authored) from a vaccine item made at a drug lab, one item per
  disease group, or one broad vaccine as a simplification. When the matching disease is added, its immunity record starts at a share
  (for example a third) instead of zero.
- **Why not plain immunity:** a head start keeps the game's race, so a weak or old pawn can still lose, which plays better and is
  closer to how vaccines reduce rather than abolish risk (agent judgement, unsourced).
- **What it needs to check when built:** how the 1.7 immunity handler creates a record when a disease is added, and whether a hook
  there needs Harmony (the dosing part has one patch already; the rule is postfix, recorded reason).

## Open questions for the owner

| Question | Agent lean |
| --- | --- |
| Start with proposal 1 only? | Yes: it needs no new items and reuses penoxycyline and Biological Warfare's vaccines |
| Our own vaccines (proposal 2): one broad item, or one per disease group? | One per group (flu, worms and parasites, mechanites), behind one research |
| Should mechanites, which are not germs, be preventable at all? | No: they are machines; leave them to treatment |
| Hygiene and masks (3 and 4): this round or later? | Later, after 1 and 2 are tested |

## Sources

The game's Defs (Core `Drugs/Penoxycyline.xml`, read 7 Nov 2026) and Biological Warfare's Defs (its `CombatDiseases` folder, 1.6), read
locally; no outside figures are used.
