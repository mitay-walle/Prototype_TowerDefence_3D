# Unity-MCP Workflow Patterns

Common workflows and patterns for effective Unity-MCP usage.

## Table of Contents

- [Setup & Verification](#setup--verification)
- [Scene Creation Workflows](#scene-creation-workflows)
- [Script Development Workflows](#script-development-workflows)
- [Asset Management Workflows](#asset-management-workflows)
- [Testing Workflows](#testing-workflows)
- [Debugging Workflows](#debugging-workflows)
- [UI Creation Workflows](#ui-creation-workflows)
- [Camera & Cinemachine Workflows](#camera--cinemachine-workflows)
- [ProBuilder Workflows](#probuilder-workflows)
- [Graphics & Rendering Workflows](#graphics--rendering-workflows)
- [Package Management Workflows](#package-management-workflows)
- [Package Deployment Workflows](#package-deployment-workflows)
- [API Verification Workflows](#api-verification-workflows)
- [Batch Operations](#batch-operations)

---

## Setup & Verification

### Unity MCP Routing

With one Unity Editor, use typed Unity MCP commands directly and do not require `mcpforunity://instances` or `unity_instance`. For multiple Unity Editors or projects, resolve the matching `Name@hash` from `mcpforunity://instances` and pass it as `unity_instance` on every tool/resource call whose schema supports it; include it in every nested `batch_execute` command. If a required multi-Editor call cannot accept `unity_instance`, stop that concurrent operation. Never use global active-instance routing as a fallback.

### Initial Connection Verification

```python
# 1. Check editor state
# Read mcpforunity://editor/state

# 2. Verify ready_for_tools == true
# If false, wait for recommended_retry_after_ms

# 3. Check active scene
# Read mcpforunity://editor/state → active_scene

# 4. Only for multi-instance work, list available instances
# Read mcpforunity://instances when more than one Unity Editor or project is active
```

### Before Any Operation

```python
# Quick readiness check pattern:
editor_state = read_resource("mcpforunity://editor/state")

if not editor_state["ready_for_tools"]:
    # Check blocking_reasons
    # Wait recommended_retry_after_ms
    pass

if editor_state["is_compiling"]:
    # Wait for compilation to complete
    pass
```

---
