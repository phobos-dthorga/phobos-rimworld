"""Prepare generated sounds for listening, measure what an ear may miss, and write a dark listening page.

For each candidate FLAC under a generated folder (left byte for byte), it writes into a review folder beside it:

    <stem>.review.ogg        the ends trimmed of silence, faded, and levelled to REVIEW_LUFS, so candidates compare fairly
    <stem>.lowered.ogg       the review copy with everything above SPLIT_HZ moved down an octave (--octaves for more): a listening aid only,
                             for ears that lose the high frequencies (the owner's request, 10 Nov 2026); never shipped
    <stem>.loop.ogg          for a request with "loop": true, a seamless loop made with a circular crossfade
    <stem>.spectrogram.png   a picture of the master: time left to right, pitch upwards, brightness for loudness
    <stem>.seam.png          for a loop, the loop played twice, so a seam shows as a line in the middle
    <stem>.measure.json      the master's loudness, true peak, high-frequency level, the loop seam and the flags

and sound-review.html and sound-review.json in the generated folder. The flags name what a listener with high-frequency
hearing loss could miss (hiss, whine, clicks, clipping, a sharp attack, a bump at a loop's seam), so a pick never rests on
the ear alone. Every threshold is a named constant below and an authored choice, not a standard, unless it says otherwise.

Needs ffmpeg with rubberband (the gyan.dev full build has it): the FFMPEG environment variable, then the PATH, then winget's
install folder. Uses no graphics card, so it is safe while a game runs.

    python scripts/art/sound_review.py assets/phobos-sounds/generated
    python scripts/art/sound_review.py assets/phobos-sounds/generated --force      remake every review file
"""
import argparse
import glob
import html
import json
import math
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from review_page import STYLE as BASE_STYLE  # noqa: E402  One look for every review page.

MASTER_SUFFIX = '.flac'
REVIEW_FOLDER = 'review'
REVIEW_LUFS = -20.0         # Every review copy is levelled here (EBU R128 integrated loudness), so loudness never decides a pick.
PEAK_CEILING_DBTP = -1.0    # The levelling gain never lifts the true peak above this (EBU R128's own ceiling for delivery).
SPLIT_HZ = 3000             # Lowered copy: the band above this moves down; below it stays as made.
LOWERED_OCTAVES = 1         # How far the lowered copy moves the highs down; --octaves 2 for a quarter of their pitch.
HIGH_HZ = 4000              # "Highs" for the measurement: noise damage to hearing usually starts about here.
STRONG_HIGHS_DB = 12.0      # Flag when the band above HIGH_HZ is within this many dB of the whole sound's level.
SPIKY_DB = 20.0             # Flag when the true peak stands this far above the loudness: an attack sharp enough to startle.
SILENT_DB = -60.0           # A mean level under this counts as nearly silent.
CLIPPED_DB = -0.1           # A sample peak at or above this counts as clipped.
CLIPPED_TRUE_PEAK_DBTP = 0.0  # So does a true peak above full scale: the peak between samples clips when it is played.
TRIM_THRESHOLD_DB = -60     # Silence at either end, quieter than this, is trimmed from the review copy.
FADE_IN_S, FADE_OUT_S = 0.005, 0.02
LOOP_CROSSFADE_S = 0.25     # Ostranauts' washer and pump loops used 250 ms and were accepted (6 Oct 2026).
SEAM_WINDOW_S = 0.05
SEAM_JUMP_DB = 3.0          # Flag a loop whose level differs by more than this either side of its seam.
SPECTROGRAM_SIZE = '960x320'
OGG_QUALITY = '6'           # Vorbis quality for the review copies; the masters stay lossless.

FLAG_WORDS = {
    'clipped': 'Clipped: its loudest moments hit the ceiling, which crackles.',
    'silent': 'Nearly silent: the model may have made nothing.',
    'highs': 'Strong high frequencies (above {high} kHz, {gap} dB below the whole): hiss, whine or sizzle you may not hear. '
             'Listen to the lowered copy and look at the top of the picture.',
    'spiky': 'Sharp attack: its peaks stand {spike} dB above its loudness, which can startle.',
    'seam': 'Loop seam: the level changes by {jump} dB where it repeats.',
}

STYLE = BASE_STYLE + """
.grid { grid-template-columns: repeat(auto-fill, minmax(460px, 1fr)); }
.card img { background:#000; }
.play { margin:6px 0; } .play label { display:block; font-size:0.9rem; color:var(--muted); }
.play audio { width:100%; }
.ok { color:#9fd18b; font-size:0.95rem; }
.numbers { font-size:0.9rem; color:var(--muted); margin-top:6px; }
.help { background:var(--panel); border:1px solid var(--line); padding:10px 14px; margin:14px 0; max-width:70em; }
"""


def find_ffmpeg():
    """ffmpeg from the FFMPEG variable, the PATH, or winget's folders (a shell started before the install has the old PATH)."""
    candidates = [os.environ.get('FFMPEG'), shutil.which('ffmpeg')]
    local = os.environ.get('LOCALAPPDATA')
    if local:
        candidates.append(os.path.join(local, 'Microsoft', 'WinGet', 'Links', 'ffmpeg.exe'))
        candidates += sorted(glob.glob(os.path.join(local, 'Microsoft', 'WinGet', 'Packages', 'Gyan.FFmpeg*', '*', 'bin', 'ffmpeg.exe')),
                             reverse=True)
    return next((path for path in candidates if path and os.path.isfile(path)), None)


def run_ffmpeg(exe, args):
    """Run ffmpeg and return what it wrote to its error stream, which is where its measurements go."""
    done = subprocess.run([exe, '-hide_banner', '-nostdin', '-y', *args], capture_output=True, text=True, errors='replace')
    if done.returncode:
        raise RuntimeError(f'ffmpeg failed: {done.stderr.strip()[-600:]}')
    return done.stderr


def ffmpeg_version(exe):
    done = subprocess.run([exe, '-hide_banner', '-version'], capture_output=True, text=True, errors='replace')
    return done.stdout.splitlines()[0] if done.stdout else 'unknown'


def _number(text):
    return float(text.strip())  # ffmpeg writes silence as -inf, which float() reads.


def parse_duration(text):
    """Seconds from ffmpeg's "Duration: HH:MM:SS.ss" line, or None."""
    match = re.search(r'Duration: (\d+):(\d+):(\d+(?:\.\d+)?)', text)
    return None if not match else int(match[1]) * 3600 + int(match[2]) * 60 + float(match[3])


def parse_ebur128(text):
    """Integrated loudness (LUFS) and true peak (dBTP) from the summary ebur128 prints at the end."""
    summary = text[text.rfind('Summary:'):] if 'Summary:' in text else text
    loudness = re.search(r'I:\s+(-?inf|-?[\d.]+) LUFS', summary)
    peak = re.search(r'True peak:\s*\n\s*Peak:\s+(-?inf|-?[\d.]+) dBFS', summary)
    return (_number(loudness[1]) if loudness else None, _number(peak[1]) if peak else None)


def parse_volumedetect(text):
    """Mean (RMS) and maximum sample level in dB from volumedetect, the last report in the text."""
    means = re.findall(r'mean_volume: (-?inf|-?[\d.]+) dB', text)
    maxes = re.findall(r'max_volume: (-?inf|-?[\d.]+) dB', text)
    return (_number(means[-1]) if means else None, _number(maxes[-1]) if maxes else None)


def flags_for(measure):
    """The flags a measurement earns, as (code, words). Each is a reason to look again, not a verdict."""
    found = []
    mean, peak, true_peak = measure.get('mean_db'), measure.get('max_db'), measure.get('true_peak')
    if (peak is not None and peak >= CLIPPED_DB) or (true_peak is not None and true_peak > CLIPPED_TRUE_PEAK_DBTP):
        found.append(('clipped', FLAG_WORDS['clipped']))
    if mean is not None and mean < SILENT_DB:
        found.append(('silent', FLAG_WORDS['silent']))
        return found
    high = measure.get('high_mean_db')
    if mean is not None and high is not None and math.isfinite(high) and high - mean > -STRONG_HIGHS_DB:
        found.append(('highs', FLAG_WORDS['highs'].format(high=HIGH_HZ // 1000, gap=f'{mean - high:.0f}')))
    loudness = measure.get('lufs')
    if loudness is not None and true_peak is not None and math.isfinite(loudness) and true_peak - loudness > SPIKY_DB:
        found.append(('spiky', FLAG_WORDS['spiky'].format(spike=f'{true_peak - loudness:.0f}')))
    jump = measure.get('seam_jump_db')
    if jump is not None and jump > SEAM_JUMP_DB:
        found.append(('seam', FLAG_WORDS['seam'].format(jump=f'{jump:.1f}')))
    return found


def gain_db(measure):
    """The gain that brings a sound to REVIEW_LUFS without lifting its true peak past PEAK_CEILING_DBTP. None if unmeasurable."""
    loudness, true_peak = measure.get('lufs'), measure.get('true_peak')
    if loudness is None or not math.isfinite(loudness):
        return None
    gain = REVIEW_LUFS - loudness
    if true_peak is not None and math.isfinite(true_peak):
        gain = min(gain, PEAK_CEILING_DBTP - true_peak)
    return round(gain, 2)


def review_filter(gain, trim=True):
    """Trim silence at both ends (reversing to reach the end), fade the edges, then level. Silence inside is kept."""
    parts = []
    if trim:
        cut = f'silenceremove=start_periods=1:start_threshold={TRIM_THRESHOLD_DB}dB'
        parts += [cut, 'areverse', cut, 'areverse']
    parts += [f'afade=t=in:d={FADE_IN_S}', 'areverse', f'afade=t=in:d={FADE_OUT_S}', 'areverse', f'volume={gain or 0}dB']
    return ','.join(parts)


def lowered_graph(octaves=LOWERED_OCTAVES):
    """Split at SPLIT_HZ, move the top band down by whole octaves with rubberband (keeping its length), and mix it back over
    the untouched lows: the frequency lowering hearing aids do, as a listening aid."""
    return (f'[0:a]acrossover=split={SPLIT_HZ}[lo][hi];[hi]rubberband=pitch={0.5 ** octaves:g}:pitchq=quality[down];'
            '[lo][down]amix=inputs=2:normalize=0[out]')


def loop_graph(gain, crossfade=LOOP_CROSSFADE_S):
    """A circular crossfade: the sound from crossfade onwards, with its last crossfade seconds faded into its first. The end
    then runs straight into the start, so it repeats without a gap. Equal-power curves, because the two ends are unrelated."""
    return (f'[0:a]volume={gain or 0}dB,asplit=2[a][b];[a]atrim=start={crossfade},asetpts=N/SR/TB[body];'
            f'[b]atrim=end={crossfade},asetpts=N/SR/TB[head];[body][head]acrossfade=d={crossfade}:c1=qsin:c2=qsin[out]')


def spectrogram_filter():
    return f'showspectrumpic=s={SPECTROGRAM_SIZE}:legend=1:scale=log'


def measure_file(exe, path):
    """Loudness, true peak, mean and maximum level of the whole file, and the mean level of its band above HIGH_HZ."""
    whole = run_ffmpeg(exe, ['-i', str(path), '-af', 'ebur128=peak=true,volumedetect', '-f', 'null', '-'])
    high = run_ffmpeg(exe, ['-i', str(path), '-af', f'highpass=f={HIGH_HZ},highpass=f={HIGH_HZ},volumedetect', '-f', 'null', '-'])
    lufs, true_peak = parse_ebur128(whole)
    mean_db, max_db = parse_volumedetect(whole)
    return {'seconds': parse_duration(whole), 'lufs': lufs, 'true_peak': true_peak, 'mean_db': mean_db, 'max_db': max_db,
            'high_mean_db': parse_volumedetect(high)[0]}


def seam_jump(exe, loop_path):
    """How far the level differs between the last and first SEAM_WINDOW_S of a loop, in dB."""
    head = parse_volumedetect(run_ffmpeg(exe, ['-i', str(loop_path), '-af', f'atrim=end={SEAM_WINDOW_S},volumedetect', '-f', 'null', '-']))[0]
    tail = parse_volumedetect(run_ffmpeg(exe, ['-sseof', f'-{SEAM_WINDOW_S}', '-i', str(loop_path), '-af', 'volumedetect', '-f', 'null', '-']))[0]
    if head is None or tail is None or not (math.isfinite(head) and math.isfinite(tail)):
        return None
    return round(abs(head - tail), 2)


def measure_octaves(master):
    """The octaves a candidate's existing lowered copy used, so a new --octaves remakes it; None if it has none yet."""
    path = outputs_for(master)['measure']
    return json.loads(path.read_text(encoding='utf-8')).get('lowered_octaves', LOWERED_OCTAVES) if path.is_file() else None


def outputs_for(master):
    review = master.parent / REVIEW_FOLDER
    names = {'review': '.review.ogg', 'lowered': '.lowered.ogg', 'loop': '.loop.ogg', 'spectrogram': '.spectrogram.png',
             'seam': '.seam.png', 'measure': '.measure.json'}
    return {key: review / f'{master.stem}{suffix}' for key, suffix in names.items()}


def is_current(master, paths, loop):
    """Whether this candidate's review files exist and are newer than its master, so a rerun can reuse them."""
    needed = ['review', 'lowered', 'spectrogram', 'measure'] + (['loop', 'seam'] if loop else [])
    return all(paths[key].is_file() and paths[key].stat().st_mtime >= master.stat().st_mtime for key in needed)


def prepare(exe, master, record, force=False, octaves=LOWERED_OCTAVES):
    """Make one candidate's review files and return its measurement, flags included."""
    loop = bool(record.get('loop'))
    paths = outputs_for(master)
    if not force and is_current(master, paths, loop):
        return json.loads(paths['measure'].read_text(encoding='utf-8'))
    paths['review'].parent.mkdir(exist_ok=True)
    measure = measure_file(exe, master)
    gain = gain_db(measure)
    encode = ['-c:a', 'libvorbis', '-q:a', OGG_QUALITY]
    run_ffmpeg(exe, ['-i', str(master), '-af', review_filter(gain, trim=not loop), *encode, str(paths['review'])])
    run_ffmpeg(exe, ['-i', str(paths['review']), '-filter_complex', lowered_graph(octaves), '-map', '[out]', *encode, str(paths['lowered'])])
    run_ffmpeg(exe, ['-i', str(master), '-lavfi', spectrogram_filter(), '-update', '1', str(paths['spectrogram'])])
    if loop:
        run_ffmpeg(exe, ['-i', str(master), '-filter_complex', loop_graph(gain), '-map', '[out]', *encode, str(paths['loop'])])
        run_ffmpeg(exe, ['-i', str(paths['loop']), '-i', str(paths['loop']), '-filter_complex',
                         f'[0:a][1:a]concat=n=2:v=0:a=1,{spectrogram_filter()}', '-update', '1', str(paths['seam'])])
        measure['seam_jump_db'] = seam_jump(exe, paths['loop'])
    measure['review_gain_db'] = gain
    measure['lowered_octaves'] = octaves
    measure['flags'] = [{'code': code, 'words': words} for code, words in flags_for(measure)]
    paths['measure'].write_text(json.dumps(measure, indent=2), encoding='utf-8')
    return measure


def _fmt(value, unit, digits=1):
    return 'n/a' if value is None else ('silent' if not math.isfinite(value) else f'{value:.{digits}f} {unit}')


def _player(label, src, loop=False):
    return (f'<div class="play"><label>{html.escape(label)}</label>'
            f'<audio controls preload="none"{" loop" if loop else ""} src="{html.escape(src)}"></audio></div>')


def card(generated, master, record, measure, error=None):
    rel = lambda path: os.path.relpath(path, generated).replace(os.sep, '/')  # noqa: E731
    paths = outputs_for(master)
    meta = ', '.join(f'{key}: {record[key]}' for key in ('engine', 'seed', 'steps', 'cfg', 'seconds') if key in record)
    parts = [f'<div class="card"><div class="name">{html.escape(master.stem)}</div><div class="meta">{html.escape(meta)}</div>']
    if error:
        parts.append(f'<div class="warn">Could not prepare it: {html.escape(error)}</div>')
    else:
        flags = measure.get('flags', [])
        parts += [f'<div class="warn">{html.escape(flag["words"])}</div>' for flag in flags] or ['<div class="ok">No flags.</div>']
        parts.append(_player('As made: the untouched master', rel(master)))
        parts.append(_player(f'Review level: ends trimmed, levelled to {REVIEW_LUFS:g} LUFS', rel(paths['review'])))
        octaves = measure.get('lowered_octaves', LOWERED_OCTAVES)
        parts.append(_player(f'Lowered (listening aid only): highs above {SPLIT_HZ} Hz moved down '
                             f'{"an octave" if octaves == 1 else f"{octaves} octaves"}', rel(paths['lowered'])))
        if record.get('loop') and paths['loop'].is_file():
            parts.append(_player('Loop: repeats until stopped; listen for a bump each time round', rel(paths['loop']), loop=True))
        parts.append(f'<img loading="lazy" src="{html.escape(rel(paths["spectrogram"]))}" alt="spectrogram of {html.escape(master.stem)}">')
        if record.get('loop') and paths['seam'].is_file():
            parts.append('<div class="meta">The loop played twice: a line down the middle is its seam.</div>'
                         f'<img loading="lazy" src="{html.escape(rel(paths["seam"]))}" alt="the loop played twice">')
        high_gap = (None if measure.get('high_mean_db') is None or measure.get('mean_db') is None
                    else measure['mean_db'] - measure['high_mean_db'])
        parts.append('<div class="numbers">'
                     f'Length {_fmt(measure.get("seconds"), "s", 2)} · loudness {_fmt(measure.get("lufs"), "LUFS")} · '
                     f'true peak {_fmt(measure.get("true_peak"), "dBTP")} · highs above {HIGH_HZ // 1000} kHz '
                     f'{_fmt(high_gap, "dB below the whole", 0)}'
                     + (f' · seam {_fmt(measure.get("seam_jump_db"), "dB")}' if 'seam_jump_db' in measure else '') + '</div>')
    parts.append(f'<details><summary>prompt</summary><div class="rec">{html.escape(record.get("prompt", ""))}</div></details></div>')
    return ''.join(parts)


HELP = f"""<div class="help"><b>How to read this page.</b> Each candidate has up to four players. <b>As made</b> is the untouched
file. <b>Review level</b> sets every candidate to the same loudness, so the louder one never wins by being louder.
<b>Lowered</b> moves everything above {SPLIT_HZ} Hz down (an octave unless the page says more), so hiss, whine and clicks land where they are easier to hear;
it only shows that something is up there, not how it sounds to others, and it never ships. <b>Loop</b> repeats.<br>
<b>The picture</b> (a spectrogram): time runs left to right, pitch rises upwards, brightness is loudness. A bright band near
the top is hiss or whine; a thin upright line is a click. <b>Red lines</b> are measured flags: reasons to look again, not
verdicts. Nothing here is in a mod until it is picked.</div>"""


def build(generated, force=False, octaves=LOWERED_OCTAVES):
    """Prepare every candidate under the generated folder and write the page. Returns the number that failed."""
    generated = Path(generated).resolve()
    exe = find_ffmpeg()
    if exe is None:
        raise RuntimeError('ffmpeg was not found: set FFMPEG, or install it (winget install Gyan.FFmpeg)')
    sections, summary, failures = [], {'ffmpeg': ffmpeg_version(exe), 'candidates': {}}, 0
    folders = sorted(path for path in generated.iterdir() if path.is_dir() and any(path.glob('*' + MASTER_SUFFIX)))
    for folder in folders:
        cards = []
        for master in sorted(folder.glob('*' + MASTER_SUFFIX)):
            record_path = master.with_suffix('.json')
            record = json.loads(record_path.read_text(encoding='utf-8')) if record_path.is_file() else {}
            try:
                measure = prepare(exe, master, record, force or measure_octaves(master) not in (None, octaves), octaves)
                cards.append(card(generated, master, record, measure))
                summary['candidates'][f'{folder.name}/{master.name}'] = measure
            except (RuntimeError, OSError, ValueError) as error:
                failures += 1
                cards.append(card(generated, master, record, {}, str(error)[:400]))
                summary['candidates'][f'{folder.name}/{master.name}'] = {'error': str(error)[:400]}
        sections.append(f'<h2 id="{html.escape(folder.name)}">{html.escape(folder.name)} <span class="meta">({len(cards)})</span></h2>'
                        f'<div class="grid">{"".join(cards)}</div>')
    nav = ' '.join(f'<a href="#{html.escape(folder.name)}">{html.escape(folder.name)}</a>' for folder in folders)
    title = generated.parent.name
    page = (f'<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">'
            f'<title>{html.escape(title)} sound review</title><style>{STYLE}</style></head><body>'
            f'<h1>{html.escape(title)}: generated sounds</h1><div class="meta">Local, from the records beside each sound. '
            f'Measured with {html.escape(summary["ffmpeg"])}.</div>{HELP}<nav>{nav}</nav>{"".join(sections)}</body></html>')
    (generated / 'sound-review.html').write_text(page, encoding='utf-8')
    (generated / 'sound-review.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
    print(f'wrote {generated / "sound-review.html"} ({sum(len(list(f.glob("*" + MASTER_SUFFIX))) for f in folders)} sounds, {failures} failed)')
    return failures


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('generated', help='the generated folder of one asset set')
    parser.add_argument('--force', action='store_true', help='remake every review file, even ones newer than their master')
    parser.add_argument('--octaves', type=int, choices=(1, 2, 3), default=LOWERED_OCTAVES, help='how far the lowered copy moves the highs down')
    args = parser.parse_args(argv)
    try:
        failures = build(args.generated, args.force, args.octaves)
    except RuntimeError as error:
        print(error, file=sys.stderr)
        return 2
    return 1 if failures else 0


if __name__ == '__main__':
    sys.exit(main())
