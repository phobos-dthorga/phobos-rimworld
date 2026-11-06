## Problem and change

What does this fix or make possible for a player, modder or contributor?

## Validation

What checks did you run? Separate builds and unit tests from gameplay tests, and give the RimWorld
version (1.7 leads, 1.6 is the minimum) and mod list for gameplay observations. State what remains uncertain.

- [ ] `python scripts/check-doc-links.py`, `python scripts/check-mod-layout.py` and `python -m unittest discover -s tests` pass
- [ ] `dotnet test` passes for every rule-test project whose rules changed
- [ ] The mod's `CHANGELOG.md` (Unreleased) and README are updated in this change, and `modVersion` if it needs a new version

## Compatibility and attribution

Mention changes to `packageId`s, defNames, saved data, load order, dependencies or translations. Credit
sources and keep applicable licences. Delete this section if it doesn't apply.
