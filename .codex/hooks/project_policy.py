import hashlib
import json
import re
import shlex
import subprocess
import sys
import tempfile
from pathlib import Path

SERIALIZED_EXTENSIONS = (".unity", ".prefab", ".asset", ".mat", ".anim", ".meta")
SERIALIZED_PATH_RE = re.compile(r"(?i)(?:^|[\s\"'])([^\s\"']+\.(?:unity|prefab|asset|mat|anim|meta))(?=$|[\s\"'])")
PATCH_FILE_RE = re.compile(r"^\*\*\* (?:Update|Add|Delete) File:\s*(.+?)\s*$", re.MULTILINE)
PATCH_MOVE_RE = re.compile(r"^\*\*\* Move to:\s*(.+?)\s*$", re.MULTILINE)
CS_PATH_RE = re.compile(r"(?i)(?:^|[\s\"'])([^\s\"']+\.cs)(?=$|[\s\"'])")

DANGEROUS_COMMANDS = (
    (re.compile(r"(?i)(?<![\w.-])msbuild(?:\.exe)?(?![\w.-])"), "Unity project compilation must run through Unity Editor/MCP, not msbuild."),
    (re.compile(r"(?i)\bgit\s+reset\s+--hard\b"), "git reset --hard is blocked to preserve existing dirty and untracked work."),
    (re.compile(r"(?i)\bgit\s+clean\b"), "git clean is blocked to preserve existing untracked work."),
    (re.compile(r"(?i)\bgit\s+worktree\b"), "git worktree operations are blocked in the shared main repository."),
    (re.compile(r"(?i)(?:rm\s+-[a-z]*r|remove-item\s+[^\r\n]*-recurse|rmdir\s+/s|rd\s+/s)"), "Recursive deletion is blocked by the repository safety hook."),
)

MUTATING_COMMAND = re.compile(
    r"(?i)(?:\b(?:set-content|add-content|out-file|clear-content|remove-item|move-item|copy-item|rename-item|new-item|rm|mv|cp|touch|sed|perl)\b|\b(?:git\s+(?:add|commit|mv|rm|apply|restore|checkout))\b|>>?|\b(?:write_text|write_bytes|writealltext|writefile)\b|text_conventions\.py\s+(?:write|replace)\b)"
)
GENERIC_UNITY_TOOL = re.compile(
    r"(?i)(?:manage_gameobject[^\s]*\.(?:get_components|get_component)|component_properties|set_component_property)"
)
UNITY_MUTATION_TOOL = re.compile(
    r"(?i)(?:create|delete|destroy|modify|set|move|save|play|stop|execute|refresh|recompile|run)"
)
ALLOCATOR_MARKERS = ("TLS Allocator ALLOC_TEMP_TLS", "TLS Allocator ALLOC_TEMP_MAIN", "ValidTRS()")
SCENE_RELOAD_RE = re.compile(r"(?i)(?:(?<![a-z])reload[_ .-]?scene(?![a-z])|(?<![a-z])scene[_ .-]?reload(?![a-z])|(?<![a-z])manage_scene[\s\S]*?\breload\b)")
BLOCKING_DIALOG_RE = re.compile(r"(?i)(?:\bdisplay[_ .]?dialog\b|\bshow[_ .]?dialog\b|\b(?:blocking|modal)[-_ ]?dialog\b|\bconfirm[_ .]?dialog\b|\beditorutility\s*\.\s*displaydialog\b)")
SHELL_ACTION_RE = re.compile(r"(?i)(?:\b(?:python|py|powershell|pwsh|dotnet|unity|start-process|invoke)\b|\b(?:set-content|add-content|out-file|remove-item|move-item|copy-item|rename-item|new-item|rm|mv|cp|sed|perl)\b)")
ROUTING_STATE_DIR = Path(tempfile.gettempdir()) / "td3d-codex-hook-state"
OPEN_SCENE_PATH = 'Assets/Scenes/Gameplay.unity'

ROLE_MAP = {
    "game-director": ".agents/game-director.md",
    "gameplay-designer": ".agents/gameplay-designer.md",
    "gameplay-systems-programmer": ".agents/gameplay-systems-programmer.md",
    "gameplay-tester": ".agents/gameplay-tester.md",
    "project-auditor": ".agents/project-auditor.md",
    "ui-designer": ".agents/ui-designer.md",
    "unity-editor-tools-programmer": ".agents/unity-editor-tools-programmer.md",
}
SKILL_MAP = {
    "apply-patch": ".codex/skills/apply-patch/SKILL.md",
    "code-style": ".codex/skills/code-style/SKILL.md",
    "editor-tool-authoring": ".codex/skills/editor-tool-authoring/SKILL.md",
    "mcp-unity-validate-script": ".codex/skills/mcp-unity-validate-script/SKILL.md",
    "prefab-creation": ".codex/skills/prefab-creation/SKILL.md",
    "test-writing": ".codex/skills/test-writing/SKILL.md",
    "ui-prefab-authoring": ".codex/skills/ui-prefab-authoring/SKILL.md",
    "ui-prefab-localization": ".codex/skills/ui-prefab-localization/SKILL.md",
    "unity-mcp-skill": ".codex/skills/unity-mcp-skill/SKILL.md",
}


def read_event():
    try:
        value = json.load(sys.stdin)
    except Exception:
        return {}
    return value if isinstance(value, dict) else {}


def tool_input(event):
    value = event.get("tool_input", {})
    if isinstance(value, dict):
        command = value.get("command")
        if isinstance(command, str):
            return command
        return json.dumps(value, ensure_ascii=False)
    return value if isinstance(value, str) else json.dumps(value, ensure_ascii=False)


def tool_response_text(event):
    value = event.get("tool_response", "")
    if isinstance(value, str):
        return value
    return json.dumps(value, ensure_ascii=False)


def patch_paths(command):
    paths = list(PATCH_FILE_RE.findall(command))
    paths.extend(PATCH_MOVE_RE.findall(command))
    return [path.strip().strip('"') for path in paths]


def is_serialized_path(path):
    return Path(path.replace("\\", "/")).suffix.lower() in SERIALIZED_EXTENSIONS


def serialized_paths(text):
    return [match.group(1) for match in SERIALIZED_PATH_RE.finditer(text)]


def changed_csharp(text, paths=None):
    if paths:
        return any(Path(path.replace("\\", "/")).suffix.lower() == ".cs" for path in paths)
    return bool(CS_PATH_RE.search(text)) and bool(MUTATING_COMMAND.search(text))


def project_root(event):
    cwd = event.get("cwd")
    if not isinstance(cwd, str) or not cwd:
        raise RuntimeError("Codex did not provide a session cwd.")
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        cwd=cwd,
        capture_output=True,
        text=True,
        timeout=5,
        check=False,
    )
    if result.returncode != 0:
        raise RuntimeError("git rev-parse could not resolve the project root.")
    return Path(result.stdout.strip()).resolve()


def state_path(event):
    session_id = str(event.get("session_id", "")).strip()
    if not session_id:
        return None
    safe_id = re.sub(r"[^A-Za-z0-9_.-]", "_", session_id)
    return ROUTING_STATE_DIR / f"{safe_id}.json"


def read_source(root, relative_path):
    path = root / relative_path
    if not path.is_file():
        raise RuntimeError(f"Required source is missing: {relative_path}")
    content = path.read_text(encoding="utf-8")
    return {
        "path": relative_path,
        "lines": len(content.splitlines()),
        "chars": len(content),
        "sha256": hashlib.sha256(content.encode("utf-8")).hexdigest()[:16],
    }


def has_any(text, words):
    return any(word in text for word in words)


def route_prompt(prompt):
    text = prompt.lower()
    is_test = has_any(text, ("test", "тест", "smoke", "play mode", "проверить"))
    is_ui = has_any(text, ("ui", "интерфейс", "hud", "локализа", "localization", "кнопк", "prefab ui"))
    is_editor = has_any(text, ("editor", "editorwindow", "menuitem", "editor tool", "mcp", "assetdatabase", "инструмент"))
    is_design = has_any(text, ("дизайн", "design", "награ", "волна", "башн", "враг", "tower", "monster", "gameplay", "механик"))
    is_runtime = has_any(text, ("c#", "script", "runtime", "код", "исправ", "реализ", "баг", "ошиб", "compile", "компиля"))
    is_docs = has_any(text, ("документ", "documentation", "agents", "hook", "skill", "инструкц", "роль", "структур"))

    if is_test:
        role = "gameplay-tester"
    elif is_ui:
        role = "ui-designer"
    elif is_editor:
        role = "unity-editor-tools-programmer"
    elif is_design:
        role = "gameplay-designer"
    elif is_runtime:
        role = "gameplay-systems-programmer"
    elif is_docs:
        role = "project-auditor"
    else:
        role = "project-auditor"

    skills = []

    def add_skill(name):
        if name not in skills:
            skills.append(name)

    add_skill("apply-patch")
    if is_ui:
        add_skill("ui-prefab-authoring")
        if "локализа" in text or "localization" in text:
            add_skill("ui-prefab-localization")
    if is_test:
        add_skill("test-writing")
    if is_editor:
        add_skill("unity-mcp-skill")
        add_skill("editor-tool-authoring")
    if is_design and not is_ui:
        add_skill("unity-mcp-skill")
    if is_runtime:
        add_skill("code-style")
        add_skill("mcp-unity-validate-script")
        if not is_editor:
            add_skill("unity-mcp-skill")
    if "prefab" in text:
        add_skill("prefab-creation")
    if not is_docs and not skills:
        add_skill("unity-mcp-skill")

    docs = ["AGENTS.md", ".codex/docs/ProjectStructure.md"]
    if is_design or is_runtime or is_ui or is_test or is_editor or "unity" in text:
        docs.append("Assets/Documentation/GAMEPLAY_REFERENCES.md")
    if "greenfield" in text:
        docs.append("Assets/Documentation/GameplayGreenfield/00_INDEX.md")
    if "ml-agent" in text or "ml agent" in text:
        docs.append("Assets/Documentation/ML_AGENTS.md")

    unique_docs = []
    for path in docs:
        if path not in unique_docs:
            unique_docs.append(path)

    return role, skills, unique_docs


def route_and_load(event):
    root = project_root(event)
    prompt = str(event.get("prompt", ""))
    role, skills, docs = route_prompt(prompt)
    sources = [read_source(root, "AGENTS.md"), read_source(root, ROLE_MAP[role])]
    sources.extend(read_source(root, SKILL_MAP[skill]) for skill in skills)
    sources.extend(read_source(root, doc) for doc in docs if doc != "AGENTS.md")

    path = state_path(event)
    if path is None:
        raise RuntimeError("Codex did not provide a session id for routing state.")
    ROUTING_STATE_DIR.mkdir(parents=True, exist_ok=True)
    state = {
        "session_id": str(event.get("session_id")),
        "turn_id": str(event.get("turn_id", "")),
        "project_root": str(root),
        "role": role,
        "skills": skills,
        "sources": sources,
    }
    path.write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")
    return root, role, skills, sources


def routing_ready(event):
    path = state_path(event)
    if path is None or not path.is_file():
        return False
    try:
        state = json.loads(path.read_text(encoding="utf-8"))
        root = str(project_root(event))
    except (OSError, json.JSONDecodeError, RuntimeError):
        return False
    current_turn = str(event.get("turn_id", ""))
    stored_turn = str(state.get("turn_id", ""))
    return state.get("project_root") == root and (not current_turn or not stored_turn or current_turn == stored_turn)


def is_project_mutation(tool_name, command):
    name = tool_name.lower()
    if name == "apply_patch":
        return True
    if name == "bash":
        return bool(MUTATING_COMMAND.search(command))
    return name.startswith("mcp__unitymcp") and bool(UNITY_MUTATION_TOOL.search(name))


def forbidden_unity_action(tool_name, command, pattern):
    name = tool_name.lower()
    combined = f"{tool_name}\n{command}"
    if name.startswith("mcp__unitymcp"):
        return bool(pattern.search(combined))
    if name == "apply_patch":
        return bool(pattern.search(command))
    if name == "bash":
        return bool(pattern.search(command) and (MUTATING_COMMAND.search(command) or SHELL_ACTION_RE.search(command)))
    return False


def normalized_repo_path(value):
    return value.strip().replace("\\", "/").lstrip("./").lower()


def includes_open_scene(paths):
    target = OPEN_SCENE_PATH.lower()
    return any(normalized_repo_path(path) == target for path in paths)


def git_names(cwd, args):
    result = subprocess.run(
        ["git", *args],
        cwd=cwd,
        capture_output=True,
        text=True,
        timeout=5,
        check=False,
    )
    if result.returncode != 0:
        return None
    return result.stdout.splitlines()


def path_may_include_open_scene(value):
    raw = value.strip().replace("\\", "/").lower()
    if raw in (".", "./", "*", "./*", "assets/scenes", "assets/scenes/*"):
        return True
    normalized = raw.lstrip("./")
    target = OPEN_SCENE_PATH.lower()
    if normalized == target:
        return True
    if "*" in normalized:
        return target.startswith(normalized.split("*", 1)[0])
    return False


def git_restore_is_index_only(command):
    lower_command = command.replace("\\", "/").lower()
    return bool(re.search(r"(?i)(?<![\w.-])git\s+restore\b[^\n]*--staged\b", command)) and "--worktree" not in lower_command


def git_restore_touches_open_scene(event, command):
    if not re.search(r"(?i)(?<![\w.-])git\s+(?:restore|checkout)\b", command):
        return False

    lower_command = command.replace("\\", "/").lower()
    if git_restore_is_index_only(command):
        return False
    if OPEN_SCENE_PATH.lower() in lower_command:
        return True

    try:
        tokens = shlex.split(command, posix=False)
    except ValueError:
        return False

    try:
        restore_index = next(index for index, token in enumerate(tokens) if token.lower() in ("restore", "checkout") and index > 0 and tokens[index - 1].lower() == "git")
    except StopIteration:
        return False

    args = tokens[restore_index + 1:]
    if "--" in args:
        paths = args[args.index("--") + 1:]
    else:
        paths = []
        skip_next = False
        options_with_values = {"--source", "--pathspec-from-file", "--pathspec-file-nul"}
        for arg in args:
            if skip_next:
                skip_next = False
                continue
            if arg in options_with_values:
                skip_next = True
                continue
            if arg.startswith("-"):
                continue
            paths.append(arg)

    return any(path_may_include_open_scene(path) for path in paths)
def emit(value):
    sys.stdout.write(json.dumps(value, ensure_ascii=False) + "\n")


def deny(reason):
    emit({
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": reason,
        }
    })


def post_feedback(message, block=False):
    value = {"hookSpecificOutput": {"hookEventName": "PostToolUse", "additionalContext": message}}
    if block:
        value["decision"] = "block"
        value["reason"] = message
    emit(value)


def main():
    event = read_event()
    event_name = event.get("hook_event_name", "")
    tool_name = str(event.get("tool_name", ""))
    command = tool_input(event)
    tool_name_lower = tool_name.lower()

    if event_name == "SessionStart":
        emit({
            "hookSpecificOutput": {
                "hookEventName": "SessionStart",
                "additionalContext": "TD3D hooks are active. UserPromptSubmit will load AGENTS.md, select the closest role and project-local skills, read relevant documentation, and initialize the mutation gate before project changes.",
            }
        })
        return

    if event_name == "UserPromptSubmit":
        try:
            root, role, skills, sources = route_and_load(event)
        except (OSError, RuntimeError, KeyError) as error:
            emit({"decision": "block", "reason": f"TD3D instruction-routing gate failed: {error}"})
            return
        source_lines = ", ".join(f"{source['path']} ({source['lines']} lines, {source['sha256']})" for source in sources)
        emit({
            "hookSpecificOutput": {
                "hookEventName": "UserPromptSubmit",
                "additionalContext": (
                    f"TD3D instruction router completed for {root}. Selected role: .agents/{role}.md. "
                    f"Selected project-local skills: {', '.join(skills)}. "
                    f"Hook fully read and verified these sources before this turn: {source_lines}. "
                    "Project mutations are gated on this routing state; architectural owner-chain, no-fallback, and runtime-proof rules remain mandatory."
                ),
            }
        })
        return

    if event_name == "SessionEnd":
        path = state_path(event)
        if path is not None and path.is_file():
            path.unlink()
        return

    if event_name == "PreToolUse":
        if forbidden_unity_action(tool_name, command, SCENE_RELOAD_RE):
            deny("Scene reload is blocked in TD3D because it can open Unity's blocking unsaved-changes dialog. Scene open/load/save remain allowed when no blocking dialog is requested.")
            return

        if forbidden_unity_action(tool_name, command, BLOCKING_DIALOG_RE):
            deny("Blocking/modal Unity dialogs are blocked in TD3D. Use non-modal state inspection or a direct tool result instead.")
            return

        if is_project_mutation(tool_name, command) and not routing_ready(event):
            deny("TD3D instruction-routing gate is not initialized for this turn. UserPromptSubmit must first load AGENTS.md, choose the closest role and project-local skills, and read the relevant project documentation.")
            return

        if GENERIC_UNITY_TOOL.search(tool_name):
            deny("Blocked generic Unity GameObject/component reflection call. Use a narrow typed MCP tool, Unity API, or project MenuItem; restart Unity after allocator errors.")
            return

        if tool_name_lower == "apply_patch":
            paths = patch_paths(command)
            if any(is_serialized_path(path) for path in paths):
                deny("Blocked manual apply_patch edit of a Unity serialized asset. Use Unity/MCP/AssetDatabase/PrefabUtility unless this is an explicitly necessary small serialized change.")
                return

        if tool_name_lower == "bash":
            for pattern, reason in DANGEROUS_COMMANDS:
                if pattern.search(command):
                    deny(reason)
                    return
            if git_restore_touches_open_scene(event, command):
                deny("Local git restore/checkout is blocked because it would discard changes in the open scene Assets/Scenes/Gameplay.unity. Restore/checkout other files is allowed.")
                return
            if MUTATING_COMMAND.search(command) and serialized_paths(command) and not git_restore_is_index_only(command):
                deny("Blocked shell mutation of a Unity serialized asset. Use Unity/MCP/AssetDatabase/PrefabUtility to preserve serialized references and GUIDs.")
                return
        return

    if event_name == "PostToolUse":
        response = tool_response_text(event)
        if any(marker in response for marker in ALLOCATOR_MARKERS):
            post_feedback("Unity allocator/ValidTRS signature detected. Stop further GameObject/Component MCP calls and Play/Save attempts; restart Unity before continuing.", block=True)
            return

        paths = patch_paths(command) if tool_name_lower == "apply_patch" else []
        if changed_csharp(command, paths):
            post_feedback("C# changed in TD3D. Before further diagnosis: run direct mcp-unity-validate-script, wait for Unity compilation, inspect Console, run TD/Automation/Force Recompile All if compilation fails or hangs, and run the bounded Gameplay.unity Play Mode smoke for runtime changes.")
        return


if __name__ == "__main__":
    main()
