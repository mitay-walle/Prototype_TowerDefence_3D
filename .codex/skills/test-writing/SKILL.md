---
name: test-writing
description: Write, update, place, and run Unity tests in this project. Use when adding EditMode or PlayMode tests, creating test helper components, moving test code, debugging Unity Test Runner failures, or reporting test results.
---

# Test Writing

## Core Workflow

1. Load `$code-style` before creating or editing C# test files.
2. Use `$apply-patch` for all test source edits.
3. Use `$unity-mcp-orchestrator` for Unity Test Runner, Editor state, imports, console reads, and MCP routing.
4. After C# test changes, run `$mcp-unity-validate-script`, then `$unity-recompile-menuitem` before running tests.
5. Run the narrowest useful test set first: changed test method, changed fixture, category, assembly, then broader suites only when risk justifies it.
6. Print or relay test results through the Unity console, MCP response, terminal stdout, or the final assistant response. Do not write test result files, XML reports, log dumps, or other result artifacts unless the user explicitly asks for files.

## Test Types

Use two practical test types in this project:

- Unit tests: verify a small code contract in isolation, such as a service, state, DTO, command, cost calculation, save rule, or component behavior with a minimal fixture. Prefer `new`, `ScriptableObject.CreateInstance`, temporary `GameObject` instances, and small test doubles. These tests should not depend on scenes, authored prefabs, broad AssetDatabase searches, or previous tests.
- Real object and prefab tests: verify that Unity-authored data still matches code expectations, such as a required asset path, prefab component, serialized field, graph blackboard variable, database entry, or config reference. Keep these tests narrow, load assets explicitly with useful failure messages, and read/verify asset state without modifying or saving assets.

Start with a unit test when proving logic. Add a real object or prefab test only for the authoring contract the unit test cannot cover. Avoid combining complex logic setup and real project assets in the same test unless the purpose is explicitly integration coverage; otherwise failures will not clearly identify whether code or authored data broke.

Real object and prefab checks usually belong in EditMode when AssetDatabase and serialized reads are enough. Use PlayMode only when the contract requires Unity runtime lifecycle, frames, physics, scene loading, or behavior that cannot be honestly verified in EditMode.

## Placement Rules

- Put EditMode tests under an Editor test folder only when every type in that file is editor-only and never attached to a GameObject.
- Any `MonoBehaviour` type must live outside folders named `Editor`, including test-only helper components. Unity Editor folders are for editor-only code and must not contain component types intended to be attached to GameObjects.
- If a test needs a helper `MonoBehaviour`, place that helper in a runtime-visible test support folder, keep it small, and instantiate it from the test with `GameObject.AddComponent<T>()`. Current Outcasts examples include folders such as `Assets/Tests/EditMode/Entities`, `SceneVariantTestSupport`, `StateMachineTestSupport`, and `UIManagerTestSupport`.
- Keep one top-level C# type per `.cs` file unless Unity/NUnit constraints make a local nested helper materially clearer. Do not mix production runtime types into test fixture files.

## Authoring Rules

- Prefer deterministic tests that build their own GameObjects, services, DTOs, and fixtures in setup, then destroy Unity objects in teardown.
- Avoid relying on scene state, asset database global searches, execution order side effects, or previous tests unless the test is explicitly an integration test for that behavior.
- Keep assertions specific enough to diagnose the broken contract. Include failure messages when setup is non-obvious.
- Do not add sleeps, frame delays, or polling loops unless the behavior under test is genuinely asynchronous or frame-driven. For async code, prefer UniTask-aware patterns consistent with `$unitask`.
- Do not weaken production code only to make a test pass. If a seam is needed, use existing project patterns: constructor parameters, interfaces, providers, or serialized references.

## Running And Reporting

- Use Unity MCP test tools when available: discover the relevant tests, run them with failure details, poll until completion, then read recent console errors.
- Keep output in the console/stdout path. If a tool returns structured test data, summarize it in the final response and include failing test names, messages, and stack locations.
- Do not redirect test output to files or leave generated result files in the workspace. If an external tool unavoidably creates a temporary result file, print the relevant result to console/stdout and delete the temporary file before finishing.
- Report exactly what ran, whether it passed, and any tests not run because Unity MCP, compilation, or Editor routing was unavailable.
- When multiple Codex chats or agents may be active, do not fix unrelated compile or test failures from other chats. Fix only failures caused by the current request or current-chat edits; report unrelated failures as external/shared-state blockers with the failing test names or console messages.
