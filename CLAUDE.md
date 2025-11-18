# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 🔴 CRITICAL RULES (MUST ALWAYS FOLLOW)

1 **After editing code:** ALWAYS call `TD/Automation/Force Recompile All` via MenuItem
2 **Check compilation errors** immediately after recompile and fix them
3 **Use Unity rename/move functions** to preserve references (never manual file operations)
4 **Use MCP** to call `EditorApplication.ExecuteMenuItem` for automation
5 **DON'T create .asmdef**
6 **Wait for compilation and fix errors** to complete before checking for errors
7 **Never write comments** in code (NO comments anywhere)
8 **Use SOLID principles** within reasonable limits
9 **Use `UniTask` instead of Coroutine**
10 **Resources.Load<ComponentType>(string) for loading prefabs*
11 [Seriaizable] / [SerializeReference] вместо ScriptableObject

## ⚠️ MANDATORY CODE CONVENTIONS

### Attributes & Annotations
- Remove `[Header("Header")]` attributes
- Remove `[BoxGroup]` attributes  
- Remove `[Tooltip]`
- Remove all documentation comments
- Use Odin Inspector `[Button]` for test buttons

### Unity Components
- Use `TMP_Text` / `TextMeshProUGUI` instead of `Text`
- Use scripts from `Plugins` folder if need

### Code Patterns
- Close gameplay loops first - don't pre-optimize
- Use `{get; private set;}` pattern for properties
- UGUI objects must be nested hierarchically inside scripts (facade pattern)
- Use `if (Logs) Debug.Log()` for debugging/tracing
- Refactor nested classes and structs into separate files

- Reflection only for unity-internal access

### Naming Conventions
- Use "Tower" instead of "Turret" in all naming
- Store all MenuItem scripts in `Automation` folder

### Asset Management
- `.meta` files created via `ImportAsset()`
- Assign references and configure settings with MCP ExecuteMenuItem
- Create config files, prefabs, scenes with MCP ExecuteMenuItem

## 🔧 WORKFLOW REQUIREMENTS

### Compilation Process
1. Call `TD/Automation/Force Recompile All` with MCP ExecuteMenuItem
2. Wait for compilation to complete
3. Check for errors immediately
4. Fix any errors before proceeding

### Automation
- Use `EditorApplication.ExecuteMenuItem` to execute MenuItem without user clicks
- Use MCP to invoke ExecuteMenuItem programmatically
- Never ask user to click - automate everything possible

## 📋 PROJECT INFO

**Unity Version:** 6000.2.6+ (URP 17.2.0)  
**Namespace:** `TD` (Tower Defense)  
**Primary Scene:** `Assets/Scenes/Gameplay.unity`

### Key Packages
- URP (17.2.0) - Universal Render Pipeline
- Cinemachine (3.1.4) - Camera control
- Input System (1.14.2) - New Unity Input System
- AI Navigation (2.0.9) - NavMesh
- Odin Inspector (3rd party) - Editor tools
- UniTask - Async operations