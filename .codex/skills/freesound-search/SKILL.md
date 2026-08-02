---
name: freesound-search
description: Search Freesound through the Freesound API from rough or fragmentary keywords, rank candidates by download count, and enforce hard duration/file-size limits before any download. Use when Codex needs to find suitable Freesound audio candidates, shortlist lightweight sounds, avoid too-long or too-heavy original files, or prepare IDs for the freesound-downloader skill.
---

# Freesound Search

Use this skill before downloading Freesound audio when the user gives rough keywords or asks for suitable candidates. This skill searches only; download originals later with `$freesound-downloader` only for approved results.

## Script

Run the bundled script:

```powershell
python C:\Users\LEGO\.codex\skills\local\freesound-search\scripts\search_sounds.py sea bubbles bells --max-duration 45 --max-size 15MB --limit 15 --types wav mp3 ogg --exclude-explicit
```

Defaults:

- `--max-duration 60` seconds.
- `--max-size 20MB` original file size.
- `sort=downloads_desc` for popularity-first results.
- Search filters include both `duration:[* TO max]` and `filesize:[* TO max_bytes]`.

The script reads optional credentials from:

```text
C:\Users\LEGO\.codex\secrets\freesound_auth.txt
C:\Users\LEGO\.codex\secrets\freesound_token.json
```

Do not print secrets or token file contents. Freesound search can use token authentication or OAuth bearer tokens; original downloads still belong to `$freesound-downloader`.

## Freesound Query Format

Use the current text-search endpoint:

```text
GET https://freesound.org/apiv2/search/
```

Put the text expression in `query`. Supported forms:

```text
query=cars
query=bass -drum
query="bass drum" -kick
query=+metal +impact
query=
```

Rules:

- Separate words with spaces for ordinary keyword search.
- Wrap multi-word phrases in double quotes.
- Prefix `+term` to make a term mandatory.
- Prefix `-term` to exclude a term.
- Use empty `query=` only when intentionally searching all sounds with filters.
- Treat numeric text such as `query=123` as general text search, not a guaranteed sound-ID lookup.
- Prefer `/apiv2/search/`; do not use deprecated `/apiv2/search/text/` for new work.

Always combine rough text search with explicit controls:

```text
sort=downloads_desc
fields=id,name,url,username,license,type,duration,filesize,num_downloads,tags,previews
filter=duration:[* TO 10] filesize:[* TO 5000000] type:(wav OR mp3)
page_size=50
```

## Rough Keywords

Pass the user's fragments as separate words. The script normalizes short phrases, tries a few query variants, deduplicates IDs, and sorts the final merged list by `num_downloads` descending.

Examples:

```powershell
python C:\Users\LEGO\.codex\skills\local\freesound-search\scripts\search_sounds.py metal hit short impact --max-duration 3 --max-size 5MB --limit 20 --types wav
python C:\Users\LEGO\.codex\skills\local\freesound-search\scripts\search_sounds.py sea calm bubbles bell --max-duration 90 --max-size 25MB --licenses "Creative Commons 0"
```


## Sound Selection Semantics

Let the sound's narrative function dictate candidate duration and placement:

- Impact, hit, snap, crack, gunshot, UI click, and other one-shot events should be very short. If a source file is longer, inspect metadata/name and assume it may contain multiple short takes; trim or place only the relevant transient instead of using the full file as ambience.
- Continuous process sounds such as engine idle, drill, machine rattle, fire, crowd bed, room tone, or machinery loops can be longer because they represent ongoing state.
- Do not pick a result only because it is popular. Compare the name, tags, duration, license, and scene context before approving it for download.
## Download Gate

Treat `approved_for_download=true` as the gate for follow-up downloads. Do not download a result that is rejected for duration or filesize unless the user explicitly changes the limits.

When reporting candidates, include ID, name, URL, license, duration, file size, download count, and why anything was rejected. Prefer returning 5-10 top approved candidates, not a huge list.

## Output Files

Use `--json path` or `--csv path` only when the user asks for a saved shortlist or when the result list is too large for chat. Keep generated shortlist files outside Unity `Assets/` unless they are intentionally project documentation.
