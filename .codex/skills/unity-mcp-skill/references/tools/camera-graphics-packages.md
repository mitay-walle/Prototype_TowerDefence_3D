## Camera Tools

### manage_camera

Unified camera management (Unity Camera + Cinemachine). Works without Cinemachine using basic Camera; unlocks presets, pipelines, and blending when Cinemachine is installed. Use `ping` to check availability.

**Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `action` | string | Yes | Action to perform (see categories below) |
| `target` | string | Sometimes | Target camera (name, path, or instance ID) |
| `search_method` | string | No | `by_id`, `by_name`, `by_path` |
| `properties` | dict \| string | No | Action-specific parameters |

**Screenshot parameters** (for `screenshot` and `screenshot_multiview` actions):

| Parameter | Type | Description |
|-----------|------|-------------|
| `capture_source` | string | `"game_view"` (default) or `"scene_view"` (editor viewport) |
| `view_target` | string\|int\|list | Target to focus on (GO name/path/ID or [x,y,z]). game_view: aims camera; scene_view: frames viewport |
| `camera` | string | Camera to capture from (defaults to Camera.main). game_view only |
| `include_image` | bool | Return base64 PNG inline (default false) |
| `max_resolution` | int | Downscale cap in px (default 640) |
| `batch` | string | `"surround"` (6 angles) or `"orbit"` (configurable grid). game_view only |
| `view_position` | list[float] | World position [x,y,z] to place camera. game_view only |
| `view_rotation` | list[float] | Euler rotation [x,y,z] (overrides view_target). game_view only |

**Actions by category:**

**Setup:**
- `ping` — Check Cinemachine availability and version
- `ensure_brain` — Ensure CinemachineBrain exists on main camera. Properties: `camera` (target camera), `defaultBlendStyle`, `defaultBlendDuration`
- `get_brain_status` — Get Brain state (active camera, blend status)

**Creation:**
- `create_camera` — Create camera with optional preset. Properties: `name`, `preset` (follow/third_person/freelook/dolly/static/top_down/side_scroller), `follow`, `lookAt`, `priority`, `fieldOfView`. Falls back to basic Camera without Cinemachine.

**Configuration:**
- `set_target` — Set Follow and/or LookAt targets. Properties: `follow`, `lookAt` (GO name/path/ID)
- `set_priority` — Set camera priority for Brain selection. Properties: `priority` (int)
- `set_lens` — Configure lens. Properties: `fieldOfView`, `nearClipPlane`, `farClipPlane`, `orthographicSize`, `dutch`
- `set_body` — Configure Body component (Cinemachine). Properties: `bodyType` (to swap), plus component-specific properties
- `set_aim` — Configure Aim component (Cinemachine). Properties: `aimType` (to swap), plus component-specific properties
- `set_noise` — Configure Noise (Cinemachine). Properties: `amplitudeGain`, `frequencyGain`

**Extensions (Cinemachine):**
- `add_extension` — Add extension. Properties: `extensionType` (CinemachineConfiner2D, CinemachineDeoccluder, CinemachineImpulseListener, CinemachineFollowZoom, CinemachineRecomposer, etc.)
- `remove_extension` — Remove extension by type. Properties: `extensionType`

**Control:**
- `list_cameras` — List all cameras with status
- `set_blend` — Configure default blend on Brain. Properties: `style` (Cut/EaseInOut/Linear/etc.), `duration`
- `force_camera` — Override Brain to use specific camera
- `release_override` — Release camera override

**Capture:**
- `screenshot` — Capture screenshot. Supports `capture_source="game_view"` (default, camera-based) or `"scene_view"` (editor viewport). game_view supports inline base64, batch surround/orbit, positioned capture. scene_view supports `view_target` for framing.
- `screenshot_multiview` — Shorthand for screenshot with batch='surround' and include_image=true.

**Examples:**

```python
# Check Cinemachine availability
manage_camera(action="ping")

# Create a third-person camera following the player
manage_camera(action="create_camera", properties={
    "name": "FollowCam", "preset": "third_person",
    "follow": "Player", "lookAt": "Player", "priority": 20
})

# Ensure Brain exists on main camera
manage_camera(action="ensure_brain")

# Configure body component
manage_camera(action="set_body", target="FollowCam", properties={
    "bodyType": "CinemachineThirdPersonFollow",
    "cameraDistance": 5.0, "shoulderOffset": [0.5, 0.5, 0]
})

# Set aim
manage_camera(action="set_aim", target="FollowCam", properties={
    "aimType": "CinemachineRotationComposer"
})

# Add camera shake
manage_camera(action="set_noise", target="FollowCam", properties={
    "amplitudeGain": 0.5, "frequencyGain": 1.0
})

# Set priority to make this the active camera
manage_camera(action="set_priority", target="FollowCam", properties={"priority": 50})

# Force a specific camera
manage_camera(action="force_camera", target="CinematicCam")

# Release override (return to priority-based selection)
manage_camera(action="release_override")

# Configure blend transitions
manage_camera(action="set_blend", properties={"style": "EaseInOut", "duration": 2.0})

# Add deoccluder extension
manage_camera(action="add_extension", target="FollowCam", properties={
    "extensionType": "CinemachineDeoccluder"
})

# Screenshot from a specific camera (game_view, default)
manage_camera(action="screenshot", camera="FollowCam", include_image=True, max_resolution=512)

# Scene View screenshot (captures editor viewport — gizmos, wireframes, grid)
manage_camera(action="screenshot", capture_source="scene_view", include_image=True)

# Scene View screenshot framed on a specific object
manage_camera(action="screenshot", capture_source="scene_view", view_target="Canvas", include_image=True)

# Multi-view screenshot (6-angle contact sheet)
manage_camera(action="screenshot_multiview", max_resolution=480)

# List all cameras
manage_camera(action="list_cameras")
```

**Tier system:**
- Tier 1 actions (ping, create_camera, set_target, set_lens, set_priority, list_cameras, screenshot, screenshot_multiview) work without Cinemachine — they fall back to basic Unity Camera.
- Tier 2 actions (ensure_brain, get_brain_status, set_body, set_aim, set_noise, add/remove_extension, set_blend, force_camera, release_override) require `com.unity.cinemachine`. If called without Cinemachine, they return an error with a fallback suggestion.

**Resource:** Read `mcpforunity://scene/cameras` for current camera state before modifying.

---
## Graphics Tools

### manage_graphics

Unified rendering and graphics management: volumes/post-processing, light baking, rendering stats, pipeline configuration, and URP renderer features. Requires URP or HDRP for volume/feature actions. Use `ping` to check pipeline status and available features.

**Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `action` | string | Yes | Action to perform (see categories below) |
| `target` | string | Sometimes | Target object name or instance ID |
| `effect` | string | Sometimes | Effect type name (e.g., `Bloom`, `Vignette`) |
| `properties` | dict | No | Action-specific properties to set |
| `parameters` | dict | No | Effect parameter values |
| `settings` | dict | No | Bake or pipeline settings |
| `name` | string | No | Name for created objects |
| `profile_path` | string | No | Asset path for VolumeProfile |
| `path` | string | No | Asset path (for `volume_create_profile`) |
| `position` | list[float] | No | Position [x,y,z] |

**Actions by category:**

**Status:**
- `ping` — Check render pipeline type, available features, and package status

**Volume (require URP/HDRP):**
- `volume_create` — Create a Volume GameObject with optional effects. Properties: `name`, `is_global` (default true), `weight` (0-1), `priority`, `profile_path` (existing profile), `effects` (list of effect defs)
- `volume_add_effect` — Add effect override to a Volume. Params: `target` (Volume GO), `effect` (e.g., "Bloom")
- `volume_set_effect` — Set effect parameters. Params: `target`, `effect`, `parameters` (dict of param name to value)
- `volume_remove_effect` — Remove effect override. Params: `target`, `effect`
- `volume_get_info` — Get Volume details (profile, effects, parameters). Params: `target`
- `volume_set_properties` — Set Volume component properties (weight, priority, isGlobal). Params: `target`, `properties`
- `volume_list_effects` — List all available volume effects for the active pipeline
- `volume_create_profile` — Create a standalone VolumeProfile asset. Params: `path`, `effects` (optional)

**Bake (Edit mode only):**
- `bake_start` — Start lightmap bake. Params: `async_bake` (default true)
- `bake_cancel` — Cancel in-progress bake
- `bake_status` — Check bake progress
- `bake_clear` — Clear baked lightmap data
- `bake_reflection_probe` — Bake a specific reflection probe. Params: `target`
- `bake_get_settings` — Get current Lightmapping settings
- `bake_set_settings` — Set Lightmapping settings. Params: `settings` (dict)
- `bake_create_light_probe_group` — Create a Light Probe Group. Params: `name`, `position`, `grid_size` [x,y,z], `spacing`
- `bake_create_reflection_probe` — Create a Reflection Probe. Params: `name`, `position`, `size` [x,y,z], `resolution`, `mode`, `hdr`, `box_projection`
- `bake_set_probe_positions` — Set Light Probe positions manually. Params: `target`, `positions` (array of [x,y,z])

**Stats:**
- `stats_get` — Get rendering counters (draw calls, batches, triangles, vertices, etc.)
- `stats_list_counters` — List all available ProfilerRecorder counters
- `stats_set_scene_debug` — Set Scene View debug/draw mode. Params: `mode`
- `stats_get_memory` — Get rendering memory usage

**Pipeline:**
- `pipeline_get_info` — Get active render pipeline info (type, quality level, asset paths)
- `pipeline_set_quality` — Switch quality level. Params: `level` (name or index)
- `pipeline_get_settings` — Get pipeline asset settings
- `pipeline_set_settings` — Set pipeline asset settings. Params: `settings` (dict)

**Features (URP only):**
- `feature_list` — List renderer features on the active URP renderer
- `feature_add` — Add a renderer feature. Params: `feature_type`, `name`, `material` (for full-screen effects)
- `feature_remove` — Remove a renderer feature. Params: `index` or `name`
- `feature_configure` — Set feature properties. Params: `index` or `name`, `properties` (dict)
- `feature_toggle` — Enable/disable a feature. Params: `index` or `name`, `active` (bool)
- `feature_reorder` — Reorder features. Params: `order` (list of indices)

**Examples:**

```python
# Check pipeline status
manage_graphics(action="ping")

# Create a global post-processing volume with Bloom and Vignette
manage_graphics(action="volume_create", name="PostProcessing", is_global=True,
    effects=[
        {"type": "Bloom", "parameters": {"intensity": 1.5, "threshold": 0.9}},
        {"type": "Vignette", "parameters": {"intensity": 0.4}}
    ])

# Add an effect to an existing volume
manage_graphics(action="volume_add_effect", target="PostProcessing", effect="ColorAdjustments")

# Configure effect parameters
manage_graphics(action="volume_set_effect", target="PostProcessing",
    effect="ColorAdjustments", parameters={"postExposure": 0.5, "saturation": 10})

# Get volume info
manage_graphics(action="volume_get_info", target="PostProcessing")

# List all available effects for the active pipeline
manage_graphics(action="volume_list_effects")

# Create a VolumeProfile asset
manage_graphics(action="volume_create_profile", path="Assets/Settings/MyProfile.asset",
    effects=[{"type": "Bloom"}, {"type": "Tonemapping"}])

# Start async lightmap bake
manage_graphics(action="bake_start", async_bake=True)

# Check bake progress
manage_graphics(action="bake_status")

# Create a Light Probe Group with a 3x2x3 grid
manage_graphics(action="bake_create_light_probe_group", name="ProbeGrid",
    position=[0, 1, 0], grid_size=[3, 2, 3], spacing=2.0)

# Create a Reflection Probe
manage_graphics(action="bake_create_reflection_probe", name="RoomProbe",
    position=[0, 2, 0], size=[10, 5, 10], resolution=256, hdr=True)

# Get rendering stats
manage_graphics(action="stats_get")

# Get memory usage
manage_graphics(action="stats_get_memory")

# Get pipeline info
manage_graphics(action="pipeline_get_info")

# Switch quality level
manage_graphics(action="pipeline_set_quality", level="High")

# List URP renderer features
manage_graphics(action="feature_list")

# Add a full-screen renderer feature
manage_graphics(action="feature_add", feature_type="FullScreenPassRendererFeature",
    name="NightVision", material="Assets/Materials/NightVision.mat")

# Toggle a feature off
manage_graphics(action="feature_toggle", index=0, active=False)

# Reorder features
manage_graphics(action="feature_reorder", order=[2, 0, 1])
```

**Resources:**
- `mcpforunity://scene/volumes` — Lists all Volume components in the scene with their profiles and effects
- `mcpforunity://rendering/stats` — Current rendering performance counters
- `mcpforunity://pipeline/renderer-features` — URP renderer features on the active renderer

---
## Package Tools

### manage_packages

Manage Unity packages: query, install, remove, embed, and configure registries. Install/remove trigger domain reload.

**Query Actions (read-only):**

| Action | Parameters | Description |
|--------|-----------|-------------|
| `list_packages` | — | List all installed packages (async, returns job_id) |
| `search_packages` | `query` | Search Unity registry by keyword (async, returns job_id) |
| `get_package_info` | `package` | Get details about a specific installed package |
| `list_registries` | — | List all scoped registries (names, URLs, scopes); immediate result |
| `ping` | — | Check package manager availability, Unity version, package count |
| `status` | `job_id` (required for list/search; optional for add/remove/embed) | Poll async job status; omit job_id to poll latest add/remove/embed job |

**Mutating Actions:**

| Action | Parameters | Description |
|--------|-----------|-------------|
| `add_package` | `package` | Install a package (name, name@version, git URL, or file: path) |
| `remove_package` | `package`, `force` (optional) | Remove a package; blocked if dependents exist unless `force=true` |
| `embed_package` | `package` | Generic tool only; prohibited in this project because packages must not be edited |
| `resolve_packages` | — | Force re-resolution of all packages |
| `add_registry` | `name`, `url`, `scopes` | Add a scoped registry (e.g., OpenUPM) |
| `remove_registry` | `name` or `url` | Remove a scoped registry |

**Input validation:**
- Valid package IDs: `com.unity.inputsystem`, `com.unity.cinemachine@3.1.6`
- Git URLs: allowed with warning ("ensure this is a trusted source")
- `file:` paths: allowed with warning
- Invalid names (uppercase, missing dots): rejected

**Example — List installed packages:**
```python
manage_packages(action="list_packages")
# Returns job_id, then poll:
manage_packages(action="status", job_id="<job_id>")
```

**Example — Search for a package:**
```python
manage_packages(action="search_packages", query="input system")
```

**Example — Install a package:**
```python
manage_packages(action="add_package", package="com.unity.inputsystem")
# Poll until complete:
manage_packages(action="status", job_id="<job_id>")
```

**Example — Remove with dependency check:**
```python
manage_packages(action="remove_package", package="com.unity.modules.ui")
# Error: "Cannot remove: 3 package(s) depend on it: ..."
manage_packages(action="remove_package", package="com.unity.modules.ui", force=True)
# Proceeds anyway
```

**Example — Add OpenUPM registry:**
```python
manage_packages(
    action="add_registry",
    name="OpenUPM",
    url="https://package.openupm.com",
    scopes=["com.cysharp", "com.neuecc"]
)
```

---
