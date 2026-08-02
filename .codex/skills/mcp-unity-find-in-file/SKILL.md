---
name: mcp-unity-find-in-file
description: "Search Unity files with direct sequential `mcp__unityMCP.find_in_file` calls; never route `find_in_file` through `batch_execute`."
---

# MCP Unity Find In File


## Rule

Call `mcp__unityMCP.find_in_file` directly. Never wrap it in `mcp__unityMCP.batch_execute`; the batch router does not dispatch it reliably.

## Workflow

1. Choose the narrowest URI that contains the target.
2. Call the tool directly with bounded results:
   ```text
   mcp__unityMCP.find_in_file(uri="Assets/Scripts/Foo.cs", pattern="public void \\w+", max_results=50, ignore_case=false, unity_instance="<target Name@hash>")
   ```
3. For multiple files or patterns, issue separate direct calls.
4. Before Unity MCP text edits, treat results as locators and verify exact content/SHA through the available script tools.

Use local `rg` instead when the task is plain text search and Unity Editor context is not required.

If direct `find_in_file` is unavailable and Unity MCP context matters, run one targeted `tool_search` for `Unity MCP find_in_file`. If it remains unavailable, ask the user to reconnect/enable MCP or approve local fallback. Do not use `set_active_instance` as a fallback.
