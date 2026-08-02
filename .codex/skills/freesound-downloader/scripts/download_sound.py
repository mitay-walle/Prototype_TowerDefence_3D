#!/usr/bin/env python3
"""Download original Freesound audio using OAuth2 credentials stored outside a repo."""

from __future__ import annotations

import argparse
import hashlib
import json
import mimetypes
import os
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

API_ROOT = "https://freesound.org/apiv2"
DEFAULT_SECRET_DIR = Path.home() / ".codex" / "secrets"
DEFAULT_AUTH_PATH = DEFAULT_SECRET_DIR / "freesound_auth.txt"
DEFAULT_TOKEN_PATH = DEFAULT_SECRET_DIR / "freesound_token.json"


def _read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def load_auth(path: Path) -> tuple[str, str]:
    if not path.exists():
        raise SystemExit(f"Auth file not found: {path}")

    text = _read_text(path).strip()
    if not text:
        raise SystemExit(f"Auth file is empty: {path}")

    if text.startswith("{"):
        data = json.loads(text)
        client_id = data.get("client_id") or data.get("clientId")
        client_secret = data.get("client_secret") or data.get("clientSecret") or data.get("api_key")
        if client_id and client_secret:
            return str(client_id).strip(), str(client_secret).strip()

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

    client_id = values.get("client_id") or values.get("clientid")
    client_secret = values.get("client_secret") or values.get("clientsecret") or values.get("api_key")
    if client_id and client_secret:
        return client_id, client_secret
    if len(positional) >= 2:
        return positional[0], positional[1]

    raise SystemExit("Auth file must contain client_id and client_secret as JSON, key=value, or two non-empty lines.")


def load_token(path: Path) -> dict:
    if not path.exists():
        return {}
    return json.loads(_read_text(path))


def save_token(path: Path, token: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    token = dict(token)
    if "expires_in" in token:
        try:
            token["expires_at"] = int(time.time()) + int(token["expires_in"])
        except (TypeError, ValueError):
            pass
    path.write_text(json.dumps(token, indent=2, sort_keys=True), encoding="utf-8")


def post_token(client_id: str, client_secret: str, data: dict[str, str]) -> dict:
    body = urllib.parse.urlencode({"client_id": client_id, "client_secret": client_secret, **data}).encode("utf-8")
    request = urllib.request.Request(
        f"{API_ROOT}/oauth2/access_token/",
        data=body,
        headers={"Content-Type": "application/x-www-form-urlencoded", "Accept": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise SystemExit(f"Token request failed with HTTP {exc.code}: {detail}") from exc


def exchange_code(client_id: str, client_secret: str, code: str) -> dict:
    return post_token(client_id, client_secret, {"grant_type": "authorization_code", "code": code})


def refresh_token(client_id: str, client_secret: str, refresh: str) -> dict:
    return post_token(client_id, client_secret, {"grant_type": "refresh_token", "refresh_token": refresh})


def ensure_access_token(auth_path: Path, token_path: Path, code: str | None) -> str:
    client_id, client_secret = load_auth(auth_path)
    token = load_token(token_path)

    if code:
        token = exchange_code(client_id, client_secret, code)
        save_token(token_path, token)
        return str(token["access_token"])

    expires_at = int(token.get("expires_at") or 0)
    if token.get("access_token") and (not expires_at or expires_at - time.time() > 120):
        return str(token["access_token"])

    refresh = token.get("refresh_token")
    if not refresh:
        authorize_url = f"{API_ROOT}/oauth2/authorize/?client_id={urllib.parse.quote(client_id)}&response_type=code"
        raise SystemExit(
            "No valid Freesound access token or refresh token. "
            f"Open this URL, authorize access, then rerun with --code CODE: {authorize_url}"
        )

    token = refresh_token(client_id, client_secret, str(refresh))
    save_token(token_path, token)
    return str(token["access_token"])


def api_get_json(url: str, access_token: str) -> dict:
    request = urllib.request.Request(url, headers={"Authorization": f"Bearer {access_token}", "Accept": "application/json"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.loads(response.read().decode("utf-8"))


def sanitize_filename(value: str) -> str:
    value = re.sub(r"[<>:\\|?*\x00-\x1f]", "-", value)
    value = re.sub(r"\s+", " ", value).strip().strip(".")
    return value or "freesound-audio"


def filename_from_headers(headers, fallback: str) -> str:
    disposition = headers.get("Content-Disposition") or headers.get("content-disposition") or ""
    match = re.search(r"filename\*=UTF-8''([^;]+)", disposition, flags=re.IGNORECASE)
    if match:
        return sanitize_filename(urllib.parse.unquote(match.group(1)))
    match = re.search(r'filename="?([^";]+)"?', disposition, flags=re.IGNORECASE)
    if match:
        return sanitize_filename(match.group(1))
    return fallback


def extension_from_type(content_type: str | None, fallback: str = ".wav") -> str:
    if not content_type:
        return fallback
    media_type = content_type.split(";", 1)[0].strip().lower()
    return mimetypes.guess_extension(media_type) or fallback


def download(sound_id: str, output_dir: Path, access_token: str, overwrite: bool) -> dict:
    metadata = api_get_json(f"{API_ROOT}/sounds/{sound_id}/", access_token)
    base_name = sanitize_filename(f"{metadata.get('id', sound_id)}__{metadata.get('username', 'freesound')}__{metadata.get('name', 'sound')}")

    request = urllib.request.Request(
        f"{API_ROOT}/sounds/{sound_id}/download/",
        headers={"Authorization": f"Bearer {access_token}"},
    )
    output_dir.mkdir(parents=True, exist_ok=True)
    try:
        with urllib.request.urlopen(request, timeout=300) as response:
            fallback = base_name + extension_from_type(response.headers.get("Content-Type"))
            filename = filename_from_headers(response.headers, fallback)
            target = output_dir / filename
            if target.exists() and not overwrite:
                raise SystemExit(f"Target already exists: {target} (pass --overwrite to replace it)")
            temp = target.with_suffix(target.suffix + ".download")
            digest = hashlib.sha256()
            size = 0
            with temp.open("wb") as handle:
                while True:
                    chunk = response.read(1024 * 1024)
                    if not chunk:
                        break
                    handle.write(chunk)
                    digest.update(chunk)
                    size += len(chunk)
            temp.replace(target)
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise SystemExit(f"Download failed with HTTP {exc.code}: {detail}") from exc

    sidecar = target.with_suffix(target.suffix + ".freesound.json")
    sidecar_data = {
        "id": metadata.get("id"),
        "name": metadata.get("name"),
        "username": metadata.get("username"),
        "license": metadata.get("license"),
        "url": metadata.get("url"),
        "duration": metadata.get("duration"),
        "samplerate": metadata.get("samplerate"),
        "bitdepth": metadata.get("bitdepth"),
        "channels": metadata.get("channels"),
        "sha256": digest.hexdigest().upper(),
        "bytes": size,
        "downloaded_at_unix": int(time.time()),
    }
    sidecar.write_text(json.dumps(sidecar_data, indent=2, sort_keys=True), encoding="utf-8")

    return {"path": str(target), "sidecar": str(sidecar), "bytes": size, "sha256": digest.hexdigest().upper(), "license": metadata.get("license"), "url": metadata.get("url")}


def main() -> int:
    parser = argparse.ArgumentParser(description="Download original Freesound audio through OAuth2.")
    parser.add_argument("sound_id", help="Freesound sound id, for example 861596")
    parser.add_argument("--output-dir", required=True, help="Destination folder for the audio file")
    parser.add_argument("--auth", type=Path, default=Path(os.environ.get("FREESOUND_AUTH_FILE", DEFAULT_AUTH_PATH)))
    parser.add_argument("--token", type=Path, default=Path(os.environ.get("FREESOUND_TOKEN_FILE", DEFAULT_TOKEN_PATH)))
    parser.add_argument("--code", help="One-time OAuth authorization code to exchange before downloading")
    parser.add_argument("--overwrite", action="store_true", help="Replace an existing downloaded file")
    args = parser.parse_args()

    access_token = ensure_access_token(args.auth, args.token, args.code)
    try:
        result = download(args.sound_id, Path(args.output_dir), access_token, args.overwrite)
    except SystemExit as exc:
        if "HTTP 401" not in str(exc):
            raise
        client_id, client_secret = load_auth(args.auth)
        token = load_token(args.token)
        refresh = token.get("refresh_token")
        if not refresh:
            raise
        token = refresh_token(client_id, client_secret, str(refresh))
        save_token(args.token, token)
        result = download(args.sound_id, Path(args.output_dir), str(token["access_token"]), args.overwrite)

    print(json.dumps(result, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
