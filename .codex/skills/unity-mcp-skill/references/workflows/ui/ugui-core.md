### uGUI (Canvas-Based) Workflows

The sections below cover legacy Canvas-based UI using `batch_execute`. Use these when working with existing uGUI projects or when UI Toolkit is not suitable.

### RectTransform Sizing (Critical for All UI Children)

Every GameObject under a Canvas gets a `RectTransform` instead of `Transform`. **Without setting anchor/size, UI elements default to zero size and won't be visible.** Use `set_property` on `RectTransform`:

```python
# Stretch to fill parent (common for panels/backgrounds)
{"tool": "manage_components", "params": {
    "action": "set_property", "target": "MyPanel",
    "component_type": "RectTransform",
    "properties": {
        "anchorMin": [0, 0],        # bottom-left corner
        "anchorMax": [1, 1],        # top-right corner
        "sizeDelta": [0, 0],        # no extra size beyond anchors
        "anchoredPosition": [0, 0]  # centered on anchors
    }
}}

# Fixed-size centered element (e.g. 300x50 button)
{"tool": "manage_components", "params": {
    "action": "set_property", "target": "MyButton",
    "component_type": "RectTransform",
    "properties": {
        "anchorMin": [0.5, 0.5],
        "anchorMax": [0.5, 0.5],
        "sizeDelta": [300, 50],
        "anchoredPosition": [0, 0]
    }
}}

# Top-anchored bar (e.g. health bar at top of screen)
{"tool": "manage_components", "params": {
    "action": "set_property", "target": "TopBar",
    "component_type": "RectTransform",
    "properties": {
        "anchorMin": [0, 1],        # left-top
        "anchorMax": [1, 1],        # right-top (stretch horizontally)
        "sizeDelta": [0, 60],       # 60px tall, full width
        "anchoredPosition": [0, -30] # offset down by half height
    }
}}
```

> **Note:** Vector2 properties accept both `[x, y]` array format and `{"x": ..., "y": ...}` object format.

### Create Canvas (Foundation for All UI)

Every UI element must be under a Canvas. A Canvas requires three components: `Canvas`, `CanvasScaler`, and `GraphicRaycaster`.

```python
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "MainCanvas"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "MainCanvas", "component_type": "Canvas"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "MainCanvas", "component_type": "CanvasScaler"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "MainCanvas", "component_type": "GraphicRaycaster"
    }},
    # renderMode: 0=ScreenSpaceOverlay, 1=ScreenSpaceCamera, 2=WorldSpace
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "MainCanvas",
        "component_type": "Canvas", "property": "renderMode", "value": 0
    }},
    # CanvasScaler: uiScaleMode 1=ScaleWithScreenSize
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "MainCanvas",
        "component_type": "CanvasScaler", "property": "uiScaleMode", "value": 1
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "MainCanvas",
        "component_type": "CanvasScaler", "property": "referenceResolution",
        "value": [1920, 1080]
    }}
])
```

### Create EventSystem (Required Once Per Scene for UI Interaction)

If no EventSystem exists in the scene, buttons and other interactive UI elements won't respond to input. Create one alongside your first Canvas. **Check `project_info.activeInputHandler` to pick the correct input module.**

```python
# For activeInputHandler == "New" or "Both" (project has Input System package):
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "EventSystem"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "EventSystem",
        "component_type": "UnityEngine.EventSystems.EventSystem"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "EventSystem",
        "component_type": "UnityEngine.InputSystem.UI.InputSystemUIInputModule"
    }}
])

# For activeInputHandler == "Old" (legacy Input Manager only):
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "EventSystem"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "EventSystem",
        "component_type": "UnityEngine.EventSystems.EventSystem"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "EventSystem",
        "component_type": "UnityEngine.EventSystems.StandaloneInputModule"
    }}
])
```

### Create Panel (Background Container)

A Panel is an Image component used as a background/container for other UI elements.

```python
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "MenuPanel", "parent": "MainCanvas"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "MenuPanel", "component_type": "Image"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "MenuPanel",
        "component_type": "Image", "property": "color",
        "value": [0.1, 0.1, 0.1, 0.8]
    }},
    # Size the panel (stretch to 60% of canvas, centered)
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "MenuPanel",
        "component_type": "RectTransform",
        "properties": {
            "anchorMin": [0.2, 0.1], "anchorMax": [0.8, 0.9],
            "sizeDelta": [0, 0], "anchoredPosition": [0, 0]
        }
    }}
])
```

### Create Text (TextMeshPro)

TextMeshProUGUI automatically adds a RectTransform when added to a child of a Canvas. If `packages.textmeshpro` is `false`, use `UnityEngine.UI.Text` instead.

```python
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "TitleText", "parent": "MenuPanel"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "TitleText",
        "component_type": "TextMeshProUGUI"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "TitleText",
        "component_type": "TextMeshProUGUI",
        "properties": {
            "text": "My Game Title",
            "fontSize": 48,
            "alignment": 514,
            "color": [1, 1, 1, 1]
        }
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "TitleText",
        "component_type": "RectTransform",
        "properties": {
            "anchorMin": [0, 0.8], "anchorMax": [1, 1],
            "sizeDelta": [0, 0], "anchoredPosition": [0, 0]
        }
    }}
])
```

> **TextMeshPro alignment values:** 257=TopLeft, 258=TopCenter, 260=TopRight, 513=MiddleLeft, 514=MiddleCenter, 516=MiddleRight, 1025=BottomLeft, 1026=BottomCenter, 1028=BottomRight.

### Create Button (With Label)

A Button needs an `Image` (visual) + `Button` (interaction) on the parent, and a child with `TextMeshProUGUI` for the label.

```python
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "StartButton", "parent": "MenuPanel"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "StartButton", "component_type": "Image"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "StartButton", "component_type": "Button"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "StartButton",
        "component_type": "Image", "property": "color",
        "value": [0.2, 0.6, 1.0, 1.0]
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "StartButton",
        "component_type": "RectTransform",
        "properties": {
            "anchorMin": [0.5, 0.5], "anchorMax": [0.5, 0.5],
            "sizeDelta": [300, 60], "anchoredPosition": [0, 0]
        }
    }},
    # Child text label (stretches to fill button)
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "StartButton_Label", "parent": "StartButton"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "StartButton_Label",
        "component_type": "TextMeshProUGUI"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "StartButton_Label",
        "component_type": "TextMeshProUGUI",
        "properties": {"text": "Start Game", "fontSize": 24, "alignment": 514}
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "StartButton_Label",
        "component_type": "RectTransform",
        "properties": {
            "anchorMin": [0, 0], "anchorMax": [1, 1],
            "sizeDelta": [0, 0], "anchoredPosition": [0, 0]
        }
    }}
])
```
