## Physics Tools

### `manage_physics`

Manage 3D and 2D physics: settings, collision matrix, materials, joints, queries, validation, and simulation. All actions support `dimension="3d"` (default) or `dimension="2d"` where applicable.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `action` | string | Yes | See action groups below |
| `dimension` | string | No | `"3d"` (default) or `"2d"` |
| `settings` | object | For set_settings | Key-value physics settings dict |
| `layer_a` / `layer_b` | string | For collision matrix | Layer name or index |
| `collide` | bool | For set_collision_matrix | `true` to enable, `false` to disable |
| `name` | string | For create_physics_material | Material asset name |
| `path` | string | No | Asset folder path (create) or asset path (configure) |
| `dynamic_friction` / `static_friction` / `bounciness` | float | No | Material properties (0–1) |
| `friction_combine` / `bounce_combine` | string | No | `Average`, `Minimum`, `Multiply`, `Maximum` |
| `material_path` | string | For assign_physics_material | Path to physics material asset |
| `target` | string | For joints/queries/validate | GameObject name or instance ID |
| `joint_type` | string | For joints | 3D: `fixed`, `hinge`, `spring`, `character`, `configurable`; 2D: `distance`, `fixed`, `friction`, `hinge`, `relative`, `slider`, `spring`, `target`, `wheel` |
| `connected_body` | string | For add_joint | Connected body GameObject |
| `motor` / `limits` / `spring` / `drive` | object | For configure_joint | Joint sub-config objects |
| `properties` | object | For configure_joint/material | Direct property dict |
| `origin` / `direction` | float[] | For raycast | Ray origin and direction `[x,y,z]` or `[x,y]` |
| `max_distance` | float | No | Max raycast distance |
| `shape` | string | For overlap | `sphere`, `box`, `capsule` (3D); `circle`, `box`, `capsule` (2D) |
| `position` | float[] | For overlap | `[x,y,z]` or `[x,y]` |
| `size` | float or float[] | For overlap | Radius (sphere/circle) or half-extents `[x,y,z]` (box) |
| `layer_mask` | string | No | Layer name or int mask for queries |
| `start` / `end` | float[] | For linecast | Start and end points `[x,y,z]` or `[x,y]` |
| `point1` / `point2` | float[] | For shapecast capsule | Capsule endpoints (3D alternative) |
| `height` | float | For shapecast capsule | Capsule height |
| `capsule_direction` | int | For shapecast capsule | 0=X, 1=Y (default), 2=Z |
| `angle` | float | For 2D shapecasts | Rotation angle in degrees |
| `force` | float[] | For apply_force | Force vector `[x,y,z]` or `[x,y]` |
| `force_mode` | string | For apply_force | `Force`, `Impulse`, `Acceleration`, `VelocityChange` (3D); `Force`, `Impulse` (2D) |
| `force_type` | string | For apply_force | `normal` (default) or `explosion` (3D only) |
| `torque` | float[] | For apply_force | Torque `[x,y,z]` (3D) or `[z]` (2D) |
| `explosion_position` | float[] | For apply_force explosion | Explosion center `[x,y,z]` |
| `explosion_radius` | float | For apply_force explosion | Explosion sphere radius |
| `explosion_force` | float | For apply_force explosion | Explosion force magnitude |
| `upwards_modifier` | float | For apply_force explosion | Y-axis offset (default 0) |
| `steps` | int | For simulate_step | Number of steps (1–100) |
| `step_size` | float | No | Step size in seconds (default: `Time.fixedDeltaTime`) |

**Action groups:**

- **Settings:** `ping`, `get_settings`, `set_settings`
- **Collision Matrix:** `get_collision_matrix`, `set_collision_matrix`
- **Materials:** `create_physics_material`, `configure_physics_material`, `assign_physics_material`
- **Joints:** `add_joint`, `configure_joint`, `remove_joint`
- **Queries:** `raycast`, `raycast_all`, `linecast`, `shapecast`, `overlap`
- **Forces:** `apply_force`
- **Rigidbody:** `get_rigidbody`, `configure_rigidbody`
- **Validation:** `validate`
- **Simulation:** `simulate_step`

```python
# Check physics status
manage_physics(action="ping")

# Get/set gravity
manage_physics(action="get_settings", dimension="3d")
manage_physics(action="set_settings", dimension="3d", settings={"gravity": [0, -20, 0]})

# Collision matrix
manage_physics(action="get_collision_matrix")
manage_physics(action="set_collision_matrix", layer_a="Player", layer_b="Enemy", collide=False)

# Create a bouncy physics material and assign it
manage_physics(action="create_physics_material", name="Bouncy", bounciness=0.9, dynamic_friction=0.2)
manage_physics(action="assign_physics_material", target="Ball", material_path="Assets/Physics Materials/Bouncy.physicMaterial")

# Add and configure a hinge joint
manage_physics(action="add_joint", target="Door", joint_type="hinge", connected_body="DoorFrame")
manage_physics(action="configure_joint", target="Door", joint_type="hinge",
               motor={"targetVelocity": 90, "force": 100},
               limits={"min": -90, "max": 0, "bounciness": 0})

# Raycast and overlap
manage_physics(action="raycast", origin=[0, 10, 0], direction=[0, -1, 0], max_distance=50)
manage_physics(action="overlap", shape="sphere", position=[0, 0, 0], size=5.0)

# Validate scene physics setup
manage_physics(action="validate")                    # whole scene
manage_physics(action="validate", target="Player")  # single object

# Multi-hit raycast (returns all hits sorted by distance)
manage_physics(action="raycast_all", origin=[0, 10, 0], direction=[0, -1, 0])

# Linecast (point A to point B)
manage_physics(action="linecast", start=[0, 0, 0], end=[10, 0, 0])

# Shapecast (sphere/box/capsule sweep)
manage_physics(action="shapecast", shape="sphere", origin=[0, 5, 0], direction=[0, -1, 0], size=0.5)
manage_physics(action="shapecast", shape="box", origin=[0, 5, 0], direction=[0, -1, 0], size=[1, 1, 1])

# Apply force (works with simulate_step for edit-mode previewing)
manage_physics(action="apply_force", target="Ball", force=[0, 500, 0], force_mode="Impulse")
manage_physics(action="apply_force", target="Ball", torque=[0, 10, 0])

# Explosion force (3D only)
manage_physics(action="apply_force", target="Crate", force_type="explosion",
               explosion_force=1000, explosion_position=[0, 0, 0], explosion_radius=10)

# Configure rigidbody properties
manage_physics(action="configure_rigidbody", target="Player",
               properties={"mass": 80, "drag": 0.5, "useGravity": True, "collisionDetectionMode": "Continuous"})

# Step physics in edit mode
manage_physics(action="simulate_step", steps=10, step_size=0.02)
```

---

## ProBuilder Tools

### manage_probuilder

Unified tool for ProBuilder mesh operations. Requires `com.unity.probuilder` package. When available, **prefer ProBuilder over primitive GameObjects** for editable geometry, multi-material faces, or complex shapes.

**Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `action` | string | Yes | Action to perform (see categories below) |
| `target` | string | Sometimes | Target GameObject name/path/id |
| `search_method` | string | No | How to find target: `by_id`, `by_name`, `by_path`, `by_layer`. Unity also exposes `by_tag`, but project workflows must not use it. |
| `properties` | dict \| string | No | Action-specific parameters (dict or JSON string) |

**Actions by category:**

**Shape Creation:**
- `create_shape` — Create ProBuilder primitive (shape_type, size, position, rotation, name). 12 types: Cube, Cylinder, Sphere, Plane, Cone, Torus, Pipe, Arch, Stair, CurvedStair, Door, Prism
- `create_poly_shape` — Create from 2D polygon footprint (points, extrudeHeight, flipNormals)

**Mesh Editing:**
- `extrude_faces` — Extrude faces (faceIndices, distance, method: FaceNormal/VertexNormal/IndividualFaces)
- `extrude_edges` — Extrude edges (edgeIndices or edges [{a,b},...], distance, asGroup)
- `bevel_edges` — Bevel edges (edgeIndices or edges [{a,b},...], amount 0-1)
- `subdivide` — Subdivide faces via ConnectElements (faceIndices optional)
- `delete_faces` — Delete faces (faceIndices)
- `bridge_edges` — Bridge two open edges (edgeA, edgeB as {a,b} pairs, allowNonManifold)
- `connect_elements` — Connect edges/faces (edgeIndices or faceIndices)
- `detach_faces` — Detach faces to new object (faceIndices, deleteSourceFaces)
- `flip_normals` — Flip face normals (faceIndices)
- `merge_faces` — Merge faces into one (faceIndices)
- `combine_meshes` — Combine ProBuilder objects (targets list)
- `merge_objects` — Merge objects with auto-convert (targets, name)
- `duplicate_and_flip` — Create double-sided geometry (faceIndices)
- `create_polygon` — Connect existing vertices into a new face (vertexIndices, unordered)

**Vertex Operations:**
- `merge_vertices` — Collapse vertices to single point (vertexIndices, collapseToFirst)
- `weld_vertices` — Weld vertices within proximity radius (vertexIndices, radius)
- `split_vertices` — Split shared vertices (vertexIndices)
- `move_vertices` — Translate vertices (vertexIndices, offset [x,y,z])
- `insert_vertex` — Insert vertex on edge or face (edge {a,b} or faceIndex + point [x,y,z])
- `append_vertices_to_edge` — Insert evenly-spaced points on edges (edgeIndices or edges, count)

**Selection:**
- `select_faces` — Select faces by criteria (direction + tolerance, growFrom + growAngle)

**UV & Materials:**
- `set_face_material` — Assign material to faces (faceIndices, materialPath)
- `set_face_color` — Set vertex color on faces (faceIndices, color [r,g,b,a])
- `set_face_uvs` — Set UV params (faceIndices, scale, offset, rotation, flipU, flipV)

**Query:**
- `get_mesh_info` — Get mesh details with `include` parameter:
  - `"summary"` (default): counts, bounds, materials
  - `"faces"`: + face normals, centers, and direction labels (capped at 100)
  - `"edges"`: + edge vertex pairs with world positions (capped at 200, deduplicated)
  - `"all"`: everything
- `ping` — Check if ProBuilder is available

**Smoothing:**
- `set_smoothing` — Set smoothing group on faces (faceIndices, smoothingGroup: 0=hard, 1+=smooth)
- `auto_smooth` — Auto-assign smoothing groups by angle (angleThreshold: default 30)

**Mesh Utilities:**
- `center_pivot` — Move pivot to mesh bounds center
- `freeze_transform` — Bake transform into vertices, reset transform
- `validate_mesh` — Check mesh health (read-only diagnostics)
- `repair_mesh` — Auto-fix degenerate triangles

**Not Yet Working (known bugs):**
- `set_pivot` — Vertex positions don't persist through mesh rebuild. Use `center_pivot` or Transform positioning instead.
- `convert_to_probuilder` — MeshImporter throws internally. Create shapes natively instead.

**Examples:**

```python
# Check availability
manage_probuilder(action="ping")

# Create a cube
manage_probuilder(action="create_shape", properties={"shape_type": "Cube", "name": "MyCube"})

# Get face info with directions
manage_probuilder(action="get_mesh_info", target="MyCube", properties={"include": "faces"})

# Extrude the top face (find it via direction="top" in get_mesh_info results)
manage_probuilder(action="extrude_faces", target="MyCube",
    properties={"faceIndices": [2], "distance": 1.5})

# Select all upward-facing faces
manage_probuilder(action="select_faces", target="MyCube",
    properties={"direction": "up", "tolerance": 0.7})

# Create double-sided geometry (for room interiors)
manage_probuilder(action="duplicate_and_flip", target="Room",
    properties={"faceIndices": [0, 1, 2, 3, 4, 5]})

# Weld nearby vertices
manage_probuilder(action="weld_vertices", target="MyCube",
    properties={"vertexIndices": [0, 1, 2, 3], "radius": 0.1})

# Auto-smooth
manage_probuilder(action="auto_smooth", target="MyCube", properties={"angleThreshold": 30})

# Cleanup workflow
manage_probuilder(action="center_pivot", target="MyCube")
manage_probuilder(action="validate_mesh", target="MyCube")
```

For ProBuilder workflow patterns, use `../workflows/camera-graphics-probuilder.md`.

---

## Profiler Tools

### `manage_profiler`

Unity Profiler session control, counter reads, memory snapshots, and Frame Debugger. Group: `profiling` (opt-in via `manage_tools`).

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `action` | string | Yes | See action groups below |
| `category` | string | For get_counters | Profiler category name (e.g. `Render`, `Scripts`, `Memory`, `Physics`) |
| `counters` | list[str] | No | Specific counter names for get_counters. Omit to read all in category |
| `object_path` | string | For get_object_memory | Scene hierarchy or asset path |
| `log_file` | string | No | Path to `.raw` file for profiler_start recording |
| `enable_callstacks` | bool | No | Enable allocation callstacks for profiler_start |
| `areas` | dict[str, bool] | For profiler_set_areas | Area name to enabled/disabled mapping |
| `snapshot_path` | string | No | Output path for memory_take_snapshot |
| `search_path` | string | No | Search directory for memory_list_snapshots |
| `snapshot_a` | string | For memory_compare_snapshots | First snapshot file path |
| `snapshot_b` | string | For memory_compare_snapshots | Second snapshot file path |
| `page_size` | int | No | Page size for frame_debugger_get_events (default 50) |
| `cursor` | int | No | Cursor offset for frame_debugger_get_events |

**Action groups:**

- **Session:** `profiler_start`, `profiler_stop`, `profiler_status`, `profiler_set_areas`
- **Counters:** `get_frame_timing`, `get_counters`, `get_object_memory`
- **Memory Snapshot:** `memory_take_snapshot`, `memory_list_snapshots`, `memory_compare_snapshots` (requires `com.unity.memoryprofiler`)
- **Frame Debugger:** `frame_debugger_enable`, `frame_debugger_disable`, `frame_debugger_get_events`
- **Utility:** `ping`

```python
# Check profiler availability
manage_profiler(action="ping")

# Start profiling (optionally record to file)
manage_profiler(action="profiler_start")
manage_profiler(action="profiler_start", log_file="Assets/profiler.raw", enable_callstacks=True)

# Check profiler status
manage_profiler(action="profiler_status")

# Toggle profiler areas
manage_profiler(action="profiler_set_areas", areas={"CPU": True, "GPU": True, "Rendering": True, "Memory": False})

# Stop profiling
manage_profiler(action="profiler_stop")

# Read frame timing data (12 fields from FrameTimingManager)
manage_profiler(action="get_frame_timing")

# Read counters by category
manage_profiler(action="get_counters", category="Render")
manage_profiler(action="get_counters", category="Memory", counters=["Total Used Memory", "GC Used Memory"])

# Get memory size of a specific object
manage_profiler(action="get_object_memory", object_path="Player/Mesh")

# Memory snapshots (requires com.unity.memoryprofiler)
manage_profiler(action="memory_take_snapshot")
manage_profiler(action="memory_take_snapshot", snapshot_path="Assets/Snapshots/baseline.snap")
manage_profiler(action="memory_list_snapshots")
manage_profiler(action="memory_compare_snapshots", snapshot_a="Assets/Snapshots/before.snap", snapshot_b="Assets/Snapshots/after.snap")

# Frame Debugger
manage_profiler(action="frame_debugger_enable")
manage_profiler(action="frame_debugger_get_events", page_size=20, cursor=0)
manage_profiler(action="frame_debugger_disable")
```

---

## Docs Tools

Tools for verifying Unity C# APIs and fetching official documentation. Group: `docs`.

### `unity_reflect`

Inspect Unity's live C# API via reflection. **Always use this before writing C# code that references Unity APIs** — LLM training data frequently contains incorrect, outdated, or hallucinated APIs.

Requires Unity connection.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `action` | string | Yes | `search`, `get_type`, or `get_member` |
| `class_name` | string | For get_type, get_member | Fully qualified or simple C# class name |
| `member_name` | string | For get_member | Method, property, or field name to inspect |
| `query` | string | For search | Search query for type name search |
| `scope` | string | No | Assembly scope for search: `unity`, `packages`, `project`, `all` (default: `unity`) |

**Actions:**

- **`search`**: Search for types by name across loaded assemblies. Returns matching type names.
- **`get_type`**: Get a member summary (names only) for a class. Returns list of methods, properties, fields.
- **`get_member`**: Get full signature detail for one member. Returns parameter types, return type, overloads.

```python
# Search for types matching a name
unity_reflect(action="search", query="NavMesh")
unity_reflect(action="search", query="Camera", scope="all")

# Get all members of a type
unity_reflect(action="get_type", class_name="UnityEngine.AI.NavMeshAgent")

# Get detailed signature for a specific member
unity_reflect(action="get_member", class_name="Physics", member_name="Raycast")
unity_reflect(action="get_member", class_name="NavMeshAgent", member_name="SetDestination")
```

### `unity_docs`

Fetch official Unity documentation from docs.unity3d.com. Returns descriptions, parameter details, code examples, and caveats. Use after `unity_reflect` confirms a type exists.

No Unity connection needed for doc fetching. The `lookup` action with asset-related queries will also search project assets (requires Unity connection).

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `action` | string | Yes | `get_doc`, `get_manual`, `get_package_doc`, or `lookup` |
| `class_name` | string | For get_doc | Unity class name (e.g., `Physics`, `Transform`) |
| `member_name` | string | No | Method or property name for get_doc |
| `version` | string | No | Unity version (e.g., `6000.0.38f1`). Auto-extracts major.minor. |
| `slug` | string | For get_manual | Manual page slug (e.g., `execution-order`) |
| `package` | string | For get_package_doc, optional for lookup | Package name (e.g., `com.unity.render-pipelines.universal`) |
| `page` | string | For get_package_doc | Package doc page (e.g., `index`, `2d-index`) |
| `pkg_version` | string | For get_package_doc, optional for lookup | Package version major.minor (e.g., `17.0`) |
| `query` | string | For lookup (single) | Single search query |
| `queries` | string | For lookup (batch) | Comma-separated queries (e.g., `Physics.Raycast,NavMeshAgent,Light2D`) |

**Actions:**

- **`get_doc`**: Fetch ScriptReference docs for a class or member. Parses HTML to extract description, signatures, parameters, return type, and code examples.
- **`get_manual`**: Fetch a Unity Manual page by slug. Returns title, sections, and code examples.
- **`get_package_doc`**: Fetch package documentation. Requires package name, page slug, and package version.
- **`lookup`**: Search doc sources in parallel (ScriptReference + Manual; also package docs if `package` + `pkg_version` provided). Supports batch queries. For asset-related queries (shader, material, texture, etc.), also searches project assets via `manage_asset`.

```python
# Fetch ScriptReference for a class
unity_docs(action="get_doc", class_name="Physics")
unity_docs(action="get_doc", class_name="Physics", member_name="Raycast")
unity_docs(action="get_doc", class_name="Transform", version="6000.0.38f1")

# Fetch a Manual page
unity_docs(action="get_manual", slug="execution-order")
unity_docs(action="get_manual", slug="urp/urp-introduction")

# Fetch package documentation
unity_docs(action="get_package_doc", package="com.unity.render-pipelines.universal",
           page="2d-index", pkg_version="17.0")

# Parallel lookup across all sources (single query)
unity_docs(action="lookup", query="Physics.Raycast")

# Batch lookup (multiple queries in one call)
unity_docs(action="lookup", queries="Physics.Raycast,NavMeshAgent,Light2D")

# Lookup with package docs included
unity_docs(action="lookup", query="VolumeProfile",
           package="com.unity.render-pipelines.universal", pkg_version="17.0")
```
