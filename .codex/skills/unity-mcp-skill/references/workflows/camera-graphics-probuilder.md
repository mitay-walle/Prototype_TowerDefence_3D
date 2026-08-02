## Camera & Cinemachine Workflows

### Setting Up a Third-Person Camera

```python
# 1. Check Cinemachine availability
manage_camera(action="ping")

# 2. Ensure Brain on main camera
manage_camera(action="ensure_brain")

# 3. Create third-person camera with preset
manage_camera(action="create_camera", properties={
    "name": "FollowCam", "preset": "third_person",
    "follow": "Player", "lookAt": "Player", "priority": 20
})

# 4. Fine-tune body
manage_camera(action="set_body", target="FollowCam", properties={
    "cameraDistance": 5.0, "shoulderOffset": [0.5, 0.5, 0]
})

# 5. Add camera shake
manage_camera(action="set_noise", target="FollowCam", properties={
    "amplitudeGain": 0.3, "frequencyGain": 0.8
})

# 6. Verify with screenshot
manage_camera(action="screenshot", camera="FollowCam", include_image=True, max_resolution=512)
```

### Multi-Camera Setup with Blending

```python
# 1. Read current cameras
# Read mcpforunity://scene/cameras

# 2. Create gameplay camera (highest priority = active by default)
manage_camera(action="create_camera", properties={
    "name": "GameplayCam", "preset": "follow",
    "follow": "Player", "lookAt": "Player", "priority": 10
})

# 3. Create cinematic camera (lower priority, activated on demand)
manage_camera(action="create_camera", properties={
    "name": "CinematicCam", "preset": "dolly",
    "lookAt": "CutsceneTarget", "priority": 5
})

# 4. Set blend transition
manage_camera(action="set_blend", properties={"style": "EaseInOut", "duration": 2.0})

# 5. Force cinematic camera for a cutscene
manage_camera(action="force_camera", target="CinematicCam")

# 6. Release override to return to priority-based selection
manage_camera(action="release_override")
```

### Camera Without Cinemachine

```python
# Tier 1 actions work with plain Unity Camera
manage_camera(action="create_camera", properties={
    "name": "MainCam", "fieldOfView": 50
})

# Set lens
manage_camera(action="set_lens", target="MainCam", properties={
    "fieldOfView": 60, "nearClipPlane": 0.1, "farClipPlane": 1000
})

# Point camera at target (uses manage_gameobject look_at under the hood)
manage_camera(action="set_target", target="MainCam", properties={
    "lookAt": "Player"
})

# Screenshot from this camera
manage_camera(action="screenshot", camera="MainCam", include_image=True, max_resolution=512)
```

### Camera Inspection Workflow

```python
# 1. Read all cameras via resource
# Read mcpforunity://scene/cameras
# → Shows brain status, all Cinemachine cameras (priority, pipeline, targets),
#   all Unity cameras (FOV, depth, brain)

# 2. Get brain status for blending info
manage_camera(action="get_brain_status")

# 3. List cameras via tool (alternative to resource)
manage_camera(action="list_cameras")

# 4. Multi-view screenshot to see from different angles
manage_camera(action="screenshot_multiview", max_resolution=480)
```

### Scene View Screenshot Workflow

Use `capture_source="scene_view"` to capture the editor's Scene View viewport — useful for seeing gizmos, wireframes, grid, debug overlays, and objects without cameras.

```python
# 1. Capture the Scene View as-is
manage_camera(action="screenshot", capture_source="scene_view", include_image=True)

# 2. Frame on a specific object first, then capture
manage_camera(action="screenshot", capture_source="scene_view",
    view_target="Player", include_image=True, max_resolution=512)

# 3. Frame on UI Canvas (RectTransform bounds are supported)
manage_camera(action="screenshot", capture_source="scene_view",
    view_target="Canvas", include_image=True)

# Limitations: scene_view does not support batch, view_position, view_rotation, or camera selection.
# Use capture_source="game_view" (default) for those features.
```

---
## ProBuilder Workflows

When `com.unity.probuilder` is installed, prefer ProBuilder shapes over primitive GameObjects for any geometry that needs editing, multi-material faces, or non-trivial shapes. Check availability first with `manage_probuilder(action="ping")`.

Keep ProBuilder examples in this file; this is the project ProBuilder workflow reference.

### ProBuilder vs Primitives Decision

| Need | Use Primitives | Use ProBuilder |
|------|---------------|----------------|
| Simple placeholder cube | `manage_gameobject(action="create", primitive_type="Cube")` | - |
| Editable geometry | - | `manage_probuilder(action="create_shape", ...)` |
| Per-face materials | - | `set_face_material` |
| Custom shapes (L-rooms, arches) | - | `create_poly_shape` or `create_shape` |
| Mesh editing (extrude, bevel) | - | Face/edge/vertex operations |
| Batch environment building | Either | ProBuilder + `batch_execute` |

### Basic ProBuilder Scene Build

```python
# 1. Check ProBuilder availability
manage_probuilder(action="ping")

# 2. Create shapes (use batch for multiple)
batch_execute(commands=[
    {"tool": "manage_probuilder", "params": {
        "action": "create_shape",
        "properties": {"shape_type": "Cube", "name": "Floor", "width": 20, "height": 0.2, "depth": 20}
    }},
    {"tool": "manage_probuilder", "params": {
        "action": "create_shape",
        "properties": {"shape_type": "Cube", "name": "Wall1", "width": 20, "height": 3, "depth": 0.3,
                       "position": [0, 1.5, 10]}
    }},
    {"tool": "manage_probuilder", "params": {
        "action": "create_shape",
        "properties": {"shape_type": "Cylinder", "name": "Pillar1", "radius": 0.4, "height": 3,
                       "position": [5, 1.5, 5]}
    }},
])

# 3. Edit geometry (always get_mesh_info first!)
info = manage_probuilder(action="get_mesh_info", target="Wall1",
    properties={"include": "faces"})
# Find direction="front" face, subdivide it, delete center for a window

# 4. Apply materials per face
manage_probuilder(action="set_face_material", target="Floor",
    properties={"faceIndices": [0], "materialPath": "Assets/Materials/Stone.mat"})

# 5. Smooth organic shapes
manage_probuilder(action="auto_smooth", target="Pillar1",
    properties={"angleThreshold": 45})

# 6. Screenshot to verify
manage_camera(action="screenshot", include_image=True, max_resolution=512)
```

### Edit-Verify Loop Pattern

Face indices change after every edit. Always re-query:

```python
# WRONG: Assume face indices are stable
manage_probuilder(action="subdivide", target="Obj", properties={"faceIndices": [2]})
manage_probuilder(action="delete_faces", target="Obj", properties={"faceIndices": [5]})  # Index may be wrong!

# RIGHT: Re-query after each edit
manage_probuilder(action="subdivide", target="Obj", properties={"faceIndices": [2]})
info = manage_probuilder(action="get_mesh_info", target="Obj", properties={"include": "faces"})
# Find the correct face by direction/center, then delete
manage_probuilder(action="delete_faces", target="Obj", properties={"faceIndices": [correct_index]})
```

### Known Limitations

- **`set_pivot`**: Broken -- vertex positions don't persist through mesh rebuild. Use `center_pivot` or Transform positioning.
- **`convert_to_probuilder`**: Broken -- MeshImporter throws. Create shapes natively with `create_shape`/`create_poly_shape`.
- **`subdivide`**: Uses `ConnectElements.Connect` (not traditional quad subdivision). Connects face midpoints.

---
## Graphics & Rendering Workflows

### Setting Up Post-Processing

Add post-processing effects to a URP/HDRP scene using Volumes.

```python
# 1. Check pipeline status and available effects
manage_graphics(action="ping")

# 2. List available volume effects for the active pipeline
manage_graphics(action="volume_list_effects")

# 3. Create a global post-processing volume with common effects
manage_graphics(action="volume_create", name="GlobalPostProcess", is_global=True,
    effects=[
        {"type": "Bloom", "parameters": {"intensity": 1.0, "threshold": 0.9, "scatter": 0.7}},
        {"type": "Vignette", "parameters": {"intensity": 0.35}},
        {"type": "Tonemapping", "parameters": {"mode": 1}},
        {"type": "ColorAdjustments", "parameters": {"postExposure": 0.2, "contrast": 10}}
    ])

# 4. Verify the volume was created
# Read mcpforunity://scene/volumes

# 5. Fine-tune an effect parameter
manage_graphics(action="volume_set_effect", target="GlobalPostProcess",
    effect="Bloom", parameters={"intensity": 1.5})

# 6. Screenshot to verify visual result
manage_camera(action="screenshot", include_image=True, max_resolution=512)
```

**Tips:**
- Always `ping` first to confirm URP/HDRP is active. Volumes do nothing on Built-in RP.
- Use `volume_list_effects` to discover available effect types for the active pipeline (URP and HDRP have different sets).
- Use `volume_get_info` to inspect current effect parameters before modifying.
- Create a reusable VolumeProfile asset with `volume_create_profile` and reference it via `profile_path` on multiple volumes.

### Adding a Full-Screen Effect via Renderer Features (URP)

Add a custom full-screen shader pass using URP Renderer Features.

```python
# 1. Check pipeline and confirm URP
manage_graphics(action="ping")

# 2. Create a material for the full-screen effect
manage_material(action="create",
    material_path="Assets/Materials/GrayscaleEffect.mat",
    shader="Shader Graphs/GrayscaleFullScreen")

# 3. List current renderer features
manage_graphics(action="feature_list")

# 4. Add a FullScreenPassRendererFeature with the material
manage_graphics(action="feature_add",
    feature_type="FullScreenPassRendererFeature",
    name="GrayscalePass",
    material="Assets/Materials/GrayscaleEffect.mat")

# 5. Verify it was added
manage_graphics(action="feature_list")

# 6. Toggle it on/off to compare
manage_graphics(action="feature_toggle", index=0, active=False)  # disable
manage_camera(action="screenshot", include_image=True, max_resolution=512)

manage_graphics(action="feature_toggle", index=0, active=True)   # re-enable
manage_camera(action="screenshot", include_image=True, max_resolution=512)

# 7. Reorder features if needed (execution order matters)
manage_graphics(action="feature_reorder", order=[1, 0, 2])
```

**Tips:**
- Renderer Features are URP-only. `feature_*` actions return an error on HDRP or Built-in RP.
- Read `mcpforunity://pipeline/renderer-features` to inspect features without modifying.
- Feature execution order affects the final image. Use `feature_reorder` to control pass ordering.

### Configuring Light Baking

Set up lightmaps, light probes, and reflection probes for baked GI.

```python
# 1. Set lights to Baked or Mixed mode
manage_components(action="set_property", target="Directional Light",
    component_type="Light", properties={"lightmapBakeType": 1})  # 1 = Mixed

# 2. Mark static objects for lightmapping
manage_gameobject(action="modify", target="Environment",
    component_properties={"StaticFlags": "ContributeGI"})

# 3. Configure lightmap settings
manage_graphics(action="bake_get_settings")
manage_graphics(action="bake_set_settings", settings={
    "lightmapper": 1,           # 1 = Progressive GPU
    "directSamples": 32,
    "indirectSamples": 128,
    "maxBounces": 4,
    "lightmapResolution": 40
})

# 4. Place light probes for dynamic objects
manage_graphics(action="bake_create_light_probe_group", name="MainProbeGrid",
    position=[0, 1.5, 0], grid_size=[5, 3, 5], spacing=3.0)

# 5. Place a reflection probe for an interior room
manage_graphics(action="bake_create_reflection_probe", name="RoomReflection",
    position=[0, 2, 0], size=[8, 4, 8], resolution=256,
    hdr=True, box_projection=True)

# 6. Start async bake
manage_graphics(action="bake_start", async_bake=True)

# 7. Poll bake status
manage_graphics(action="bake_status")
# Repeat until complete

# 8. Bake the reflection probe separately if needed
manage_graphics(action="bake_reflection_probe", target="RoomReflection")

# 9. Check rendering stats after bake
manage_graphics(action="stats_get")
```

**Tips:**
- Baking only works in Edit mode. If the editor is in Play mode, `bake_start` will fail.
- Use `bake_cancel` to abort a long bake.
- `bake_clear` removes all baked data (lightmaps, probes). Use before re-baking from scratch.
- For large scenes, use `async_bake=True` (default) and poll `bake_status` periodically.

---
