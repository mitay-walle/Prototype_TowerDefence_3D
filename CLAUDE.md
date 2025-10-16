# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a 3D tower defense prototype built in Unity using the Universal Render Pipeline (URP). The game features **procedural generation** for levels, roads, and towers using a voxel-based aesthetic with greedy mesh optimization.

**Unity Version:** 2022.3+ (using URP 17.2.0)
**Namespace:** `TD` (Tower Defense)
**Primary Scene:** `Assets/Scenes/Gameplay.unity`

## Core Architecture

### 1. Procedural Generation System

The generation system is built around a base class architecture:

- **`GenerationProfile.cs`** - Abstract base class for all procedural generation
- **`VoxelGenerator.cs`** - Core engine that converts voxel data into optimized 3D meshes using greedy meshing

**Generation Profiles:**
- **`LevelRoadGenerationProfile.cs`** (538 lines) - Generates terrain with noise (Perlin/Voronoi/Random) and roads with multiple path types (Straight, Loop, S-Curve, Branch)
- **`TurretGenerationProfile.cs`** (537 lines) - Generates complex tower models with 9+ component types (base, body, barrels, sensors, power cores, armor, etc.)
- **`TerrainTileGenerationProfile.cs`** - Tile-based terrain generation
- **`MonsterGenerationProfile.cs`** - Enemy generation (implementation pending)

**Key Classes:**
- **`VoxelData`** - Defines position, size, color index, emission properties
- **`Part`** - Named collection of voxels (e.g., "Base", "Turret", "Terrain", "Road")
- **`MaterialKey`** - Hashable material definition for mesh combining

**How it works:**
1. Profile's `Generate()` method creates `Part` objects containing lists of `VoxelData`
2. `VoxelGenerator` converts voxels to meshes using greedy meshing algorithm (merges adjacent voxels)
3. Meshes are optimized by material type and combined
4. Result is instantiated as GameObjects with proper hierarchy

### 2. Tower Placement System

- **`TowerPlacementSystem.cs`** (126 lines) - Handles placement mechanics
  - Grid-based snapping (rounds X/Z coordinates)
  - Ghost preview with collision detection
  - Supports mouse (LMB) and gamepad (button south) input
  - Validates placement via `TriggerIntersectColor` component
  - Raycasts against `groundMask` layer

- **`TowerShopUI.cs`** (128 lines) - UI for selecting and purchasing towers
  - Auto-generates tower preview icons via `TowerPreviewGenerator`
  - Hotkey support and gamepad navigation
  - Integrates with `DisablerList` to pause camera during UI interaction

### 3. Selection & Targeting System

- **`SelectionSystem.cs`** (186 lines) - Unified selection for all targetable objects
  - Mouse: screen-point raycasting
  - Gamepad: center-screen raycasting
  - Visual feedback via rendering layers: Default → Hovered (layer 2) → Selected (layer 4)
  - Works with `ITargetable` interface

- **`ITargetable.cs`** - Interface with `OnSelected()` / `OnDeselected()` callbacks

### 4. Camera System

- **`RTSCameraController.cs`** (245 lines) - RTS-style camera with Cinemachine
  - Mouse: edge scrolling, right-click drag, scroll wheel zoom
  - Gamepad: left stick movement, right stick zoom
  - Keyboard: WASD movement
  - Auto-detects input method and locks cursor for gamepad

### 5. UI Systems

- **`ButtonSector.cs`** (169 lines) - Extended UI Button with spatial navigation
  - Finds adjacent buttons by world position for radial menus
  - Supports wrap-around and distance-weighted selection

- **`TriggerIntersectColor.cs`** - Changes ghost material color when placement is invalid

### 6. Enemy/Path Systems (Stub)

- **`MoveToBase.cs`** - Placeholder for enemy pathfinding
- **`Base.cs`** - Placeholder for player base/destination

NavMesh is configured and ready (`Assets/Scenes/Gameplay/NavMesh-NavMesh Surface.asset`).

## Development Workflow

### Iterating on Procedural Generation

1. Open the prefab or scene containing a `VoxelGenerator` component
2. Modify the assigned `GenerationProfile` properties in the Inspector
3. Use **Odin Inspector buttons** to regenerate:
   - Click "Generate" button to regenerate with current settings
   - Use ">" button to increment seed and regenerate
4. Changes are visible immediately in the editor

### Adding New Generation Types

Extend `GenerationProfile`:
```csharp
public class MyGenerationProfile : GenerationProfile
{
    public override void Generate()
    {
        var part = new Part("MyPart");
        // Add VoxelData to part.Voxels list
        parts = new[] { part };
    }
}
```

Assign to a `VoxelGenerator.profile` field and click Generate.

### Adding New Towers

1. Create a new tower prefab with generation profile
2. Add to `TowerShopUI.towers` list
3. System auto-generates preview icons and handles placement

### Testing Placement & Selection

1. Open `Assets/Scenes/Gameplay.unity`
2. Enter Play mode
3. Camera: WASD/mouse edge scroll to move, mouse wheel to zoom
4. Open tower shop (check hotkey in `TowerShopUI`)
5. Select tower and place with LMB or gamepad button
6. Test selection system by clicking on placed objects

## Key Unity Packages

- **URP** (17.2.0) - Universal Render Pipeline for rendering
- **Cinemachine** (3.1.4) - Camera control
- **Input System** (1.14.2) - New Unity Input System (action-based)
- **AI Navigation** (2.0.9) - NavMesh for pathfinding
- **ProBuilder** (6.0.7) - Level building tools
- **Splines** (2.8.2) - Path generation support
- **Odin Inspector** (3rd party) - Enhanced editor tools
- **URP Outline** (custom) - Object outlining for selection
- **Unified Universal Blur** (custom) - UI blur effects

## Important Layer & Tag Setup

**Layers:**
- **groundMask** - Used by `TowerPlacementSystem` for raycasting valid placement surfaces

**Rendering Layers:**
- Default (0) - Normal rendering
- Hovered (2) - Objects under cursor/crosshair
- Selected (4) - Currently selected objects

## Project Structure Notes

```
Assets/
├── Scripts/           # All C# gameplay scripts
├── Prefabs/           # Reusable GameObjects (Turret, LevelRoad, TerrainTile)
├── Scenes/            # Gameplay.unity is the main scene
├── Rendering/         # URP configuration assets
├── Materials/         # Voxel materials with emission support
├── Plugins/Sirenix/   # Odin Inspector (3rd party editor tools)
├── Resources/         # Runtime-loadable assets
└── Input/             # Input Action assets
```

## Common Issues & Solutions

### Voxel Meshes Not Appearing
- Check if `VoxelGenerator.profile` is assigned
- Click "Generate" button in Inspector
- Verify materials are assigned in generation profile

### Tower Placement Not Working
- Ensure ground objects have correct layer set in `groundMask`
- Check `TowerPlacementSystem.ghostPrefab` is assigned
- Verify Input System actions are bound correctly

### Selection Not Working
- Objects must implement `ITargetable` interface
- Check rendering layer mask on camera and renderer
- Verify `SelectionSystem` is active in scene

### Camera Not Responding
- Check if `DisablerList` has paused the camera (happens during UI interaction)
- Verify Input System is enabled in Project Settings
- Check Cinemachine virtual camera is active

## Input System

Uses **Unity's New Input System** (action-based, not legacy):
- Input actions defined in `Assets/Input/` folder
- Supports simultaneous mouse, keyboard, and gamepad input
- Actions referenced by components (e.g., `InputActionReference` in placement system)
- Automatically detects input method and adapts UI/camera behavior

## Rendering Pipeline

- **Color Space:** Linear
- **Pipeline:** URP with custom outline and blur features
- **Voxel Rendering:** Custom shader with emission support
- **Optimization:** Greedy meshing combines adjacent voxels into larger quads/boxes

## Recent Development Focus

Based on recent commits, active work includes:
- Tower placement refinements
- Selection wheel UI with blur effects
- Gamepad support for outline/selection camera
- Turret system improvements
- Level road generation with NavMesh integration

## Code Style Notes

- Uses **Odin Inspector attributes** extensively (`[Button]`, `[ShowIf]`, etc.)
- Generation code uses **fluent/builder patterns** for voxel construction
- Input handling checks multiple sources and auto-detects active method
- Editor-time generation supported via custom Inspector buttons
- используй принципы программирования SOLID, в разумных пределах
- не пиши комментарии в коде
- удаляй [Header("Header")]
- перемещение и переименование нужно делать функциями Unity, иначе будут пропадать ссылки
- используй [Tooltip] где не очевидно, но выноси строку в константу
- не преоптимизируй, в первую очередь замыкай геймплейные лупы, это самое важное
- Используй Odin Inspector [Button] для кнопок с тестами
- сразу пиши if (Logs) Debug.Log() для отслеживания процессов в консоли
- не используй [BoxGroup]
- не использу вложенные классы и структуры
- автоматически  назначай ссылки и настраивай конфиги, создавай файлы настроек, префабы, сцены
- не пиши документацию, если я не просил прямым текстом
- вызывай рекомпиляцию вручную
- складывай все MenuItem для себя в папку Automation
- Tower вместо Turret и подобных в названиях
- .meta файл создается с помощью ImportAsset()
- Reflection только для доступа к unity-internal
- {get; private set;}
- используй TMP_Text TextMeshProUGUI вместо Text
- UniTask вместо Coroutine
- UGUI объекты вкладывай иерархически внутрь скриптов, которым они пренадлежат
- используй скрипты из Plugins
- проверяй на наличие ошибок компиляции и  сразу исправляй их
- после редактирования кода вызывай TD/Automation/Force Recompile All
- вызывай MenuItem без кликов пользователя
- EditorApplication.ExecuteMenuItem для выполнения автоматического кода
- используй MCP чтобы вызывать ExecuteMenuItem