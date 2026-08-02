#!/usr/bin/env python3
import argparse
import codecs
import json
import re
import subprocess
import sys
from pathlib import Path


MOJIBAKE_MARKERS = (
    "\ufffd",
    "\u00d0",
    "\u00d1",
    "\u0420\u045f",
    "\u0420\u0452",
    "\u0420\u2018",
    "\u0420\u00b0",
    "\u0420\u00b5",
    "\u0420\u0451",
    "\u0420\u0455",
    "\u0420\u0406",
    "\u0420\u0405",
    "\u0420\u0454",
    "\u0421\u0402",
    "\u0421\u0403",
    "\u0421\u201a",
    "\u0421\u040a",
    "\u0421\u040f",
)


def decode_text(path: Path) -> tuple[str, bytes, bool]:
    raw = path.read_bytes()
    try:
        text = raw.decode("utf-8-sig")
    except UnicodeDecodeError as exc:
        raise SystemExit(f"Not UTF-8 text, refusing to inspect: {path}") from exc
    return text, raw, raw.startswith(codecs.BOM_UTF8)


def detect_eol(text: str) -> str:
    crlf = text.count("\r\n")
    lf = len(re.findall(r"(?<!\r)\n", text))
    cr = len(re.findall(r"\r(?!\n)", text))
    present = sum(1 for count in (crlf, lf, cr) if count > 0)
    if present == 0:
        return "none"
    if present > 1:
        return "mixed"
    if crlf:
        return "crlf"
    if lf:
        return "lf"
    return "cr"


def final_newline(raw: bytes) -> bool:
    return raw.endswith(b"\n") or raw.endswith(b"\r")


def unicode_info(text: str) -> dict:
    markers = [marker for marker in MOJIBAKE_MARKERS if marker in text]
    return {
        "has_non_ascii": any(ord(char) > 127 for char in text),
        "has_cyrillic": bool(re.search(r"[\u0400-\u04ff]", text)),
        "has_replacement_char": "\ufffd" in text,
        "suspicious_mojibake_markers": markers,
    }


def display_marker(marker: str) -> str:
    return marker.encode("unicode_escape").decode("ascii")


def inspect_file(path: Path) -> dict:
    text, raw, has_bom = decode_text(path)
    result = {
        "path": str(path),
        "exists": True,
        "eol": detect_eol(text),
        "bom": has_bom,
        "final_newline": final_newline(raw),
        "valid_utf8": True,
    }
    result.update(unicode_info(text))
    return result


def eol_string(eol: str) -> str | None:
    if eol == "crlf":
        return "\r\n"
    if eol == "lf":
        return "\n"
    if eol == "cr":
        return "\r"
    return None


def normalize_text(text: str, eol: str, want_final_newline: bool | None) -> str:
    target_eol = eol_string(eol)
    if target_eol is not None:
        text = re.sub(r"\r\n|\r|\n", "\n", text)
        text = text.replace("\n", target_eol)

    if want_final_newline is True:
        if not (text.endswith("\n") or text.endswith("\r")):
            text += target_eol or "\n"
    elif want_final_newline is False:
        text = text.rstrip("\r\n")

    return text


def write_text(path: Path, text: str, bom: bool) -> None:
    raw = text.encode("utf-8")
    if bom:
        raw = codecs.BOM_UTF8 + raw
    path.write_bytes(raw)


def default_conventions(path: Path) -> dict:
    suffix = path.suffix.lower()
    return {
        "eol": "lf",
        "bom": False,
        "final_newline": suffix != ".cs",
    }


def write_eol(eol: str, path: Path) -> str:
    if eol in ("crlf", "lf", "cr"):
        return eol
    return default_conventions(path)["eol"]


def read_content(path: Path) -> str:
    text, _, _ = decode_text(path)
    return text


def conventions_for_write(target: Path, like: Path | None, eol: str, bom: str, final_newline_value: str) -> dict:
    if like is not None:
        base = inspect_file(like)
    elif target.exists():
        base = inspect_file(target)
    else:
        base = default_conventions(target)

    return {
        "eol": write_eol(base["eol"] if eol == "keep" else eol, target),
        "bom": base["bom"] if bom == "keep" else bom == "yes",
        "final_newline": base["final_newline"] if final_newline_value == "keep" else final_newline_value == "yes",
    }


def write_with_conventions(path: Path, text: str, conventions: dict) -> bool:
    normalized = normalize_text(text, conventions["eol"], conventions["final_newline"])
    encoded = normalized.encode("utf-8")
    if conventions["bom"]:
        encoded = codecs.BOM_UTF8 + encoded
    old = path.read_bytes() if path.exists() else None
    if old == encoded:
        return False
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(encoded)
    return True


def restore_file(entry: dict) -> bool:
    path = Path(entry["path"])
    if not path.exists():
        return False
    text, raw, _ = decode_text(path)
    restored = normalize_text(text, entry["eol"], entry["final_newline"])
    before = raw
    encoded = restored.encode("utf-8")
    if entry["bom"]:
        encoded = codecs.BOM_UTF8 + encoded
    if encoded != before:
        path.write_bytes(encoded)
        return True
    return False


def cmd_inspect(args) -> int:
    rows = [inspect_file(Path(file)) for file in args.files]
    if args.json:
        print(json.dumps(rows, indent=2, ensure_ascii=False))
    else:
        for row in rows:
            print(format_inspect_row(row))
    return 0


def format_inspect_row(row: dict) -> str:
    bom = "bom" if row["bom"] else "no-bom"
    final = "final-newline" if row["final_newline"] else "no-final-newline"
    unicode_bits = []
    if row["has_cyrillic"]:
        unicode_bits.append("cyrillic")
    elif row["has_non_ascii"]:
        unicode_bits.append("non-ascii")
    if row["suspicious_mojibake_markers"]:
        unicode_bits.append("possible-mojibake")
    unicode_suffix = ", " + ", ".join(unicode_bits) if unicode_bits else ""
    return f"{row['path']}: {row['eol']}, {bom}, {final}{unicode_suffix}"


def check_files(files: list[str], allow_mojibake: bool, allow_mixed_eol: bool) -> tuple[list[dict], list[dict]]:
    failed = []
    rows = []
    for file in files:
        path = Path(file)
        try:
            row = inspect_file(path)
        except SystemExit as exc:
            failed.append({"path": str(path), "error": str(exc)})
            continue
        rows.append(row)
        if row["eol"] == "mixed" and not allow_mixed_eol:
            failed.append({"path": str(path), "error": "contains mixed line endings"})
        if row["has_replacement_char"]:
            failed.append({"path": str(path), "error": "contains Unicode replacement character U+FFFD"})
        markers = [marker for marker in row["suspicious_mojibake_markers"] if marker != "\ufffd"]
        if markers and not allow_mojibake:
            failed.append({"path": str(path), "error": "possible mojibake markers: " + ", ".join(display_marker(marker) for marker in markers[:8])})
    return rows, failed


def print_check_result(failed: list[dict]) -> None:
    if failed:
        print("Encoding check failed:", file=sys.stderr)
        for item in failed:
            print(f"  {item['path']}: {item['error']}", file=sys.stderr)
    else:
        print("Encoding check passed.")


def run_git_diff_check(files: list[str]) -> dict:
    command = ["git", "diff", "--check", "--"] + files
    completed = subprocess.run(command, text=True, capture_output=True)
    output = completed.stdout + completed.stderr
    line_ending_warning = bool(re.search(r"will be replaced by (CRLF|LF)", output))
    return {
        "ok": completed.returncode == 0 and not line_ending_warning,
        "returncode": completed.returncode,
        "line_ending_warning": line_ending_warning,
        "stdout": completed.stdout,
        "stderr": completed.stderr,
    }


def verify_files(files: list[str], allow_mojibake: bool, allow_mixed_eol: bool, git_diff_check: bool) -> dict:
    rows, failures = check_files(files, allow_mojibake, allow_mixed_eol)
    git_result = run_git_diff_check(files) if git_diff_check else None
    return {
        "ok": not failures and (git_result is None or git_result["ok"]),
        "files": rows,
        "failures": failures,
        "git_diff_check": git_result,
    }


def print_verify_result(result: dict) -> None:
    for row in result["files"]:
        print(format_inspect_row(row))
    print_check_result(result["failures"])
    git_result = result.get("git_diff_check")
    if git_result is not None:
        if git_result["stdout"]:
            print(git_result["stdout"], end="")
        if git_result["stderr"]:
            print(git_result["stderr"], end="", file=sys.stderr)
        if git_result.get("line_ending_warning"):
            print("git line-ending warning failed verification.", file=sys.stderr)
        if git_result["ok"]:
            print("git diff --check passed.")


def cmd_check(args) -> int:
    rows, failed = check_files(args.files, args.allow_mojibake, args.allow_mixed_eol)
    result = {"ok": not failed, "files": rows, "failures": failed}
    if args.json:
        print(json.dumps(result, indent=2, ensure_ascii=False))
    else:
        print_check_result(failed)
    return 0 if not failed else 1


def cmd_verify(args) -> int:
    result = verify_files(args.files, args.allow_mojibake, args.allow_mixed_eol, args.git_diff_check)
    if args.json:
        print(json.dumps(result, indent=2, ensure_ascii=False))
    else:
        print_verify_result(result)
    return 0 if result["ok"] else 1


def cmd_snapshot(args) -> int:
    rows = []
    for file in args.files:
        path = Path(file)
        if path.exists():
            rows.append(inspect_file(path))
        else:
            rows.append({"path": str(path), "exists": False})
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(rows, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Wrote snapshot: {output}")
    return 0


def cmd_restore(args) -> int:
    snapshot = json.loads(Path(args.snapshot).read_text(encoding="utf-8"))
    changed = []
    for entry in snapshot:
        if not entry.get("exists"):
            continue
        if restore_file(entry):
            changed.append(entry["path"])
    if args.json:
        print(json.dumps({"changed": changed}, indent=2, ensure_ascii=False))
    else:
        if changed:
            print("Restored conventions:")
            for path in changed:
                print(f"  {path}")
        else:
            print("No convention changes to restore.")
    return 0


def cmd_normalize(args) -> int:
    changed = []
    bom = None if args.bom == "keep" else args.bom == "yes"
    final = None if args.final_newline == "keep" else args.final_newline == "yes"
    for file in args.files:
        path = Path(file)
        text, raw, current_bom = decode_text(path)
        target_bom = current_bom if bom is None else bom
        target_eol = detect_eol(text) if args.eol == "keep" else args.eol
        normalized = normalize_text(text, target_eol, final)
        encoded = normalized.encode("utf-8")
        if target_bom:
            encoded = codecs.BOM_UTF8 + encoded
        if encoded != raw:
            path.write_bytes(encoded)
            changed.append(str(path))
    if args.json:
        print(json.dumps({"changed": changed}, indent=2, ensure_ascii=False))
    else:
        for path in changed:
            print(path)
        print(f"Changed: {len(changed)}")
    return 0


def cmd_write(args) -> int:
    target = Path(args.file)
    like = Path(args.like) if args.like else None
    if target.exists() and args.eol == "keep":
        current = inspect_file(target)
        if current["eol"] == "mixed" and not args.allow_mixed_eol:
            raise SystemExit(f"Target has mixed line endings; refusing to preserve them: {target}")
    content = read_content(Path(args.content_file))
    conventions = conventions_for_write(target, like, args.eol, args.bom, args.final_newline)
    changed = write_with_conventions(target, content, conventions)
    result = {"changed": changed, "file": str(target), "conventions": conventions}
    if args.verify:
        result["verify"] = verify_files([str(target)], args.allow_mojibake, args.allow_mixed_eol, args.git_diff_check)
    if args.json:
        print(json.dumps(result, indent=2, ensure_ascii=False))
    else:
        state = "changed" if changed else "unchanged"
        final = "final-newline" if conventions["final_newline"] else "no-final-newline"
        bom = "bom" if conventions["bom"] else "no-bom"
        print(f"{target}: {state}; {conventions['eol']}, {bom}, {final}")
        if args.verify:
            print_verify_result(result["verify"])
    return 0 if not args.verify or result["verify"]["ok"] else 1


def cmd_replace(args) -> int:
    target = Path(args.file)
    if not target.exists():
        raise SystemExit(f"Target file does not exist: {target}")

    current = inspect_file(target)
    if current["eol"] == "mixed" and not args.allow_mixed_eol:
        raise SystemExit(f"Target has mixed line endings; refusing replacement: {target}")
    conventions = {
        "eol": write_eol(current["eol"], target),
        "bom": current["bom"],
        "final_newline": current["final_newline"],
    }
    text = read_content(target)
    old = normalize_text(read_content(Path(args.old_file)), conventions["eol"], None)
    new = normalize_text(read_content(Path(args.new_file)), conventions["eol"], None)
    if not old:
        raise SystemExit("Old snippet is empty; refusing replacement.")

    max_count = 0 if args.count == 0 else args.count
    occurrences = text.count(old)
    if occurrences == 0:
        raise SystemExit(f"Old snippet not found in {target}")
    if args.count != 0 and occurrences < args.count:
        raise SystemExit(f"Requested {args.count} replacements but found {occurrences} in {target}")

    replaced = text.replace(old, new, max_count)
    changed = write_with_conventions(target, replaced, conventions)
    result = {
        "changed": changed,
        "file": str(target),
        "occurrences": occurrences,
        "replaced": occurrences if args.count == 0 else args.count,
        "conventions": conventions,
    }
    if args.verify:
        result["verify"] = verify_files([str(target)], args.allow_mojibake, args.allow_mixed_eol, args.git_diff_check)
    if args.json:
        print(json.dumps(result, indent=2, ensure_ascii=False))
    else:
        state = "changed" if changed else "unchanged"
        print(f"{target}: {state}; replaced {result['replaced']} of {occurrences}; {conventions['eol']}")
        if args.verify:
            print_verify_result(result["verify"])
    return 0 if not args.verify or result["verify"]["ok"] else 1


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Inspect and write text files with explicit EOL/BOM/final-newline conventions.")
    sub = parser.add_subparsers(dest="command", required=True)

    inspect_parser = sub.add_parser("inspect")
    inspect_parser.add_argument("--files", nargs="+", required=True)
    inspect_parser.add_argument("--json", action="store_true")
    inspect_parser.set_defaults(func=cmd_inspect)

    check_parser = sub.add_parser("check")
    check_parser.add_argument("--files", nargs="+", required=True)
    check_parser.add_argument("--allow-mojibake", action="store_true")
    check_parser.add_argument("--allow-mixed-eol", action="store_true")
    check_parser.add_argument("--json", action="store_true")
    check_parser.set_defaults(func=cmd_check)

    verify_parser = sub.add_parser("verify")
    verify_parser.add_argument("--files", nargs="+", required=True)
    verify_parser.add_argument("--allow-mojibake", action="store_true")
    verify_parser.add_argument("--allow-mixed-eol", action="store_true")
    verify_parser.add_argument("--git-diff-check", action="store_true")
    verify_parser.add_argument("--json", action="store_true")
    verify_parser.set_defaults(func=cmd_verify)

    snapshot_parser = sub.add_parser("snapshot")
    snapshot_parser.add_argument("--output", required=True)
    snapshot_parser.add_argument("--files", nargs="+", required=True)
    snapshot_parser.set_defaults(func=cmd_snapshot)

    restore_parser = sub.add_parser("restore")
    restore_parser.add_argument("--snapshot", required=True)
    restore_parser.add_argument("--json", action="store_true")
    restore_parser.set_defaults(func=cmd_restore)

    normalize_parser = sub.add_parser("normalize")
    normalize_parser.add_argument("--eol", choices=["keep", "crlf", "lf", "cr"], default="keep")
    normalize_parser.add_argument("--bom", choices=["keep", "yes", "no"], default="keep")
    normalize_parser.add_argument("--final-newline", choices=["keep", "yes", "no"], default="keep")
    normalize_parser.add_argument("--files", nargs="+", required=True)
    normalize_parser.add_argument("--json", action="store_true")
    normalize_parser.set_defaults(func=cmd_normalize)

    write_parser = sub.add_parser("write")
    write_parser.add_argument("--file", required=True)
    write_parser.add_argument("--content-file", required=True)
    write_parser.add_argument("--like")
    write_parser.add_argument("--eol", choices=["keep", "crlf", "lf", "cr"], default="keep")
    write_parser.add_argument("--bom", choices=["keep", "yes", "no"], default="keep")
    write_parser.add_argument("--final-newline", choices=["keep", "yes", "no"], default="keep")
    write_parser.add_argument("--verify", action="store_true")
    write_parser.add_argument("--git-diff-check", action="store_true")
    write_parser.add_argument("--allow-mojibake", action="store_true")
    write_parser.add_argument("--allow-mixed-eol", action="store_true")
    write_parser.add_argument("--json", action="store_true")
    write_parser.set_defaults(func=cmd_write)

    replace_parser = sub.add_parser("replace")
    replace_parser.add_argument("--file", required=True)
    replace_parser.add_argument("--old-file", required=True)
    replace_parser.add_argument("--new-file", required=True)
    replace_parser.add_argument("--count", type=int, default=1, help="Number of replacements; 0 means replace all.")
    replace_parser.add_argument("--verify", action="store_true")
    replace_parser.add_argument("--git-diff-check", action="store_true")
    replace_parser.add_argument("--allow-mojibake", action="store_true")
    replace_parser.add_argument("--allow-mixed-eol", action="store_true")
    replace_parser.add_argument("--json", action="store_true")
    replace_parser.set_defaults(func=cmd_replace)

    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
