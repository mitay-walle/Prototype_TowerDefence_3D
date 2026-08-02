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

### Outcasts MCP Routing

Before using any project-scoped Unity MCP command in these workflows, resolve the Outcasts `Name@hash` from `mcpforunity://instances` and pass it as `unity_instance` on every tool/resource call. For `batch_execute`, include `unity_instance` in every nested command's `params`. If a required call cannot accept `unity_instance`, stop and report MCP routing is blocked. Never use global active-instance routing as a fallback.

### Initial Connection Verification

```python
# 1. Check editor state
# Read mcpforunity://editor/state

# 2. Verify ready_for_tools == true
# If false, wait for recommended_retry_after_ms

# 3. Check active scene
# Read mcpforunity://editor/state → active_scene

# 4. List available instances (multi-instance)
# Read mcpforunity://instances
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
