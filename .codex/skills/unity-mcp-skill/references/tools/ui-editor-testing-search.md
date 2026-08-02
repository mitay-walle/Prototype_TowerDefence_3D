## UI Tools

### manage_ui

Manage Unity UI Toolkit elements: UXML documents, USS stylesheets, UIDocument components, and visual tree inspection.

```python
# Create a UXML file
manage_ui(
    action="create",
    path="Assets/UI/MainMenu.uxml",
    contents='<ui:UXML xmlns:ui="UnityEngine.UIElements"><ui:Label text="Hello" /></ui:UXML>'
)

# Create a USS stylesheet
manage_ui(
    action="create",
    path="Assets/UI/Styles.uss",
    contents=".title { font-size: 32px; color: white; }"
)

# Read a UXML/USS file
manage_ui(
    action="read",
    path="Assets/UI/MainMenu.uxml"
)
# Returns: {"success": true, "data": {"contents": "...", "path": "..."}}

# Update an existing file
manage_ui(
    action="update",
    path="Assets/UI/Styles.uss",
    contents=".title { font-size: 48px; color: yellow; -unity-font-style: bold; }"
)

# Attach UIDocument to a GameObject
manage_ui(
    action="attach_ui_document",
    target="UICanvas",                    # GameObject name or path
    source_asset="Assets/UI/MainMenu.uxml",
    panel_settings="Assets/UI/Panel.asset",  # optional, auto-creates if omitted
    sort_order=0                          # optional, default 0
)

# Create PanelSettings asset
manage_ui(
    action="create_panel_settings",
    path="Assets/UI/Panel.asset",
    scale_mode="ScaleWithScreenSize",     # optional: "ConstantPixelSize"|"ConstantPhysicalSize"|"ScaleWithScreenSize"
    reference_resolution={"width": 1920, "height": 1080}  # optional, for ScaleWithScreenSize
)

# Inspect the visual tree of a UIDocument
manage_ui(
    action="get_visual_tree",
    target="UICanvas",                    # GameObject with UIDocument
    max_depth=10                          # optional, default 10
)
# Returns: hierarchy of VisualElements with type, name, classes, styles, text, children
```

**UI Toolkit workflow:**

1. Create UXML (structure, like HTML) and USS (styling, like CSS) files
2. Create a PanelSettings asset (or let `attach_ui_document` auto-create one)
3. Create an empty GameObject and attach UIDocument with the UXML source
4. Use `get_visual_tree` to inspect the result

**Important:** Always use `<ui:Style>` (with the `ui:` namespace prefix) in UXML files, not bare `<Style>`. UI Builder will fail to open files that use `<Style>` without the prefix.

---
## Editor Control Tools

### manage_editor

Control Unity Editor state.

```python
manage_editor(action="play")               # Enter play mode
manage_editor(action="pause")              # Pause play mode
manage_editor(action="stop")               # Exit play mode

manage_editor(action="set_active_tool", tool_name="Move")  # Move/Rotate/Scale/etc.

manage_editor(action="add_tag", tag_name="Enemy")
manage_editor(action="remove_tag", tag_name="OldTag")

manage_editor(action="add_layer", layer_name="Projectiles")
manage_editor(action="remove_layer", layer_name="OldLayer")

manage_editor(action="open_prefab_stage", prefab_path="Assets/Prefabs/Enemy.prefab")
manage_editor(action="save_prefab_stage")   # Save changes in the open prefab stage
manage_editor(action="close_prefab_stage")  # Exit prefab editing mode back to main scene

# Package deployment is prohibited for this project; read packages, do not edit/deploy them.
# Do not call deploy_package/restore_package as a project fix.
manage_editor(action="deploy_package")     # Generic tool only; prohibited by project package rule
manage_editor(action="restore_package")    # Generic tool only; prohibited by project package rule
```

**Deploy workflow:** Generic MCPForUnity tool documentation only. For this project, do not deploy or restore package source; packages are read-only reference context.

### execute_menu_item

Execute any Unity menu item.

```python
execute_menu_item(menu_path="File/Save Project")
execute_menu_item(menu_path="GameObject/3D Object/Cube")
execute_menu_item(menu_path="Window/General/Console")
```

### read_console

Read or clear Unity console messages.

```python
# Get recent messages
read_console(
    action="get",
    types=["error", "warning", "log"],  # or ["all"]
    count=10,                    # max messages (ignored with paging)
    filter_text="NullReference", # optional text filter
    page_size=50,
    cursor=0,
    format="detailed",           # "plain"|"detailed"|"json"
    include_stacktrace=True
)

# Clear console
read_console(action="clear")
```

---
## Testing Tools

### run_tests

Start async test execution.

```python
result = run_tests(
    mode="EditMode",             # "EditMode"|"PlayMode"
    test_names=["MyTests.TestA", "MyTests.TestB"],  # specific tests
    group_names=["Integration*"],  # regex patterns
    category_names=["Unit"],     # NUnit categories
    assembly_names=["Tests"],    # assembly filter
    include_failed_tests=True,   # include failure details
    include_details=False        # include all test details
)
# Returns: {"job_id": "abc123", ...}
```

### get_test_job

Poll test job status.

```python
result = get_test_job(
    job_id="abc123",
    wait_timeout=60,             # wait up to N seconds
    include_failed_tests=True,
    include_details=False
)
# Returns: {"status": "complete"|"running"|"failed", "results": {...}}
```

---
## Search Tools

### find_in_file

Search file contents with regex.

```python
find_in_file(
    uri="mcpforunity://path/Assets/Scripts/MyScript.cs",
    pattern="public void \\w+",  # regex pattern
    max_results=200,
    ignore_case=True
)
# Returns: line numbers, content excerpts, match positions
```

Do not put `find_in_file` inside `batch_execute`; the batch router cannot dispatch it reliably. For multiple files or patterns, issue separate direct `mcp__unityMCP.find_in_file` calls, or use local `rg` when Unity MCP routing is not required.

---
## Custom Tools

### execute_custom_tool

Execute project-specific custom tools.

```python
execute_custom_tool(
    tool_name="my_custom_tool",
    parameters={"param1": "value", "param2": 42}
)
```

Discover available custom tools via `mcpforunity://custom-tools` resource.

---
