# Guides and records

How the player guide and the development records are written, indexed and attributed. A session reads it before writing a guide, a
design record or a research note.

Split from the root `AGENTS.md` on 21 Nov 2026 (the owner's yes: "keep a slimmed-down AGENTS at the root"), every rule moved word
for word; the root's rules apply here too.

## Documentation

- Player guides live in each mod's README and `docs/`; contributor instructions, research, designs and
  records in `docs/development/`, each with its index. Add every new document to its index, **in one line**: the record, its date,
  what it is and its status (`scripts/check-doc-index.py`). Guides lead
  with player actions and match the implemented behaviour; dated records keep their original scope.
- **The player guide (`docs/player-guide/`) is the players' front door** (owner's request, 12 Nov 2026): a chapter per mod or
  area, saying what it does today, without version history, illustrated with the mods' own art. A change that alters what a
  player builds, presses or sees updates its chapter in the same commit. Its figures are drawn from shipped textures by
  `python scripts/art/guide_figures.py` (listed in `docs/player-guide/images/figures.json`); redraw them when a texture they use
  changes. The gate checks their sources exist.
- **A design record's status is one line at its top** (owner, 17 Nov 2026, after the documentation audit found about thirty records
  opening with "nothing is built" over a build): `**Status (date):**`, saying what is built and what is not, updated in the change that
  builds it. What a version delivered goes in its changelog; the record keeps only what differs from its design.
  `scripts/check-doc-status.py` refuses an opening that says nothing is built while the record records a build, and
  `scripts/check-doc-links.py` checks every `#anchor` against the headings it names.
- **The tie-in register (`docs/development/tie-in-register.md`) is held to the mods by the gate** (owner's question card, 24 Nov
  2026): a change that adds, removes or renames a mod's `IfModActive` folder (or its tie-in assembly folder) edits the register's row
  and its counts in the same commit; `tests/test_tie_in_register.py` fails while a mod's `LoadFolders.xml` and its section disagree.
- Diagrams are Mermaid and never Figma (the shared rules); do not suggest or mention Figma's connector either.
- Beyond the shared rule on writing plainly: gotchas at the top, and restate a key detail where it matters again instead of
  "see above".

## Research attribution

- Name the organisation or researchers whose work supports a claim, calculation or design decision, with
  a labelled primary-source link beside the claim: ITU-R recommendations, the ACMA's instruments, the
  WSJT-X authors' papers, Ludeon's announcements, other mod authors. Distinguish authorship from a hosting
  site.
- Separate the source's findings from our inference, simplified models, fictional equipment and authored
  balance. Never imply endorsement. Mark unverified references as unverified; never invent citations;
  never cite figures a design record lists as unverified. Keep citations in text and documentation, never
  in artwork. Repair missing attribution when you meet it.
