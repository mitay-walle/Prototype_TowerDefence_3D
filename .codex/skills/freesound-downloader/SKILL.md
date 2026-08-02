---
name: freesound-downloader
description: Download original-quality Freesound audio through the Freesound API using OAuth2 credentials stored outside the repository. Use when Codex needs to fetch Freesound originals by sound ID or Freesound URL, refresh Freesound OAuth tokens, exchange a one-time authorization code, import downloaded audio into a Unity project, or record source/license sidecar metadata for downloaded Freesound assets.
---

# Freesound Downloader

Use the bundled script for Freesound downloads. Do not ask for or print Freesound passwords, cookies, access tokens, refresh tokens, client secrets, or full auth-file contents.

## Credential Locations

Default local-only paths:

```text
C:\Users\LEGO\.codex\secrets\freesound_auth.txt
C:\Users\LEGO\.codex\secrets\freesound_token.json
```

`freesound_auth.txt` may be JSON, `key=value`, or two non-empty lines: first `client_id`, second `client_secret`. Keep both files outside repositories. If they appear inside a repo, move them to the default secret folder and add ignore rules before continuing.

The script also accepts `FREESOUND_AUTH_FILE` and `FREESOUND_TOKEN_FILE` environment variables or explicit `--auth` / `--token` arguments.

## Workflow

1. Extract the numeric sound ID from a Freesound URL such as `https://freesound.org/people/name/sounds/861596/`.
2. If the user provides a one-time OAuth code, exchange it with `--code CODE`. Authorization codes are short-lived and single-use.
3. Download originals with:

```powershell
python C:\Users\LEGO\.codex\skills\local\freesound-downloader\scripts\download_sound.py 861596 --output-dir "G:\UnityProjects\Outcasts\Assets\AudioClips\Music"
```

4. For Unity projects, call Unity MCP `refresh_unity` after downloading into `Assets/`, then verify `manage_asset get_info` reports `UnityEngine.AudioClip`.
5. Report the downloaded audio path, byte size, SHA256, license, and source URL. Do not report secrets or tokens.

The script writes a `.freesound.json` sidecar next to the audio file containing source, license, size, and SHA256 metadata. For Unity projects, keep that sidecar outside `Assets/` (for example in `Temp/` or another non-imported staging folder) unless the user explicitly asks for in-project license artifacts. When importing downloaded Freesound audio into `Assets/`, copy/import only the audio file; do not copy `.freesound.json` sidecars into the Unity project because they clutter the AssetDatabase and create unnecessary `.meta` files.

## OAuth Recovery

If the token is missing or expired and no refresh token is available, the script prints an authorization URL containing only the client ID. Ask the user to open it, authorize access, and provide only the displayed `code`; then rerun the script with `--code CODE`.

## Safety

- Keep Freesound secrets outside the workspace and out of git.
- Prefer original downloads through `GET /apiv2/sounds/<sound_id>/download/`; only use public preview files when the user explicitly accepts previews.
- Do not overwrite an existing audio file unless the user asked for replacement or `--overwrite` is appropriate for the task.
- When downloading into Unity `Assets/`, do not hand-create `.meta` files. Let Unity import and generate metadata.
