## Asset Management Workflows

### Create and Apply Material

```python
# 1. Create material
manage_material(
    action="create",
    material_path="Assets/Materials/PlayerMaterial.mat",
    shader="Standard",
    properties={
        "_Color": [0.2, 0.5, 1.0, 1.0],
        "_Metallic": 0.5,
        "_Glossiness": 0.8
    }
)

# 2. Assign to renderer
manage_material(
    action="assign_material_to_renderer",
    target="Player",
    material_path="Assets/Materials/PlayerMaterial.mat",
    slot=0
)

# 3. Verify visually
manage_camera(action="screenshot")
```

### Create Procedural Texture

```python
# 1. Create base texture
manage_texture(
    action="create",
    path="Assets/Textures/Checkerboard.png",
    width=256,
    height=256,
    fill_color=[255, 255, 255, 255]
)

# 2. Apply checkerboard pattern
manage_texture(
    action="apply_pattern",
    path="Assets/Textures/Checkerboard.png",
    pattern="checkerboard",
    palette=[[0, 0, 0, 255], [255, 255, 255, 255]],
    pattern_size=32
)

# 3. Create material with texture
manage_material(
    action="create",
    material_path="Assets/Materials/CheckerMaterial.mat",
    shader="Standard"
)

# 4. Assign texture to material (via manage_material set_material_shader_property)
```

### Organize Assets into Folders

Folder rule: create category folders for groups of assets, not one folder for one file whose name repeats the folder concept. Prefer `Assets/Editor/Foo.cs`, `Assets/Materials/Foo.mat`, or `Assets/Prefabs/Foo.prefab` over `Assets/Editor/Foo/Foo.cs`, `Assets/Materials/Foo/Foo.mat`, or `Assets/Prefabs/Foo/Foo.prefab` unless several related files will share that folder or the user explicitly asks for it.

```python
# 1. Create folder structure
batch_execute(commands=[
    {"tool": "manage_asset", "params": {"action": "create_folder", "path": "Assets/Prefabs"}},
    {"tool": "manage_asset", "params": {"action": "create_folder", "path": "Assets/Materials"}},
    {"tool": "manage_asset", "params": {"action": "create_folder", "path": "Assets/Scripts"}},
    {"tool": "manage_asset", "params": {"action": "create_folder", "path": "Assets/Textures"}}
])

# 2. Move existing assets
manage_asset(action="move", path="Assets/MyMaterial.mat", destination="Assets/Materials/MyMaterial.mat")
manage_asset(action="move", path="Assets/MyScript.cs", destination="Assets/Scripts/MyScript.cs")
```

### Search and Process Assets

```python
# Find all prefabs
result = manage_asset(
    action="search",
    path="Assets",
    search_pattern="*.prefab",
    page_size=50,
    generate_preview=False
)

# Process each prefab
for asset in result["assets"]:
    prefab_path = asset["path"]
    # Get prefab info
    info = manage_prefabs(action="get_info", prefab_path=prefab_path)
    print(f"Prefab: {prefab_path}, Children: {info['childCount']}")
```

### Instantiate Prefab in Scene

Use `manage_gameobject` (not `manage_prefabs`) to place prefab instances in the scene.

```python
# Full path
manage_gameobject(
    action="create",
    name="Enemy_1",
    prefab_path="Assets/Prefabs/Enemy.prefab",
    position=[5, 0, 3],
    parent="Enemies"
)

# Smart lookup — just the prefab name works too
manage_gameobject(action="create", name="Enemy_2", prefab_path="Enemy", position=[10, 0, 3])

# Batch-spawn multiple instances
batch_execute(commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": f"Enemy_{i}",
        "prefab_path": "Enemy", "position": [i * 3, 0, 0], "parent": "Enemies"
    }}
    for i in range(5)
])
```

> **Note:** `manage_prefabs` is for headless prefab editing (inspect, modify contents, create from GameObject). To *instantiate* a prefab into the scene, always use `manage_gameobject(action="create", prefab_path="...")`.

---
## Testing Workflows

Use `$test-writing` for Unity test authoring, helper placement, execution workflow, and result reporting rules. Consult this MCP reference only for exact `run_tests` / `get_test_job` parameter details when `$test-writing` needs them.

---
## Debugging Workflows
### Diagnose Compilation Errors

```python
# 1. Check console for errors
errors = read_console(
    types=["error"],
    count=20,
    include_stacktrace=True,
    format="detailed"
)

# 2. For each error, find the file and line
for error in errors["messages"]:
    # Parse error message for file:line info
    # Use find_in_file to locate the problematic code
    pass

# 3. After fixing, refresh and check again
refresh_unity(mode="force", scope="scripts", compile="request", wait_for_ready=True)
read_console(types=["error"], count=10)
```

### Investigate Missing References

```python
# 1. Find the GameObject
result = find_gameobjects(search_term="Player", search_method="by_name")

# 2. Get all components
# Read mcpforunity://scene/gameobject/{id}/components

# 3. Check for null references in serialized fields
# Look for fields with null/missing values

# 4. Find the referenced object
result = find_gameobjects(search_term="Target", search_method="by_name")

# 5. Set the reference
manage_components(
    action="set_property",
    target="Player",
    component_type="PlayerController",
    property="target",
    value={"instanceID": result["ids"][0]}  # Reference by ID
)
```

### Check Scene State

```python
# 1. Get hierarchy
hierarchy = manage_scene(action="get_hierarchy", page_size=100, include_transform=True)

# 2. Find objects at unexpected positions
for item in hierarchy["data"]["items"]:
    if item.get("transform", {}).get("position", [0,0,0])[1] < -100:
        print(f"Object {item['name']} fell through floor!")

# 3. Visual verification
manage_camera(action="screenshot")
```

---
