# Outcasts Agents

This directory defines persistent role profiles for Codex sub-agents used on `G:\UnityProjects\Outcasts`.

These files are project-visible routing docs, not runtime state. When spawning or briefing an agent, use the matching role file as the standing instruction source, then still follow `AGENTS.md` and the relevant project skills.

## Agents

- `game-director.md`: turns Outcasts docs into scoped gameplay tasks and reviews work against macro design, narrative, world, and content intent.
- `gameplay-designer.md`: builds authored gameplay in Unity, places encounters, tunes pacing, and configures rewards/risk.
- `ui-designer.md`: owns player-facing UI/UX design and authored UI implementation.
- `gameplay-systems-programmer.md`: implements runtime gameplay systems code.
- `read-only-gameplay-architect.md`: reviews architecture and code read-only.
- `gameplay-tester.md`: writes/runs focused gameplay tests and verification.
- `unity-editor-tools-programmer.md`: builds editor-only tools and editor code.
- `project-auditor.md`: audits project organization, naming, folder placement, `Resources`, and Addressables usage.

## Shared Memory Protocol

Before non-trivial Outcasts work, do a quick memory pass:

1. Start from the provided memory summary when available.
2. Search `C:\Users\LEGO\.codex\memories\MEMORY.md` for task-specific owners, systems, scene names, asset names, or workflow keywords.
3. Open only the one or two directly relevant rollout summaries or memory skill files when `MEMORY.md` points to them.
4. Treat memory as prior context, not proof of current state. Verify current code, assets, and Unity Editor state when cheap or drift-prone.
5. Do not write memory files unless the user explicitly asks to remember or update memory.

If a durable project rule must be remembered, update the relevant project doc or skill and the mirrored local skill copy when applicable. Do not use an ad-hoc memory note as the final remembered form.

## Shared Skill Rules

- Load only the skills that match the task and role.
- For Unity-owned state, use Unity MCP or Unity Editor APIs. Do not raw-edit scenes, prefabs, ScriptableObjects, materials, Timeline, animation, or `.meta` files when MCP is unavailable.
- Treat unrelated dirty files, console errors, test failures, and import side effects as shared workspace state unless caused by the current task.
