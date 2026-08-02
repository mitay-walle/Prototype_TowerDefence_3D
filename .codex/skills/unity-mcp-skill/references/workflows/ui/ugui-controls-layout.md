### Create Slider (With Reference Wiring)

A Slider requires a specific hierarchy and **must have its `fillRect` and `handleRect` references wired** to function.

```python
# Step 1: Create hierarchy
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "HealthSlider", "parent": "MainCanvas"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "HealthSlider", "component_type": "Slider"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "HealthSlider", "component_type": "Image"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "HealthSlider",
        "component_type": "RectTransform",
        "properties": {
            "anchorMin": [0.5, 0.5], "anchorMax": [0.5, 0.5],
            "sizeDelta": [400, 30], "anchoredPosition": [0, 0]
        }
    }},
    # Background
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "SliderBG", "parent": "HealthSlider"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "SliderBG", "component_type": "Image"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "SliderBG",
        "component_type": "Image", "property": "color", "value": [0.3, 0.3, 0.3, 1.0]
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "SliderBG",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [0, 0]}
    }},
    # Fill Area + Fill
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "FillArea", "parent": "HealthSlider"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "FillArea",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [0, 0]}
    }},
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "SliderFill", "parent": "FillArea"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "SliderFill", "component_type": "Image"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "SliderFill",
        "component_type": "Image", "property": "color", "value": [0.2, 0.8, 0.2, 1.0]
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "SliderFill",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [0, 0]}
    }},
    # Handle Area + Handle
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "HandleArea", "parent": "HealthSlider"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "HandleArea",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [0, 0]}
    }},
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "SliderHandle", "parent": "HandleArea"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "SliderHandle", "component_type": "Image"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "SliderHandle",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0.5, 0], "anchorMax": [0.5, 1], "sizeDelta": [20, 0]}
    }}
])

# Step 2: Wire Slider references (CRITICAL — slider won't work without this)
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "HealthSlider",
        "component_type": "Slider", "property": "fillRect",
        "value": {"name": "SliderFill"}
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "HealthSlider",
        "component_type": "Slider", "property": "handleRect",
        "value": {"name": "SliderHandle"}
    }}
])
```

### Create Input Field (With Reference Wiring)

```python
# Step 1: Create hierarchy
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "NameInput", "parent": "MenuPanel"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "NameInput", "component_type": "Image"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "NameInput",
        "component_type": "TMP_InputField"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "NameInput",
        "component_type": "RectTransform",
        "properties": {
            "anchorMin": [0.5, 0.5], "anchorMax": [0.5, 0.5],
            "sizeDelta": [400, 50], "anchoredPosition": [0, 0]
        }
    }},
    # Text Area child (clips text to input bounds)
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "InputTextArea", "parent": "NameInput"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "InputTextArea", "component_type": "RectMask2D"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "InputTextArea",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [-16, -8]}
    }},
    # Placeholder
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "InputPlaceholder", "parent": "InputTextArea"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "InputPlaceholder", "component_type": "TextMeshProUGUI"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "InputPlaceholder",
        "component_type": "TextMeshProUGUI",
        "properties": {"text": "Enter name...", "fontStyle": 2, "color": [0.5, 0.5, 0.5, 0.5]}
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "InputPlaceholder",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [0, 0]}
    }},
    # Actual text display
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "InputText", "parent": "InputTextArea"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "InputText", "component_type": "TextMeshProUGUI"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "InputText",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [0, 0]}
    }}
])

# Step 2: Wire TMP_InputField references (CRITICAL)
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "NameInput",
        "component_type": "TMP_InputField", "property": "textViewport",
        "value": {"name": "InputTextArea"}
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "NameInput",
        "component_type": "TMP_InputField", "property": "textComponent",
        "value": {"name": "InputText"}
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "NameInput",
        "component_type": "TMP_InputField", "property": "placeholder",
        "value": {"name": "InputPlaceholder"}
    }}
])
```

### Create Toggle (With Reference Wiring)

```python
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "SoundToggle", "parent": "MenuPanel"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "SoundToggle", "component_type": "Toggle"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "SoundToggle",
        "component_type": "RectTransform",
        "properties": {
            "anchorMin": [0.5, 0.5], "anchorMax": [0.5, 0.5],
            "sizeDelta": [200, 30], "anchoredPosition": [0, 0]
        }
    }},
    # Background box
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "ToggleBG", "parent": "SoundToggle"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "ToggleBG", "component_type": "Image"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "ToggleBG",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0, 0.5], "anchorMax": [0, 0.5], "sizeDelta": [26, 26], "anchoredPosition": [13, 0]}
    }},
    # Checkmark
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "ToggleCheckmark", "parent": "ToggleBG"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "ToggleCheckmark", "component_type": "Image"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "ToggleCheckmark",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0.1, 0.1], "anchorMax": [0.9, 0.9], "sizeDelta": [0, 0]}
    }},
    # Label
    {"tool": "manage_gameobject", "params": {
        "action": "create", "name": "ToggleLabel", "parent": "SoundToggle"
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "ToggleLabel", "component_type": "TextMeshProUGUI"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "ToggleLabel",
        "component_type": "TextMeshProUGUI",
        "properties": {"text": "Sound Effects", "fontSize": 18, "alignment": 513}
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "ToggleLabel",
        "component_type": "RectTransform",
        "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [-35, 0], "anchoredPosition": [17.5, 0]}
    }}
])

# Wire Toggle references (CRITICAL)
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "SoundToggle",
        "component_type": "Toggle", "property": "graphic",
        "value": {"name": "ToggleCheckmark"}
    }}
])
```

### Add Layout Group (Vertical/Horizontal/Grid)

Layout groups auto-arrange child elements, so you can skip manual RectTransform positioning for children.

```python
batch_execute(fail_fast=True, commands=[
    {"tool": "manage_components", "params": {
        "action": "add", "target": "MenuPanel",
        "component_type": "VerticalLayoutGroup"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "MenuPanel",
        "component_type": "VerticalLayoutGroup",
        "properties": {
            "spacing": 10,
            "childAlignment": 4,
            "childForceExpandWidth": True,
            "childForceExpandHeight": False,
            "padding": {"left": 20, "right": 20, "top": 20, "bottom": 20}
        }
    }},
    {"tool": "manage_components", "params": {
        "action": "add", "target": "MenuPanel",
        "component_type": "ContentSizeFitter"
    }},
    {"tool": "manage_components", "params": {
        "action": "set_property", "target": "MenuPanel",
        "component_type": "ContentSizeFitter",
        "properties": { "verticalFit": 2 }
    }}
])
```

> **childAlignment values:** 0=UpperLeft, 1=UpperCenter, 2=UpperRight, 3=MiddleLeft, 4=MiddleCenter, 5=MiddleRight, 6=LowerLeft, 7=LowerCenter, 8=LowerRight.
> **ContentSizeFitter fit modes:** 0=Unconstrained, 1=MinSize, 2=PreferredSize.

### Complete Example: Main Menu Screen

Combines multiple templates into a full menu screen in two batch calls (default 25 command limit per batch, configurable in Unity MCP Tools window up to 100). **Assumes `project_info` has been read and `activeInputHandler` is known.**

```python
# Batch 1: Canvas + EventSystem + Panel + Title
batch_execute(fail_fast=True, commands=[
    # Canvas
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "MenuCanvas"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "MenuCanvas", "component_type": "Canvas"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "MenuCanvas", "component_type": "CanvasScaler"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "MenuCanvas", "component_type": "GraphicRaycaster"}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "MenuCanvas", "component_type": "Canvas", "property": "renderMode", "value": 0}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "MenuCanvas", "component_type": "CanvasScaler", "properties": {"uiScaleMode": 1, "referenceResolution": [1920, 1080]}}},
    # EventSystem — use StandaloneInputModule OR InputSystemUIInputModule based on project_info
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "EventSystem"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "EventSystem", "component_type": "UnityEngine.EventSystems.EventSystem"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "EventSystem", "component_type": "UnityEngine.EventSystems.StandaloneInputModule"}},
    # Panel (centered, 60% width)
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "MenuPanel", "parent": "MenuCanvas"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "MenuPanel", "component_type": "Image"}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "MenuPanel", "component_type": "Image", "property": "color", "value": [0.1, 0.1, 0.15, 0.9]}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "MenuPanel", "component_type": "RectTransform", "properties": {"anchorMin": [0.2, 0.15], "anchorMax": [0.8, 0.85], "sizeDelta": [0, 0]}}},
    {"tool": "manage_components", "params": {"action": "add", "target": "MenuPanel", "component_type": "VerticalLayoutGroup"}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "MenuPanel", "component_type": "VerticalLayoutGroup", "properties": {"spacing": 20, "childAlignment": 4, "childForceExpandWidth": True, "childForceExpandHeight": False}}},
    # Title
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "Title", "parent": "MenuPanel"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "Title", "component_type": "TextMeshProUGUI"}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "Title", "component_type": "TextMeshProUGUI", "properties": {"text": "My Game", "fontSize": 64, "alignment": 514, "color": [1, 1, 1, 1]}}}
])

# Batch 2: Buttons
batch_execute(fail_fast=True, commands=[
    # Play Button
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "PlayButton", "parent": "MenuPanel"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "PlayButton", "component_type": "Image"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "PlayButton", "component_type": "Button"}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "PlayButton", "component_type": "Image", "property": "color", "value": [0.2, 0.6, 1.0, 1.0]}},
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "PlayLabel", "parent": "PlayButton"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "PlayLabel", "component_type": "TextMeshProUGUI"}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "PlayLabel", "component_type": "TextMeshProUGUI", "properties": {"text": "Play", "fontSize": 32, "alignment": 514}}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "PlayLabel", "component_type": "RectTransform", "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [0, 0]}}},
    # Settings Button
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "SettingsButton", "parent": "MenuPanel"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "SettingsButton", "component_type": "Image"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "SettingsButton", "component_type": "Button"}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "SettingsButton", "component_type": "Image", "property": "color", "value": [0.3, 0.3, 0.35, 1.0]}},
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "SettingsLabel", "parent": "SettingsButton"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "SettingsLabel", "component_type": "TextMeshProUGUI"}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "SettingsLabel", "component_type": "TextMeshProUGUI", "properties": {"text": "Settings", "fontSize": 32, "alignment": 514}}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "SettingsLabel", "component_type": "RectTransform", "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [0, 0]}}},
    # Quit Button
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "QuitButton", "parent": "MenuPanel"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "QuitButton", "component_type": "Image"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "QuitButton", "component_type": "Button"}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "QuitButton", "component_type": "Image", "property": "color", "value": [0.8, 0.2, 0.2, 1.0]}},
    {"tool": "manage_gameobject", "params": {"action": "create", "name": "QuitLabel", "parent": "QuitButton"}},
    {"tool": "manage_components", "params": {"action": "add", "target": "QuitLabel", "component_type": "TextMeshProUGUI"}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "QuitLabel", "component_type": "TextMeshProUGUI", "properties": {"text": "Quit", "fontSize": 32, "alignment": 514}}},
    {"tool": "manage_components", "params": {"action": "set_property", "target": "QuitLabel", "component_type": "RectTransform", "properties": {"anchorMin": [0, 0], "anchorMax": [1, 1], "sizeDelta": [0, 0]}}}
])
```

### UI Component Quick Reference

| UI Element | Required Components | Notes |
| ---------- | ------------------- | ----- |
| **Canvas** | Canvas + CanvasScaler + GraphicRaycaster | Root for all UI. One per screen. |
| **EventSystem** | EventSystem + input module (see below) | One per scene. Required for interaction. |
| **Panel** | Image + RectTransform sizing | Container. Set color for background. |
| **Text** | TextMeshProUGUI (or Text if no TMP) + RectTransform | Check `packages.textmeshpro`. |
| **Button** | Image + Button + child(TextMeshProUGUI) + RectTransform | Image = visual, Button = click handler. |
| **Slider** | Slider + Image + children + **wire fillRect/handleRect** | Won't function without wiring. |
| **Toggle** | Toggle + children + **wire graphic** | Wire checkmark Image to `graphic`. |
| **Input Field** | Image + TMP_InputField + children + **wire textViewport/textComponent/placeholder** | Won't function without wiring. |
| **Layout Group** | VerticalLayoutGroup / HorizontalLayoutGroup / GridLayoutGroup | Auto-arranges children; skip manual RectTransform on children. |

---
