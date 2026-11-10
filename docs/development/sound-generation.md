# Sound generation: research and pipeline (10 Nov 2026)

**Status:** the tools are built and their offline tests pass; the first real sounds (the pilot) are the next step.
**No sound is in any mod**, and nothing here is tested in game.

## Read this first (gotchas)

- **Local, like the icon pipeline.** Stable Audio 3 Small SFX runs in the owner's ComfyUI (0.41.0 already supports it), driven
  by `scripts/art-run.py` with the same stop time, game check and records as images. On the graphics card it never runs while
  RimWorld is open. **With `--cpu` it runs beside the game** on the processor (see [Running beside the game](#running-beside-the-game)).
  A run needs the owner's go either way.
- **ffmpeg does every audio job** (owner, 10 Nov 2026: "a much finer tool"): the gyan.dev full build, installed with
  `winget install Gyan.FFmpeg --scope user`. It needs the full build for `rubberband`. Python only drives it.
- **Masters are never changed.** Each candidate is a lossless FLAC kept byte for byte; every copy anyone hears is made from it.
- **Licence:** generated sounds are ours, and shipping them in a free mod needs no notice (see [Licence](#licence)).
- **The quiet rule stands** (AGENTS.md): sounds only where usefulness is high, quiet, never alarming, with a volume setting.

## What it lets us do

| Question (AGENTS.md) | Answer |
| --- | --- |
| What it lets the owner do | Make sound effects for our mods locally, with no subscription or credits, and judge them by eye and number as well as ear |
| What it stops | Picking a sound on listening alone, which misses hiss, whine and clicks for anyone who loses the high frequencies |
| How it changes the work | Sounds follow the image route: a request file, a run with safeguards, a review page, a pick, a record |

## Who chose what

| Choice | By |
| --- | --- |
| A local sound pipeline like the icon one | Owner, 10 Nov 2026 |
| ffmpeg for all processing, not Python audio libraries | Owner, 10 Nov 2026 |
| A lowered listening copy with the high frequencies moved down | Owner, 10 Nov 2026 ("just for the moment") |
| Go for the tools and the pilot | Owner, 10 Nov 2026 |
| Running small models on the processor beside the game | Owner, 10 Nov 2026 (asked whether it could share the PC with RimWorld) |
| Which processors, the priority, the thread count and the free-memory floor | Agent choices |
| Stable Audio 3 Small SFX as the model; its distilled twin as the comparison; Medium optional | Agent choice |
| Every threshold, the review loudness, the loop crossfade, the split frequency | Agent choices, named constants in `scripts/art/sound_review.py` |
| The pilot's four sounds and their wording | Agent choice, for the owner's review |

## Models compared

| Model | Made by | Weights' licence | Verdict |
| --- | --- | --- | --- |
| **Stable Audio 3 Small SFX** (base and distilled, 2.27 GB each) | Stability AI | Stability AI Community License | **Used.** Made for sound effects, up to 2 minutes, stereo, trained on "licensed and Creative Commons data" (Stability) |
| Stable Audio 3 Medium (base and distilled, 9.2 GB each) | Stability AI | Same | Optional comparison: up to about 6 minutes, music as well |
| Woosh (Flow, DFlow) | Sony AI | CC BY-NC 4.0 | Second opinion only: a community ComfyUI node with an open "fails on the second run" issue |
| TangoFlux | declare-lab | Non-commercial research only | Ruled out |
| MMAudio | Ho Kei Cheng and colleagues | CC BY-NC (secondary source), built for video | Ruled out |
| HunyuanVideo-Foley | Tencent | Hunyuan licence, said to exclude the EU, UK and South Korea (secondary source) | Ruled out, as Hunyuan3D was |
| Stable Audio Open 1.0 | Stability AI | Community License | Replaced by version 3 |

Weights come from Comfy-Org's packaging (`Comfy-Org/stable-audio-3`, not gated; the Hub lists its licence as
`stable-audio-community` and links Stable Audio Open 1.0's `LICENSE.md`). Stability's own repositories ask for a click-through
acceptance. `scripts/art/fetch_models.py --only stable_audio_3_small_sfx` and `--only t5gemma` fetch what the pilot needs,
checking the Hub's licence and each file's SHA-256; Medium is marked optional and waits for `--optional` or `--only medium`.

## Licence

The Stability AI Community License Agreement (last updated 5 July 2024), read 10 Nov 2026. The agreement never names Stable
Audio 3; Stability's announcement ties the two ("Under the Stability AI Community License, you own your outputs").

- **Outputs are ours:** "You own any outputs generated from the Models or Derivative Works to the extent permitted by applicable
  law."
- **Outputs are not Derivative Works:** the definition ends "but do not include the output of any Model".
- **The notice duty does not reach a mod's sounds:** the "Powered by Stability AI" and Notice duties apply when we "distribute or
  make available the Stability AI Materials or a Derivative Work". Shipping weights or a fine-tune would trigger them; shipping
  sounds does not. Our reading of the text, not legal advice.
- **A free mod is a Non-Commercial Purpose** ("personal use (i.e., hobbyist)"), which needs no registration. Commercial use is
  free under US$1M a year in revenue, with registration.
- **One limit:** no output may be used "to create or improve any foundational generative AI model".

## The pipeline

```mermaid
flowchart LR
    R[Request file] --> A[art-run.py generate]
    A -->|ComfyUI, GPU| F[FLAC master + record]
    F --> S[sound_review.py, ffmpeg]
    S --> P[Listening page]
    P --> O[Owner's pick]
    O -.-> E[Export to a mod: not built yet]
```

A sound request, beside the image requests in an asset folder (`assets/phobos-sounds/requests/pilot.json`):

```json
{"name": "generator-loop", "engine": "sa3-sfx-base", "seconds": 8, "loop": true, "seeds": [11, 12],
 "prompt": "Small diesel generator running steadily ... calm. Length: 8 seconds"}
```

- **Engines** (`scripts/generate-art.py`): `sa3-sfx-base` (50 steps, guidance 7), `sa3-sfx` (8 steps, guidance 1),
  `sa3-medium-base`, `sa3-medium`. The settings are ComfyUI's Stable Audio 3 Medium templates; **ComfyUI has no Small SFX
  template, so using them for Small SFX is an assumption the pilot tests.** The graph is the templates' core without their Qwen
  prompt rewriter: our prompts are written the way its sound-effects guide asks (source, material, space, how it changes, and a
  closing "Length: N seconds").
- **Run:** `python scripts/art-run.py generate assets/phobos-sounds/requests/pilot.json --stop-at HH:MM`. After the sounds,
  it runs `scripts/art/sound_review.py` and names the listening page in its report.
- **Records:** each FLAC has a JSON record beside it: engine, model file and SHA-256, text encoder, prompt, seed, steps,
  guidance, seconds asked, whether it loops, the ComfyUI version, the time taken and an AI disclosure.

## The listening page

`sound_review.py` writes `sound-review.html` (dark) in the generated folder. Each candidate gets:

| Item | What it is |
| --- | --- |
| **As made** | The untouched master |
| **Review level** | Ends trimmed of silence, faded, levelled to −20 LUFS (true peak kept under −1 dBTP), so loudness never decides a pick |
| **Lowered** | The review copy with everything above 3 kHz moved down an octave by rubberband, mixed back over the untouched lows: the frequency lowering hearing aids do. A listening aid only, never shipped; it shows *that* something is up there, not how it sounds to others. `--octaves 2` moves the highs down two octaves |
| **Whine alone** | Only for a sound with the whine flag: a band around the whine, moved down to 1 kHz and set to the review copies' −20 LUFS, so it can be heard plainly (at −26 LUFS the owner could only just hear it). Added 10 Nov 2026 because the pilot's hum whine sat 31 dB under the hum, and the owner could not pick it out of the lowered copy. A listening aid only, never shipped |
| **Loop** | For a looping request: a circular crossfade of 0.25 s, so the end runs into the start; plays on repeat |
| **Spectrogram** | Time left to right, pitch upwards, brightness for loudness. A bright band near the top is hiss or whine; a thin upright line is a click |
| **Seam picture** | The loop played twice: a line down the middle is its seam |
| **Numbers and flags** | Measured on the master; see below |

Flags are reasons to look again, not verdicts. All thresholds are authored except where a standard is named.

| Flag | When |
| --- | --- |
| Clipped | A sample peak at −0.1 dBFS or more, or a true peak above 0 dBTP |
| Nearly silent | A mean level under −60 dB |
| Strong high frequencies | The band above 4 kHz within 12 dB of the whole sound's mean level |
| Sharp attack | The true peak more than 20 dB above the integrated loudness: may startle |
| Loop seam | The level either side of the seam differs by more than 3 dB |
| Seam dip | In 20 ms slices of the loop played twice, the quietest slice within 0.15 s of the join falls more than 3 dB under the 10th percentile of the other slices: a short gap or thud each time round that longer measures average away |
| Gap or dip | Played twice, the loop's momentary loudness falls more than 10 dB under its overall loudness for 0.3 s or more |
| Whine | In 200 Hz bands every 500 Hz from 4 to 20 kHz, one band rises 12 dB or more above a straight line fitted through them all (level against log-frequency), less the median rise. The line takes out a sound's natural fall towards the top, which made every sound's lowest band look like a whine against a plain median |

Checked offline on 10 Nov 2026 with made-up sounds: a 9 kHz whine over a hum was flagged and showed as a bright line, the lowered
copy moved it to about 4.5 kHz, and the seam picture showed the loop's join. A synthetic white-noise click earned the
high-frequency and clipping flags. These are signal checks on test tones, not a judgement of any generated sound.

## Loops

The crossfade follows what worked in Phobos Ostranauts (washer and pump loops, 6 Oct 2026): a 250 ms circular crossfade, and in
the game, volume fades only when a machine starts or stops, never at each repeat. Equal-power curves are used because the two
ends of a generated sound are unrelated.

**A model fades a sound in late and out early, even when the prompt says "continuous".** The pilot's first loop (generator,
seed 11) had sound from about 0.5 s to 6.4 s of the 8 asked, with a low rumble carrying on through the fade, so a silence test
saw nothing wrong while the chugging stopped for half a second each time round. So a loop keeps only its **steady span**: from
the first to the last EBU R128 momentary reading (400 ms) within 8 dB of its overall loudness. The "gap or dip" flag checks
the finished loop played twice, seam included. For that sound, the span was 0.5 to 6.3 s, and the loop had no dip.

**A hum's two ends are in step, so the join has to be found, not just made** (10 Nov 2026). The owner heard a seam in the
distilled concentrator hum (seed 12) that every flag missed: joined at an arbitrary point, the two ends of a steady hum can sit
out of phase and cancel for a moment, a dip of 5.2 dB lasting a few hundredths of a second. Each loop's end is now chosen within
the last 0.5 s of its steady span, where its final 0.25 s best matches its first (normalised correlation, on a 16 kHz mono
decode by ffmpeg). Ends that match at 0.5 or better are crossfaded at equal gain, others at equal power. The pilot's hums now
match at 0.96 to 0.985 and that seam measures 0.1 dB; the new seam-dip flag (20 ms slices) catches the old join and a real
5.3 dB dip in the full-steps generator loop, seed 12.

## Running beside the game

`python scripts/art-run.py generate <requests> --cpu --stop-at HH:MM` (owner's request, 10 Nov 2026). RimWorld fills the
graphics card's memory (11.6 of 12.3 GB in use while playing, measured 10 Nov 2026), and pushing it further would make the
game stutter, so a run beside the game leaves the card alone and uses the processor:

- **Its own ComfyUI on port 8189**, started with `--cpu` and no graphics card visible (`CUDA_VISIBLE_DEVICES` empty), so it never
  meets a graphics-card run on 8188.
- **Below-normal priority**, so Windows serves the game first.
- **Held to the processors away from the game.** Where the L3 caches differ, as on the owner's Ryzen 9 7950X3D (96 MB of 3D
  V-cache on logical processors 0 to 15, where AMD's driver puts games; 32 MB on 16 to 31), it takes the smaller cache's group;
  otherwise the upper half. `--cpu-cores 16-31` chooses by hand. **One thread per core** there (8 on the owner's PC).
- **No game check, but a memory floor:** it ends before free memory falls under 6 GB (`--min-free-ram-gb`), since the game
  used 12.5 GB and grows with a colony. The stop time and disk floor still apply.
- In a night plan, a generate step takes `"cpu": true`.

Measured on 10 Nov 2026 with RimWorld running: the server ran at BelowNormal priority on processors 16 to 31 and did not appear on
the graphics card. **One 8-second sound at full steps (Small SFX base, 50 steps) took 60 s, model loading included**; the model
used about 0.5 GB, and its text encoder 1.75 GB. **The owner noticed no stutter in the game** (10 Nov 2026). Medium would be
far slower on the processor and is not meant for this.

## The pilot (10 Nov 2026)

All 16 sounds on the processor beside the game, in 8 minutes: 41 to 55 s each at full steps, **4 to 9 s each distilled**.
Agent's reading of the measurements and spectrograms, before the owner has listened:

| Sound | Full steps (seeds 11, 12) | Distilled (seeds 11, 12) |
| --- | --- | --- |
| Generator loop | 11 loud, clipped, harsh clicks; **12 clean and steady** | **Both clean and steady**, no flags |
| Concentrator hum | **Both have a steady whine at 15 kHz** (whine flag, 17 to 21 dB) | **Both quiet and clean**; no puff of air was heard in the pictures |
| Sonar ping | 11 is two loud noise bursts (clipped); 12 a clean held tone, more beep than ping | Both a soft held two-note tone; **none is a ping with an echo** |
| Switch click | Both a rattle with a loud burst in the middle (clipped) | **Three clean clicks each**; one could be cut out |

Lessons, from 16 sounds and so tentative:

- **The distilled model did better here**, not just faster: none of its 8 sounds clipped, against 4 of 8 at full steps, which also
  came out loud (up to −5.3 LUFS) and harsh. Its sounds came out quiet (−22 to −38 LUFS), which the export will level.
- **Sounds end at about 75 to 85% of the length asked** (6.3 to 6.9 s of 8, 2.9 of 4, 1.9 of 2). Ask for more than needed.
- **Prompt adherence is loose:** "one click" gave three, "ping with a long echo" gave held tones, "puff of air every few seconds"
  gave a steady hum. Rewording, more seeds, or Medium are the next things to try for the ping.
- **The whine flag earned its place on day one:** the 15 kHz line was invisible to every broad measure.

**The owner's picks (10 Nov 2026):** concentrator hum distilled 12 (chosen over the first pick, full-steps seed 12, which
stays the runner-up with its filter), generator loop distilled 11, sonar ping distilled
11, switch click distilled 11, recorded in `assets/phobos-sounds/picks.json`. The concentrator hum whines near 15 kHz
and, in other stretches, near 19.5 kHz (which the whine search then missed, so it now reaches 20 kHz). A narrow notch at 15 kHz
left a shoulder and the 19.5 kHz band; **a steep low-pass at 11 kHz removed both** and left its loudness unchanged, so the
export applies it to that pick. A muffled hum has nothing worth keeping up there.

**First sound, first lessons (generator loop, seed 11):** stereo at 44,100 Hz; loud as made (−10.7 LUFS, true peak +0.9 dBTP,
so it earned the clipped flag), which the review copy levels and a future export must level and limit; strong highs (the band above
4 kHz only 11 dB under the whole), with broadband clicks in the chugging that reach the top of the picture.

## History: what did not work

In Phobos Ostranauts (6 Oct 2026), 38 sounds synthesised by Python were rejected and deleted at the owner's request; the owner
then picked ElevenLabs Sound Effects v2 loops (paid). Stable Audio 3 is the local equivalent of the route that worked.
Synthesis from code is not offered as the main route again. A physical engine model such as Ange Yaghi's
[Engine Simulator](https://github.com/ange-yaghi/engine-sim) (MIT) is a different thing and stays an idea for engines only.

## Into the game (built 10 Nov 2026; not yet tested in game)

- RimWorld loads `.wav`, `.mp3` and `.ogg` from a mod (`Verse.ModContentLoader.AcceptableExtensionsAudio`, read with
  `scripts/query-code.py`). A picked sound would ship as `.ogg` under the mod's `Sounds/` folder with a `SoundDef`.
- **RimWorld's own mix is quiet** (owner, 10 Nov 2026), and the owner judges candidates at the volume they play at, where the
  review copies at −20 LUFS sound fine. The export levels against the game's sounds, not against the review level.
- **The game's own sounds are measurable.** RimWorld 1.7 keeps them as plain Ogg Vorbis in `Assets.zip` (`Resources/Sounds/`)
  and each DLC's `Assets.zip` (`Sounds/`), and its SoundDefs as XML. **`python scripts/art/game_sounds.py`** finds and measures them
  (`find`, `measure`, `sounddef`), copying a clip to a temporary file only for ffmpeg and deleting it at once, so no game file reaches
  the repository. A SoundDef plays at its `volumeRange / 100` (`Verse.Sound.SubSoundDef.RandomizedVolume`), so a sound's
  **effective loudness** is its clip's plus 20 log10 of that. Every game sound measured was **mono at 44,100 Hz**.
- **`python scripts/art/export_sound.py assets/phobos-sounds/exports.json`** writes each pick into its mod: clean-up filter, mono
  mix, the matched loop or the one-shot cut, levelled so that through our SoundDef's volume it sits at the chosen game sound's
  effective loudness plus an offset (by true peak for a sound under 0.4 s, which loudness meters ignore), Ogg Vorbis, the review
  flags run on the result (a clip, a whine, a seam dip or a gap refuses it), and a `.record.md` beside the file as for textures.
  `--check` verifies the files against `assets/phobos-sounds/exported.json`.
- **The volume setting is one, in D'thorga Framework** ("D'thorga sounds volume", 100%, 0 to 150%): every SoundDef of ours carries
  `Phobos.Framework.Sounds.PhobosSoundVolumeExtension`, and the setting scales its sub-sounds from their authored volume.

| Sound | Mod | Hook | Beside the game's | Offset | Volume |
| --- | --- | --- | --- | --- | --- |
| Concentrator hum (loop) | Medical 0.12.0 | the power comp's `soundAmbientPowered`: plays while powered, no code | Hemopump_Ambience, −37.1 LUFS | −3 dB | 50 |
| Generator truck running (loop) | Vehicles 0.31.0 | `runningSound` on the generator set: plays at the truck while set up and running | ChemfuelFiredGenerator_Ambience, −36.5 LUFS | 0 | 13 |
| Sonar ping (one-shot) | Vehicles 0.31.0 | `spottedSound` on submersibles: once at the hull when it is spotted | ScannerLongRangeMineral_Start, −35.9 LUFS | 0 | 20 |
| Switch click (one-shot, on camera) | Framework 0.71.0 | `Command_Toggle`'s own sounds, through `AsPhysicalSwitch()`: vehicle electrical loads, rig overdrive, survey long range; off plays at 0.9 pitch | Checkbox_TurnedOn, by peak | −2 dB | 14 |

The generator truck's cable end never ticks, so the game's own power-plant sound (`soundAmbientProducingPower`) would never start
there; a running generator should sound from the truck anyway. The ping's moment (a hull spotted) is an agent choice, since no
sonar is built yet; the owner may move it to a sonar or listening upgrade when one exists.

## Open questions

1. **Archive branches for sounds?** Images go to `art-masters` and `art-references`; whether sounds should too is the owner's call.
2. **Which sounds first** after the pilot, and for which mod.
3. **Whether the template settings suit Small SFX.** Its output is stereo at 44,100 Hz (first sound, 10 Nov 2026); the full
   pilot answers the rest.
4. **Medium:** worth its 9.2 GB only if Small SFX falls short.

## Sources

- Stability AI: [Meet Stable Audio 3](https://stability.ai/news-updates/meet-stable-audio-3-the-model-family-built-for-artistic-experimentation-with-open-weight-models);
  [Community License Agreement](https://stability.ai/community-license-agreement); the agreement as
  [Stable Audio Open 1.0's LICENSE.md](https://huggingface.co/stabilityai/stable-audio-open-1.0/blob/main/LICENSE.md).
- Comfy-Org: [Stable Audio 3 workflow guide](https://docs.comfy.org/tutorials/audio/stable-audio/stable-audio-3);
  [stable-audio-3 packaging](https://huggingface.co/Comfy-Org/stable-audio-3); the installed templates
  `audio_stable_audio_3_medium.json` and `audio_stable_audio_3_medium_base.json` (graph, settings and the SFX prompt guide).
- Sony AI: [Woosh](https://github.com/SonyResearch/Woosh); [ComfyUI-Woosh](https://github.com/Saganaki22/ComfyUI-Woosh)
  (community node, Saganaki22).
- declare-lab: [TangoFlux licence](https://huggingface.co/declare-lab/TangoFlux/blob/main/LICENSE.md). Ho Kei Cheng and
  colleagues: [MMAudio](https://arxiv.org/abs/2412.15322). Tencent:
  [HunyuanVideo-Foley notice](https://huggingface.co/tencent/HunyuanVideo-Foley/blob/main/NOTICE).
- FFmpeg developers: [filter documentation](https://ffmpeg.org/ffmpeg-filters.html) (`ebur128`, `acrossover`, `rubberband`,
  `acrossfade`, `showspectrumpic`); gyan.dev's [Windows builds](https://www.gyan.dev/ffmpeg/builds/).
- EBU: [R 128 loudness recommendation](https://tech.ebu.ch/publications/r128) (the LUFS and true peak measures).
