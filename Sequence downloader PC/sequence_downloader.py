import argparse
import json
import sys
import time
import zipfile
from pathlib import Path

from pycomm3 import LogixDriver


REQUIRED_UDT_NAME = "SEQ"
REQUIRED_ARRAY_LEN = 100
DEFAULT_SETTINGS = {
    "ip": "192.168.1.11",
    "eth_slot": 1,
    "cpu_slot": 0,
    "out_dir": "output",
    "chunk_size": 20,
    "pretty_json": True,
}
RETRY_ATTEMPTS = 5
RETRY_DELAY_SEC = 3


def write_settings(settings_path: Path, settings: dict) -> None:
    settings_path.write_text(json.dumps(settings, indent=2) + "\n", encoding="utf-8")


def load_settings():
    if getattr(sys, "frozen", False):
        base_dir = Path(sys.executable).resolve().parent
    else:
        base_dir = Path(__file__).resolve().parent
    settings_path = base_dir / "settings.json"
    if not settings_path.exists():
        write_settings(settings_path, DEFAULT_SETTINGS)
        return dict(DEFAULT_SETTINGS)
    try:
        current = json.loads(settings_path.read_text(encoding="utf-8"))
    except Exception:
        write_settings(settings_path, DEFAULT_SETTINGS)
        return dict(DEFAULT_SETTINGS)
    if not isinstance(current, dict):
        write_settings(settings_path, DEFAULT_SETTINGS)
        return dict(DEFAULT_SETTINGS)
    merged = dict(DEFAULT_SETTINGS)
    merged.update(current)
    if merged != current:
        write_settings(settings_path, merged)
    return merged


def write_error_file(error_path: Path, message: str) -> None:
    error_path.parent.mkdir(parents=True, exist_ok=True)
    error_path.write_text(message.strip() + "\n", encoding="utf-8")


def safe_filename(name: str) -> str:
    bad = r'<>:"/\|?*'
    out = name
    for ch in bad:
        out = out.replace(ch, "_")
    out = out.replace("[", "_").replace("]", "_")
    out = out.strip().strip(".")
    return out or "tag"


def is_seq_100(tag_def: dict) -> bool:
    if tag_def.get("data_type_name") != REQUIRED_UDT_NAME:
        return False
    dims = tag_def.get("dimensions") or [0, 0, 0]
    while len(dims) < 3:
        dims.append(0)
    return dims[0] == REQUIRED_ARRAY_LEN and dims[1] == 0 and dims[2] == 0


def ensure_snapshot_dict(value) -> dict:
    if value is None:
        raise ValueError("value is None")
    if not isinstance(value, dict):
        raise ValueError(f"unexpected element value type {type(value).__name__} (expected dict snapshot)")
    return value


def read_seq100_elements(plc: LogixDriver, base_tag: str, chunk_size: int):
    elems = [None] * REQUIRED_ARRAY_LEN
    element_tags = [f"{base_tag}[{i}]" for i in range(REQUIRED_ARRAY_LEN)]

    for i in range(0, len(element_tags), chunk_size):
        chunk = element_tags[i:i + chunk_size]
        results = plc.read(*chunk)
        if not isinstance(results, list):
            results = [results]

        for r in results:
            if r.error:
                raise RuntimeError(f"{r.tag}: {r.error}")
            try:
                idx = int(r.tag.split("[", 1)[1].split("]", 1)[0])
            except Exception:
                raise RuntimeError(f"Could not parse index from returned tag name: {r.tag}")
            elems[idx] = ensure_snapshot_dict(r.value)

    missing = [i for i, v in enumerate(elems) if v is None]
    if missing:
        raise RuntimeError(
            f"{base_tag}: missing elements at indices {missing[:10]}" + ("..." if len(missing) > 10 else "")
        )
    return elems


def parse_args():
    parser = argparse.ArgumentParser(description="Export SEQ[100] tags to JSON and zip the results.")
    parser.add_argument("--ip", help="Controller IP address.")
    parser.add_argument("--eth-slot", type=int, help="Ethernet slot number.")
    parser.add_argument("--cpu-slot", type=int, help="Controller slot number.")
    parser.add_argument("--out-dir", help="Output directory for JSON and ZIP.")
    parser.add_argument(
        "--include-program-tags",
        action="store_true",
        help="Include program-scoped tags (Program:*.Tag).",
    )
    return parser.parse_args()


def main():
    args = parse_args()
    settings = load_settings()

    ip = args.ip or settings.get("ip")
    eth_slot = args.eth_slot if args.eth_slot is not None else settings.get("eth_slot")
    cpu_slot = args.cpu_slot if args.cpu_slot is not None else settings.get("cpu_slot")
    chunk_size = int(settings.get("chunk_size", 20))
    pretty_json = bool(settings.get("pretty_json", True))

    if not ip or eth_slot is None or cpu_slot is None:
        raise SystemExit("Missing connection settings. Set ip/eth_slot/cpu_slot in settings.json.")

    if getattr(sys, "frozen", False):
        base_dir = Path(sys.executable).resolve().parent
    else:
        base_dir = Path(__file__).resolve().parent

    settings_out = settings.get("out_dir")
    if args.out_dir:
        out_base = Path(args.out_dir)
    elif settings_out:
        out_base = (base_dir / settings_out).resolve()
    else:
        out_base = base_dir
    timestamp = time.strftime("%Y%m%d_%H%M%S")
    out_base.mkdir(parents=True, exist_ok=True)
    zip_path = out_base / f"seq_export_{timestamp}.zip"
    error_path = out_base / "EXPORT_ERROR.txt"

    path = f"{ip}/{eth_slot}/{cpu_slot}"

    exported = 0
    last_exc = None
    for attempt in range(1, RETRY_ATTEMPTS + 1):
        try:
            with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as zf:
                with LogixDriver(path) as plc:
                    if args.include_program_tags:
                        tags = plc.get_tag_list(program="*")
                    else:
                        tags = plc.get_tag_list(program=None)

                    matches = [t for t in tags if is_seq_100(t)]
                    names = sorted([t.get("tag_name", "") for t in matches if t.get("tag_name")], key=str.lower)

                    for base in names:
                        values = read_seq100_elements(plc, base, chunk_size)
                        base_name = safe_filename(base)
                        payload = {
                            "source_tag_name": base,
                            "required_definition": f"{REQUIRED_UDT_NAME} dims [{REQUIRED_ARRAY_LEN},0,0]",
                            "value": values,
                        }
                        json_text = json.dumps(payload, indent=2 if pretty_json else None)
                        zf.writestr(f"{base_name}.json", json_text)
                        exported += 1
            last_exc = None
            break
        except Exception as exc:
            last_exc = exc
            try:
                if zip_path.exists():
                    zip_path.unlink()
            except Exception:
                pass
            if attempt < RETRY_ATTEMPTS:
                time.sleep(RETRY_DELAY_SEC)

    if last_exc is not None:
        err = (
            "EXPORT FAILED\n"
            f"Time: {time.strftime('%Y-%m-%d %H:%M:%S')}\n"
            f"IP: {ip}\n"
            f"Ethernet slot: {eth_slot}\n"
            f"CPU slot: {cpu_slot}\n"
            f"Include program tags: {bool(args.include_program_tags)}\n"
            f"ZIP path: {zip_path}\n"
            f"Attempts: {RETRY_ATTEMPTS}\n"
            f"Delay seconds: {RETRY_DELAY_SEC}\n\n"
            f"{type(last_exc).__name__}: {last_exc}\n"
        )
        write_error_file(error_path, err)
        raise last_exc

    print(f"Exported {exported} tag(s).")
    print(f"ZIP file: {zip_path}")


if __name__ == "__main__":
    main()
