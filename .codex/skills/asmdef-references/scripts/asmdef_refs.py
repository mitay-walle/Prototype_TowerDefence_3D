#!/usr/bin/env python3
import argparse
import codecs
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path


SKIP_DIRS = {".git", ".idea", ".vs", "Library", "Temp", "Logs", "obj", "Build", "Builds"}


@dataclass
class Asmdef:
    path: Path
    name: str
    references: list[str]
    include_platforms: list[str]
    guid: str | None
    data: dict
    text: str
    eol: str
    had_bom: bool
    final_newline: bool

    @property
    def is_editor_only(self) -> bool:
        return "Editor" in self.include_platforms


def detect_eol(text: str) -> str:
    crlf = text.count("\r\n")
    lf_only = len(re.findall(r"(?<!\r)\n", text))
    return "\r\n" if crlf >= lf_only and crlf > 0 else "\n"


def has_final_newline(text: str) -> bool:
    return text.endswith(("\r\n", "\n", "\r"))


def read_text(path: Path) -> tuple[str, bool]:
    raw = path.read_bytes()
    had_bom = raw.startswith(codecs.BOM_UTF8)
    return raw.decode("utf-8-sig"), had_bom


def apply_text_convention(text: str, eol: str, final_newline: bool) -> str:
    normalized = re.sub(r"\r\n|\r|\n", "\n", text)
    if final_newline:
        if not normalized.endswith("\n"):
            normalized += "\n"
    else:
        normalized = normalized.rstrip("\n")
    return normalized.replace("\n", eol)


def write_text(path: Path, text: str, had_bom: bool, eol: str, final_newline: bool) -> None:
    formatted = apply_text_convention(text, eol, final_newline)
    raw = formatted.encode("utf-8")
    if had_bom:
        raw = codecs.BOM_UTF8 + raw
    path.write_bytes(raw)


def normalize_path(path: Path) -> str:
    return path.as_posix()


def read_guid(asmdef_path: Path) -> str | None:
    meta = asmdef_path.with_suffix(asmdef_path.suffix + ".meta")
    if not meta.exists():
        return None
    for line in meta.read_text(encoding="utf-8", errors="ignore").splitlines():
        if line.startswith("guid:"):
            return line.split(":", 1)[1].strip()
    return None


def iter_asmdef_paths(project_root: Path):
    for path in project_root.rglob("*.asmdef"):
        relative_parts = path.relative_to(project_root).parts
        if any(part in SKIP_DIRS for part in relative_parts):
            continue
        yield path


def load_asmdefs(project_root: Path) -> list[Asmdef]:
    asmdefs: list[Asmdef] = []
    for path in iter_asmdef_paths(project_root):
        text, had_bom = read_text(path)
        try:
            data = json.loads(text)
        except json.JSONDecodeError as exc:
            raise SystemExit(f"Invalid JSON in {path}: {exc}") from exc
        name = data.get("name")
        if not isinstance(name, str) or not name:
            raise SystemExit(f"Asmdef has no valid name: {path}")
        refs = data.get("references", [])
        if refs is None:
            refs = []
        if not isinstance(refs, list) or not all(isinstance(item, str) for item in refs):
            raise SystemExit(f"Asmdef references must be a string array: {path}")
        include = data.get("includePlatforms", [])
        if include is None:
            include = []
        if not isinstance(include, list):
            include = []
        asmdefs.append(
            Asmdef(
                path=path,
                name=name,
                references=list(refs),
                include_platforms=[str(item) for item in include],
                guid=read_guid(path),
                data=data,
                text=text,
                eol=detect_eol(text),
                had_bom=had_bom,
                final_newline=has_final_newline(text),
            )
        )
    return asmdefs


def build_indexes(asmdefs: list[Asmdef]):
    by_name: dict[str, list[Asmdef]] = {}
    by_guid: dict[str, Asmdef] = {}
    by_path: dict[str, Asmdef] = {}
    for asmdef in asmdefs:
        by_name.setdefault(asmdef.name, []).append(asmdef)
        if asmdef.guid:
            by_guid[asmdef.guid.lower()] = asmdef
        by_path[normalize_path(asmdef.path).lower()] = asmdef
        by_path[normalize_path(asmdef.path.resolve()).lower()] = asmdef
    return by_name, by_guid, by_path


def resolve_asmdef(token: str, asmdefs: list[Asmdef], project_root: Path) -> Asmdef:
    by_name, by_guid, by_path = build_indexes(asmdefs)
    raw = token.removeprefix("GUID:")
    guid_match = by_guid.get(raw.lower())
    if guid_match:
        return guid_match

    path = Path(token)
    candidates: list[Asmdef] = []
    if path.suffix == ".asmdef" or "\\" in token or "/" in token:
        full = path if path.is_absolute() else project_root / path
        found = by_path.get(normalize_path(full).lower()) or by_path.get(normalize_path(full.resolve()).lower())
        if found:
            return found
        candidates = [item for item in asmdefs if normalize_path(item.path).lower().endswith(normalize_path(path).lower())]
    else:
        candidates = by_name.get(token, [])

    if len(candidates) == 1:
        return candidates[0]
    if not candidates:
        raise SystemExit(f"Could not resolve asmdef: {token}")
    choices = "\n".join(f"  - {item.name}: {normalize_path(item.path)}" for item in candidates)
    raise SystemExit(f"Ambiguous asmdef '{token}'. Use a path or GUID:\n{choices}")


def owner_for_file(file_path: Path, asmdefs: list[Asmdef], project_root: Path) -> Asmdef | None:
    full = file_path if file_path.is_absolute() else project_root / file_path
    full = full.resolve()
    best: Asmdef | None = None
    best_len = -1
    for asmdef in asmdefs:
        parent = asmdef.path.resolve().parent
        try:
            full.relative_to(parent)
        except ValueError:
            continue
        length = len(parent.parts)
        if length > best_len:
            best = asmdef
            best_len = length
    return best


def ref_points_to(ref: str, asmdef: Asmdef) -> bool:
    if ref == asmdef.name:
        return True
    return bool(asmdef.guid and ref.lower() == f"guid:{asmdef.guid}".lower())


def choose_ref_value(target: Asmdef, referenced: Asmdef, style: str) -> str:
    if style == "auto":
        style = "guid" if any(ref.startswith("GUID:") for ref in target.references) else "name"
    if style == "guid":
        if not referenced.guid:
            raise SystemExit(f"Cannot use GUID reference because {referenced.path} has no .meta guid")
        return f"GUID:{referenced.guid}"
    return referenced.name


def resolved_ref_name(ref: str, asmdefs: list[Asmdef]) -> str | None:
    for asmdef in asmdefs:
        if ref_points_to(ref, asmdef):
            return asmdef.name
    return None


def graph_with_refs(asmdefs: list[Asmdef], target: Asmdef, new_refs: list[str]) -> dict[str, set[str]]:
    graph: dict[str, set[str]] = {}
    for asmdef in asmdefs:
        refs = list(asmdef.references)
        if asmdef.name == target.name:
            refs = new_refs
        graph[asmdef.name] = {name for ref in refs if (name := resolved_ref_name(ref, asmdefs))}
    return graph


def path_exists(graph: dict[str, set[str]], start: str, goal: str) -> bool:
    stack = [start]
    seen: set[str] = set()
    while stack:
        node = stack.pop()
        if node == goal:
            return True
        if node in seen:
            continue
        seen.add(node)
        stack.extend(graph.get(node, set()) - seen)
    return False


def find_references_array_span(text: str) -> tuple[int, int] | None:
    match = re.search(r'"references"\s*:', text)
    if not match:
        return None
    i = match.end()
    while i < len(text) and text[i].isspace():
        i += 1
    if i >= len(text) or text[i] != "[":
        return None
    start = i
    depth = 0
    in_string = False
    escaped = False
    while i < len(text):
        char = text[i]
        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
        else:
            if char == '"':
                in_string = True
            elif char == "[":
                depth += 1
            elif char == "]":
                depth -= 1
                if depth == 0:
                    return start, i + 1
        i += 1
    return None


def format_references_array(original_text: str, span: tuple[int, int], refs: list[str], eol: str) -> str:
    start, end = span
    original = original_text[start:end]
    line_start = original_text.rfind("\n", 0, start) + 1
    key_line = original_text[line_start:start]
    key_indent = re.match(r"[ \t]*", key_line).group(0)
    item_indent = key_indent + "    "

    if not refs:
        return "[]"

    use_multiline = "\n" in original or "\r" in original or len(refs) > 2 or original.strip() == "[]"
    if not use_multiline:
        return "[" + ",".join(json.dumps(ref, ensure_ascii=False) for ref in refs) + "]"

    lines = ["["]
    for index, ref in enumerate(refs):
        comma = "," if index < len(refs) - 1 else ""
        lines.append(f"{item_indent}{json.dumps(ref, ensure_ascii=False)}{comma}")
    lines.append(f"{key_indent}]")
    return eol.join(lines)


def update_references_text(asmdef: Asmdef, refs: list[str]) -> str:
    data = dict(asmdef.data)
    data["references"] = refs
    span = find_references_array_span(asmdef.text)
    if span:
        updated = asmdef.text[: span[0]] + format_references_array(asmdef.text, span, refs, asmdef.eol) + asmdef.text[span[1] :]
    else:
        updated = json.dumps(data, indent=4, ensure_ascii=False)

    return updated


def cmd_list(args) -> int:
    root = Path(args.project_root).resolve()
    asmdefs = load_asmdefs(root)
    rows = [
        {
            "name": item.name,
            "path": normalize_path(item.path.relative_to(root)),
            "guid": item.guid,
            "references": item.references,
            "editorOnly": item.is_editor_only,
        }
        for item in sorted(asmdefs, key=lambda x: x.name.lower())
    ]
    if args.json:
        print(json.dumps(rows, indent=2, ensure_ascii=False))
    else:
        for row in rows:
            marker = " [Editor]" if row["editorOnly"] else ""
            print(f"{row['name']}{marker} :: {row['path']}")
    return 0


def cmd_owner(args) -> int:
    root = Path(args.project_root).resolve()
    asmdefs = load_asmdefs(root)
    owner = owner_for_file(Path(args.file), asmdefs, root)
    if not owner:
        print("No owning asmdef found")
        return 2
    result = {
        "name": owner.name,
        "path": normalize_path(owner.path.relative_to(root)),
        "guid": owner.guid,
        "references": owner.references,
        "editorOnly": owner.is_editor_only,
    }
    if args.json:
        print(json.dumps(result, indent=2, ensure_ascii=False))
    else:
        print(f"{result['name']} :: {result['path']}")
    return 0


def cmd_add(args) -> int:
    root = Path(args.project_root).resolve()
    asmdefs = load_asmdefs(root)
    target = resolve_asmdef(args.target, asmdefs, root)
    refs = list(target.references)
    added: list[str] = []
    skipped: list[str] = []
    errors: list[str] = []

    for token in args.reference:
        referenced = resolve_asmdef(token, asmdefs, root)
        if referenced.name == target.name:
            errors.append(f"Cannot add self-reference: {target.name}")
            continue
        if referenced.is_editor_only and not target.is_editor_only and not args.allow_editor_reference:
            errors.append(
                f"Refusing runtime-to-Editor reference: {target.name} -> {referenced.name}. "
                "Move editor code to an Editor assembly or pass --allow-editor-reference."
            )
            continue
        if any(ref_points_to(ref, referenced) for ref in refs):
            skipped.append(referenced.name)
            continue

        ref_value = choose_ref_value(target, referenced, args.style)
        candidate_refs = refs + [ref_value]
        graph = graph_with_refs(asmdefs, target, candidate_refs)
        if not args.allow_cycle and path_exists(graph, referenced.name, target.name):
            errors.append(f"Refusing circular reference: {target.name} -> {referenced.name}")
            continue

        refs.append(ref_value)
        added.append(ref_value)

    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1

    result = {
        "target": target.name,
        "targetPath": normalize_path(target.path.relative_to(root)),
        "added": added,
        "skippedExisting": skipped,
        "applied": bool(args.apply),
    }

    if args.apply and added:
        write_text(target.path, update_references_text(target, refs), target.had_bom, target.eol, target.final_newline)

    if args.json:
        print(json.dumps(result, indent=2, ensure_ascii=False))
    else:
        mode = "APPLIED" if args.apply else "DRY RUN"
        print(f"{mode}: {target.name} :: {result['targetPath']}")
        if added:
            for ref in added:
                print(f"  + {ref}")
        else:
            print("  no new references")
        for item in skipped:
            print(f"  = already referenced: {item}")
        if not args.apply and added:
            print("Run again with --apply to write changes.")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Inspect and update Unity .asmdef references.")
    parser.add_argument("--project-root", default=".", help="Unity project root. Defaults to current directory.")
    sub = parser.add_subparsers(dest="command", required=True)

    list_parser = sub.add_parser("list", help="List asmdefs in the project.")
    list_parser.add_argument("--json", action="store_true")
    list_parser.set_defaults(func=cmd_list)

    owner_parser = sub.add_parser("owner", help="Find the closest owning asmdef for a file.")
    owner_parser.add_argument("--file", required=True)
    owner_parser.add_argument("--json", action="store_true")
    owner_parser.set_defaults(func=cmd_owner)

    add_parser = sub.add_parser("add", help="Add references to a target asmdef.")
    add_parser.add_argument("--target", required=True, help="Target asmdef name, GUID, or path.")
    add_parser.add_argument("--reference", required=True, action="append", help="Referenced asmdef name, GUID, or path.")
    add_parser.add_argument("--style", choices=["auto", "name", "guid"], default="auto")
    add_parser.add_argument("--apply", action="store_true", help="Write changes. Without this, only print a dry run.")
    add_parser.add_argument("--allow-editor-reference", action="store_true")
    add_parser.add_argument("--allow-cycle", action="store_true")
    add_parser.add_argument("--json", action="store_true")
    add_parser.set_defaults(func=cmd_add)

    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
