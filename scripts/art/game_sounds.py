"""Find, read and measure RimWorld's own sounds, so ours can sit in the game's mix.

RimWorld 1.7 keeps its sound files as .ogg (some .mp3 and .wav) inside Assets.zip under Resources/Sounds/, and each DLC's in
Data/<DLC>/Assets.zip under Sounds/. Its SoundDefs are plain XML under Data/*/Defs. This reads both where they lie: a clip is copied to a temporary
folder only for ffmpeg to measure it and is deleted at once, so no game file ever reaches the repository (AGENTS.md). Measurements
(loudness, peak, channels) are facts about the game's files and may be recorded.

A SoundDef's playback gain is its volumeRange / 100 (Verse.Sound.SubSoundDef.RandomizedVolume), so a sound's effective loudness in
the game is its clip's integrated loudness plus 20 log10(volume / 100). Matching that is how our sounds sit at the game's level.

    python scripts/art/game_sounds.py find "*Generator*"                  clips whose path matches
    python scripts/art/game_sounds.py measure "*Electricity/Chemfuel*"   loudness, peak, channels, rate and length of each match
    python scripts/art/game_sounds.py sounddef ChemfuelFiredGenerator_Ambience Checkbox_TurnedOn
                                                                          a SoundDef's settings, its clips and its effective loudness
"""
import argparse
import fnmatch
import importlib.util
import json
import math
import os
from pathlib import Path
import re
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
import sound_review  # noqa: E402

_spec = importlib.util.spec_from_file_location('phobos_deploy', ROOT / 'scripts' / 'deploy.py')
_deploy = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_deploy)

SOUND_ROOTS = ('Resources/Sounds/', 'Sounds/')  # The base game's archive uses the first, the DLCs' the second (read 10 Nov 2026).
AUDIO_EXTENSIONS = ('.ogg', '.mp3', '.wav')
SHORT_CLIP_S = 0.4  # Shorter than EBU R128's 400 ms block, a clip has no integrated loudness.


def archives(game):
    """The game's asset archives that exist: the base game's first, then each DLC's."""
    game = Path(game)
    found = [game / 'Assets.zip'] + sorted((game / 'Data').glob('*/Assets.zip'))
    return [path for path in found if path.is_file()]


def sound_entries(game):
    """{path under Resources/Sounds without extension: (archive, entry name)} for every sound file in every archive."""
    entries = {}
    for archive in archives(game):
        with zipfile.ZipFile(archive) as zf:
            for name in zf.namelist():
                root = next((root for root in SOUND_ROOTS if name.startswith(root)), None)
                if root and name.lower().endswith(AUDIO_EXTENSIONS):
                    entries.setdefault(name[len(root):].rsplit('.', 1)[0], (archive, name))
    return entries


def parse_range(text, default=None):
    """A game FloatRange such as "13", "14~14" or "10~20", as (low, high)."""
    if text is None:
        return default
    low, _, high = text.strip().partition('~')
    try:
        return (float(low), float(high or low))
    except ValueError:  # A few game Defs leave a range empty or half-written; the game then keeps its default.
        return default


def read_sounddefs(game):
    """{defName: settings} for every SoundDef in the game's Defs XML (inheritance from a ParentName is not followed)."""
    return read_sounddef_files(Path(game, 'Data').glob('*/Defs/**/*.xml'), game)


def read_sounddef_files(paths, base):
    """{defName: settings} for every SoundDef in these XML files, each file named relative to base: the game's, or our mods'
    (scripts/art/export_sound.py checks ours against their exports)."""
    defs = {}
    for path in sorted(paths):
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            continue
        for node in root.iter('SoundDef'):
            name = node.findtext('defName')
            if not name:
                continue
            subs = []
            for sub in node.iterfind('subSounds/li'):
                grains = []
                for grain in sub.iterfind('grains/li'):
                    kind = grain.get('Class', '')
                    grains.append({'class': kind, 'path': grain.findtext('clipPath') or grain.findtext('clipFolderPath') or ''})
                subs.append({'grains': grains, 'volume': parse_range(sub.findtext('volumeRange'), (50.0, 50.0)),
                             'distance': parse_range(sub.findtext('distRange')), 'pitch': parse_range(sub.findtext('pitchRange'), (1.0, 1.0)),
                             'on_camera': (sub.findtext('onCamera') or '').strip().lower() == 'true'})
            defs[name] = {'file': str(Path(path).relative_to(base)).replace('\\', '/'), 'sustain': (node.findtext('sustain') or '').strip().lower() == 'true',
                          'context': node.findtext('context'), 'max_simultaneous': node.findtext('maxSimultaneous'), 'sub_sounds': subs}
    return defs


def clips_for(grain, entries):
    """The sound files a grain plays: one clip, or every file in a clip folder."""
    if 'Folder' in grain['class']:
        prefix = grain['path'].rstrip('/') + '/'
        return sorted(key for key in entries if key.startswith(prefix))
    return [grain['path']] if grain['path'] in entries else []


def measure_clip(exe, entries, key):
    """Loudness, true peak, channels, sample rate and length of one game clip, read from its archive into a temporary file."""
    archive, name = entries[key]
    with tempfile.TemporaryDirectory() as folder:
        target = Path(folder) / Path(name).name
        with zipfile.ZipFile(archive) as zf:
            target.write_bytes(zf.read(name))
        measure = sound_review.measure_file(exe, target)
        probe = sound_review.run_ffmpeg(exe, ['-i', str(target), '-f', 'null', '-'])
    stream = re.search(r'Audio: \w+.*?, (\d+) Hz, (mono|stereo|[\d.]+ channels?)', probe)
    measure['rate'] = int(stream[1]) if stream else None
    measure['channels'] = (1 if stream[2] == 'mono' else 2 if stream[2] == 'stereo' else None) if stream else None
    return measure


def effective_lufs(clip_lufs, volume):
    """A clip's loudness as the game plays it: its own loudness plus the SoundDef's gain (volume / 100)."""
    if clip_lufs is None or not math.isfinite(clip_lufs) or volume <= 0:
        return None
    return round(clip_lufs + 20 * math.log10(volume / 100.0), 1)


def describe_sounddef(exe, name, defs, entries):
    """A SoundDef's settings with each clip measured and its effective loudness."""
    found = defs.get(name)
    if found is None:
        return {'defName': name, 'error': 'no SoundDef of that name in the game\'s Defs'}
    out = {'defName': name, **{key: found[key] for key in ('file', 'sustain', 'context', 'max_simultaneous')}, 'sub_sounds': []}
    for sub in found['sub_sounds']:
        mid_volume = sum(sub['volume']) / 2
        clips = []
        for grain in sub['grains']:
            for key in clips_for(grain, entries):
                measure = measure_clip(exe, entries, key)
                clips.append({'clip': key, **{k: measure[k] for k in ('lufs', 'true_peak', 'seconds', 'channels', 'rate')},
                              'effective_lufs': effective_lufs(measure['lufs'], mid_volume)})
        out['sub_sounds'].append({**sub, 'clips': clips})
    return out


def reference_level(exe, name, game=None):
    """The median effective loudness of a SoundDef's clips, and its first sub-sound's settings, for levelling a sound of ours."""
    game = game or _deploy.game_path()
    described = describe_sounddef(exe, name, read_sounddefs(game), sound_entries(game))
    if 'error' in described:
        raise ValueError(f'{name}: {described["error"]}')
    clips = [clip for sub in described['sub_sounds'] for clip in sub['clips']]
    if not clips:
        raise ValueError(f'{name}: none of its clips could be measured')
    first = described['sub_sounds'][0]
    gain = 20 * math.log10(sum(first['volume']) / 200.0)
    # A clip under 0.4 s reads as silence to a loudness meter (EBU R128 gates in 400 ms blocks), so such a reference is matched by
    # its true peak instead: the game's tiny hiss and drop clips (10 Nov 2026) would otherwise have levelled our sounds to nothing.
    measurable = sorted(clip['effective_lufs'] for clip in clips
                        if clip['effective_lufs'] is not None and (clip['seconds'] or 0) >= SHORT_CLIP_S)
    peaks = sorted(clip['true_peak'] for clip in clips if clip['true_peak'] is not None and math.isfinite(clip['true_peak']))
    channels = sorted(clip['channels'] for clip in clips if clip['channels'])
    return {'defName': name, 'effective_lufs': measurable[len(measurable) // 2] if measurable else None,
            'effective_peak': round(peaks[-1] + gain, 1) if peaks else None, 'by_peak': not measurable,
            'volume': first['volume'], 'distance': first['distance'],
            'channels': channels[len(channels) // 2] if channels else None, 'sustain': described['sustain'],
            'clip_lufs': sorted(clip['lufs'] for clip in clips)}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('command', choices=('find', 'measure', 'sounddef'))
    parser.add_argument('names', nargs='+', help='a wildcard pattern over paths under Resources/Sounds, or SoundDef names')
    parser.add_argument('--game', help='the RimWorld folder (default: as scripts/deploy.py finds it)')
    parser.add_argument('--json', action='store_true', help='print JSON')
    args = parser.parse_args(argv)
    game = _deploy.game_path(args.game)
    entries = sound_entries(game)
    exe = sound_review.find_ffmpeg()
    if args.command != 'find' and exe is None:
        print('ffmpeg was not found: set FFMPEG, or install it (winget install Gyan.FFmpeg)', file=sys.stderr)
        return 2
    if args.command == 'sounddef':
        defs = read_sounddefs(game)
        results = [describe_sounddef(exe, name, defs, entries) for name in args.names]
    else:
        keys = sorted({key for pattern in args.names for key in entries if fnmatch.fnmatch(key.lower(), pattern.lower())})
        if args.command == 'find':
            results = [{'clip': key, 'archive': entries[key][0].name if entries[key][0].parent == Path(game) else
                        str(entries[key][0].relative_to(game)).replace('\\', '/')} for key in keys]
        else:
            results = [{'clip': key, **measure_clip(exe, entries, key)} for key in keys]
    if args.json:
        print(json.dumps(results, indent=2))
        return 0
    for item in results:
        if args.command == 'sounddef':
            if 'error' in item:
                print(f'{item["defName"]}: {item["error"]}')
                continue
            print(f'{item["defName"]} ({item["file"]}): sustain {item["sustain"]}, context {item["context"]}, max simultaneous {item["max_simultaneous"]}')
            for sub in item['sub_sounds']:
                print(f'  volume {sub["volume"]}, distance {sub["distance"]}, pitch {sub["pitch"]}, on camera {sub["on_camera"]}')
                for clip in sub['clips']:
                    print(f'    {clip["clip"]}: {clip["lufs"]} LUFS, peak {clip["true_peak"]} dBTP, {clip["seconds"]} s, '
                          f'{clip["channels"]} ch at {clip["rate"]} Hz -> effective {clip["effective_lufs"]} LUFS')
        elif args.command == 'find':
            print(f'{item["clip"]}  ({item["archive"]})')
        else:
            print(f'{item["clip"]}: {item["lufs"]} LUFS, peak {item["true_peak"]} dBTP, {item["seconds"]} s, {item["channels"]} ch at {item["rate"]} Hz')
    if not results:
        print('nothing matched')
    return 0


if __name__ == '__main__':
    sys.exit(main())
