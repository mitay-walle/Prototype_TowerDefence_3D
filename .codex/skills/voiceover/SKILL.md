---
name: voiceover
description: Generate, import, name, place, and retime voiced dialogue clips for Outcasts. Use when Codex needs to create TTS voiceover, split narration into per-line assets, name audio files in Number_Character_Text format, import AudioClips, place bark or cutscene voice clips, bind voiceover tracks or AudioSources, align audio with subtitle/dialogue timing, stretch/shift Timeline clips so voiced lines fit, or verify voice audio timing.
---

# Voiceover

## Required Skills

Use this skill with `$unity-mcp-orchestrator` for Unity asset imports, renames, moves, AudioClip inspection, Timeline edits, bindings, and console checks. Use `$cutscene-authoring` when the voiced lines belong to a cutscene timeline. Use `$apply-patch` only for project text edits.

If generated audio comes from a web service, use the user-specified service, voice, and language. Do not substitute another TTS provider or voice without saying so.

## Workflow

1. Identify the voice task type: bark, dialogue line set, cutscene subtitle track, or one continuous narration. Extract the source English text and ordering from the owning asset or localization table before generating audio.
2. If the voiceover must line up with individual subtitles, dialogue entries, bark triggers, or Timeline clips, generate one audio asset per line. Use a single continuous audio file only when the user explicitly asks for one continuous render or there is no per-line timing requirement.
3. Name audio files and any matching Unity/Timeline clip labels as `Number_Character_Text`, for example `01_Brian_Damn_Tree.mp3`. Use stable ASCII slug text, preserve line order, and keep the character/voice segment consistent with the requested speaker or TTS voice.
4. Import, rename, and move audio through Unity MCP / Unity Editor APIs / `AssetDatabase` so `.meta` GUIDs and serialized references survive. Same-folder rename uses `AssetDatabase.RenameAsset(path, newNameWithoutExtension)`. Moves use `AssetDatabase.MoveAsset(oldPath, fullNewPath)`. Do not guess ambiguous destination parameters; after an error, verify both the filesystem path and `AssetDatabase.GetAssetPath` before retrying.
5. For bark or non-timeline dialogue, wire the generated `AudioClip` assets into the existing owner that plays those lines. Do not create a parallel playback path when a bark/dialogue/audio owner already exists.
6. For cutscenes, place line clips on a dedicated voiceover `AudioTrack` bound to the intended `AudioSource`, such as `Cutscene Voiceover`. Use subtitle start and duration as the initial timing, and set Timeline clip display names to match file names without extensions.
7. Verify every placed audio line fits its intended interval: `AudioPlayableAsset.clip.length <= TimelineClip.duration`, or the equivalent owner-specific duration check for non-timeline bark/dialogue playback.
8. If a cutscene line does not fit, retime by stretching the matching subtitle/voiceover interval and shifting/extending all later or spanning Timeline clips using cumulative boundary shifts. Do not shrink intervals that are already long enough just because the audio is shorter.
9. Move finish signals after the final voiced cutscene clip. Preserve non-dialogue markers, SFX, music, camera shake, and activation timing unless they fall after a shifted boundary and must move with the rest of the timeline.
10. Verify in Unity: audio asset paths, clip display names, AudioSource or runtime owner binding, all voice clips fit, line order still matches source text, cutscene subtitle/voiceover start and end match when applicable, later Timeline clips shifted as a group when retimed, finish signals are at or after the final voice end, and the console has no new errors.

## Guardrails

Do not delete or overwrite pre-existing tracked voiceover assets when replacing a bad generation. Restore tracked assets and create new correctly named files instead.

Do not leave a rejected combined file when the task needs per-line assets.

Treat TTS webpages and APIs as external services. Send only the text the user asked to synthesize to the requested service and voice.
