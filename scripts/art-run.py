"""Run art work unattended, with the safeguards: one command from a request file to a review page and a report.

Ends at the stop time, the moment RimWorld starts, or before a drive falls under the free-space floor. Requests for the paid
"openai" engine also need --max-usd (or "max_usd" in a night step), never start ComfyUI, and do not stop for the game, since
they use none of the graphics card; the run ends before any image that could pass the cap. However it ends, it
frees the graphics memory, stops any server it started, and writes a report. Keeps Windows awake only while it runs. If ComfyUI
dies or hangs it starts a fresh one and tries that image again. Candidates are checked against their references for near-copies.

    python scripts/art-run.py generate assets/phobos-medical/requests/first-batch.json --stop-at 07:00
    python scripts/art-run.py generate assets/phobos-medical/requests/first-batch.json --dry-run
    python scripts/art-run.py train C:/AI/lora-experiments/vanilla/config.yaml --stop-at 07:00
    python scripts/art-run.py night .local/art/night.json --stop-at 07:00

A night plan is a JSON list run in order under one stop time. A failed step does not end the night; a later step that needs its
work fails in turn and says so. Each step is one of:

    {"generate": "request file", "near_copy_against": ["extra source folder"], "max_usd": 0.50}
    {"train": "ai-toolkit config"}
    {"run": ["scripts/art/some_tool.py", "arguments"]}
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
NOT_STARTED = 3  # The exit code for a run that a safeguard refused to start.
NIGHT_STEPS = ('generate', 'train', 'run')


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
    """Free space by drive, so a folder that does not exist yet (or a mistyped one) cannot crash the check."""
    drives = {Path(path).resolve().anchor for path in paths}
    return {drive: shutil.disk_usage(drive).free / 1e9 for drive in drives}


def make_checker(stop_at, min_free_gb, watch_paths, game_names):
    """The should_stop callback. It reads the process list and the disks at most once per POLL_SECONDS."""
    state = {'next': 0.0, 'reason': None}

    def should_stop():
        if time.monotonic() >= state['next'] and not state['reason']:
            state['next'] = time.monotonic() + POLL_SECONDS
            state['reason'] = safeguards.stop_reason(_dt.datetime.now(), stop_at, read_processes(), free_gb(watch_paths), min_free_gb, game_names)
        return state['reason']
    return should_stop


OWN_ART = ('mods', 'assets', '.local/art')


def near_copy_sources(requests, extra=()):
    """The folders holding a batch's references, plus any named by hand, each once. Our own art is left out: it lives in the
    repository, which never holds game art, or in .local/art, which holds only the owner's picks from our own generations (the
    game's art extracted for reference lives in .local/references and stays a source). Looking like our own art is the point of
    using it as a reference; before this, a pick used as a reference flagged every image drawn from it and itself."""
    folders = [ROOT / ref if not Path(ref).is_absolute() else Path(ref) for req in requests for ref in req.get('references', [])]
    folders = [path for path in folders if not any(path.is_relative_to(ROOT / own) for own in OWN_ART)]
    return list(dict.fromkeys([*(str(path.parent) for path in folders), *(str(Path(path)) for path in extra)]))


def check_near_copies(python, generated, sources):
    """Returns (whether the check ran, the flags, a problem). A flagged image is a finding, not a failure of the check, and the
    tool exits 1 for both, so what tells them apart is whether it wrote its results."""
    results = generated / 'near-copy.json'
    results.unlink(missing_ok=True)
    try:
        done = subprocess.run([python, '-I', str(ROOT / 'scripts' / 'art' / 'near_copy.py'), str(generated), *sources],
                              capture_output=True, text=True, check=False)
    except OSError as error:
        return False, [], f'near_copy.py: {error}'
    if not results.is_file():
        return False, [], f'near_copy.py: {done.stderr.strip()[-400:]}'
    return True, json.loads(results.read_text(encoding='utf-8')).get('flagged', []), None


def near_copy_lines(sources, ran, flags):
    if not sources:
        return []
    if not ran:
        return ['- **The check did not run, so pick nothing from this batch until it has.**']
    if not flags:
        return ['- Nothing flagged. A slightly tilted copy can still pass, so your eye is the last check.']
    return [f'- **{len(flags)} flagged; do not pick these:**'] + [
        f'  - {entry["output"]}: too close to {entry["source"]} (structure {entry["ssim"]}, hash distance {entry["hash_distance"]})' for entry in flags]


def timing_lines(timings):
    """Seconds per image by request. The first of each is apart because it may include loading the model."""
    groups = {}
    for item in timings:
        groups.setdefault((item['request'], item['engine'], item['steps']), []).append(item['seconds'])
    lines = []
    for (request, engine, steps), seconds in groups.items():
        rest = seconds[1:]
        later = f', then {sum(rest) / len(rest):.0f} s each for {len(rest)} more' if rest else ''
        lines.append(f'- {request} ({engine}, {steps} steps): first image {seconds[0]:.0f} s{later}')
    return lines


def post_process(comfy, generated, folders, sources=()):
    """Sort, sheet, near-copy check and review page. A failure here is reported; it must not hide the run's own result.
    Returns the problems and the lines for the report's near-copy section."""
    python = str(Path(comfy) / 'python_embeded' / 'python.exe')
    tools = ROOT / 'scripts' / 'art'
    problems = []
    existing = [name for name in dict.fromkeys(folders) if (generated / name).is_dir() and any((generated / name).glob('*.png'))]
    steps = []
    if existing:
        steps.append([python, '-I', str(tools / 'sort_candidates.py'), *[str(generated / name) for name in existing]])
        steps += [[python, '-I', str(tools / 'contact_sheet.py'), str(generated), name] for name in existing]
    for step in steps:
        problems += _run_tool(step)
    ran, flags = False, []
    if existing and sources:
        ran, flags, problem = check_near_copies(python, generated, sources)
        if problem:
            problems.append(problem)
    problems += _run_tool([sys.executable, '-I', str(tools / 'review_page.py'), str(generated)])
    return problems, near_copy_lines(sources if existing else (), ran, flags)


def _run_tool(step):
    try:
        done = subprocess.run(step, capture_output=True, text=True, check=False)
    except OSError as error:
        return [f'{Path(step[2]).name}: {error}']
    return [f'{Path(step[2]).name}: {done.stderr.strip()[-400:]}'] if done.returncode else []


def stop_process(process):
    process.terminate()
    try:
        process.wait(timeout=60)
    except subprocess.TimeoutExpired:
        process.kill()


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
        stop_process(process)
        notes.append('stopped the ComfyUI server this run started')
    else:
        notes.append('left running the ComfyUI server that was already up')
    return notes


def write_report(folder, kind, source, started, stop_at, result, notes, problems, near_copy=()):
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
    if 'spent_usd' in result:
        lines.append(f'- **Spent:** about US${result["spent_usd"]:.3f} of the US${result["max_usd"]:.2f} cap, worked out from '
                     f'the token counts OpenAI returned (each image\'s record has its own figure)')
    if result.get('failures'):
        lines += ['', '## Failures', ''] + [f'- {item["image"]}: {item["error"]}' for item in result['failures']]
    if result.get('restarts'):
        lines += ['', '## ComfyUI restarted', ''] + [f'- {item}' for item in result['restarts']]
    if near_copy:
        lines += ['', '## Near-copy check', ''] + list(near_copy)
    if result.get('timings'):
        lines += ['', '## Time per image', ''] + timing_lines(result['timings'])
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
    requests = list({item[0]['name']: item[0] for item in images}.values())
    any_paid = any(art.is_paid(req) for req in requests)
    any_local = any(not art.is_paid(req) for req in requests)
    paid_problems = art.paid_problems(requests)
    estimate = art.paid_estimate(images)
    graphs = art.preview_graphs(request_file, args.only)
    if args.dry_run:
        for req in requests:
            engine = art.ENGINES[req.get('engine', 'schnell')]
            how = (f'{art.paid.settings(req)["quality"]} quality, up to US${art.paid.estimate_usd(req):.3f} an image'
                   if art.is_paid(req) else f'{req.get("steps", engine["steps"])} steps')
            print(f'  {req["name"]}: {req.get("engine", "schnell")}, {how}, '
                  f'{len(req["seeds"])} seeds, {len(req.get("references", []))} references, {req["width"]}x{req["height"]}')
        if any_paid:
            print(f'paid images still to make should cost at most about US${estimate:.2f}')
        for problem in paid_problems:
            print(f'  REFUSED: {problem}')
        return (2 if paid_problems else 0), None
    if any_paid:
        if paid_problems:
            print('not starting: the paid requests would be refused, so nothing was sent:\n  ' + '\n  '.join(paid_problems))
            return NOT_STARTED, None
        if args.max_usd is None:
            print(f'not starting: paid requests need a spending cap; pass --max-usd (these should cost at most about US${estimate:.2f})')
            return NOT_STARTED, None
        if not art.os.environ.get(art.paid.KEY_VARIABLE):
            print(f'not starting: the {art.paid.KEY_VARIABLE} environment variable is not set')
            return NOT_STARTED, None
    budget = art.paid.Budget(args.max_usd) if any_paid else None
    if not any_local:
        game_names = ()  # Every image is made on OpenAI's servers: the game can have the graphics card meanwhile.

    comfy = Path(args.comfy)
    watch = [comfy, generated.parent]
    should_stop = make_checker(stop_at, args.min_free_gb, watch, game_names)
    reason = should_stop()
    if reason:
        print(f'not starting: {reason}')
        return 3, None
    server = {'process': None}
    started = _dt.datetime.now()
    result = {'made': 0, 'skipped': 0, 'failures': [], 'restarts': [], 'timings': [], 'folders': [], 'stopped': None}
    notes, problems, near_copy = [], [], []
    sources = near_copy_sources((item[0] for item in images), args.near_copy_against)

    def restart_server():
        """A fresh server, if that is possible. A running one this run did not start belongs to someone else, so it stays."""
        if server['process'] is None and art.server_up():
            return False
        try:
            if server['process'] is not None:
                stop_process(server['process'])
            server['process'] = None
            server['process'] = art.start_server(comfy)
        except (RuntimeError, OSError) as error:
            print(f'could not restart ComfyUI: {error}', flush=True)
            return False
        return True

    stay_awake(True)
    try:
        invalid = []
        if any_local:
            if not art.server_up():
                server['process'] = art.start_server(comfy)
            info = art.get_json('/object_info')
            invalid = [f'{name}: {problem}' for name, graph in graphs.items() for problem in art.validate_graph(graph, info)]
        if invalid:
            result['stopped'] = 'the graphs failed their check, so no image was attempted'
            result['failures'] = [{'image': 'graph check', 'error': problem} for problem in invalid]
        else:
            result = art.run(request_file, args.only, comfy, False, should_stop, keep_going=True, restart_server=restart_server,
                             budget=budget)
    except (RuntimeError, urllib.error.URLError, OSError, ValueError, FileNotFoundError) as error:
        result['stopped'] = f'the run failed: {art.paid.redact(error)}'
    finally:
        if budget is not None:
            result['spent_usd'], result['max_usd'] = round(budget.spent_usd, 4), budget.max_usd
        notes = free_gpu(server['process']) if any_local else ['none used: every image was made on OpenAI\'s servers']
        problems, near_copy = post_process(comfy, generated, result['folders'] or [item[0]['name'] for item in images], sources)
        stay_awake(False)
        report = write_report(generated, 'generate', request_file, started, stop_at, result, notes, problems, near_copy)
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


def night_problems(steps):
    """Everything wrong with a night plan, found before the night starts rather than at three in the morning."""
    if not isinstance(steps, list) or not steps:
        return ['a night plan is a list with at least one step']
    problems = []
    for index, step in enumerate(steps, 1):
        kinds = [kind for kind in NIGHT_STEPS if isinstance(step, dict) and kind in step]
        if len(kinds) != 1:
            problems.append(f'step {index} needs exactly one of {", ".join(NIGHT_STEPS)}')
            continue
        unknown = set(step) - {kinds[0], 'near_copy_against', 'max_usd'}
        generate_only = [key for key in ('near_copy_against', 'max_usd') if key in step and kinds[0] != 'generate']
        if unknown or generate_only:
            problems.append(f'step {index} has settings it cannot use: {sorted(unknown) or generate_only}')
        if kinds[0] == 'run':
            script = step['run'][0].replace('\\', '/') if step['run'] else ''
            if not script.startswith('scripts/') or '..' in script or not (ROOT / script).is_file():
                problems.append(f'step {index} runs {script or "nothing"}: only a script in this repository\'s scripts folder may run')
    return problems


def step_command(step, stop_at, args):
    """The command for one night step. All of them share the night's one stop time."""
    if 'run' in step:
        return [sys.executable, str(ROOT / step['run'][0]), *step['run'][1:]]
    kind = 'generate' if 'generate' in step else 'train'
    command = [sys.executable, str(Path(__file__).resolve()), kind, step[kind], '--comfy', args.comfy, '--min-free-gb', str(args.min_free_gb)]
    command += ['--dry-run'] if args.dry_run else ['--stop-at', stop_at.isoformat(timespec='seconds')]
    for name in args.also_stop_on:
        command += ['--also-stop-on', name]
    for folder in step.get('near_copy_against', []):
        command += ['--near-copy-against', folder]
    if 'max_usd' in step:
        command += ['--max-usd', str(step['max_usd'])]
    return command


def run_step(command):
    """Run a step, passing its output through. Returns its exit code and the report it named, if any."""
    report = None
    with subprocess.Popen(command, cwd=str(ROOT), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, errors='replace') as process:
        for line in process.stdout:
            print(line, end='', flush=True)
            if line.startswith('report: '):
                report = line[len('report: '):].strip()
    return process.returncode, report


def describe_step(step):
    kind = next(kind for kind in NIGHT_STEPS if kind in step)
    return f'{kind}: {" ".join(step[kind]) if kind == "run" else step[kind]}'


def night(args, stop_at, _game_names):
    """Each step in order, whatever became of the one before. The safeguards live in the steps, so a game started at midnight
    also refuses every later one. Writes one summary beside the plan, pointing at each step's own report."""
    plan_file = Path(args.source).resolve()
    steps = json.loads(plan_file.read_text(encoding='utf-8'))
    problems = night_problems(steps)
    if problems:
        print('the night plan was refused:\n  ' + '\n  '.join(problems))
        return 2, None
    if args.dry_run:
        for index, step in enumerate(steps, 1):
            print(f'step {index}, {describe_step(step)}')
            target = step.get('generate')
            if target and Path(target).is_file():
                run_step(step_command(step, stop_at, args))
            elif target:
                print('  an earlier step has to make this file first')
        return 0, None
    started = _dt.datetime.now()
    lines = [f'# Art night {started:%Y%m%d-%H%M}', '', f'- **Plan:** {art.display_path(plan_file)}', f'- **Stop time:** {stop_at:%Y-%m-%d %H:%M}', '',
             '| Step | Started | Minutes | How it ended | Report |', '| --- | --- | --- | --- | --- |']
    stay_awake(True)
    try:
        for step in steps:
            began = _dt.datetime.now()
            if began >= stop_at:
                lines.append(f'| {describe_step(step)} | | | not started: the stop time was reached | |')
                continue
            print(f'--- {describe_step(step)}', flush=True)
            code, report = run_step(step_command(step, stop_at, args))
            ended = {0: 'finished or stopped safely; its report says which', NOT_STARTED: 'a safeguard refused to start it'}.get(code, f'failed, exit code {code}')
            lines.append(f'| {describe_step(step)} | {began:%H:%M} | {(_dt.datetime.now() - began).total_seconds() / 60:.0f} | {ended} | {report or ""} |')
    finally:
        stay_awake(False)
        path = plan_file.parent / f'night-{started:%Y%m%d-%H%M}.md'
        path.write_text('\n'.join(lines) + '\n', encoding='utf-8')
        print(f'night summary: {path}', flush=True)
    return 0, path


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('kind', choices=('generate', 'train', 'night'))
    parser.add_argument('source', help='request file, an ai-toolkit config for train, or a plan for night')
    parser.add_argument('--stop-at', help='HH:MM (its next occurrence) or a full ISO date and time; required unless --dry-run')
    parser.add_argument('--only', help='generate only the request with this name')
    parser.add_argument('--comfy', default=str(art.DEFAULT_COMFY), help='ComfyUI portable folder')
    parser.add_argument('--min-free-gb', type=float, default=safeguards.DEFAULT_MIN_FREE_GB)
    parser.add_argument('--also-stop-on', action='append', default=[], metavar='NAME.exe', help='another process that ends the run, such as another game')
    parser.add_argument('--near-copy-against', action='append', default=[], metavar='FOLDER', help='check candidates against this folder too')
    parser.add_argument('--max-usd', type=float, help='spending cap in US dollars for paid engines (openai); required when any request is paid')
    parser.add_argument('--dry-run', action='store_true', help='print the work, and what paid images could cost, without touching ComfyUI, the GPU or any paid service')
    args = parser.parse_args(argv)
    if not args.stop_at and not args.dry_run:
        parser.error('--stop-at is required: an unattended run must have an end')
    now = _dt.datetime.now()
    stop_at = safeguards.parse_stop_at(args.stop_at, now) if args.stop_at else now
    names = (*safeguards.GAME_PROCESSES, *args.also_stop_on)
    code, _ = {'generate': generate, 'train': train, 'night': night}[args.kind](args, stop_at, names)
    return code


if __name__ == '__main__':
    sys.exit(main())
