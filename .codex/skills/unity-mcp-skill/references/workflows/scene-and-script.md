## Scene Generator Build Workflow

### Fresh Scene Before Building

**Always start a generated scene build with `manage_scene(action="create")`** to get a clean empty scene. This avoids conflicts with existing default objects (Camera, Light) that would cause "already exists" errors when the execution plan tries to create its own.

```python
# Step 0: Create fresh empty scene (replaces current scene entirely)
manage_scene(action="create", name="MyGeneratedScene", path="Assets/Scenes/")

# Now proceed with the phased execution plan...
# Phase 1: Environment (camera, lights) — no conflicts
# Phase 2: Objects (GameObjects)
# Phase 3: Materials
# etc.
```

### Wiring Object References Between Components

After creating scripts and attaching components, use `set_property` to wire cross-references between GameObjects. Use the `{"name": "ObjectName"}` format to reference scene objects by name:

```python
# Wire a list of target GameObjects into a script's serialized field
manage_components(
    action="set_property",
    target="BeeManager",
    component_type="BeeManagerScript",
    property="targetObjects",
    value=[{"name": "Flower_1"}, {"name": "Flower_2"}, {"name": "Flower_3"}]
)
```

### Physics Requirements for Trigger-Based Interactions

When scripts use `OnTriggerEnter` / `OnTriggerStay` / `OnTriggerExit`, at least one of the two colliding objects **must** have a `Rigidbody` component. Common pattern:

```python
# Moving objects (bees, players) need Rigidbody for triggers to fire
batch_execute(commands=[
    {"tool": "manage_components", "params": {
        "action": "add", "target": "Bee_1", "component_type": "Rigidbody"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "Bee_1",
        "component_type": "Rigidbody",
        "properties": {"useGravity": false, "isKinematic": true}
    }}
])
```

### Script Overwrites with `manage_script(action="update")`

When a generated script needs to be rewritten (e.g., to add auto-wiring logic), use `update` instead of deleting and recreating:

```python
manage_script(
    action="update",
    path="Assets/Scripts/MyScript.cs",
    contents="using UnityEngine;\n\npublic class MyScript : MonoBehaviour { ... }"
)
# manage_script update auto-triggers import + compile — just wait and check console
# Read mcpforunity://editor/state → wait until is_compiling == false
read_console(types=["error"], count=10)
```

---
## Scene Creation Workflows

### Create Complete Scene from Scratch

```python
# 1. Create new scene
manage_scene(action="create", name="GameLevel", path="Assets/Scenes/")

# 2. Batch create environment objects
batch_execute(commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "Ground", "primitive_type": "Plane",
        "position": [0, 0, 0], "scale": [10, 1, 10]
    }},
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "Light", "primitive_type": "Cube"
    }},
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "Player", "primitive_type": "Capsule",
        "position": [0, 1, 0]
    }}
])

# 3. Add light component (delete cube mesh, add light)
manage_components(action="remove", target="Light", component_type="MeshRenderer")
manage_components(action="remove", target="Light", component_type="MeshFilter")
manage_components(action="remove", target="Light", component_type="BoxCollider")
manage_components(action="add", target="Light", component_type="Light")
manage_components(action="set_property", target="Light", component_type="Light",
    property="type", value="Directional")

# 4. Set up camera
manage_gameobject(action="modify", target="Main Camera", position=[0, 5, -10],
    rotation=[30, 0, 0])

# 5. Verify with screenshot
manage_camera(action="screenshot")

# 6. Save scene
manage_scene(action="save")
```

### Populate Scene with Grid of Objects

```python
# Create 5x5 grid of cubes using batch
commands = []
for x in range(5):
    for z in range(5):
        commands.append({
            "tool": "manage_gameobject",
            "params": {
                "action": "create",
                "name": f"Cube_{x}_{z}",
                "primitive_type": "Cube",
                "position": [x * 2, 0, z * 2]
            }
        })

# Execute in batches of 25
batch_execute(commands=commands[:25], parallel=True)
```

### Clone and Arrange Objects

```python
# Find template object
result = find_gameobjects(search_term="Template", search_method="by_name")
template_id = result["ids"][0]

# Duplicate in a line
for i in range(10):
    manage_gameobject(
        action="duplicate",
        target=template_id,
        new_name=f"Instance_{i}",
        offset=[i * 2, 0, 0]
    )
```

---
## Script Development Workflows

Before any script creation step, load and apply `$code-style`. C# file rule: create exactly one top-level type per `.cs` file. If a workflow needs multiple helper or wrapper types, split them into separate files named after each type before validating or compiling. New systems do not get migration scripts, conversion utilities, backfill logic, or migration assets unless the user explicitly requests migration.

### Create New Script and Attach

```python
# 1. Create script (automatically triggers import + compilation)
create_script(
    path="Assets/Scripts/EnemyAI.cs",
    contents='''using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 5f;
    public Transform target;

    void Update()
    {
        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
        }
    }
}'''
)

# 2. Wait for compilation to finish
# Read mcpforunity://editor/state → wait until is_compiling == false

# 3. Check for errors
console = read_console(types=["error"], count=10)
if console["messages"]:
    # Handle compilation errors
    print("Compilation errors:", console["messages"])
else:
    # 4. Attach to GameObject
    manage_gameobject(action="modify", target="Enemy", components_to_add=["EnemyAI"])

    # 5. Set component properties
    manage_components(
        action="set_property",
        target="Enemy",
        component_type="EnemyAI",
        properties={
            "speed": 10.0
        }
    )
```

### Edit Existing Script Safely

```python
# 1. Get current SHA
sha_info = get_sha(uri="mcpforunity://path/Assets/Scripts/PlayerController.cs")

# 2. Find the method to edit
matches = find_in_file(
    uri="mcpforunity://path/Assets/Scripts/PlayerController.cs",
    pattern="void Update\\(\\)"
)

# 3. Apply structured edit
script_apply_edits(
    name="PlayerController",
    path="Assets/Scripts",
    edits=[{
        "op": "replace_method",
        "methodName": "Update",
        "replacement": '''void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(h, 0, v) * speed * Time.deltaTime);
    }'''
    }]
)

# 4. Validate
validate_script(
    uri="mcpforunity://path/Assets/Scripts/PlayerController.cs",
    level="standard"
)

# 5. Wait for compilation (script_apply_edits auto-triggers import + compile)
# Read mcpforunity://editor/state → wait until is_compiling == false

# 6. Check console
read_console(types=["error"], count=10)
```

### Add Method to Existing Class

```python
script_apply_edits(
    name="GameManager",
    path="Assets/Scripts",
    edits=[
        {
            "op": "insert_method",
            "afterMethod": "Start",
            "code": '''
    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }'''
        },
        {
            "op": "anchor_insert",
            "anchor": "using UnityEngine;",
            "position": "after",
            "text": "\nusing UnityEngine.SceneManagement;"
        }
    ]
)
```

---
