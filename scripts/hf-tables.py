"""The radio's HF propagation tables, from VOACAP (docs/development/radio-hf.md; the owner's cards of 28 Nov 2026).

    python scripts/hf-tables.py run          every path of the grid through voacapl in WSL, then packed
    python scripts/hf-tables.py pack         pack the results again (another set of bands, a new format)
    python scripts/hf-tables.py show         the packed file's axes and size

voacapl runs only on the machine that makes the tables, inside WSL (the owner's choice: voacapl under WSL; installed 28 Nov 2026 in
the distribution Ubuntu-24.04, run as root, its data in /root/itshfbc). Its results and the packed file go to the work folder, the
setting hfTablesPath (config/settings.json; the owner's machine keeps it on K:). Nothing here is committed: the same voacapl and grid
give the same tables, and the packed file goes into the mod only when HF's first slice in play is built. It uses the processor only,
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
from radio import hf_tables  # noqa: E402

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
    args = parser.parse_args(argv)
    return {"run": run, "pack": pack, "show": show}[args.command](args)


if __name__ == "__main__":
    sys.exit(main())
