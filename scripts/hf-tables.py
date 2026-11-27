"""The radio's HF propagation tables, from VOACAP (docs/development/radio-hf.md; the owner's cards of 28 Nov 2026).

    python scripts/hf-tables.py run          every path of the grid through voacapl in WSL, then packed
    python scripts/hf-tables.py pack         pack the results again (another set of bands, a new format)
    python scripts/hf-tables.py show         the packed file's axes and size
    python scripts/hf-tables.py ground       the ground wave through NTIA's LFMF in WSL, then packed (radio slice 1)
    python scripts/hf-tables.py ship         copy both packed tables into the radio mod's Common/Data/HF

voacapl runs only on the machine that makes the tables, inside WSL (the owner's choice: voacapl under WSL; installed 28 Nov 2026 in
the distribution Ubuntu-24.04, run as root, its data in /root/itshfbc). Its results and the packed file go to the work folder, the
setting hfTablesPath (config/settings.json; the owner's machine keeps it on K:). Nothing here is committed: the same voacapl and grid
give the same tables; `ship` copies the packed files into the mod (HF's slice 1, 28 Nov 2026). LFMF (NTIA's LF/MF model, the program
behind ITU-R P.368-10) was built in /opt/lfmf of the same distribution on 28 Nov 2026. It uses the processor only,
at low priority, so it may run while the owner plays.
"""
import argparse
import json
import subprocess
import sys
from datetime import date
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import _modkit  # noqa: E402

_modkit.ensure()
from phobos_modkit.progress import Progress  # noqa: E402
from radio import ground_wave, hf_tables  # noqa: E402

ROOT = HERE.parent
DISTRO = "Ubuntu-24.04"
# The bands HF's first slice uses (the owner's card: 40 m and 20 m first); the worker keeps every band's results for later slices.
FIRST_BANDS = ["40m", "20m"]


def work_folder(explicit=None):
    import deploy
    return deploy.local_path("hfTablesPath", "PHOBOS_HF_TABLES", ROOT / ".local" / "hf-tables", explicit)


def wsl_path(path):
    """A Windows path as WSL sees it: K:\\x\\y is /mnt/k/x/y."""
    p = Path(path).resolve()
    return "/mnt/" + p.drive.rstrip(":").lower() + p.as_posix()[len(p.drive):]


def voacapl_version(distro):
    done = subprocess.run(["wsl", "-d", distro, "-u", "root", "--", "bash", "-lc", "cd /opt/voacapl 2>/dev/null && git log -1 --format=%h"],
                          capture_output=True, text=True)
    return done.stdout.strip() or "unknown"


def run(args):
    folder = work_folder(args.folder)
    raw = folder / "raw"
    raw.mkdir(parents=True, exist_ok=True)
    months = args.months or hf_tables.MONTHS
    progress = Progress()
    command = ["wsl", "-d", args.distro, "-u", "root", "--", "python3", wsl_path(HERE / "radio" / "hf_tables.py"), wsl_path(raw),
               "--parallel", str(args.parallel), "--months", *map(str, months)]
    print(f"running {len(months)} month(s) through voacapl in {args.distro}, {args.parallel} at a time, low priority")
    done_months = 0
    with subprocess.Popen(command, stdout=subprocess.PIPE, text=True) as worker:
        for line in worker.stdout:
            line = line.strip()
            if not line:
                continue
            print(line)
            if '"month"' in line:
                done_months += 1
                progress(f"Month {json.loads(line)['month']} done", done_months, len(months))
    if worker.returncode != 0:
        print("the worker failed; nothing packed")
        return 1
    return pack(args)


def pack(args):
    folder = work_folder(args.folder)
    cells = hf_tables.load_cells(str(folder / "raw"))
    if not cells:
        print(f"no results in {folder / 'raw'}: run first")
        return 1
    bands = args.bands.split(",") if args.bands else FIRST_BANDS
    data = hf_tables.pack(cells, bands)
    out = folder / "hf-propagation.bin"
    out.write_bytes(data)
    header = hf_tables.unpack_header(data)
    manifest = {"made": date.today().isoformat(), "from": "VOACAP (voacapl " + voacapl_version(args.distro) + ")",
                "cells": len(cells), "bands": bands, "bytes": len(data), "axes": {k: header[k] for k in
                ("months", "sunspots", "latitudes", "distances", "hours", "kHz")},
                "how": "isotropic antennas at both ends, 1 kW, man-made noise remote (164); each cell averaged over longitudes "
                       f"{hf_tables.LONGITUDES} and bearings {hf_tables.BEARINGS}, in the midpoint's local hours"}
    (folder / "hf-propagation.json").write_text(json.dumps(manifest, indent=1) + "\n", encoding="utf-8")
    print(f"packed {len(cells)} cells, bands {', '.join(bands)}: {len(data):,} bytes in {out}")
    return 0


def show(args):
    out = work_folder(args.folder) / "hf-propagation.bin"
    if not out.is_file():
        print(f"no packed tables at {out}: run first")
        return 1
    header = hf_tables.unpack_header(out.read_bytes())
    print(json.dumps({k: v for k, v in header.items()}, indent=1))
    return 0


SHIP = ROOT / "mods" / "PhobosRadioFramework" / "Common" / "Data" / "HF"


def ground(args):
    folder = work_folder(args.folder)
    folder.mkdir(parents=True, exist_ok=True)
    raw = folder / "ground-wave-raw.json"
    print(f"running {len(ground_wave.jobs())} ground-wave points through LFMF in {args.distro}, {args.parallel} at a time, low priority")
    done = subprocess.run(["wsl", "-d", args.distro, "-u", "root", "--", "python3", wsl_path(HERE / "radio" / "ground_wave.py"), wsl_path(raw),
                           "--parallel", str(args.parallel)], capture_output=True, text=True)
    print(done.stdout.strip() or done.stderr.strip())
    if done.returncode != 0 or '"failures": 0' not in done.stdout:
        print("the worker failed or some points did not run; nothing packed")
        return 1
    bands = args.bands.split(",") if args.bands else FIRST_BANDS
    data = ground_wave.pack(ground_wave.load(str(raw)), bands)
    out = folder / "ground-wave.bin"
    out.write_bytes(data)
    lfmf = subprocess.run(["wsl", "-d", args.distro, "-u", "root", "--", "bash", "-lc", "cd /opt/lfmf && git log -1 --format=%h"],
                          capture_output=True, text=True).stdout.strip() or "unknown"
    manifest = {"made": date.today().isoformat(), "from": f"NTIA/ITS LFMF (commit {lfmf})", "bands": bands, "bytes": len(data),
                "grounds": [g[0] for g in ground_wave.GROUNDS], "heights": ground_wave.HEIGHTS, "distances": ground_wave.DISTANCES,
                "how": "basic transmission loss, both polarisations; grounds from ITU-R P.527-6 Figure 24 read by eye; N_s 301"}
    (folder / "ground-wave.json").write_text(json.dumps(manifest, indent=1) + "\n", encoding="utf-8")
    print(f"packed bands {', '.join(bands)}: {len(data):,} bytes in {out}")
    return 0


def ship(args):
    import shutil
    folder = work_folder(args.folder)
    SHIP.mkdir(parents=True, exist_ok=True)
    for name in ("hf-propagation.bin", "hf-propagation.json", "ground-wave.bin", "ground-wave.json"):
        if not (folder / name).is_file():
            print(f"no {name} in {folder}: run and ground first")
            return 1
        shutil.copyfile(folder / name, SHIP / name)
    print(f"copied the tables into {SHIP.relative_to(ROOT)}")
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--folder", help="the work folder (default: the hfTablesPath setting)")
    parser.add_argument("--distro", default=DISTRO, help=f"the WSL distribution with voacapl (default {DISTRO})")
    sub = parser.add_subparsers(dest="command", required=True)
    r = sub.add_parser("run", help="every path through voacapl, then pack")
    r.add_argument("--parallel", type=int, default=8, help="voacapl runs at once (default 8, leaving the game its cores)")
    r.add_argument("--months", type=int, nargs="*", help="only these months (default all twelve)")
    r.add_argument("--bands", help="bands to pack, comma-separated (default the first slice's: 40m,20m)")
    p = sub.add_parser("pack", help="pack the results again")
    p.add_argument("--bands", help="bands to pack, comma-separated (default 40m,20m)")
    sub.add_parser("show", help="the packed file's axes and size")
    g = sub.add_parser("ground", help="the ground wave through LFMF, then pack")
    g.add_argument("--parallel", type=int, default=8, help="LFMF runs at once (default 8)")
    g.add_argument("--bands", help="bands to pack, comma-separated (default 40m,20m)")
    sub.add_parser("ship", help="copy both packed tables into the radio mod")
    args = parser.parse_args(argv)
    return {"run": run, "pack": pack, "show": show, "ground": ground, "ship": ship}[args.command](args)


if __name__ == "__main__":
    sys.exit(main())
