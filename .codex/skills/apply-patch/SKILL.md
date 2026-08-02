---
name: apply-patch
description: Write text edits with correct EOL, UTF-8 BOM, final newline, indentation, and Cyrillic-safe encoding. Use before editing or creating project text files.
---

# Apply Patch


## Rule

For project text files, do not use raw `apply_patch` as the default edit mechanism. Use the bundled convention-aware helper so the first write already preserves EOL, UTF-8 BOM, final newline, and UTF-8 text.

Resolve the helper from this skill directory, not from the repository root:

```powershell
$tc = ".codex/skills/apply-patch/scripts/text_conventions.py"
if (!(Test-Path $tc)) { $tc = "C:/Users/LEGO/.codex/skills/local/apply-patch/scripts/text_conventions.py" }
```

Before editing an existing text file, inspect it:

```powershell
python $tc inspect --files Assets\Path\File.cs
```

Track and preserve:

- EOL style: CRLF, LF, mixed, or no-newlines.
- UTF-8 validity, especially when Cyrillic/Russian text is present.
- UTF-8 BOM: present or absent.
- Final newline: present or absent.
- Local indentation style: tabs or spaces.

If the helper reports mixed EOL, invalid UTF-8, replacement characters, or mojibake markers, stop and decide explicitly with the user unless the requested task is to fix that exact convention problem.

## Existing Files

For exact snippet replacement, use `replace`. It reads the target convention and writes the result back with the same EOL/BOM/final-newline in one operation:

```powershell
python $tc replace --file Assets\Path\File.cs --old-file $env:TEMP\old.txt --new-file $env:TEMP\new.txt --verify --git-diff-check
```

For whole-file replacement, put the desired content in a temp file and use `write`. Existing target files keep their own convention by default:

```powershell
python $tc write --file Assets\Path\File.cs --content-file $env:TEMP\content.txt --verify --git-diff-check
```

Create snippet/content temp files with deterministic UTF-8 writes. Prefer PowerShell here-string snippets for non-empty temp files, especially when the snippet contains quotes, apostrophes, backticks, `$`, braces, XML/YAML, or C# string literals. Do not pass such snippets as inline PowerShell string arguments; quoting failures can silently change the text before the helper sees it.

PowerShell requires here-string delimiters on their own lines. Do not use an empty here-string for empty replacements; empty bodies are easy to emit incorrectly. For an empty `--old-file`, `--new-file`, or `--content-file`, create the temp file explicitly:

```powershell
[System.IO.File]::WriteAllText($env:TEMP + "\empty.txt", "", [System.Text.UTF8Encoding]::new($false))
```

For non-empty snippets, use `[System.IO.File]::WriteAllText(...)` directly only for simple literal content that has no shell-sensitive characters.

Rules for existing project files:

- Do not run raw `apply_patch` on an existing project text file when `replace` or `write` can express the edit.
- Do not perform a raw patch and then clean up EOL/EOF churn as a normal workflow. That means the edit path was wrong.
- If a target file has no final newline, treat that as a first-class convention. Do not use raw `apply_patch` unless you snapshot first and restore immediately after the patch.
- If any edit adds or removes only the final newline, stop and restore that convention immediately through `text_conventions.py restore` or `write`; do not describe it as a harmless cleanup.
- Run `--verify --git-diff-check` in the same helper command whenever possible.
- After editing, inspect the diff and treat unrelated whitespace, EOL, BOM, or final-newline churn as a failed edit that must be corrected before continuing.

`verify` fails on mixed EOL by default. Treat that as a bad edit, not as a warning to normalize later.

## New Files

For new project text files, create the complete intended content first and write the file once through `write`. Do not create a new project file with raw `apply_patch`, `Set-Content`, `Out-File`, or an editor-side shell write and then patch it again. A newly created CRLF/BOM file is already a convention bug.

Before creating any new `.cs` file, load and apply `$code-style` first. `$apply-patch` controls text conventions only; it must not be used as a bypass around project C# architecture, naming, one-type-per-file, UniTask, DI, DTO, or runtime/editor separation rules.

Copy nearest sibling convention:

```powershell
python $tc write --file Assets\Path\NewFile.cs --content-file $env:TEMP\content.txt --like Assets\Nearest\Existing.cs --verify --git-diff-check
```

Use project defaults when there is no local example:

```powershell
python $tc write --file Assets\Path\NewFile.cs --content-file $env:TEMP\content.txt --eol lf --bom no --final-newline no --verify --git-diff-check
```

Rules for new project files:

- Do not split creation and first edit into separate raw patch steps. Build the final first version in the content file and call `write` once.
- Do not rely on exact-text patch matching against a file that was just created with unknown EOL/BOM conventions.
- If a new file already exists with the wrong convention, stop patching it and rewrite the whole file through `write` with `--like` or explicit `--eol/--bom/--final-newline`.
- Keep temp snippet/content files outside the workspace. Their own BOM/EOL does not matter when read by the helper, but they must never be copied directly into the repository.

Defaults:

- C#: LF, UTF-8 without BOM, no final newline, Rider default indentation with 4 spaces.
- Markdown and other non-C# text: LF, UTF-8 without BOM, one final newline.

## Cyrillic And Non-ASCII

Russian/Cyrillic text in comments, string literals, localization keys, and generated text must stay as valid UTF-8.

Do not:

- Write with ANSI/default encodings.
- Convert Cyrillic to `?`, replacement character `U+FFFD`, mojibake markers like `\u00D0`/`\u00D1`/`\u0420\u045F`, or `\uXXXX` escapes unless the target file already uses escaped Unicode.
- Use PowerShell `Set-Content`/`Out-File` for workspace files without routing the final write through `text_conventions.py write`.

Run this after editing any file with Russian or other non-ASCII text:

```powershell
python $tc verify --files Assets\Path\File.cs --git-diff-check
```

## Raw Apply Patch Fallback

Raw `apply_patch` is only a guarded fallback for cases where the helper cannot reasonably express the edit, such as a very large structured patch where snippet replacement would be less reliable. It is not allowed as the first choice for existing project text files.

If raw `apply_patch` is unavoidable on any existing text file, snapshot conventions first, patch, restore conventions immediately, then verify:

```powershell
python $tc snapshot --output $env:TEMP\codex-text-conventions.json --files Assets\Path\File.cs
# apply_patch
python $tc restore --snapshot $env:TEMP\codex-text-conventions.json
python $tc verify --files Assets\Path\File.cs --git-diff-check
```

For multiple existing files, include every touched file in the same snapshot before patching. If restore changes any file, inspect the diff before proceeding; content edits should be intentional and convention restores should be the only non-content change.

The preferred path is always to write the correct convention on the first write.
