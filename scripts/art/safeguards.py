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
# A run on the processor shares the PC with the game, so it also ends before free memory falls under this (authored: the
# game used 12.5 GB on 10 Nov 2026 and grows as a colony does; a sound model on the processor needs several more).
DEFAULT_MIN_FREE_RAM_GB = 6.0


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


def stop_reason(now, stop_at, processes, free_gb_by_drive, min_free_gb=DEFAULT_MIN_FREE_GB, names=GAME_PROCESSES,
                free_ram_gb=None, min_free_ram_gb=DEFAULT_MIN_FREE_RAM_GB):
    """Why the run must end now, or None. The game comes first: the owner's play always wins over a batch. An unreadable
    process list counts as a stop, because a safeguard that cannot look must not assume all is well. A processor run passes no
    game names (it leaves the graphics card alone) but passes the free memory, so it ends before the game could run short."""
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
    if free_ram_gb is not None and free_ram_gb < min_free_ram_gb:
        return f'only {free_ram_gb:.1f} GB of memory is free, under the {min_free_ram_gb:g} GB floor kept for the game'
    return None


def parse_cpu_list(text):
    """An affinity mask from logical processor numbers such as "16-31" or "16-23,28"."""
    mask = 0
    for part in text.split(','):
        first, _, last = part.strip().partition('-')
        for number in range(int(first), int(last or first) + 1):
            mask |= 1 << number
    if not mask:
        raise ValueError(f'no processors in {text!r}')
    return mask


def describe_mask(mask):
    """Logical processor numbers of a mask as ranges, such as "16-31"."""
    numbers = [i for i in range(mask.bit_length()) if mask >> i & 1]
    ranges, start = [], None
    for i, number in enumerate(numbers):
        start = number if start is None else start
        if i + 1 == len(numbers) or numbers[i + 1] != number + 1:
            ranges.append(f'{start}-{number}' if number != start else f'{start}')
            start = None
    return ','.join(ranges)


def pick_cpu_mask(l3_groups, logical_count):
    """Which logical processors a processor run may use, from the L3 caches and the processors sharing each, as (size,
    mask) pairs. Where the caches differ in size, as on a Ryzen X3D whose games sit on the larger 3D V-cache, it is the
    group with the smallest cache, away from the game. Otherwise it is the upper half of the processors, which Windows
    fills last. The run is also kept at low priority, so the game wins any contest for a core."""
    sizes = {size for size, _ in l3_groups}
    if len(sizes) > 1:
        smallest = min(sizes)
        mask = 0
        for size, group in l3_groups:
            if size == smallest:
                mask |= group
        return mask
    half = max(1, logical_count // 2)
    return ((1 << logical_count) - 1) & ~((1 << half) - 1) if logical_count > 1 else 1
