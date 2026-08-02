---
name: asmdef-references
description: Resolve Unity asmdef owners and add missing `.asmdef` references safely, preserving GUIDs and file conventions.
---

# ASMDEF References


## Overview

Fill Unity assembly definition references with minimal, reviewable changes. Prefer Unity/MCP and this skill's script over hand-editing `.asmdef` JSON.

Do not create new `.asmdef` files for ordinary systems just to organize folders. Most project-owned `Assets/Scripts` code lives in Unity default assemblies; a new asmdef is a compile boundary and needs a real dependency-direction reason.

## Workflow

1. Identify the compile error or changed scripts.
2. Find the asmdef that owns the script with the missing reference:
   ```powershell
   python scripts/asmdef_refs.py owner --file Assets\Path\SomeScript.cs
   ```
3. Find the asmdef that owns the referenced type by locating that type's `.cs` file, then run `owner` for it too.
4. Add the reference with a dry run first:
   ```powershell
   python scripts/asmdef_refs.py add --target Target.Assembly --reference Referenced.Assembly
   ```
5. If the plan is correct, apply it:
   ```powershell
   python scripts/asmdef_refs.py add --target Target.Assembly --reference Referenced.Assembly --apply
   ```
6. Verify Unity compilation through the Editor/MCP when available, using explicit `unity_instance="<target Name@hash>"` routing. If MCP is unavailable or the exposed schema cannot be routed per call, stop and ask the user to reconnect/fix MCP before doing Editor-owned verification. Do not use `set_active_instance`.

## Rules

- Add references in the direction of use: if code in assembly `A` uses a type from assembly `B`, add `B` to `A.references`.
- Do not add or split asmdefs as a structure cleanup. Use this skill for real existing asmdef ownership or compile errors, and ask before introducing a new assembly boundary.
- Do not add the reverse reference unless code in `B` also uses `A`; circular asmdef references are invalid and usually indicate the design needs refactoring.
- Runtime assemblies must not reference Editor-only assemblies. Move editor-only code under an Editor assembly instead.
- Prefer name references unless the target asmdef already uses `GUID:` references; the script's `auto` style follows that local convention.
- Preserve existing `.meta` GUIDs. Never invent GUIDs.
- Keep `.asmdef` edits scoped to the `references` array whenever possible.
- Use the script's `--apply` path for writes. It preserves the target `.asmdef` EOL style, UTF-8 BOM state, and final-newline state on the first write.
- After applying changes, run `git diff --check` and inspect the target `.asmdef` diff for accidental churn.

## Script Commands

List known asmdefs:

```powershell
python scripts/asmdef_refs.py list
```

Find the owner asmdef for a file:

```powershell
python scripts/asmdef_refs.py owner --file Assets\Scripts\Foo.cs
```

Add one or more references:

```powershell
python scripts/asmdef_refs.py add --target My.Gameplay --reference My.Core --reference My.Data --apply
```

Useful options:

- `--project-root <path>`: run against a project other than the current directory.
- `--style auto|name|guid`: choose reference format. Default is `auto`.
- `--allow-editor-reference`: allow a non-Editor target to reference an Editor-only asmdef.
- `--allow-cycle`: allow a reference that creates a cycle. Avoid this unless Unity-specific context proves it is safe.
- `--json`: print machine-readable output.
