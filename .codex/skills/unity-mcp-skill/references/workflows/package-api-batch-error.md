## Package Management Workflows

Project rule: packages are read-only implementation context. Use package manager queries, package docs, reflection, and source reads to understand APIs. Do not edit package source, embedded packages, `Packages/`, or `Library/PackageCache/`; do not use package embedding/deployment/cache patches as a project fix.

### Install a Package and Verify

```python
# 1. Check what's installed
manage_packages(action="ping")
manage_packages(action="list_packages")
# Poll status until complete
manage_packages(action="status", job_id="<job_id>")

# 2. Install the package
manage_packages(action="add_package", package="com.unity.inputsystem")
# Poll until domain reload completes
manage_packages(action="status", job_id="<job_id>")

# 3. Verify no compilation errors
read_console(types=["error"], count=10)

# 4. Confirm it's installed
manage_packages(action="get_package_info", package="com.unity.inputsystem")
```

### Add OpenUPM Registry and Install Package

```python
# 1. Add the OpenUPM scoped registry
manage_packages(
    action="add_registry",
    name="OpenUPM",
    url="https://package.openupm.com",
    scopes=["com.cysharp"]
)

# 2. Force resolution to pick up the new registry
manage_packages(action="resolve_packages")

# 3. Install a package from OpenUPM
manage_packages(action="add_package", package="com.cysharp.unitask")
manage_packages(action="status", job_id="<job_id>")
```

### Safe Package Removal

```python
# 1. Check dependencies before removing
manage_packages(action="remove_package", package="com.unity.modules.ui")
# If blocked: "Cannot remove: 3 package(s) depend on it"

# 2. Force removal if you're sure
manage_packages(action="remove_package", package="com.unity.modules.ui", force=True)
manage_packages(action="status", job_id="<job_id>")
```

### Install from Git URL (e.g., NuGetForUnity)

```python
# Git URLs trigger a security warning — ensure the source is trusted
manage_packages(
    action="add_package",
    package="https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity"
)
manage_packages(action="status", job_id="<job_id>")
```

---

## Package Deployment Workflows

Package deployment is prohibited for this project. Do not use `deploy_package`, `restore_package`, package embedding, or package-cache edits to solve project tasks. Read package code/docs/APIs when needed, then make project-owned changes under `Assets/` or project configuration.

### Iterative Package Development Loop

Do not run this workflow for this project. It exists only as generic tool documentation and is superseded by the project package rule above. For project test execution, use `$test-writing`.

```python
# Prerequisites: Set the MCPForUnity source path in Advanced Settings first.

# 1. Make code changes (e.g., edit C# tools)
# script_apply_edits or create_script as needed

# 2. Deploy the updated package (copies source → installed package, creates backup)
manage_editor(action="deploy_package")

# 3. Wait for recompilation to finish
refresh_unity(mode="force", compile="request", wait_for_ready=True)

# 4. Check for compilation errors
read_console(types=["error"], count=10, include_stacktrace=True)

# 5. Project test execution is handled by $test-writing, not this package workflow.
```

### Rollback After Failed Deploy

```python
# Restore from the automatic pre-deployment backup
manage_editor(action="restore_package")

# Wait for recompilation
refresh_unity(mode="force", compile="request", wait_for_ready=True)
```

---

## API Verification Workflows

### Full API Verification Before Writing Code

Use `unity_reflect` and `unity_docs` to verify Unity APIs before writing C# code. This prevents hallucinated or outdated API references.

**Trust hierarchy:** reflection (live runtime) > project assets > official docs.

```python
# Step 1: Search for the type you need
unity_reflect(action="search", query="NavMesh")
# → Returns matching types: NavMeshAgent, NavMeshPath, NavMeshHit, etc.

# Step 2: Get member summary for the type
unity_reflect(action="get_type", class_name="UnityEngine.AI.NavMeshAgent")
# → Returns all methods, properties, fields (names only)

# Step 3: Get full signature for specific members you plan to use
unity_reflect(action="get_member", class_name="NavMeshAgent", member_name="SetDestination")
# → Returns parameter types, return type, all overloads

# Step 4: Get official docs for usage patterns and examples
unity_docs(action="get_doc", class_name="NavMeshAgent", member_name="SetDestination")
# → Returns description, signatures, parameters, code examples
```

### Batch API Lookup

Use `unity_docs` `lookup` action to search multiple APIs in a single call:

```python
# Search ScriptReference + Manual + package docs in parallel
unity_docs(action="lookup", queries="Physics.Raycast,NavMeshAgent,Light2D")

# Include package docs in the search
unity_docs(action="lookup", query="VolumeProfile",
           package="com.unity.render-pipelines.universal", pkg_version="17.0")
```

### Finding Shaders and Materials in Project

The `lookup` action automatically searches project assets for asset-related queries:

```python
# This searches both docs AND project assets for shader-related content
unity_docs(action="lookup", query="Lit shader")
# → Returns doc hits + matching project assets (shaders, materials, etc.)
```

### Manual and Package Documentation

```python
# Fetch Unity Manual pages (execution order, scripting concepts, etc.)
unity_docs(action="get_manual", slug="execution-order")

# Fetch package-specific documentation
unity_docs(action="get_package_doc",
           package="com.unity.render-pipelines.universal",
           page="2d-index", pkg_version="17.0")
```

### Verifying APIs Across Unity Versions

```python
# Specify Unity version for version-specific docs
unity_docs(action="get_doc", class_name="Camera", member_name="main", version="6000.0.38f1")

# Use reflection to check what's actually available in the running editor
unity_reflect(action="search", query="InputAction", scope="packages")
```

---

## Batch Operations

### Batch Discovery (Multi-Search)

Use `batch_execute` to search for multiple things in a single call instead of calling `find_gameobjects` repeatedly:

```python
# Instead of 4 separate find_gameobjects calls, batch them:
batch_execute(commands=[
    {"tool": "find_gameobjects", "params": {"search_term": "Camera", "search_method": "by_component"}},
    {"tool": "find_gameobjects", "params": {"search_term": "Rigidbody", "search_method": "by_component"}},
    {"tool": "find_gameobjects", "params": {"search_term": "Player", "search_method": "by_name"}},
    {"tool": "find_gameobjects", "params": {"search_term": "GameManager", "search_method": "by_name"}}
])
# Returns array of results, one per command
```

### Mass Property Update

```python
# Find all enemies by component
enemies = find_gameobjects(search_term="EnemyHealth", search_method="by_component")

# Update health on all enemies
commands = []
for enemy_id in enemies["ids"]:
    commands.append({
        "tool": "manage_components",
        "params": {
            "action": "set_property",
            "target": enemy_id,
            "component_type": "EnemyHealth",
            "property": "maxHealth",
            "value": 100
        }
    })

# Execute in batches
for i in range(0, len(commands), 25):
    batch_execute(commands=commands[i:i+25], parallel=True)
```

### Mass Object Creation with Variations

```python
import random

commands = []
for i in range(20):
    commands.append({
        "tool": "manage_gameobject",
        "params": {
            "action": "create",
            "name": f"Tree_{i}",
            "primitive_type": "Capsule",
            "position": [random.uniform(-50, 50), 0, random.uniform(-50, 50)],
            "scale": [1, random.uniform(2, 5), 1]
        }
    })

batch_execute(commands=commands, parallel=True)
```

### Cleanup Pattern

```python
# Find all temporary objects
temps = find_gameobjects(search_term="Temp_", search_method="by_name")

# Delete in batch
commands = [
    {"tool": "manage_gameobject", "params": {"action": "delete", "target": id}}
    for id in temps["ids"]
]

batch_execute(commands=commands, fail_fast=False)
```

---

## Error Recovery Patterns

### Stale File Recovery

```python
try:
    apply_text_edits(uri=script_uri, edits=[...], precondition_sha256=old_sha)
except Exception as e:
    if "stale_file" in str(e):
        # Re-fetch SHA
        new_sha = get_sha(uri=script_uri)
        # Retry with new SHA
        apply_text_edits(uri=script_uri, edits=[...], precondition_sha256=new_sha["sha256"])
```

### Domain Reload Recovery

```python
# After domain reload, connection may be lost
# Wait and retry pattern:
import time

max_retries = 5
for attempt in range(max_retries):
    try:
        editor_state = read_resource("mcpforunity://editor/state")
        if editor_state["ready_for_tools"]:
            break
    except:
        time.sleep(2 ** attempt)  # Exponential backoff
```

### Compilation Block Recovery

```python
# If tools fail due to compilation:
# 1. Check console for errors
errors = read_console(types=["error"], count=20)

# 2. Fix the script errors
# ... edit scripts ...

# 3. Force refresh
refresh_unity(mode="force", scope="scripts", compile="request", wait_for_ready=True)

# 4. Verify clean console
errors = read_console(types=["error"], count=5)
if not errors["messages"]:
    # Safe to proceed with tools
    pass
```
