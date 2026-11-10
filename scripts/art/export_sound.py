"""Export the owner's picked sounds into the mods, levelled against the game's own sounds, with a record beside each.

Reads an exports file (assets/phobos-sounds/exports.json): for each sound, the picked master under the asset set's generated
folder, the mod file to write, whether it loops or plays once, any clean-up filter, and the game SoundDef it should sit beside.
For each it:

1. runs the clean-up filter and mixes to mono at 44,100 Hz, as every game sound measured is (scripts/art/game_sounds.py);
2. for a loop, keeps the steady span and joins it where its ends match (scripts/art/sound_review.py); for a one-shot, cuts the
   part asked for, trims silence at the ends and fades them;
3. levels it so that, played through our SoundDef's volume, it sits at the reference's effective loudness plus offset_db
   (by integrated loudness, or by true peak for a sound under 0.4 s, which loudness meters ignore), with the true peak kept
   under -1 dBTP;
4. encodes it as Ogg Vorbis into the mod, measures the result, runs the review flags on it, and writes <file>.record.md beside
   it and every figure into the exports file's record (exported.json).

Masters stay byte for byte. Uses no graphics card. --check verifies every exported file and master against its record.

    python scripts/art/export_sound.py assets/phobos-sounds/exports.json
    python scripts/art/export_sound.py assets/phobos-sounds/exports.json --only ConcentratorHum
    python scripts/art/export_sound.py assets/phobos-sounds/exports.json --check
"""
import argparse
import datetime as _dt
import hashlib
import json
import math
from pathlib import Path
import sys
import tempfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
import game_sounds  # noqa: E402
import sound_review  # noqa: E402

RATE = 44100
MONO = 'pan=mono|c0=0.5*c0+0.5*c1,'
PEAK_CEILING_DBTP = sound_review.PEAK_CEILING_DBTP
SHORT_S = 0.4  # Under this, integrated loudness reads as silence (EBU R128 gates in 400 ms blocks), so the true peak levels it.
OGG_QUALITY = '5'
EXPORT_FLAGS_TO_REFUSE = sound_review.SERIOUS_FLAGS  # One list, so the review page and the export agree on what stops a sound.


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def fingerprint(spec, master):
    """What an export was made from: its spec and its master. Ogg Vorbis gives every encode a new stream serial number, so a file
    made again differs in its bytes though not in its sound; a rerun leaves alone what has not changed (owner's repository stays quiet)."""
    return hashlib.sha256((json.dumps(spec, sort_keys=True) + sha256(master)).encode('utf-8')).hexdigest()


def unchanged(spec, entry, master):
    """Whether a sound's export still matches its spec and master, and its file its record."""
    target = ROOT / spec['target']
    return (entry is not None and entry.get('fingerprint') == fingerprint(spec, master) and target.is_file()
            and sha256(target) == entry['sha256'])


def gain_for(measure, target_lufs=None, target_peak=None):
    """The gain that brings a sound to its target loudness, or to its target true peak for a short one, never lifting the
    true peak past PEAK_CEILING_DBTP. Pure, so it can be tested."""
    peak = measure.get('true_peak')
    if target_peak is not None:
        gain = target_peak - peak
    else:
        loudness = measure.get('lufs')
        if loudness is None or not math.isfinite(loudness):
            raise ValueError('the sound measured as silent, so it cannot be levelled by loudness')
        gain = target_lufs - loudness
    if peak is not None and math.isfinite(peak):
        gain = min(gain, PEAK_CEILING_DBTP - peak)
    return round(gain, 2)


def file_level_for(reference_effective, offset_db, volume):
    """The level our file needs so that, through our SoundDef's volume (percent, as the game reads it), it plays at the reference's
    effective level plus offset_db."""
    return round(reference_effective + offset_db - 20 * math.log10(volume / 100.0), 2)


def oneshot_filter(cut=None, filter_chain=None):
    """Clean-up, mono mix, the cut asked for, silence trimmed at both ends, and short fades. No levelling yet."""
    parts = []
    if filter_chain:
        parts.append(filter_chain)
    parts.append(MONO.rstrip(','))
    if cut:
        parts += [f'atrim=start={cut[0]}:end={cut[1]}', 'asetpts=N/SR/TB']
    trim = f'silenceremove=start_periods=1:start_threshold={sound_review.TRIM_THRESHOLD_DB}dB'
    parts += [trim, 'areverse', trim, f'afade=t=in:d={sound_review.FADE_OUT_S}', 'areverse',
              f'afade=t=in:d={sound_review.FADE_IN_S}', f'aresample={RATE}']
    return ','.join(parts)


def render(exe, master, spec, out):
    """The cleaned, mono, unlevelled sound at RATE into out (a WAV). Returns what was done, for the record."""
    done = {}
    if spec['kind'] == 'loop':
        done.update(best_loop(exe, master, spec, out))
    else:
        sound_review.run_ffmpeg(exe, ['-i', str(master), '-af', oneshot_filter(spec.get('cut'), spec.get('filter')), '-ar', str(RATE),
                                     '-c:a', 'pcm_s24le', str(out)])
        if spec.get('cut'):
            done['cut_s'] = spec['cut']
    if spec.get('filter'):
        done['filter'] = spec['filter']
    return done


LOOP_CROSSFADES_S = (0.25, 0.5, 0.75, 1.0)  # Tried in turn; a rough, pulsing engine (the tipper, 11 Nov 2026) joined cleanly at one
                                          # length and dipped at the others, so the export measures each and keeps the best.


def best_loop(exe, master, spec, out):
    """The loop with the smallest dip at its join, of every crossfade length in LOOP_CROSSFADES_S with both curves: each joined
    where its ends best match for that length (sound_review.best_loop_end), rendered, played twice and measured. Writes the best
    to out and returns what it chose."""
    frames, integrated = sound_review.loudness_frames(exe, [master])
    span = sound_review.steady_span(frames, integrated)
    mono = sound_review.decode_mono(exe, master)
    pre = (spec['filter'] + ',' if spec.get('filter') else '') + MONO
    best = None
    with tempfile.TemporaryDirectory() as folder:
        for crossfade in LOOP_CROSSFADES_S:
            joined, match = span, None
            if span:
                end, match = sound_review.best_loop_end(mono, sound_review.ANALYSIS_RATE, *span, crossfade=crossfade)
                joined = (span[0], end)
            for coherent in (False, True):
                trial = Path(folder) / f'{crossfade}-{coherent}.wav'
                graph = sound_review.loop_graph(0, crossfade=crossfade, span=joined, coherent=coherent, pre=pre)
                sound_review.run_ffmpeg(exe, ['-i', str(master), '-filter_complex', graph, '-map', '[out]', '-ar', str(RATE), '-c:a', 'pcm_s24le', str(trial)])
                twice = sound_review.decode_mono(exe, trial, twice=True)
                dip = sound_review.seam_dip_db(twice, sound_review.ANALYSIS_RATE, len(twice) / 2 / sound_review.ANALYSIS_RATE) or 0.0
                if best is None or dip < best[0]:
                    best = (dip, trial.read_bytes(), {'loop_span_s': list(joined) if joined else None, 'loop_match': match,
                                                      'loop_crossfade_s': crossfade, 'loop_curve': 'equal gain' if coherent else 'equal power',
                                                      'loop_seam_dip_db': round(dip, 2)})
    out.write_bytes(best[1])
    return best[2]


def review(exe, path, loop):
    """The review flags on an exported file, as sound_review measures them."""
    measure = sound_review.measure_file(exe, path)
    measure['whine'] = sound_review.find_whine(exe, path)
    if loop:
        twice = sound_review.decode_mono(exe, path, twice=True)
        measure['seam_dip_db'] = sound_review.seam_dip_db(twice, sound_review.ANALYSIS_RATE, len(twice) / 2 / sound_review.ANALYSIS_RATE)
        frames, integrated = sound_review.loudness_frames(exe, [path, path])
        measure['loop_gap_s'] = sound_review.longest_dip(frames, integrated)
    measure['flags'] = [code for code, _ in sound_review.flags_for(measure)]
    return measure


def record_text(spec, entry):
    """The Markdown record kept beside the exported file, as for the mods' textures."""
    source = entry['source']
    lines = [f'# {Path(spec["target"]).name}', '',
             f'**{spec.get("use", spec["name"])}.** Made with generative AI: {source.get("generator", "a Stable Audio 3 model")}, run locally, '
             'prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model\'s outputs belong to their '
             'maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).', '',
             '| | |', '| --- | --- |',
             f'| Picked by | the owner, {spec.get("picked", "")} |',
             f'| Master | assets/phobos-sounds/generated/{spec["candidate"]} (sha256 {entry["master_sha256"]}) |',
             f'| Model | {source.get("model", "")} (sha256 {source.get("model_sha256", "")}), seed {source.get("seed", "")}, '
             f'{source.get("steps", "")} steps, guidance {source.get("cfg", "")} |',
             f'| Prompt | {source.get("prompt", "")} |',
             f'| Made into | {spec["kind"]}, mono, {RATE} Hz, Ogg Vorbis quality {OGG_QUALITY}; '
             + ', '.join(f'{key} {value}' for key, value in entry['processing'].items()) + ' |',
             f'| Levelled against | the game\'s {spec["reference"]} (effective {entry["reference"]["effective"]}) {spec.get("offset_db", 0):+g} dB, '
             f'through volume {spec["volume"]} |',
             f'| Result | {entry["result"]["lufs"]} LUFS, true peak {entry["result"]["true_peak"]} dBTP, {entry["result"]["seconds"]} s; '
             f'flags: {", ".join(entry["result"]["flags"]) or "none"} |',
             f'| File | sha256 {entry["sha256"]}, made {entry["date"]} with {entry["ffmpeg"]} |', '',
             'Not yet tested in game.']
    return '\n'.join(lines) + '\n'


def export_one(exe, spec, generated, game):
    master = generated / spec['candidate']
    target = ROOT / spec['target']
    source_record = master.with_suffix('.json')
    source = json.loads(source_record.read_text(encoding='utf-8')) if source_record.is_file() else {}
    reference = game_sounds.reference_level(exe, spec['reference'], game)
    short = False
    with tempfile.TemporaryDirectory() as folder:
        work = Path(folder) / 'work.wav'
        processing = render(exe, master, spec, work)
        measure = sound_review.measure_file(exe, work)
        short = (measure.get('seconds') or 0) < SHORT_S
        if short or reference['by_peak']:
            # Either ours or the game's clip is too short for a loudness reading: the reference's true peak through its own
            # volume, our file's peak through ours.
            target_peak = file_level_for(reference['effective_peak'], spec.get('offset_db', 0), spec['volume'])
            gain = gain_for(measure, target_peak=target_peak)
            reference['effective'] = f'peak {reference["effective_peak"]} dB'
            processing['levelled_by'] = f'true peak, to {target_peak} dBTP'
        else:
            target_lufs = file_level_for(reference['effective_lufs'], spec.get('offset_db', 0), spec['volume'])
            gain = gain_for(measure, target_lufs=target_lufs)
            reference['effective'] = f'{reference["effective_lufs"]} LUFS'
            processing['levelled_by'] = f'loudness, to {target_lufs} LUFS'
        processing['gain_db'] = gain
        target.parent.mkdir(parents=True, exist_ok=True)
        sound_review.run_ffmpeg(exe, ['-i', str(work), '-af', f'volume={gain}dB', '-c:a', 'libvorbis', '-q:a', OGG_QUALITY, str(target)])
    result = review(exe, target, spec['kind'] == 'loop')
    entry = {'name': spec['name'], 'target': spec['target'], 'sha256': sha256(target), 'master_sha256': sha256(master),
             'fingerprint': fingerprint(spec, master),
             'date': _dt.date.today().isoformat(), 'ffmpeg': sound_review.ffmpeg_version(exe), 'processing': processing,
             'reference': {key: reference[key] for key in ('defName', 'effective', 'volume', 'distance', 'channels')},
             'result': {key: result.get(key) for key in ('lufs', 'true_peak', 'seconds', 'flags', 'seam_dip_db', 'loop_gap_s')},
             'source': {key: source.get(key) for key in ('generator', 'model', 'model_sha256', 'seed', 'steps', 'cfg', 'prompt')}}
    target.with_name(target.name + '.record.md').write_text(record_text(spec, entry), encoding='utf-8')
    return entry


def check(specs, records):
    """Problems with the exported files and masters against their records, without remaking anything."""
    problems = []
    for spec in specs:
        entry = records.get(spec['name'])
        target = ROOT / spec['target']
        if entry is None:
            problems.append(f'{spec["name"]}: never exported')
        elif not target.is_file():
            problems.append(f'{spec["name"]}: {spec["target"]} is missing')
        elif sha256(target) != entry['sha256']:
            problems.append(f'{spec["name"]}: {spec["target"]} differs from its record')
        elif not target.with_name(target.name + '.record.md').is_file():
            problems.append(f'{spec["name"]}: its record beside the file is missing')
    return problems


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('exports', help='the exports file, such as assets/phobos-sounds/exports.json')
    parser.add_argument('--only', help='export only the sound with this name')
    parser.add_argument('--check', action='store_true', help='verify the exported files against their records')
    parser.add_argument('--force', action='store_true', help='remake every file, and write one even when its flags would refuse it')
    args = parser.parse_args(argv)
    exports_path = Path(args.exports).resolve()
    specs = json.loads(exports_path.read_text(encoding='utf-8'))['exports']
    record_path = exports_path.with_name('exported.json')
    records = json.loads(record_path.read_text(encoding='utf-8')) if record_path.is_file() else {}
    if args.check:
        problems = check(specs, records)
        print('\n'.join(problems) or f'{len(specs)} exported sounds match their records')
        return 1 if problems else 0
    exe = sound_review.find_ffmpeg()
    if exe is None:
        print('ffmpeg was not found: set FFMPEG, or install it (winget install Gyan.FFmpeg)', file=sys.stderr)
        return 2
    game = game_sounds._deploy.game_path()
    generated = exports_path.parent / 'generated'
    refused = []
    for spec in specs:
        if args.only and spec['name'] != args.only:
            continue
        if not args.force and unchanged(spec, records.get(spec['name']), generated / spec['candidate']):
            print(f'{spec["name"]}: unchanged, left as it is')
            continue
        entry = export_one(exe, spec, generated, game)
        bad = [flag for flag in entry['result']['flags'] if flag in EXPORT_FLAGS_TO_REFUSE]
        if bad and not args.force:
            (ROOT / spec['target']).unlink()
            (ROOT / spec['target']).with_name(Path(spec['target']).name + '.record.md').unlink()
            refused.append(f'{spec["name"]}: refused, flagged {", ".join(bad)}')
            continue
        records[spec['name']] = entry
        print(f'{spec["name"]}: {spec["target"]} at {entry["result"]["lufs"]} LUFS, peak {entry["result"]["true_peak"]} dBTP, '
              f'{entry["result"]["seconds"]} s, flags {entry["result"]["flags"] or "none"}')
    record_path.write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')
    for line in refused:
        print(line)
    return 1 if refused else 0


if __name__ == '__main__':
    sys.exit(main())
