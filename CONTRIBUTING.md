# Contributing

Questions, design feedback, documentation fixes, translations, reproducible bug reports and code are
welcome. The project is in its design stage, so ideas and corrections to the radio physics are the most
useful contributions right now.

**Maintainer workflow (18 October 2026):** owner-directed maintainer and agent changes use checked commits
pushed directly to `main`. Pull requests are not required for that work unless the owner asks. The fork
and pull request steps below are for external contributors.

## A small, useful contribution

1. Search [existing issues](https://github.com/phobos-dthorga/phobos-rimworld/issues). Discuss a substantial
   feature before building it; small fixes can go straight to a pull request.
2. Fork and create a focused branch. Keep unrelated edits out.
3. Describe the problem, the resulting behaviour, the checks you ran and what remains uncertain.
   Screenshots help with UI changes; use GitHub-native Mermaid for diagrams.
4. Run the checks. None of them needs RimWorld:

   ```text
   python scripts/check-doc-links.py
   python scripts/check-mod-layout.py
   python -m unittest discover -s tests
   ```

5. Open a pull request. Expect review and possible revisions; there is no fixed review timetable.

Follow [community expectations](CODE_OF_CONDUCT.md). See [SUPPORT](SUPPORT.md) for gameplay reports and
[SECURITY](SECURITY.md) for sensitive vulnerabilities.

## Design and testing

Read [AGENTS.md](AGENTS.md) and the [project direction](docs/development/project-direction.md) before
changing behaviour. Prefer useful working slices. The UI presents state and delegates to services; data
lives in Defs so other modders can extend it; tie-ins with other mods are optional. Preserve saved
identities and `packageId`s.

Exercise the behaviour you change. Record the RimWorld version and mod list for gameplay observations;
builds and unit tests are not gameplay tests. Never commit game files, decompiled code, saves, credentials
or personal paths. Use synthetic test fixtures.

## Writing and research

Every change to a mod updates its `CHANGELOG.md` and README in the same commit. Player text follows
[writing READMEs and in-game text](docs/development/player-language.md) and lives in language files, not
code.

Name the researchers, standards bodies, regulators and mod authors beside the claims their work supports,
with primary-source links. Separate findings from inference and from authored gameplay balance. Never
imply endorsement.

## Authorship and licensing

Contribute only material you have the right to contribute. Preserve notices, and record third-party
material in [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.md). Original contributions follow [LICENSE](LICENSE).

Mods built on the radio framework are welcome. They are published by their own authors under their own
terms, and carry only their authors' files.

Forks and community maintenance are welcome within the licence. Suggested credit: "Based on Phobos
RimWorld by Phobos A. D'thorga." This is a courtesy request, not an extra licence condition.
