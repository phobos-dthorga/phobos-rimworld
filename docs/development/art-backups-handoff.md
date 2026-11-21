# Hand-off: backing up the art training sets and style models (21 Nov 2026)

**Status (21 Nov 2026):** built, except the Companion card. phobos-modkit 1.66.0's `safe-copies` (its `docs/safe-copies.md`),
`scripts/safe-copies.py` with `config/safe-copies.json`, the art folders as settings, and a step after every night plan. The first run
copied 1,587 files (1.07 GB) to K: and 382 of our own (145.7 MB) to the private repository dthorga-safe-copies; every copy verified,
and a restore from the repository came back byte for byte. **Still to build: the Safe copies card in D'thorga Companion**, and the
later steps (saves, Claude's notes, game settings). **The owner answered its main questions the same day: [the owner's
answers](#the-owners-answers-21-nov-2026) overrule the proposal below wherever they differ.** Every other choice marked *proposal* is the
agent's, for the owner to judge.

From the phobos-rimworld session that built the camp (Framework 0.212.0). **The owner's question (21 Nov 2026):** "Should we add backup
functionality for our training sets, models, etc. and the like within Companion, do you think?", then "Can you create a hand-off for what
you think the best way forward with this is, please?" It came after night 9 trained **house-v1**, the first style model of our own art
that draws the game's look by itself (158 minutes of training on the owner's graphics card).

## Read this first (gotchas)

- **Back up what cannot be made again, not what is big.** Of the 7.2 GB in the LoRA work folders, the house-v1 training set itself is
  **57 MB**: 151 pictures and their 151 captions. Its `_t_e_cache` (2.3 GB) and `_latent_cache` (214 MB) are the trainer's own caches,
  rebuilt at every training; leave them out. The 48 GB of base models in ComfyUI's `models` folder come back with
  `scripts/art/fetch_models.py`, which checks each against its recorded hash: back up the list, never the files.
- **Two kinds of material, two rules.** house-v1's set is our own art only (the owner's picks from the `art-masters` branch, read by
  `lora_experiment.py picks`). The **vanilla, vanilla-v2 and vanilla-v3 sets are drawn from the game's own textures**, and the three
  `phobos-private-vanilla*` models were trained on them. `lora_experiment.py` keeps its work folders "outside the repository and outside
  the synced drive" on purpose, so game-derived material is never committed or shared. **Game-derived material must never reach a synced
  or cloud folder or any git branch.** A backup that mixes the two kinds is the one way this tool could do harm.
- **The repository's own drive syncs.** Treat the Synology folder the repositories live in as shared for this rule. An open question
  follows from it (question 5).
- **A copy on the same physical disk is not a backup.** It survives a mistaken delete, not a failed drive. Where the copies go is the
  owner's choice (question 1).
- **Never delete at the source, and never prune a backup without the owner's confirmation** (AGENTS.md: never delete anything to free
  space without first telling the owner; D'thorga Companion's clean-up rule: it deletes only what the owner chose and confirmed).
- **No graphics card is involved.** It is disk work only, so it can run while the owner plays. Copying gigabytes may still slow the
  disk a little; keep the copies small by leaving the caches out.
- **The art pictures themselves are already safe.** Every generated image is on the `art-masters` or `art-references` branch, pushed to
  GitHub (assets/AGENTS.md). Request files are committed. This tool is for what those branches refuse: the private models and datasets.

## The owner's answers (21 Nov 2026)

Given on question cards after a second session checked this hand-off against the machine. They overrule the proposal below.

| Question | The owner's answer |
| --- | --- |
| The tool's name | **Safe copies**: command `safe-copies`, a Companion card "Safe copies", buttons "Copy now" and "Check the copies". This keeps it apart from the Mods page's install backups (`installs backup`) |
| Where our own material goes | **K:** (a separate physical SSD in this PC, not synced) **and a private GitHub repository**. Not the Synology folder, and no USB stick |
| The game-derived sets and models | **Copy models and sets to K: only**, never anywhere else. Without their caches the three sets are 634 MB (vanilla 336, vanilla-v2 291, vanilla-v3 7), plus the two models at 46 MB each |
| Later scope, each its own step after the art | **RimWorld saves** (14.8 GB in 46 files, not synced by Synology), **Claude's notes about the owner** (the memory folder and the global instructions), and **game settings** (RimWorld's Config folder and RimSort's lists) |

What follows from the answers (agent's reading, for the owner to correct):

- **The private repository is created by the owner**, or by an agent only on the owner's word at the time: creating a repository is
  outward-facing. Its name and visibility are the owner's choice. It takes our own material only. The tool refuses to push anything
  game-derived there, as it refuses any other destination not marked private and local.
- **Each model is 46 MB, under GitHub's 100 MB limit for a single file**, but every new training adds 46 MB or more to the repository's
  history for good. Only final models go there; checkpoints and `optimizer.pt` stay on K: alone.
- **Claude's notes are personal.** They go to K: only, until the owner says otherwise.
- **Questions 3 to 5 below were not asked.** Their agent defaults stand: checkpoints kept for the newest training only; a card on
  Companion's Home; `.local/references` left where it is (`.local/` already holds game-derived research by standing rule).

## Found by the second session (21 Nov 2026)

- **The training folders are hard-coded**: `WORK_ROOT` in `scripts/art/lora_experiment.py` (`C:\AI\lora-experiments`), and `AI_TOOLKIT`
  in `scripts/art-run.py`, alongside `COMFYUI_PATH`, which can be changed only through an environment variable. A backup list naming a
  settings key for each root needs those keys first: declare them in `config/settings.json`, with today's folders as their defaults,
  so nothing moves. This is the first step, a commit of its own.
- **Prove a set is ours; do not trust its label.** house-v1's `manifest.json` holds a hash for each picture, and all 151 match the
  dataset (checked 21 Nov 2026). The tool hashes each picture of an `own` set against its manifest, and anything it cannot prove is
  treated as game-derived. The `own-art.json` marker alone is not proof.
- **Detect a synced or shared destination as well as reading the owner's mark.** A `.SynologyWorkingDirectory` folder sits at
  `D:\SynologyDrive`; the tool looks for it, and for a `.git` folder, above any destination. Game-derived material needs both: the
  owner's "private and local" mark, and nothing detected.
- **The kit's safety rules list every tool that writes.** `safe-copies run` (to the destinations chosen) and `restore` (to a folder the
  owner names) are one more writer, so the kit's AGENTS.md needs a line for it, on the owner's word, in the commit that adds the tool.
- **The recipe records what really rebuilds a model**: this repository's commit, ai-toolkit's commit (`e9c72b7` on 21 Nov 2026), the
  base models' hashes from `models.json`, the game's version, and the training config.
- **Copy only what changed, at the end of every night plan**, not only after a training, so a caption edited by hand is caught too.
  A changed file's earlier version is kept beside it, never overwritten. No pruning in the first version.
- **The copies are plain folders** you can open: a mirror of the files, plus one JSON record for each run. Neither needs the tool to
  read it.

## What exists today (measured 21 Nov 2026)

| What | Where | Size | Kind | Can it be made again? |
| --- | --- | --- | --- | --- |
| house-v1 training set | `lora-experiments/house-v1/dataset` (PNG + TXT) | 57 MB | Our own art | **No**: the owner's picks and their captions |
| house-v1 caches | `dataset/_t_e_cache`, `dataset/_latent_cache` | 2.5 GB | Derived | Yes, by the trainer, automatically |
| house-v1 configs, manifest, run reports, logs | `lora-experiments/house-v1/*.yaml, manifest.json, run-*.md, train-*.log` | under 1 MB | Our own | **No**: the record of what worked |
| house-v1 checkpoints | `lora-experiments/house-v1/output` | 266 MB | Our own | Only by retraining. The final file is the installed model; the others (2,000 to 2,750 steps) and `optimizer.pt` are for resuming |
| The three style models in use | ComfyUI `models/loras/phobos-private-*.safetensors` | 46 MB each | house-v1: our own; vanilla and vanilla-v2: game-derived | Only by retraining (about 2.5 hours each) |
| vanilla, vanilla-v2, vanilla-v3 sets | `lora-experiments/vanilla*` | 1.3 to 2.0 GB each | **Game-derived** | Yes: `.local/tools/art/extract_references.py` and `lora_experiment.py` rebuild them from the game's files |
| Third-party LoRAs (sprite sheet, RefControl depth) | ComfyUI `models/loras` | | Others' (licensed) | Yes: downloaded again |
| Base models | ComfyUI `models` | 48 GB | Others' (licensed) | Yes: `fetch_models.py`, hash-checked |
| Night plans, picking pages, pick lists | the art work folder (`artWorkPath`, now on K:) and `.local/art` | small | Our own | **No**: which image was picked, and why |

## The best way forward (proposal)

1. **A shared tool in phobos-modkit: `backup`.** Generic, game-free, so the Ostranauts art and any later game get it too. Commands:
   `plan` (what would be copied, sizes, which rule each file falls under), `run`, `verify` (re-hash the copy against its manifest),
   `status` (last run per set, what has changed since) and `restore` (to a folder the owner names, never over a newer file without
   asking). Copies are incremental by content hash, written new then renamed into place, and each run writes a manifest: file, size,
   hash, kind, when. It reads every path through the shared order (`paths.resolve`) and declares its settings in the central settings
   file (scripts/AGENTS.md), never a machine path in committed config.
2. **What to back up is each repository's own list**: `config/backups.json` here. One entry per set, naming a settings key for its root,
   include and exclude patterns, and its **kind**:
   - `own`: may go to the main destination. house-v1's pictures, captions, configs, manifest, run reports and logs; the installed
     house-v1 model; night plans and pick lists.
   - `game-derived`: may go **only** to a destination marked private and not synced, or nowhere. The vanilla sets and the two vanilla
     models. The tool refuses to write this kind anywhere else, and says so.
   - `recipe`: never copied; the tool records the command lines and hashes that rebuild it (the base models through `fetch_models.py`;
     the game-derived sets through their `lora_experiment.py` commands, which the run reports already hold).
   Leave out the caches by pattern (`_t_e_cache/**`, `_latent_cache/**`). Keep the final model of every training; the intermediate
   checkpoints and `optimizer.pt` only for the newest training of each set (the owner may want more: question 3).
3. **After every training, by itself.** `art-run.py` runs `backup run` once a `train` step and its `install` have finished, as a step of
   the night plan, and the night summary says what was saved. A failed backup never fails the night; it is reported.
4. **D'thorga Companion shows it** (built in phobos-companion, under its own AGENTS.md): a Backups card with each set's last backup,
   anything new or changed since, and a warning for a model or training set that has never been backed up. Buttons **Back up now** and
   **Check the copies** (verify), each through the kit's manifest as Companion's host requires. **Restore** asks where to and confirms.
   Which page holds it is the owner's call (question 4); record the choice in Companion's `config/decisions.json`.
5. **Tests** (synthetic fixtures in a temporary folder, never the owner's real files): a game-derived file is refused at a synced or
   unmarked destination; caches are left out; an unchanged file is not copied twice; a changed one is; verify catches a corrupted copy;
   restore never overwrites a newer file unasked. Anything a test leaves gets a rule in `config/cleanup.json`. A test that writes
   history or settings points `PHOBOS_HISTORY` and `PHOBOS_SETTINGS` into its own folder.

**Order of work:** the kit tool with its tests and docs (`docs/backup.md` in phobos-modkit); the pin moved here with
`config/backups.json` and the night-plan step; then the Companion card. Take a claim on each version before using it.

## Why not simpler

- **Copying the whole `lora-experiments` folder** would take 7.2 GB each time, mostly caches, and would carry game-derived sets to
  wherever it went.
- **A git branch** is refused by our own rules for the private models and sets, and would publish them.
- **A cloud service** goes against the owner's preference for things owned, not leased, and for game-derived material against the rule
  above. The owner may still choose one for the `own` kind.

## Questions for the owner

Questions 1 and 2 are answered ([the owner's answers](#the-owners-answers-21-nov-2026)); 3 to 5 keep their agent defaults until the
owner says otherwise.

1. **Where should the main copy go?** The Synology (off this PC, synced), another drive in the PC, an external drive, or more than
   one. A copy in the same house shares the house's risks; that is the owner's judgement, not the tool's.
2. **The game-derived sets and models:** back them up to a private, unsynced place (which?), or keep only their recipe and rebuild
   when needed (agent default: recipe only, since the game's files and our scripts rebuild them)?
3. **How many trainings' checkpoints to keep:** the newest only (agent default), or every training's?
4. **Where in Companion:** its own Backups page, or a card on Home or the Mods page?
5. **`.local/references` sits inside the repository folder, on the synced drive**, and holds reference pictures extracted from the
   game (7.1 MB). It has been there since 5 Nov 2026, so it already syncs. Is that acceptable to the owner, or should it move beside
   the LoRA work folders? This hand-off only notes it; nothing was moved.
