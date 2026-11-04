"""Run art work unattended, with the safeguards: one command from a request file to a review page and a report.

Ends at the stop time, the moment RimWorld starts, or before a drive falls under the free-space floor. However it ends, it
frees the graphics memory, stops any server it started, and writes a report. Keeps Windows awake only while it runs.

    python scripts/art-run.py generate assets/phobos-medical/requests/first-batch.json --stop-at 07:00
    python scripts/art-run.py generate assets/phobos-medical/requests/first-batch.json --dry-run
    python scripts/art-run.py train C:/AI/lora-experiments/vanilla/config.yaml --stop-at 07:00
"""
import argparse
import ctypes
import datetime as _dt
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import time
import urllib.error

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import safeguards  # noqa: E402

_spec = importlib.util.spec_from_file_location('generate_art', ROOT / 'scripts' / 'generate-art.py')
art = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(art)

AI_TOOLKIT = Path(r'C:\AI\ai-toolkit')
POLL_SECONDS = 5
ES_CONTINUOUS, ES_SYSTEM_REQUIRED = 0x80000000, 0x00000001


def stay_awake(on):
    """Ask Windows not to sleep while this process runs. Ends with the process; changes no setting."""
    if hasattr(ctypes, 'windll'):
        ctypes.windll.kernel32.SetThreadExecutionState(ES_CONTINUOUS | (ES_SYSTEM_REQUIRED if on else 0))


def read_processes():
    """Lower-case process names, or None if they cannot be read (which stops the run: see safeguards.stop_reason)."""
    try:
        listing = subprocess.run(['tasklist', '/FO', 'CSV', '/NH'], capture_output=True, text=True, timeout=30, check=True)
    except (OSError, subprocess.SubprocessError):
        return None
    return safeguards.parse_tasklist(listing.stdout)


def free_gb(paths):
    return {path.anchor or str(path): shutil.disk_usage(path).free / 1e9 for path in paths}


def make_checker(stop_at, min_free_gb, watch_paths, game_names):
    """The should_stop callback. It reads the process list and the disks at most once per POLL_SECONDS."""
    state = {'next': 0.0, 'reason': None}

    def should_stop():
        if time.monotonic() >= state['next'] and not state['reason']:
            state['next'] = time.monotonic() + POLL_SECONDS
            state['reason'] = safeguards.stop_reason(_dt.datetime.now(), stop_at, read_processes(), free_gb(watch_paths), min_free_gb, game_names)
        return state['reason']
    return should_stop


def post_process(comfy, generated, folders):
    """Sort, sheet and review page. A failure here is reported; it must not hide the run's own result."""
    python = str(Path(comfy) / 'python_embeded' / 'python.exe')
    tools = ROOT / 'scripts' / 'art'
    problems = []
    existing = [name for name in dict.fromkeys(folders) if (generated / name).is_dir() and any((generated / name).glob('*.png'))]
    steps = []
    if existing:
        steps.append([python, '-I', str(tools / 'sort_candidates.py'), *[str(generated / name) for name in existing]])
        steps += [[python, '-I', str(tools / 'contact_sheet.py'), str(generated), name] for name in existing]
    steps.append([sys.executable, '-I', str(tools / 'review_page.py'), str(generated)])
    for step in steps:
        done = subprocess.run(step, capture_output=True, text=True, check=False)
        if done.returncode:
            problems.append(f'{Path(step[2]).name}: {done.stderr.strip()[-400:]}')
    return problems


def free_gpu(process):
    """Ask ComfyUI to unload everything, then stop the server if this run started it. Returns what happened, in words."""
    notes = []
    try:
        if art.server_up():
            art.post_json('/free', {'unload_models': True, 'free_memory': True})
            notes.append('asked ComfyUI to unload its models and free memory')
    except (urllib.error.URLError, OSError, ValueError) as error:
        notes.append(f'the free-memory call failed: {error}')
    if process is not None:
        process.terminate()
        try:
            process.wait(timeout=60)
        except subprocess.TimeoutExpired:
            process.kill()
        notes.append('stopped the ComfyUI server this run started')
    else:
        notes.append('left running the ComfyUI server that was already up')
    return notes


def write_report(folder, kind, source, started, stop_at, result, notes, problems):
    finished = _dt.datetime.now()
    folder.mkdir(parents=True, exist_ok=True)
    stamp = started.strftime('%Y%m%d-%H%M')
    lines = [f'# Art run {stamp}', '',
             f'- **Kind:** {kind}, from {art.display_path(source)}',
             f'- **Started:** {started:%Y-%m-%d %H:%M}; **finished:** {finished:%Y-%m-%d %H:%M}; **stop time:** {stop_at:%Y-%m-%d %H:%M}',
             f'- **Ended because:** {result.get("stopped") or "the work finished"}']
    for key, label in (('made', 'Images made'), ('skipped', 'Already there, skipped'), ('exit_code', 'Exit code')):
        if key in result:
            lines.append(f'- **{label}:** {result[key]}')
    if result.get('failures'):
        lines += ['', '## Failures', ''] + [f'- {item["image"]}: {item["error"]}' for item in result['failures']]
    lines += ['', '## Graphics memory', ''] + [f'- {note}' for note in notes]
    if problems:
        lines += ['', '## Review tools that failed', ''] + [f'- {problem}' for problem in problems]
    if (folder / 'review.html').is_file():
        lines += ['', f'Review page: {art.display_path(folder / "review.html")}']
    path = folder / f'run-{stamp}.md'
    path.write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print(f'report: {path}')
    return path


def generate(args, stop_at, game_names):
    request_file = Path(args.source)
    images = art.plan(request_file, args.only)
    generated = art.output_root(request_file)
    missing = [item for item in images if not item[2].exists()]
    print(f'{len(images)} images asked for, {len(missing)} still to make')
    graphs = art.preview_graphs(request_file, args.only)
    if args.dry_run:
        for req in {item[0]['name']: item[0] for item in images}.values():
            engine = art.ENGINES[req.get('engine', 'schnell')]
            print(f'  {req["name"]}: {req.get("engine", "schnell")}, {req.get("steps", engine["steps"])} steps, '
                  f'{len(req["seeds"])} seeds, {len(req.get("references", []))} references, {req["width"]}x{req["height"]}')
        return 0, None

    comfy = Path(args.comfy)
    watch = [comfy, generated.parent]
    should_stop = make_checker(stop_at, args.min_free_gb, watch, game_names)
    reason = should_stop()
    if reason:
        print(f'not starting: {reason}')
        return 3, None
    process = None
    started = _dt.datetime.now()
    result = {'made': 0, 'skipped': 0, 'failures': [], 'folders': [], 'stopped': None}
    notes, problems = [], []
    stay_awake(True)
    try:
        if not art.server_up():
            process = art.start_server(comfy)
        info = art.get_json('/object_info')
        invalid = [f'{name}: {problem}' for name, graph in graphs.items() for problem in art.validate_graph(graph, info)]
        if invalid:
            result['stopped'] = 'the graphs failed their check, so no image was attempted'
            result['failures'] = [{'image': 'graph check', 'error': problem} for problem in invalid]
        else:
            result = art.run(request_file, args.only, comfy, False, should_stop, keep_going=True)
    except (RuntimeError, urllib.error.URLError, OSError, ValueError, FileNotFoundError) as error:
        result['stopped'] = f'the run failed: {error}'
    finally:
        notes = free_gpu(process)
        problems = post_process(comfy, generated, result['folders'] or [item[0]['name'] for item in images])
        stay_awake(False)
        report = write_report(generated, 'generate', request_file, started, stop_at, result, notes, problems)
    print(json.dumps({key: result.get(key) for key in ('made', 'skipped', 'stopped')}))
    return (1 if result['failures'] and not result['made'] else 0), report


def train(args, stop_at, game_names):
    """Run an ai-toolkit config under the same safeguards. The toolkit saves checkpoints as it goes and resumes from the
    newest, so a stop loses at most the steps since the last save."""
    config = Path(args.source).resolve()
    python = AI_TOOLKIT / 'venv' / 'Scripts' / 'python.exe'
    if args.dry_run:
        print(f'would run {python} run.py {config}')
        return 0, None
    if not python.is_file():
        print(f'ai-toolkit is not installed at {AI_TOOLKIT}')
        return 2, None
    should_stop = make_checker(stop_at, args.min_free_gb, [AI_TOOLKIT, config.parent], game_names)
    reason = should_stop()
    if reason:
        print(f'not starting: {reason}')
        return 3, None
    started = _dt.datetime.now()
    stay_awake(True)
    result = {'stopped': None}
    with open(config.parent / f'train-{started:%Y%m%d-%H%M}.log', 'ab') as log:
        process = subprocess.Popen([str(python), 'run.py', str(config)], cwd=str(AI_TOOLKIT), stdout=log, stderr=subprocess.STDOUT)
        try:
            while process.poll() is None:
                reason = should_stop()
                if reason:
                    result['stopped'] = reason
                    process.terminate()
                    try:
                        process.wait(timeout=120)
                    except subprocess.TimeoutExpired:
                        process.kill()
                    break
                time.sleep(POLL_SECONDS)
        finally:
            stay_awake(False)
    result['exit_code'] = process.returncode
    report = write_report(config.parent, 'train', config, started, stop_at, result, ['training ended, which released its graphics memory'], [])
    return (0 if result['stopped'] or process.returncode == 0 else 1), report


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('kind', choices=('generate', 'train'))
    parser.add_argument('source', help='request file, or an ai-toolkit config for train')
    parser.add_argument('--stop-at', help='HH:MM (its next occurrence) or a full ISO date and time; required unless --dry-run')
    parser.add_argument('--only', help='generate only the request with this name')
    parser.add_argument('--comfy', default=str(art.DEFAULT_COMFY), help='ComfyUI portable folder')
    parser.add_argument('--min-free-gb', type=float, default=safeguards.DEFAULT_MIN_FREE_GB)
    parser.add_argument('--also-stop-on', action='append', default=[], metavar='NAME.exe', help='another process that ends the run, such as another game')
    parser.add_argument('--dry-run', action='store_true', help='print the work without touching ComfyUI or the GPU')
    args = parser.parse_args(argv)
    if not args.stop_at and not args.dry_run:
        parser.error('--stop-at is required: an unattended run must have an end')
    now = _dt.datetime.now()
    stop_at = safeguards.parse_stop_at(args.stop_at, now) if args.stop_at else now
    names = (*safeguards.GAME_PROCESSES, *args.also_stop_on)
    code, _ = (generate if args.kind == 'generate' else train)(args, stop_at, names)
    return code


if __name__ == '__main__':
    sys.exit(main())
