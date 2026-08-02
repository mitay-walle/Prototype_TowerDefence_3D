#!/usr/bin/env python3
"""Search Freesound candidates from rough keywords with hard download limits."""

from __future__ import annotations

import argparse
import csv
import itertools
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path
from typing import Iterable

API_ROOT = "https://freesound.org/apiv2"
DEFAULT_SECRET_DIR = Path.home() / ".codex" / "secrets"
DEFAULT_AUTH_PATH = DEFAULT_SECRET_DIR / "freesound_auth.txt"
DEFAULT_TOKEN_PATH = DEFAULT_SECRET_DIR / "freesound_token.json"
DEFAULT_FIELDS = [
    "id",
    "name",
    "url",
    "username",
    "license",
    "type",
    "duration",
    "filesize",
    "num_downloads",
    "avg_rating",
    "num_ratings",
    "tags",
    "previews",
]


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def load_api_key(auth_path: Path) -> str | None:
    if not auth_path.exists():
        return None

    text = read_text(auth_path).strip()
    if not text:
        return None

    if text.startswith("{"):
        data = json.loads(text)
        value = data.get("api_key") or data.get("client_secret") or data.get("clientSecret")
        return str(value).strip() if value else None

    values: dict[str, str] = {}
    positional: list[str] = []
    for raw_line in text.splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        if "=" in line:
            key, value = line.split("=", 1)
            values[key.strip().lower().replace("-", "_")] = value.strip()
        elif ":" in line and not line.lower().startswith("http"):
            key, value = line.split(":", 1)
            values[key.strip().lower().replace("-", "_")] = value.strip()
        else:
            positional.append(line)

    for key in ("api_key", "client_secret", "clientsecret"):
        if values.get(key):
            return values[key]
    if len(positional) >= 2:
        return positional[1]
    if len(positional) == 1 and len(positional[0]) >= 30:
        return positional[0]
    return None


def load_bearer_token(token_path: Path) -> str | None:
    if not token_path.exists():
        return None
    try:
        token = json.loads(read_text(token_path))
    except json.JSONDecodeError:
        return None
    value = token.get("access_token")
    return str(value).strip() if value else None


def auth_headers(auth_path: Path, token_path: Path) -> dict[str, str]:
    bearer = load_bearer_token(token_path)
    if bearer:
        return {"Authorization": f"Bearer {bearer}"}
    api_key = load_api_key(auth_path)
    if api_key:
        return {"Authorization": f"Token {api_key}"}
    return {}


def parse_size(value: str) -> int:
    text = value.strip().lower().replace(" ", "")
    match = re.fullmatch(r"([0-9]+(?:\.[0-9]+)?)(b|kb|kib|mb|mib|gb|gib)?", text)
    if not match:
        raise argparse.ArgumentTypeError(f"Invalid size: {value}")
    number = float(match.group(1))
    unit = match.group(2) or "b"
    scale = {
        "b": 1,
        "kb": 1000,
        "kib": 1024,
        "mb": 1000**2,
        "mib": 1024**2,
        "gb": 1000**3,
        "gib": 1024**3,
    }[unit]
    return int(number * scale)


def tokenize(parts: Iterable[str]) -> list[str]:
    raw = " ".join(parts).lower()
    tokens = re.findall(r"[a-z0-9][a-z0-9_-]*", raw)
    stop = {"a", "an", "and", "or", "the", "of", "for", "to", "sfx", "sound", "sounds"}
    result: list[str] = []
    seen: set[str] = set()
    for token in tokens:
        token = token.strip("_-")
        if len(token) < 2 or token in stop or token in seen:
            continue
        seen.add(token)
        result.append(token)
    return result


def query_variants(tokens: list[str], max_variants: int) -> list[str]:
    if not tokens:
        return [""]

    variants: list[str] = []
    full = " ".join(tokens)
    variants.append(full)

    if len(tokens) > 1:
        variants.append(" ".join(f'+{token}' for token in tokens))

    tag_like = [f"tag:{token}" for token in tokens if len(token) >= 3]
    if tag_like:
        variants.append(" ".join(tag_like))

    for width in range(min(3, len(tokens)), 0, -1):
        for combo in itertools.combinations(tokens, width):
            variants.append(" ".join(combo))
            if len(variants) >= max_variants:
                break
        if len(variants) >= max_variants:
            break

    clean: list[str] = []
    seen: set[str] = set()
    for variant in variants:
        if variant not in seen:
            seen.add(variant)
            clean.append(variant)
    return clean[:max_variants]


def build_filter(args: argparse.Namespace) -> str:
    filters = [f"duration:[* TO {args.max_duration}]", f"filesize:[* TO {args.max_size_bytes}]"]
    if args.types:
        types = " OR ".join(t.lower().lstrip(".") for t in args.types)
        filters.append(f"type:({types})")
    if args.licenses:
        licenses = " OR ".join(f'"{license_name}"' if " " in license_name else license_name for license_name in args.licenses)
        filters.append(f"license:({licenses})")
    if args.exclude_explicit:
        filters.append("is_explicit:false")
    return " ".join(filters)


def request_json(url: str, headers: dict[str, str]) -> dict:
    request = urllib.request.Request(url, headers={"Accept": "application/json", **headers})
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise SystemExit(f"Freesound search failed with HTTP {exc.code}: {detail}") from exc


def search(query: str, args: argparse.Namespace, headers: dict[str, str]) -> list[dict]:
    params = {
        "query": query,
        "filter": build_filter(args),
        "sort": "downloads_desc",
        "fields": ",".join(DEFAULT_FIELDS),
        "page_size": str(args.page_size),
        "page": "1",
    }
    url = f"{API_ROOT}/search/?{urllib.parse.urlencode(params)}"
    data = request_json(url, headers)
    return data.get("results") or []


def approved(item: dict, args: argparse.Namespace) -> bool:
    duration = float(item.get("duration") or 0)
    filesize = int(item.get("filesize") or 0)
    return duration <= args.max_duration and filesize <= args.max_size_bytes


def normalize_result(item: dict, query: str, args: argparse.Namespace) -> dict:
    duration = float(item.get("duration") or 0)
    filesize = int(item.get("filesize") or 0)
    return {
        "id": item.get("id"),
        "name": item.get("name"),
        "url": item.get("url"),
        "username": item.get("username"),
        "license": item.get("license"),
        "type": item.get("type"),
        "duration": duration,
        "filesize": filesize,
        "filesize_mb": round(filesize / (1000 * 1000), 2),
        "num_downloads": int(item.get("num_downloads") or 0),
        "avg_rating": item.get("avg_rating"),
        "num_ratings": item.get("num_ratings"),
        "tags": item.get("tags") or [],
        "preview_hq_mp3": (item.get("previews") or {}).get("preview-hq-mp3"),
        "matched_query": query,
        "approved_for_download": approved(item, args),
        "reject_reason": reject_reason(duration, filesize, args),
    }


def reject_reason(duration: float, filesize: int, args: argparse.Namespace) -> str | None:
    reasons: list[str] = []
    if duration > args.max_duration:
        reasons.append(f"duration {duration:.2f}s > {args.max_duration:.2f}s")
    if filesize > args.max_size_bytes:
        reasons.append(f"filesize {filesize} > {args.max_size_bytes}")
    return "; ".join(reasons) if reasons else None


def collect(args: argparse.Namespace) -> list[dict]:
    tokens = tokenize(args.keywords)
    variants = query_variants(tokens, args.variants)
    headers = auth_headers(args.auth, args.token)
    seen: set[int] = set()
    results: list[dict] = []

    for variant in variants:
        for item in search(variant, args, headers):
            sound_id = item.get("id")
            if sound_id in seen:
                continue
            seen.add(sound_id)
            results.append(normalize_result(item, variant, args))
            if len(results) >= args.limit:
                return sorted(results, key=lambda x: x["num_downloads"], reverse=True)

    return sorted(results, key=lambda x: x["num_downloads"], reverse=True)


def write_csv(results: list[dict], path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fields = [
        "approved_for_download",
        "id",
        "name",
        "url",
        "license",
        "type",
        "duration",
        "filesize_mb",
        "num_downloads",
        "avg_rating",
        "num_ratings",
        "matched_query",
        "reject_reason",
        "preview_hq_mp3",
    ]
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields)
        writer.writeheader()
        for result in results:
            writer.writerow({field: result.get(field) for field in fields})


def print_table(results: list[dict]) -> None:
    for index, item in enumerate(results, 1):
        status = "OK" if item["approved_for_download"] else "NO"
        print(
            f"{index:02d}. [{status}] {item['id']} | {item['num_downloads']} dl | "
            f"{item['duration']:.1f}s | {item['filesize_mb']:.1f} MB | {item['type']} | {item['name']}"
        )
        print(f"    {item['url']}")
        if item["reject_reason"]:
            print(f"    reject: {item['reject_reason']}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Search Freesound with rough keywords and hard download limits.")
    parser.add_argument("keywords", nargs="+", help="Rough keywords/fragments, for example: sea bubbles bells")
    parser.add_argument("--max-duration", type=float, default=60.0, help="Maximum allowed duration in seconds")
    parser.add_argument("--max-size", default="20MB", help="Maximum allowed original file size, e.g. 20MB or 5000KB")
    parser.add_argument("--limit", type=int, default=20, help="Maximum unique results to return")
    parser.add_argument("--page-size", type=int, default=50, help="Freesound page size per query variant")
    parser.add_argument("--variants", type=int, default=8, help="Maximum query variants to try for rough keywords")
    parser.add_argument("--types", nargs="*", default=None, help="Allowed original formats, e.g. wav mp3 ogg")
    parser.add_argument("--licenses", nargs="*", default=None, help="Allowed licenses, e.g. 'Creative Commons 0'")
    parser.add_argument("--exclude-explicit", action="store_true", help="Filter out explicit sounds")
    parser.add_argument("--auth", type=Path, default=Path(os.environ.get("FREESOUND_AUTH_FILE", DEFAULT_AUTH_PATH)))
    parser.add_argument("--token", type=Path, default=Path(os.environ.get("FREESOUND_TOKEN_FILE", DEFAULT_TOKEN_PATH)))
    parser.add_argument("--json", type=Path, help="Write full JSON results to this path")
    parser.add_argument("--csv", type=Path, help="Write compact CSV results to this path")
    args = parser.parse_args()
    args.max_size_bytes = parse_size(args.max_size)

    results = collect(args)
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(results, indent=2, ensure_ascii=False), encoding="utf-8")
    if args.csv:
        write_csv(results, args.csv)

    print_table(results)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
