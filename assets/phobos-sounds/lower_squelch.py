"""Lowered copies of the base station's night-16 squelch tries (the owner's note on the Picks page, 27 Nov 2026: "Can you shift
everything down?").

    python assets/phobos-sounds/lower_squelch.py

Each try is pitched down with ffmpeg's rubberband filter, keeping its length, by 5 semitones and by a full octave, into a folder of its
own (generated/n16-squelch-station-low), each with a record naming its source, its hash and the command. The originals stay byte for
byte. Then offer them: python scripts/art/sound_review.py assets/phobos-sounds/generated --offer n16-squelch-station-low
"""
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GENERATED = ROOT / 'assets' / 'phobos-sounds' / 'generated'
SOURCE = 'n16-squelch-station'
OUT = 'n16-squelch-station-low'
STEPS = {'d5': -5, 'd12': -12}  # semitones down


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    folder = GENERATED / OUT
    folder.mkdir(parents=True, exist_ok=True)
    for source in sorted((GENERATED / SOURCE).glob(f'{SOURCE}-*.flac')):
        seed = source.stem.rsplit('-', 1)[-1]
        record = json.loads(source.with_suffix('.json').read_text(encoding='utf-8'))
        for tag, semitones in STEPS.items():
            out = folder / f'{OUT}-{seed}{tag}.flac'
            factor = 2 ** (semitones / 12)
            command = ['ffmpeg', '-v', 'error', '-y', '-i', str(source), '-af', f'rubberband=pitch={factor:.6f}', str(out)]
            subprocess.run(command, check=True)
            derived = dict(record)
            derived.update({
                'file': out.name, 'sha256': sha(out),
                'derived_from': {'file': f'{SOURCE}/{source.name}', 'sha256': sha(source),
                                 'how': f'pitched down {-semitones} semitones, length kept: ffmpeg -af rubberband=pitch={factor:.6f}',
                                 'why': "the owner's note on the Picks page, 27 Nov 2026: \"Can you shift everything down?\""},
            })
            out.with_suffix('.json').write_text(json.dumps(derived, indent=2) + '\n', encoding='utf-8')
            print('wrote', out.relative_to(ROOT).as_posix())


if __name__ == '__main__':
    main()
