---
name: cutscene-authoring
description: Create or modify Outcasts scene cutscenes that match the existing Unity Timeline setup. Use when Codex needs to add a travel or first-visit cutscene to a scene, duplicate Cutscene Travel or Cutscene First Visit objects, create or clone Timeline .playable assets under Assets/Cutscenes, wire PlayableDirector references into GameSceneContext, configure or extend Cinemachine cameras/storyboards/audio/signals, bind CutsceneWindow cinematic frame/subtitle tracks, or verify cutscene scene objects through Unity MCP.
---

# Cutscene Authoring

## Required Skills

Use this skill with `$unity-mcp-orchestrator`, `$voiceover` for generated or placed dialogue audio, `$prefab-creation` for prefab variants or prefab source edits, `$ui-prefab-authoring` when touching CutsceneWindow/UI timeline bindings, `$apply-patch` for project text edits, and `$code-style` before any C# changes.

If the task changes C# scripts, also use `$mcp-unity-validate-script` and `$unity-recompile-menuitem` before reporting completion.

## Workflow

1. Read the active scene and existing cutscene objects through Unity MCP. Search for `PlayableDirector` objects and inspect their children/components before modifying anything.
2. Choose the closest source pattern:
   - Travel cutscene: source object/prefab `Cutscene Travel`, timeline `Assets/Cutscenes/Timeline Travel Loc1.playable`, finish signal `Signal Travel Finished`, scene reference `GameSceneContext._travelCutscene`.
   - First-visit cutscene: source object/prefab `Cutscene First Visit` or `Assets/Cutscenes/Cutscene Loc1_FirstVisit.prefab`, timeline `Assets/Cutscenes/Timeline Loc1_FirstVisit.playable`, finish signal `Signal Loc1_FirstVisit Finished`, scene reference `GameSceneContext._firstVisitCutscene`.
3. Duplicate or instantiate the closest existing cutscene instead of building from empty objects. Keep the root inactive by default; runtime services activate the root before `PlayableDirector.Play()`.
4. If the cutscene object is a prefab instance, make structural and shot-content edits inside the prefab asset/prefab stage, not as scene overrides. This includes cameras, shot groups, duplicated actors, props, activation-ready children, audio children, and reusable bindings. Scene instance overrides are only for scene-specific references that cannot live in the prefab, such as `GameSceneContext` fields, explicit scene-object bindings, or intentionally per-scene transforms approved by the user.
5. Clone the timeline asset before editing shot timing, clips, tracks, signals, subtitles, or audio. Do not point multiple scene-specific cutscenes at one timeline if the new cutscene needs different content.
6. For generated or placed voiceover, use [`$voiceover`](../voiceover/SKILL.md). Keep this skill focused on cutscene object, storyboard, Timeline ownership, and scene/runtime wiring.
7. If the source cutscene uses `CinemachineStoryboard`, treat it as a real storyboard/fade layer. Extend it by duplicating the nearest existing storyboard camera/clip and adjusting image, alpha, timing, and camera fields; do not remove or replace storyboard components unless the user explicitly asks.
8. Author shots as staged compositions, not as camera-only timeline gaps:
   - Inspect nearby completed shots first. Match their pattern of duplicated or repositioned cutscene objects, nested shot groups, activation clips, and transform clips.
   - For every new shot, make the required subject explicit: first reuse an existing scene or cutscene object if it is already visible from the required angle, active at the right time, and does not need to move. Duplicate or create a cutscene-local object only when the storyboard needs a different position, pose, activation window, prop variant, or stability independent from gameplay state.
   - Do not point a camera at empty world space just because the subtitle timing exists. If the storyboard shows a character, truck, fan, hanged body, or village prop, that object must be present, active, and framed during the whole camera clip.
   - Do not rely on gameplay-scene object positions unless they already match the storyboard at that time. Prefer reuse for existing objects that already compose correctly; prefer cutscene-local duplicates or explicit transform tracks when the shot would otherwise depend on unrelated scene edits.
   - Keep camera clips continuous unless the storyboard intentionally calls for black/fade/empty time. Adding a camera for a subtitle interval is not enough; the shot must have blocking.
9. Run a recursive storyboard alignment pass from the top of the storyboard to the bottom before reporting completion:
   - For each storyboard panel/row in order, identify the matching timeline clip(s), subtitle/audio interval, camera, and active objects.
   - Compare camera distance, angle, height, lens/FOV, screen composition, character pose/position, vehicle/prop placement, foreground/background objects, and visible silhouettes against the panel.
   - Fix the first mismatch found, then restart the pass from the top so earlier panels are not accidentally regressed by later fixes.
   - Continue until a full top-to-bottom pass finds no meaningful mismatch, or explicitly report the remaining subjective/asset-limited differences.
   - Technical checks like no gaps and resolved references are necessary but not sufficient; the shot must visually match the storyboard intent.
10. Every cutscene camera that blends back to `CinemachineCamera Gameplay` must use the existing Cut-montage pattern in `Assets/Cutscenes/CinemachineBlenderSettings.asset`: add or preserve a `CustomBlends` entry from that camera name to `CinemachineCamera Gameplay` with a cut blend (`Style: 0`, `Time: 0`).
11. Preserve the runtime contract:
   - root GameObject has `PlayableDirector` with `playOnAwake = true`, `timeUpdateMode = Game Time`, and `extrapolationMode = Hold`.
   - timeline includes the expected finish `SignalTrack` marker name for the service that will play it.
   - travel cutscenes bind to `GameSceneFlow`; first-visit cutscenes bind to `FirstVisitCutsceneService`.
   - `CutsceneWindow.BindTimeline` must be able to find `CinematicFrameTrack` and `CinematicSubtitlesTrack` and bind them to shared UI targets.
12. Wire the scene owner. Assign the new `PlayableDirector` to the matching serialized field on `GameSceneContext`; do not add a second local startup path to play the cutscene.
13. When adding audio, let the scene narrative and existing runtime owners constrain the sound design:
   - Match ambience to the actual place and population state. A deserted desert/village shot should not get generic street/crowd ambience just because the camera sees buildings.
   - Before adding direct wind, storm, weather, music, or other broad ambience clips, check whether the project already has a stronger environment/runtime owner for that layer. Do not duplicate existing wind/storm systems with a blunt Timeline audio clip unless the requested cutscene needs a deliberate localized accent.
   - Match clip duration to sound function: impacts and cracks should be short one-shots or trimmed takes; continuous process sounds such as engines, drills, machine rattles, or mechanical loops may be longer.
14. Verify in Unity: scene hierarchy/components, timeline asset path on the director, storyboard cameras/clips when present, Cut-montage blends to gameplay camera, signal presence, `GameSceneContext` reference, console errors, and a short manual play/test if requested. For authored shots, also verify no unintended camera gaps, all camera exposed references resolve, each camera clip frames its storyboard subject for the full clip duration, and the recursive storyboard alignment pass has completed.

## Reference

Read [references/current-patterns.md](references/current-patterns.md) before authoring a new cutscene or changing an existing cutscene pattern. It records the current scene/prefab anatomy and runtime binding requirements discovered from the project.

## Guardrails

Do not raw-edit `.unity`, `.prefab`, `.playable`, `.asset`, `.mat`, `.controller`, `.anim`, `.renderTexture`, or `.meta` files when Unity MCP can do the operation. If MCP is unavailable for required Editor-owned state, stop and ask the user to reconnect or approve a weaker serialized-file fallback.

Do not create new C# helpers, services, DTOs, or editor tools unless the current cutscene task cannot be completed with the existing PlayableDirector, Timeline, Cinemachine, `GameSceneContext`, `GameSceneFlow`, `FirstVisitCutsceneService`, and `CutsceneWindow` flow.
