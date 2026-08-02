## UI Creation Workflows

Unity has two UI systems: **UI Toolkit** (modern, recommended) and **uGUI** (Canvas-based, legacy). Use `manage_ui` for UI Toolkit workflows, and `batch_execute` with `manage_gameobject` + `manage_components` for uGUI.

> **Template warning:** This section is a skill template library, not a guaranteed source of truth. Examples may be inaccurate for your Unity version, package setup, or project conventions.
> **Use safely:**
> 1. **Always read `mcpforunity://project/info` first** to detect installed packages and input system.
> 2. Validate component/property names against the current project.
> 3. Prefer targeting by instance ID or full path over generic names.
> 4. Treat numeric enum values as placeholders and verify before reuse.

### Step 0: Detect Project UI Capabilities

**Before creating any UI**, read project info to determine which packages and input system are available.

```python
# Read mcpforunity://project/info — returns:
# {
#   "renderPipeline": "BuiltIn" | "Universal" | "HighDefinition" | "Custom",
#   "activeInputHandler": "Old" | "New" | "Both",
#   "packages": {
#     "ugui": true/false,        — com.unity.ugui (Canvas, Image, Button, etc.)
#     "textmeshpro": true/false,  — com.unity.textmeshpro (TextMeshProUGUI)
#     "inputsystem": true/false,  — com.unity.inputsystem (new Input System)
#     "uiToolkit": true/false,    — UI Toolkit (always true for Unity 2021.3+)
#     "screenCapture": true/false  — ScreenCapture module enabled
#   }
# }
```

**Decision matrix:**

| project_info field | Value | What to use |
|---|---|---|
| `packages.uiToolkit` | `true` | **Preferred:** Use `manage_ui` for UI Toolkit (UXML/USS) |
| `packages.ugui` | `true` | Canvas-based UI (Image, Button, etc.) via `batch_execute` |
| `packages.textmeshpro` | `true` | `TextMeshProUGUI` for text (uGUI) |
| `packages.textmeshpro` | `false` | `UnityEngine.UI.Text` (legacy, lower quality) |
| `activeInputHandler` | `"Old"` | `StandaloneInputModule` for EventSystem (uGUI) |
| `activeInputHandler` | `"New"` | `InputSystemUIInputModule` for EventSystem (uGUI) |
| `activeInputHandler` | `"Both"` | Either works; prefer `InputSystemUIInputModule` for UI |

### UI Toolkit Workflows (manage_ui)

UI Toolkit uses a web-like approach: **UXML** (like HTML) for structure, **USS** (like CSS) for styling. This is the preferred UI system for new projects.

> **Important:** Always use `<ui:Style>` (with the `ui:` namespace prefix) in UXML, not bare `<Style>`. UI Builder will fail to open files that use `<Style>` without the prefix.

#### Create a Complete UI Screen

```python
# 1. Create UXML document (structure)
manage_ui(
    action="create",
    path="Assets/UI/MainMenu.uxml",
    contents='''<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements">
    <ui:Style src="Assets/UI/MainMenu.uss" />
    <ui:VisualElement name="root" class="root-container">
        <ui:Label text="My Game" class="title" />
        <ui:Button text="Play" name="play-btn" class="menu-button" />
        <ui:Button text="Settings" name="settings-btn" class="menu-button" />
        <ui:Button text="Quit" name="quit-btn" class="menu-button" />
    </ui:VisualElement>
</ui:UXML>'''
)

# 2. Create USS stylesheet (styling)
manage_ui(
    action="create",
    path="Assets/UI/MainMenu.uss",
    contents='''.root-container {
    flex-grow: 1;
    justify-content: center;
    align-items: center;
    background-color: rgba(0, 0, 0, 0.8);
}
.title {
    font-size: 48px;
    color: white;
    -unity-font-style: bold;
    margin-bottom: 40px;
}
.menu-button {
    width: 300px;
    height: 60px;
    font-size: 24px;
    margin: 8px;
    background-color: rgb(50, 120, 200);
    color: white;
    border-radius: 8px;
}
.menu-button:hover {
    background-color: rgb(70, 140, 220);
}'''
)

# 3. Create a GameObject and attach UIDocument
manage_gameobject(action="create", name="UIRoot")
manage_ui(
    action="attach_ui_document",
    target="UIRoot",
    source_asset="Assets/UI/MainMenu.uxml"
    # panel_settings auto-created if omitted
)

# 4. Verify the visual tree
manage_ui(action="get_visual_tree", target="UIRoot", max_depth=5)
```

#### Update Existing UI

```python
# Read current content
result = manage_ui(action="read", path="Assets/UI/MainMenu.uss")
# Modify and update
manage_ui(
    action="update",
    path="Assets/UI/MainMenu.uss",
    contents=".title { font-size: 64px; color: yellow; }"
)
```

#### Custom PanelSettings

```python
# Create PanelSettings with ScaleWithScreenSize
manage_ui(
    action="create_panel_settings",
    path="Assets/UI/GamePanelSettings.asset",
    scale_mode="ScaleWithScreenSize",
    reference_resolution={"width": 1920, "height": 1080}
)

# Attach UIDocument with custom PanelSettings
manage_ui(
    action="attach_ui_document",
    target="UIRoot",
    source_asset="Assets/UI/MainMenu.uxml",
    panel_settings="Assets/UI/GamePanelSettings.asset"
)
```
