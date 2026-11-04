"""The rules that end an unattended art run: the stop time, the game check and the disk floor. Game-free and GPU-free, so
they can be tested. scripts/art-run.py feeds them the clock, the process list and the free space.
"""
import csv
import datetime as _dt
import io

# The 1.7 install holds RimWorld.exe (read from the folder, 5 Nov 2026). RimWorldWin64.exe is the 1.6 name, from memory: the
# owner's 1.6 install is gone, so it could not be read. Compared in lower case.
GAME_PROCESSES = ('rimworld.exe', 'rimworldwin64.exe')
DEFAULT_MIN_FREE_GB = 5.0


def parse_stop_at(text, now):
    """HH:MM means its next occurrence, so a run started at 23:00 with 07:00 ends the following morning. Anything else must
    be a full ISO date and time, and one already past is refused rather than ending the run at once."""
    text = text.strip()
    if len(text) <= 5 and ':' in text:
        hour, minute = (int(part) for part in text.split(':'))
        stop = now.replace(hour=hour, minute=minute, second=0, microsecond=0)
        return stop if stop > now else stop + _dt.timedelta(days=1)
    stop = _dt.datetime.fromisoformat(text)
    if stop <= now:
        raise ValueError(f'the stop time {text} has already passed')
    return stop


def parse_tasklist(text):
    """Lower-case process names from `tasklist /FO CSV /NH`."""
    return {row[0].strip().lower() for row in csv.reader(io.StringIO(text)) if row}


def game_running(processes, names=GAME_PROCESSES):
    """The first watched process that is running, or None."""
    for name in names:
        if name.lower() in processes:
            return name
    return None


def stop_reason(now, stop_at, processes, free_gb_by_drive, min_free_gb=DEFAULT_MIN_FREE_GB, names=GAME_PROCESSES):
    """Why the run must end now, or None. The game comes first: the owner's play always wins over a batch. An unreadable
    process list counts as a stop, because a safeguard that cannot look must not assume all is well."""
    if processes is None:
        return 'the process list could not be read, so the game check cannot be trusted'
    game = game_running(processes, names)
    if game:
        return f'{game} is running, and the GPU is the player\'s'
    if now >= stop_at:
        return f'the stop time {stop_at:%H:%M} was reached'
    for drive, free_gb in sorted(free_gb_by_drive.items()):
        if free_gb < min_free_gb:
            return f'{drive} has {free_gb:.1f} GB free, under the {min_free_gb:g} GB floor'
    return None
