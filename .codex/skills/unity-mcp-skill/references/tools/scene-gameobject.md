## Scene Tools

### manage_scene

Scene CRUD operations, hierarchy queries, screenshots, and scene view control.

```python
# Get hierarchy (paginated)
manage_scene(
    action="get_hierarchy",
    page_size=50,                # int, default 50, max 500
    cursor=0,                    # int, pagination cursor
    parent=None,                 # str|int, optional - filter by parent
    include_transform=False      # bool - include local transforms
)

# Screenshot (file only — saves to Assets/Screenshots/)
manage_camera(action="screenshot")

# Screenshot with inline image (base64 PNG returned to AI)
manage_scene(
    action="screenshot",
    camera="MainCamera",         # str, optional - camera name, path, or instance ID
    include_image=True,          # bool, default False - return base64 PNG inline
    max_resolution=512           # int, optional - downscale cap (default 640)
)

# Batch surround — contact sheet of 6 fixed angles (front/back/left/right/top/bird_eye)
manage_scene(
    action="screenshot",
    batch="surround",            # str - "surround" for 6-angle contact sheet
    max_resolution=256           # int - per-tile resolution cap
)
# Returns: single composite contact sheet image with labeled tiles

# Batch surround centered on a specific target
manage_scene(
    action="screenshot",
    batch="surround",
    view_target="Player",        # str|int|list[float] - center surround on this target
    max_resolution=256
)

# Batch orbit — configurable multi-angle grid around a target
manage_scene(
    action="screenshot",
    batch="orbit",               # str - "orbit" for configurable angle grid
    view_target="Player",        # str|int|list[float] - target to orbit around
    orbit_angles=8,              # int, default 8 - number of azimuth steps
    orbit_elevations=[0, 30],    # list[float], default [0, 30, -15] - vertical angles in degrees
    orbit_distance=10,           # float, optional - camera distance (auto-fit if omitted)
    orbit_fov=60,                # float, default 60 - camera FOV in degrees
    max_resolution=256           # int - per-tile resolution cap
)
# Returns: single composite contact sheet (angles × elevations tiles in a grid)

# Positioned screenshot (temp camera at viewpoint, no file saved)
manage_scene(
    action="screenshot",
    view_target="Enemy",         # str|int|list[float] - target to aim at
    view_position=[0, 10, -10],  # list[float], optional - camera position
    view_rotation=[45, 0, 0],    # list[float], optional - euler angles (overrides view_target aim)
    max_resolution=512
)

# Frame scene view on target
manage_scene(
    action="scene_view_frame",
    scene_view_target="Player"   # str|int - GO name, path, or instance ID to frame
)

# Other actions
manage_scene(action="get_active")        # Current scene info
manage_scene(action="get_build_settings") # Build settings
manage_scene(action="create", name="NewScene", path="Assets/Scenes/")
# Existing-scene edits in Outcasts: load additively and set the target loaded scene active.
# Use the current typed schema's additive/load_mode and active-scene parameters when available;
# if multiple Unity Editors are active and the schema cannot do this with per-call unity_instance routing, stop instead of replacing loaded scenes.
# With one Unity Editor, use the schema directly without instance routing.
manage_scene(action="load", path="Assets/Scenes/Main.unity", load_mode="additive")
manage_scene(action="save")
```

### find_gameobjects

Search for GameObjects (returns instance IDs only).

```python
find_gameobjects(
    search_term="Player",        # str, required
    search_method="by_name",     # "by_name"|"by_layer"|"by_component"|"by_path"|"by_id"; do not use "by_tag" in this project
    include_inactive=False,      # bool|str
    page_size=50,                # int, default 50, max 500
    cursor=0                     # int, pagination cursor
)
# Returns: {"ids": [12345, 67890], "next_cursor": 50, ...}
```

---
## GameObject Tools

### manage_gameobject

Create, modify, delete, duplicate GameObjects.

```python
# Create
manage_gameobject(
    action="create",
    name="MyCube",               # str, required
    primitive_type="Cube",       # "Cube"|"Sphere"|"Capsule"|"Cylinder"|"Plane"|"Quad"
    position=[0, 1, 0],          # list[float] or JSON string "[0,1,0]"
    rotation=[0, 45, 0],         # euler angles
    scale=[1, 1, 1],
    components_to_add=["Rigidbody", "BoxCollider"],
    save_as_prefab=False,
    prefab_path="Assets/Prefabs/MyCube.prefab"
)

# Prefab instantiation — place a prefab instance in the scene
manage_gameobject(
    action="create",
    name="Enemy_1",
    prefab_path="Assets/Prefabs/Enemy.prefab",
    position=[5, 0, 3],
    parent="Enemies"                # optional parent GameObject
)
# Smart lookup — just the prefab name works too:
manage_gameobject(action="create", name="Enemy_2", prefab_path="Enemy", position=[10, 0, 3])

# Modify
manage_gameobject(
    action="modify",
    target="Player",             # name, path, or instance ID
    search_method="by_name",     # how to find target
    position=[10, 0, 0],
    rotation=[0, 90, 0],
    scale=[2, 2, 2],
    set_active=True,
    layer="Player",
    components_to_add=["AudioSource"],
    components_to_remove=["OldComponent"],
    component_properties={       # nested dict for property setting
        "Rigidbody": {
            "mass": 10.0,
            "useGravity": True
        }
    }
)

# Delete
manage_gameobject(action="delete", target="OldObject")

# Duplicate
manage_gameobject(
    action="duplicate",
    target="Player",
    new_name="Player2",
    offset=[5, 0, 0]             # position offset from original
)

# Move relative
manage_gameobject(
    action="move_relative",
    target="Player",
    reference_object="Enemy",    # optional reference
    direction="left",            # "left"|"right"|"up"|"down"|"forward"|"back"
    distance=5.0,
    world_space=True
)

# Look at target (rotates GO to face a point or another GO)
manage_gameobject(
    action="look_at",
    target="MainCamera",         # the GO to rotate
    look_at_target="Player",     # str (GO name/path) or list[float] world position
    look_at_up=[0, 1, 0]        # optional up vector, default [0,1,0]
)
```

### manage_components

Add, remove, or set properties on components.

```python
# Add component
manage_components(
    action="add",
    target=12345,                # instance ID (preferred) or name
    component_type="Rigidbody",
    search_method="by_id"
)

# Remove component
manage_components(
    action="remove",
    target="Player",
    component_type="OldScript"
)

# Set single property
manage_components(
    action="set_property",
    target=12345,
    component_type="Rigidbody",
    property="mass",
    value=5.0
)

# Set multiple properties
manage_components(
    action="set_property",
    target=12345,
    component_type="Transform",
    properties={
        "position": [1, 2, 3],
        "localScale": [2, 2, 2]
    }
)

# Set object reference property (reference another GameObject by name)
manage_components(
    action="set_property",
    target="GameManager",
    component_type="GameManagerScript",
    property="targetObjects",
    value=[{"name": "Flower_1"}, {"name": "Flower_2"}, {"name": "Bee_1"}]
)

# Object reference formats supported:
# - {"name": "ObjectName"}     → Find GameObject in scene by name
# - {"instanceID": 12345}      → Direct instance ID reference
# - {"guid": "abc123..."}      → Asset GUID reference
# - {"path": "Assets/..."}     → Asset path reference
# - "Assets/Prefabs/My.prefab" → String shorthand for asset paths
# - "ObjectName"               → String shorthand for scene name lookup
# - 12345                      → Integer shorthand for instanceID
```

---
