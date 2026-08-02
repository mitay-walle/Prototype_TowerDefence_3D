# Current Cutscene Patterns

## Scene Objects

Active scene inspected: `Assets/Scenes/Loc1_Acharagma_Village.unity`.

Two inactive root objects currently carry `PlayableDirector` components:

- `Cutscene Travel`
  - root transform approximately `(31.9, -0.01, 91.85)`, rotation Y `7.327559`.
  - children: `CinemachineCamera`, `Position`, `Position 1`, `Truck Gameplay`.
  - director asset: `Assets/Cutscenes/Timeline Travel Loc1.playable`.
  - duration observed: about `10.43` seconds.
- `Cutscene First Visit`
  - same root transform as travel in the inspected scene.
  - children include `Cutscene Audio`, `Cutscene Voiceover`, `Shot 124`, `Truck`, `Cam Shot_6 black fade in`, `Cam Shot_7 black fade in`, `Tree`, `Cam Shot_8 black fade out`, `Cam Shot_9`, `Cam Shot_10`, `Shot 11`, `Joke`.
  - director asset: `Assets/Cutscenes/Timeline Loc1_FirstVisit.playable`.
  - duration observed: about `40.97` seconds.

Prefab sources under `Assets/Cutscenes` include `Cutscene Travel.prefab` and `Cutscene Loc1_FirstVisit.prefab`. Prefer cloning these or existing scene roots over creating empty timelines from scratch. When the active cutscene is a prefab instance, reusable shot content belongs in the prefab asset: cameras, staged actors, props, shot groups, and activation-ready children should be edited through prefab stage/headless prefab modification. Do not leave those changes only as scene overrides unless they are deliberately scene-specific.

## Timeline Assets

Travel timeline:

- `Assets/Cutscenes/Timeline Travel Loc1.playable`.
- Uses `Cinematic Frame Track` with `CinematicFrameAsset`.
- Must contain finish signal named `Signal Travel Finished` for `GameSceneFlow.BindTravelCutsceneFinishedSignal`.

First-visit timeline:

- `Assets/Cutscenes/Timeline Loc1_FirstVisit.playable`.
- Uses `CinematicSubtitlesTrack`, `Cinematic Frame Track`, audio tracks, activation tracks, volume tracks, and signal track.
- Must contain finish signal named `Signal Loc1_FirstVisit Finished` for `FirstVisitCutsceneService.BindFirstVisitCutsceneFinishedSignal`.
- Uses localized dialogue keys such as `cutscene. first visit Loc1. What a Hole` in `Assets/Localization/Dialogue`.

## Runtime Binding

`GameSceneContext` owns scene references:

- `_travelCutscene` is required and becomes `GameSceneProvider.TravelCutscene`.
- `_firstVisitCutscene` is optional and becomes `GameSceneProvider.FirstVisitCutscene`.

`GameSceneFlow.ShowTravelCutscene(scene)`:

- requires a travel director or loads the target scene immediately.
- binds the `Signal Travel Finished` signal to `TravelCutsceneSignalReceiver`.
- shows `CutsceneWindow` in `CutsceneWindowMode.Travel`.
- calls `CutsceneWindow.BindTimeline(travelCutscene)`, resets time, activates the root, then plays.

`FirstVisitCutsceneService.ShowFirstVisitCutscene()`:

- returns if no first-visit director is assigned.
- binds the `Signal Loc1_FirstVisit Finished` signal to `FirstVisitCutsceneSignalReceiver`.
- shows `CutsceneWindow` in `CutsceneWindowMode.FirstVisit`.
- calls `CutsceneWindow.BindTimeline(firstVisitCutscene)`, resets time, activates the root, then plays.
- on completion, hides the window, deactivates the root, and marks the scene visited in save state.

`CutsceneWindow.BindTimeline(PlayableDirector)`:

- iterates `TimelineAsset.GetOutputTracks()`.
- for `CinematicFrameTrack`, calls `director.SetGenericBinding(track, _cinematicFrame)` and sets every `CinematicFrameAsset.TimelineCinematicFrame.exposedName` reference to `_cinematicFrame`.
- for `CinematicSubtitlesTrack`, calls `director.SetGenericBinding(track, _subtitles)`.

Do not duplicate CutsceneWindow UI inside every cutscene to satisfy bindings. Keep UI shared and bind timeline tracks/references before playback.

## Components To Preserve

Audio child objects use `AudioSource` with mixer `Assets/Audio/AudioMixer.mixer`, `playOnAwake = false`, `spatialBlend = 1`, `dopplerLevel = 0`, spread `60`, min distance `10`, max distance `500`, and custom rolloff.

Cinemachine shot cameras commonly use `CinemachineCamera` priority `20`, output channel `1`, far clip `1500`, near clip `0.1`, and FOV values set per shot. Fade shots use `CinemachineStoryboard` with `Assets/UI/Utility/black_pixel_16px.png`, alpha `1`, aspect `2`, sync scale enabled, and mute camera disabled.

Some cutscenes intentionally contain storyboard/fade cameras, for example `Cam Shot_6 black fade in`, `Cam Shot_7 black fade in`, and `Cam Shot_8 black fade out`. When adding shots to a cutscene that already uses storyboard cameras, extend the storyboard sequence by duplicating the nearest existing storyboard camera/clip and adjusting timing/properties. Do not collapse the storyboard into plain camera cuts, delete `CinemachineStoryboard`, or swap it for UI fades unless the user explicitly asks for that redesign.

Completed first-visit shots are staged around cutscene-local objects, not only cameras. Examples include nested shot groups under `Cutscene First Visit/Truck`, duplicated bot actors such as `Bot_Cutscene Shot 2`, cutscene-owned `Truck`, shot cameras parented near their subject, and activation/transform timeline clips for objects like `Tree`, `Truck`, and `Bot`. Continue that pattern when extending the cutscene: add or reuse shot-local actors/props, set their transforms for the shot, and bind activation/transform clips where timing matters.

Storyboard frames are authoritative for blocking. If a storyboard panel shows the character, truck, fan, branch, sign, village silhouettes, hanged bodies, or a specific prop, the authored shot must contain those objects in-frame. Do not fill subtitle time by aiming a camera at an arbitrary scene coordinate. Existing scene objects should be reused when they already sit in the required position and are visible from the required angle, for example the gameplay truck, trees, or hanged/dead NPCs. Duplicate or create cutscene-local objects only when the shot needs a different transform, pose, activation timing, prop variant, or isolation from gameplay changes.

`Assets/Cutscenes/CinemachineBlenderSettings.asset` contains Cut-montage custom blends from cutscene cameras to `CinemachineCamera Gameplay`. Existing entries use each camera name as `From`, `CinemachineCamera Gameplay` as `To`, `Style: 0`, and `Time: 0`. When adding, renaming, or duplicating any cutscene camera that can return to gameplay, add or preserve the matching custom blend entry. This includes storyboard/fade cameras and ordinary shot cameras.

The root director is inactive in hierarchy when the cutscene root is inactive, but the component itself remains enabled. Runtime services activate/deactivate the root.

## Recursive Storyboard Alignment

When a storyboard or screenshot sequence is provided, verify the authored timeline recursively from the first/top panel to the last/bottom panel. For each panel, compare the rendered or scene-view camera result against the storyboard before moving on:

- matched timeline time range and subtitle/audio line;
- camera position, height, angle, lens/FOV, and foreground/background framing;
- character/vehicle/prop presence, transform, scale, pose, and activation;
- silhouettes and readable story objects such as fans, branches, signs, hanged bodies, village markers, and truck parts;
- continuity with the previous and next panel.

If any panel differs meaningfully, fix that panel and restart the check from the first panel. Do not only inspect the changed panel; later fixes can disturb earlier composition through shared objects, activation tracks, or timeline bindings. A cutscene pass is done only after a complete top-to-bottom storyboard check has no meaningful mismatches, or after explicitly listing remaining differences that cannot be solved with available assets/time.

## Verification Checklist

- The active scene has exactly the expected new or changed `PlayableDirector` roots.
- If a cutscene root is a prefab instance, reusable shot-content changes live in the prefab asset, not only as scene overrides. Scene overrides are limited to scene-specific bindings/transforms and must be intentional.
- The director references the intended cloned timeline asset, not an unrelated source timeline.
- The relevant `GameSceneContext` field points to the correct director.
- Required finish signal name exists on a `SignalTrack` in the timeline.
- `CinematicFrameTrack` and `CinematicSubtitlesTrack` bindings still resolve through `CutsceneWindow.BindTimeline`.
- Storyboard/fade shots remain present and extended when the source cutscene used `CinemachineStoryboard`.
- New camera clips have no unintended gaps between them or against neighboring shots.
- Every new or changed shot has explicit blocking: required characters, truck/vehicle, fan, branch, signs, hanged bodies, village props, or other storyboard subjects are active, positioned, and framed for that clip.
- Existing scene objects are reused when they already match the shot composition; shot-specific duplicates or Timeline transform/activation tracks are used only when existing gameplay-scene positions, visibility, or state are not correct for the storyboard.
- Every cutscene camera that returns to `CinemachineCamera Gameplay` has a Cut-montage custom blend in `Assets/Cutscenes/CinemachineBlenderSettings.asset`.
- Console has no new errors after import/recompile or scene save.
- Scene/prefab/timeline changes are made through Unity MCP unless the user explicitly approved serialized-file fallback.
